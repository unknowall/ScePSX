﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;
using ScePSX.Core.GPU;

namespace ScePSX
{
    public static class VectorExtensions
    {
        public static int Sum(this Vector<int> vector)
        {
            int sum = 0;
            for (int i = 0; i < Vector<int>.Count; i++)
            {
                sum += vector[i];
            }
            return sum;
        }
        // Cross 方法在 seq,avsz3,avsz4 方法不可用
        //public static Vector<int> Cross(this Vector<int> a, Vector<int> b)
        //{
        //    int ax = a[0], ay = a[1], az = a[2];
        //    int bx = b[0], by = b[1], bz = b[2];

        //    int cx = ay * bz - az * by;
        //    int cy = az * bx - ax * bz;
        //    int cz = ax * by - ay * bx;

        //    return new Vector<int>(new int[] { cx, cy, cz });
        //}
    }

    [Serializable]
    public class GTE
    { //PSX MIPS Coprocessor 02 - Geometry Transformation Engine

        private static ReadOnlySpan<byte> unrTable => new byte[] {
            0xFF, 0xFD, 0xFB, 0xF9, 0xF7, 0xF5, 0xF3, 0xF1, 0xEF, 0xEE, 0xEC, 0xEA, 0xE8, 0xE6, 0xE4, 0xE3,
            0xE1, 0xDF, 0xDD, 0xDC, 0xDA, 0xD8, 0xD6, 0xD5, 0xD3, 0xD1, 0xD0, 0xCE, 0xCD, 0xCB, 0xC9, 0xC8,
            0xC6, 0xC5, 0xC3, 0xC1, 0xC0, 0xBE, 0xBD, 0xBB, 0xBA, 0xB8, 0xB7, 0xB5, 0xB4, 0xB2, 0xB1, 0xB0,
            0xAE, 0xAD, 0xAB, 0xAA, 0xA9, 0xA7, 0xA6, 0xA4, 0xA3, 0xA2, 0xA0, 0x9F, 0x9E, 0x9C, 0x9B, 0x9A,
            0x99, 0x97, 0x96, 0x95, 0x94, 0x92, 0x91, 0x90, 0x8F, 0x8D, 0x8C, 0x8B, 0x8A, 0x89, 0x87, 0x86,
            0x85, 0x84, 0x83, 0x82, 0x81, 0x7F, 0x7E, 0x7D, 0x7C, 0x7B, 0x7A, 0x79, 0x78, 0x77, 0x75, 0x74,
            0x73, 0x72, 0x71, 0x70, 0x6F, 0x6E, 0x6D, 0x6C, 0x6B, 0x6A, 0x69, 0x68, 0x67, 0x66, 0x65, 0x64,
            0x63, 0x62, 0x61, 0x60, 0x5F, 0x5E, 0x5D, 0x5D, 0x5C, 0x5B, 0x5A, 0x59, 0x58, 0x57, 0x56, 0x55,
            0x54, 0x53, 0x53, 0x52, 0x51, 0x50, 0x4F, 0x4E, 0x4D, 0x4D, 0x4C, 0x4B, 0x4A, 0x49, 0x48, 0x48,
            0x47, 0x46, 0x45, 0x44, 0x43, 0x43, 0x42, 0x41, 0x40, 0x3F, 0x3F, 0x3E, 0x3D, 0x3C, 0x3C, 0x3B,
            0x3A, 0x39, 0x39, 0x38, 0x37, 0x36, 0x36, 0x35, 0x34, 0x33, 0x33, 0x32, 0x31, 0x31, 0x30, 0x2F,
            0x2E, 0x2E, 0x2D, 0x2C, 0x2C, 0x2B, 0x2A, 0x2A, 0x29, 0x28, 0x28, 0x27, 0x26, 0x26, 0x25, 0x24,
            0x24, 0x23, 0x22, 0x22, 0x21, 0x20, 0x20, 0x1F, 0x1E, 0x1E, 0x1D, 0x1D, 0x1C, 0x1B, 0x1B, 0x1A,
            0x19, 0x19, 0x18, 0x18, 0x17, 0x16, 0x16, 0x15, 0x15, 0x14, 0x14, 0x13, 0x12, 0x12, 0x11, 0x11,
            0x10, 0x0F, 0x0F, 0x0E, 0x0E, 0x0D, 0x0D, 0x0C, 0x0C, 0x0B, 0x0A, 0x0A, 0x09, 0x09, 0x08, 0x08,
            0x07, 0x07, 0x06, 0x06, 0x05, 0x05, 0x04, 0x04, 0x03, 0x03, 0x02, 0x02, 0x01, 0x01, 0x00, 0x00,
            0x00
        };

        [Serializable]
        private struct Matrix
        {
            public Vector3 v1;
            public Vector3 v2;
            public Vector3 v3;
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private struct Vector3
        {
            [FieldOffset(0)] public uint XY;
            [FieldOffset(0)] public short x;
            [FieldOffset(2)] public short y;
            [FieldOffset(4)] public short z;
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private struct Vector2
        {
            [FieldOffset(0)] public uint val;
            [FieldOffset(0)] public short x;
            [FieldOffset(2)] public short y;
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit, Size = 16)]
        private struct Color
        {
            [FieldOffset(0)] public uint val;
            [FieldOffset(0)] public byte r;
            [FieldOffset(1)] public byte g;
            [FieldOffset(2)] public byte b;
            [FieldOffset(3)] public byte c;
        }

        //Data Registers
        private Vector3[] V = new Vector3[3];   //R0-1 R2-3 R4-5 s16
        private Color RGBC;                     //R6
        private ushort OTZ;                     //R7
        private short[] IR = new short[4];      //R8-11
        private Vector2[] SXY = new Vector2[4]; //R12-15 FIFO
        private ushort[] SZ = new ushort[4];    //R16-19 FIFO
        private Color[] RGB = new Color[3];     //R20-22 FIFO
        private uint RES1;                      //R23 prohibited
        private int MAC0;                       //R24
        private int MAC1, MAC2, MAC3;           //R25-27
        private ushort IRGB;//, ORGB;           //R28-29 Orgb is readonly and read by irgb
        private int LZCS, LZCR;                 //R30-31

        //Control Registers
        private Matrix RT, LM, LRGB;        //R32-36 R40-44 R48-52
        private int TRX, TRY, TRZ;          //R37-39
        private int RBK, GBK, BBK;          //R45-47
        private int RFC, GFC, BFC;          //R53-55
        private int OFX, OFY, DQB;          //R56 57 60
        private ushort H;                   //R58
        private short ZSF3, ZSF4, DQA;      //R61 62 59
        private uint FLAG;                  //R63

        //Command decode
        private int sf;                     //Shift fraction (0 or 12)
        private bool lm;                    //Saturate IR1,IR2,IR3 result (0=To -8000h..+7FFFh, 1=To 0..+7FFFh)
        private uint currentCommand;        //GTE current command temporary stored for MVMVA decoding

        public bool use_pgxp = false;

