using Portable_Anymap_Viewer.Classes;
using Portable_Anymap_Viewer.Controls;
using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI.ViewManagement;
using Windows.Storage.Search;
using Windows.System;

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Displays anymap images in flipview gallery
    /// </summary>
    public sealed partial class ViewerPage : Page
    {
        public ViewerPage()
        {
            this.InitializeComponent();
            DataContext = this;
        }

        private OpenFileParams openFileParams;
        private List<DecodeResult> imagesInfo = new List<DecodeResult>();
        private AnymapDecoder anymapDecoder = new AnymapDecoder();
        private bool isLoadingCompleted = false;
        
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            openFileParams = e.Parameter as OpenFileParams;
            flipView.Visibility = Visibility.Collapsed;
            for (int i = 0; i < openFileParams.FileList.Count; ++i)
            {
                CanvasWrapper wrapper = new CanvasWrapper();
                flipView.Items.Add(wrapper);
                imagesInfo.Add(null);
                switch (e.NavigationMode)
                {
                    case NavigationMode.New:
                        if (openFileParams.ClickedFile != null && openFileParams.FileList[i].Name == openFileParams.ClickedFile.Filename)
                        {
                            flipView.SelectedIndex = i;
                            await this.LoadCanvas(i);
                        }
                        break;
                    case NavigationMode.Back:
                        if (openFileParams.FileList[i].Name == openFileParams.NavigateBackFilename)
                        {
                            flipView.SelectedIndex = i;
                            await this.LoadCanvas(i);
                        }
                        break;
                }
            }
            if (imagesInfo[flipView.SelectedIndex] != null)
            {
                ViewerFilenameTop.Text = imagesInfo[flipView.SelectedIndex].Filename;
                ViewerFilenameBottom.Text = imagesInfo[flipView.SelectedIndex].Filename;
            }
            
            flipView.Visibility = Visibility.Visible;
            DataTransferManager.GetForCurrentView().DataRequested += ViewerPage_DataRequested;
            isLoadingCompleted = true;   
        }

        private async Task LoadCanvas(int pos)
        {
            // Skip other extensions
            var file = openFileParams.FileList[pos];
            if (file.FileType != ".pbm" && file.FileType != ".pgm" && file.FileType != ".ppm")
            {
                return;
            }

            // Skip corrupted formats
            DecodeResult result = await anymapDecoder.decode(file);
            imagesInfo[pos] = result;
            if (result.Bytes == null)
            {
                return;
            }

            Double width = ApplicationView.GetForCurrentView().VisibleBounds.Width;
            Double height = ApplicationView.GetForCurrentView().VisibleBounds.Height;
            Double widthDiff = result.Width - width;
            Double heightDiff = result.Height - height;
            if (widthDiff > 0 || heightDiff > 0)
            {
                if (widthDiff > heightDiff)
                {
                    result.CurrentZoom = (Single)width / result.Width;
                }
                else
                {
                    result.CurrentZoom = (Single)height / result.Height;
                }
            }
            // Create canvas
            CanvasControl canvas = new CanvasControl();
            canvas.Tag = result;
            canvas.CreateResources += Img_CreateResources;
            canvas.Draw += Img_Draw;

            var wrapper = (flipView.Items[pos] as CanvasWrapper);
            wrapper.SetImageInfo(result);
            wrapper.Margin = new Thickness(0, 0, 0, 0);
            wrapper.SetCanvas(canvas);
        }

        private void Img_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            var result = sender.Tag as DecodeResult;
            CanvasBitmap cbm = CanvasBitmap.CreateFromBytes(sender, result.Bytes, result.Width, result.Height, result.DoubleBytesPerColor ? DirectXPixelFormat.R16G16B16A16UIntNormalized : DirectXPixelFormat.B8G8R8A8UIntNormalized);
            CanvasImageBrush brush = new CanvasImageBrush(sender, cbm);
            brush.Interpolation = CanvasImageInterpolation.NearestNeighbor;                    
            brush.Transform = Matrix3x2.CreateScale(result.CurrentZoom);
            sender.Width = result.Width * result.CurrentZoom;
            sender.Height = result.Height * result.CurrentZoom;
            sender.Tag = brush;
        }

        private void Img_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(new Rect(new Point(), sender.Size), sender.Tag as CanvasImageBrush);
        }

        private async void flipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flipView.Visibility == Visibility.Visible &&
                0 <= flipView.SelectedIndex &&
                flipView.SelectedIndex < flipView.Items.Count)
            {
                if (!(flipView.SelectedItem as CanvasWrapper).IsCanvasSet)
                {
                    await this.LoadCanvas(flipView.SelectedIndex);
                }
                if (imagesInfo[flipView.SelectedIndex] != null)
                {
                    ViewerFilenameTop.Text = imagesInfo[flipView.SelectedIndex].Filename;
                    ViewerFilenameBottom.Text = imagesInfo[flipView.SelectedIndex].Filename;
                }
            }
        }

        private void flipView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(CanvasWrapper) ||
                e.OriginalSource.GetType() == typeof(Image))
            {
                try
                {
                    if (DisplayModeStates.CurrentState == DesktopMode)
                    {
                        if (ViewerTopCommandBar.Visibility == Visibility.Visible)
                        {
                            ViewerTopCommandBar.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ViewerTopCommandBar.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        if (ViewerBottomCommandBar.Visibility == Visibility.Visible)
                        {
                            ViewerBottomCommandBar.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            ViewerBottomCommandBar.Visibility = Visibility.Visible;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("ViewerPage flipView_Tapped: {0}", ex.Message);
                }
            }
        }

        private void ViewerShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void ViewerEdit_Click(object sender, RoutedEventArgs e)
        {
            openFileParams.NavigateBackFilename = openFileParams.FileList[flipView.SelectedIndex].Name;
            EditFileParams editFileParams = new EditFileParams();
            editFileParams.Bytes = imagesInfo[flipView.SelectedIndex].Bytes;
            editFileParams.Width = imagesInfo[flipView.SelectedIndex].Width;
            editFileParams.Height = imagesInfo[flipView.SelectedIndex].Height;
            editFileParams.Type = imagesInfo[flipView.SelectedIndex].Type;
            editFileParams.File = openFileParams.FileList[flipView.SelectedIndex];
            editFileParams.SaveMode = EditFileSaveMode.Save | EditFileSaveMode.SaveAs | EditFileSaveMode.SaveCopy;
            Frame.Navigate(typeof(EditorPage), editFileParams);
        }

        private async void ViewerDelete_Click(object sender, RoutedEventArgs e)
        {
            var loader = new ResourceLoader();
            var warningTilte = loader.GetString("DeleteFileTitle") + " " + openFileParams.FileList[flipView.SelectedIndex].Name;
            var warningMesage = loader.GetString("DeleteFileWarning");
            var yes = loader.GetString("Yes");
            var no = loader.GetString("No");
            MessageDialog deleteConfirmation = new MessageDialog(warningMesage, warningTilte);
            deleteConfirmation.Commands.Add(new UICommand(yes));
            deleteConfirmation.Commands.Add(new UICommand(no));
            deleteConfirmation.DefaultCommandIndex = 1;

            var selectedCommand = await deleteConfirmation.ShowAsync();

            if (selectedCommand.Label == yes)
            {
                await openFileParams.FileList[flipView.SelectedIndex].DeleteAsync();
                (flipView.SelectedItem as CanvasWrapper).RemoveCanvas();
                flipView.Items.RemoveAt(flipView.SelectedIndex);
                imagesInfo.RemoveAt(flipView.SelectedIndex);
                List<string> fileTypeFilter = new List<string>();
                fileTypeFilter.Add(".pbm");
                fileTypeFilter.Add(".pgm");
                fileTypeFilter.Add(".ppm");
                QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, fileTypeFilter);
                StorageFileQueryResult results = this.openFileParams.Folder.CreateFileQueryWithOptions(queryOptions);
                openFileParams.FileList = await results.GetFilesAsync();
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

        void ViewerPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (!string.IsNullOrEmpty(ViewerFilenameTop.Text))
            {
                List<StorageFile> shareFiles = new List<StorageFile>();
                shareFiles.Insert(0, openFileParams.FileList.ElementAt<StorageFile>(flipView.SelectedIndex));
                args.Request.Data.Properties.Title = ViewerFilenameTop.Text;
                args.Request.Data.Properties.Description = "Portable Anymap Sharing";
                args.Request.Data.SetStorageItems(shareFiles);
            }
            else
            {
                args.Request.FailWithDisplayText("Nothing to share");
            }
        }

        private void ViewerZoomReal_Click(object sender, RoutedEventArgs e)
        {
            (flipView.SelectedItem as CanvasWrapper).ZoomReal();
        }

        private void ViewerZoomOut_Click(object sender, RoutedEventArgs e)
        {
            (flipView.SelectedItem as CanvasWrapper).Zoom(0.8f);
        }

        private void ViewerZoomIn_Click(object sender, RoutedEventArgs e)
        {
            (flipView.SelectedItem as CanvasWrapper).Zoom(1.25f);
        }

        private void ViewerGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Right:
                    if (flipView.SelectedIndex + 1 < flipView.Items.Count)
                    {
                        ++flipView.SelectedIndex;
                    }
                    break;
                case Windows.System.VirtualKey.Left:
                    if (flipView.SelectedIndex > 0)
                    {
                        --flipView.SelectedIndex;
                    }
                    break;
            }
        }

        private void flipView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Right:
                    if (flipView.SelectedIndex + 1 < flipView.Items.Count)
                    {
                        ++flipView.SelectedIndex;
                    }
                    break;
                case Windows.System.VirtualKey.Left:
                    if (flipView.SelectedIndex > 0)
                    {
                        --flipView.SelectedIndex;
                    }
                    break;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            // Wait for loading to avoid memory leak
            int i = 0;
            while (!isLoadingCompleted)
            {
                Debug.WriteLine("Unloaded waiting: {0}", ++i);
            }

            this.Unloaded -= Page_Unloaded;
            DataTransferManager.GetForCurrentView().DataRequested -= ViewerPage_DataRequested;

            ViewerGrid.KeyDown -= ViewerGrid_KeyDown;
            flipView.KeyDown -= flipView_KeyDown;
            flipView.Tapped -= flipView_Tapped;
            flipView.SelectionChanged -= flipView_SelectionChanged;
            ViewerZoomReal.Click -= ViewerZoomReal_Click;
            ViewerZoomIn.Click -= ViewerZoomIn_Click;
            ViewerZoomOut.Click -= ViewerZoomOut_Click;

            ViewerShareTop.Click -= ViewerShare_Click;
            ViewerEditTop.Click -= ViewerEdit_Click;
            ViewerDeleteTop.Click -= ViewerDelete_Click;

            ViewerShareBottom.Click -= ViewerShare_Click;
            ViewerEditBottom.Click -= ViewerEdit_Click;
            ViewerDeleteBottom.Click -= ViewerDelete_Click;

            this.MobileTrigger.Detach();
            this.DesktopTrigger.Detach();

            foreach (CanvasWrapper wrapper in this.flipView.Items)
            {
                wrapper.RemoveCanvas();
            }

            flipView.Items.Clear();
            imagesInfo.Clear();
            GC.Collect();
        }
    }
}
