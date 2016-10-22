using Portable_Anymap_Viewer.Controls;
using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Shows the content of the folder
    /// </summary>
    public sealed partial class ExplorerPage : Page
    {
        public ExplorerPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            ExplorerItem paramFolder = e.Parameter as ExplorerItem;
            var loader = new ResourceLoader();
            this.CreateFolderPopupName.Text = loader.GetString("NewFolderDefaultName");
            this.CreateFilePopupName.Text = loader.GetString("NewFileDefaultName");
            this.ExplorerHeaderTitle.Text = paramFolder.Path;
            this.currentStorageFolder = await StorageFolder.GetFolderFromPathAsync(paramFolder.Path);
            this.GetItemsInCurentFolder();
        }

        private async void GetItemsInCurentFolder()
        {
            this.ExplorerItemList.Items.Clear();

            IReadOnlyList<StorageFolder> folderList = await this.currentStorageFolder.GetFoldersAsync();

            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".pbm");
            fileTypeFilter.Add(".pgm");
            fileTypeFilter.Add(".ppm");
            QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypeFilter);
            StorageFileQueryResult results = this.currentStorageFolder.CreateFileQueryWithOptions(queryOptions);
            this.FileList = await results.GetFilesAsync();

            foreach (StorageFolder storageFolder in folderList)
            {
                StorageItemThumbnail thumbnail = await storageFolder.GetThumbnailAsync(ThumbnailMode.SingleItem);

                BitmapImage thumbnailBitmap = new BitmapImage();
                thumbnailBitmap.SetSource(thumbnail);

                ExplorerItem explorerItem = new ExplorerItem();
                explorerItem.Thumbnail = thumbnailBitmap;
                explorerItem.Filename = storageFolder.Name;
                explorerItem.Type = "Folder";
                explorerItem.DisplayName = storageFolder.DisplayName;
                explorerItem.DisplayType = storageFolder.DisplayType;
                explorerItem.Path = storageFolder.Path;
                explorerItem.Token = "";
                this.ExplorerItemList.Items.Add(explorerItem);
            }

            foreach (StorageFile file in this.FileList)
            {
                StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                BitmapImage thumbnailBitmap = new BitmapImage();
                thumbnailBitmap.SetSource(thumbnail);

                ExplorerItem explorerItem = new ExplorerItem();
                explorerItem.Thumbnail = thumbnailBitmap;
                explorerItem.Filename = file.Name;
                explorerItem.Type = file.FileType;
                explorerItem.DisplayName = file.DisplayName;
                explorerItem.DisplayType = file.DisplayType;
                explorerItem.Path = file.Path;
                explorerItem.Token = "";
                this.ExplorerItemList.Items.Add(explorerItem);
            }
        }
        
        private IReadOnlyList<StorageFile> FileList;
        private StorageFolder currentStorageFolder;
        private String newFileExtension;

        private void ExplorerItemList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (this.CreateFolderPopup.IsOpen ||
                this.CreateFilePopup.IsOpen)
            {
                this.CreateFolderPopup.IsOpen = false;
                this.CreateFilePopup.IsOpen = false;
                this.ExplorerTopCommandBar.IsEnabled = true;
                this.ExplorerBottomCommandBar.IsEnabled = true;
                return;
            }

            if (ExplorerItemList.SelectionMode == ListViewSelectionMode.None)
            {
                ExplorerItem explorerItem = e.ClickedItem as ExplorerItem;
                if (explorerItem.Type == "Folder")
                {
                    Frame.Navigate(typeof(ExplorerPage), explorerItem);
                }
                else
                {
                    OpenFileParams openFileParams = new OpenFileParams();
                    openFileParams.ClickedFile = explorerItem;
                    openFileParams.Folder = this.currentStorageFolder;
                    openFileParams.FileList = FileList;
                    Frame.Navigate(typeof(ViewerPage), openFileParams);
                }
            }
        }

        private void ExplorerItemList_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (this.CreateFolderPopup.IsOpen ||
                this.CreateFilePopup.IsOpen)
            {
                this.CreateFolderPopup.IsOpen = false;
                this.CreateFilePopup.IsOpen = false;
                this.ExplorerTopCommandBar.IsEnabled = true;
                this.ExplorerBottomCommandBar.IsEnabled = true;
            }
        }

        private void ExplorerItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            this.ExplorerTopCommandBar.IsEnabled = !this.ExplorerTopCommandBar.IsEnabled;
            this.ExplorerBottomCommandBar.IsEnabled = !this.ExplorerBottomCommandBar.IsEnabled;
            this.CreateFolderPopup.IsOpen = false;
            this.CreateFilePopup.IsOpen = !this.CreateFilePopup.IsOpen;
        }

        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {
            this.ExplorerTopCommandBar.IsEnabled = !this.ExplorerTopCommandBar.IsEnabled;
            this.ExplorerBottomCommandBar.IsEnabled = !this.ExplorerBottomCommandBar.IsEnabled;
            this.CreateFilePopup.IsOpen = false;
            this.CreateFolderPopup.IsOpen = !this.CreateFolderPopup.IsOpen;
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            StorageFolder folder;
            StorageFile file;
            foreach (ExplorerItem item in this.ExplorerItemList.SelectedItems)
            {
                if (item.Type == "Folder")
                {
                    folder = await currentStorageFolder.GetFolderAsync(item.Filename);
                    await folder.DeleteAsync();
                }
                else
                {
                    file = await currentStorageFolder.GetFileAsync(item.Filename);
                    await file.DeleteAsync();
                }
            }
            this.GetItemsInCurentFolder();
        }

        private void FileTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (FileTypeCombo.SelectedIndex)
            {
                case 0:
                    newFileExtension = ".pbm";
                    break;
                case 1:
                    newFileExtension = ".pgm";
                    break;
                case 2:
                    newFileExtension = ".ppm";
                    break;
                default:
                    newFileExtension = "";
                    break;
            }
        }

        private async void CreateFolderPopupButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.CreateFolderPopupName.Text != "")
            {
                try
                {
                    await this.currentStorageFolder.CreateFolderAsync(this.CreateFolderPopupName.Text, (CreationCollisionOption)FoldernameCollisionCombo.SelectedIndex);
                    
                }
                catch (Exception ex)
                {

                }
            }
            this.CreateFolderPopup.IsOpen = false;
            this.GetItemsInCurentFolder();
        }

        private async void CreateFilePopupButton_Click(object sender, RoutedEventArgs e)
        {
            StorageFile newFile;
            if (this.CreateFilePopupName.Text != "")
            {
                try
                {
                    byte type = (byte)((FileTypeCombo.SelectedIndex + 1) * (IsBinary.IsOn ? 2 : 1) + 0x30);
                    newFile = await this.currentStorageFolder.CreateFileAsync(this.CreateFilePopupName.Text + newFileExtension, (CreationCollisionOption)FilenameCollisionCombo.SelectedIndex);
                    await FileIO.WriteBytesAsync(newFile, new Byte[4] { 0x50, type, 0x0D, 0x0A });
                    this.GetItemsInCurentFolder();
                    this.CreateFilePopup.IsOpen = false;                   
                    EditFileParams editParams = new EditFileParams();
                    editParams.Type = type - 0x30;
                    editParams.Width = editParams.Height = 0;
                    editParams.File = newFile;
                    editParams.SaveMode = EditFileSaveMode.Save;
                    this.Frame.Navigate(typeof(EditorPage), editParams);
                }
                catch (Exception ex)
                {

                }
            }
            this.CreateFilePopup.IsOpen = false;
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            if (this.ExplorerItemList.SelectionMode == ListViewSelectionMode.None)
            {
                var loader = new ResourceLoader();
                var str = loader.GetString("CancelSelection");
                this.CreateFileTop.Visibility = Visibility.Collapsed;
                this.CreateFolderTop.Visibility = Visibility.Collapsed;
                this.DeleteTop.Visibility = Visibility.Visible;
                this.SelectTop.Icon = new SymbolIcon(Symbol.List);
                this.SelectTop.Label = str;
                this.CreateFileBottom.Visibility = Visibility.Collapsed;
                this.CreateFolderBottom.Visibility = Visibility.Collapsed;
                this.DeleteBottom.Visibility = Visibility.Visible;
                this.SelectBottom.Icon = new SymbolIcon(Symbol.List);
                this.SelectBottom.Label = str;
                this.ExplorerItemList.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else if (ExplorerItemList.SelectionMode == ListViewSelectionMode.Multiple)
            {
                var loader = new ResourceLoader();
                var str = loader.GetString("Select");
                this.CreateFileTop.Visibility = Visibility.Visible;
                this.CreateFolderTop.Visibility = Visibility.Visible;
                this.DeleteTop.Visibility = Visibility.Collapsed;
                this.SelectTop.Icon = new SymbolIcon(Symbol.Bullets);
                this.SelectTop.Label = str;
                this.CreateFileBottom.Visibility = Visibility.Visible;
                this.CreateFolderBottom.Visibility = Visibility.Visible;
                this.DeleteBottom.Visibility = Visibility.Collapsed;
                this.SelectBottom.Icon = new SymbolIcon(Symbol.Bullets);
                this.SelectBottom.Label = str;
                this.ExplorerItemList.SelectionMode = ListViewSelectionMode.None;
            }
        }

        private async void Rate_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(string.Format("ms-windows-store:REVIEW?PFN={0}", Windows.ApplicationModel.Package.Current.Id.FamilyName)));
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= Page_Unloaded;

            this.ExplorerItemList.SelectionChanged -= this.ExplorerItemList_SelectionChanged;
            this.ExplorerItemList.Tapped -= this.ExplorerItemList_Tapped;
            this.CreateFolderPopupButton.Click -= this.CreateFolderPopupButton_Click;
            this.CreateFilePopupButton.Click -= this.CreateFilePopupButton_Click;

            this.CreateFileTop.Click -= this.CreateFile_Click;
            this.CreateFolderTop.Click -= this.CreateFolder_Click;
            this.DeleteTop.Click -= this.Delete_Click;
            this.SelectTop.Click -= this.Select_Click;
            this.RateTop.Click -= this.Rate_Click;
            this.AboutTop.Click -= this.About_Click;

            this.CreateFileBottom.Click -= this.CreateFile_Click;
            this.CreateFolderBottom.Click -= this.CreateFolder_Click;
            this.DeleteBottom.Click -= this.Delete_Click;
            this.SelectBottom.Click -= this.Select_Click;
            this.RateBottom.Click -= this.Rate_Click;
            this.AboutBottom.Click -= this.About_Click;

            this.MobileTrigger.Detach();
            this.DesktopTrigger.Detach();

            this.ExplorerItemList.Items.Clear();
            GC.Collect();
        }
    }
}
