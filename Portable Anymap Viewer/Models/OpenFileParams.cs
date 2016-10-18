using Portable_Anymap_Viewer.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Portable_Anymap_Viewer.Models
{
    public class OpenFileParams
    {
        public ExplorerItem ClickedFile { get; set; }
        public IReadOnlyList<StorageFile> FileList { get; set; }
        public StorageFolder Folder { get; set; }
        public String NavigateBackFilename { get; set; }
    }
}
