using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Portable_Anymap_Viewer.Classes;
using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

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
            Window.Current.CoreWindow.KeyDown += this.CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
        }

        AnymapDecoder decoder;
        EditFileParams editFileParams;
        CanvasBitmap cbm;
        CanvasImageBrush brush;
        DecodeResult lastDecodeResult;
        String initialStrAll;
        String currentStrAll;
        byte[] initialBytes;
        byte[] currentBytes;
        int editorRow = -1;
        
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            editFileParams = (e.Parameter as EditFileParams);
            EditorSaveCopyTop.Visibility = EditorSaveBottom.Visibility = (((byte)editFileParams.SaveMode & (byte)EditFileSaveMode.SaveCopy) == 0x00) ? Visibility.Collapsed : Visibility.Visible;
            EditorSaveTop.Visibility = EditorSaveBottom.Visibility = (((byte)editFileParams.SaveMode & (byte)EditFileSaveMode.Save) == 0x00) ? Visibility.Collapsed : Visibility.Visible;
            EditorSaveAsTop.Visibility = EditorSaveAsBottom.Visibility = (((byte)editFileParams.SaveMode & (byte)EditFileSaveMode.SaveAs) == 0x00) ? Visibility.Collapsed : Visibility.Visible;


            if (editFileParams.File != null)
            {
                EditorFilenameTop.Text = EditorFilenameBottom.Text = editFileParams.File.Name;
            }
            
            switch (editFileParams.Type)
            {
                case 1:
                case 2:
                case 3:
                    editorRow = 0;
                    EditorText.Visibility = Visibility.Visible;
                    EditorInputModeTop.Visibility = Visibility.Collapsed;
                    EditorInputModeBottom.Visibility = Visibility.Collapsed;

                    if (editFileParams.File != null)
                    {
                        var streamT = await editFileParams.File.OpenAsync(FileAccessMode.Read);
                        var dataReaderT = new DataReader(streamT.GetInputStreamAt(0));
                        uint bytesLoadedT = await dataReaderT.LoadAsync((uint)(streamT.Size));

                        initialStrAll = dataReaderT.ReadString(bytesLoadedT);
                    }
                    else
                    {
                        initialStrAll = "";
                    }
                    //char[] str = new char[bytesLoadedT];
                    //initialStrAll. CopyTo(0, str, 0, initialStrAll.Length);
                    //currentStrAll = new String(str);
                    EditorText.Text = initialStrAll;
                    break;
                case 4:
                case 5:
                case 6:
                    editorRow = 1;
                    EditorHex.Visibility = Visibility.Visible;
                    
                    if (editFileParams.File != null)
                    {
                        var streamB = await editFileParams.File.OpenAsync(FileAccessMode.Read);
                        var dataReaderB = new DataReader(streamB.GetInputStreamAt(0));
                        uint bytesLoadedB = await dataReaderB.LoadAsync((uint)(streamB.Size));

                        initialBytes = new byte[bytesLoadedB];
                        dataReaderB.ReadBytes(initialBytes);
                        EditorHex.Bytes = new byte[bytesLoadedB];
                        initialBytes.CopyTo(EditorHex.Bytes, 0);
                    }
                    else
                    {
                        initialBytes = new Byte[0];
                        EditorHex.Bytes = new Byte[0];
                    }
                    EditorHex.Invalidate();
                    DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
                    break;
            }
            EditorEditGrid.RowDefinitions[editorRow].Height = new GridLength(1, GridUnitType.Star);
            //var currentView = SystemNavigationManager.GetForCurrentView();
            //currentView.BackRequested += CurrentView_BackRequested;
        }

        public bool IsNeedToSave()
        {
            switch (this.editorRow)
            {
                case 0:
                    return !EditorText.Text.SequenceEqual(initialStrAll);
                case 1:
                    return !EditorHex.Bytes.SequenceEqual(initialBytes);
                default:
                    return false;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            DisplayInformation.AutoRotationPreferences = DisplayOrientations.None;
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

        private void EditorInputMode_Checked(object sender, RoutedEventArgs e)
        {
            var loader = new ResourceLoader();
            this.EditorInputModeTop.Label = loader.GetString("InputModeInsert");
            this.EditorInputModeBottom.Label = loader.GetString("InputModeInsert");
            (this.EditorInputModeTop.Icon as FontIcon).Glyph = "\u0049";
            this.EditorHex.IsInputModeInsert = true;
        }

        private void EditorInputMode_Unchecked(object sender, RoutedEventArgs e)
        {
            var loader = new ResourceLoader();
            this.EditorInputModeTop.Label = loader.GetString("InputModeOverwrite");
            this.EditorInputModeBottom.Label = loader.GetString("InputModeOverwrite");
            (this.EditorInputModeTop.Icon as FontIcon).Glyph = "\u004F";
            this.EditorHex.IsInputModeInsert = false;
        }

        private void EditorCompare_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.onEditorComparePointerPressed();
        }

        private void onEditorComparePointerPressed()
        {
            EditorCompareTop.IsChecked = true;
            EditorCompareBottom.IsChecked = true;
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
            this.onEditorComparePointerReleased();
        }

        private void onEditorComparePointerReleased()
        {
            EditorCompareTop.IsChecked = false;
            EditorCompareBottom.IsChecked = false;
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

        private async void EditorPreview_Checked(object sender, RoutedEventArgs e)
        {
            if (EditorText.Visibility == Visibility.Visible)
            {
                try
                {
                    lastDecodeResult = await decoder.decode(ASCIIEncoding.ASCII.GetBytes(EditorText.Text));
                }
                catch
                {
                    var loader = new ResourceLoader();
                    var warningTilte = loader.GetString("DecodeFailureTitle");
                    var warningMesage = loader.GetString("DecodeFailureMessage");
                    var ok = loader.GetString("Ok");
                    MessageDialog decodeFailedDialog = new MessageDialog(warningMesage, warningTilte);
                    decodeFailedDialog.Commands.Add(new UICommand(ok));
                    decodeFailedDialog.DefaultCommandIndex = 0;
                    await decodeFailedDialog.ShowAsync();
                    return;
                }
            }
            else if (EditorHex.Visibility == Visibility.Visible)
            {
                try
                {
                    lastDecodeResult = await decoder.decode(EditorHex.Bytes);
                }
                catch (Exception ex)
                {
                    var loader = new ResourceLoader();
                    var warningTilte = loader.GetString("DecodeFailureTitle");
                    var warningMesage = loader.GetString("DecodeFailureMessage");
                    var ok = loader.GetString("Ok");
                    MessageDialog decodeFailedDialog = new MessageDialog(warningMesage, warningTilte);
                    decodeFailedDialog.Commands.Add(new UICommand(ok));
                    decodeFailedDialog.DefaultCommandIndex = 0;
                    await decodeFailedDialog.ShowAsync();
                    return;
                }
            }
            cbm = CanvasBitmap.CreateFromBytes(EditorCanvas, lastDecodeResult.Bytes, lastDecodeResult.Width, lastDecodeResult.Height, DirectXPixelFormat.B8G8R8A8UIntNormalized);
            brush = new CanvasImageBrush(EditorCanvas, cbm);
            brush.Interpolation = CanvasImageInterpolation.NearestNeighbor;
            EditorCanvas.Width = lastDecodeResult.Width;
            EditorCanvas.Height = lastDecodeResult.Height;

            EditorCanvas.Visibility = Visibility.Visible;
            EditorEditGrid.RowDefinitions[editorRow].Height = new GridLength(0, GridUnitType.Pixel);
            EditorEditGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
            EditorCanvas.Invalidate();
        }

        private void EditorPreview_Unchecked(object sender, RoutedEventArgs e)
        {
            EditorCanvas.Visibility = Visibility.Collapsed;
            EditorEditGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Pixel);
            EditorEditGrid.RowDefinitions[editorRow].Height = new GridLength(1, GridUnitType.Star);
        }

        //private void EditorUndo_Click(object sender, RoutedEventArgs e)
        //{

        //}

        //private void EditorRedo_Click(object sender, RoutedEventArgs e)
        //{
            
        //}

        private async void onSaveCopyClick()
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
            (Window.Current.Content as Frame).GoBack();
        }
        private void EditorSaveCopy_Click(object sender, RoutedEventArgs e)
        {
            onSaveCopyClick();
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
            (Window.Current.Content as Frame).GoBack();
        }

        private async void EditorSaveAs_Click(object sender, RoutedEventArgs e)
        {
            var saveAsPicker = new FileSavePicker();
            saveAsPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            saveAsPicker.SuggestedFileName = "New Anymap";
            saveAsPicker.FileTypeChoices.Add("Bitmap", new List<String>() { ".pbm"});
            saveAsPicker.FileTypeChoices.Add("Graymap", new List<String>() { ".pgm" });
            saveAsPicker.FileTypeChoices.Add("Pixmap", new List<String>() { ".ppm" });
            var file = await saveAsPicker.PickSaveFileAsync();
            if (file != null)
            {
                CachedFileManager.DeferUpdates(file);
                if (this.EditorText.Visibility == Visibility.Visible)
                {
                    await FileIO.WriteTextAsync(file, EditorText.Text);
                }
                else
                {
                    await FileIO.WriteBytesAsync(file, EditorHex.Bytes);
                }
                var status = await CachedFileManager.CompleteUpdatesAsync(file);
                (Window.Current.Content as Frame).GoBack();
            }
        }

        private async void EditorCancel_Click(object sender, RoutedEventArgs e)
        {
            //ExitPopup.IsOpen = !ExitPopup.IsOpen;
            if (this.IsNeedToSave())
            {
                var loader = new ResourceLoader();
                var warningTilte = loader.GetString("EditorExitTitle");
                var warningMesage = loader.GetString("EditorExitMessage");
                var yes = loader.GetString("Yes");
                var no = loader.GetString("No");
                MessageDialog goBackConfirmation = new MessageDialog(warningMesage, warningTilte);
                goBackConfirmation.Commands.Add(new UICommand(yes));
                goBackConfirmation.Commands.Add(new UICommand(no));
                goBackConfirmation.DefaultCommandIndex = 1;
                var selectedCommand = await goBackConfirmation.ShowAsync();
                if (selectedCommand.Label == yes)
                {
                    (Window.Current.Content as Frame).GoBack();
                }
            }
            else
            {
                (Window.Current.Content as Frame).GoBack();
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

        private bool isCtrlPressed = false;
        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Control:
                    this.isCtrlPressed = true;
                    break;
                case VirtualKey.Escape:
                    EditorCancel_Click(this, new RoutedEventArgs());
                    break;
                case VirtualKey.Insert:
                    this.EditorInputModeTop.IsChecked = !this.EditorInputModeTop.IsChecked;
                    this.EditorInputModeBottom.IsChecked = !this.EditorInputModeBottom.IsChecked;
                    this.EditorHex.IsInputModeInsert = !this.EditorHex.IsInputModeInsert;
                    break;
                case VirtualKey.S:
                    if (this.isCtrlPressed)
                    {
                        this.onSaveCopyClick();
                    }
                    break;
            }
        }

        private void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs args)
        {
            switch (args.VirtualKey)
            {
                case VirtualKey.Control:
                    this.isCtrlPressed = false;
                    break;
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= this.Page_Unloaded;
            Window.Current.CoreWindow.KeyDown -= this.CoreWindow_KeyDown;
            Window.Current.CoreWindow.KeyUp -= this.CoreWindow_KeyUp;

            this.EditorHex.Detach();

            this.EditorCanvas.CreateResources -= this.EditorCanvas_CreateResources;
            this.EditorCanvas.Draw -= this.EditorCanvas_Draw;
            this.Key0.Click -= this.Key0_Click;
            this.Key1.Click -= this.Key1_Click;
            this.Key2.Click -= this.Key2_Click;
            this.Key3.Click -= this.Key3_Click;
            this.Key4.Click -= this.Key4_Click;
            this.Key5.Click -= this.Key5_Click;
            this.Key6.Click -= this.Key6_Click;
            this.Key7.Click -= this.Key7_Click;
            this.Key8.Click -= this.Key8_Click;
            this.Key9.Click -= this.Key9_Click;
            this.KeyA.Click -= this.KeyA_Click;
            this.KeyB.Click -= this.KeyB_Click;
            this.KeyC.Click -= this.KeyC_Click;
            this.KeyD.Click -= this.KeyD_Click;
            this.KeyE.Click -= this.KeyE_Click;
            this.KeyF.Click -= this.KeyF_Click;

            this.EditorInputModeTop.Checked -= this.EditorInputMode_Checked;
            this.EditorInputModeTop.Unchecked -= this.EditorInputMode_Unchecked;
            this.EditorPreviewTop.Checked -= this.EditorPreview_Checked;
            this.EditorPreviewTop.Unchecked -= this.EditorPreview_Unchecked;
            //this.EditorUndoTop.Click -= this.EditorUndo_Click;
            //this.EditorRedoTop.Click -= this.EditorRedo_Click;
            this.EditorSaveCopyTop.Click -= this.EditorSaveCopy_Click;
            this.EditorSaveTop.Click -= this.EditorSave_Click;
            this.EditorCancelTop.Click -= this.EditorCancel_Click;
            //this.RateTop.Click -= this.Rate_Click;
            //this.AboutTop.Click -= this.About_Click;

            this.EditorInputModeBottom.Checked -= this.EditorInputMode_Checked;
            this.EditorInputModeBottom.Unchecked -= this.EditorInputMode_Unchecked;
            this.EditorPreviewBottom.Checked -= this.EditorPreview_Checked;
            this.EditorPreviewBottom.Unchecked -= this.EditorPreview_Unchecked;
            //this.EditorUndoBottom.Click -= this.EditorUndo_Click;
            //this.EditorRedoBottom.Click -= this.EditorRedo_Click;
            this.EditorSaveCopyBottom.Click -= this.EditorSaveCopy_Click;
            this.EditorSaveBottom.Click -= this.EditorSave_Click;
            this.EditorCancelBottom.Click -= this.EditorCancel_Click;
            //this.RateBottom.Click -= this.Rate_Click;
            //this.AboutBottom.Click -= this.About_Click;

            this.EditorCompareTop.PointerPressed -= this.EditorCompare_PointerPressed;
            this.EditorCompareTop.PointerReleased -= this.EditorCompare_PointerReleased;
            this.EditorCompareBottom.PointerPressed -= this.EditorCompare_PointerPressed;
            this.EditorCompareBottom.PointerReleased -= this.EditorCompare_PointerReleased;

            this.MobileTrigger.Detach();
            this.DesktopTrigger.Detach();
            GC.Collect();
        }

        private void Key0_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number0);
        }

        private void Key1_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number1);
        }

        private void Key2_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number2);
        }

        private void Key3_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number3);
        }

        private void Key4_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number4);
        }

        private void Key5_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number5);
        }

        private void Key6_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number6);
        }

        private void Key7_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number7);
        }

        private void Key8_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number8);
        }

        private void Key9_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.Number9);
        }

        private void KeyA_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.A);
        }

        private void KeyB_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.B);
        }

        private void KeyC_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.C);
        }

        private void KeyD_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.D);
        }

        private void KeyE_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.E);
        }

        private void KeyF_Click(object sender, RoutedEventArgs e)
        {
            this.EditorHex.ReceiveKey(VirtualKey.F);
        }
    }
}
