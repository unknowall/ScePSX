using Android.Content;
using Android.Graphics;
using Android.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using static ScePSX.Controller;

#pragma warning disable CS8602
#pragma warning disable CS8604
#pragma warning disable CS8618

namespace ScePSX
{
    public class VirtualGamepadOverlay : View
    {
        private List<ButtonConfig> buttons = new List<ButtonConfig>();
        private Paint paint = new Paint();
        private Paint textPaint = new Paint();
        private Dictionary<int, ButtonConfig> pointerToButton = new Dictionary<int, ButtonConfig>();

        // 顶部控制栏相关
        private bool isTopBarExpanded = false;
        private RectF topBarRect = new RectF();
        private RectF cheatsButtonRect = new RectF();
        private RectF saveStateRect = new RectF();
        private RectF loadStateRect = new RectF();
        private RectF undoRect = new RectF();
        private RectF slotMinusRect = new RectF();
        private RectF slotPlusRect = new RectF();
        private RectF slotNumberRect = new RectF();
        public int currentSlot = 0;
        private const int MAX_SLOT = 9;
        public bool IsCheat = false;

        // 拖动相关
        private bool isDragging = false;
        private int dragPointerId = -1;
        private float dragStartX, dragStartY;
        private ButtonConfig? dragButton = null;
        private string dragMode = ""; // "dpad", "action", "single"
        private long dragStartTime = 0;
        private const long LONG_PRESS_TIME = 300; // 300ms长按触发拖动

        // 区域偏移量
        private float dpadOffsetX = 0;
        private float dpadOffsetY = 0;
        private float actionOffsetX = 0;
        private float actionOffsetY = 0;

        // 区域基础位置
        private readonly float dpadBaseX = 0.20f;
        private readonly float dpadBaseY = 0.50f;
        private readonly float actionBaseX = 0.80f;
        private readonly float actionBaseY = 0.55f;

        // 原始位置记录
        private Dictionary<string, (float x, float y)> originalPositions = new Dictionary<string, (float x, float y)>();

        // 记录每个指针的按下时间和位置
        private Dictionary<int, (long time, float x, float y)> pointerDownInfo = new Dictionary<int, (long, float, float)>();

        public event Action<string, InputAction, bool> OnButtonStateChanged;
        public event Action<string> OnTopBarAction; // 顶部栏动作：cheats, save, load, undo, slot_change

        public VirtualGamepadOverlay(Context context) : base(context)
        {
            paint.TextSize = 40;
            paint.Color = Color.Argb(180, 255, 255, 255);
            paint.SetStyle(Paint.Style.Fill);
            paint.TextAlign = Paint.Align.Center;

            textPaint.TextSize = 36;
            textPaint.Color = Color.White;
            textPaint.SetStyle(Paint.Style.Fill);
            textPaint.TextAlign = Paint.Align.Center;

            var initialButtons = new[] {
                new ButtonConfig("SELECT", InputAction.Select, 0.40f, 0.85f),
                new ButtonConfig("START", InputAction.Start, 0.60f, 0.85f),

                new ButtonConfig("□", InputAction.Square, 0.80f, 0.70f),
                new ButtonConfig("△", InputAction.Triangle, 0.90f, 0.55f),
                new ButtonConfig("○", InputAction.Circle, 0.80f, 0.40f),
                new ButtonConfig("×", InputAction.Cross, 0.70f, 0.55f),

                new ButtonConfig("L1", InputAction.L1, 0.10f, 0.35f), // 从0.25f改为0.35f
                new ButtonConfig("L2", InputAction.L2, 0.10f, 0.20f), // 从0.10f改为0.20f
                new ButtonConfig("R1", InputAction.R1, 0.90f, 0.35f), // 从0.25f改为0.35f
                new ButtonConfig("R2", InputAction.R2, 0.90f, 0.20f), // 从0.10f改为0.20f

                new ButtonConfig("▲", InputAction.DPadUp, 0.20f, 0.40f),
                new ButtonConfig("▼", InputAction.DPadDown, 0.20f, 0.60f),
                new ButtonConfig("◀", InputAction.DPadLeft, 0.10f, 0.50f),
                new ButtonConfig("▶", InputAction.DPadRight, 0.30f, 0.50f)
            };

            foreach (var btn in initialButtons)
            {
                originalPositions[btn.Label] = (btn.PositionX, btn.PositionY);
            }

            SetupButtons(initialButtons);
            Touch += OnTouch;
        }

