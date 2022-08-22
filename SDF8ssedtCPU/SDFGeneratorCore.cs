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
            
            public Pixel GetPixel(int x, int y)
            {
                if (x < 0 || x > sizeX || y < 0 || y > sizeY)
                    return new Pixel(Type.Empty, 0, 0, 2147483647);
                else
                    return pixels[x, y];
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
            WriteTex(newTex, in whiteSideData, in blackSideData, width, height);
            return newTex;
        }
        
        void MarkRawData(ref TexData whiteSideData, ref TexData blackSideData, Texture2D tex, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int value = (int)tex.GetPixel(x, y).r;
                    if (value == 1)
                    {
                        whiteSideData.pixels[x, y] = new Pixel(Type.Object, 0, 0, 0);
                        blackSideData.pixels[x, y] = new Pixel(Type.Empty, 0, 0, MaxDistance);
                    }
                    else
                    {
                        whiteSideData.pixels[x, y] = new Pixel(Type.Empty, 0, 0, MaxDistance);
                        blackSideData.pixels[x, y] = new Pixel(Type.Object, 0, 0, 0);
                    }
                }
            }
        }

        void ComparePixel(ref TexData data, int x, int y, int offsetX, int offsetY)
        {
            Pixel other = data.GetPixel(x + offsetX, y + offsetY);
            if (other.sqrDistance == MaxDistance || other.sqrDistance >= data.pixels[x, y].sqrDistance)
                return;

            Pixel tmp = new Pixel(Type.Empty,other.dx + Abs(offsetX),other.dy + Abs(offsetY));
            if (data.pixels[x, y].sqrDistance > tmp.sqrDistance)
            {
                data.pixels[x, y] = tmp;
            }
        }

        void ComparePixels(ref TexData data, int x, int y, int ox0, int oy0, int ox1, int oy1)
        {
            ComparePixel(ref data, x, y, ox0, oy0);
            ComparePixel(ref data, x, y, ox1, oy1);
        }
        
        void ComparePixels(ref TexData data, int x, int y, int ox0, int oy0, int ox1, int oy1, int ox2, int oy2)
        {
            ComparePixel(ref data, x, y, ox0, oy0);
            ComparePixel(ref data, x, y, ox1, oy1);
            ComparePixel(ref data, x, y, ox2, oy2);
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

        void WriteTex(Texture2D texture, in TexData whiteSideDate, in TexData blackSideData, int width, int height)
        {
            float scale = height / 256f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float value1 = Sqrt(whiteSideDate.pixels[x, y].sqrDistance) / scale;
                    float value2 = Sqrt(blackSideData.pixels[x, y].sqrDistance) / scale;
                    float v = (value1 - value2 + 128f) / 256f;
                    Color color = new Color(v, v, v);
                    texture.SetPixel(x,y,color);
                }
            }
        }
        
    }

}