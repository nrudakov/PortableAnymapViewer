using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;

namespace Portable_Anymap_Viewer.Models
{
    public class AnymapProperties
    {
        public UInt32 Width { get; set; }
        public UInt32 Height { get; set; }
        public UInt32 MaxValue { get; set; }
        public Byte Threshold8 { get; set; }
        public UInt16 Threshold16 { get; set; }
        public UInt32 BytesPerColor { get; set; }
        public UInt32 StreamPosition { get; set; }
        public AnymapType AnymapType { get; set; }
        public BitmapPixelFormat SourcePixelFormat { get; set; }
    }

    public enum AnymapType
    {
        Bitmap, Graymap, Pixmap
    }
}