        public void execute(uint command)
        {
            //Console.WriteLine($"GTE EXECUTE {(command & 0x3F):x2}");

            currentCommand = command;
            sf = (int)((command & 0x80_000) >> 19) * 12;
            lm = ((command >> 10) & 0x1) != 0;
            FLAG = 0;

            switch (command & 0x3F)
            {
                case 0x01:
                    RTPS(0, true);
                    break;
                case 0x06:
                    NCLIP();
                    break;
                case 0x0C:
                    OP();
                    break;
                case 0x10:
                    DPCS(false);
                    break;
                case 0x11:
                    INTPL();
                    break;
                case 0x12:
                    MVMVA();
                    break;
                case 0x13:
                    NCDS(0);
                    break;
                case 0x14:
                    CDP();
                    break;
                case 0x16:
                    NCDT();
                    break;
                case 0x1B:
                    NCCS(0);
                    break;
                case 0x1C:
                    CC();
                    break;
                case 0x1E:
                    NCS(0);
                    break;
                case 0x20:
                    NCT();
                    break;
                case 0x28:
                    SQR();
                    break;
                case 0x29:
                    DCPL();
                    break;
                case 0x2A:
                    DCPT();
                    break;
                case 0x2D:
                    AVSZ3();
                    break;
                case 0x2E:
                    AVSZ4();
                    break;
                case 0x30:
                    RTPT();
                    break;
                case 0x3D:
                    GPF();
                    break;
                case 0x3E:
                    GPL();
                    break;
                case 0x3F:
                    NCCT();
                    break;
                default:
                    Console.WriteLine($"UNIMPLEMENTED GTE COMMAND {command & 0x3F:x2}");
                    break;/* throw new NotImplementedException();*/
            }

            if ((FLAG & 0x7F87_E000) != 0)
            {
                FLAG |= 0x8000_0000;
            }
        }

        private void CDP()
        {
            Vector<int> bkVec = new Vector<int>(new int[]
            {
                RBK * 0x1000, // 背景红色偏移
                GBK * 0x1000, // 背景绿色偏移
                BBK * 0x1000  // 背景蓝色偏移
            });
            Vector<int> irVec = new Vector<int>(new int[] { IR[1], IR[2], IR[3] });
            Vector<int> lrgbVec = new Vector<int>(new int[] { LRGB.v1.x, LRGB.v2.y, LRGB.v3.z });
            Vector<int> result = bkVec + (lrgbVec * irVec);

            MAC1 = (int)setMAC(1, result[0] >> sf);
            MAC2 = (int)setMAC(2, result[1] >> sf);
            MAC3 = (int)setMAC(3, result[2] >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);
        }

        private void CC()
        {
            // [IR1, IR2, IR3] = [MAC1, MAC2, MAC3] = (BK * 1000h + LCM * IR) SAR(sf * 12)
            // WARNING each multiplication can trigger mac flags so the check is needed on each op! Somehow this only affects the color matrix and not the light one
            MAC1 = (int)(setMAC(1, setMAC(1, setMAC(1, (long)RBK * 0x1000 + LRGB.v1.x * IR[1]) + (long)LRGB.v1.y * IR[2]) + (long)LRGB.v1.z * IR[3]) >> sf);
            MAC2 = (int)(setMAC(2, setMAC(2, setMAC(2, (long)GBK * 0x1000 + LRGB.v2.x * IR[1]) + (long)LRGB.v2.y * IR[2]) + (long)LRGB.v2.z * IR[3]) >> sf);
            MAC3 = (int)(setMAC(3, setMAC(3, setMAC(3, (long)BBK * 0x1000 + LRGB.v3.x * IR[1]) + (long)LRGB.v3.y * IR[2]) + (long)LRGB.v3.z * IR[3]) >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            // [MAC1, MAC2, MAC3] = [R * IR1, G * IR2, B * IR3] SHL 4;
            MAC1 = (int)(setMAC(1, (long)RGBC.r * IR[1]) << 4);
            MAC2 = (int)(setMAC(2, (long)RGBC.g * IR[2]) << 4);
            MAC3 = (int)(setMAC(3, (long)RGBC.b * IR[3]) << 4);

            // [MAC1, MAC2, MAC3] = [MAC1, MAC2, MAC3] SAR(sf * 12);< --- for NCDx / NCCx
            MAC1 = (int)(setMAC(1, MAC1) >> sf);
            MAC2 = (int)(setMAC(2, MAC2) >> sf);
            MAC3 = (int)(setMAC(3, MAC3) >> sf);

            // Color FIFO = [MAC1 / 16, MAC2 / 16, MAC3 / 16, CODE], [IR1, IR2, IR3] = [MAC1, MAC2, MAC3]
            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);
        }

        private void DCPL()
        {
            //[MAC1, MAC2, MAC3] = [R*IR1, G*IR2, B*IR3] SHL 4          ;<--- for DCPL only
            MAC1 = (int)(setMAC(1, RGBC.r * IR[1]) << 4);
            MAC2 = (int)(setMAC(2, RGBC.g * IR[2]) << 4);
            MAC3 = (int)(setMAC(3, RGBC.b * IR[3]) << 4);

            interpolateColor(MAC1, MAC2, MAC3, PGXPVector.use_pgxp && PGXPVector.use_perspective_correction);

            // Color FIFO = [MAC1 / 16, MAC2 / 16, MAC3 / 16, CODE]
            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;
        }

        private void NCCT()
        {
            NCCS(0);
            NCCS(1);
            NCCS(2);
        }

