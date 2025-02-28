using System;
using System.Drawing;
using System.Windows.Forms;
using static SDL2.SDL;

namespace ScePSX
{
    class SDL2Renderer : Panel
    {
        private int[] pixels = new int[4096 * 2048];
        private int scale, oldscale;

        private IntPtr m_Window;
        private IntPtr m_Renderer;
        private IntPtr m_Texture;
        private SDL_Rect srcRect, dstRect;

        private bool sizeing = false;
        private readonly object _renderLock = new object();

        public SDL2Renderer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            SetStyle(ControlStyles.DoubleBuffer, false);
            DoubleBuffered = false;
            SetStyle(ControlStyles.ResizeRedraw, true);
            SetStyle(ControlStyles.UserPaint, true);
            UpdateStyles();

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

            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.Name = "SDL2Renderer";
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.Size = new System.Drawing.Size(441, 246);
            this.ResumeLayout(false);
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
            this.pixels = pixels;

            srcRect.w = width;
            srcRect.h = height;
            this.scale = scale;

            Invalidate();
        }

        private unsafe void Render()
        {
            if (sizeing)
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

                fixed (int* ptr = pixels)
                {
                    SDL_UpdateTexture(m_Texture, IntPtr.Zero, (IntPtr)ptr, srcRect.w * sizeof(int));
                }
                SDL_RenderClear(m_Renderer);
                SDL_RenderCopy(m_Renderer, m_Texture, ref srcRect, ref dstRect);
                SDL_RenderPresent(m_Renderer);
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
            //base.OnPaint(e);
        }
    }
}
