using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

using static SDL2.SDL;

namespace ScePSX
{
    class SDL2Renderer : UserControl
    {
        private int[] pixels = new int[4096 * 2048];
        private int scale, oldscale;

        private IntPtr m_Window;
        private IntPtr m_Renderer;
        private IntPtr m_Texture;
        private SDL_Rect srcRect, dstRect;

        private bool sizeing = false;
        private readonly object _renderLock = new object();
        private readonly object bufferLock = new object();

        public int frameskip, fsk = 1;

        public SDL2Renderer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer, false);
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            DoubleBuffered = false;

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(64, 64, 64);
            this.Size = new System.Drawing.Size(441, 246);
            this.Name = "SDL2Renderer";
            this.ResumeLayout(false);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            SDL_Init(SDL_INIT_VIDEO);

            SDL_SetHint(SDL_HINT_RENDER_SCALE_QUALITY, "2");

            IntPtr hwnd = new IntPtr(this.Handle);
            m_Window = SDL_CreateWindowFrom(hwnd);
            m_Renderer = SDL_CreateRenderer(m_Window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            m_Texture = SDL_CreateTexture(m_Renderer, SDL_PIXELFORMAT_ARGB8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, 1024, 512);
            SDL_RenderClear(m_Renderer);
            SDL_RenderPresent(m_Renderer);
            srcRect = new SDL_Rect
            {
                x = 0,
                y = 0,
                w = 1024,
                h = 512
            };
            dstRect = new SDL_Rect
            {
                x = 0,
                y = 0,
                w = this.Width,
                h = this.Height
            };
            //SDL事件全部给主窗口
            IntPtr p_Window = SDL_CreateWindowFrom(this.Parent.Handle);
            SDL_RaiseWindow(p_Window);
            SDL_SetWindowInputFocus(p_Window);
        }

        ~SDL2Renderer()
        {
            if (m_Texture != IntPtr.Zero)
                SDL_DestroyTexture(m_Texture);
            if (m_Renderer != IntPtr.Zero)
                SDL_DestroyRenderer(m_Renderer);
            if (m_Window != IntPtr.Zero)
                SDL_DestroyWindow(m_Window);
        }

        public void RenderBuffer(int[] pixels, int width, int height, int scale = 0)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => RenderBuffer(pixels, width, height, scale)));
                return;
            }

            if (fsk > 0)
            {
                fsk--;
                return;
            }

            lock (bufferLock)
            {
                this.pixels = pixels;
            }

            srcRect.w = width;
            srcRect.h = height;
            this.scale = scale;

            Invalidate();

            fsk = frameskip;
        }

        private void Render()
        {
            if (sizeing || this.Visible == false)
                return;

            if (scale > 0)
            {
                pixels = XbrScaler.ScaleXBR(pixels, srcRect.w, srcRect.h, scale);

                srcRect.w = srcRect.w * scale;
                srcRect.h = srcRect.h * scale;
            }

            if (oldscale != scale)
            {
                oldscale = scale;
                SDL_DestroyTexture(m_Texture);
                m_Texture = SDL_CreateTexture(m_Renderer, SDL_PIXELFORMAT_ARGB8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, srcRect.w, srcRect.h);
            }

            lock (_renderLock)
            {
                dstRect.w = this.Width;
                dstRect.h = this.Height;

                lock (bufferLock)
                {
                    SDL_UpdateTexture(m_Texture, IntPtr.Zero, Marshal.UnsafeAddrOfPinnedArrayElement<int>(pixels, 0), srcRect.w * sizeof(int));

                    SDL_RenderClear(m_Renderer);
                    SDL_RenderCopy(m_Renderer, m_Texture, ref srcRect, ref dstRect);
                    SDL_RenderPresent(m_Renderer);
                }
            }
        }

        protected override void OnResize(EventArgs e)
        {
            try
            {
                lock (_renderLock)
                {
                    sizeing = true;

                    if (m_Texture != IntPtr.Zero)
                    {
                        SDL_DestroyTexture(m_Texture);
                        m_Texture = IntPtr.Zero;
                    }
                    if (m_Renderer != IntPtr.Zero)
                    {
                        SDL_DestroyRenderer(m_Renderer);
                        m_Renderer = IntPtr.Zero;
                    }
                    dstRect.w = this.Width;
                    dstRect.h = this.Height;

                    m_Renderer = SDL_CreateRenderer(m_Window, -1, SDL_RendererFlags.SDL_RENDERER_ACCELERATED | SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
                    m_Texture = SDL_CreateTexture(m_Renderer, SDL_PIXELFORMAT_ARGB8888, (int)SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, srcRect.w, srcRect.h);

                    SDL_RenderSetViewport(m_Renderer, ref dstRect);
                    SDL_RenderSetLogicalSize(m_Renderer, dstRect.w, dstRect.h);

                    SDL_RenderClear(m_Renderer);
                    SDL_RenderPresent(m_Renderer);

                    sizeing = false;
                }

                base.OnResize(e);
            } catch
            {

            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Render();

            base.OnPaint(e);
        }
    }
}
