using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ScePSX
{

    unsafe class XbrScaler
    {

        // 定义阈值，用于判断颜色差异是否显著
        private const int Threshold = 15;

        /// <summary>
        /// 实现任意倍率的 xBR 缩放
        /// </summary>
        /// <param name="pixels">输入像素数组（ARGB 格式）</param>
        /// <param name="width">输入图像宽度</param>
        /// <param name="height">输入图像高度</param>
        /// <param name="scaleFactor">缩放倍率（必须是 2 的幂次，如 2、4、6、8）</param>
        /// <returns>放大后的像素数组</returns>
        public static int[] ScaleXBR(int[] pixels, int width, int height, int scaleFactor)
        {
            // 检查缩放倍率是否为 2 的幂次
            if ((scaleFactor & (scaleFactor - 1)) != 0)
            {
                return null;
            }

            int currentWidth = width;
            int currentHeight = height;
            int[] currentPixels = (int[])pixels.Clone();

            // 递归应用 2xBR 直到达到目标倍率
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
                    // 获取当前像素及其邻域
                    int p = GetPixel(pixels, x, y, width, height);
                    int a = GetPixel(pixels, x - 1, y - 1, width, height);
                    int b = GetPixel(pixels, x, y - 1, width, height);
                    int c = GetPixel(pixels, x + 1, y - 1, width, height);
                    int d = GetPixel(pixels, x - 1, y, width, height);
                    int e = p; // 当前像素
                    int f = GetPixel(pixels, x + 1, y, width, height);
                    int g = GetPixel(pixels, x - 1, y + 1, width, height);
                    int h = GetPixel(pixels, x, y + 1, width, height);
                    int i = GetPixel(pixels, x + 1, y + 1, width, height);

                    // 计算输出像素
                    int e0 = e, e1 = e, e2 = e, e3 = e;

                    if (IsSimilar(d, b) && !IsSimilar(h, f))
                    {
                        e0 = IsSimilar(d, b) && IsSimilar(e, b) ? Average(e, b) : e;
                        e1 = IsSimilar(d, b) && IsSimilar(e, d) ? Average(e, d) : e;
                    } else if (IsSimilar(h, f) && !IsSimilar(d, b))
                    {
                        e2 = IsSimilar(h, f) && IsSimilar(e, f) ? Average(e, f) : e;
                        e3 = IsSimilar(h, f) && IsSimilar(e, h) ? Average(e, h) : e;
                    }

                    // 将结果写入输出数组
                    SetPixel(scaledPixels, x * 2, y * 2, e0, outputWidth);
                    SetPixel(scaledPixels, x * 2 + 1, y * 2, e1, outputWidth);
                    SetPixel(scaledPixels, x * 2, y * 2 + 1, e2, outputWidth);
                    SetPixel(scaledPixels, x * 2 + 1, y * 2 + 1, e3, outputWidth);
                }
            }

            return scaledPixels;
        }

        // 判断两个像素是否相似
        private static bool IsSimilar(int p1, int p2)
        {
            int r1 = (p1 >> 16) & 0xFF; // 提取红色通道
            int g1 = (p1 >> 8) & 0xFF;  // 提取绿色通道
            int b1 = p1 & 0xFF;         // 提取蓝色通道

            int r2 = (p2 >> 16) & 0xFF;
            int g2 = (p2 >> 8) & 0xFF;
            int b2 = p2 & 0xFF;

            return Math.Abs(r1 - r2) + Math.Abs(g1 - g2) + Math.Abs(b1 - b2) < Threshold;
        }

        // 获取像素（处理边界情况）
        private static int GetPixel(int[] pixels, int x, int y, int width, int height)
        {
            if (x < 0 || x >= width || y < 0 || y >= height)
            {
                return 0; // 边界外返回透明黑色（ARGB: 0x00000000）
            }

            return pixels[y * width + x];
        }

        // 设置像素
        private static void SetPixel(int[] pixels, int x, int y, int color, int width)
        {
            pixels[y * width + x] = color;
        }

        // 计算两个颜色的平均值
        private static int Average(int c1, int c2)
        {
            int r = (((c1 >> 16) & 0xFF) + ((c2 >> 16) & 0xFF)) / 2;
            int g = (((c1 >> 8) & 0xFF) + ((c2 >> 8) & 0xFF)) / 2;
            int b = ((c1 & 0xFF) + (c2 & 0xFF)) / 2;
            return (r << 16) | (g << 8) | b;
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