        private void NCCS(int r)
        {
            MAC1 = (int)(setMAC(1, (long)LM.v1.x * V[r].x + LM.v1.y * V[r].y + LM.v1.z * V[r].z) >> sf);
            MAC2 = (int)(setMAC(2, (long)LM.v2.x * V[r].x + LM.v2.y * V[r].y + LM.v2.z * V[r].z) >> sf);
            MAC3 = (int)(setMAC(3, (long)LM.v3.x * V[r].x + LM.v3.y * V[r].y + LM.v3.z * V[r].z) >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            MAC1 = (int)(setMAC(1, setMAC(1, setMAC(1, (long)RBK * 0x1000 + LRGB.v1.x * IR[1]) + (long)LRGB.v1.y * IR[2]) + (long)LRGB.v1.z * IR[3]) >> sf);
            MAC2 = (int)(setMAC(2, setMAC(2, setMAC(2, (long)GBK * 0x1000 + LRGB.v2.x * IR[1]) + (long)LRGB.v2.y * IR[2]) + (long)LRGB.v2.z * IR[3]) >> sf);
            MAC3 = (int)(setMAC(3, setMAC(3, setMAC(3, (long)BBK * 0x1000 + LRGB.v3.x * IR[1]) + (long)LRGB.v3.y * IR[2]) + (long)LRGB.v3.z * IR[3]) >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            MAC1 = (int)setMAC(1, (RGBC.r * IR[1]) << 4);
            MAC2 = (int)setMAC(2, (RGBC.g * IR[2]) << 4);
            MAC3 = (int)setMAC(3, (RGBC.b * IR[3]) << 4);

            MAC1 = (int)setMAC(1, MAC1 >> sf);
            MAC2 = (int)setMAC(2, MAC2 >> sf);
            MAC3 = (int)setMAC(3, MAC3 >> sf);

            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);
        }

        private void DCPT()
        {
            DPCS(true);
            DPCS(true);
            DPCS(true);
        }

        private void DPCS(bool dpct)
        {
            byte r = RGBC.r;
            byte g = RGBC.g;
            byte b = RGBC.b;

            // WHEN DCPT it uses RGB FIFO instead RGBC
            if (dpct)
            {
                r = RGB[0].r;
                g = RGB[0].g;
                b = RGB[0].b;
            }
            //[MAC1, MAC2, MAC3] = [R, G, B] SHL 16
            MAC1 = (int)(setMAC(1, r) << 16);
            MAC2 = (int)(setMAC(2, g) << 16);
            MAC3 = (int)(setMAC(3, b) << 16);

            interpolateColor(MAC1, MAC2, MAC3, PGXPVector.use_pgxp && PGXPVector.use_perspective_correction);

            // Color FIFO = [MAC1 / 16, MAC2 / 16, MAC3 / 16, CODE]
            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;
        }

        private void INTPL()
        {
            if (PGXPVector.use_pgxp && PGXPVector.use_pgxp_aff)
            {
                if (PGXPVector.Find(SXY[0].x, SXY[0].y, out var p0) &&
                    PGXPVector.Find(SXY[1].x, SXY[1].y, out var p1) &&
                    PGXPVector.Find(SXY[2].x, SXY[2].y, out var p2))
                {
                    double z0 = Math.Max(p0.worldZ, 0.001);
                    double z1 = Math.Max(p1.worldZ, 0.001);
                    double z2 = Math.Max(p2.worldZ, 0.001);

                    double w0 = 1.0 / z0;
                    double w1 = 1.0 / z1;
                    double w2 = 1.0 / z2;
                    double totalWeight = w0 + w1 + w2;

                    double ir0 = IR[0];
                    double weightedIR0 = (w0 * ir0 + w1 * ir0 + w2 * ir0) / totalWeight;

                    // [MAC1, MAC2, MAC3] = ((IR1, IR2, IR3) * weightedIR0) SHL 12

                    //long mac1 = (long)((IR[1] * weightedIR0) * 4096);
                    //long mac2 = (long)((IR[2] * weightedIR0) * 4096);
                    //long mac3 = (long)((IR[3] * weightedIR0) * 4096);

                    long mac1 = clampMAC(1, (long)((IR[1] * weightedIR0) * 4096));
                    long mac2 = clampMAC(2, (long)((IR[2] * weightedIR0) * 4096));
                    long mac3 = clampMAC(3, (long)((IR[3] * weightedIR0) * 4096));

                    MAC1 = (int)setMAC(1, mac1);
                    MAC2 = (int)setMAC(2, mac2);
                    MAC3 = (int)setMAC(3, mac3);

                    interpolateColor(MAC1, MAC2, MAC3, false); //PGXPVector.use_perspective_correction

                    RGB[0] = RGB[1];
                    RGB[1] = RGB[2];
                    RGB[2].r = setRGB(1, MAC1 >> 4);
                    RGB[2].g = setRGB(2, MAC2 >> 4);
                    RGB[2].b = setRGB(3, MAC3 >> 4);
                    RGB[2].c = RGBC.c;
                } else
                {
                    BASE_INTPL();
                }
            } else
            {
                BASE_INTPL();
            }
        }

        private void BASE_INTPL()
        {
            // [MAC1, MAC2, MAC3] = [IR1, IR2, IR3] SHL 12               ;<--- for INTPL only
            MAC1 = (int)setMAC(1, (long)IR[1] << 12);
            MAC2 = (int)setMAC(2, (long)IR[2] << 12);
            MAC3 = (int)setMAC(3, (long)IR[3] << 12);

            interpolateColor(MAC1, MAC2, MAC3);

            // Color FIFO = [MAC1 / 16, MAC2 / 16, MAC3 / 16, CODE]
            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;
        }

        private void NCT()
        {
            NCS(0);
            NCS(1);
            NCS(2);
        }

        private void NCS(int r)
        {
            //In: V0 = Normal vector(for triple variants repeated with V1 and V2),
            //BK = Background color, RGBC = Primary color / code, LLM = Light matrix, LCM = Color matrix, IR0 = Interpolation value.

            // [IR1, IR2, IR3] = [MAC1, MAC2, MAC3] = (LLM * V0) SAR(sf * 12)
            MAC1 = (int)(setMAC(1, (long)LM.v1.x * V[r].x + LM.v1.y * V[r].y + LM.v1.z * V[r].z) >> sf);
            MAC2 = (int)(setMAC(2, (long)LM.v2.x * V[r].x + LM.v2.y * V[r].y + LM.v2.z * V[r].z) >> sf);
            MAC3 = (int)(setMAC(3, (long)LM.v3.x * V[r].x + LM.v3.y * V[r].y + LM.v3.z * V[r].z) >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            // [IR1, IR2, IR3] = [MAC1, MAC2, MAC3] = (BK * 1000h + LCM * IR) SAR(sf * 12)
            // WARNING each multiplication can trigger mac flags so the check is needed on each op! Somehow this only affects the color matrix and not the light one
            MAC1 = (int)(setMAC(1, setMAC(1, setMAC(1, (long)RBK * 0x1000 + LRGB.v1.x * IR[1]) + (long)LRGB.v1.y * IR[2]) + (long)LRGB.v1.z * IR[3]) >> sf);
            MAC2 = (int)(setMAC(2, setMAC(2, setMAC(2, (long)GBK * 0x1000 + LRGB.v2.x * IR[1]) + (long)LRGB.v2.y * IR[2]) + (long)LRGB.v2.z * IR[3]) >> sf);
            MAC3 = (int)(setMAC(3, setMAC(3, setMAC(3, (long)BBK * 0x1000 + LRGB.v3.x * IR[1]) + (long)LRGB.v3.y * IR[2]) + (long)LRGB.v3.z * IR[3]) >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            // Color FIFO = [MAC1 / 16, MAC2 / 16, MAC3 / 16, CODE], [IR1, IR2, IR3] = [MAC1, MAC2, MAC3]
            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);
        }

        private void MVMVA()
        {
            const int factor = 0x1000;
            uint mxIndex = (currentCommand >> 17) & 0x3; // 乘矩阵
            uint mvIndex = (currentCommand >> 15) & 0x3; // 乘向量
            uint tvIndex = (currentCommand >> 13) & 0x3; // 平移向量
            long tx, ty, tz;

            // 根据 mxIndex 选择矩阵
            Matrix mx = mxIndex switch
            {
                0 => RT,
                1 => LM,
                2 => LRGB,
                _ => new Matrix
                {
                    v1 = new Vector3 { x = (short)-(RGBC.r << 4), y = (short)(RGBC.r << 4), z = IR[0] },
                    v2 = new Vector3 { x = RT.v1.z, y = RT.v1.z, z = RT.v1.z },
                    v3 = new Vector3 { x = RT.v2.y, y = RT.v2.y, z = RT.v2.y }
                }
            };

            // 根据 mvIndex 选择向量
            Vector3 vx = mvIndex switch
            {
                0 => V[0],
                1 => V[1],
                2 => V[2],
                _ => new Vector3 { x = IR[1], y = IR[2], z = IR[3] }
            };

            // 如果 tvIndex==2，特殊处理（硬件中该向量未正确添加）
            if (tvIndex == 2)
            {
                tx = RFC;
                ty = GFC;
                tz = BFC;
                Vector4 macPart1 = new Vector4(
                    tx * factor + mx.v1.x * vx.x,
                    ty * factor + mx.v2.x * vx.x,
                    tz * factor + mx.v3.x * vx.x,
                    0);
                macPart1 /= (1 << sf);
                setIR(1, (int)macPart1.X, false);
                setIR(2, (int)macPart1.Y, false);
                setIR(3, (int)macPart1.Z, false);

                Vector4 macPart2 = new Vector4(
                    mx.v1.y * vx.y + mx.v1.z * vx.z,
                    mx.v2.y * vx.y + mx.v2.z * vx.z,
                    mx.v3.y * vx.y + mx.v3.z * vx.z,
                    0);
                macPart2 /= (1 << sf);
                MAC1 = (int)macPart2.X;
                MAC2 = (int)macPart2.Y;
                MAC3 = (int)macPart2.Z;
                IR[1] = setIR(1, MAC1, lm);
                IR[2] = setIR(2, MAC2, lm);
                IR[3] = setIR(3, MAC3, lm);
                return;
            }

            // 根据 tvIndex 计算平移向量（tvIndex==0,1，其余归零）
            if (tvIndex == 0)
            {
                tx = TRX;
                ty = TRY;
                tz = TRZ;
            } else if (tvIndex == 1)
            {
                tx = RBK;
                ty = GBK;
                tz = BBK;
            } else
            {
                tx = ty = tz = 0;
            }

            // 使用 SIMD 风格的运算进行矩阵–向量乘法运算（分两部分相加）
            Vector4 mac = new Vector4(
                tx * factor + mx.v1.x * vx.x,
                ty * factor + mx.v2.x * vx.x,
                tz * factor + mx.v3.x * vx.x,
                0);
            mac += new Vector4(
                mx.v1.y * vx.y + mx.v1.z * vx.z,
                mx.v2.y * vx.y + mx.v2.z * vx.z,
                mx.v3.y * vx.y + mx.v3.z * vx.z,
                0);
            mac /= (1 << sf);

            MAC1 = (int)mac.X;
            MAC2 = (int)mac.Y;
            MAC3 = (int)mac.Z;
            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);
        }

        private void GPL()
        {
            //[MAC1, MAC2, MAC3] = [MAC1, MAC2, MAC3] SHL(sf*12);<--- for GPL only
            //[MAC1, MAC2, MAC3] = (([IR1, IR2, IR3] * IR0) + [MAC1, MAC2, MAC3]) SAR(sf*12)
            // Color FIFO = [MAC1 / 16, MAC2 / 16, MAC3 / 16, CODE], [IR1, IR2, IR3] = [MAC1, MAC2, MAC3]
            //Note: Although the SHL in GPL is theoretically undone by the SAR, 44bit overflows can occur internally when sf=1.

            long mac1 = (long)MAC1 << sf;
            long mac2 = (long)MAC2 << sf;
            long mac3 = (long)MAC3 << sf;

            MAC1 = (int)(setMAC(1, IR[1] * IR[0] + mac1) >> sf); //this is a good example of why setMac cant return int directly
            MAC2 = (int)(setMAC(2, IR[2] * IR[0] + mac2) >> sf); //as you cant >> before cause it dosnt triggers the flags and if
            MAC3 = (int)(setMAC(3, IR[3] * IR[0] + mac3) >> sf); //you do it after you get wrong values

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;
        }

        private void GPF()
        {
            //[MAC1, MAC2, MAC3] = [0,0,0]                            ;<--- for GPF only
            //[MAC1, MAC2, MAC3] = (([IR1, IR2, IR3] * IR0) + [MAC1, MAC2, MAC3]) SAR(sf*12)
            // Color FIFO = [MAC1 / 16, MAC2 / 16, MAC3 / 16, CODE], [IR1, IR2, IR3] = [MAC1, MAC2, MAC3]

            MAC1 = (int)setMAC(1, IR[1] * IR[0]) >> sf;
            MAC2 = (int)setMAC(2, IR[2] * IR[0]) >> sf;
            MAC3 = (int)setMAC(3, IR[3] * IR[0]) >> sf;

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;
        }

        private void OP()
        {
            // 提取 RT 的对角线元素（RT 的 RT11, RT22, RT33）
            short d1 = RT.v1.x;
            short d2 = RT.v2.y;
            short d3 = RT.v3.z;

            // 计算外积各分量，结果右移 sf 位（SAR(sf)）
            int r1 = ((IR[3] * d2) - (IR[2] * d3)) >> sf;
            int r2 = ((IR[1] * d3) - (IR[3] * d1)) >> sf;
            int r3 = ((IR[2] * d1) - (IR[1] * d2)) >> sf;

            // 更新 MAC 寄存器并触发相应的 flag 检查
            MAC1 = (int)setMAC(1, r1);
            MAC2 = (int)setMAC(2, r2);
            MAC3 = (int)setMAC(3, r3);

            // 将最终结果写入 IR 寄存器
            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);
        }

