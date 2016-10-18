using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace Portable_Anymap_Viewer.Models
{
    public class EditFileParams
    {
        public byte[] Bytes { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Type { get; set; }
        public StorageFile File { get; set; }
        public EditFileSaveMode SaveMode { get; set; }
    }

    public enum EditFileSaveMode
    {
        SaveCopy = 1,
        Save = 2,
        SaveAs = 4
    }
}