        public void SetupButtons(ButtonConfig[] buttonConfigs)
        {
            buttons.Clear();
            buttons.AddRange(buttonConfigs);
            Invalidate();
        }

        private void OnTouch(object? sender, TouchEventArgs e)
        {
            e.Handled = true;

            var motionEvent = e.Event;
            if (motionEvent == null)
                return;

            int pointerIndex = motionEvent.ActionIndex;
            int pointerId = motionEvent.GetPointerId(pointerIndex);
            float x = motionEvent.GetX(pointerIndex);
            float y = motionEvent.GetY(pointerIndex);
            int width = Width;
            int height = Height;

            switch (motionEvent.ActionMasked)
            {
                case MotionEventActions.Down:
                case MotionEventActions.PointerDown:
                    // 先检查是否点击了顶部栏区域
                    float topBarTouchHeight = isTopBarExpanded ? 200 : 50;
                    if (y < topBarTouchHeight && x > 0 && x < width)
                    {
                        HandleTopBarTouch(x, y, width, height);
                        break;
                    }

                    // 记录按下信息
                    pointerDownInfo[pointerId] = (motionEvent.EventTime, x, y);

                    // 立即处理按钮按下
                    CheckAndAssignButton(pointerId, x, y, width, height, true);
                    break;

                case MotionEventActions.Up:
                case MotionEventActions.PointerUp:
                case MotionEventActions.Cancel:
                    // 清除按下信息
                    pointerDownInfo.Remove(pointerId);

                    if (isDragging && pointerId == dragPointerId)
                    {
                        StopDrag();
                    }

                    // 处理按钮抬起
                    ReleaseButton(pointerId);
                    break;

                case MotionEventActions.Move:
                    // 先处理正常的按钮状态更新
                    for (int i = 0; i < motionEvent.PointerCount; i++)
                    {
                        int id = motionEvent.GetPointerId(i);
                        float touchX = motionEvent.GetX(i);
                        float touchY = motionEvent.GetY(i);

                        // 如果触摸在顶部栏区域，跳过按钮处理
                        float topBarMoveHeight = isTopBarExpanded ? 200 : 50;
                        if (touchY < topBarMoveHeight)
                            continue;

                        // 检查是否需要进入拖动模式
                        if (!isDragging && pointerDownInfo.ContainsKey(id))
                        {
                            var downInfo = pointerDownInfo[id];
                            float moveDelta = (float)Math.Sqrt(
                                Math.Pow(touchX - downInfo.x, 2) +
                                Math.Pow(touchY - downInfo.y, 2));

                            bool isLongPress = (motionEvent.EventTime - downInfo.time) >= LONG_PRESS_TIME;
                            bool isMoving = moveDelta > 20; // 移动超过20像素

                            if (isLongPress || isMoving)
                            {
                                // 找到这个指针按下的按钮
                                ButtonConfig? pressedButton = FindHitButton(downInfo.x, downInfo.y, width, height);

                                if (pressedButton != null)
                                {
                                    // 进入拖动模式
                                    if (IsDpadButton(pressedButton.Label))
                                    {
                                        StartDrag(id, touchX, touchY, "dpad", pressedButton);
                                    } else if (IsActionButton(pressedButton.Label))
                                    {
                                        StartDrag(id, touchX, touchY, "action", pressedButton);
                                    } else
                                    {
                                        StartDrag(id, touchX, touchY, "single", pressedButton);
                                    }

                                    // 释放按钮按下状态
                                    if (pointerToButton.ContainsKey(id))
                                    {
                                        pointerToButton.Remove(id);
                                        pressedButton.isPressed = false;
                                        OnButtonStateChanged?.Invoke(pressedButton.Label, pressedButton.Button, false);
                                    }
                                }
                            }
                        }

                        // 如果不是拖动模式，正常更新按钮状态
                        if (!isDragging || id != dragPointerId)
                        {
                            CheckAndUpdateButton(id, touchX, touchY, width, height);
                        }
                    }

                    // 处理拖动
                    if (isDragging && pointerId == dragPointerId)
                    {
                        float deltaX = (x - dragStartX) / width;
                        float deltaY = (y - dragStartY) / height;

                        if (dragMode == "dpad")
                        {
                            dpadOffsetX += deltaX;
                            dpadOffsetY += deltaY;
                            UpdateDpadPositions();
                        } else if (dragMode == "action")
                        {
                            actionOffsetX += deltaX;
                            actionOffsetY += deltaY;
                            UpdateActionPositions();
                        } else if (dragMode == "single" && dragButton != null)
                        {
                            dragButton.PositionX += deltaX;
                            dragButton.PositionY += deltaY;
                        }

                        dragStartX = x;
                        dragStartY = y;
                    }
                    break;
            }

            Invalidate();
        }

