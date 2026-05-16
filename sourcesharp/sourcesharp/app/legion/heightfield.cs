using materialsystem;
using tier2;
using bitmap;
using tier1;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;
using System;

namespace sourcesharp.app.legion
{
    class heightfield
    {

            private int m_nPowX;
            private int m_nPowY;
            private int m_nWidth;
            private int m_nHeight;
            private int m_nScale;
            private int m_nPowScale;
            private float m_flOOScale;
            private List<float> m_pHeightField;
            private Material m_Material;
            private Texture2D m_Texture;

            public heightfield(int nPowX, int nPowY, int nPowScale)
            {
                m_nPowX = nPowX;
                m_nPowY = nPowY;
                m_nWidth = (1 << nPowX) + 1;
                m_nHeight = (1 << nPowY) + 1;
                m_nScale = (1 << nPowScale);
                m_nPowScale = nPowScale;
                m_flOOScale = 1.0f / m_nScale;
                m_pHeightField = new List<float>(m_nWidth * m_nHeight);

                // Initialize the height field to zero
                for (int i = 0; i < m_nWidth * m_nHeight; ++i)
                {
                    m_pHeightField.Add(0.0f);
                }

                m_Material = new Material(Shader.Find("Diffuse"));
                m_Texture = new Texture2D(m_nWidth, m_nHeight, TextureFormat.RGBA32, false);
            }

            // Loads the heights from a file
            public bool LoadHeightFromFile(string fileName)
            {
                // Load heights from file and store in m_pHeightField
            }

            // Returns the max range of x, y
            public int GetWidth() { return m_nWidth; }
            public int GetHeight() { return m_nHeight; }

            // Returns the height of the field at a particular (x,y)
            public float GetHeight(float x, float y)
            {
                int i = Mathf.Clamp((int)(x * m_flOOScale), 0, m_nWidth - 1);
                int j = Mathf.Clamp((int)(y * m_flOOScale), 0, m_nHeight - 1);
                return m_pHeightField[j * m_nWidth + i];
            }
        public float GetHeightAndSlope(float x, float y, float dx, float dy) { }
        public void Draw() { }
        public int GetWidth()
        {
            return m_nWidth << m_nPowScale;
        }

        public int GetHeight()
        {
            return m_nHeight << m_nPowScale;
        }



        public int HEIGHT(int x, int y)
        {
            return m_pHeightField[(y << m_nPowX) + x];
        }
        public int[] ROW(int y)
        {
            return m_pHeightField.AsSpan(y << m_nPowX, 1 << m_nPowX).ToArray();
        }
        public class CHeightField
        {
            private int m_nPowX;
            private int m_nPowY;
            private int m_nPowScale;
            private int m_nWidth;
            private int m_nHeight;
            private int m_nScale;
            private float m_flOOScale;
            private float[] m_pHeightField;
            private KeyValues pKeyValues;
            private IMaterial m_Material;

            public CHeightField(int nPowX, int nPowY, int nPowScale)
            {
                m_nPowX = nPowX;
                m_nPowY = nPowY;
                m_nPowScale = nPowScale;
                m_nWidth = (1 << nPowX);
                m_nHeight = (1 << nPowY);
                m_nScale = (1 << nPowScale);
                m_flOOScale = 1.0f / m_nScale;
                m_pHeightField = new float[m_nWidth * m_nHeight];
                Array.Clear(m_pHeightField, 0, m_pHeightField.Length);

                pKeyValues = new KeyValues("Wireframe");
                pKeyValues.SetInt("$nocull", 1);
                m_Material.Init("__Temp", pKeyValues);
            }

