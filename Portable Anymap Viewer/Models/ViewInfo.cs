using System;

namespace Portable_Anymap_Viewer.Models
{
    public class ViewInfo
    {
        public Int32 Type { get; set; }
        public Int32 Width { get; set; }
        public Int32 Height { get; set; }
        public Double CurrentZoom { get; set; }
        public String Filename { get; set; }
    }
}
