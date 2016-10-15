using Portable_Anymap_Viewer.Controls;
using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
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
            this.ExplorerHeaderTitle.Text = paramFolder.Path;
            StorageFolder currentStorageFolder = await StorageFolder.GetFolderFromPathAsync(paramFolder.Path);
            IReadOnlyList<StorageFolder> folderList = await currentStorageFolder.GetFoldersAsync();

            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".pbm");
            fileTypeFilter.Add(".pgm");
            fileTypeFilter.Add(".ppm");
            QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypeFilter);
            StorageFileQueryResult results = currentStorageFolder.CreateFileQueryWithOptions(queryOptions);
            this.FileList = await results.GetFilesAsync();

            foreach (StorageFolder storageFolder in folderList)
            {
                StorageItemThumbnail thumbnail = await storageFolder.GetThumbnailAsync(ThumbnailMode.SingleItem);

                BitmapImage thumbnailBitmap = new BitmapImage();   
                thumbnailBitmap.SetSource(thumbnail);
                
                ExplorerItem explorerItem = new ExplorerItem();
                explorerItem.Thumbnail = thumbnailBitmap;
                explorerItem.Filename = storageFolder.Name;
                explorerItem.Type = null;
                explorerItem.DisplayName = storageFolder.DisplayName;
                explorerItem.DisplayType = storageFolder.DisplayType;
                explorerItem.Path = storageFolder.Path;
                explorerItem.Token = "";
                this.ExplorerItemList.Items.Add(explorerItem);
            }

            foreach(StorageFile file in this.FileList)
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

        private void ExplorerItemList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (ExplorerItemList.SelectionMode == ListViewSelectionMode.None)
            {
                ExplorerItem explorerItem = e.ClickedItem as ExplorerItem;
                if (explorerItem.Type == null)
                {
                    Frame.Navigate(typeof(ExplorerPage), explorerItem);
                }
                else
                {
                    OpenFileParams openFileParams = new OpenFileParams();
                    openFileParams.ClickedFile = explorerItem;
                    openFileParams.FileList = FileList;
                    Frame.Navigate(typeof(ViewerPage), openFileParams);
                }
            }   
        }

        private void ExplorerItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CreateFolder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {

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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= Page_Unloaded;

            this.ExplorerItemList.SelectionChanged -= this.ExplorerItemList_SelectionChanged;

            this.CreateFileTop.Click -= this.CreateFile_Click;
            this.CreateFolderTop.Click -= this.CreateFolder_Click;
            this.DeleteTop.Click -= this.Delete_Click;
            this.SelectTop.Click -= this.Select_Click;

            this.CreateFileBottom.Click -= this.CreateFile_Click;
            this.CreateFolderBottom.Click -= this.CreateFolder_Click;
            this.DeleteBottom.Click -= this.Delete_Click;
            this.SelectBottom.Click -= this.Select_Click;

            this.MobileTrigger.Detach();
            this.DesktopTrigger.Detach();

            this.ExplorerItemList.Items.Clear();
            GC.Collect();
        }
    }
}
