﻿using System;
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
    }
}