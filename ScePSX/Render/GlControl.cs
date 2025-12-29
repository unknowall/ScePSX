using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ScePSX.GL;

namespace ScePSX.Render
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
                this.Context = GlContextFactory.CreateFromWindowHandle(this.Handle);
				this.Context.MakeCurrent();
			}
		}

		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (!DesignMode)
			{
				this.Context.Dispose();
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
			}
			else
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
			Gl.ClearColor(0, 0, 0, 1);
			Gl.Clear(Gl.GL_COLOR_BUFFER_BIT);
			if (RenderFrame != null) RenderFrame();
			Context.SwapBuffers();
		}

		sealed protected override void OnPaint(PaintEventArgs e)
		{
			if (!DesignMode)
			{
				this.Context.MakeCurrent();
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
			this.SuspendLayout();

			this.Name = "GLControl";
			this.Load += new System.EventHandler(this.GLControl_Load);
			this.ResumeLayout(false);
		}

		private void GLControl_Load(object sender, EventArgs e)
		{

		}
	}
}
