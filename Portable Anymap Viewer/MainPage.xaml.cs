using Portable_Anymap_Viewer.Controls;
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
        public MainPage()
        {
            this.InitializeComponent();
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
                FolderList.Items.Add(folder);
                saveFolder(faToken);
            }
        }

        private void foldersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
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
                    folder.Filename = storageFolder.Name;
                    folder.Type = null;
                    folder.DisplayName = storageFolder.DisplayName;
                    folder.DisplayType = storageFolder.DisplayType;
                    folder.Path = storageFolder.Path;
                    folder.Token = token;
                    FolderList.Items.Add(folder);
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
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                var str = loader.GetString("CancelSelection");
                AddFolderTop.Visibility = Visibility.Collapsed;
                RemoveFoldersTop.Visibility = Visibility.Visible;
                SelectFoldersTop.Icon = new SymbolIcon(Symbol.List);
                SelectFoldersTop.Label = str;
                AddFolderBottom.Visibility = Visibility.Collapsed;
                RemoveFoldersBottom.Visibility = Visibility.Visible;
                SelectFoldersBottom.Icon = new SymbolIcon(Symbol.List);
                SelectFoldersBottom.Label = str;
                FolderList.SelectionMode = ListViewSelectionMode.Multiple;
            }
            else if (FolderList.SelectionMode == ListViewSelectionMode.Multiple)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                var str = loader.GetString("SelectFolders");
                AddFolderTop.Visibility = Visibility.Visible;
                RemoveFoldersTop.Visibility = Visibility.Collapsed;
                SelectFoldersTop.Icon = new SymbolIcon(Symbol.Bullets);
                SelectFoldersTop.Label = str;
                AddFolderBottom.Visibility = Visibility.Visible;
                RemoveFoldersBottom.Visibility = Visibility.Collapsed;
                SelectFoldersBottom.Icon = new SymbolIcon(Symbol.Bullets);
                SelectFoldersBottom.Label = str;
                FolderList.SelectionMode = ListViewSelectionMode.None;
            }
        }

        private async void RemoveFolders_Click(object sender, RoutedEventArgs e)
        {
            IList<string> FolderTokens = new List<string>();
            foreach (ExplorerItem item in FolderList.SelectedItems)
            {
                FolderList.Items.Remove(item);
            }
            foreach(ExplorerItem item in FolderList.Items)
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
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            var str = loader.GetString("SelectFolders");
            AddFolderTop.Visibility = Visibility.Visible;
            RemoveFoldersTop.Visibility = Visibility.Collapsed;
            SelectFoldersTop.Icon = new SymbolIcon(Symbol.Bullets);
            SelectFoldersTop.Label = str;
            AddFolderTop.Visibility = Visibility.Visible;
            RemoveFoldersBottom.Visibility = Visibility.Collapsed;
            SelectFoldersBottom.Icon = new SymbolIcon(Symbol.Bullets);
            SelectFoldersBottom.Label = str;
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
                var firstFile = openFileParams.FileList[0];
                ExplorerItem ei = new ExplorerItem();
                ei.Filename = firstFile.Name;
                ei.DisplayName = firstFile.DisplayName;
                ei.DisplayType = firstFile.DisplayType;
                openFileParams.ClickedFile = ei;
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= Page_Unloaded;

            FolderList.SelectionChanged -= foldersListView_SelectionChanged;

            AddFolderTop.Click -= AddFolder_Click;
            RemoveFoldersTop.Click -= RemoveFolders_Click;
            SelectFoldersTop.Click -= SelectFolders_Click;
            OpenFileTop.Click -= OpenFile_Click;
            ConvertTop.Click -= Convert_Click;

            AddFolderBottom.Click -= AddFolder_Click;
            RemoveFoldersBottom.Click -= RemoveFolders_Click;
            SelectFoldersBottom.Click -= SelectFolders_Click;
            OpenFileBottom.Click -= OpenFile_Click;
            ConvertBottom.Click -= Convert_Click;

            MobileTrigger.Detach();
            DesktopTrigger.Detach();

            FolderList.Items.Clear();
            GC.Collect();
        }
    }
}
