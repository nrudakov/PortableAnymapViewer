using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ViewerPage : Page
    {
        public ViewerPage()
        {
            transforms = new TransformGroup();
            previousTrasform = new MatrixTransform
            {
                Matrix = Matrix.Identity
            };
            deltaTransform = new CompositeTransform();
            transforms.Children.Add(previousTrasform);
            transforms.Children.Add(deltaTransform);

            this.InitializeComponent();
        }

        private OpenFileParams openFileParams;
        private List<DecodeResult> imagesInfo = new List<DecodeResult>();
        private AnymapDecoder anymapDecoder = new AnymapDecoder();

        private TransformGroup transforms;
        private MatrixTransform previousTrasform;
        private CompositeTransform deltaTransform;


        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            //if (e.NavigationMode == NavigationMode.Back)
            //{

            //}
            //else
            //{
            if (String.Equals(AnalyticsInfo.VersionInfo.DeviceFamily, "Windows.Desktop"))
            {
                ViewerCommandBar.VerticalAlignment = VerticalAlignment.Top;
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

                imagesInfo.Add(result);

                // Create canvas
                CanvasControl canvas = new CanvasControl();
                canvas.Tag = result;
                canvas.ManipulationMode =
                    ManipulationModes.Scale |
                    ManipulationModes.TranslateX |
                    ManipulationModes.TranslateY;
                canvas.CreateResources += Img_CreateResources;
                canvas.Draw += Img_Draw;
                canvas.ManipulationDelta += Canvas_ManipulationDelta;
                canvas.RenderTransform = transforms;


                ScrollViewer scroll = new ScrollViewer();
                scroll.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                scroll.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                scroll.Content = canvas;
                flipView.Items.Add(scroll);




                if (file.Name == openFileParams.ClickedFile.Name)
                {
                    flipView.SelectedItem = flipView.Items.ElementAt(fileId);
                }
                ++fileId;
            }
            text.Text = imagesInfo[flipView.SelectedIndex].Filename;
            flipView.Visibility = Visibility.Visible;
            DataTransferManager.GetForCurrentView().DataRequested += ViewerPage_DataRequested;
        }

        private void Canvas_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            previousTrasform.Matrix = transforms.Value;
            deltaTransform.TranslateX = e.Delta.Translation.X;
            deltaTransform.TranslateY = e.Delta.Translation.Y;
            deltaTransform.ScaleX = e.Delta.Scale;
            deltaTransform.ScaleY = e.Delta.Scale;
        }

        private void Img_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            var result = sender.Tag as DecodeResult;
            CanvasBitmap cbm = CanvasBitmap.CreateFromBytes(sender, result.Bytes, result.Width, result.Height, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);
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
                text.Text = imagesInfo[flipView.SelectedIndex].Filename;
            }
        }

        private void flipView_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.OriginalSource.GetType() == typeof(Grid) ||
                e.OriginalSource.GetType() == typeof(Image))
            {
                if (ViewerCommandBar.Visibility == Visibility.Collapsed)
                {
                    ViewerCommandBar.Visibility = Visibility.Visible;
                    if (String.Equals(AnalyticsInfo.VersionInfo.DeviceFamily, "Windows.Mobile"))
                        text.Visibility = Visibility.Visible;
                }
                else
                {
                    ViewerCommandBar.Visibility = Visibility.Collapsed;
                    if (String.Equals(AnalyticsInfo.VersionInfo.DeviceFamily, "Windows.Mobile"))
                        text.Visibility = Visibility.Collapsed;
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
            if (!string.IsNullOrEmpty(text.Text))
            {
                List<StorageFile> shareFiles = new List<StorageFile>();
                shareFiles.Insert(0, openFileParams.FileList.ElementAt<StorageFile>(flipView.SelectedIndex));
                args.Request.Data.Properties.Title = text.Text;
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
            CanvasControl canvas = (flipView.SelectedItem as ScrollViewer).Content as CanvasControl;
            canvas.Width = imagesInfo[flipView.SelectedIndex].Width;
            canvas.Height = imagesInfo[flipView.SelectedIndex].Height;
            imagesInfo[flipView.SelectedIndex].CurrentZoom = 1.0;
            (canvas.Tag as CanvasImageBrush).Transform = Matrix3x2.CreateScale((float)imagesInfo[flipView.SelectedIndex].CurrentZoom);
            canvas.Invalidate();
            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).Tag as CanvasImageBrush).Transform = System.Numerics.Matrix3x2.CreateScale((float)1);
            //((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).Invalidate();
            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).RenderTransform as CompositeTransform).ScaleX = 1.0;
            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).RenderTransform as CompositeTransform).ScaleY = 1.0;
            //(((flipView.SelectedItem as ScrollViewer).Content as Image).Source as WriteableBitmap).Invalidate();
        }

        private void ViewerZoomOut_Click(object sender, RoutedEventArgs e)
        {
            CanvasControl canvas = (flipView.SelectedItem as ScrollViewer).Content as CanvasControl;
            if (canvas.Width * 0.8 >= 1.0 &&
                canvas.Height * 0.8 >= 1.0)
            {
                canvas.Width *= 0.8;
                canvas.Height *= 0.8;
                imagesInfo[flipView.SelectedIndex].CurrentZoom *= 0.8;
                (canvas.Tag as CanvasImageBrush).Transform = Matrix3x2.CreateScale((float)imagesInfo[flipView.SelectedIndex].CurrentZoom);
                canvas.Invalidate();
            }
            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).Tag as CanvasImageBrush).Transform = System.Numerics.Matrix3x2.CreateScale((float)0.5);
            //((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).Invalidate();
            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).RenderTransform as CompositeTransform).ScaleX *= 0.8;
            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).RenderTransform as CompositeTransform).ScaleY *= 0.8;
            //(((flipView.SelectedItem as ScrollViewer).Content as Image).Source as WriteableBitmap).Invalidate();
        }

        private void ViewerZoomIn_Click(object sender, RoutedEventArgs e)
        {
            CanvasControl canvas = (flipView.SelectedItem as ScrollViewer).Content as CanvasControl;
            if (canvas.Width * 1.2 < canvas.Device.MaximumBitmapSizeInPixels &&
                canvas.Height * 1.2 < canvas.Device.MaximumBitmapSizeInPixels)
            {
                canvas.Width *= 1.2;
                canvas.Height *= 1.2;
                imagesInfo[flipView.SelectedIndex].CurrentZoom *= 1.2;
                (canvas.Tag as CanvasImageBrush).Transform = Matrix3x2.CreateScale((float)imagesInfo[flipView.SelectedIndex].CurrentZoom);
                canvas.Invalidate();
            }
            //((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).Width *= 100;
            //((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).Height *= 100;
            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).Tag as CanvasImageBrush).Transform = System.Numerics.Matrix3x2.CreateScale((float)100);
            //((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).Invalidate();

            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).RenderTransform as CompositeTransform).ScaleX *= 1.2;
            //(((flipView.SelectedItem as ScrollViewer).Content as CanvasControl).RenderTransform as CompositeTransform).ScaleY *= 1.2;
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
