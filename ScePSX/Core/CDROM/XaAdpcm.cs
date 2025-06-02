using System;
using System.Collections.Generic;

namespace ScePSX.CdRom
{
    [Serializable]
    public struct XaVolume
    {
        public byte LtoL;
        public byte LtoR;
        public byte RtoL;
        public byte RtoR;
    }

    [Serializable]
    public class XaAdpcm
    {
        short[] left_Buffer = new short[2016];
        short[] right_Buffer = new short[2016];
        short[] mono_Buffer = new short[4032];

        short old_left;
        short old_right;
        short older_left;
        short older_right;
        short[][] ringbuf = {
            new short[32],  //L (or Mono, for some reason) 
            new short[32]   //R
        };
        private int[] pos_xa_adpcm_table = { 0, +60, +115, +98, +122 };
        private int[] neg_xa_adpcm_table = { 0, 0, -52, -55, -60 };

        int sixstep = 6; //The initial counter value on power-up is uninitialized random
        int p = 0;

        public void handle_XA_ADPCM(ReadOnlySpan<byte> XA_ADPCM_Sector, byte codingInfo, XaVolume currentVolume, ref Queue<short> output)
        {
            //finalBuffer.Clear(); //Clear last sector samples 

            bool isStereo = (codingInfo & 0x3) == 1;
            bool is18900Hz = ((codingInfo >> 2) & 0x3) == 1;
            bool is8BitsPerSample = ((codingInfo >> 4) & 0x3) == 1;

            //if (is8BitsPerSample)
            //{
            //    Console.WriteLine("8 Bits Per Sample");
            //}

            ReadOnlySpan<byte> data = XA_ADPCM_Sector.Slice(12 + 4 + 8);    //skip sync, header, subheader
            int dst_Mono = 0;
            int dst_Stereo_L = 0;
            int dst_Stereo_R = 0;
            Span<short> mono = new Span<short>(mono_Buffer);
            Span<short> left = new Span<short>(left_Buffer);
            Span<short> right = new Span<short>(right_Buffer);

            for (int i = 0; i < 0x12; i++)
            {
                for (int blk = 0; blk < 4; blk++)
                { //8-bits mode?
                    if (isStereo)
                    {
                        decode_28_nibbles(data, left, blk, 0, ref dst_Stereo_L, ref old_left, ref older_left);
                        decode_28_nibbles(data, right, blk, 1, ref dst_Stereo_R, ref old_right, ref older_right);
                    } else
                    {
                        decode_28_nibbles(data, mono, blk, 0, ref dst_Mono, ref old_left, ref older_left);
                        decode_28_nibbles(data, mono, blk, 1, ref dst_Mono, ref old_left, ref older_left);
                    }
                }
                data = data.Slice(128);
            }

            if (isStereo)
            {
                List<short> L = resample(left, is18900Hz, 0);
                List<short> R = resample(right, is18900Hz, 1);
                for (int i = 0; i < L.Count; i++)
                {
                    short leftSample = L[i];
                    short rightSample = R[i];

                    int leftOutput = ((leftSample * currentVolume.LtoL) >> 7) + ((rightSample * currentVolume.RtoL) >> 7);
                    int rightOutput = ((rightSample * currentVolume.RtoR) >> 7) + ((leftSample * currentVolume.LtoR) >> 7);

                    short finalLeft = (short)Math.Clamp(leftOutput, -0x8000, 0x7FFF);
                    short finalRight = (short)Math.Clamp(rightOutput, -0x8000, 0x7FFF);

                    output.Enqueue(finalLeft);
                    output.Enqueue(finalRight);
                }
            } else
            {
                List<short> M = resample(mono, is18900Hz, 0);
                for (int i = 0; i < M.Count; i++)
                {
                    short leftSample = M[i];    //M to L
                    short rightSample = M[i];   //M to R

                    //Apply volume, could be wrong here because mono...
                    int leftOutput = ((leftSample * currentVolume.LtoL) >> 7) + ((rightSample * currentVolume.RtoL) >> 7);
                    int rightOutput = ((rightSample * currentVolume.RtoR) >> 7) + ((leftSample * currentVolume.LtoR) >> 7);

                    short finalLeft = (short)Math.Clamp(leftOutput, -0x8000, 0x7FFF);
                    short finalRight = (short)Math.Clamp(rightOutput, -0x8000, 0x7FFF);
                    output.Enqueue(finalLeft);
                    output.Enqueue(finalRight);

                    //The reason why leftSample = M[i] and rightSample = M[i] is because
                    //Naudio expects Stereo buffer [first L sample then R sample],
                    //so we output the same sample twice, one for L and one for R
                    //It is worth noting that the result is much better that using dst = dst + 2 as in psxspx
                }
            }
        }
        private void decode_28_nibbles(ReadOnlySpan<byte> src, Span<short> buffer, int blk, int nibble, ref int dst, ref short old, ref short older)
        {

            int shift = 12 - (src[4 + blk * 2 + nibble] & 0x0F);
            int filter = (src[4 + blk * 2 + nibble] & 0x30) >> 4;
            int f0 = pos_xa_adpcm_table[filter];
            int f1 = neg_xa_adpcm_table[filter];
            for (int j = 0; j < 28; j++)
            {
                int t = signed4Bits((byte)((src[16 + blk + j * 4] >> (nibble * 4)) & 0x0F));
                int s = (t << shift) + ((old * f0 + older * f1 + 32) / 64);
                s = Math.Clamp(s, -0x8000, +0x7FFF);
                buffer[dst] = (short)s;
                dst = dst + 1;
                older = old;
                old = (short)s;
            }   // dst = dst + 2 (what psx-spx does) will break stereo

        }