        private void HandleTopBarTouch(float x, float y, int width, int height)
        {
            if (isTopBarExpanded)
            {
                // 检查各个按钮
                if (cheatsButtonRect.Contains(x, y))
                {
                    IsCheat = !IsCheat;
                    OnTopBarAction?.Invoke("cheats");
                } else if (saveStateRect.Contains(x, y))
                {
                    OnTopBarAction?.Invoke("save");
                } else if (loadStateRect.Contains(x, y))
                {
                    OnTopBarAction?.Invoke("load");
                } else if (undoRect.Contains(x, y))
                {
                    OnTopBarAction?.Invoke("undo");
                } else if (slotMinusRect.Contains(x, y))
                {
                    currentSlot = (currentSlot - 1 + MAX_SLOT + 1) % (MAX_SLOT + 1);
                    OnTopBarAction?.Invoke($"slot_change:{currentSlot}");
                } else if (slotPlusRect.Contains(x, y))
                {
                    currentSlot = (currentSlot + 1) % (MAX_SLOT + 1);
                    OnTopBarAction?.Invoke($"slot_change:{currentSlot}");
                } else if (y < 200)
                {
                    // 点击折叠区域
                    isTopBarExpanded = false;
                }
            } else
            {
                // 点击折叠的顶部栏展开
                isTopBarExpanded = true;
            }
        }

        private bool IsDpadButton(string label)
        {
            return label == "▲" || label == "▼" || label == "◀" || label == "▶";
        }

        private bool IsActionButton(string label)
        {
            return label == "□" || label == "△" || label == "○" || label == "×";
        }

        private void StartDrag(int pointerId, float touchX, float touchY, string mode, ButtonConfig button)
        {
            isDragging = true;
            dragPointerId = pointerId;
            dragMode = mode;
            dragStartX = touchX;
            dragStartY = touchY;
            dragButton = button;
        }

        private void StopDrag()
        {
            isDragging = false;
            dragPointerId = -1;
            dragButton = null;
            dragMode = "";
        }

        private void UpdateDpadPositions()
        {
            foreach (var btn in buttons)
            {
                if (IsDpadButton(btn.Label))
                {
                    var original = originalPositions[btn.Label];
                    btn.PositionX = original.x + dpadOffsetX;
                    btn.PositionY = original.y + dpadOffsetY;
                }
            }
        }

        private void UpdateActionPositions()
        {
            foreach (var btn in buttons)
            {
                if (IsActionButton(btn.Label))
                {
                    var original = originalPositions[btn.Label];
                    btn.PositionX = original.x + actionOffsetX;
                    btn.PositionY = original.y + actionOffsetY;
                }
            }
        }

        private void CheckAndAssignButton(int pointerId, float touchX, float touchY, int width, int height, bool isPressed)
        {
            ButtonConfig? hitButton = FindHitButton(touchX, touchY, width, height);

            if (hitButton != null)
            {
                pointerToButton[pointerId] = hitButton;

                bool oldState = hitButton.isPressed;
                if (oldState != isPressed)
                {
                    hitButton.isPressed = isPressed;
                    OnButtonStateChanged?.Invoke(hitButton.Label, hitButton.Button, isPressed);
                }
            }
        }

