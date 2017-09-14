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
using Windows.System;
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
            ReadSavedFolders();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
        }

        private async void AddFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker folderPicker = new FolderPicker()
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary
            };
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

                ExplorerItem folder = new ExplorerItem()
                {
                    Thumbnail = thumbnailBitmap,
                    Filename = storageFolder.Name,
                    Type = null,
                    DisplayName = storageFolder.DisplayName,
                    DisplayType = storageFolder.DisplayType,
                    Path = storageFolder.Path,
                    Token = faToken
                };
                FolderList.Items.Add(folder);
                SaveFolder(faToken);
            }
        }

        private void FoldersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private async void ReadSavedFolders()
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

                    ExplorerItem folder = new ExplorerItem()
                    {
                        Thumbnail = thumbnailBitmap,
                        Filename = storageFolder.Name,
                        Type = null,
                        DisplayName = storageFolder.DisplayName,
                        DisplayType = storageFolder.DisplayType,
                        Path = storageFolder.Path,
                        Token = token
                    };
                    FolderList.Items.Add(folder);
                }
            }
        }

        private async void SaveFolder(string token)
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

        private void CreateFile_Click(object sender, RoutedEventArgs e)
        {
            MainTopCommandBar.IsEnabled = !MainTopCommandBar.IsEnabled;
            MainBottomCommandBar.IsEnabled = !MainBottomCommandBar.IsEnabled;
            CreateFilePopup.IsOpen = !CreateFilePopup.IsOpen;
        }

        private void CreateFilePopupButton_Click(object sender, RoutedEventArgs e)
        {
            EditFileParams editParams = new EditFileParams()
            {
                Type = IsBinary.IsOn ? 4 : 1,
                Width = 0,
                Height = 0,
                File = null,
                SaveMode = EditFileSaveMode.SaveAs
            };
            this.Frame.Navigate(typeof(EditorPage), editParams);
        }

        private void FolderList_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (CreateFilePopup.IsOpen)
            {
                CreateFilePopup.IsOpen = false;
                MainTopCommandBar.IsEnabled = true;
                MainBottomCommandBar.IsEnabled = true;
            }
        }

        private async void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".pbm");
            picker.FileTypeFilter.Add(".pgm");
            picker.FileTypeFilter.Add(".ppm");

            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                OpenFileParams openFileParams = new OpenFileParams()
                {
                    FileList = files,
                    ClickedFile = new ExplorerItem()
                    {
                        Filename = files[0].Name,
                        DisplayName = files[0].DisplayName,
                        DisplayType = files[0].DisplayType
                    }
                };
                Frame.Navigate(typeof(ViewerPage), openFileParams);
            }
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");

            IReadOnlyList<StorageFile> files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                Frame.Navigate(typeof(ConverterPage), files);
            }
        }

        private async void Rate_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(string.Format("ms-windows-store:REVIEW?PFN={0}", Windows.ApplicationModel.Package.Current.Id.FamilyName)));
        }

        private void Donate_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(DonatePage));
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(AboutPage));
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= Page_Unloaded;

            FolderList.Tapped -= FolderList_Tapped;
            FolderList.SelectionChanged -= FoldersListView_SelectionChanged;
            CreateFilePopupButton.Click -= CreateFilePopupButton_Click;

            AddFolderTop.Click -= AddFolder_Click;
            RemoveFoldersTop.Click -= RemoveFolders_Click;
            SelectFoldersTop.Click -= SelectFolders_Click;
            CreateFileTop.Click -= CreateFile_Click;
            OpenFileTop.Click -= OpenFile_Click;
            ConvertTop.Click -= Convert_Click;
            RateTop.Click -= Rate_Click;
            DonateTop.Click -= Donate_Click;
            AboutTop.Click -= About_Click;

            AddFolderBottom.Click -= AddFolder_Click;
            RemoveFoldersBottom.Click -= RemoveFolders_Click;
            SelectFoldersBottom.Click -= SelectFolders_Click;
            CreateFileBottom.Click -= CreateFile_Click;
            OpenFileBottom.Click -= OpenFile_Click;
            ConvertBottom.Click -= Convert_Click;
            RateBottom.Click -= Rate_Click;
            DonateBottom.Click -= Donate_Click;
            AboutBottom.Click -= About_Click;

            MobileTrigger.Detach();
            DesktopTrigger.Detach();

            FolderList.Items.Clear();
            GC.Collect();
        }
    }
}
