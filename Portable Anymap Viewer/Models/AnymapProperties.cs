using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portable_Anymap_Viewer.Models
{
    public class AnymapProperties
    {
        public UInt32 Width { get; set; }
        public UInt32 Height { get; set; }
        public UInt32 MaxValue { get; set; }
        public UInt32 BytesPerColor { get; set; }
        public UInt32 StreamPosition { get; set; }
    }
}
