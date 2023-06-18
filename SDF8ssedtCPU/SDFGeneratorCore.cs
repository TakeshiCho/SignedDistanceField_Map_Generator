using UnityEngine;
using static UnityEngine.Mathf;

namespace SDF8ssedtCPU
{
    public class SDFGeneratorCore
    {
        private const int MaxDistance = 2147483647;
        private enum Type
        {
            Object,
            Empty,
        }

        private struct Pixel
        {
            public Type type;
            public int dx, dy;
            public int sqrDistance;

            public Pixel(Type type, int dx, int dy)
            {
                this.type = type;
                this.dx = dx;
                this.dy = dy;
                sqrDistance = dx * dx + dy * dy;
            }
            public Pixel(Type type, int dx, int dy, int sqrDistance)
            {
                this.type = type;
                this.dx = dx;
                this.dy = dy;
                this.sqrDistance = sqrDistance;
            }
        }

        static Pixel _emptyPixel = new Pixel(Type.Empty, 0, 0, MaxDistance);
        
        private struct TexData
        {
            private int sizeX,sizeY;
            public Pixel[,] pixels;
            public TexData(int x, int y)
            {
                pixels = new Pixel[x,y];
                sizeX = x - 1;
                sizeY = y - 1;
            }
            
            public ref Pixel GetPixel(int x, int y)
            {
                if (x < 0 || x > sizeX || y < 0 || y > sizeY)
                    return ref _emptyPixel;
                else
                    return ref pixels[x, y];
            }
        }
        
        public Texture2D CreateSDFTex(Texture2D rawTex)
        {
            int width = rawTex.width;
            int height = rawTex.height;
            
            TexData whiteSideData = new TexData(width, height);
            TexData blackSideData = new TexData(width, height);
            
            MarkRawData(ref whiteSideData, ref blackSideData, rawTex, width, height);
            GenerateSDF(ref whiteSideData, ref blackSideData, width, height);

            Texture2D newTex = new Texture2D(width, height);
            WriteTex(newTex, ref whiteSideData, ref blackSideData, width, height);
            return newTex;
        }
        
        void MarkRawData(ref TexData whiteSideData, ref TexData blackSideData, Texture2D tex, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int value = (int)tex.GetPixel(x, y).r;
                    Pixel e = new Pixel(Type.Empty, 0, 0, MaxDistance);
                    
                    if (value == 1)
                        blackSideData.pixels[x, y] = e;
                    else
                        whiteSideData.pixels[x, y] = e;
                }
            }
        }

        void ComparePixel(ref Pixel pixel, ref TexData data, int x, int y, int offsetX, int offsetY)
        {
            ref Pixel comparedPixel = ref data.GetPixel(x + offsetX, y + offsetY);
            if (comparedPixel.sqrDistance == MaxDistance || comparedPixel.sqrDistance >= pixel.sqrDistance)
                return;

            Pixel tmp = new Pixel(Type.Empty,comparedPixel.dx + Abs(offsetX),comparedPixel.dy + Abs(offsetY));
            if (pixel.sqrDistance > tmp.sqrDistance)
            {
                pixel = tmp;
            }
        }

        void ComparePixels(ref TexData data, int x, int y, int ox0, int oy0, int ox1, int oy1)
        {
            ref Pixel pixel = ref data.pixels[x, y];
            ComparePixel(ref pixel, ref data, x, y, ox0, oy0);
            ComparePixel(ref pixel, ref data, x, y, ox1, oy1);
        }
        
        void ComparePixels(ref TexData data, int x, int y, int ox0, int oy0, int ox1, int oy1, int ox2, int oy2)
        {
            ref Pixel pixel = ref data.pixels[x, y];
            ComparePixel(ref pixel, ref data, x, y, ox0, oy0);
            ComparePixel(ref pixel, ref data, x, y, ox1, oy1);
            ComparePixel(ref pixel, ref data, x, y, ox2, oy2);
        }
        
        void GenerateSDF(ref TexData whiteSideData, ref TexData blackSideData, int width, int height)
        {
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (whiteSideData.pixels[x, y].type != Type.Object)
                        ComparePixels(ref whiteSideData, x, y, -1, 0, -1, -1, 0, -1);
                    else
                        ComparePixels(ref blackSideData, x, y, -1, 0, -1, -1, 0, -1);
                }

                for (int x = width-1; x >= 0 ; x--)
                {
                    if (whiteSideData.pixels[x, y].type != Type.Object)
                        ComparePixels(ref whiteSideData, x, y, 1, -1, 1, 0);
                    else
                        ComparePixels(ref blackSideData, x, y, 1, -1, 1, 0);
                }
            }
            
            for (int y = height-1; y >= 0 ; y--)
            {
                for (int x = width-1; x >= 0 ; x--)
                {
                    if (whiteSideData.pixels[x, y].type != Type.Object)
                        ComparePixels(ref whiteSideData, x, y, 1, 0, 1, 1, 0, 1);
                    else
                        ComparePixels(ref blackSideData, x, y, 1, 0, 1, 1, 0, 1);
                }

                for (int x = 0; x < width; x++)
                {
                    if (whiteSideData.pixels[x, y].type != Type.Object)
                        ComparePixels(ref whiteSideData, x, y, -1, 1, -1, 0);
                    else
                        ComparePixels(ref blackSideData, x, y, -1, 1, -1, 0);
                }
            }
        }

        void WriteTex(Texture2D texture, ref TexData whiteSideData, ref TexData blackSideData, int width, int height)
        {
            Color[] colors = new Color[height * width];
            float scale = height / 256f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value1 = Sqrt(whiteSideData.pixels[x, y].sqrDistance) / scale;
                    float value2 = Sqrt(blackSideData.pixels[x, y].sqrDistance) / scale;
                    float v = (value1 - value2 + 128f) / 256f;
                    Color color = new Color(v, v, v);
                    colors[y * height + x] = color;
                }
            }
            texture.SetPixels(colors);
        }
        
    }

}