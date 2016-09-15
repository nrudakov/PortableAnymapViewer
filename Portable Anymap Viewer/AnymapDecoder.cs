using Portable_Anymap_Viewer.Models;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Portable_Anymap_Viewer
{
    public class AnymapDecoder
    {
        private async Task<DecodeResult> decode(IRandomAccessStream stream, String filename)
        {
            ulong size = stream.Size;
            stream.Seek(0);
            var dataReader = new DataReader(stream);
            await dataReader.LoadAsync((uint)2);
            string formatType = dataReader.ReadString(2);
            DecodeResult result = new DecodeResult();
            result.Type = 0;
            result.Width = 0;
            result.Height = 0;
            result.Bytes = null;
            result.Filename = filename;
            result.CurrentZoom = 1.0;
            if (formatType[0] != 'P')
            {
                return result;
            }
            AnymapProperties properties = null;
            byte[] decodedAnymap = null;
            uint bytesLoaded = 0;
            switch (formatType[1])
            {
                case '1':
                    properties = await GetImageProperties(stream, false);
                    if (properties.MaxValue != 0)
                    {
                        decodedAnymap = new byte[properties.Width * properties.Height * 4];
                        stream.Seek(properties.StreamPosition);
                        dataReader = new DataReader(stream);

                        bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                        if (properties.BytesPerColor == 1)
                        {
                            int resultIndex = 0;
                            byte[] bytes = new byte[bytesLoaded];
                            dataReader.ReadBytes(bytes);

                            ASCIIEncoding ascii = new ASCIIEncoding();
                            string strAll = ascii.GetString(bytes);

                            string[] strs = strAll.Split('\n');
                            foreach (string str in strs)
                            {
                                MatchCollection mc = Regex.Matches(Regex.Match(str, @"[^#]{0,}").ToString(), @"\b\d+");
                                for (int i = 0; i < mc.Count && resultIndex < decodedAnymap.Length; ++i)
                                {
                                    decodedAnymap[resultIndex] = (byte)((Convert.ToUInt32(mc[i].Value) ^ 0x01) * 255);
                                    decodedAnymap[resultIndex + 1] = decodedAnymap[resultIndex];
                                    decodedAnymap[resultIndex + 2] = decodedAnymap[resultIndex];
                                    decodedAnymap[resultIndex + 3] = 255;
                                    resultIndex += 4;
                                }
                            }
                        }
                    }
                    result.Type = 1;
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
                case '2':
                    properties = await GetImageProperties(stream, true);
                    if (properties.MaxValue != 0)
                    {
                        decodedAnymap = new byte[properties.Width * properties.Height * 4];
                        stream.Seek(properties.StreamPosition);
                        dataReader = new DataReader(stream);

                        bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                        if (properties.BytesPerColor == 1)
                        {
                            int resultIndex = 0;
                            byte[] bytes = new byte[bytesLoaded];
                            dataReader.ReadBytes(bytes);

                            ASCIIEncoding ascii = new ASCIIEncoding();
                            string strAll = ascii.GetString(bytes);

                            string[] strs = strAll.Split('\n');
                            foreach (string str in strs)
                            {
                                MatchCollection mc = Regex.Matches(Regex.Match(str, @"[^#]{0,}").ToString(), @"\b\d+");
                                for (int i = 0; i < mc.Count && resultIndex < decodedAnymap.Length; ++i)
                                {
                                    decodedAnymap[resultIndex] = (byte)(Convert.ToUInt32(mc[i].Value) / (double)properties.MaxValue * 255);
                                    decodedAnymap[resultIndex + 1] = decodedAnymap[resultIndex];
                                    decodedAnymap[resultIndex + 2] = decodedAnymap[resultIndex];
                                    decodedAnymap[resultIndex + 3] = 255;
                                    resultIndex += 4;
                                }
                            }
                        }
                    }
                    result.Type = 2;
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
                case '3':
                    properties = await GetImageProperties(stream, true);
                    if (properties.MaxValue != 0)
                    {
                        decodedAnymap = new byte[properties.Width * properties.Height * 4];
                        stream.Seek(properties.StreamPosition);
                        dataReader = new DataReader(stream);

                        bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                        if (properties.BytesPerColor == 1)
                        {
                            int resultIndex = 0;
                            byte[] bytes = new byte[bytesLoaded];
                            dataReader.ReadBytes(bytes);

                            ASCIIEncoding ascii = new ASCIIEncoding();
                            string strAll = ascii.GetString(bytes);

                            string[] strs = strAll.Split('\n');
                            int BgraIndex = 2;
                            foreach (string str in strs)
                            {
                                MatchCollection mc = Regex.Matches(Regex.Match(str, @"[^#]{0,}").ToString(), @"\b\d+");
                                for (int i = 0; i < mc.Count; ++i)
                                {
                                    decodedAnymap[resultIndex + BgraIndex] = (byte)(Convert.ToUInt32(mc[i].Value) / (double)properties.MaxValue * 255);
                                    --BgraIndex;
                                    if (BgraIndex == -1)
                                    {
                                        BgraIndex = 3;
                                        decodedAnymap[resultIndex + BgraIndex] = 255;
                                        resultIndex += 4;
                                        --BgraIndex;
                                        if (resultIndex >= decodedAnymap.Length)
                                        {
                                            break;
                                        }
                                    }
                                }
                                if (resultIndex >= decodedAnymap.Length)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    result.Type = 3;
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
                case '4':
                    properties = await GetImageProperties(stream, false);
                    decodedAnymap = new byte[properties.Width * properties.Height * 4];
                    stream.Seek(properties.StreamPosition);
                    dataReader = new DataReader(stream);

                    bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                    if (properties.BytesPerColor == 1)
                    {
                        int resultIndex = 0;
                        int mod = (int)(properties.Width * properties.Height) % 8;
                        
                        for (uint i = 0; i < bytesLoaded - 1; ++i)
                        {
                            unpackBytePbm(decodedAnymap, resultIndex, 8, dataReader.ReadByte());
                        }
                        unpackBytePbm(decodedAnymap, resultIndex, mod == 0 ? 8 : mod, dataReader.ReadByte());
                    }
                    result.Type = 4;
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
                case '5':
                    properties = await GetImageProperties(stream, true);
                    if (properties.MaxValue != 0)
                    {
                        decodedAnymap = new byte[properties.Width * properties.Height * 4];
                        stream.Seek(properties.StreamPosition);
                        dataReader = new DataReader(stream);

                        bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                        if (properties.BytesPerColor == 1)
                        {
                            int resultIndex = 0;
                            for (int i = 0; i < properties.Height; ++i)
                            {
                                for (int j = 0; j < properties.Width; ++j)
                                {
                                    decodedAnymap[resultIndex] = (byte)(dataReader.ReadByte() / (double)properties.MaxValue * 255);
                                    decodedAnymap[resultIndex + 1] = decodedAnymap[resultIndex];
                                    decodedAnymap[resultIndex + 2] = decodedAnymap[resultIndex];
                                    decodedAnymap[resultIndex + 3] = 255;
                                    resultIndex += 4;
                                }
                            }
                        }
                    }
                    result.Type = 5;
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
                case '6':
                    properties = await GetImageProperties(stream, true);
                    if (properties.MaxValue != 0)
                    {
                        decodedAnymap = new byte[properties.Width * properties.Height * 4];
                        stream.Seek(properties.StreamPosition);
                        dataReader = new DataReader(stream);

                        bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                        if (properties.BytesPerColor == 1)
                        {
                            int resultIndex = 0;
                            for (int i = 0; i < properties.Height; ++i)
                            {
                                for (int j = 0; j < properties.Width; ++j)
                                {
                                    decodedAnymap[resultIndex + 2] = (byte)(dataReader.ReadByte() / (double)properties.MaxValue * 255);
                                    decodedAnymap[resultIndex + 1] = (byte)(dataReader.ReadByte() / (double)properties.MaxValue * 255);
                                    decodedAnymap[resultIndex] = (byte)(dataReader.ReadByte() / (double)properties.MaxValue * 255);
                                    decodedAnymap[resultIndex + 3] = 255;
                                    resultIndex += 4;
                                }
                            }
                        }
                    }
                    result.Type = 6;
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
            }
            return result;
        }
        public async Task<DecodeResult> decode(StorageFile file)
        {
            var stream = await file.OpenAsync(FileAccessMode.Read);
            return await decode(stream, file.Name);
        }

        public async Task<DecodeResult> decode(byte[] bytes)
        {
            DecodeResult result = new DecodeResult();
            var stream = (new MemoryStream(bytes)).AsRandomAccessStream();
            return await decode(stream, "");
        }
        
        private async Task<AnymapProperties> GetImageProperties(IRandomAccessStream stream, bool isContainMaxValue)
        {
            ulong size = stream.Size;
            stream.Seek(0);
            var dataReader = new DataReader(stream);
            uint bytesLoaded = await dataReader.LoadAsync((uint)size);
            byte[] bytes = new byte[bytesLoaded];
            dataReader.ReadBytes(bytes);

            ASCIIEncoding ascii = new ASCIIEncoding();
            string strAll = ascii.GetString(bytes);

            uint[] imageProperties = new uint[3];
            uint imagePropertiesCounter = 0;
            //string[] strs = strAll.Split(new String[] { Environment.NewLine }, StringSplitOptions.None);
            string[] strs = strAll.Split('\r');
            int seekPos = 0;
            foreach (string str in strs)
            {
                MatchCollection mc = Regex.Matches(Regex.Match(str, "[^#]{0,}").ToString(), @"\b\d+");
                for (int i = 0; i < mc.Count; ++i)
                {
                    imageProperties[imagePropertiesCounter++] = Convert.ToUInt32(mc[i].Value);
                    if (imagePropertiesCounter == (isContainMaxValue ? 3 : 2))
                    {
                        seekPos += (mc[i].Index + mc[i].Length + 1);
                        break;
                    }
                }
                if (imagePropertiesCounter == (isContainMaxValue ? 3 : 2))
                {
                    break;
                }
                seekPos += str.Length + 1;
            }

            if (imagePropertiesCounter != (isContainMaxValue ? 3 : 2))
            {
                AnymapProperties badProperties = new AnymapProperties();
                badProperties.Width = 0;
                badProperties.Height = 0;
                badProperties.MaxValue = 0;
                badProperties.BytesPerColor = 0;
                badProperties.StreamPosition = 0;
                return badProperties;
            }
            stream.Seek((ulong)(seekPos));
            AnymapProperties properties = new AnymapProperties();
            properties.Width = imageProperties[0];
            properties.Height = imageProperties[1];
            properties.MaxValue = (isContainMaxValue ? imageProperties[2] : 255);
            properties.BytesPerColor = (properties.MaxValue < 256) ? (uint)1 : (uint)2;
            properties.StreamPosition = (uint)(seekPos);
            return properties;
        }

        private Action<byte[], int, int, byte> unpackBytePbm = delegate (byte[] arr, int arrPos, int num, byte b)
        {
            byte kk = (byte)(0x80 >> num);
            for (byte k = 0x80; k != kk; k >>= 1)
            {
                arr[arrPos] = (byte)(((b & k) == k) ? 0 : 255);
                arr[arrPos + 1] = arr[arrPos];
                arr[arrPos + 2] = arr[arrPos];
                arr[arrPos + 3] = 255;
                arrPos += 4;
            }
        };
    }
}
