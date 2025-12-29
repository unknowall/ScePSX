using System;

namespace LightGL
{
    unsafe public delegate void CallbackFloatPointer(float* Values);

    unsafe public struct GLMatrix4
    {
        private GLVector4 Row0;
        private GLVector4 Row1;
        private GLVector4 Row2;
        private GLVector4 Row3;

        public GLMatrix4(params double[] Arguments)
        {
            Row0 = default(GLVector4);
            Row1 = default(GLVector4);
            Row2 = default(GLVector4);
            Row3 = default(GLVector4);
            for (int n = 0; n < Arguments.Length; n++)
                this[n % 4, n / 4] = (float)Arguments[n];
        }

        public GLMatrix4(params float[] Arguments)
        {
            Row0 = default(GLVector4);
            Row1 = default(GLVector4);
            Row2 = default(GLVector4);
            Row3 = default(GLVector4);
            for (int n = 0; n < Arguments.Length; n++)
                this[n % 4, n / 4] = Arguments[n];
        }

        public float this[int Column, int Row]
        {

            get
            {
                return Get(Column, Row);
            }

            set
            {
                Set(Column, Row, value);
            }
        }


        public float Get(int Column, int Row)
        {
            fixed (GLVector4* RowPtr = &this.Row0)
                return RowPtr[Row][Column];
        }


        public void Set(int Column, int Row, float Value)
        {
            fixed (GLVector4* RowPtr = &this.Row0)
                RowPtr[Row][Column] = Value;
        }


        public void SetRow(int Row, GLVector4 Value)
        {
            fixed (GLVector4* RowPtr = &this.Row0)
                RowPtr[Row] = Value;
        }


        public GLVector4 GetRow(int Row)
        {
            fixed (GLVector4* RowPtr = &this.Row0)
                return RowPtr[Row];
        }


        public void SetColumn(int Column, GLVector4 Value)
        {
            for (int n = 0; n < 4; n++)
                this[Column, n] = Value[n];
        }


        public GLVector4 GetColumn(int Column)
        {
            var Out = default(GLVector4);
            for (int n = 0; n < 4; n++)
                Out[n] = this[Column, n];
            return Out;
        }


        public void LoadIdentity()
        {
            this.Row0.Set(1, 0, 0, 0);
            this.Row1.Set(0, 1, 0, 0);
            this.Row2.Set(0, 0, 1, 0);
            this.Row3.Set(0, 0, 0, 1);
        }


        public void Multiply(GLMatrix4 that)
        {
            for (int Row = 0; Row < 4; Row++)
            {
                var l = this.GetRow(Row);
                for (int Column = 0; Column < 4; Column++)
                {
                    var r = that.GetRow(Column);
                    this[Column, Row] = (l[0] * r[0]) + (l[1] * r[1]) + (l[2] * r[2]) + (l[3] * r[3]);
                }
            }
        }


        public void Scale(float Value)
        {
            for (int Row = 0; Row < 4; Row++)
            {
                for (int Column = 0; Column < 4; Column++)
                {
                    this[Column, Row] *= Value;
                }
            }
        }

        public void FixValues(CallbackFloatPointer Callback)
        {
            fixed (GLVector4* RowPtr = &this.Row0)
            {
                Callback((float*)RowPtr);
            }
        }

        public void Dump()
        {
            for (int n = 0; n < 4; n++)
                Console.WriteLine("{0}", this.GetRow(n).ToString());
        }

        public void Ortho(double left, double right, double bottom, double top, double near, double far)
        {
            var tx = -((right + left) / (right - left));
            var ty = -((top + bottom) / (top - bottom));
            var tz = -((far + near) / (far - near));

            this.Multiply(
                new GLMatrix4(
                    2 / (right - left), 0, 0, tx,
                    0, 2 / (top - bottom), 0, ty,
                    0, 0, -2 / (far - near), tz,
                    0, 0, 0, 1
                )
            );
        }
    }
}
