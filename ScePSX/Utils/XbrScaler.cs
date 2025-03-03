using System.Numerics;

namespace ScePSX
{

    class XbrScaler
    {
        private const float Threshold = 15.0f;

        public static int[] ScaleXBR(int[] pixels, int width, int height, int scaleFactor)
        {
            if ((scaleFactor & (scaleFactor - 1)) != 0)
                return null;

            int currentWidth = width;
            int currentHeight = height;
            int[] currentPixels = (int[])pixels.Clone();

            while (scaleFactor > 1)
            {
                currentPixels = Scale2xBR(currentPixels, currentWidth, currentHeight);
                currentWidth *= 2;
                currentHeight *= 2;
                scaleFactor /= 2;
            }

            return currentPixels;
        }

        private static int[] Scale2xBR(int[] pixels, int width, int height)
        {
            int outputWidth = width * 2;
            int outputHeight = height * 2;
            int[] scaledPixels = new int[outputWidth * outputHeight];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int p = GetPixel(pixels, x, y, width, height);
                    int a = GetPixel(pixels, x - 1, y - 1, width, height);
                    int b = GetPixel(pixels, x, y - 1, width, height);
                    int c = GetPixel(pixels, x + 1, y - 1, width, height);
                    int d = GetPixel(pixels, x - 1, y, width, height);
                    int e = p;
                    int f = GetPixel(pixels, x + 1, y, width, height);
                    int g = GetPixel(pixels, x - 1, y + 1, width, height);
                    int h = GetPixel(pixels, x, y + 1, width, height);
                    int i = GetPixel(pixels, x + 1, y + 1, width, height);

                    Vector3 vecE = GetVector(e);
                    int e0 = e, e1 = e, e2 = e, e3 = e;

                    if (IsSimilar(vecE, a) && !IsSimilar(vecE, i))
                    {
                        e0 = IsSimilar(vecE, b) ? AverageFast(e, b) : e;
                        e1 = IsSimilar(vecE, d) ? AverageFast(e, d) : e;
                    } else if (IsSimilar(vecE, i) && !IsSimilar(vecE, a))
                    {
                        e2 = IsSimilar(vecE, f) ? AverageFast(e, f) : e;
                        e3 = IsSimilar(vecE, h) ? AverageFast(e, h) : e;
                    }

                    SetPixel(scaledPixels, x * 2, y * 2, e0, outputWidth);
                    SetPixel(scaledPixels, x * 2 + 1, y * 2, e1, outputWidth);
                    SetPixel(scaledPixels, x * 2, y * 2 + 1, e2, outputWidth);
                    SetPixel(scaledPixels, x * 2 + 1, y * 2 + 1, e3, outputWidth);
                }
            }

            return scaledPixels;
        }

        private static Vector3 GetVector(int color)
        {
            return new Vector3(
                (color >> 16) & 0xFF,
                (color >> 8) & 0xFF,
                color & 0xFF
            );
        }

        private static bool IsSimilar(Vector3 v1, int color)
        {
            Vector3 v2 = GetVector(color);
            return Vector3.Distance(v1, v2) < Threshold;
        }

        private static int AverageFast(int c1, int c2)
        {
            return ((c1 & 0xFEFEFE) + (c2 & 0xFEFEFE)) >> 1;
        }

        private static int GetPixel(int[] pixels, int x, int y, int width, int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
                return 0;
            return pixels[y * width + x];
        }

        private static void SetPixel(int[] pixels, int x, int y, int color, int width)
        {
            pixels[y * width + x] = color;
        }

        public static unsafe int CutBlackLine(int[] In, int[] Out, int width, int height)
        {
            int top = -1, bottom = -1;

            // 查找顶部非黑边起始行
            for (int y = 0; y < height; y++)
            {
                bool isBlack = true;
                for (int x = 0; x < width; x++)
                {
                    if (In[y * width + x] != unchecked((int)0x0))
                    {
                        isBlack = false;
                        break;
                    }
                }
                if (!isBlack)
                {
                    top = y;
                    break;
                }
            }

            // 查找底部非黑边结束行
            for (int y = height - 1; y >= 0; y--)
            {
                bool isBlack = true;
                for (int x = 0; x < width; x++)
                {
                    if (In[y * width + x] != unchecked((int)0x0))
                    {
                        isBlack = false;
                        break;
                    }
                }
                if (!isBlack)
                {
                    bottom = y;
                    break;
                }
            }

            if (top == -1 || bottom == -1 || top > bottom)
            {
                return 0;
            }

            if (top > 20 || (height - bottom) > 20)
            {
                return 0;
            }

            int newHeight = bottom - top + 1;

            fixed (int* srcPtr = In, dstPtr = Out)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    int srcIndex = (top + y) * width;
                    int dstIndex = y * width;
                    System.Buffer.MemoryCopy(
                        srcPtr + srcIndex,
                        dstPtr + dstIndex,
                        width * sizeof(int),
                        width * sizeof(int)
                    );
                }
            }

            return newHeight;
        }
    }

}
