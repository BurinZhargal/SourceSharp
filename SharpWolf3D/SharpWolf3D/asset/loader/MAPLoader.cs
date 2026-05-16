using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using J2N;
using J2N.IO;

namespace SharpWolf3D.asset.loader
{
    public class MAPLoader
    {
        public static void Fill<T>(T[] array, int start, int end, T value)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (start < 0 || start >= end)
            {
                throw new ArgumentOutOfRangeException("fromIndex");
            }
            if (end >= array.Length)
            {
                throw new ArgumentOutOfRangeException("toIndex");
            }
            for (int i = start; i < end; i++)
            {
                array[i] = value;
            }
        }
        private static int[] mapOffsets;
        private static readonly Dictionary<int, int[][]> MAPS = new Dictionary<int, int[][]>();

        public static string GetEntityMAPS(int code)
        {
            // java's get method returns null when the key has no mapping
            // so we'll do the same

            string val;
            if (MAPS.TryGetValue(code, out val))
                return val;
            else
                return null;
        }

        private static readonly Dictionary<int, string> MAP_NAMES = new Dictionary<int, string>();

        public static string GetEntityMAPNAMES(int code)
        {
            // java's get method returns null when the key has no mapping
            // so we'll do the same

            string val;
            if (MAP_NAMES.TryGetValue(code, out val))
                return val;
            else
                return null;
        }
        public static void load(String path, String mapHeadRes, String gameMapsRes)
        {
            try
            {
               
                
                Stream mapHeadIs = new FileStream(path + mapHeadRes, FileMode.Open, FileAccess.Read);
                Stream gameMapIs = new FileStream(path + gameMapsRes, FileMode.Open, FileAccess.Read);
                    // extract the header and map offsets info
                    sbyte[] magic = new sbyte[2];
                     mapHeadIs.Read((byte[])(Array)magic, 0, magic.Length);
                //??? ByteBuffer conversion problem
                    MemoryStream mapHeadData = new MemoryStream();
                    using(StreamWriter sw = new StreamWriter(mapHeadIs))
                {
                    sw.Write(mapHeadIs.ToString());
                }
             

                    mapOffsets = new int[100];
                    int index = 0;
                    while (index < mapOffsets.Length)
                    {
                        mapOffsets[index++] = Convert.ToInt32(mapHeadData);
                    }

                // extract all the maps
              
                ByteBuffer gameMapData = ByteBuffer.Wrap(Convert.FromBase64String(gameMapIs.ToString()));
                gameMapData.SetOrder(ByteOrder.LittleEndian);
                    for (int i = 0; i < index; i++)
                    {
                        int[][] map = new int[2][];
                        int mapOffset = mapOffsets[i];
                        if (mapOffset <= 0)
                        {
                            continue;
                        }
                        gameMapData.SetPosition(mapOffset);
                        int offPlane0 = gameMapData.GetInt32();
                        int offPlane1 = gameMapData.GetInt32();
                        int offPlane2 = gameMapData.GetInt32();
                        int lenPlane0 = gameMapData.GetInt16();
                        int lenPlane1 = gameMapData.GetInt16();
                        int lenPlane2 = gameMapData.GetInt16();
                        int width = gameMapData.GetInt16();
                        int height = gameMapData.GetInt16();
                        sbyte[] name = new sbyte[16];
                        gameMapData.Get((byte[])(Array)name);
                        String mapName = new String(name.ToString());

                        // layer 0
                        ByteBuffer carmackDecompressed = decompressCarmack(gameMapData, offPlane0);

                        carmackDecompressed.SetOrder(ByteOrder.LittleEndian);
                        carmackDecompressed.SetPosition(0);
                        Int32Buffer rlewDecompressed = decompressRLEW(carmackDecompressed, 0);
                    //?????
                        map[0] = (int[])(Array)Convert.ToInt32(rlewDecompressed);

                        // layer 1 
                        carmackDecompressed = decompressCarmack(gameMapData, offPlane1);
                        carmackDecompressed.SetOrder(ByteOrder.LittleEndian);
                        carmackDecompressed.SetPosition(0);
                        rlewDecompressed = decompressRLEW(carmackDecompressed, 0);
                        map[1] = rlewDecompressed.Array;

                        MAPS[i] = map;
                        MAP_NAMES[i] = mapName;
                    }
                
            }
            catch (IOException ex)
            {
                throw new Exception("Could not load MAP resources properly!", ex);
            }
        }
        private static ByteBuffer decompressCarmack(ByteBuffer bb, int start) 
        {
            byte nearTag = (byte)0xA7;
            byte farTag = (byte)0xA8;
            bb.SetPosition(start);
            byte[] decompressedData = new byte[bb.GetInt16() & 0xFFFF];
            int decompressedDataIndex = 0;
            while(decompressedDataIndex< decompressedData.Length)
            {
                byte b0 = bb.Get();
                byte b1 = bb.Get();
                if((b0==0&&b1==nearTag)||(b0==0&&b1==farTag))
                {
                    decompressedData[decompressedDataIndex++] = bb.Get();
                    decompressedData[decompressedDataIndex++]=b1; 
                }
                else if (b1==nearTag||b1==farTag)
                {
                    int location = (b1 == nearTag ? decompressedDataIndex - 2 * (bb.Get() & 0xff) : 2 * (bb.GetInt16() & 0xFFFF));
                    int sizeInBytes = (b0 & 0xff) * 2;
                    System.Array.Copy(decompressedData, location, decompressedData, decompressedDataIndex, sizeInBytes);
                    decompressedDataIndex += sizeInBytes;
                }
                else
                {
                    decompressedData[decompressedDataIndex++] = b0;

                    decompressedData[decompressedDataIndex++] = b1;
                }
            }
            ByteBuffer decompressedByteBuffer = ByteBuffer.Wrap(decompressedData);
            decompressedByteBuffer.SetOrder(ByteOrder.LittleEndian);
            return decompressedByteBuffer;
        }
        private static Int32Buffer decompressRLEW(ByteBuffer bb, int start)
        {
            bb.SetPosition(start);
            int[] decompressedData = new int[(bb.GetInt16() & 0xFFFF) / 2];
            int decompressedDataIndex = 0;
            while(decompressedDataIndex<decompressedData.Length)
            {
                short s = bb.GetInt16();
                if (s == Convert.ToInt16( 0xABCD))
                {
                    int count = bb.GetInt16() & 0xffff;
                    Fill(decompressedData, decompressedDataIndex, decompressedDataIndex + count, bb.GetInt16() & 0xFFFF);
                    decompressedDataIndex += count;
                }
                else
                {
                    decompressedData[decompressedDataIndex++] = s & 0xffff;
                }

            }
            Int32Buffer decompressedBuffer = Int32Buffer.Wrap(decompressedData);
            return decompressedBuffer;
        }
        public static int[][] getMap (int mapId)
        {
            int[][] Map = new int[Convert.ToInt32(GetEntityMAPS(mapId).ToString().Select(o=>Convert.ToInt32(o)).ToArray())][];
            return Map;
        }
        public static String GetMapName (int mapId)
        {
            return GetEntityMAPNAMES(mapId);
        }
    }
}
