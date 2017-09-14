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
using Windows.Security.Cryptography;
using Windows.Storage.Search;
using Windows.System;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using System.IO;
using Windows.Security.Cryptography.Core;

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Displays anymap images in FlipView gallery
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
            FlipView.Visibility = Visibility.Collapsed;
            for (int i = 0; i < openFileParams.FileList.Count; ++i)
            {
                CanvasWrapper wrapper = new CanvasWrapper();
                FlipView.Items.Add(wrapper);
                imagesInfo.Add(null);
                switch (e.NavigationMode)
                {
                    case NavigationMode.New:
                        if (openFileParams.ClickedFile != null && openFileParams.FileList[i].Name == openFileParams.ClickedFile.Filename)
                        {
                            FlipView.SelectedIndex = i;
                            await this.LoadCanvas(i);
                        }
                        break;
                    case NavigationMode.Back:
                        if (openFileParams.FileList[i].Name == openFileParams.NavigateBackFilename)
                        {
                            FlipView.SelectedIndex = i;
                            await this.LoadCanvas(i);
                        }
                        break;
                    case NavigationMode.Refresh:
                        break;
                }
            }
            if (imagesInfo[FlipView.SelectedIndex] != null)
            {
                this.UpdateInfo();
            }
            
            FlipView.Visibility = Visibility.Visible;
            DataTransferManager.GetForCurrentView().DataRequested += ViewerPage_DataRequested;
            isLoadingCompleted = true;   
        }

        private async Task LoadCanvas(int position)
        {
            // Skip other extensions
            var file = openFileParams.FileList[position];
            if (file.FileType != ".pbm" && file.FileType != ".pgm" && file.FileType != ".ppm")
            {
                return;
            }

            // Skip corrupted formats
            DecodeResult result = await anymapDecoder.decode(file);
            imagesInfo[position] = result;
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
            CanvasControl canvas = new CanvasControl()
            {
                Tag = result
            };
            canvas.CreateResources += Img_CreateResources;
            canvas.Draw += Img_Draw;

            var wrapper = (FlipView.Items[position] as CanvasWrapper);
            wrapper.SetImageInfo(result);
            wrapper.Margin = new Thickness(0, 0, 0, 0);
            wrapper.SetCanvas(canvas);
        }

        private void Img_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            var result = sender.Tag as DecodeResult;
            CanvasBitmap cbm = CanvasBitmap.CreateFromBytes(sender, result.Bytes, result.Width, result.Height, result.DoubleBytesPerColor ? DirectXPixelFormat.R16G16B16A16UIntNormalized : DirectXPixelFormat.B8G8R8A8UIntNormalized);
            CanvasImageBrush brush = new CanvasImageBrush(sender, cbm)
            {
                Interpolation = CanvasImageInterpolation.NearestNeighbor,
                Transform = Matrix3x2.CreateScale(result.CurrentZoom)
            };
            sender.Width = result.Width * result.CurrentZoom;
            sender.Height = result.Height * result.CurrentZoom;
            sender.Tag = brush;
        }

        private void Img_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(new Rect(new Point(), sender.Size), sender.Tag as CanvasImageBrush);
        }

        private async void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FlipView.Visibility == Visibility.Visible &&
                0 <= FlipView.SelectedIndex &&
                FlipView.SelectedIndex < FlipView.Items.Count)
            {
                if (!(FlipView.SelectedItem as CanvasWrapper).IsCanvasSet)
                {
                    await this.LoadCanvas(FlipView.SelectedIndex);
                }
                if (imagesInfo[FlipView.SelectedIndex] != null)
                {
                    this.UpdateInfo();
                }
            }
        }

        private void FlipView_Tapped(object sender, TappedRoutedEventArgs e)
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
                    Debug.WriteLine("ViewerPage FlipView_Tapped: {0}", ex.Message);
                }
            }
        }

        private void UpdateInfo()
        {
            ViewerFilenameTop.Text = imagesInfo[FlipView.SelectedIndex].Filename;
            ViewerFilenameBottom.Text = imagesInfo[FlipView.SelectedIndex].Filename;
            FilenameValue.Text = imagesInfo[FlipView.SelectedIndex].Filename;
            WidthValue.Text = imagesInfo[FlipView.SelectedIndex].Width.ToString() + " px";
            HeightValue.Text = imagesInfo[FlipView.SelectedIndex].Height.ToString() + " px";
            DepthValue.Text = imagesInfo[FlipView.SelectedIndex].DoubleBytesPerColor ? "16 bit" : "8 bit";
            Md5Value.Text = imagesInfo[FlipView.SelectedIndex].Md5;
            Sha1Value.Text = imagesInfo[FlipView.SelectedIndex].Sha1;
            //Sha256Value.Text = imagesInfo[FlipView.SelectedIndex].Sha256;
            //Sha384Value.Text = imagesInfo[FlipView.SelectedIndex].Sha384;
            //Sha512Value.Text = imagesInfo[FlipView.SelectedIndex].Sha512;
            //Histogram.Title = "Histogram";
            //HistogramPlotR.ItemsSource = imagesInfo[FlipView.SelectedIndex].HistogramValues[2];
            //HistogramPlotG.ItemsSource = imagesInfo[FlipView.SelectedIndex].HistogramValues[1];
            //HistogramPlotB.ItemsSource = imagesInfo[FlipView.SelectedIndex].HistogramValues[0];
        }

        private void ViewerShare_Click(object sender, RoutedEventArgs e)
        {
            DataTransferManager.ShowShareUI();
        }

        private void ViewerEdit_Click(object sender, RoutedEventArgs e)
        {
            openFileParams.NavigateBackFilename = openFileParams.FileList[FlipView.SelectedIndex].Name;
            EditFileParams editFileParams = new EditFileParams()
            {
                Bytes = imagesInfo[FlipView.SelectedIndex].Bytes,
                Width = imagesInfo[FlipView.SelectedIndex].Width,
                Height = imagesInfo[FlipView.SelectedIndex].Height,
                Type = imagesInfo[FlipView.SelectedIndex].Type,
                File = openFileParams.FileList[FlipView.SelectedIndex],
                SaveMode = EditFileSaveMode.Save | EditFileSaveMode.SaveAs | EditFileSaveMode.SaveCopy
            };
            Frame.Navigate(typeof(EditorPage), editFileParams);
        }

        private async void ViewerDelete_Click(object sender, RoutedEventArgs e)
        {
            var loader = new ResourceLoader();
            var stringWriter = new StringWriter();
            stringWriter.Write(loader.GetString("DeleteDialogContent"), imagesInfo[FlipView.SelectedIndex].Filename);
            ContentDialog deleteDialog = new ContentDialog()
            {
                Title = loader.GetString("DeleteDialogTitle"),
                Content = stringWriter.ToString(),
                CloseButtonText = loader.GetString("DeleteDialogClose"),
                SecondaryButtonText = loader.GetString("DeleteDialogSecondary"),
                DefaultButton = ContentDialogButton.Close
            };
            if (await deleteDialog.ShowAsync() == ContentDialogResult.Secondary)
            {
                await openFileParams.FileList[FlipView.SelectedIndex].DeleteAsync();
                (FlipView.SelectedItem as CanvasWrapper).RemoveCanvas();
                imagesInfo.RemoveAt(FlipView.SelectedIndex);
                List<StorageFile> tempList = new List<StorageFile>();
                foreach (var file in openFileParams.FileList)
                {
                    tempList.Add(file);
                }
                tempList.RemoveAt(FlipView.SelectedIndex);
                FlipView.Items.RemoveAt(FlipView.SelectedIndex);
                openFileParams.FileList = tempList;
            }         
        }

        private void Properties_Click(object sender, RoutedEventArgs e)
        {
            PropertiesPane.IsPaneOpen = !PropertiesPane.IsPaneOpen;
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

        void ViewerPage_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (!string.IsNullOrEmpty(ViewerFilenameTop.Text))
            {
                List<StorageFile> shareFiles = new List<StorageFile>();
                shareFiles.Insert(0, openFileParams.FileList.ElementAt<StorageFile>(FlipView.SelectedIndex));
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
            (FlipView.SelectedItem as CanvasWrapper).ZoomReal();
        }

        private void ViewerZoomOut_Click(object sender, RoutedEventArgs e)
        {
            (FlipView.SelectedItem as CanvasWrapper).Zoom(0.8f);
        }

        private void ViewerZoomIn_Click(object sender, RoutedEventArgs e)
        {
            (FlipView.SelectedItem as CanvasWrapper).Zoom(1.25f);
        }

        private void ViewerGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Right:
                    if (FlipView.SelectedIndex + 1 < FlipView.Items.Count)
                    {
                        ++FlipView.SelectedIndex;
                    }
                    break;
                case Windows.System.VirtualKey.Left:
                    if (FlipView.SelectedIndex > 0)
                    {
                        --FlipView.SelectedIndex;
                    }
                    break;
            }
        }

        private void FlipView_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Right:
                    if (FlipView.SelectedIndex + 1 < FlipView.Items.Count)
                    {
                        ++FlipView.SelectedIndex;
                    }
                    break;
                case Windows.System.VirtualKey.Left:
                    if (FlipView.SelectedIndex > 0)
                    {
                        --FlipView.SelectedIndex;
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
            FlipView.KeyDown -= FlipView_KeyDown;
            FlipView.Tapped -= FlipView_Tapped;
            FlipView.SelectionChanged -= FlipView_SelectionChanged;
            ViewerZoomReal.Click -= ViewerZoomReal_Click;
            ViewerZoomIn.Click -= ViewerZoomIn_Click;
            ViewerZoomOut.Click -= ViewerZoomOut_Click;

            ViewerShareTop.Click -= ViewerShare_Click;
            ViewerEditTop.Click -= ViewerEdit_Click;
            ViewerDeleteTop.Click -= ViewerDelete_Click;
            this.PropertiesTop.Click -= Properties_Click;
            this.RateTop.Click -= Rate_Click;
            this.DonateTop.Click -= Donate_Click;
            this.AboutTop.Click -= About_Click;

            ViewerShareBottom.Click -= ViewerShare_Click;
            ViewerEditBottom.Click -= ViewerEdit_Click;
            ViewerDeleteBottom.Click -= ViewerDelete_Click;
            this.PropertiesBottom.Click -= Properties_Click;
            this.RateBottom.Click -= Rate_Click;
            this.DonateBottom.Click -= Donate_Click;
            this.AboutBottom.Click -= About_Click;

            this.MobileTrigger.Detach();
            this.DesktopTrigger.Detach();

            foreach (CanvasWrapper wrapper in this.FlipView.Items)
            {
                wrapper.RemoveCanvas();
            }

            FlipView.Items.Clear();
            imagesInfo.Clear();
            GC.Collect();
        }
    }
}