        private void SQR()
        {
            MAC1 = (int)setMAC(1, (IR[1] * IR[1]) >> sf);
            MAC2 = (int)setMAC(2, (IR[2] * IR[2]) >> sf);
            MAC3 = (int)setMAC(3, (IR[3] * IR[3]) >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);
        }

        private void AVSZ3()
        {
            if (PGXPVector.use_pgxp && PGXPVector.use_pgxp_avs)
            {
                if (PGXPVector.Find(SXY[1].x, SXY[1].y, out var p1) &&
                    PGXPVector.Find(SXY[2].x, SXY[2].y, out var p2) &&
                    PGXPVector.Find(SXY[3].x, SXY[3].y, out var p3))
                {
                    double avgZ = (p1.worldZ + p2.worldZ + p3.worldZ) / 3.0;
                    long avsz3 = (long)(ZSF3 * avgZ * 4096.0);
                    MAC0 = (int)setMAC0(avsz3);
                    OTZ = setSZ3(avsz3 >> 12);
                } else
                {
                    long avsz3 = (long)ZSF3 * (SZ[1] + SZ[2] + SZ[3]);
                    MAC0 = (int)setMAC0(avsz3);
                    OTZ = setSZ3(avsz3 >> 12);
                }
            } else
            {
                long avsz3 = (long)ZSF3 * (SZ[1] + SZ[2] + SZ[3]);
                MAC0 = (int)setMAC0(avsz3);
                OTZ = setSZ3(avsz3 >> 12);
            }
        }

        private void AVSZ4()
        {
            if (PGXPVector.use_pgxp && PGXPVector.use_pgxp_avs)
            {
                if (PGXPVector.Find(SXY[0].x, SXY[0].y, out var p0) &&
                    PGXPVector.Find(SXY[1].x, SXY[1].y, out var p1) &&
                    PGXPVector.Find(SXY[2].x, SXY[2].y, out var p2) &&
                    PGXPVector.Find(SXY[3].x, SXY[3].y, out var p3))
                {
                    double avgZ = (p0.worldZ + p1.worldZ + p2.worldZ + p3.worldZ) / 4.0;
                    long avsz4 = (long)(ZSF4 * avgZ * 4096.0);
                    MAC0 = (int)setMAC0(avsz4);
                    OTZ = setSZ3(avsz4 >> 12);
                } else
                {
                    long avsz4 = (long)ZSF4 * (SZ[0] + SZ[1] + SZ[2] + SZ[3]);
                    MAC0 = (int)setMAC0(avsz4);
                    OTZ = setSZ3(avsz4 >> 12);
                }
            } else
            {
                long avsz4 = (long)ZSF4 * (SZ[0] + SZ[1] + SZ[2] + SZ[3]);
                MAC0 = (int)setMAC0(avsz4);
                OTZ = setSZ3(avsz4 >> 12);
            }
        }

        private void NCDT()
        {
            NCDS(0);
            NCDS(1);
            NCDS(2);
        }

