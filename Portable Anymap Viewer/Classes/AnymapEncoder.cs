using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Portable_Anymap_Viewer.Classes
{
    public class AnymapEncoder
    {
        private static byte[] newline = new byte[2] { 0x0d, 0x0a };
        private delegate void WriteDelegate(byte[] image, MemoryStream stream, UInt32 totalPixels, UInt32 bytesPerColor);

        public async Task<String> encodeText(BitmapDecoder imageDecoder, AnymapProperties properties)
        {
            String anymap = "";
            return anymap;
        }

        public async Task<byte[]> encodeBinary(BitmapDecoder imageDecoder, AnymapProperties properties)
        {
            //var pixelDataProvider = await imageDecoder.GetPixelDataAsync(
            //    properties.BytesPerColor == 1 ? BitmapPixelFormat.Bgra8 : BitmapPixelFormat.Rgba16,
            //    BitmapAlphaMode.Straight,
            //    new BitmapTransform(),
            //    ExifOrientationMode.RespectExifOrientation,
            //    ColorManagementMode.ColorManageToSRgb
            //    );
            var pixelDataProvider = await imageDecoder.GetPixelDataAsync();
            var image = pixelDataProvider.DetachPixelData();
            UInt32 totalPixels = imageDecoder.OrientedPixelWidth * imageDecoder.OrientedPixelHeight;
            UInt32 bytesNumForPixels = 0;
            Byte type = 0;
            WriteDelegate write = null;
            bool isContainMaxValue = true;
            switch (properties.AnymapType)
            {
                case AnymapType.Bitmap:
                    bytesNumForPixels = totalPixels / 8;
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
            UInt32 bytesForType = 4;
            UInt32 bytesForSize = (UInt32)properties.Width.ToString().Length + 1 + (UInt32)properties.Height.ToString().Length + 2;
            UInt32 bytesForMaxV = (UInt32)properties.MaxValue.ToString().Length + 2;
            UInt32 bytesNum = bytesForType + bytesForSize + bytesForMaxV + bytesNumForPixels;
            byte[] anymap = new byte[bytesNum];
            MemoryStream stream = new MemoryStream(anymap);
            // Format
            stream.WriteByte((Byte)'P');
            stream.WriteByte(type);
            stream.Write(AnymapEncoder.newline, 0, 2);
            // Size
            byte[] widthArr = Encoding.ASCII.GetBytes(properties.Width.ToString());
            stream.Write(widthArr, 0, widthArr.Length);
            stream.WriteByte((Byte)' ');
            byte[] heightArr = Encoding.ASCII.GetBytes(properties.Height.ToString());
            stream.Write(heightArr, 0, heightArr.Length);
            stream.Write(AnymapEncoder.newline, 0, 2);
            // Maximum pixel value
            if (isContainMaxValue)
            {
                byte[] maxPixelValueArr = Encoding.ASCII.GetBytes(properties.MaxValue.ToString());
                stream.Write(maxPixelValueArr, 0, maxPixelValueArr.Length);
                stream.Write(AnymapEncoder.newline, 0, 2);
            }
            // Pixels
            write(image, stream, totalPixels, properties.BytesPerColor);
            return anymap;
        }

        private void WriteP4(byte[] image, MemoryStream stream, UInt32 totalPixels, UInt32 bytesPerColor)
        {
            for (UInt32 i = 0; i < totalPixels; ++i)
            {

            }
        }

        private void WriteP5(byte[] image, MemoryStream stream, UInt32 totalPixels, UInt32 bytesPerColor)
        {
            for (UInt32 i = 0; i < totalPixels; ++i)
            {

            }
        }

        private void WriteP6(byte[] image, MemoryStream stream, UInt32 totalPixels, UInt32 bytesPerColor)
        {
            if (bytesPerColor == 1)
            {
                byte[] pixel = new byte[3];
                for (UInt32 i = 0; i < totalPixels; ++i)
                {
                    pixel[0] = image[i * 4];
                    pixel[1] = image[i * 4 + 1];
                    pixel[2] = image[i * 4 + 2];
                    stream.Write(pixel, 0, 3);
                }
            }
            else
            {
                byte[] pixel = new byte[6];
                for (UInt32 i = 0; i < totalPixels; ++i)
                {
                    pixel[0] = image[i * 8];
                    pixel[1] = image[i * 8 + 1];
                    pixel[2] = image[i * 8 + 2];
                    pixel[3] = image[i * 8 + 3];
                    pixel[4] = image[i * 8 + 4];
                    pixel[5] = image[i * 8 + 5];
                    stream.Write(pixel, 0, 3);
                }
            }
        }
    }
}
