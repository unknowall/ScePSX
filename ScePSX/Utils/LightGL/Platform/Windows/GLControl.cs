using System;
using System.Drawing;
using System.Windows.Forms;

namespace LightGL.Windows
{
    unsafe public class GLControl : UserControl
    {
        protected IGlContext Context;

        public GLControl()
        {
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode)
            {
                Context = GlContextFactory.CreateFromWindowHandle(Handle);
                Context.MakeCurrent();
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (!DesignMode)
            {
                Context.Dispose();
            }
            base.OnHandleDestroyed(e);
        }

        bool MustRefresh = true;

        public void ReDraw()
        {
            if (MustRefresh)
            {
                MustRefresh = false;
                Refresh();
            } else
            {
                Context.MakeCurrent();
                OnDrawFrame();
            }
        }

        //public override void Refresh()
        //{
        //	Context.MakeCurrent();
        //	OnDrawFrame();
        //}

        virtual protected void OnDrawFrame()
        {
            GL.ClearColor(0, 0, 0, 1);
            GL.Clear(GL.GL_COLOR_BUFFER_BIT);
            if (RenderFrame != null)
                RenderFrame();
            Context.SwapBuffers();
        }

        sealed protected override void OnPaint(PaintEventArgs e)
        {
            if (!DesignMode)
            {
                Context.MakeCurrent();
                OnDrawFrame();
            }
        }

        public event Action RenderFrame;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (DesignMode)
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.BlueViolet), e.ClipRectangle);
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            Name = "GLControl";
            Load += new EventHandler(GLControl_Load);
            ResumeLayout(false);
        }

        private void GLControl_Load(object sender, EventArgs e)
        {

        }
    }
}