        private List<short> resample(ReadOnlySpan<short> buffer, bool is18900Hz, int channel)
        {
            List<short> output = new List<short>();
            for (int i = 0; i < buffer.Length; i++)
            {
                short sample = buffer[i];
                ringbuf[channel][p & 0x1F] = sample;
                p = p + 1;
                sixstep = sixstep - 1;
                if (sixstep <= 0)
                {
                    sixstep = 6;
                    for (int j = 0; j < 7; j++)
                    {
                        short resampled = (short)ZigZagInterpolate(p, j, channel);
                        output.Add(resampled);
                        if (is18900Hz)
                        {
                            output.Add(resampled);  //Double for 18900 Hz
                        }
                    }
                }
            }
            return output;
        }

        private short ZigZagInterpolate(int p, int tableNumber, int channel)
        {
            int sum = 0;
            for (int i = 0; i < 29; i++)
            {
                sum = sum + ((ringbuf[channel][(p - i) & 0x1F] * zigZagTable[tableNumber][i]) / 0x8000);
            }
            return (short)Math.Clamp(sum, -0x8000, +0x7FFF);
        }

        private int signed4Bits(byte value)
        {
            return (value << 28) >> 28; //Raise the sign (bit 3) then sign extend down
        }

        private short[][] zigZagTable = new short[][] {
                     new short[]{       0,       0,       0,       0,       0, -0x0002,  0x000A, -0x0022,   //Table 0
                                   0x0041, -0x0054,  0x0034,  0x0009, -0x010A,  0x0400, -0x0A78,  0x234C,
                                   0x6794, -0x1780,  0x0BCD, -0x0623,  0x0350, -0x016D,  0x006B,  0x000A,
                                  -0x0010,  0x0011, -0x0008,  0x0003, -0x0001},

                     new short[]{       0,       0,       0, -0x0002,       0,  0x0003, -0x0013,  0x003C,   //Table 1
                                  -0x004B,  0x00A2, -0x00E3,  0x0132, -0x0043, -0x0267,  0x0C9D,  0x74BB,
                                  -0x11B4,  0x09B8, -0x05BF,  0x0372, -0x01A8,  0x00A6, -0x001B,  0x0005,
                                   0x0006, -0x0008,  0x0003, -0x0001,      0},

                     new short[]{       0,       0, -0x0001,  0x0003, -0x0002, -0x0005,  0x001F, -0x004A,   //Table 2
                                   0x00B3, -0x0192,  0x02B1, -0x039E,  0x04F8, -0x05A6,  0x7939, -0x05A6,
                                   0x04F8, -0x039E,  0x02B1, -0x0192,  0x00B3, -0x004A,  0x001F, -0x0005,
                                  -0x0002,  0x0003, -0x0001,       0,      0},

                     new short[]{       0, -0x0001,  0x0003, -0x0008,  0x0006,  0x0005, -0x001B,  0x00A6,   //Table 3
                                  -0x01A8,  0x0372, -0x05BF,  0x09B8, -0x11B4,  0x74BB,  0x0C9D, -0x0267,
                                  -0x0043,  0x0132, -0x00E3,  0x00A2, -0x004B,  0x003C, -0x0013,  0x0003,
                                        0, -0x0002,       0,       0,      0},

                     new short[]{ -0x0001,  0x0003, -0x0008,  0x0011, -0x0010,  0x000A,  0x006B, -0x016D,   //Table 4
                                   0x0350, -0x0623,  0x0BCD, -0x1780,  0x6794,  0x234C, -0x0A78,  0x0400,
                                  -0x010A,  0x0009,  0x0034, -0x0054,  0x0041, -0x0022,  0x000A, -0x0001,
                                        0,  0x0001,       0,       0,      0},

                     new short[]{  0x0002, -0x0008,  0x0010, -0x0023,  0x002B,  0x001A, -0x00EB,  0x027B,   //Table 5
                                  -0x0548,  0x0AFA, -0x16FA,  0x53E0,  0x3C07, -0x1249,  0x080E, -0x0347,
                                   0x015B, -0x0044, -0x0017,  0x0046, -0x0023,  0x0011, -0x0005,       0,
                                        0,       0,       0,       0,      0},

                     new short[]{ -0x0005,  0x0011, -0x0023,  0x0046, -0x0017, -0x0044,  0x015B, -0x0347,   //Table 6
                                   0x080E, -0x1249,  0x3C07,  0x53E0, -0x16FA,  0x0AFA, -0x0548,  0x027B,
                                  -0x00EB,  0x001A,  0x002B, -0x0023,  0x0010, -0x0008,  0x0002,       0,
                                        0,       0,       0,       0,      0}
        };
    }
}
