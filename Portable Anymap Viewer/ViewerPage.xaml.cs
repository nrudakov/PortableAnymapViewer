using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Storage;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI.ViewManagement;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

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
        
        //private static readonly DependencyProperty CommandBarPositionProperty =
        //    DependencyProperty.Register("CommandBarPosition", typeof(CommandBarPosition), typeof(ViewerPage), new PropertyMetadata(CommandBarPosition.Top));
        //private CommandBarPosition _commandBarPosition;
        //private CommandBarPosition CommandBarPosition
        //{
        //    get
        //    {
        //        //return (CommandBarPosition)GetValue(CommandBarPositionProperty);
        //        return _commandBarPosition;
        //    }
        //    set
        //    {
        //        //SetValue(CommandBarPositionProperty, value);
        //        _commandBarPosition = value;
        //    }
        //}

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            //if (e.NavigationMode == NavigationMode.Back)
            //{

            //}
            //else
            //{

            if (String.Equals(AnalyticsInfo.VersionInfo.DeviceFamily, "Windows.Desktop"))
            {
                //ViewerCommandBar.VerticalAlignment = VerticalAlignment.Top;
                ViewerZoomStack.Visibility = Visibility.Visible;
            }

            openFileParams = e.Parameter as OpenFileParams;
            flipView.Visibility = Visibility.Collapsed;
            int fileId = 0;

            foreach (StorageFile file in openFileParams.FileList)
            {
                // Skip other extensions
                if (file.FileType != ".pbm" && file.FileType != ".pgm" && file.FileType != ".ppm")
                {
                    continue;
                }

                // Skip corrupted formats
                DecodeResult result = await anymapDecoder.decode(file);
                if (result.Bytes == null)
                {
                    continue;
                }
                
                // Create canvas
                CanvasControl canvas = new CanvasControl();
                canvas.Tag = result;
                canvas.CreateResources += Img_CreateResources;
                canvas.Draw += Img_Draw;

                CanvasWrapper wrapper = new CanvasWrapper(result);
                wrapper.Margin = new Thickness(0, 0, 0, 0);
                wrapper.SetCanvas(canvas);

                imagesInfo.Add(result);
                flipView.Items.Add(wrapper);
                
                if (openFileParams.ClickedFile != null && file.Name == openFileParams.ClickedFile.Name)
                {
                    flipView.SelectedItem = flipView.Items.ElementAt(fileId);
                }
                ++fileId;
            }
            ViewerFilenameTop.Text = imagesInfo[flipView.SelectedIndex].Filename;
            ViewerFilenameBottom.Text = imagesInfo[flipView.SelectedIndex].Filename;
            flipView.Visibility = Visibility.Visible;
            DataTransferManager.GetForCurrentView().DataRequested += ViewerPage_DataRequested;
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
        }

        private void Img_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            var result = sender.Tag as DecodeResult;
            CanvasBitmap cbm = CanvasBitmap.CreateFromBytes(sender, result.Bytes, result.Width, result.Height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            CanvasImageBrush brush = new CanvasImageBrush(sender, cbm);
            brush.Interpolation = CanvasImageInterpolation.NearestNeighbor;
            sender.Tag = brush;
            sender.Width = result.Width;
            sender.Height = result.Height;
        }

        private void Img_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(new Rect(new Point(), sender.Size), sender.Tag as CanvasImageBrush);
        }

        private void flipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flipView.Visibility == Visibility.Visible)
            {
                ViewerFilenameTop.Text = imagesInfo[flipView.SelectedIndex].Filename;
                ViewerFilenameBottom.Text = imagesInfo[flipView.SelectedIndex].Filename;
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
            EditFileParams editFileParams = new EditFileParams();
            editFileParams.Bytes = imagesInfo[flipView.SelectedIndex].Bytes;
            editFileParams.Width = imagesInfo[flipView.SelectedIndex].Width;
            editFileParams.Height = imagesInfo[flipView.SelectedIndex].Height;
            editFileParams.Type = imagesInfo[flipView.SelectedIndex].Type;
            editFileParams.File = openFileParams.FileList[flipView.SelectedIndex];
            Frame.Navigate(typeof(EditorPage), editFileParams);
        }

        private void ViewerDelete_Click(object sender, RoutedEventArgs e)
        {

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
    }
}
