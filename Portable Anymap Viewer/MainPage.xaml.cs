using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Main page displaying pinned folders
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        public MainPage()
        {
            this.InitializeComponent();
            Current = this;
            Folders = new ObservableCollection<ExplorerItem>();
            SelectedFolders = new List<ExplorerItem>();
            readSavedFolders();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add(".pbm");
            folderPicker.FileTypeFilter.Add(".pgm");
            folderPicker.FileTypeFilter.Add(".ppm");
            StorageFolder storageFolder = await folderPicker.PickSingleFolderAsync();
            if (storageFolder != null)
            {
                string faToken = StorageApplicationPermissions.FutureAccessList.Add(storageFolder);

                StorageItemThumbnail thumbnail = await storageFolder.GetThumbnailAsync(ThumbnailMode.SingleItem);
                BitmapImage thumbnailBitmap = new BitmapImage();
                thumbnailBitmap.SetSource(thumbnail);

                ExplorerItem folder = new ExplorerItem();
                folder.Thumbnail = thumbnailBitmap;
                folder.Name = storageFolder.Name;
                folder.Type = null;
                folder.DisplayName = storageFolder.DisplayName;
                folder.DisplayType = storageFolder.DisplayType;
                folder.Path = storageFolder.Path;
                folder.Token = faToken;
                Folders.Add(folder);
                saveFolder(faToken);
            }
        }

        private void foldersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(ExplorerItem item in e.RemovedItems)
            {
                SelectedFolders.Remove(item);
            }
            foreach(ExplorerItem item in e.AddedItems)
            {
                SelectedFolders.Add(item);
            }
        }

        private async void readSavedFolders()
        {
            IList<string> folders = await FileIO.ReadLinesAsync(await ApplicationData.Current.LocalFolder.CreateFileAsync("FolderTokens.txt", CreationCollisionOption.OpenIfExists),Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
            foreach (string token in folders)
            {
                StorageFolder storageFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
                if (storageFolder != null)
                {
                    StorageItemThumbnail thumbnail = await storageFolder.GetThumbnailAsync(ThumbnailMode.SingleItem);
                    BitmapImage thumbnailBitmap = new BitmapImage();
                    thumbnailBitmap.SetSource(thumbnail);

                    ExplorerItem folder = new ExplorerItem();
                    folder.Thumbnail = thumbnailBitmap;
                    folder.Name = storageFolder.Name;
                    folder.Type = null;
                    folder.DisplayName = storageFolder.DisplayName;
                    folder.DisplayType = storageFolder.DisplayType;
                    folder.Path = storageFolder.Path;
                    folder.Token = token;
                    Folders.Add(folder);
                }
            }
        }

        private async void saveFolder(string token)
        {
            await FileIO.AppendTextAsync(await ApplicationData.Current.LocalFolder.CreateFileAsync("FolderTokens.txt", CreationCollisionOption.OpenIfExists), token+"\n", Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
        }
        
        public static Rect GetElementRect(FrameworkElement element)
        {
            GeneralTransform buttonTransform = element.TransformToVisual(null);
            Point point = buttonTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private ObservableCollection<ExplorerItem> Folders;
        private List<ExplorerItem> SelectedFolders;

        private void FolderList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (FolderList.SelectionMode == ListViewSelectionMode.None)
            {
                Frame.Navigate(typeof(ExplorerPage), e.ClickedItem as ExplorerItem);
            }
        }

        private void SelectFolders_Click(object sender, RoutedEventArgs e)
        {
            if (FolderList.SelectionMode == ListViewSelectionMode.None)
            {
                SelectedFolders.Clear();
                AddFolderTop.Visibility = Visibility.Collapsed;
                RemoveFoldersTop.Visibility = Visibility.Visible;
                SelectFoldersTop.Icon = new SymbolIcon(Symbol.List);
                SelectFoldersTop.Label = "Folders List";
                AddFolderBottom.Visibility = Visibility.Collapsed;
                RemoveFoldersBottom.Visibility = Visibility.Visible;
                SelectFoldersBottom.Icon = new SymbolIcon(Symbol.List);
                SelectFoldersBottom.Label = "Folders List";
                FolderList.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else if (FolderList.SelectionMode == ListViewSelectionMode.Multiple)
            {
                SelectedFolders.Clear();
                AddFolderTop.Visibility = Visibility.Visible;
                RemoveFoldersTop.Visibility = Visibility.Collapsed;
                SelectFoldersTop.Icon = new SymbolIcon(Symbol.Bullets);
                SelectFoldersTop.Label = "Select Folders";
                AddFolderBottom.Visibility = Visibility.Visible;
                RemoveFoldersBottom.Visibility = Visibility.Collapsed;
                SelectFoldersBottom.Icon = new SymbolIcon(Symbol.Bullets);
                SelectFoldersBottom.Label = "Select Folders";
                FolderList.SelectionMode = ListViewSelectionMode.None;
            }
        }

        private async void RemoveFolders_Click(object sender, RoutedEventArgs e)
        {
            IList<string> FolderTokens = new List<string>();
            for (int i = SelectedFolders.Count-1; i >= 0; --i)
            {
                Folders.Remove(SelectedFolders[i]);
            }
            foreach(ExplorerItem item in Folders)
            {
                FolderTokens.Add(item.Token);
            }
            StorageFile FolderFile = await ApplicationData.Current.LocalFolder.CreateFileAsync("FolderTokens.txt", CreationCollisionOption.OpenIfExists);
            if (FolderTokens.Count == 0)
            {
                await FolderFile.DeleteAsync();
            }
            else
            {
                await FileIO.WriteLinesAsync(FolderFile, FolderTokens, Windows.Storage.Streams.UnicodeEncoding.Utf16LE);
            }
            SelectedFolders.Clear();
            AddFolderTop.Visibility = Visibility.Visible;
            RemoveFoldersTop.Visibility = Visibility.Collapsed;
            SelectFoldersTop.Icon = new SymbolIcon(Symbol.Bullets);
            SelectFoldersTop.Label = "Select Folders";
            AddFolderTop.Visibility = Visibility.Visible;
            RemoveFoldersBottom.Visibility = Visibility.Collapsed;
            SelectFoldersBottom.Icon = new SymbolIcon(Symbol.Bullets);
            SelectFoldersBottom.Label = "Select Folders";
            FolderList.SelectionMode = ListViewSelectionMode.None;
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".pbm");
            picker.FileTypeFilter.Add(".pgm");
            picker.FileTypeFilter.Add(".ppm");

            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                OpenFileParams openFileParams = new OpenFileParams();
                openFileParams.FileList = files;
                Frame.Navigate(typeof(ViewerPage), openFileParams);
            }
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                Frame.Navigate(typeof(ConverterPage), files);
            }
        }
    }
}
