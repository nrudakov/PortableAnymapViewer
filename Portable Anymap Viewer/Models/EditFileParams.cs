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
        public Image image { get; set; }
        public int type { get; set; }
        public StorageFile file { get; set; }
    }
}
