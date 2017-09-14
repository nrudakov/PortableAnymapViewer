using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;

namespace Portable_Anymap_Viewer.Classes
{
    public class AnymapDecoder
    {
        private async Task<DecodeResult> decode(IRandomAccessStream stream, String filename)
        {
            ulong size = stream.Size;
            DecodeResult result = new DecodeResult();
            result.Type = 0;
            result.Width = 0;
            result.Height = 0;
            result.Bytes = null;
            result.Filename = filename;
            result.CurrentZoom = 1.0f;
            if (size >= 2)
            {
                stream.Seek(0);
                var dataReader = new DataReader(stream);
                await dataReader.LoadAsync((uint)2);
                string formatType = dataReader.ReadString(2);
                if (formatType[0] != 'P')
                {
                    return result;
                }
                AnymapProperties properties = null;
                byte[] decodedAnymap = null;
                uint bytesLoaded = 0;
                uint pixelsNum = 0;
                uint packedPixelsNum = 0;
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
                            String strAll = dataReader.ReadString(bytesLoaded);
                            if (properties.BytesPerColor == 1)
                            {
                                int resultIndex = 0;
                                String[] strs = strAll.Split(new String[] { "\r", "\r\n", "\n" }, StringSplitOptions.None);
                                MatchCollection mc = null;
                                foreach (string str in strs)
                                {
                                    mc = Regex.Matches(Regex.Match(str, @"[^#]{0,}").ToString(), @"\b\d+");
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
                        result.DoubleBytesPerColor = properties.BytesPerColor == 2;
                        break;
                    case '2':
                        properties = await GetImageProperties(stream, true);
                        if (properties.MaxValue != 0)
                        {
                            stream.Seek(properties.StreamPosition);
                            dataReader = new DataReader(stream);
                            bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                            String strAll = dataReader.ReadString(bytesLoaded);
                            if (properties.BytesPerColor == 1)
                            {
                                decodedAnymap = new byte[properties.Width * properties.Height * 4];
                                int resultIndex = 0;
                                String[] strs = strAll.Split(new String[] { "\r", "\r\n", "\n" }, StringSplitOptions.None);
                                MatchCollection mc = null;
                                foreach (string str in strs)
                                {
                                    mc = Regex.Matches(Regex.Match(str, @"[^#]{0,}").ToString(), @"\b\d+");
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
                            else
                            {
                                decodedAnymap = new byte[properties.Width * properties.Height * 8];
                                int resultIndex = 0;
                                String[] strs = strAll.Split(new String[] { "\r", "\r\n", "\n" }, StringSplitOptions.None);
                                MatchCollection mc = null;
                                UInt16 tmp = 0;
                                foreach (string str in strs)
                                {
                                    mc = Regex.Matches(Regex.Match(str, @"[^#]{0,}").ToString(), @"\b\d+");
                                    for (int i = 0; i < mc.Count && resultIndex < decodedAnymap.Length; ++i)
                                    {
                                        tmp = (UInt16)(Convert.ToUInt32(mc[i].Value) / (double)properties.MaxValue * 65535);
                                        decodedAnymap[resultIndex + 1] = (byte)(tmp >> 8);
                                        decodedAnymap[resultIndex] = (byte)(tmp & 0x00FF);

                                        decodedAnymap[resultIndex + 3] = decodedAnymap[resultIndex + 1];
                                        decodedAnymap[resultIndex + 2] = decodedAnymap[resultIndex];

                                        decodedAnymap[resultIndex + 5] = decodedAnymap[resultIndex + 1];
                                        decodedAnymap[resultIndex + 4] = decodedAnymap[resultIndex];

                                        decodedAnymap[resultIndex + 7] = 255;
                                        decodedAnymap[resultIndex + 6] = 255;
                                        resultIndex += 8;
                                    }
                                }
                            }
                        }
                        result.Type = 2;
                        result.Width = (Int32)properties.Width;
                        result.Height = (Int32)properties.Height;
                        result.Bytes = decodedAnymap;
                        result.DoubleBytesPerColor = properties.BytesPerColor == 2;
                        break;
                    case '3':
                        result.HistogramValues = new List<List<HistogramValue>>(3);
                        result.HistogramValues.Add(new List<HistogramValue>(256));
                        result.HistogramValues.Add(new List<HistogramValue>(256));
                        result.HistogramValues.Add(new List<HistogramValue>(256));
                        foreach (List<HistogramValue> list in result.HistogramValues)
                        {
                            for (int i = 0; i < 256; ++i)
                            {
                                list.Add(new HistogramValue { Brightness = i, Level = 0.0 });
                            }
                        }
                        properties = await GetImageProperties(stream, true);
                        if (properties.MaxValue != 0)
                        {
                            stream.Seek(properties.StreamPosition);
                            dataReader = new DataReader(stream);
                            bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                            String strAll = dataReader.ReadString(bytesLoaded);
                            if (properties.BytesPerColor == 1)
                            {
                                decodedAnymap = new byte[properties.Width * properties.Height * 4];
                                int resultIndex = 0;
                                string[] strs = strAll.Split(new String[] { "\r", "\r\n", "\n" }, StringSplitOptions.None);
                                int BgraIndex = 2;
                                MatchCollection mc = null;
                                foreach (string str in strs)
                                {
                                    mc = Regex.Matches(Regex.Match(str, @"[^#]{0,}").ToString(), @"\b\d+");
                                    for (int i = 0; i < mc.Count; ++i)
                                    {
                                        decodedAnymap[resultIndex + BgraIndex] = (byte)(Convert.ToUInt32(mc[i].Value) / (double)properties.MaxValue * 255);
                                        result.HistogramValues[BgraIndex][decodedAnymap[resultIndex + BgraIndex]].Level += 1.0;
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
                            else
                            {
                                decodedAnymap = new byte[properties.Width * properties.Height * 8];
                                int resultIndex = 0;
                                string[] strs = strAll.Split(new String[] { "\r", "\r\n", "\n" }, StringSplitOptions.None);
                                MatchCollection mc = null;
                                UInt16 tmp = 0;
                                int rgbaIndex = 0;
                                foreach (string str in strs)
                                {
                                    mc = Regex.Matches(Regex.Match(str, @"[^#]{0,}").ToString(), @"\b\d+");
                                    for (int i = 0; i < mc.Count; ++i)
                                    {
                                        tmp = (UInt16)(Convert.ToUInt32(mc[i].Value) / (double)properties.MaxValue * 65535);
                                        decodedAnymap[resultIndex + rgbaIndex + 1] = (byte)(tmp >> 8);
                                        decodedAnymap[resultIndex + rgbaIndex + 0] = (byte)(tmp & 0x00FF);
                                        rgbaIndex += 2;
                                        if (rgbaIndex == 6)
                                        {
                                            decodedAnymap[resultIndex + 7] = 255;
                                            decodedAnymap[resultIndex + 6] = 255;
                                            rgbaIndex = 0;
                                            resultIndex += 8;
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
                        result.DoubleBytesPerColor = properties.BytesPerColor == 2;
                        break;
                    case '4':
                        properties = await GetImageProperties(stream, false);
                        pixelsNum = properties.Width * properties.Height;
                        decodedAnymap = new byte[pixelsNum * 4];
                        stream.Seek(properties.StreamPosition);
                        dataReader = new DataReader(stream);
                        bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                        if (properties.BytesPerColor == 1)
                        {
                            int resultIndex = 0;
                            int mod = (int)(properties.Width * properties.Height) % 8;
                            int colmod = (int)(properties.Width) % 8;
                            packedPixelsNum = (properties.Width / 8 + (uint)(colmod == 0 ? 0 : 1)) * properties.Height;
                            uint col = 0;
                            int num = 0;
                            for (uint i = 0; i < packedPixelsNum; ++i)
                            {
                                if (col == properties.Width - colmod)
                                {
                                    num = colmod == 0 ? 8 : colmod;
                                    
                                    unpackBitsPbm(decodedAnymap, resultIndex, num, dataReader.ReadByte());
                                    col = 0;
                                    resultIndex += num * 4;
                                }
                                else
                                {
                                    unpackBitsPbm(decodedAnymap, resultIndex, 8, dataReader.ReadByte());
                                    col += 8;
                                    resultIndex += 32;
                                }
                            }
                        }
                        result.Type = 4;
                        result.Width = (Int32)properties.Width;
                        result.Height = (Int32)properties.Height;
                        result.Bytes = decodedAnymap;
                        result.DoubleBytesPerColor = properties.BytesPerColor == 2;
                        break;
                    case '5':
                        properties = await GetImageProperties(stream, true);
                        if (properties.MaxValue != 0)
                        {
                            
                            stream.Seek(properties.StreamPosition);
                            dataReader = new DataReader(stream);
                            bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                            if (properties.BytesPerColor == 1)
                            {
                                decodedAnymap = new byte[properties.Width * properties.Height * 4];
                                uint resultIndex = 0;
                                for (int i = 0; i < properties.Height; ++i)
                                {
                                    for (int j = 0; j < properties.Width && dataReader.UnconsumedBufferLength >= 1; ++j)
                                    {
                                        decodedAnymap[resultIndex] = (byte)(dataReader.ReadByte() / (double)properties.MaxValue * 255);
                                        decodedAnymap[resultIndex + 1] = decodedAnymap[resultIndex];
                                        decodedAnymap[resultIndex + 2] = decodedAnymap[resultIndex];
                                        decodedAnymap[resultIndex + 3] = 255;
                                        resultIndex += 4;
                                    }
                                }
                            }
                            else
                            {
                                decodedAnymap = new byte[properties.Width * properties.Height * 8];
                                uint resultIndex = 0;
                                UInt16 tmp;
                                for (int i = 0; i < properties.Height; ++i)
                                {
                                    for (int j = 0; j < properties.Width && dataReader.UnconsumedBufferLength >= 1; ++j)
                                    {
                                        tmp = dataReader.ReadByte();
                                        tmp <<= 8;
                                        tmp += dataReader.ReadByte();
                                        tmp = (UInt16)(tmp / (Double)properties.MaxValue * 65535);
                                        decodedAnymap[resultIndex + 1] = (byte)(tmp >> 8);
                                        decodedAnymap[resultIndex] = (byte)(tmp & 0x00FF);

                                        decodedAnymap[resultIndex + 3] = decodedAnymap[resultIndex + 1];
                                        decodedAnymap[resultIndex + 2] = decodedAnymap[resultIndex];

                                        decodedAnymap[resultIndex + 5] = decodedAnymap[resultIndex + 1];
                                        decodedAnymap[resultIndex + 4] = decodedAnymap[resultIndex];

                                        decodedAnymap[resultIndex + 7] = 255;
                                        decodedAnymap[resultIndex + 6] = 255;
                                        resultIndex += 8;
                                    }
                                }
                            }
                        }
                        result.Type = 5;
                        result.Width = (Int32)properties.Width;
                        result.Height = (Int32)properties.Height;
                        result.Bytes = decodedAnymap;
                        result.DoubleBytesPerColor = properties.BytesPerColor == 2;
                        break;
                    case '6':
                        result.HistogramValues = new List<List<HistogramValue>>(3);
                        result.HistogramValues.Add(new List<HistogramValue>(256));
                        result.HistogramValues.Add(new List<HistogramValue>(256));
                        result.HistogramValues.Add(new List<HistogramValue>(256));
                        foreach (List<HistogramValue> list in result.HistogramValues)
                        {
                            for (int i = 0; i < 256; ++i)
                            {
                                list.Add(new HistogramValue { Brightness = i, Level = 0.0 });
                            }
                        }
                        properties = await GetImageProperties(stream, true);
                        if (properties.MaxValue != 0)
                        {
                            decodedAnymap = new byte[properties.Width * properties.Height * 4];
                            stream.Seek(properties.StreamPosition);
                            dataReader = new DataReader(stream);

                            bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size - properties.StreamPosition));
                            if (properties.BytesPerColor == 1)
                            {
                                uint resultIndex = 0;
                                for (int i = 0; i < properties.Height; ++i)
                                {
                                    for (int j = 0; j < properties.Width && dataReader.UnconsumedBufferLength >= 3; ++j)
                                    {
                                        decodedAnymap[resultIndex + 2] = (byte)(dataReader.ReadByte() / (double)properties.MaxValue * 255);
                                        decodedAnymap[resultIndex + 1] = (byte)(dataReader.ReadByte() / (double)properties.MaxValue * 255);
                                        decodedAnymap[resultIndex] = (byte)(dataReader.ReadByte() / (double)properties.MaxValue * 255);
                                        decodedAnymap[resultIndex + 3] = 255;
                                        result.HistogramValues[0][decodedAnymap[resultIndex + 2]].Level += 1.0;
                                        result.HistogramValues[1][decodedAnymap[resultIndex + 1]].Level += 1.0;
                                        result.HistogramValues[2][decodedAnymap[resultIndex]].Level += 1.0;
                                        resultIndex += 4;
                                    }
                                }
                            }
                            else
                            {
                                decodedAnymap = new byte[properties.Width * properties.Height * 8];
                                uint resultIndex = 0;
                                UInt16 tmp;
                                for (int i = 0; i < properties.Height; ++i)
                                {
                                    for (int j = 0; j < properties.Width && dataReader.UnconsumedBufferLength >= 1; ++j)
                                    {
                                        tmp = dataReader.ReadByte();
                                        tmp <<= 8;
                                        tmp += dataReader.ReadByte();
                                        tmp = (UInt16)(tmp / (Double)properties.MaxValue * 65535);
                                        decodedAnymap[resultIndex + 1] = (byte)(tmp >> 8);
                                        decodedAnymap[resultIndex] = (byte)(tmp & 0x00FF);

                                        tmp = dataReader.ReadByte();
                                        tmp <<= 8;
                                        tmp += dataReader.ReadByte();
                                        tmp = (UInt16)(tmp / (Double)properties.MaxValue * 65535);
                                        decodedAnymap[resultIndex + 3] = (byte)(tmp >> 8);
                                        decodedAnymap[resultIndex + 2] = (byte)(tmp & 0x00FF);

                                        tmp = dataReader.ReadByte();
                                        tmp <<= 8;
                                        tmp += dataReader.ReadByte();
                                        tmp = (UInt16)(tmp / (Double)properties.MaxValue * 65535);
                                        decodedAnymap[resultIndex + 5] = (byte)(tmp >> 8);
                                        decodedAnymap[resultIndex + 4] = (byte)(tmp & 0x00FF);

                                        decodedAnymap[resultIndex + 7] = 255;
                                        decodedAnymap[resultIndex + 6] = 255;
                                        resultIndex += 8;
                                    }
                                }
                            }
                        }
                        result.Type = 6;
                        result.Width = (Int32)properties.Width;
                        result.Height = (Int32)properties.Height;
                        result.Bytes = decodedAnymap;
                        result.DoubleBytesPerColor = properties.BytesPerColor == 2;
                        break;
                }
                dataReader.DetachBuffer();
                dataReader.DetachStream();
                dataReader.Dispose();
            }
            //foreach (List<HistogramValue> list in result.HistogramValues)
            //{
            //    foreach (HistogramValue value in list)
            //    {
            //        value.Level /= result.Width * result.Height;
            //    }
            //}
            GC.Collect();
            return result;
        }

        public async Task<DecodeResult> decode(StorageFile file)
        {
            var stream = await file.OpenAsync(FileAccessMode.Read);
            var dataReader = new DataReader(stream);
            var bytesLoaded = await dataReader.LoadAsync((uint)stream.Size);
            stream.Seek(0);
            var result = await decode(stream, file.Name);
            // Hash sum
            var iBuffer = dataReader.ReadBuffer(bytesLoaded);
            var hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).CreateHash();
            hasher.Append(iBuffer);
            result.Md5 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1).CreateHash();
            hasher.Append(iBuffer);
            result.Sha1 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            //hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256).CreateHash();
            //hasher.Append(iBuffer);
            //result.Sha256 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            //hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha384).CreateHash();
            //hasher.Append(iBuffer);
            //result.Sha384 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            //hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha512).CreateHash();
            //hasher.Append(iBuffer);
            //result.Sha512 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            return result;
        }

        public async Task<DecodeResult> decode(byte[] bytes)
        {
            var stream = (new MemoryStream(bytes)).AsRandomAccessStream();
            var dataReader = new DataReader(stream);
            var bytesLoaded = await dataReader.LoadAsync((uint)stream.Size);
            stream.Seek(0);
            var result = await decode(stream, "");
            // Hash sum
            var iBuffer = dataReader.ReadBuffer(bytesLoaded);
            var hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Md5).CreateHash();
            hasher.Append(iBuffer);
            result.Md5 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha1).CreateHash();
            hasher.Append(iBuffer);
            result.Sha1 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            //hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha256).CreateHash();
            //hasher.Append(iBuffer);
            //result.Sha256 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            //hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha384).CreateHash();
            //hasher.Append(iBuffer);
            //result.Sha384 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            //hasher = HashAlgorithmProvider.OpenAlgorithm(HashAlgorithmNames.Sha512).CreateHash();
            //hasher.Append(iBuffer);
            //result.Sha512 = CryptographicBuffer.EncodeToHexString(hasher.GetValueAndReset());
            return result;
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
            String strAll = ascii.GetString(bytes);
            uint[] imageProperties = new uint[3];
            uint imagePropertiesCounter = 0;
            string[] strs = strAll.Split(new String[] { "\r", "\r\n", "\n" }, StringSplitOptions.None);
            int seekPos = 0;
            MatchCollection mc = null;
            foreach (string str in strs)
            {
                mc = Regex.Matches(Regex.Match(str, "[^#]{0,}").ToString(), @"\b\d+");
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
            dataReader.DetachBuffer();
            dataReader.DetachStream();
            dataReader.Dispose();
            GC.Collect();
            stream.Seek((ulong)(seekPos));
            AnymapProperties properties = new AnymapProperties();
            properties.Width = imageProperties[0];
            properties.Height = imageProperties[1];
            properties.MaxValue = (isContainMaxValue ? imageProperties[2] : 255);
            properties.BytesPerColor = (properties.MaxValue < 256) ? (uint)1 : (uint)2;
            properties.StreamPosition = (uint)(seekPos);
            return properties;
        }

        private Action<byte[], int, int, byte> unpackBitsPbm = delegate (byte[] arr, int arrPos, int num, byte b)
        {
            byte kk = (byte)(0x80 >> num);
            int shift = 0;
            for (byte k = 0x80; k != kk; k >>= 1)
            {
                arr[arrPos + shift] = (byte)(((b & k) == k) ? 0 : 255);
                arr[arrPos + shift + 1] = arr[arrPos + shift];
                arr[arrPos + shift + 2] = arr[arrPos + shift];
                arr[arrPos + shift + 3] = 255;
                shift += 4;
            }
        };
    }
}
