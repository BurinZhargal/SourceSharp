using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using J2N.IO;
using System.Drawing.Imaging;
using J2N.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using System.Runtime.InteropServices;

namespace SharpWolf3D.asset.loader
{
    public class VGAGRAPHLoader
    {
        public class VGAGRAPHFont
        {
            private readonly byte[] fontData;
            private int fontHeight;
            private int[] fontLocation;
            private int[] fontWidth;
            private readonly Bitmap[] charImages = new Bitmap[256];

            public VGAGRAPHFont(byte[] fontData, Color color)
            {
                this.fontData = fontData;
                extractFontInformation();
                generateCharImages(color);
            }
            private void extractFontInformation()
            {
                ByteBuffer bb = ByteBuffer.Wrap(fontData);
                bb.SetOrder(ByteOrder.LittleEndian);
                fontHeight = bb.GetInt16() & 0xffff;
                fontLocation = new int[256];
                fontWidth = new int[256];
                for (int i = 0; i < fontLocation.Length; i++)
                {
                    fontLocation[i] = bb.GetInt16() & 0xffff;
                }
                for (int i = 0; i < fontWidth.Length; i++)
                {
                    fontWidth[i] = bb.Get() & 0xff;
                }
            }
            private void generateCharImages(Color color)
            {
                for (int c = 0; c < 256; c++)
                {
                    int ch = fontHeight;
                    int cw = fontWidth[c];
                    int cl = fontLocation[c];

                    Bitmap charImage = null;
                    if (ch > 0 && cw > 0)
                    {
                        charImage = new Bitmap(cw, ch);
                    }
                    if (charImage == null) continue;
                    charImages[c] = charImage;
                    int index = 0;
                    for (int y = 0; y < ch; y++)
                    {
                        for (int x = 0; x < cw; x++)
                        {
                            int rgb = fontData[cl + index++] & 0xff;
                            if (rgb > 0)
                            {

                                charImage.SetPixel(x, y, Color.FromArgb(255, 0, 0));
                            }
                        }
                    }
                }
            }
            public void drawString(Graphics g, string text, int x, int y)
            {
                int dx = 0;
                for (int i = 0; i < text.Length; i++)
                {
                    int c = text[i];
                    Bitmap charImage = charImages[c];
                    g.DrawImage(charImage, x + dx, y);
                    dx += fontWidth[c];
                }

            }
        }
        private static readonly sbyte[] huffmanNodes = new sbyte[256 * 4];
        private static FrameDimension[] pictable;
        private static readonly IDictionary<int, Bitmap> PICS = new Dictionary<int, Bitmap>();
        private static readonly IDictionary<string, Bitmap> FONTS = new Dictionary<string, Bitmap>();

