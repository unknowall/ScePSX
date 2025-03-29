
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Vulkan;
using static ScePSX.VulkanDevice;
using static Vulkan.VulkanNative;

namespace ScePSX
{
    //时间线信号量必须显卡支持 1.2+ 兼容性不够好
    public unsafe class ImmediateCMD : IDisposable
    {
        VulkanDevice Device;

        readonly static int SemaphoreTypeCreateInfo = 1000207002; // VK_STRUCTURE_TYPE_SEMAPHORE_TYPE_CREATE_INFO
        readonly static int TimelineSemaphoreSubmitInfo = 1000207003; // VK_STRUCTURE_TYPE_TIMELINE_SEMAPHORE_SUBMIT_INFO
        readonly static int SemaphoreWaitInfo = 1000207004; // VK_STRUCTURE_TYPE_SEMAPHORE_WAIT_INFO

        [Flags]
        public enum VkSemaphoreWaitFlags
        {
            None = 0,
            // Vulkan 规范目前没有定义具体标志位
        }

        public enum VkSemaphoreType
        {
            Timeline = 1 // VK_SEMAPHORE_TYPE_TIMELINE
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VkSemaphoreTypeCreateInfo
        {
            public VkStructureType sType;
            public IntPtr pNext;
            public VkSemaphoreType semaphoreType;
            public ulong initialValue;
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate VkResult vkWaitSemaphoresDelegate(
            VkDevice device,
            ref VkSemaphoreWaitInfo pWaitInfo,
            ulong timeout);

        public static vkWaitSemaphoresDelegate vkWaitSemaphores;

        [StructLayout(LayoutKind.Sequential)]
        public struct VkTimelineSemaphoreSubmitInfo
        {
            public VkStructureType sType;
            public IntPtr pNext;
            public uint waitSemaphoreValueCount;  // 等待信号量值数量
            public IntPtr pWaitSemaphoreValues;   // 等待信号量值指针
            public uint signalSemaphoreValueCount; // 信号信号量值数量
            public ulong* pSignalSemaphoreValues;  // 信号信号量值指针
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct VkSemaphoreWaitInfo
        {
            public VkStructureType sType;
            public IntPtr pNext;
            public VkSemaphoreWaitFlags flags;
            public uint semaphoreCount;
            public VkSemaphore* pSemaphores;
            public uint* pValues;
        }

        private const int RING_SIZE = 3;
        private readonly VkCommandBuffer[] _commandRings = new VkCommandBuffer[RING_SIZE];
        private readonly VkFence[] _fences = new VkFence[RING_SIZE];
        private int _currentRingIndex;

        // 时间轴信号量（Vulkan 1.2+）
        private VkSemaphore _timelineSemaphore;
        private uint _timelineValue = 0;

        private VkCommandPool cmdPool;

        private readonly Stack<VkCommandBuffer> _emergencyBuffers = new();

        public ImmediateCMD(VulkanDevice device)
        {
            Device = device;

            cmdPool = Device.CreateCommandPool();

            IntPtr funcPtr = vkGetInstanceProcAddr(Device.instance, "vkWaitSemaphores");
            vkWaitSemaphores = Marshal.GetDelegateForFunctionPointer<vkWaitSemaphoresDelegate>(funcPtr);

            for (int i = 0; i < RING_SIZE; i++)
            {
                _commandRings[i] = CreateTransientCommandBuffer();
                _fences[i] = Device.CreateFence(true);
            }

            // 创建时间轴信号量
            var st = new VkSemaphoreTypeCreateInfo
            {
                sType = (VkStructureType)SemaphoreTypeCreateInfo,
                semaphoreType = VkSemaphoreType.Timeline,
                initialValue = 0
            };
            var semaphoreCreateInfo = new VkSemaphoreCreateInfo
            {
                sType = VkStructureType.SemaphoreCreateInfo,
                pNext = &st
            };

            vkCreateSemaphore(Device.device, &semaphoreCreateInfo, null, out _timelineSemaphore);
        }

        public void Dispose()
        {
            vkDestroyCommandPool(Device.device, cmdPool, 0);

            vkDestroySemaphore(Device.device, _timelineSemaphore, 0);
        }

        public VkCommandBuffer GetImmediateCommandBuffer()
        {
            if (vkGetFenceStatus(Device.device, _fences[_currentRingIndex]) == VkResult.Success)
            {
                var cmd = _commandRings[_currentRingIndex];
                vkResetCommandBuffer(cmd, VkCommandBufferResetFlags.None);
                return cmd;
            }

            if (_emergencyBuffers.TryPop(out var emergencyCmd))
            {
                vkResetCommandBuffer(emergencyCmd, VkCommandBufferResetFlags.None);
                return emergencyCmd;
            }

            var newCmd = CreateTransientCommandBuffer();
            _emergencyBuffers.Push(newCmd);
            return newCmd;
        }

        public void SubmitImmediate(VkCommandBuffer cmd)
        {
            ulong ssv =++_timelineValue;

            var signalInfo = new VkTimelineSemaphoreSubmitInfo
            {
                sType = (VkStructureType)TimelineSemaphoreSubmitInfo,
                signalSemaphoreValueCount = 1,
                pSignalSemaphoreValues = &ssv
            };

            var ts = _timelineSemaphore;

            var submitInfo = new VkSubmitInfo
            {
                pNext = &signalInfo,
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = &cmd,
                signalSemaphoreCount = 1,
                pSignalSemaphores = &ts
            };

            // 异步提交（不带围栏）
            vkQueueSubmit(Device.graphicsQueue, 1, &submitInfo, VkFence.Null);

            if (IsRingBuffer(cmd))
            {
                vkResetFences(Device.device, 1, ref _fences[_currentRingIndex]);
                _currentRingIndex = (_currentRingIndex + 1) % RING_SIZE;
            }
        }

        public void FinishSemaphore()
        {
            var ts = _timelineSemaphore;
            var tv = _timelineValue;
            var waitInfo = new VkSemaphoreWaitInfo
            {
                sType = (VkStructureType)SemaphoreWaitInfo,
                semaphoreCount = 1,
                pSemaphores = &ts,
                pValues = &tv
            };
            
            if (vkWaitSemaphores(Device.device, ref waitInfo, 0) == VkResult.Success)
            {
                while (_emergencyBuffers.Count > 0)
                {
                    var cmd = _emergencyBuffers.Pop();
                    vkResetCommandBuffer(cmd, VkCommandBufferResetFlags.None);
                    if (!IsRingBuffer(cmd))
                    {
                        vkFreeCommandBuffers(Device.device, cmdPool, 1, &cmd);
                    }
                }
            }
        }

        private bool IsRingBuffer(VkCommandBuffer cmd)
        {
            foreach (var ringCmd in _commandRings)
            {
                if (ringCmd.Handle == cmd.Handle)
                    return true;
            }
            return false;
        }

        private VkCommandBuffer CreateTransientCommandBuffer()
        {
            var allocInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo,
                commandPool = cmdPool,
                level = VkCommandBufferLevel.Primary,
                commandBufferCount = 1
            };
            vkAllocateCommandBuffers(Device.device, &allocInfo, out var cmd);
            return cmd;
        }
    }

