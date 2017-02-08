using System;

namespace Portable_Anymap_Viewer.Models
{
    public class DecodeResult
    {
        public Int32 Type { get; set; }
        public Int32 Width { get; set; }
        public Int32 Height { get; set; }
        public byte[] Bytes { get; set; }
        public String Filename { get; set; }
        public Single CurrentZoom { get; set; }
        public Boolean DoubleBytesPerColor { get; set; }
    }
}
