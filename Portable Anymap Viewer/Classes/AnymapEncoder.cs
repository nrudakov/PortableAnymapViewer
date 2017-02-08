using Portable_Anymap_Viewer.Models;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Portable_Anymap_Viewer.Classes
{
    public class AnymapEncoder
    {

        private static Double coeffR;
        private static Double coeffG;
        private static Double coeffB;

        public AnymapEncoder()
        {
            coeffR = 0.299;
            coeffG = 0.587;
            coeffB = 0.114;
        }

        public AnymapEncoder(Double CoeffR, Double CoeffG, Double CoeffB)
        {
            coeffR = CoeffR;
            coeffG = CoeffG;
            coeffB = CoeffB;
        }

        private delegate void WriteBinaryDelegate(byte[] image, MemoryStream stream, UInt32 totalPixels, AnymapProperties properties);
        private delegate void WriteTextDelegate(byte[] image, StringWriter stringWriter, UInt32 totalPixels, AnymapProperties properties);
        
        public async Task<String> encodeText(BitmapDecoder imageDecoder, AnymapProperties properties)
        {
            var pixelDataProvider = await imageDecoder.GetPixelDataAsync();
            var image = pixelDataProvider.DetachPixelData();
            UInt32 totalPixels = imageDecoder.OrientedPixelWidth * imageDecoder.OrientedPixelHeight;
            WriteTextDelegate write = null;
            bool isContainMaxValue = true;
            StringWriter stringWriter = new StringWriter();
            // Format
            switch (properties.AnymapType)
            {
                case AnymapType.Bitmap:
                    stringWriter.WriteLine("P1");
                    isContainMaxValue = false;
                    write = WriteP1;
                    break;
                case AnymapType.Graymap:
                    stringWriter.WriteLine("P2");
                    write = WriteP2;
                    break;
                case AnymapType.Pixmap:
                    stringWriter.WriteLine("P3");
                    write = WriteP3;
                    break;
            }
            // Size
            stringWriter.WriteLine("{0} {1}", imageDecoder.OrientedPixelWidth, imageDecoder.OrientedPixelHeight);
            properties.Width = imageDecoder.OrientedPixelWidth;
            properties.Height = imageDecoder.OrientedPixelHeight;
            // Maximum pixel value
            if (isContainMaxValue)
            {
                stringWriter.WriteLine("{0}", properties.MaxValue);
            }
            // Pixels
            write(image, stringWriter, totalPixels, properties);
            return stringWriter.ToString();
        }

        public async Task<byte[]> encodeBinary(BitmapDecoder imageDecoder, AnymapProperties properties)
        {
            var pixelDataProvider = await imageDecoder.GetPixelDataAsync();
            var image = pixelDataProvider.DetachPixelData();
            UInt32 totalPixels = imageDecoder.OrientedPixelWidth * imageDecoder.OrientedPixelHeight;
            UInt32 bytesNumForPixels = 0;
            Byte type = 0;
            WriteBinaryDelegate write = null;
            bool isContainMaxValue = true;
            switch (properties.AnymapType)
            {
                case AnymapType.Bitmap:
                    bytesNumForPixels = totalPixels / 8 + ((totalPixels % 8 == 0) ? (UInt32)0 : (UInt32)1);
                    type = (Byte)'4';
                    write = WriteP4;
                    isContainMaxValue = false;
                    break;
                case AnymapType.Graymap:
                    bytesNumForPixels = totalPixels * properties.BytesPerColor;
                    type = (Byte)'5';
                    write = WriteP5;
                    break;
                case AnymapType.Pixmap:
                    bytesNumForPixels = totalPixels * 3 * properties.BytesPerColor;
                    type = (Byte)'6';
                    write = WriteP6;
                    break;
            }
            UInt32 bytesForType = 3;
            UInt32 bytesForSize = (UInt32)properties.Width.ToString().Length + 1 + (UInt32)properties.Height.ToString().Length + 1;
            UInt32 bytesForMaxV = (UInt32)properties.MaxValue.ToString().Length + 1;
            UInt32 bytesNum = bytesForType + bytesForSize + (isContainMaxValue ? bytesForMaxV : 0) + bytesNumForPixels;
            byte[] anymap = new byte[bytesNum];
            MemoryStream stream = new MemoryStream(anymap);
            // Format
            stream.WriteByte((Byte)'P');
            stream.WriteByte(type);
            stream.WriteByte(0x0a);
            // Size
            byte[] widthArr = Encoding.ASCII.GetBytes(properties.Width.ToString());
            stream.Write(widthArr, 0, widthArr.Length);
            stream.WriteByte((Byte)' ');
            byte[] heightArr = Encoding.ASCII.GetBytes(properties.Height.ToString());
            stream.Write(heightArr, 0, heightArr.Length);
            stream.WriteByte(0x0a);
            // Maximum pixel value
            if (isContainMaxValue)
            {
                byte[] maxPixelValueArr = Encoding.ASCII.GetBytes(properties.MaxValue.ToString());
                stream.Write(maxPixelValueArr, 0, maxPixelValueArr.Length);
                stream.WriteByte(0x0a);
            }
            // Pixels
            write(image, stream, totalPixels, properties);
            return anymap;
        }

        private void WriteP1(byte[] image, StringWriter stringWriter, UInt32 totalPixels, AnymapProperties properties)
        {
            stringWriter.Write("{0} ", AnymapEncoder.grayscale8(image, 0) <= properties.MaxValue ? 1 : 0);
            for (UInt32 i = 1; i < totalPixels; ++i)
            {
                if (i % properties.Width == 0)
                {
                    stringWriter.WriteLine();
                }
                stringWriter.Write("{0} ", AnymapEncoder.grayscale8(image, i * 4) <= properties.MaxValue ? 1 : 0);
            }
        }

        private void WriteP2(byte[] image, StringWriter stringWriter, UInt32 totalPixels, AnymapProperties properties)
        {
            if (properties.BytesPerColor == 1)
            {
                Double ratio = (Double)properties.MaxValue / 255;
                stringWriter.Write((byte)(AnymapEncoder.grayscale8(image, 0) * ratio));
                for (UInt32 i = 1; i < totalPixels; ++i)
                {
                    if (i % properties.Width == 0)
                    {
                        stringWriter.WriteLine();
                    }
                    stringWriter.Write("{0} ", (byte)(AnymapEncoder.grayscale8(image, i * 4) * ratio));
                }
            }
            else
            {
                Double ratio = (Double)properties.MaxValue / 65535;
                byte[] pixel = new byte[2];
                UInt16[] temp = new UInt16[3];
                temp[0] = packU16(image[0], image[1]);
                temp[1] = packU16(image[2], image[3]);
                temp[2] = packU16(image[4], image[5]);
                stringWriter.Write("{0} ", AnymapEncoder.grayscale16(temp, 0) * ratio);
                for (UInt32 i = 1; i < totalPixels; ++i)
                {
                    if (i % properties.Width == 0)
                    {
                        stringWriter.WriteLine();
                    }
                    temp[0] = packU16(image[i * 8], image[i * 8 + 1]);
                    temp[1] = packU16(image[i * 8 + 2], image[i * 8 + 3]);
                    temp[2] = packU16(image[i * 8 + 4], image[i * 8 + 5]);
                    stringWriter.Write("{0} ", AnymapEncoder.grayscale16(temp, 0) * ratio);
                }
            }
        }

        private void WriteP3(byte[] image, StringWriter stringWriter, UInt32 totalPixels, AnymapProperties properties)
        {
            if (properties.BytesPerColor == 1)
            {
                Double ratio = (Double)properties.MaxValue / 255;
                // BGR -> RGB (first pixel)
                stringWriter.Write("{0} {1} {2} ",
                    (byte)(image[2] * ratio),
                    (byte)(image[1] * ratio),
                    (byte)(image[0] * ratio)
                );
                for (UInt32 i = 1; i < totalPixels; ++i)
                {
                    if (i % properties.Width == 0)
                    {
                        stringWriter.WriteLine();
                    }
                    // BGR -> RGB (other pixels)
                    stringWriter.Write("{0} {1} {2} ",
                        (byte)(image[i * 4 + 2] * ratio),
                        (byte)(image[i * 4 + 1] * ratio),
                        (byte)(image[i * 4] * ratio)
                    );
                }
            }
            else
            {
                Double ratio = (Double)properties.MaxValue / 65535;
                // BGR -> RGB (first pixel)
                stringWriter.WriteLine("{0} {1} {2} ",
                    (UInt16)(packU16(image[4], image[5]) * ratio), // R
                    (UInt16)(packU16(image[2], image[3]) * ratio), // G   
                    (UInt16)(packU16(image[8], image[1]) * ratio)  // B
                );
                for (UInt32 i = 1; i < totalPixels; ++i)
                {
                    if (i % properties.Width == 0)
                    {
                        stringWriter.WriteLine();
                    }
                    // BGR -> RGB (other pixels)
                    stringWriter.WriteLine("{0} {1} {2} ",
                        (UInt16)(packU16(image[i * 8 + 4], image[i * 8 + 5]) * ratio), // R
                        (UInt16)(packU16(image[i * 8 + 2], image[i * 8 + 3]) * ratio), // G   
                        (UInt16)(packU16(image[i * 8], image[i * 8 + 1]) * ratio)      // B
                    );
                }
            }
        }

        private void WriteP4(byte[] image, MemoryStream stream, UInt32 totalPixels, AnymapProperties properties)
        {
            int colmod = (int)(properties.Width) % 8;
            int col = 0;
            int num = 0;
            for (int i = 0; i < totalPixels;)
            {
                if (col == properties.Width - colmod)
                {
                    num = colmod == 0 ? 8 : colmod;
                    stream.WriteByte(
                        packBytePbm((int)properties.MaxValue, image, (UInt32)i, num)
                    );
                    col = 0;
                    i += num;
                }
                else
                {
                    stream.WriteByte(
                        packBytePbm((int)properties.MaxValue, image, (UInt32)i, totalPixels - i > 8 ? 8 : (int)(totalPixels - i))
                    );
                    col += 8;
                    i += 8;
                }
            }
        }

        private void WriteP5(byte[] image, MemoryStream stream, UInt32 totalPixels, AnymapProperties properties)
        {
            if (properties.BytesPerColor == 1)
            {
                Double ratio = (Double)properties.MaxValue / 255;
                for (UInt32 i = 0; i < totalPixels; ++i)
                {
                    stream.WriteByte((byte)(AnymapEncoder.grayscale8(image, i * 4) * ratio));
                }
            }
            else
            {
                Double ratio = (Double)properties.MaxValue / 65535;
                byte[] pixel = new byte[2];
                UInt16[] temp = new UInt16[3];
                for (UInt32 i = 0; i < totalPixels; ++i)
                {
                    temp[0] = packU16(image[i * 8], image[i * 8 + 1]);
                    temp[1] = packU16(image[i * 8 + 2], image[i * 8 + 3]);
                    temp[2] = packU16(image[i * 8 + 4], image[i * 8 + 5]);
                    
                    unpackBytes(
                        (UInt16)(AnymapEncoder.grayscale16(temp, 0) * ratio),
                        pixel[0], pixel[1]);
                    stream.Write(pixel, 0, 2);
                }
            }
        }

        private void WriteP6(byte[] image, MemoryStream stream, UInt32 totalPixels, AnymapProperties properties)
        {
            if (properties.BytesPerColor == 1)
            {
                Double ratio = (Double)properties.MaxValue / 255;
                byte[] pixel = new byte[3];
                for (UInt32 i = 0; i < totalPixels; ++i)
                {
                    // BGR -> RGB
                    pixel[0] = (byte)(image[i * 4 + 2] * ratio);
                    pixel[1] = (byte)(image[i * 4 + 1] * ratio);
                    pixel[2] = (byte)(image[i * 4] * ratio);
                    stream.Write(pixel, 0, 3);
                }
            }
            else
            {
                Double ratio = (Double)properties.MaxValue / 65535;
                byte[] pixel = new byte[6];
                for (UInt32 i = 0; i < totalPixels; ++i)
                {
                    // BGR -> RGB
                    // R
                    unpackBytes(
                        (UInt16)(packU16(image[i * 8 + 4], image[i * 8 + 5]) * ratio),
                        pixel[0], pixel[1]);
                    // G
                    unpackBytes(
                        (UInt16)(packU16(image[i * 8 + 2], image[i * 8 + 3]) * ratio),
                        pixel[2], pixel[3]);
                    // B
                    unpackBytes(
                        (UInt16)(packU16(image[i * 8], image[i * 8 + 1]) * ratio),
                        pixel[4], pixel[5]);
                    stream.Write(pixel, 0, 6);
                }
            }
        }

        private static Func<byte[], UInt32, byte> grayscale8 = delegate (byte[] arr, UInt32 pos)
        {
            return (byte)(arr[pos] * coeffB + arr[pos + 1] * coeffG + arr[pos + 2] * coeffR);
        };

        private static Func<UInt16[], UInt32, UInt16> grayscale16 = delegate (UInt16[] arr, UInt32 pos)
        {
            return (UInt16)(arr[pos] * coeffB + arr[pos + 1] * coeffG + arr[pos + 2] * coeffR);
        };

        private static Func<int, byte[], UInt32, int, byte> packBytePbm = delegate (int threshold, byte[] arr, UInt32 arrPos, int num)
        {
            byte b = 0x00;
            byte kk = (byte)(0x80 >> num);
            int shift = 0;
            for (byte k = 0x80; k != kk; k >>= 1)
            {
                if (AnymapEncoder.grayscale8(arr, (UInt32)((arrPos + shift) * 4)) <= threshold)
                {
                    b |= k;
                }
                ++shift;
            }
            return b;
        };

        private static Func<byte, byte, UInt16> packU16 = delegate (byte high, byte low)
        {
            return (UInt16)((UInt16)(high << 8) + (UInt16)low);
        };

        private static Action<UInt16, byte, byte> unpackBytes = delegate (UInt16 u16, byte high, byte low)
        {
            high = (byte)(u16 >> 8);
            low = (byte)(u16 & 0xFF);
        };
    }
}
