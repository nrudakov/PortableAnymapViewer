using System;
using System.Collections.Generic;

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
        public List<List<HistogramValue>> HistogramValues { get; set; }
        public String Md5;
        public String Sha1;
        public String Sha256;
        public String Sha384;
        public String Sha512;
    }
}
