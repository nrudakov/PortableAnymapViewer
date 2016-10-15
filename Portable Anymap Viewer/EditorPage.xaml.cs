using Portable_Anymap_Viewer.Classes;
using Portable_Anymap_Viewer.Models;
using System;
using System.Text;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Page for editing anymap files
    /// </summary>
    public sealed partial class EditorPage : Page
    {
        public EditorPage()
        {
            this.InitializeComponent();
            decoder = new AnymapDecoder();
            EditorCompareTop.AddHandler(PointerPressedEvent, new PointerEventHandler(EditorCompare_PointerPressed), true);
            EditorCompareTop.AddHandler(PointerReleasedEvent, new PointerEventHandler(EditorCompare_PointerReleased), true);
            EditorCompareBottom.AddHandler(PointerPressedEvent, new PointerEventHandler(EditorCompare_PointerPressed), true);
            EditorCompareBottom.AddHandler(PointerReleasedEvent, new PointerEventHandler(EditorCompare_PointerReleased), true);
        }

        AnymapDecoder decoder;
        EditFileParams editFileParams;
        CanvasBitmap cbm;
        CanvasImageBrush brush;
        DecodeResult lastDecodeResult;
        string initialStrAll;
        string currentStrAll;
        byte[] initialBytes;
        byte[] currentBytes;
        int editorRow;
        
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            editFileParams = (e.Parameter as EditFileParams);
            switch (editFileParams.Type)
            {
                case 1:
                case 2:
                case 3:
                    editorRow = 0;
                    EditorText.Visibility = Visibility.Visible;
                    
                    var streamT = await editFileParams.File.OpenAsync(FileAccessMode.Read);
                    var dataReaderT = new DataReader(streamT.GetInputStreamAt(0));
                    uint bytesLoadedT = await dataReaderT.LoadAsync((uint)(streamT.Size));

                    byte[] bytesText = new byte[bytesLoadedT];
                    dataReaderT.ReadBytes(bytesText);

                    ASCIIEncoding ascii = new ASCIIEncoding();
                    char[] str = new char[bytesLoadedT];
                    ascii.GetDecoder().GetChars(bytesText, 0, (int)bytesLoadedT, str, 0);
                    initialStrAll = new String(str);
                    currentStrAll = new String(str);
                    EditorText.Text = initialStrAll;
                    break;
                case 4:
                case 5:
                case 6:
                    editorRow = 1;
                    EditorHex.Visibility = Visibility.Visible;
                    
                    var streamB = await editFileParams.File.OpenAsync(FileAccessMode.Read);
                    var dataReaderB = new DataReader(streamB.GetInputStreamAt(0));
                    uint bytesLoadedB = await dataReaderB.LoadAsync((uint)(streamB.Size));

                    initialBytes = new byte[bytesLoadedB];
                    dataReaderB.ReadBytes(initialBytes);
                    EditorHex.Bytes = new byte[bytesLoadedB];
                    initialBytes.CopyTo(EditorHex.Bytes, 0);
                    EditorHex.Invalidate();
                    break;
            }
            EditorEditGrid.RowDefinitions[editorRow].Height = new GridLength(1, GridUnitType.Star);
        }

        private void EditorCanvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            cbm = CanvasBitmap.CreateFromBytes(sender, editFileParams.Bytes, editFileParams.Width, editFileParams.Height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            brush = new CanvasImageBrush(sender, cbm);
            brush.Interpolation = CanvasImageInterpolation.NearestNeighbor;
            sender.Width = editFileParams.Width;
            sender.Height = editFileParams.Height;
        }

        private void EditorCanvas_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(new Rect(new Point(), sender.Size), brush);
        }

        private void EditorCompare_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (EditorCanvas.Visibility == Visibility.Visible)
            {
                cbm = CanvasBitmap.CreateFromBytes(EditorCanvas, editFileParams.Bytes, editFileParams.Width, editFileParams.Height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
                brush = new CanvasImageBrush(EditorCanvas, cbm);
                brush.Interpolation = CanvasImageInterpolation.NearestNeighbor;
                EditorCanvas.Width = editFileParams.Width;
                EditorCanvas.Height = editFileParams.Height;
                EditorCanvas.Invalidate();
            }
            else
            {
                if (EditorText.Visibility == Visibility.Visible)
                {
                    currentStrAll = EditorText.Text;
                    EditorText.Text = initialStrAll;
                }
                else if (EditorHex.Visibility == Visibility.Visible)
                {
                    currentBytes = new Byte[EditorHex.Bytes.Length];
                    EditorHex.Bytes.CopyTo(currentBytes, 0);
                    EditorHex.Bytes = new Byte[initialBytes.Length];
                    initialBytes.CopyTo(EditorHex.Bytes, 0);
                    EditorHex.Invalidate();
                }
            }
        }

        private void EditorCompare_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (EditorCanvas.Visibility == Visibility.Visible)
            {
                cbm = CanvasBitmap.CreateFromBytes(EditorCanvas, lastDecodeResult.Bytes, lastDecodeResult.Width, lastDecodeResult.Height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
                brush = new CanvasImageBrush(EditorCanvas, cbm);
                brush.Interpolation = CanvasImageInterpolation.NearestNeighbor;
                EditorCanvas.Width = lastDecodeResult.Width;
                EditorCanvas.Height = lastDecodeResult.Height;
                EditorCanvas.Invalidate();
            }
            else
            {
                if (EditorText.Visibility == Visibility.Visible)
                {
                    EditorText.Text = currentStrAll;
                }
                else if (EditorHex.Visibility == Visibility.Visible)
                {
                    EditorHex.Bytes = new byte[currentBytes.Length];
                    currentBytes.CopyTo(EditorHex.Bytes, 0);
                    EditorHex.Invalidate();
                }
            }
        }
        
        private async void EditorPreview_Click(object sender, RoutedEventArgs e)
        {
            if (EditorCanvas.Visibility == Visibility.Collapsed)
            {
                var uiSettings = new Windows.UI.ViewManagement.UISettings();
                var color = uiSettings.GetColorValue(UIColorType.Accent);
                (sender as AppBarButton).Background = new SolidColorBrush(color);

                if (EditorText.Visibility == Visibility.Visible)
                {
                    lastDecodeResult = await decoder.decode(ASCIIEncoding.ASCII.GetBytes(EditorText.Text));
                }
                else if (EditorHex.Visibility == Visibility.Visible)
                {
                    lastDecodeResult = await decoder.decode(EditorHex.Bytes);
                }
                
                cbm = CanvasBitmap.CreateFromBytes(EditorCanvas, lastDecodeResult.Bytes, lastDecodeResult.Width, lastDecodeResult.Height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
                brush = new CanvasImageBrush(EditorCanvas, cbm);
                brush.Interpolation = CanvasImageInterpolation.NearestNeighbor;
                EditorCanvas.Width = editFileParams.Width;
                EditorCanvas.Height = EditorCanvas.Height;

                EditorCanvas.Visibility = Visibility.Visible;
                EditorEditGrid.RowDefinitions[editorRow].Height = new GridLength(0, GridUnitType.Pixel);
                EditorEditGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                EditorCanvas.Invalidate();
            }
            else
            {
                EditorCanvas.Visibility = Visibility.Collapsed;
                EditorEditGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Pixel);
                EditorEditGrid.RowDefinitions[editorRow].Height = new GridLength(1, GridUnitType.Star);
                (sender as AppBarButton).Background = new SolidColorBrush(Windows.UI.Colors.Black);
            }
        }

        private void EditorUndo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditorRedo_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private async void EditorSaveCopy_Click(object sender, RoutedEventArgs e)
        {
            EditorTopCommandBar.IsEnabled = false;
            EditorBottomCommandBar.IsEnabled = false;
            EditorRing.Visibility = Visibility.Visible;
            EditorRing.IsActive = true;
            StorageFolder folder = await editFileParams.File.GetParentAsync();
            StorageFile file = await folder.CreateFileAsync(editFileParams.File.Name, CreationCollisionOption.GenerateUniqueName);
            switch (editFileParams.Type)
            {
                case 1:
                case 2:
                case 3:
                    await FileIO.WriteTextAsync(file, EditorText.Text);
                    break;
                case 4:
                case 5:
                case 6:
                    await FileIO.WriteBytesAsync(file, EditorHex.Bytes);
                    break;
            }
            EditorRing.IsActive = false;
            EditorRing.Visibility = Visibility.Collapsed;
            EditorTopCommandBar.IsEnabled = true;
            EditorBottomCommandBar.IsEnabled = true;
        }

        private async void EditorSave_Click(object sender, RoutedEventArgs e)
        {
            EditorTopCommandBar.IsEnabled = false;
            EditorBottomCommandBar.IsEnabled = false;
            EditorRing.Visibility = Visibility.Visible;
            EditorRing.IsActive = true;
            switch (editFileParams.Type)
            {
                case 1:
                case 2:
                case 3:
                    await FileIO.WriteTextAsync(editFileParams.File, EditorText.Text);
                    break;
                case 4:
                case 5:
                case 6:
                    await FileIO.WriteBytesAsync(editFileParams.File, EditorHex.Bytes);
                    break;
            }
            EditorRing.IsActive = false;
            EditorRing.Visibility = Visibility.Collapsed;
            EditorTopCommandBar.IsEnabled = true;
            EditorBottomCommandBar.IsEnabled = true;
        }

        private void EditorCancel_Click(object sender, RoutedEventArgs e)
        {
            ExitPopup.IsOpen = !ExitPopup.IsOpen;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= this.Page_Unloaded;

            this.EditorCanvas.CreateResources -= this.EditorCanvas_CreateResources;
            this.EditorCanvas.Draw -= this.EditorCanvas_Draw;

            this.EditorPreviewTop.Click -= this.EditorPreview_Click;
            this.EditorUndoTop.Click -= this.EditorUndo_Click;
            this.EditorRedoTop.Click -= this.EditorRedo_Click;
            this.EditorSaveCopyTop.Click -= this.EditorSaveCopy_Click;
            this.EditorSaveTop.Click -= this.EditorSave_Click;
            this.EditorCancelTop.Click -= this.EditorCancel_Click;

            this.EditorPreviewBottom.Click -= this.EditorPreview_Click;
            this.EditorUndoBottom.Click -= this.EditorUndo_Click;
            this.EditorRedoBottom.Click -= this.EditorRedo_Click;
            this.EditorSaveCopyBottom.Click -= this.EditorSaveCopy_Click;
            this.EditorSaveBottom.Click -= this.EditorSave_Click;
            this.EditorCancelBottom.Click -= this.EditorCancel_Click;

            this.EditorCompareTop.PointerPressed -= this.EditorCompare_PointerPressed;
            this.EditorCompareTop.PointerReleased -= this.EditorCompare_PointerReleased;
            this.EditorCompareBottom.PointerPressed -= this.EditorCompare_PointerPressed;
            this.EditorCompareBottom.PointerReleased -= this.EditorCompare_PointerReleased;

            this.MobileTrigger.Detach();
            this.DesktopTrigger.Detach();
            GC.Collect();
        }
    }
}
