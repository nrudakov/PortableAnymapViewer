using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Portable_Anymap_Viewer
{
    public sealed partial class FolderItemControl : UserControl
    {
        public FolderItemControl(StorageFolder folder)
        {
            this.InitializeComponent();
            _folder = folder;
            initializingByFolder();
        }

        public StorageFolder getStorageFolder()
        {
            return _folder;
        }

        private async void initializingByFolder()
        {
            String t = StorageApplicationPermissions.FutureAccessList.Add(_folder);
            StorageItemThumbnail thumbnail = await _folder.GetThumbnailAsync(ThumbnailMode.SingleItem);
            BitmapImage thumbnailBitmap = new BitmapImage();
            thumbnailBitmap.SetSource(thumbnail);
            thumbnailControl.Source = thumbnailBitmap;
            foldernameControl.Text = _folder.DisplayName;
            folderPathControl.Text = _folder.Path;
        }

        private readonly StorageFolder _folder;
    }
}
