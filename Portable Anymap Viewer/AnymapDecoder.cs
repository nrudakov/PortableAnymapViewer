using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Threading;

namespace Portable_Anymap_Viewer
{
    public class AnymapDecoder
    {
        public async Task<DecodeResult> decode(StorageFile file)
        {
            var stream = await file.OpenAsync(FileAccessMode.Read);
            ulong size = stream.Size;

            var dataReader = new DataReader(stream.GetInputStreamAt(0));
            await dataReader.LoadAsync((uint)2);
            string formatType = dataReader.ReadString(2);
            DecodeResult result = new DecodeResult();
            result.Width = 0;
            result.Height = 0;
            result.Bytes = null;
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
                    break;
                case '2':
                    break;
                case '3':
                    break;
                case '4':
                    properties = await GetBinaryImageProperties(file, false);
                    decodedAnymap = new byte[properties.Width * properties.Height * 4];
                    stream = await file.OpenAsync(FileAccessMode.Read);
                    size = stream.Size;
                    dataReader = new DataReader(stream.GetInputStreamAt(properties.StreamPosition));

                    bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                    if (properties.BytesPerColor == 1)
                    {
                        int resultIndex = 0;
                        int mod = (int)(properties.Width * properties.Height) % 8;
                        
                        // All bytes except last one
                        for (uint i = 0; i < bytesLoaded - 1; ++i)
                        {
                            unpackBytePbm(decodedAnymap, resultIndex, 8, dataReader.ReadByte());
                        }
                        // The last byte
                        unpackBytePbm(decodedAnymap, resultIndex, mod == 0 ? 8 : mod, dataReader.ReadByte());
                    }
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
                case '5':
                    properties = await GetBinaryImageProperties(file, true);
                    decodedAnymap = new byte[properties.Width * properties.Height * 4];
                    stream = await file.OpenAsync(FileAccessMode.Read);
                    size = stream.Size;
                    dataReader = new DataReader(stream.GetInputStreamAt(properties.StreamPosition));

                    bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                    if (properties.BytesPerColor == 1)
                    {
                        int resultIndex = 0;
                        for (int i = 0; i < properties.Height; ++i)
                        {
                            for (int j = 0; j < properties.Width; ++j)
                            {
                                decodedAnymap[resultIndex] = dataReader.ReadByte();
                                decodedAnymap[resultIndex + 1] = decodedAnymap[resultIndex];
                                decodedAnymap[resultIndex + 2] = decodedAnymap[resultIndex];
                                decodedAnymap[resultIndex + 3] = 255;
                                resultIndex += 4;
                            }
                        }
                    }
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
                case '6':
                    properties = await GetBinaryImageProperties(file, true);
                    decodedAnymap = new byte[properties.Width * properties.Height * 4];

                    stream = await file.OpenAsync(FileAccessMode.Read);
                    size = stream.Size;
                    dataReader = new DataReader(stream.GetInputStreamAt(properties.StreamPosition));

                    bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                    if (properties.BytesPerColor == 1)
                    {
                        int resultIndex = 0;
                        for (int i = 0; i < properties.Height; ++i)
                        {
                            for (int j = 0; j < properties.Width; ++j)
                            {
                                decodedAnymap[resultIndex + 2] = dataReader.ReadByte();
                                decodedAnymap[resultIndex + 1] = dataReader.ReadByte();
                                decodedAnymap[resultIndex] = dataReader.ReadByte();
                                decodedAnymap[resultIndex + 3] = 255;
                                resultIndex += 4;
                            }
                        }
                    }
                    result.Width = (Int32)properties.Width;
                    result.Height = (Int32)properties.Height;
                    result.Bytes = decodedAnymap;
                    break;
            }
            return result;
        }

        private async Task<AnymapProperties> GetBinaryImageProperties(StorageFile file, bool isContainMaxValue)
        {
            var stream = await file.OpenAsync(FileAccessMode.Read);
            ulong size = stream.Size;

            var dataReader = new DataReader(stream.GetInputStreamAt(0));
            uint bytesLoaded = await dataReader.LoadAsync((uint)size);
            byte[] bytes = new byte[bytesLoaded];
            dataReader.ReadBytes(bytes);

            ASCIIEncoding ascii = new ASCIIEncoding();
            string strAll = ascii.GetString(bytes);

            uint[] imageProperties = new uint[3];
            uint imagePropertiesCounter = 0;
            string[] strs = strAll.Split('\n');
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
            stream = await file.OpenAsync(FileAccessMode.Read);
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
                arr[arrPos] = (byte)(((b & k) == k) ? 255 : 0);
                arr[arrPos + 1] = arr[arrPos];
                arr[arrPos + 2] = arr[arrPos];
                arr[arrPos + 3] = 255;
                arrPos += 4;
            }
        };
    }
}