        private void CheckAndUpdateButton(int pointerId, float touchX, float touchY, int width, int height)
        {
            if (pointerToButton.TryGetValue(pointerId, out ButtonConfig? btn))
            {
                ButtonConfig? hitButton = FindHitButton(touchX, touchY, width, height);

                if (hitButton == null || hitButton.Label != btn.Label)
                {
                    ReleaseButton(pointerId);
                }
            }
        }

        private ButtonConfig? FindHitButton(float touchX, float touchY, int width, int height)
        {
            foreach (var btn in buttons)
            {
                float btnX = btn.PositionX * width;
                float btnY = btn.PositionY * height;
                float radius = 80;

                float distance = (float)Math.Sqrt(
                    Math.Pow(touchX - btnX, 2) +
                    Math.Pow(touchY - btnY, 2)
                );

                if (distance < radius)
                {
                    return btn;
                }
            }
            return null;
        }

        private void ReleaseButton(int pointerId)
        {
            if (pointerToButton.TryGetValue(pointerId, out ButtonConfig? btn))
            {
                pointerToButton.Remove(pointerId);

                if (!pointerToButton.ContainsValue(btn))
                {
                    bool oldState = btn.isPressed;
                    if (oldState != false)
                    {
                        btn.isPressed = false;
                        OnButtonStateChanged?.Invoke(btn.Label, btn.Button, false);
                    }
                }
            }
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            int width = Width;
            int height = Height;

            // 绘制顶部栏
            DrawTopBar(canvas, width);

            // 绘制游戏按钮
            foreach (var btn in buttons)
            {
                float x = btn.PositionX * width;
                float y = btn.PositionY * height;

                float circleRadius = 45;
                float textSize = 22;

                if (btn.Label == "SELECT" || btn.Label == "START")
                {
                    circleRadius = 40;
                    textSize = 18;
                } else if (IsDpadButton(btn.Label))
                {
                    circleRadius = 50;
                    textSize = 30;
                } else if (btn.Label == "L1" || btn.Label == "L2" || btn.Label == "R1" || btn.Label == "R2")
                {
                    circleRadius = 40;
                    textSize = 18;
                }

                if (btn.isPressed)
                {
                    paint.Color = Color.Argb(220, 255, 100, 100);
                } else
                {
                    paint.Color = Color.Argb(140, 100, 100, 100);
                }

                canvas.DrawCircle(x, y, circleRadius, paint);

                paint.Color = Color.White;
                paint.TextSize = textSize;

                float textX = x;
                float textY = y + (textSize / 3);

                canvas.DrawText(btn.Label, textX, textY, paint);
            }
        }

