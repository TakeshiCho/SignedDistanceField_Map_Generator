using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
            None,
        }

        private struct Pixel
        {
            public Type type;
            public int dx;
            public int dy;
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
        
        static Pixel _nonePixel = new Pixel(Type.None, 0, 0, MaxDistance);
        
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
                    return ref _nonePixel;
                else
                    return ref pixels[x, y];
            }
        }
        
        public Texture2D CreateSDFTex(Texture2D rawTex)
        {
            int width = rawTex.width;
            int height = rawTex.height;
            
            TexData data = new TexData(width, height);

            MarkRawData(ref data, rawTex, width, height);
            GenerateSDF(ref data, width, height);
            Texture2D newTex = new Texture2D(width, height, GraphicsFormat.R8_UNorm,0);
            WriteTex(newTex, ref data, width, height);
            return newTex;
        }
        
        void MarkRawData(ref TexData data, Texture2D tex, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int value = (int)tex.GetPixel(x, y).r;
                    Pixel e = new Pixel(Type.Empty, 0, 0, MaxDistance);
                    Pixel o = new Pixel(Type.Object, 0, 0, MaxDistance);
                    data.pixels[x, y] = value == 1 ? o : e;
                }
            }
        }

        void ComparePixel(ref Pixel pixel, ref TexData data, int x, int y, int offsetX, int offsetY)
        {
            ref Pixel comparedPixel = ref data.GetPixel(x + offsetX, y + offsetY);
            
            if (comparedPixel.type == Type.None || (comparedPixel.type == pixel.type  && comparedPixel.sqrDistance == MaxDistance))
                return;

            int dx, dy;
            if (comparedPixel.type == pixel.type)
            {
                dx = comparedPixel.dx;
                dy = comparedPixel.dy;
            }
            else
            {
                dx = dy = 0;
            }
            
            Pixel tmp = new Pixel(pixel.type,dx + Abs(offsetX),dy + Abs(offsetY));

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
        
        void GenerateSDF(ref TexData data, int width, int height)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ComparePixels(ref data, x, y, -1, 0, -1, -1, 0, -1);
                }

                for (int x = width-1; x >= 0 ; x--)
                {
                    ComparePixels(ref data, x, y, 1, -1, 1, 0);
                }
            }
            
            for (int y = height-1; y >= 0 ; y--)
            {
                for (int x = width-1; x >= 0 ; x--)
                {
                    ComparePixels(ref data, x, y, 1, 0, 1, 1, 0, 1);
                }

                for (int x = 0; x < width; x++)
                {
                    ComparePixels(ref data, x, y, -1, 1, -1, 0);
                }
            }
        }

        void WriteTex(Texture2D texture, ref TexData data, int width, int height)
        {
            NativeArray<byte> col = new NativeArray<byte>(width * height,Allocator.TempJob);
            float scale = height / 256f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ref Pixel p = ref data.pixels[x, y];
                    float value = Sqrt(p.sqrDistance) / scale;
                    byte v = (byte)(Lerp(value, - value, (int)p.type) + 128);;
                    col[y * height + x] = v;
                }
            }
            texture.SetPixelData(col,0);
            col.Dispose();
        }
        
    }

}