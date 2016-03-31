using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Portable_Anymap_Viewer.Models
{
    public class DecodeResult
    {
        public Int32 Width { get; set; }
        public Int32 Height { get; set; }
        public byte[] Bytes { get; set; }
    }
}