            ~CHeightField()
            {
                // free unmanaged resources, if any
                if (m_pHeightField != null)
                {
                    Marshal.FreeHGlobal(Marshal.AllocHGlobal(m_pHeightField.Length * sizeof(float)));
                    m_pHeightField = null;
                }

                // release managed resources
                if (pKeyValues != null)
                {
                    pKeyValues.Dispose();
                    pKeyValues = null;
                }

                if (m_Material.IsValid())
                {
                    m_Material.Shutdown();
                    m_Material = null;
                }

            }
            public float BilerpBitmap(Bitmap bitmap, float x, float y)
            {
                Debug.Assert(bitmap.PixelFormat == PixelFormat.Format32bppArgb);

                float w = (float)bitmap.Width;
                float h = (float)bitmap.Height;

                // Clamp to a valid range
                x = Math.Clamp(x, 0, w - 1.0f);
                y = Math.Clamp(y, 0, h - 1.0f);

                // pick bilerp coordinates
                int i0 = (int)Math.Floor(x);
                int i1 = i0 + 1;
                int j0 = (int)Math.Floor(y);
                int j1 = j0 + 1;
                if (i1 >= bitmap.Width)
                {
                    i1 = bitmap.Width - 1;
                }
                if (j1 >= bitmap.Height)
                {
                    j1 = bitmap.Height - 1;
                }

                // extract pixel values
                Color pixel00 = bitmap.GetPixel(i0, j0);
                Color pixel10 = bitmap.GetPixel(i1, j0);
                Color pixel01 = bitmap.GetPixel(i0, j1);
                Color pixel11 = bitmap.GetPixel(i1, j1);

                float v00 = pixel00.R / 255.0f;
                float v10 = pixel10.R / 255.0f;
                float v01 = pixel01.R / 255.0f;
                float v11 = pixel11.R / 255.0f;

                // calculate interpolated value
                float fx = x - i0;
                float fy = y - j0;

                float result = (1 - fx) * (1 - fy) * v00 +
                               fx * (1 - fy) * v10 +
                               (1 - fx) * fy * v01 +
                               fx * fy * v11;

                return result;
            }
            public bool LoadHeightFromFile(string pFileName)
            {
                Bitmap_t bitmap;
                using (FileStream fileStream = new FileStream(pFileName, FileMode.Open, FileAccess.Read))
                {
                    using (CUtlStreamBuffer buf = new CUtlStreamBuffer(fileStream, "GAME"))
                    {
                        if (IsPSDFile(buf))
                        {
                            if (!PSDReadFileRGBA8888(buf, bitmap))
                                return false;
                        }
                    }
                }

                // map from height field into map, ensuring corner pixel centers line up
                // hfx -> mapx:  0 -> 0.5, hfw-1 -> mapw-0.5
                // x (mapw - 1)/(hfw - 1) + 0.5
                // mapx -> worldx: 0 -> 0, mapw -> worldw
                float fx = (float)(bitmap.m_nWidth - 1) / (float)(m_nWidth - 1);
                float fy = (float)(bitmap.m_nHeight - 1) / (float)(m_nHeight - 1);

                for (int i = 0; i < m_nHeight; ++i)
                {
                    float[] pRow = ROW(i);
                    for (int j = 0; j < m_nWidth; ++j, ++pRow)
                    {
                        *pRow = 50.0f * BilerpBitmap(bitmap, i * fx, j * fy);
                    }
                }
                return true;
            }
            public float GetHeightAndSlope(float x, float y, out float dx, out float dy)
            {
                x *= m_flOOScale;
                y *= m_flOOScale;

                int gx = (int)Math.Floor(x);
                int gy = (int)Math.Floor(y);
                x -= gx;
                y -= gy;

                if (gx < -1 || gy < -1 || gx >= m_nWidth || gy >= m_nHeight)
                {
                    dx = 0;
                    dy = 0;
                    return 0.0f;
                }

                float h00 = (gx >= 0 && gy >= 0) ? HEIGHT(gx, gy) : 0.0f;
                float h01 = (gx < (m_nWidth - 1) && gy >= 0) ? HEIGHT(gx + 1, gy) : 0.0f;
                float h10 = (gx >= 0 && gy < (m_nHeight - 1)) ? HEIGHT(gx, gy + 1) : 0.0f;
                float h11 = (gx < (m_nWidth - 1) && gy < (m_nHeight - 1)) ? HEIGHT(gx + 1, gy + 1) : 0.0f;

                if (x > y)
                {
                    h10 = h00 + h11 - h01;
                }
                else
                {
                    h01 = h00 + h11 - h10;
                }

                dx = (h01 - h00) * m_flOOScale;
                dy = (h10 - h00) * m_flOOScale;

                // Bilinear filter
                float h0 = h00 + (h01 - h00) * x;
                float h1 = h10 + (h11 - h10) * x;
                float h = h0 + (h1 - h0) * y;

                return h;
            }
            public void Draw()
            {
                int nVertexCount = m_nWidth * m_nHeight;
                int nIndexCount = 6 * (m_nWidth - 1) * (m_nHeight - 1);

                float flOOTexWidth = 1.0f / m_Material.GetMappingWidth();
                float flOOTexHeight = 1.0f / m_Material.GetMappingHeight();
                float iu = 0.5f * flOOTexWidth;
                float iv = 1.0f - (0.5f * flOOTexHeight);
                float du = (1.0f - flOOTexWidth) / (m_nWidth - 1);
                float dv = -(1.0f - flOOTexHeight) / (m_nHeight - 1);

                var pRenderContext = Engine.GetContext<RenderContext>();
                pRenderContext.Bind(m_Material);
                var pMesh = pRenderContext.GetDynamicMesh();

                var meshBuilder = new MeshBuilder();
                meshBuilder.Begin(pMesh, MaterialPrimitiveType.TRIANGLES, nVertexCount, nIndexCount);

                // Deal with vertices
                float v = iv;
                float y = 0.0f;
                for (int i = 0; i < m_nHeight; ++i, y += m_nScale, v += dv)
                {
                    float u = iu;
                    float x = 0.0f;
                    for (int j = 0; j < m_nWidth; ++j, x += m_nScale, u += du)
                    {
                        meshBuilder.Position3(new Vector3(x, y, HEIGHT(j, i)));
                        meshBuilder.TexCoord2(0, new Vector2(u, v));
                        meshBuilder.AdvanceVertex();
                    }
                }

                // Deal with indices
                for (int i = 0; i < (m_nHeight - 1); ++i)
                {
                    int nRow0 = m_nWidth * i;
                    int nRow1 = nRow0 + m_nWidth;
                    for (int j = 0; j < (m_nWidth - 1); ++j)
                    {
                        meshBuilder.FastIndex(nRow0 + j);
                        meshBuilder.FastIndex(nRow0 + j + 1);
                        meshBuilder.FastIndex(nRow1 + j + 1);

                        meshBuilder.FastIndex(nRow0 + j);
                        meshBuilder.FastIndex(nRow1 + j + 1);
                        meshBuilder.FastIndex(nRow1 + j);


                    }
                }
                meshBuilder.End();
                pMesh.Draw();
            }
        }
    }
}
