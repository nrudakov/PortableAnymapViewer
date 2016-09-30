using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Search;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

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
            ExplorerItems = new ObservableCollection<ExplorerItem>();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ExplorerItem paramFolder = e.Parameter as ExplorerItem;
            ExplorerHeaderTitle.Text = paramFolder.Path;
            StorageFolder currentStorageFolder = await StorageFolder.GetFolderFromPathAsync(paramFolder.Path);
            IReadOnlyList<StorageFolder> folderList = await currentStorageFolder.GetFoldersAsync();

            List<string> fileTypeFilter = new List<string>();
            fileTypeFilter.Add(".pbm");
            fileTypeFilter.Add(".pgm");
            fileTypeFilter.Add(".ppm");
            QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypeFilter);
            StorageFileQueryResult results = currentStorageFolder.CreateFileQueryWithOptions(queryOptions);
            FileList = await results.GetFilesAsync();

            foreach (StorageFolder storageFolder in folderList)
            {
                StorageItemThumbnail thumbnail = await storageFolder.GetThumbnailAsync(ThumbnailMode.SingleItem);

                BitmapImage thumbnailBitmap = new BitmapImage();   
                thumbnailBitmap.SetSource(thumbnail);
                
                ExplorerItem explorerItem = new ExplorerItem();
                explorerItem.Thumbnail = thumbnailBitmap;
                explorerItem.Name = storageFolder.Name;
                explorerItem.Type = null;
                explorerItem.DisplayName = storageFolder.DisplayName;
                explorerItem.DisplayType = storageFolder.DisplayType;
                explorerItem.Path = storageFolder.Path;
                explorerItem.Token = "";
                ExplorerItems.Add(explorerItem);
            }

            foreach(StorageFile file in FileList)
            {
                StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem);
                BitmapImage thumbnailBitmap = new BitmapImage();
                thumbnailBitmap.SetSource(thumbnail);

                ExplorerItem explorerItem = new ExplorerItem();
                explorerItem.Thumbnail = thumbnailBitmap;
                explorerItem.Name = file.Name;
                explorerItem.Type = file.FileType;
                explorerItem.DisplayName = file.DisplayName;
                explorerItem.DisplayType = file.DisplayType;
                explorerItem.Path = file.Path;
                explorerItem.Token = "";
                ExplorerItems.Add(explorerItem);
            }
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
        }

        private ObservableCollection<ExplorerItem> ExplorerItems;
        private IReadOnlyList<StorageFile> FileList;

        private void ExplorerItemList_ItemClick(object sender, ItemClickEventArgs e)
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

        private void ExplorerItemList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddFile_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