        public static void load(string path, string vgaHeadRes, string vgaDictRes, string vgaGraphRes)
        {

            using (var vgaHeadIs = new FileStream(Path.Combine(path, vgaHeadRes), FileMode.Open))
            using (var vgaDictIs = new FileStream(Path.Combine(path, vgaDictRes), FileMode.Open))
            using (var vgaGraphIs = new FileStream(Path.Combine(path, vgaGraphRes), FileMode.Open))
            {//?
                var vgaHeadData = ByteBuffer.Wrap((byte[])Convert.ToByte(vgaHeadIs.ReadByte()));
                vgaHeadData.SetOrder(ByteOrder.LittleEndian);

                // size of VGAHEAD file must be multiple of 3
                if (vgaHeadData.Limit % 3 != 0)
                {
                    throw new Exception("VGAHEAD file is corrupted!");
                }

                // extract the offsets information from the header
                var picsCount = vgaHeadData.Limit / 3;
                var offsets = new int[picsCount];
                for (var i = 0; i < offsets.Length; i++)
                {
                    var o0 = vgaHeadData.Get() & 0xff;
                    var o1 = vgaHeadData.Get() & 0xff;
                    var o2 = vgaHeadData.Get() & 0xff;
                    offsets[i] = o0 + (o1 << 8) + (o2 << 16);
                }
                ByteBuffer vgaDictData = ByteBuffer.Wrap(vgaDictIs.Read());
                vgaDictData.SetOrder(ByteOrder.LittleEndian);

                // extract huffman dictionary
                vgaDictData.Get(huffmanNodes);

                ByteBuffer vgaGraphData
                        = ByteBuffer.Wrap(vgaGraphIs.Read);

                vgaGraphData.SetOrder(ByteOrder.LittleEndian);
                for (var i = 0; i < offsets.Length - 1; i++)
                {
                    var length = offsets[i + 1] - offsets[i];
                    var compressed = vgaGraphData.Slice();
                    compressed.SetOrder(ByteOrder.LittleEndian);
                    var decompressedData = DecompressHuffman(compressed);
                    if (decompressedData == null) continue;
                    // extract pictable
                    if (i == 0)
                    {
                        ExtractPictable(decompressedData, picsCount);
                    }

                    if (i == 1)
                    {
                        // 8x8 small font
                        var fontWhite = new VGAGRAPHFont(decompressedData, Color.White);
                        var fontBlack = new VGAGRAPHFont(decompressedData, Color.Black);
                        var fontYellow = new VGAGRAPHFont(decompressedData, Color.Yellow);
                        //?
                        FONTS.Add("SMALL_WHITE", (Bitmap)fontWhite);
                        FONTS.Add("SMALL_BLACK", (Bitmap)fontBlack);
                        FONTS.Add("SMALL_YELLOW", (Bitmap)fontYellow);
                        if (i == 2)
                        {
                            // game options font
                            var fontGray = new VGAGRAPHFont(decompressedData, Color.Gray);
                            var fontLightGray = new VGAGRAPHFont(decompressedData, Color.LightGray);
                            var fontDarkRed = new VGAGRAPHFont(decompressedData, Utils.GetColor("0x710000ff"));
                            var fontYellow = new VGAGRAPHFont(decompressedData, Color.Yellow);

                            FONTS.Add("BIG_GRAY", fontGray);
                            FONTS.Add("BIG_LIGHT_GRAY", fontLightGray);
                            FONTS.Add("BIG_DARK_RED", fontDarkRed);
                            FONTS.Add("BIG_YELLOW", fontYellow);
                        }
                        else if (i > 2)
                        {
                            Dimension picDimension = pictable[i];
                            if (picDimension != null)
                            {
                                int picWidth = picDimension.width;
                                int picHeight = picDimension.height;
                                if (picWidth <= 0 || picHeight <= 0) continue;

                                BufferedImage fixedImage = fixVgaModeYPic(
                                            i, decompressedData, picWidth, picHeight);

                                PICS.put(i, fixedImage);
                            }
                        }
                    }

                }
            }

        }
        private static void ExtractPictable(byte[] pictableData, int picsCount)
        {
            int pictableOffsetIndex = 3;
            pictable = new Dimension[picsCount];
            for (int i = 0; i < pictableData.Length / 4; i++)
            {
                int b0 = pictableData[4 * i + 0] & 0xff;
                int b1 = pictableData[4 * i + 1] & 0xff;
                int b2 = pictableData[4 * i + 2] & 0xff;
                int b3 = pictableData[4 * i + 3] & 0xff;
                int picWidth = b0 + (b1 << 8);
                int picHeight = b2 + (b3 << 8);
                pictable[i + pictableOffsetIndex] = new Dimension(picWidth, picHeight);
            }
        }

        private static byte[] DecompressHuffman(ByteBuffer compressed)
        {
            int decompressedLength = (int)(compressed.GetInt() & 0xffffffffL);
            if (decompressedLength <= 0) return null;
            byte[] data = new byte[decompressedLength];
            int bitIndex = 0;
            int dataIndex = 0;
            int nodeIndex = 254;
            while (dataIndex < decompressedLength)
            {
                int bit = (compressed.Get(compressed.Position) >> bitIndex) & 1;
                bitIndex++;
                if (bitIndex > 7)
                {
                    bitIndex = 0;
                    compressed.Position(compressed.Position + 1);
                }
                if (compressed.Position >= compressed.Limit) break;
                if (huffmanNodes[nodeIndex * 4 + 1 + bit * 2] == 0)
                {
                    data[dataIndex++] = huffmanNodes[nodeIndex * 4 + bit * 2];
                    nodeIndex = 254;
                }
                else if (huffmanNodes[nodeIndex * 4 + 1 + bit * 2] == 1)
                {
                    nodeIndex = huffmanNodes[nodeIndex * 4 + bit * 2] & 0xff;
                }

            }
            return data;
        }
        public static Bitmap FixVgaModeYPic(int index, byte[] picData, int picWidth, int picHeight)
        {
            Bitmap bi = new Bitmap(picWidth, picHeight, PixelFormat.Format8bppIndexed);
            BitmapData data = bi.LockBits(new Rectangle(0, 0, picWidth, picHeight), ImageLockMode.WriteOnly, bi.PixelFormat);
            Marshal.Copy(picData, 0, data.Scan0, picData.Length);
            bi.UnlockBits(data);

            Bitmap fixedImage = new Bitmap(picWidth, picHeight, PixelFormat.Format24bppRgb);

            int w4 = picWidth / 4;
            int h4 = picHeight / 4;
            for (int y = 0; y < picHeight; y++)
            {
                for (int x = 0; x < picWidth; x++)
                {
                    int xs = 4 * (x % w4) + (y / h4);
                    int ys = 4 * (y % h4) + (x / w4);
                    Color c = bi.GetPixel(x, y);
                    fixedImage.SetPixel(xs, ys, c);
                }
            }

            return fixedImage;
        }
        public static Bitmap GetPic(int picIndex)
        {
            return PICS[picIndex];
        }

        public static VGAGRAPHFont GetFont(string fontId)
        {
            return FONTS[fontId];
        }
//gotta fix errors

    }
}
