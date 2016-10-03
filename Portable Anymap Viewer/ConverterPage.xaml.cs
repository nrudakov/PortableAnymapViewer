using Portable_Anymap_Viewer.Classes;
using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// This page provides settings for jpeg/png->anymap convertion
    /// </summary>
    public sealed partial class ConverterPage : Page
    {
        public ConverterPage()
        {
            this.InitializeComponent();
        }

        private IReadOnlyList<StorageFile> files;
        private AnymapEncoder anymapEncoder = new AnymapEncoder();
        private String outputExtension = ".ppm";

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            files = e.Parameter as IReadOnlyList<StorageFile>;
            StorageFolder folder = await files.First().GetParentAsync();
            OutputFolderPathTop.Text = folder.Path;
            OutputFolderPathBottom.Text = folder.Path;
            Double maxHalfWidth = Double.MinValue;
            foreach (StorageFile file in files)
            {
                //ConverterFilename filename = new ConverterFilename(file.Name, file.DisplayName + ".ppm");
                //FilenameList.Items.Add(filename);
                TextBlock inputTextBlock = new TextBlock();
                inputTextBlock.Text = file.Name;
                TextBlock outputTextBlock = new TextBlock();
                outputTextBlock.Text = file.DisplayName + outputExtension;
                InputFilesList.Items.Add(inputTextBlock);
                OutputFilesList.Items.Add(outputTextBlock);
            }
            //FilenameList.UpdateLayout();
            //foreach (ConverterFilename filename in FilenameList.Items)
            //{
            //    Double halfWidth = filename.GetHalfWidth();
            //    if (halfWidth > maxHalfWidth)
            //    {
            //        maxHalfWidth = halfWidth;
            //    }
            //}
            //maxHalfWidth += 10;
            //foreach (ConverterFilename filename in FilenameList.Items)
            //{
            //    filename.SetHalfWidth(maxHalfWidth);
            //}
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            UInt32 maxPixelValue = Convert.ToUInt32(MaxPixelValue.Text);
            AnymapType type = (AnymapType)FileTypeCombo.SelectedIndex;
            Parallel.For(0, InputFilesList.Items.Count, async (i, state) =>
            {
                using (var stream = await RandomAccessStreamReference.CreateFromFile(files[i]).OpenReadAsync())
                {
                    Debug.WriteLine("{0}", i);

                    var imageDecoder = await BitmapDecoder.CreateAsync(stream);
                    
                    AnymapProperties properties = new AnymapProperties();
                    properties.AnymapType = type;
                    properties.Width = imageDecoder.OrientedPixelWidth;
                    properties.Height = imageDecoder.OrientedPixelHeight;
                    properties.MaxValue = maxPixelValue;
                    properties.StreamPosition = 0;
                    properties.BytesPerColor = maxPixelValue > 255 ? (UInt32)2 : (UInt32)1;
                    StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(OutputFolderPathTop.Text);
                    StorageFile newFile = await folder.CreateFileAsync((OutputFilesList.Items[i] as TextBlock).Text, (CreationCollisionOption)NameCollisionCombo.SelectedIndex);
                    if (IsBinary.IsOn)
                    {
                        byte[] anymapBytes = await anymapEncoder.encodeBinary(imageDecoder, properties);
                        await FileIO.WriteBytesAsync(
                            newFile,
                            anymapBytes
                        );
                    }
                    else
                    {
                        await FileIO.WriteTextAsync(
                            newFile,
                            await anymapEncoder.encodeText(imageDecoder, properties)
                        );
                    }

                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    {
                        (this.InputFilesList.Items[i] as TextBlock).Visibility = Visibility.Collapsed;
                        (this.OutputFilesList.Items[i] as TextBlock).Visibility = Visibility.Visible;
                    });
                }
            });
        }

        private void ChangeFolder_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConverterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MaxPixelValue != null)
            {
                switch (((sender as ComboBox).SelectedValue as ComboBoxItem).Content as String)
                {
                    case "Bitmap":
                        outputExtension = ".pbm";
                        this.MaxPixelValue.Visibility = Visibility.Collapsed;
                        break;
                    case "Graymap":
                        outputExtension = ".pgm";
                        this.MaxPixelValue.Visibility = Visibility.Visible;
                        break;
                    case "Pixmap":
                        outputExtension = ".ppm";
                        this.MaxPixelValue.Visibility = Visibility.Visible;
                        break;
                    default:
                        outputExtension = "";
                        break;
                }
                for (int i = 0; i < OutputFilesList?.Items.Count; ++i)
                {
                    (OutputFilesList.Items[i] as TextBlock).Text = files[i].DisplayName + outputExtension;
                }
            }
        }

        private void MaxPixelValue_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (Windows.System.VirtualKey.Number0 <= e.Key &&
                e.Key <= Windows.System.VirtualKey.Number9)
            {
                String str = tb.Text + e.Key.ToString();
                try
                {
                    Byte i = Convert.ToByte(tb.Text + Convert.ToChar(e.Key));
                }
                catch (FormatException ex)
                {
                    e.Handled = true;
                }
                catch (OverflowException ex)
                {
                    e.Handled = true;
                }
            }
            else if (e.Key == Windows.System.VirtualKey.Delete || e.Key == Windows.System.VirtualKey.Back)
            {
                return;
            }
            else
            {
                e.Handled = true;
            }
        }
    }
}