        private void DrawTopBar(Canvas canvas, int width)
        {
            float barHeight = isTopBarExpanded ? 100 : 50;
            topBarRect.Set(0, 0, width, barHeight);

            if (!isTopBarExpanded)
            {
                paint.Color = Color.Argb(200, 30, 30, 30);
                paint.SetStyle(Paint.Style.Fill);
                RectF recv = new RectF();
                recv.Set(10, 15, 30, 40);
                canvas.DrawRect(recv, paint);

                // 未展开时，只显示 >>> 在左边
                textPaint.TextSize = 30;
                textPaint.Color = Color.White;
                canvas.DrawText("➡️", 30, 35, textPaint);
            } else
            {
                topBarRect.Set(0, 0, width, 120);

                paint.Color = Color.Argb(200, 30, 30, 30);
                paint.SetStyle(Paint.Style.Fill);
                canvas.DrawRect(topBarRect, paint);

                // 展开时显示 <<< 在左边
                textPaint.TextSize = 30;
                textPaint.Color = Color.White;
                canvas.DrawText("⬅️", 30, 35, textPaint);

                float buttonWidth = width / 8f;
                float buttonHeight = 60;
                float startY = 30;

                // 金手指按钮（绿色圈）
                cheatsButtonRect.Set(buttonWidth * 0.5f, startY, buttonWidth * 1.5f, startY + buttonHeight);
                if (IsCheat)
                    paint.Color = Color.Argb(200, 76, 175, 80); // 绿色
                else
                    paint.Color = Color.Argb(220, 255, 100, 100);
                canvas.DrawCircle(cheatsButtonRect.CenterX(), cheatsButtonRect.CenterY(), 30, paint);

                textPaint.TextSize = 22;
                canvas.DrawText("金手指", cheatsButtonRect.CenterX(), cheatsButtonRect.CenterY() + 8, textPaint);

                // 即时保存
                saveStateRect.Set(buttonWidth * 2, startY, buttonWidth * 3, startY + buttonHeight);
                paint.Color = Color.Argb(140, 100, 100, 100);
                canvas.DrawRect(saveStateRect, paint);
                canvas.DrawText("即时保存", saveStateRect.CenterX(), saveStateRect.CenterY() + 10, textPaint);

                // 即时加载
                loadStateRect.Set(buttonWidth * 3.2f, startY, buttonWidth * 4.2f, startY + buttonHeight);
                paint.Color = Color.Argb(140, 100, 100, 100);
                canvas.DrawRect(loadStateRect, paint);
                canvas.DrawText("即时加载", loadStateRect.CenterX(), loadStateRect.CenterY() + 10, textPaint);

                // 撤销
                undoRect.Set(buttonWidth * 4.4f, startY, buttonWidth * 5.4f, startY + buttonHeight);
                paint.Color = Color.Argb(140, 100, 100, 100);
                canvas.DrawRect(undoRect, paint);
                canvas.DrawText("撤销", undoRect.CenterX(), undoRect.CenterY() + 10, textPaint);

                // 存档位控制
                float slotStartX = buttonWidth * 5.8f;

                // 减号按钮
                slotMinusRect.Set(slotStartX, startY, slotStartX + buttonHeight, startY + buttonHeight);
                paint.Color = Color.Argb(200, 100, 100, 100);
                canvas.DrawRect(slotMinusRect, paint);
                textPaint.TextSize = 40;
                canvas.DrawText("-", slotMinusRect.CenterX(), slotMinusRect.CenterY() + 15, textPaint);

                // 存档位数字
                slotNumberRect.Set(slotStartX + buttonHeight + 10, startY, slotStartX + buttonHeight * 2 + 10, startY + buttonHeight);
                paint.Color = Color.Argb(200, 80, 80, 80);
                canvas.DrawRect(slotNumberRect, paint);
                canvas.DrawText(currentSlot.ToString(), slotNumberRect.CenterX(), slotNumberRect.CenterY() + 10, textPaint);

                // 加号按钮
                slotPlusRect.Set(slotStartX + buttonHeight * 2 + 20, startY, slotStartX + buttonHeight * 3 + 20, startY + buttonHeight);
                paint.Color = Color.Argb(200, 100, 100, 100);
                canvas.DrawRect(slotPlusRect, paint);
                canvas.DrawText("+", slotPlusRect.CenterX(), slotPlusRect.CenterY() + 15, textPaint);
            }
        }

        public void ResetPositions()
        {
            dpadOffsetX = 0;
            dpadOffsetY = 0;
            actionOffsetX = 0;
            actionOffsetY = 0;

            foreach (var btn in buttons)
            {
                var original = originalPositions[btn.Label];
                btn.PositionX = original.x;
                btn.PositionY = original.y;
            }
            Invalidate();
        }

        public int GetCurrentSlot() => currentSlot;
        public void SetCurrentSlot(int slot)
        {
            currentSlot = Math.Max(0, Math.Min(MAX_SLOT, slot));
            Invalidate();
        }
    }

    public class ButtonConfig
    {
        public string Label;
        public float PositionX;
        public float PositionY;
        public InputAction Button;
        public bool isPressed;

        public ButtonConfig(string label, InputAction button, float x, float y)
        {
            Label = label;
            Button = button;
            PositionX = x;
            PositionY = y;
        }
    }
}