    public class ThreadSafeQueue<T>
    {
        private readonly Queue<T> queue = new Queue<T>();
        private readonly object lockObj = new object();

        public void Enqueue(T item)
        {
            lock (lockObj)
            {
                queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            lock (lockObj)
            {
                return queue.Dequeue();
            }
        }

        public int Count
        {
            get
            {
                lock (lockObj)
                {
                    return queue.Count;
                }
            }
        }
    }

    public struct vkRectangle<T> where T : struct, IComparable<T>
    {
        public T Left
        {
            get; set;
        }
        public T Top
        {
            get; set;
        }
        public T Right
        {
            get; set;
        }
        public T Bottom
        {
            get; set;
        }

        public vkRectangle(T left, T top, T right, T bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int GetWidth()
        {
            dynamic dRight = Right, dLeft = Left;
            return (int)(dRight - dLeft);
        }

        public int GetHeight()
        {
            dynamic dBottom = Bottom, dTop = Top;
            return (int)(dBottom - dTop);
        }

        public static vkRectangle<T> FromExtents(T left, T top, T width, T height)
        {
            dynamic dLeft = left, dTop = top, dWidth = width, dHeight = height;
            return new vkRectangle<T>(left, top, dLeft + dWidth, dTop + dHeight);
        }

        public bool Intersects(vkRectangle<T> other)
        {
            return Left.CompareTo(other.Right) < 0 &&
                   Right.CompareTo(other.Left) > 0 &&
                   Top.CompareTo(other.Bottom) < 0 &&
                   Bottom.CompareTo(other.Top) > 0;
        }

        public void Grow(vkRectangle<T> bounds)
        {
            if (bounds.Left.CompareTo(Left) < 0)
                Left = bounds.Left;
            if (bounds.Top.CompareTo(Top) < 0)
                Top = bounds.Top;
            if (bounds.Right.CompareTo(Right) > 0)
                Right = bounds.Right;
            if (bounds.Bottom.CompareTo(Bottom) > 0)
                Bottom = bounds.Bottom;
        }

        public void Grow(T x, T y)
        {
            dynamic dX = x, dY = y;
            Left = (T)(dynamic)Math.Min(Convert.ToDouble(Left), Convert.ToDouble(x));
            Top = (T)(dynamic)Math.Min(Convert.ToDouble(Top), Convert.ToDouble(y));
            Right = (T)(dynamic)Math.Max(Convert.ToDouble(Right), Convert.ToDouble(x));
            Bottom = (T)(dynamic)Math.Max(Convert.ToDouble(Bottom), Convert.ToDouble(y));
        }

        public void ScaleInPlace(float scale)
        {
            Left = (T)(object)(Convert.ToInt32(Left) * scale);
            Top = (T)(object)(Convert.ToInt32(Top) * scale);
            Right = (T)(object)(Convert.ToInt32(Right) * scale);
            Bottom = (T)(object)(Convert.ToInt32(Bottom) * scale);
        }

        public vkRectangle<int> Scale(float scale)
        {
            return new vkRectangle<int>(
                (int)(Convert.ToInt32(Left) * scale),
                (int)(Convert.ToInt32(Top) * scale),
                (int)(Convert.ToInt32(Right) * scale),
                (int)(Convert.ToInt32(Bottom) * scale)
            );
        }

        public bool Empty()
        {
            return GetWidth() <= 0 || GetHeight() <= 0;
        }

        public override string ToString()
        {
            return $"Left: {Left}, Top: {Top}, Right: {Right}, Bottom: {Bottom}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkPosition
    {
        public short x;
        public short y;
        public short z;
        public short w;

        public vkPosition()
        {
            x = 0;
            y = 0;
            z = 0;
            w = 1;
        }

        public vkPosition(short x_, short y_)
        {
            x = x_;
            y = y_;
            z = 0;
            w = 1;
        }

        public vkPosition(uint param)
        {
            x = (short)((param << 5) >> 5);
            y = (short)((param >> 11) >> 5);
            z = 0;
            w = 1;
        }

        public static vkPosition operator +(vkPosition lhs, vkPosition rhs)
        {
            return new vkPosition((short)(lhs.x + rhs.x), (short)(lhs.y + rhs.y));
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkTexCoord
    {
        public short u;
        public short v;

        public vkTexCoord()
        {
            u = 0;
            v = 0;
        }

        public vkTexCoord(short u_, short v_)
        {
            u = u_;
            v = v_;
        }

        public vkTexCoord(uint gpuParam)
        {
            u = (short)(gpuParam & 0xff);
            v = (short)((gpuParam >> 8) & 0xff);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkClutAttribute
    {
        public const ushort WriteMask = 0x7FFF; // 低 15 位

        private ushort value;

        public ushort Value
        {
            get => (ushort)(value & WriteMask);
            set => this.value = (ushort)(value & WriteMask);
        }

        // x: 6 bits (bits 0-5)
        public byte X
        {
            get => (byte)(value & 0x3F); // 提取低 6 位
            set => this.value = (ushort)((this.value & ~0x3F) | (value & 0x3F)); // 设置低 6 位
        }

        // y: 9 bits (bits 6-14)
        public ushort Y
        {
            get => (ushort)((value >> 6) & 0x1FF); // 提取第 6-14 位
            set => this.value = (ushort)((this.value & ~(0x1FF << 6)) | ((value & 0x1FF) << 6)); // 设置第 6-14 位
        }

        public vkClutAttribute(ushort v)
        {
            value = (ushort)(v & WriteMask);
        }

        public override string ToString()
        {
            return $"Value: {value:X4}, X: {X}, Y: {Y}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkTexPage
    {
        public const ushort WriteMask = 0x09FF; //低 12 位

        private ushort value;

        public ushort Value
        {
            get => (ushort)(value & WriteMask);
            set => this.value = (ushort)(value & WriteMask);
        }

        // texturePageBaseX: 4 bits (bits 0-3)
        public byte TexturePageBaseX
        {
            get => (byte)(value & 0x000F); // 提取低 4 位
            set => value = (byte)((value & ~0x000F) | (value & 0x000F)); // 设置低 4 位
        }

        // texturePageBaseY: 1 bit (bit 4)
        public byte TexturePageBaseY
        {
            get => (byte)((value >> 4) & 0x01); // 提取第 4 位
            set => value = (byte)((value & ~(1 << 4)) | ((value & 0x01) << 4)); // 设置第 4 位
        }

        // semiTransparencymode: 2 bits (bits 5-6)
        public byte SemiTransparencymode
        {
            get => (byte)((value >> 5) & 0x03); // 提取第 5-6 位
            set => value = (byte)((value & ~(3 << 5)) | ((value & 0x03) << 5)); // 设置第 5-6 位
        }

        // texturePageColors: 2 bits (bits 7-8)
        public byte TexturePageColors
        {
            get => (byte)((value >> 7) & 0x03); // 提取第 7-8 位
            set => value = (byte)((value & ~(3 << 7)) | ((value & 0x03) << 7)); // 设置第 7-8 位
        }

        // textureDisable: 1 bit (bit 11)
        public bool TextureDisable
        {
            get => ((value >> 11) & 0x01) != 0; // 提取第 11 位
            set => this.value = (ushort)((this.value & ~(1 << 11)) | ((value ? 1 : 0) << 11)); // 设置第 11 位
        }

        public vkTexPage(ushort v)
        {
            value = (ushort)(v & WriteMask);
        }

        public override string ToString()
        {
            return $"Value: {value:X4}, " +
                   $"TexturePageBaseX: {TexturePageBaseX}, " +
                   $"TexturePageBaseY: {TexturePageBaseY}, " +
                   $"SemiTransparencymode: {SemiTransparencymode}, " +
                   $"TexturePageColors: {TexturePageColors}, " +
                   $"TextureDisable: {TextureDisable}";
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct vkColor
    {
        public byte r; // 范围 [0, 255]
        public byte g;
        public byte b;
        public byte a; // 对齐填充

        public vkColor(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = 0;
        }

        public static vkColor FromUInt32(uint color)
        {
            return new vkColor(
                (byte)((color >> 16) & 0xFF),
                (byte)((color >> 8) & 0xFF),
                (byte)((color >> 0) & 0xFF)
            );
        }
    }

    public static class vkShaders
    {
        public readonly static byte[] drawvert = new byte[] { };
        public readonly static byte[] drawfrag = new byte[] { };

        public readonly static byte[] out24vert = new byte[] { };
        public readonly static byte[] out24frag = new byte[] { };

        public readonly static byte[] out16vert = new byte[] { };
        public readonly static byte[] out16frag = new byte[] { };


    }

}
