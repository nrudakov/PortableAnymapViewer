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
        private delegate byte PackBytePbmDelegate(byte[] image, UInt32 position, int packSize, int threshold); 
        
        public async Task<String> EncodeText(BitmapDecoder imageDecoder, AnymapProperties properties)
        {
            var pixelDataProvider = await imageDecoder.GetPixelDataAsync();
            var image = pixelDataProvider.DetachPixelData();    
            properties.SourcePixelFormat = imageDecoder.BitmapPixelFormat;
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

        public async Task<byte[]> EncodeBinary(BitmapDecoder imageDecoder, AnymapProperties properties)
        {
            var pixelDataProvider = await imageDecoder.GetPixelDataAsync();
            var image = pixelDataProvider.DetachPixelData();
            properties.SourcePixelFormat = imageDecoder.BitmapPixelFormat;
            UInt32 totalPixels = imageDecoder.OrientedPixelWidth * imageDecoder.OrientedPixelHeight;
            UInt32 bytesNumForPixels = 0;
            Byte type = 0;
            WriteBinaryDelegate write = null;
            bool isContainMaxValue = true;
            switch (properties.AnymapType)
            {
                case AnymapType.Bitmap:
                    bytesNumForPixels = (properties.Width / 8 + (properties.Width % 8 == 0 ? (UInt32)0 : (UInt32)1)) * properties.Height;
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
            UInt32 i = 0;
            switch (properties.SourcePixelFormat)
            {
                case BitmapPixelFormat.Bgra8:
                    stringWriter.Write("{0} ", AnymapEncoder.GrayscaleBgra8(image, 0) <= properties.Threshold8 ? 1 : 0);
                    for (i = 1; i < totalPixels; ++i)
                    {
                        if (i % properties.Width == 0)
                        {
                            stringWriter.WriteLine();
                        }
                        stringWriter.Write("{0} ", AnymapEncoder.GrayscaleBgra8(image, i * 4) <= properties.Threshold8 ? 1 : 0);
                    }
                    break;
                case BitmapPixelFormat.Gray16:
                    UInt16 pixel = PackU16(image[0], image[1]);
                    stringWriter.Write("{0} ", pixel <= properties.Threshold16 ? 1 : 0);
                    for (i = 1; i < totalPixels; ++i)
                    {
                        if (i % properties.Width == 0)
                        {
                            stringWriter.WriteLine();
                        }
                        pixel = PackU16(image[i * 2], image[i * 2 + 1]);
                        stringWriter.Write("{0} ", pixel <= properties.Threshold16 ? 1 : 0);
                    }
                    break;
                case BitmapPixelFormat.Gray8:
                    stringWriter.Write("{0} ", image[0] <= properties.Threshold8 ? 1 : 0);
                    for (i = 1; i < totalPixels; ++i)
                    {
                        if (i % properties.Width == 0)
                        {
                            stringWriter.WriteLine();
                        }
                        stringWriter.Write("{0} ", image[i] <= properties.Threshold8 ? 1 : 0);
                    }
                    break;
                case BitmapPixelFormat.Rgba16:
                    UInt16[] temp = new UInt16[3];
                    temp[0] = PackU16(image[0], image[1]);
                    temp[1] = PackU16(image[2], image[3]);
                    temp[2] = PackU16(image[4], image[5]);
                    stringWriter.Write("{0} ", AnymapEncoder.GrayscaleRgba16(temp, 0) <= properties.Threshold16 ? 1 : 0);
                    for (i = 1; i < totalPixels; ++i)
                    {
                        if (i % properties.Width == 0)
                        {
                            stringWriter.WriteLine();
                        }
                        temp[0] = PackU16(image[i * 8], image[i * 8 + 1]);
                        temp[1] = PackU16(image[i * 8 + 2], image[i * 8 + 3]);
                        temp[2] = PackU16(image[i * 8 + 4], image[i * 8 + 5]);
                        stringWriter.Write("{0} ", AnymapEncoder.GrayscaleRgba16(temp, i * 8) <= properties.Threshold16 ? 1 : 0);
                    }
                    break;
                case BitmapPixelFormat.Rgba8:
                    stringWriter.Write("{0} ", AnymapEncoder.GrayscaleRgba8(image, 0) <= properties.Threshold8 ? 1 : 0);
                    for (i = 1; i < totalPixels; ++i)
                    {
                        if (i % properties.Width == 0)
                        {
                            stringWriter.WriteLine();
                        }
                        stringWriter.Write("{0} ", AnymapEncoder.GrayscaleRgba8(image, i * 4) <= properties.Threshold8 ? 1 : 0);
                    }
                    break;
                case BitmapPixelFormat.Nv12:
                case BitmapPixelFormat.Yuy2:
                case BitmapPixelFormat.Unknown:
                    break;
            }
        }

        private void WriteP2(byte[] image, StringWriter stringWriter, UInt32 totalPixels, AnymapProperties properties)
        {
            Double ratio = 1.0;
            switch (properties.SourcePixelFormat)
            {
                case BitmapPixelFormat.Bgra8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        stringWriter.Write("{0} ", (byte)(AnymapEncoder.GrayscaleBgra8(image, 0) * ratio));
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} ", (byte)(AnymapEncoder.GrayscaleBgra8(image, i * 4) * ratio));
                        }
                    }
                    else
                    {
                        stringWriter.Write("{0} ", (UInt16)(AnymapEncoder.GrayscaleBgra8(image, 0) * ratio));
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} ", (UInt16)(AnymapEncoder.GrayscaleBgra8(image, i * 4) * ratio));
                        }
                    }
                    break;
                case BitmapPixelFormat.Gray16:
                    if (properties.BytesPerColor == 1)
                    {
                        ratio = (Double)properties.MaxValue / 65280; // 255 / 256
                        stringWriter.Write("{0} ", (byte)(PackU16(image[0], image[1])) * ratio);
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} ", (byte)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio));
                        }
                    }
                    else
                    {
                        ratio = (Double)properties.MaxValue / 65535;
                        stringWriter.Write("{0} ", (UInt16)(PackU16(image[0], image[1]) * ratio));
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} ", (UInt16)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio));
                        }
                    }
                    break;
                case BitmapPixelFormat.Gray8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        stringWriter.Write("{0} ", (byte)(image[0] * ratio));
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} ", (byte)(image[i] * ratio));
                        }
                    }
                    else
                    {
                        stringWriter.Write("{0} ", (UInt16)(image[0] * ratio));
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} ", (UInt16)(image[i] * ratio));
                        }
                    }
                    break;
                case BitmapPixelFormat.Rgba16:
                    if (properties.BytesPerColor == 1)
                    {
                        ratio = (Double)properties.MaxValue / 65280; //  255 / 256;
                        byte[] pixel = new byte[2];
                        UInt16[] temp = new UInt16[3];
                        temp[0] = PackU16(image[0], image[1]);
                        temp[1] = PackU16(image[2], image[3]);
                        temp[2] = PackU16(image[4], image[5]);
                        stringWriter.Write("{0} ", AnymapEncoder.GrayscaleRgba16(temp, 0) * ratio);
                        for(UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            temp[0] = PackU16(image[i * 8], image[i * 8 + 1]);
                            temp[1] = PackU16(image[i * 8 + 2], image[i * 8 + 3]);
                            temp[2] = PackU16(image[i * 8 + 4], image[i * 8 + 5]);
                            stringWriter.Write("{0} ", (byte)(AnymapEncoder.GrayscaleRgba16(temp, i * 8) * ratio));
                        }
                    }
                    else
                    {
                        ratio = (Double)properties.MaxValue / 65535;
                        byte[] pixel = new byte[2];
                        UInt16[] temp = new UInt16[3];
                        temp[0] = PackU16(image[0], image[1]);
                        temp[1] = PackU16(image[2], image[3]);
                        temp[2] = PackU16(image[4], image[5]);
                        stringWriter.Write("{0} ", AnymapEncoder.GrayscaleRgba16(temp, 0) * ratio);
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            temp[0] = PackU16(image[i * 8], image[i * 8 + 1]);
                            temp[1] = PackU16(image[i * 8 + 2], image[i * 8 + 3]);
                            temp[2] = PackU16(image[i * 8 + 4], image[i * 8 + 5]);
                            stringWriter.Write("{0} ", AnymapEncoder.GrayscaleRgba16(temp, i * 8) * ratio);
                        }
                    }
                    break;
                case BitmapPixelFormat.Rgba8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        stringWriter.Write("{0} ", (byte)(AnymapEncoder.GrayscaleRgba8(image, 0) * ratio));
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} ", (byte)(AnymapEncoder.GrayscaleRgba8(image, i * 4) * ratio));
                        }
                    }
                    else
                    {
                        stringWriter.Write("{0} ", (byte)(AnymapEncoder.GrayscaleRgba8(image, 0) * ratio));
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} ", (byte)(AnymapEncoder.GrayscaleRgba8(image, i * 4) * ratio));
                        }
                    }
                    break;
                case BitmapPixelFormat.Nv12:
                case BitmapPixelFormat.Yuy2:
                case BitmapPixelFormat.Unknown:
                    break;
            }
        }

        private void WriteP3(byte[] image, StringWriter stringWriter, UInt32 totalPixels, AnymapProperties properties)
        {
            Double ratio = 1.0;
            switch (properties.SourcePixelFormat)
            {
                case BitmapPixelFormat.Bgra8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
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
                            stringWriter.Write("{0} {1} {2} ",
                                (byte)(image[i * 4 + 2] * ratio),
                                (byte)(image[i * 4 + 1] * ratio),
                                (byte)(image[i * 4] * ratio)
                            );
                        }
                    }
                    else
                    {
                        stringWriter.Write("{0} {1} {2} ",
                            (UInt16)(image[2] * ratio),
                            (UInt16)(image[1] * ratio),
                            (UInt16)(image[0] * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {1} {2} ",
                                (UInt16)(image[i * 4 + 2] * ratio),
                                (UInt16)(image[i * 4 + 1] * ratio),
                                (UInt16)(image[i * 4 + 0] * ratio)
                            );
                        }
                    }
                    break;
                case BitmapPixelFormat.Gray16:
                    if (properties.BytesPerColor == 1)
                    {
                        ratio = (Double)properties.MaxValue / 65280; // 255 / 256
                        stringWriter.Write("{0} {0} {0} ",
                            (byte)(PackU16(image[0], image[1]) * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {0} {0} ",
                                (byte)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio)
                            );
                        }
                    }
                    else
                    {
                        ratio = (Double)properties.MaxValue / 65535;
                        stringWriter.Write("{0} {0} {0} ",
                            (UInt16)(PackU16(image[0], image[1]) * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {0} {0} ",
                                (UInt16)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio)
                            );
                        }
                    }
                    break;
                case BitmapPixelFormat.Gray8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        stringWriter.Write("{0} {0} {0} ",
                            (byte)(image[0] * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {0} {0} ",
                                (byte)(image[i] * ratio)
                            );
                        }
                    }
                    else
                    {
                        stringWriter.Write("{0} {0} {0} ",
                            (UInt16)(image[0] * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {0} {0} ",
                                (UInt16)(image[i] * ratio)
                            );
                        }
                    }
                    break;
                case BitmapPixelFormat.Rgba16:
                    if (properties.BytesPerColor == 1)
                    {
                        ratio = (Double)properties.MaxValue / 65280; // 255 / 256
                        stringWriter.Write("{0} {1} {2} ",
                            (byte)(PackU16(image[0], image[1]) * ratio),
                            (byte)(PackU16(image[2], image[3]) * ratio),
                            (byte)(PackU16(image[4], image[5]) * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {1} {2} ",
                                (byte)(PackU16(image[i * 8 + 0], image[i * 8 + 1]) * ratio),
                                (byte)(PackU16(image[i * 8 + 2], image[i * 8 + 3]) * ratio),
                                (byte)(PackU16(image[i * 8 + 4], image[i * 8 + 5]) * ratio)
                            );
                        }
                    }
                    else
                    {
                        ratio = (Double)properties.MaxValue / 65535;
                        stringWriter.Write("{0} {1} {2} ",
                            (UInt16)(PackU16(image[0], image[1]) * ratio),
                            (UInt16)(PackU16(image[2], image[3]) * ratio),
                            (UInt16)(PackU16(image[4], image[5]) * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {1} {2} ",
                                (UInt16)(PackU16(image[i * 8 + 0], image[i * 8 + 1]) * ratio),
                                (UInt16)(PackU16(image[i * 8 + 2], image[i * 8 + 3]) * ratio),
                                (UInt16)(PackU16(image[i * 8 + 4], image[i * 8 + 5]) * ratio)
                            );
                        }
                    }
                    break;
                case BitmapPixelFormat.Rgba8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        stringWriter.Write("{0} {1} {2} ",
                            (byte)(image[0] * ratio),
                            (byte)(image[1] * ratio),
                            (byte)(image[2] * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {1} {2} ",
                                (byte)(image[i * 4] * ratio),
                                (byte)(image[i * 4 + 1] * ratio),
                                (byte)(image[i * 4 + 2] * ratio)
                            );
                        }
                    }
                    else
                    {
                        stringWriter.Write("{0} {1} {2} ",
                            (UInt16)(image[0] * ratio),
                            (UInt16)(image[1] * ratio),
                            (UInt16)(image[2] * ratio)
                        );
                        for (UInt32 i = 1; i < totalPixels; ++i)
                        {
                            if (i % properties.Width == 0)
                            {
                                stringWriter.WriteLine();
                            }
                            stringWriter.Write("{0} {1} {2} ",
                                (UInt16)(image[i * 4] * ratio),
                                (UInt16)(image[i * 4 + 1] * ratio),
                                (UInt16)(image[i * 4 + 2] * ratio)
                            );
                        }
                    }
                    break;
                case BitmapPixelFormat.Nv12:
                case BitmapPixelFormat.Yuy2:
                case BitmapPixelFormat.Unknown:
                    break;
            }
        }

        private void WriteP4(byte[] image, MemoryStream stream, UInt32 totalPixels, AnymapProperties properties)
        {
            PackBytePbmDelegate PackBytes = null;
            int threshold = 127;
            switch (properties.SourcePixelFormat)
            {
                case BitmapPixelFormat.Bgra8:
                    PackBytes = PackBytePbmBgra8;
                    threshold = properties.Threshold8;
                    break;
                case BitmapPixelFormat.Gray16:
                    PackBytes = PackBytePbmGray16;
                    threshold = properties.Threshold16;
                    break;
                case BitmapPixelFormat.Gray8:
                    PackBytes = PackBytePbmGray8;
                    threshold = properties.Threshold8;
                    break;
                case BitmapPixelFormat.Rgba16:
                    PackBytes = PackBytePbmRgba16;
                    threshold = properties.Threshold16;
                    break;
                case BitmapPixelFormat.Rgba8:
                    PackBytes = PackBytePbmRgba8;
                    threshold = properties.Threshold8;
                    break;
                case BitmapPixelFormat.Nv12:
                case BitmapPixelFormat.Yuy2:
                case BitmapPixelFormat.Unknown:
                    break;
            }
            int colmod = (int)properties.Width % 8;
            int col = 0;
            int packSize = 0;
            for (UInt32 i = 0; i < totalPixels;)
            {
                if (col == properties.Width - colmod)
                {
                    // Last pack in a row
                    packSize = colmod == 0 ? 8 : colmod;
                    stream.WriteByte(
                        PackBytes(image, i, packSize, threshold)
                    );
                    col = 0;
                    i += (UInt32)packSize;
                }
                else
                {
                    stream.WriteByte(
                        PackBytes(image, i, totalPixels - i > 8 ? 8 : (int)(totalPixels - i), threshold)
                    );
                    col += 8;
                    i += 8;
                }
            }
        }

        private void WriteP5(byte[] image, MemoryStream stream, UInt32 totalPixels, AnymapProperties properties)
        {
            Double ratio = 1.0;
            switch (properties.SourcePixelFormat)
            {
                case BitmapPixelFormat.Bgra8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            stream.WriteByte((byte)(GrayscaleBgra8(image, i * 4) * ratio));
                        }
                    }
                    else
                    {
                        byte[] pixel = new byte[2];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(GrayscaleBgra8(image, i * 4) * ratio), ref pixel[0], ref pixel[1]);
                            stream.Write(pixel, 0, 2);
                        }
                    }
                    break;
                case BitmapPixelFormat.Gray16:
                    if (properties.BytesPerColor == 1)
                    {
                        ratio = (Double)properties.MaxValue / 65280;
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            stream.WriteByte((byte)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio));
                        }
                    }
                    else
                    {
                        ratio = (Double)properties.MaxValue / 65535;
                        byte[] pixel = new byte[2];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio), ref pixel[0], ref pixel[1]);
                            stream.Write(pixel, 0, 2);
                        }
                    }
                    break;
                case BitmapPixelFormat.Gray8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            stream.WriteByte((byte)(image[i] * ratio));
                        }
                    }
                    else
                    {
                        byte[] pixel = new byte[2];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio), ref pixel[0], ref pixel[1]);
                            stream.Write(pixel, 0, 2);
                        }
                    }
                    break;
                case BitmapPixelFormat.Rgba16:
                    if (properties.BytesPerColor == 1)
                    {
                        ratio = (Double)properties.MaxValue / 65280;
                        UInt16[] sourcePixel = new UInt16[3];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            sourcePixel[0] = PackU16(image[i * 8 + 0], image[i * 8 + 1]);
                            sourcePixel[1] = PackU16(image[i * 8 + 2], image[i * 8 + 3]);
                            sourcePixel[2] = PackU16(image[i * 8 + 4], image[i * 8 + 5]);
                            stream.WriteByte((byte)(GrayscaleRgba16(sourcePixel, 0) * ratio));
                        }
                    }
                    else
                    {
                        ratio = (Double)properties.MaxValue / 65535;
                        UInt16[] sourcePixel = new UInt16[3];
                        byte[] pixel = new byte[2];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            sourcePixel[0] = PackU16(image[i * 8 + 0], image[i * 8 + 1]);
                            sourcePixel[1] = PackU16(image[i * 8 + 2], image[i * 8 + 3]);
                            sourcePixel[2] = PackU16(image[i * 8 + 4], image[i * 8 + 5]);
                            UnpackU16((UInt16)(GrayscaleRgba16(sourcePixel, 0) * ratio), ref pixel[0], ref pixel[1]);
                            stream.Write(pixel, 0, 2);
                        }
                    }
                    break;
                case BitmapPixelFormat.Rgba8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            stream.WriteByte((byte)(GrayscaleRgba8(image, i * 4) * ratio));
                        }
                    }
                    else
                    {
                        byte[] pixel = new byte[2];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(GrayscaleRgba8(image, i * 4) * ratio),ref pixel[0], ref pixel[1]);
                            stream.Write(pixel, 0, 2);
                        }
                    }
                    break;
                case BitmapPixelFormat.Nv12:
                case BitmapPixelFormat.Yuy2:
                case BitmapPixelFormat.Unknown:
                    break;
            }
        }
        
        private void WriteP6(byte[] image, MemoryStream stream, UInt32 totalPixels, AnymapProperties properties)
        {
            Double ratio = 1.0;
            switch (properties.SourcePixelFormat)
            {
                case BitmapPixelFormat.Bgra8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        byte[] pixel = new byte[3];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            pixel[0] = (byte)(image[i * 4 + 2] * ratio);
                            pixel[1] = (byte)(image[i * 4 + 1] * ratio);
                            pixel[2] = (byte)(image[i * 4] * ratio);
                            stream.Write(pixel, 0, 3);
                        }
                    }
                    else
                    {
                        byte[] pixel = new byte[6];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(PackU16(image[i * 8 + 4], image[i * 8 + 5]) * ratio), ref pixel[0], ref pixel[1]);
                            UnpackU16((UInt16)(PackU16(image[i * 8 + 2], image[i * 8 + 3]) * ratio), ref pixel[2], ref pixel[3]);
                            UnpackU16((UInt16)(PackU16(image[i * 8 + 0], image[i * 8 + 1]) * ratio), ref pixel[4], ref pixel[5]);
                            stream.Write(pixel, 0, 6);
                        }
                    }
                    break;
                case BitmapPixelFormat.Gray16:
                    if (properties.BytesPerColor == 1)
                    {
                        ratio = (Double)properties.MaxValue / 65280;
                        byte[] pixel = new byte[3];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            pixel[2] = pixel[1] = pixel[0] = (byte)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio);
                            stream.Write(pixel, 0, 3);
                        }
                    }
                    else
                    {
                        ratio = (Double)properties.MaxValue / 65535;
                        byte[] pixel = new byte[6];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(PackU16(image[i * 2], image[i * 2 + 1]) * ratio), ref pixel[0], ref pixel[1]);
                            pixel[4] = pixel[2] = pixel[0];
                            pixel[5] = pixel[3] = pixel[1];
                            stream.Write(pixel, 0, 6);
                        }
                    }
                    break;
                case BitmapPixelFormat.Gray8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        byte[] pixel = new byte[3];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            pixel[2] = pixel[1] = pixel[0] = (byte)(image[i] * ratio);
                            stream.Write(pixel, 0, 3);
                        }
                    }
                    else
                    {
                        byte[] pixel = new byte[6];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(image[i] * ratio), ref pixel[0], ref pixel[1]);
                            pixel[4] = pixel[2] = pixel[0];
                            pixel[5] = pixel[3] = pixel[1];
                            stream.Write(pixel, 0, 6);
                        }
                    }
                    break;
                case BitmapPixelFormat.Rgba16:
                    if (properties.BytesPerColor == 1)
                    {
                        ratio = (Double)properties.MaxValue / 65280;
                        byte[] pixel = new byte[3];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            pixel[0] = (byte)(PackU16(image[i * 8 + 0], image[i * 8 + 1]) * ratio);
                            pixel[1] = (byte)(PackU16(image[i * 8 + 2], image[i * 8 + 3]) * ratio);
                            pixel[2] = (byte)(PackU16(image[i * 8 + 4], image[i * 8 + 5]) * ratio);
                            stream.Write(pixel, 0, 3);
                        }
                    }
                    else
                    {
                        ratio = (Double)properties.MaxValue / 65535;
                        byte[] pixel = new byte[6];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(PackU16(image[i * 8 + 0], image[i * 8 + 1]) * ratio), ref pixel[0], ref pixel[1]);
                            UnpackU16((UInt16)(PackU16(image[i * 8 + 2], image[i * 8 + 3]) * ratio), ref pixel[2], ref pixel[3]);
                            UnpackU16((UInt16)(PackU16(image[i * 8 + 4], image[i * 8 + 5]) * ratio), ref pixel[4], ref pixel[5]);
                            stream.Write(pixel, 0, 6);
                        }
                    }
                    break;
                case BitmapPixelFormat.Rgba8:
                    ratio = (Double)properties.MaxValue / 255;
                    if (properties.BytesPerColor == 1)
                    {
                        byte[] pixel = new byte[3];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            pixel[0] = (byte)(image[i * 4 + 0] * ratio);
                            pixel[1] = (byte)(image[i * 4 + 1] * ratio);
                            pixel[2] = (byte)(image[i * 4 + 2] * ratio);
                            stream.Write(pixel, 0, 3);
                        }
                    }
                    else
                    {
                        byte[] pixel = new byte[6];
                        for (UInt32 i = 0; i < totalPixels; ++i)
                        {
                            UnpackU16((UInt16)(image[i * 4 + 0] * ratio), ref pixel[0], ref pixel[1]);
                            UnpackU16((UInt16)(image[i * 4 + 1] * ratio), ref pixel[2], ref pixel[3]);
                            UnpackU16((UInt16)(image[i * 4 + 2] * ratio), ref pixel[4], ref pixel[5]);
                            stream.Write(pixel, 0, 6);
                        }
                    }
                    break;
                case BitmapPixelFormat.Nv12:
                case BitmapPixelFormat.Yuy2:
                case BitmapPixelFormat.Unknown:
                    break;
            }
        }

        private static Func<byte[], UInt32, byte> GrayscaleBgra8 = delegate (byte[] arr, UInt32 pos)
        {
            return (byte)(arr[pos] * coeffB + arr[pos + 1] * coeffG + arr[pos + 2] * coeffR);
        };

        private static Func<byte[], UInt32, byte> GrayscaleRgba8 = delegate (byte[] arr, UInt32 pos)
        {
            return (byte)(arr[pos] * coeffR + arr[pos + 1] * coeffG + arr[pos + 2] * coeffB);
        };

        private static Func<UInt16[], UInt32, UInt16> GrayscaleRgba16 = delegate (UInt16[] arr, UInt32 pos)
        {
            return (UInt16)(arr[pos] * coeffR + arr[pos + 1] * coeffG + arr[pos + 2] * coeffB);
        };

        private byte PackBytePbmBgra8(byte[] image, UInt32 arrPos, int packSize, int threshold)
        {
            byte b = 0x00;
            byte kk = (byte)(0x80 >> packSize);
            int shift = 0;
            for (byte k = 0x80; k != kk; k >>= 1)
            {
                if (AnymapEncoder.GrayscaleBgra8(image, (UInt32)((arrPos + shift) * 4)) <= threshold)
                {
                    b |= k;
                }
                ++shift;
            }
            return b;
        }
        
        private byte PackBytePbmGray16(byte[] image, UInt32 arrPos, int packSize, int threshold)
        {
            byte b = 0x00;
            byte kk = (byte)(0x80 >> packSize);
            int shift = 0;
            for (byte k = 0x80; k != kk; k >>= 1)
            {
                if (PackU16(image[(arrPos + shift) * 2], image[(arrPos + shift) * 2 + 1]) <= threshold)
                {
                    b |= k;
                }
                ++shift;
            }
            return b;
        }

        private byte PackBytePbmGray8(byte[] image, UInt32 arrPos, int packSize, int threshold)
        {
            byte b = 0x00;
            byte kk = (byte)(0x80 >> packSize);
            int shift = 0;
            for (byte k = 0x80; k != kk; k >>= 1)
            {
                if (image[arrPos + shift] <= threshold)
                {
                    b |= k;
                }
                ++shift;
            }
            return b;
        }

        private byte PackBytePbmRgba8(byte[] image, UInt32 arrPos, int packSize, int threshold)
        {
            byte b = 0x00;
            byte kk = (byte)(0x80 >> packSize);
            int shift = 0;
            for (byte k = 0x80; k != kk; k >>= 1)
            {
                if (GrayscaleRgba8(image, (UInt32)((arrPos + shift) * 4)) <= threshold)
                {
                    b |= k;
                }
                ++shift;
            }
            return b;
        }

        private byte PackBytePbmRgba16(byte[] image, UInt32 arrPos, int packSize, int threshold)
        {
            byte b = 0x00;
            byte kk = (byte)(0x80 >> packSize);
            int shift = 0;
            UInt16[] sourcePixel = new UInt16[3];
            for (byte k = 0x80; k != kk; k >>= 1)
            {
                sourcePixel[0] = PackU16(image[(arrPos + shift) * 8 + 0], image[(arrPos + shift) * 8 + 1]);
                sourcePixel[1] = PackU16(image[(arrPos + shift) * 8 + 2], image[(arrPos + shift) * 8 + 3]);
                sourcePixel[2] = PackU16(image[(arrPos + shift) * 8 + 4], image[(arrPos + shift) * 8 + 5]);
                if (GrayscaleRgba16(sourcePixel, 0) <= threshold)
                {
                    b |= k;
                }
                ++shift;
            }
            return b;
        }

        private UInt16 PackU16 (byte high, byte low)
        {
            return (UInt16)((UInt16)(high << 8) + low);
        }

        private void UnpackU16(UInt16 u16, ref byte high, ref byte low)
        {
            high = (byte)(u16 >> 8);
            low = (byte)(u16 & 0xFF);
        }
    }
}