        private void NCDS(int r)
        {
            // 根据 Vector<int>.Count 构造长度一致的数组
            int vLen = Vector<int>.Count;
            int[] vArr = new int[vLen];
            vArr[0] = V[r].x;
            vArr[1] = V[r].y;
            vArr[2] = V[r].z;
            var vecV = new Vector<int>(vArr);

            // 构造 Light Matrix 的每一行向量
            int[] lm1Arr = new int[vLen];
            lm1Arr[0] = LM.v1.x;
            lm1Arr[1] = LM.v1.y;
            lm1Arr[2] = LM.v1.z;
            var vecL1 = new Vector<int>(lm1Arr);

            int[] lm2Arr = new int[vLen];
            lm2Arr[0] = LM.v2.x;
            lm2Arr[1] = LM.v2.y;
            lm2Arr[2] = LM.v2.z;
            var vecL2 = new Vector<int>(lm2Arr);

            int[] lm3Arr = new int[vLen];
            lm3Arr[0] = LM.v3.x;
            lm3Arr[1] = LM.v3.y;
            lm3Arr[2] = LM.v3.z;
            var vecL3 = new Vector<int>(lm3Arr);

            // 第一阶段：使用 SIMD 计算点积（LLM * V[r]），再调用 setMAC 与移位
            long macL1 = setMAC(1, Vector.Dot(vecL1, vecV));
            long macL2 = setMAC(2, Vector.Dot(vecL2, vecV));
            long macL3 = setMAC(3, Vector.Dot(vecL3, vecV));
            MAC1 = (int)(macL1 >> sf);
            MAC2 = (int)(macL2 >> sf);
            MAC3 = (int)(macL3 >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            // 定义局部函数，保持链式调用顺序（触发 setMAC 的 flag 检查）
            long ChainMAC(int index, long initial, params long[] values)
            {
                long result = setMAC(index, initial);
                foreach (long v in values)
                {
                    result = setMAC(index, result + v);
                }
                return result;
            }

            // 第二阶段：利用颜色矩阵进行累加计算（保持链式调用顺序）
            MAC1 = (int)(ChainMAC(1, (long)RBK * 0x1000, LRGB.v1.x * IR[1], LRGB.v1.y * IR[2], LRGB.v1.z * IR[3]) >> sf);
            MAC2 = (int)(ChainMAC(2, (long)GBK * 0x1000, LRGB.v2.x * IR[1], LRGB.v2.y * IR[2], LRGB.v2.z * IR[3]) >> sf);
            MAC3 = (int)(ChainMAC(3, (long)BBK * 0x1000, LRGB.v3.x * IR[1], LRGB.v3.y * IR[2], LRGB.v3.z * IR[3]) >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = setIR(3, MAC3, lm);

            // 第三阶段：计算最终颜色调节
            MAC1 = (int)setMAC(1, ((long)RGBC.r * IR[1]) << 4);
            MAC2 = (int)setMAC(2, ((long)RGBC.g * IR[2]) << 4);
            MAC3 = (int)setMAC(3, ((long)RGBC.b * IR[3]) << 4);

            interpolateColor(MAC1, MAC2, MAC3, PGXPVector.use_pgxp && PGXPVector.use_perspective_correction);

            // 更新颜色 FIFO（移位操作）
            RGB[0] = RGB[1];
            RGB[1] = RGB[2];

            RGB[2].r = setRGB(1, MAC1 >> 4);
            RGB[2].g = setRGB(2, MAC2 >> 4);
            RGB[2].b = setRGB(3, MAC3 >> 4);
            RGB[2].c = RGBC.c;
        }

        private void interpolateColor(int mac1, int mac2, int mac3, bool perspectiveCorrect = false)
        {
            // PSX SPX is very convoluted about this and it lacks some info
            // [MAC1, MAC2, MAC3] = MAC + (FC - MAC) * IR0;< --- for NCDx only
            // Note: Above "[IR1,IR2,IR3]=(FC-MAC)" is saturated to - 8000h..+7FFFh(ie. as if lm = 0)
            // Details on "MAC+(FC-MAC)*IR0":
            // [IR1, IR2, IR3] = (([RFC, GFC, BFC] SHL 12) - [MAC1, MAC2, MAC3]) SAR(sf * 12)
            // [MAC1, MAC2, MAC3] = (([IR1, IR2, IR3] * IR0) + [MAC1, MAC2, MAC3])
            // [MAC1, MAC2, MAC3] = [MAC1, MAC2, MAC3] SAR(sf * 12);< --- for NCDx / NCCx
            // [IR1, IR2, IR3] = [MAC1, MAC2, MAC3]

            if (!perspectiveCorrect)
            {
                Vector4 mac = new Vector4(mac1, mac2, mac3, 0);
                Vector4 fc = new Vector4(RFC, GFC, BFC, 0);
                Vector4 ir = (fc * 0x1000 - mac) / (1 << sf);
                Vector4 min = new Vector4(-0x8000, -0x8000, -0x8000, 0);
                Vector4 max = new Vector4(0x7FFF, 0x7FFF, 0x7FFF, 0);
                ir = Vector4.Clamp(ir, min, max);

                mac = ir * IR[0] + mac;
                mac /= (1 << sf);

                MAC1 = (int)mac.X;
                MAC2 = (int)mac.Y;
                MAC3 = (int)mac.Z;

                IR[1] = setIR(1, MAC1, lm);
                IR[2] = setIR(2, MAC2, lm);
                IR[3] = setIR(3, MAC3, lm);

                return;
            }

            //if (PGXPVector.Find(SXY[0].x, SXY[0].y, out var p0) &&
            //    PGXPVector.Find(SXY[1].x, SXY[1].y, out var p1) &&
            //    PGXPVector.Find(SXY[2].x, SXY[2].y, out var p2))
            //{
            //    double z0 = p0.worldZ;
            //    double z1 = p1.worldZ;
            //    double z2 = p2.worldZ;

            //    double invZ = 1.0 / ((z0 + z1 + z2) / 3); // 平均深度倒数

            //    double w0 = 1.0 / Math.Max(z0, 0.001);
            //    double w1 = 1.0 / Math.Max(z1, 0.001);
            //    double w2 = 1.0 / Math.Max(z2, 0.001);
            //    double totalWeight = w0 + w1 + w2;

            //    double r = (double)(w0 * MAC1 + w1 * MAC2 + w2 * MAC3) / totalWeight;
            //    double g = (double)(w0 * MAC2 + w1 * MAC3 + w2 * MAC1) / totalWeight;
            //    double b = (double)(w0 * MAC3 + w1 * MAC1 + w2 * MAC2) / totalWeight;

            //    MAC1 = (int)r;
            //    MAC2 = (int)g;
            //    MAC3 = (int)b;

            //    IR[1] = setIR(1, MAC1, lm);
            //    IR[2] = setIR(2, MAC2, lm);
            //    IR[3] = setIR(3, MAC3, lm);
            //} else
            if (PGXPVector.Find(SXY[2].x, SXY[2].y, out var pgxpVertex))
            {
                double z = Math.Max(pgxpVertex.worldZ, 0.001);
                double w = 1.0 / z; // 当前顶点权重

                MAC1 = (int)(mac1 * w);
                MAC2 = (int)(mac2 * w);
                MAC3 = (int)(mac3 * w);

                IR[1] = setIR(1, MAC1, lm);
                IR[2] = setIR(2, MAC2, lm);
                IR[3] = setIR(3, MAC3, lm);
            } else
            {
                interpolateColor(mac1, mac2, mac3, false);
            }
        }

        //Normal clipping
        private void NCLIP()
        {
            if (PGXPVector.use_pgxp && PGXPVector.use_pgxp_clip)
            {
                if (PGXPVector.Find(SXY[0].x, SXY[0].y, out var p0) &&
                    PGXPVector.Find(SXY[1].x, SXY[1].y, out var p1) &&
                    PGXPVector.Find(SXY[2].x, SXY[2].y, out var p2))
                {
                    double areaSign = p0.x * p1.y + p1.x * p2.y + p2.x * p0.y
                                     - p0.x * p2.y - p1.x * p0.y - p2.x * p1.y;

                    MAC0 = (int)setMAC0((long)areaSign);
                } else
                {
                    long areaSign = (long)SXY[0].x * SXY[1].y +
                                    SXY[1].x * SXY[2].y +
                                    SXY[2].x * SXY[0].y -
                                    SXY[0].x * SXY[2].y -
                                    SXY[1].x * SXY[0].y -
                                    SXY[2].x * SXY[1].y;

                    MAC0 = (int)setMAC0(areaSign);
                }
            } else
            {
                long areaSign = (long)SXY[0].x * SXY[1].y +
                                SXY[1].x * SXY[2].y +
                                SXY[2].x * SXY[0].y -
                                SXY[0].x * SXY[2].y -
                                SXY[1].x * SXY[0].y -
                                SXY[2].x * SXY[1].y;

                MAC0 = (int)setMAC0(areaSign);
            }
        }

        //Perspective Transformation Triple
        private void RTPT()
        {
            RTPS(0, false);
            RTPS(1, false);
            RTPS(2, true);
        }

        private void RTPS(int r, bool setMac0)
        {
            if (PGXPVector.use_pgxp)
            {
                double xx = V[r].x;
                double yy = V[r].y;
                double zz = V[r].z;

                //Console.WriteLine($"[PGXP] Raw Vertex {r}: X={xx}, Y={yy}, Z={zz}");

                double worldX = TRX + (RT.v1.x * xx + RT.v1.y * yy + RT.v1.z * zz) / 4096.0;
                double worldY = TRY + (RT.v2.x * xx + RT.v2.y * yy + RT.v2.z * zz) / 4096.0;
                double worldZ = TRZ + (RT.v3.x * xx + RT.v3.y * yy + RT.v3.z * zz) / 4096.0;

                //Console.WriteLine($"[PGXP] World Transformed {r}: X={worldX:F6}, Y={worldY:F6}, Z={worldZ:F6}");

                //long mac1_val = (long)Math.Round(worldX * 4096.0);
                //long mac2_val = (long)Math.Round(worldY * 4096.0);
                //long mac3_val = (long)Math.Round(worldZ * 4096.0);

                long mac1_val = (long)(worldX * 4096);
                long mac2_val = (long)(worldY * 4096);
                long mac3_val = (long)(worldZ * 4096);

                MAC1 = (int)(setMAC(1, mac1_val) >> sf);
                MAC2 = (int)(setMAC(2, mac2_val) >> sf);
                MAC3 = (int)(setMAC(3, mac3_val) >> sf);

                IR[1] = setIR(1, MAC1, lm);
                IR[2] = setIR(2, MAC2, lm);
                IR[3] = setIR(3, MAC3, lm);

                double hFloat = H;
                double invZ = 1.0 / (Math.Abs(worldZ) > 0.001 ? worldZ : 0.001);
                double screenX = (worldX * hFloat) * invZ + (OFX / 65536.0); // 16 bit
                double screenY = (worldY * hFloat) * invZ + (OFY / 65536.0);

                //Console.WriteLine($"[PGXP] Intermediate {r}: H={hFloat}, invZ={invZ:F6}, OFX={OFX / 65536.0}, OFY={OFY / 65536.0}");

                //int sx = (int)Math.Clamp(screenX, -0x400, 0x3FF);
                //int sy = (int)Math.Clamp(screenY, -0x400, 0x3FF);

                int sx = (int)(setMAC0((long)screenX));
                int sy = (int)(setMAC0((long)screenY));

                SZ[0] = SZ[1];
                SZ[1] = SZ[2];
                SZ[2] = SZ[3];
                SZ[3] = setSZ3(mac3_val >> 12);

                SXY[0] = SXY[1];
                SXY[1] = SXY[2];
                //SXY[2].x = (short)sx;
                //SXY[2].y = (short)sy;
                SXY[2].x = setSXY(1, sx);
                SXY[2].y = setSXY(2, sy);

                PGXPVector.Add(
                    new PGXPVector.LowPos { x = SXY[2].x, y = SXY[2].y, z = (short)SZ[3] },
                    new PGXPVector.HighPos { x = screenX, y = screenY, z = invZ, worldX = worldX, worldY = worldY, worldZ = worldZ }
                );

                //Console.WriteLine();
                //Console.WriteLine($"[PGXP] Screen: ({SXY[2].x}, {SXY[2].y})");
                //Console.WriteLine($"[PGXP] MAC FIFO: [{MAC1}, {MAC2}, {MAC3}]");
                //Console.WriteLine($"[PGXP] IR FIFO: [{IR[1]}, {IR[2]}, {IR[3]}]");
                //Console.WriteLine($"[PGXP] SZ FIFO: [{SZ[0]}, {SZ[1]}, {SZ[2]}, {SZ[3]}]");
                //Console.WriteLine($"[PGXP] SXY FIFO: [({SXY[0].x}, {SXY[0].y}), ({SXY[1].x}, {SXY[1].y}), ({SXY[2].x}, {SXY[2].y})]");

                if (setMac0)
                {
                    long nn;
                    if (H < SZ[3] * 2)
                    {
                        int z = BitOperations.LeadingZeroCount(SZ[3]) - 16;
                        nn = H << z;
                        uint d = (uint)(SZ[3] << z);
                        ushort u = (ushort)(unrTable[(int)((d - 0x7FC0) >> 7)] + 0x101);
                        d = ((0x2000080 - (d * u)) >> 8);
                        d = ((0x0000080 + (d * u)) >> 8);
                        nn = (int)(((nn * d) + 0x8000) >> 16);
                    } else
                    {
                        FLAG |= 1 << 17;
                        nn = 0x1FFFF;
                    }
                    //long mac0 = setMAC0((long)(DQA * (hFloat * invZ) + DQB));
                    long mac0 = setMAC0(nn * DQA + DQB);
                    MAC0 = (int)mac0;
                    IR[0] = setIR0((int)(mac0 >> 12));
                    //Console.WriteLine($"[PGXP] setMac0 [{MAC0}] IR0 [{IR[0]}]");
                }

                //Console.WriteLine($"[PGXP] FLAG (0x{FLAG:X8})");

                //if (screenX < -0x400 || screenX > 0x3FF || screenY < -0x400 || screenY > 0x3FF)
                //{
                //    FLAG |= 0x4_0000;
                //    Console.WriteLine($"[PGXP] Screen coordinate overflow detected! ScreenX={screenX:F2}, ScreenY={screenY:F2}");
                //}

                return;
            }

            long sum1 = (long)TRX * 0x1000 + RT.v1.x * V[r].x + RT.v1.y * V[r].y + RT.v1.z * V[r].z;
            long sum2 = (long)TRY * 0x1000 + RT.v2.x * V[r].x + RT.v2.y * V[r].y + RT.v2.z * V[r].z;
            long sum3 = (long)TRZ * 0x1000 + RT.v3.x * V[r].x + RT.v3.y * V[r].y + RT.v3.z * V[r].z;

            MAC1 = (int)(setMAC(1, sum1) >> sf);
            MAC2 = (int)(setMAC(2, sum2) >> sf);
            MAC3 = (int)(setMAC(3, sum3) >> sf);

            IR[1] = setIR(1, MAC1, lm);
            IR[2] = setIR(2, MAC2, lm);
            IR[3] = (short)Math.Clamp(MAC3, lm ? 0 : -0x8000, 0x7FFF);

            SZ[0] = SZ[1];
            SZ[1] = SZ[2];
            SZ[2] = SZ[3];
            SZ[3] = setSZ3(sum3 >> 12);

            long n;
            if (H < SZ[3] * 2)
            {
                int z = BitOperations.LeadingZeroCount(SZ[3]) - 16;
                n = H << z;
                uint d = (uint)(SZ[3] << z);
                ushort u = (ushort)(unrTable[(int)((d - 0x7FC0) >> 7)] + 0x101);
                d = ((0x2000080 - (d * u)) >> 8);
                d = ((0x0000080 + (d * u)) >> 8);
                n = (int)(((n * d) + 0x8000) >> 16);
            } else
            {
                FLAG |= 1 << 17;
                n = 0x1FFFF;
            }

            int x = (int)(setMAC0(n * IR[1] + OFX) >> 16);
            int y = (int)(setMAC0(n * IR[2] + OFY) >> 16);

            SXY[0] = SXY[1];
            SXY[1] = SXY[2];
            SXY[2].x = setSXY(1, x);
            SXY[2].y = setSXY(2, y);

            //Console.WriteLine();
            //Console.WriteLine($"[RTPS] Screen: ({SXY[2].x}, {SXY[2].y})");
            //Console.WriteLine($"[RTPS] MAC FIFO: [{MAC1}, {MAC2}, {MAC3}]");
            //Console.WriteLine($"[RTPS] IR FIFO: [{IR[1]}, {IR[2]}, {IR[3]}]");
            //Console.WriteLine($"[RTPS] SZ FIFO: [{SZ[0]}, {SZ[1]}, {SZ[2]}, {SZ[3]}]");
            //Console.WriteLine($"[RTPS] SXY FIFO: [({SXY[0].x}, {SXY[0].y}), ({SXY[1].x}, {SXY[1].y}), ({SXY[2].x}, {SXY[2].y})]");

            if (setMac0)
            {
                long mac0 = setMAC0(n * DQA + DQB);
                MAC0 = (int)mac0;
                IR[0] = setIR0((int)(mac0 >> 12));
                //Console.WriteLine($"[RTPS] setMac0 [{MAC0}] IR0 [{IR[0]}]");
            }

            //Console.WriteLine($"[RTPS] FLAG (0x{FLAG:X8})");
        }

        private short setIR0(long value)
        {
            if (value < 0)
            {
                FLAG |= 0x1000;
                return 0;
            } else if (value > 0x1000)
            {
                FLAG |= 0x1000;
                return 0x1000;
            }

            return (short)value;
        }

        private short setSXY(int i, int value)
        {
            if (value < -0x400)
            {
                FLAG |= (uint)(0x4000 >> (i - 1));
                return -0x400;
            } else if (value > 0x3FF)
            {
                FLAG |= (uint)(0x4000 >> (i - 1));
                return 0x3FF;
            }

            return (short)value;
        }

        private ushort setSZ3(long value)
        {
            if (value < 0)
            {
                FLAG |= 0x4_0000;
                return 0;
            } else if (value > 0xFFFF)
            {
                FLAG |= 0x4_0000;
                return 0xFFFF;
            }

            return (ushort)value;
        }

        private byte setRGB(int i, int value)
        {
            if (value < 0)
            {
                FLAG |= (uint)0x20_0000 >> (i - 1);
                return 0;
            } else if (value > 0xFF)
            {
                FLAG |= (uint)0x20_0000 >> (i - 1);
                return 0xFF;
            }

            return (byte)value;
        }

        private short setIR(int i, int value, bool lm)
        {
            if (lm && value < 0)
            {
                FLAG |= (uint)(0x100_0000 >> (i - 1));
                return 0;
            } else if (!lm && (value < -0x8000))
            {
                FLAG |= (uint)(0x100_0000 >> (i - 1));
                return -0x8000;
            } else if (value > 0x7FFF)
            {
                FLAG |= (uint)(0x100_0000 >> (i - 1));
                return 0x7FFF;
            }

            return (short)value;
        }

        private long clampMAC(int idx, long value)
        {
            const long min = -0x8000_0000;
            const long max = 0x7FFF_FFFF;

            if (value < min)
            {
                FLAG |= (uint)(0x800_0000 >> (idx - 1));
                return min;
            }
            if (value > max)
            {
                FLAG |= (uint)(0x4000_0000 >> (idx - 1));
                return max;
            }
            return value;
        }

        private long setMAC0(long value)
        {
            if (value < -0x8000_0000)
            {
                FLAG |= 0x8000;
            } else if (value > 0x7FFF_FFFF)
            {
                FLAG |= 0x1_0000;
            }

            return value;
        }

        private long setMAC(int i, long value)
        {
            if (value < -0x800_0000_0000)
            {
                FLAG |= (uint)(0x800_0000 >> (i - 1));
            } else if (value > 0x7FF_FFFF_FFFF)
            {
                FLAG |= (uint)(0x4000_0000 >> (i - 1));
            }

            return (value << 20) >> 20;
        }

        private static byte saturateRGB(int value)
        {
            if (value < 0x00)
                return 0x00;
            else if (value > 0x1F)
                return 0x1F;
            else
                return (byte)value;
        }

        private static int leadingCount(uint v)
        {
            uint sign = (v >> 31);
            int leadingCount = 0;

            for (int i = 0; i < 32; i++)
            {
                if (v >> 31 != sign)
                    break;
                leadingCount++;
                v <<= 1;
            }

            return leadingCount;
        }

        public uint readData(uint fs)
        {
            var value = fs switch
            {
                00 => V[0].XY,
                01 => (uint)V[0].z,
                02 => V[1].XY,
                03 => (uint)V[1].z,
                04 => V[2].XY,
                05 => (uint)V[2].z,
                06 => RGBC.val,
                07 => OTZ,
                08 => (uint)IR[0],
                09 => (uint)IR[1],
                10 => (uint)IR[2],
                11 => (uint)IR[3],
                12 => SXY[0].val,
                13 => SXY[1].val,
                14 or 15 => SXY[2].val,
                16 => SZ[0],
                17 => SZ[1],
                18 => SZ[2],
                19 => SZ[3],
                20 => RGB[0].val,
                21 => RGB[1].val,
                22 => RGB[2].val,
                23 => RES1,
                24 => (uint)MAC0,
                25 => (uint)MAC1,
                26 => (uint)MAC2,
                27 => (uint)MAC3,
                28 or 29 => IRGB = (ushort)(saturateRGB(IR[3] / 0x80) << 10 | saturateRGB(IR[2] / 0x80) << 5 | saturateRGB(IR[1] / 0x80)),
                30 => (uint)LZCS,
                31 => (uint)LZCR,
                _ => 0xFFFF_FFFF,
            };

            return value;
        }

        private void AddPGXPCache(short screenX, short screenY)
        {
            if (!PGXPVector.use_pgxp)
                return;

            const double fixedDepth = 1000.0; // 默认深度值
            double invZ = 1.0 / fixedDepth;

            double worldX = (screenX - (double)OFX / 65536) * fixedDepth / H;
            double worldY = (screenY - (double)OFY / 65536) * fixedDepth / H;

            PGXPVector.Add(
                new PGXPVector.LowPos { x = screenX, y = screenY, z = 0 },
                new PGXPVector.HighPos
                {
                    x = screenX,
                    y = screenY,
                    z = invZ,
                    worldX = worldX,
                    worldY = worldY,
                    worldZ = fixedDepth
                }
            );
        }

        public void writeData(uint fs, uint v)
        {
            switch (fs)
            {
                case 00:
                    V[0].XY = v;
                    break;
                case 01:
                    V[0].z = (short)v;
                    break;
                case 02:
                    V[1].XY = v;
                    break;
                case 03:
                    V[1].z = (short)v;
                    break;
                case 04:
                    V[2].XY = v;
                    break;
                case 05:
                    V[2].z = (short)v;
                    break;
                case 06:
                    RGBC.val = v;
                    break;
                case 07:
                    OTZ = (ushort)v;
                    break;
                case 08:
                    IR[0] = (short)v;
                    break;
                case 09:
                    IR[1] = (short)v;
                    break;
                case 10:
                    IR[2] = (short)v;
                    break;
                case 11:
                    IR[3] = (short)v;
                    break;
                case 12:
                    SXY[0].val = v;
                    if (PGXPVector.use_pgxp_memcap)
                        AddPGXPCache(SXY[0].x, SXY[0].y);
                    break;
                case 13:
                    SXY[1].val = v;
                    if (PGXPVector.use_pgxp_memcap)
                        AddPGXPCache(SXY[1].x, SXY[1].y);
                    break;
                case 14:
                    SXY[2].val = v;
                    if (PGXPVector.use_pgxp_memcap)
                        AddPGXPCache(SXY[2].x, SXY[2].y);
                    break;
                case 15:
                    SXY[0] = SXY[1];
                    SXY[1] = SXY[2];
                    SXY[2].val = v;
                    if (PGXPVector.use_pgxp_memcap)
                        AddPGXPCache(SXY[2].x, SXY[2].y);
                    break; //On load mirrors 0x14 on write cycles the fifo
                case 16:
                    SZ[0] = (ushort)v;
                    break;
                case 17:
                    SZ[1] = (ushort)v;
                    break;
                case 18:
                    SZ[2] = (ushort)v;
                    break;
                case 19:
                    SZ[3] = (ushort)v;
                    break;
                case 20:
                    RGB[0].val = v;
                    break;
                case 21:
                    RGB[1].val = v;
                    break;
                case 22:
                    RGB[2].val = v;
                    break;
                case 23:
                    RES1 = v;
                    break;
                case 24:
                    MAC0 = (int)v;
                    break;
                case 25:
                    MAC1 = (int)v;
                    break;
                case 26:
                    MAC2 = (int)v;
                    break;
                case 27:
                    MAC3 = (int)v;
                    break;
                case 28:
                    IRGB = (ushort)(v & 0x7FFF);
                    IR[1] = (short)((v & 0x1F) * 0x80);
                    IR[2] = (short)(((v >> 5) & 0x1F) * 0x80);
                    IR[3] = (short)(((v >> 10) & 0x1F) * 0x80);
                    break;
                case 29: /*ORGB = (ushort)v;*/
                    break; //Only Read its set by IRGB
                case 30:
                    LZCS = (int)v;
                    LZCR = leadingCount(v);
                    break;
                case 31: /*LZCR = (int)v;*/
                    break; //Only Read its set by LZCS
            }
        }

        public uint readControl(uint fs)
        {
            var value = fs switch
            {
                00 => RT.v1.XY,
                01 => (ushort)RT.v1.z | (uint)(RT.v2.x << 16),
                02 => (ushort)RT.v2.y | (uint)(RT.v2.z << 16),
                03 => RT.v3.XY,
                04 => (uint)RT.v3.z,
                05 => (uint)TRX,
                06 => (uint)TRY,
                07 => (uint)TRZ,
                08 => LM.v1.XY,
                09 => (ushort)LM.v1.z | (uint)(LM.v2.x << 16),
                10 => (ushort)LM.v2.y | (uint)(LM.v2.z << 16),
                11 => LM.v3.XY,
                12 => (uint)LM.v3.z,
                13 => (uint)RBK,
                14 => (uint)GBK,
                15 => (uint)BBK,
                16 => LRGB.v1.XY,
                17 => (ushort)LRGB.v1.z | (uint)(LRGB.v2.x << 16),
                18 => (ushort)LRGB.v2.y | (uint)(LRGB.v2.z << 16),
                19 => LRGB.v3.XY,
                20 => (uint)LRGB.v3.z,
                21 => (uint)RFC,
                22 => (uint)GFC,
                23 => (uint)BFC,
                24 => (uint)OFX,
                25 => (uint)OFY,
                26 => (uint)(short)H,
                27 => (uint)DQA,
                28 => (uint)DQB,
                29 => (uint)ZSF3,
                30 => (uint)ZSF4,
                31 => FLAG,
                _ => 0xFFFF_FFFF,
            };

            return value;
        }

        public void writeControl(uint fs, uint v)
        {
            switch (fs)
            {
                case 00:
                    RT.v1.XY = v;
                    break;
                case 01:
                    RT.v1.z = (short)v;
                    RT.v2.x = (short)(v >> 16);
                    break;
                case 02:
                    RT.v2.y = (short)v;
                    RT.v2.z = (short)(v >> 16);
                    break;
                case 03:
                    RT.v3.XY = v;
                    break;
                case 04:
                    RT.v3.z = (short)v;
                    break;
                case 05:
                    TRX = (int)v;
                    break;
                case 06:
                    TRY = (int)v;
                    break;
                case 07:
                    TRZ = (int)v;
                    break;
                case 08:
                    LM.v1.XY = v;
                    break;
                case 09:
                    LM.v1.z = (short)v;
                    LM.v2.x = (short)(v >> 16);
                    break;
                case 10:
                    LM.v2.y = (short)v;
                    LM.v2.z = (short)(v >> 16);
                    break;
                case 11:
                    LM.v3.XY = v;
                    break;
                case 12:
                    LM.v3.z = (short)v;
                    break;
                case 13:
                    RBK = (int)v;
                    break;
                case 14:
                    GBK = (int)v;
                    break;
                case 15:
                    BBK = (int)v;
                    break;
                case 16:
                    LRGB.v1.XY = v;
                    break;
                case 17:
                    LRGB.v1.z = (short)v;
                    LRGB.v2.x = (short)(v >> 16);
                    break;
                case 18:
                    LRGB.v2.y = (short)v;
                    LRGB.v2.z = (short)(v >> 16);
                    break;
                case 19:
                    LRGB.v3.XY = v;
                    break;
                case 20:
                    LRGB.v3.z = (short)v;
                    break;
                case 21:
                    RFC = (int)v;
                    break;
                case 22:
                    GFC = (int)v;
                    break;
                case 23:
                    BFC = (int)v;
                    break;
                case 24:
                    OFX = (int)v;
                    break;
                case 25:
                    OFY = (int)v;
                    break;
                case 26:
                    H = (ushort)v;
                    break;
                case 27:
                    DQA = (short)v;
                    break;
                case 28:
                    DQB = (int)v;
                    break;
                case 29:
                    ZSF3 = (short)v;
                    break;
                case 30:
                    ZSF4 = (short)v;
                    break;
                case 31: //flag is u20 with 31 Error Flag (Bit30..23, and 18..13 ORed together)
                    FLAG = v & 0x7FFF_F000;
                    if ((FLAG & 0x7F87_E000) != 0)
                    {
                        FLAG |= 0x8000_0000;
                    }
                    break;
            }
        }

        private void debug()
        {
            string gteDebug = "GTE CONTROL\n";
            for (uint i = 0; i < 32; i++)
            {
                gteDebug += $" {i:00}: {readControl(i):x8}";
                if ((i + 1) % 4 == 0)
                    gteDebug += "\n";
            }

            gteDebug += "GTE DATA\n";
            for (uint i = 0; i < 32; i++)
            {
                gteDebug += $" {i:00}: {readData(i):x8}";
                if ((i + 1) % 4 == 0)
                    gteDebug += "\n";
            }

            Console.WriteLine(gteDebug);
            Console.ReadLine();
        }
    }
}
