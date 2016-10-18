using Portable_Anymap_Viewer.Classes;
using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
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
        private StorageFolder outputFolder = null;
        private List<String> inputFiles = new List<String>();
        private List<String> outputFiles = new List<String>();

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            if (e.NavigationMode == NavigationMode.New)
            {
                files = e.Parameter as IReadOnlyList<StorageFile>;
                outputFolder = await files.First().GetParentAsync();
                if (outputFolder != null)
                {
                    OutputFolderPathTop.Text = outputFolder.Path;
                    OutputFolderPathBottom.Text = outputFolder.Path;
                }
                foreach (StorageFile file in files)
                {
                    TextBlock inputTextBlock = new TextBlock();
                    inputTextBlock.Text = file.Name;
                    TextBlock outputTextBlock = new TextBlock();
                    outputTextBlock.Text = file.DisplayName + outputExtension;
                    outputTextBlock.Visibility = Visibility.Collapsed;
                    InputFilesList.Items.Add(inputTextBlock);
                    OutputFilesList.Items.Add(outputTextBlock);
                    inputFiles.Add(inputTextBlock.Text);
                    outputFiles.Add(outputTextBlock.Text);
                }
                FileProgressBar.Value = 0;
                FileProgressBar.Minimum = 0;
                FileProgressBar.Maximum = files.Count;
            }
            else
            {
                ConverterTopCommandBar.IsEnabled = false;
                ConverterBottomCommandBar.IsEnabled = false;
            }
        }

        private async void Convert_Click(object sender, RoutedEventArgs e)
        {
            ConverterTopCommandBar.IsEnabled = false;
            ConverterBottomCommandBar.IsEnabled = false;
            this.FileProgressBar.Visibility = Visibility.Visible;
            this.IsBinary.Visibility = Visibility.Collapsed;
            this.FileTypeCombo.Visibility = Visibility.Collapsed;
            this.MaxPixelValue.Visibility = Visibility.Collapsed;
            this.NameCollisionCombo.Visibility = Visibility.Collapsed;
            if (outputFolder == null)
            {
                await this.ChangeFolder();
            }

            var maxPixelValue = Convert.ToUInt32(MaxPixelValue.Text);
            var type = (AnymapType)FileTypeCombo.SelectedIndex;
            var isBinary = IsBinary.IsOn;
            var collisionOption = (CreationCollisionOption)NameCollisionCombo.SelectedIndex;
            Parallel.For(0, InputFilesList.Items.Count, async (i, state) =>
            {
                using (var stream = await RandomAccessStreamReference.CreateFromFile(files[i]).OpenReadAsync())
                {
                    var imageDecoder = await BitmapDecoder.CreateAsync(stream);
                    
                    AnymapProperties properties = new AnymapProperties();
                    properties.AnymapType = type;
                    properties.Width = imageDecoder.OrientedPixelWidth;
                    properties.Height = imageDecoder.OrientedPixelHeight;
                    properties.MaxValue = maxPixelValue;
                    properties.StreamPosition = 0;
                    properties.BytesPerColor = maxPixelValue > 255 ? (UInt32)2 : (UInt32)1;
                    try
                    {
                        StorageFile newFile = await outputFolder.CreateFileAsync(
                            outputFiles[i],
                            collisionOption
                        );
                        if (isBinary)
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

                        await CoreApplication.GetCurrentView().Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            (this.InputFilesList.Items[i] as TextBlock).Visibility = Visibility.Collapsed;
                            (this.OutputFilesList.Items[i] as TextBlock).Visibility = Visibility.Visible;
                            this.FileProgressBar.Value = this.FileProgressBar.Value + 1;
                        });
                    }
                    catch (Exception ex)
                    {
                        (this.InputFilesList.Items[i] as TextBlock).Visibility = Visibility.Collapsed;
                        this.FileProgressBar.Value = this.FileProgressBar.Value + 1;
                    }
                }
            });
        }

        private async void ChangeFolder_Click(object sender, RoutedEventArgs e)
        {
            await ChangeFolder();
        }

        private async void Rate_Click(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri(string.Format("ms-windows-store:REVIEW?PFN={0}", Windows.ApplicationModel.Package.Current.Id.FamilyName)));
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            this.Split.IsPaneOpen = !this.Split.IsPaneOpen;
        }

        private async Task ChangeFolder()
        {
            FolderPicker folderPicker = new FolderPicker();
            folderPicker.FileTypeFilter.Add(".pbm");
            folderPicker.FileTypeFilter.Add(".pgm");
            folderPicker.FileTypeFilter.Add(".ppm");
            folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
            outputFolder = await folderPicker.PickSingleFolderAsync();
            if (outputFolder != null)
            {
                StorageApplicationPermissions.FutureAccessList.Add(outputFolder);
                OutputFolderPathTop.Text = OutputFolderPathBottom.Text = outputFolder.Path;
            }
        }

        private void FileTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MaxPixelValue != null)
            {
                switch (((sender as ComboBox).SelectedValue as ComboBoxItem).Content as String)
                {
                    case "Bitmap":
                        outputExtension = ".pbm";
                        this.MaxPixelValue.Header = "Threshold level (0-255)";
                        this.MaxPixelValue.Text = "127";
                        break;
                    case "Graymap":
                        outputExtension = ".pgm";
                        this.MaxPixelValue.Header = "Maximum pixel value (0-255)";
                        this.MaxPixelValue.Text = "255";
                        break;
                    case "Pixmap":
                        outputExtension = ".ppm";
                        this.MaxPixelValue.Header = "Maximum pixel value (0-255)";
                        this.MaxPixelValue.Text = "255";
                        break;
                    default:
                        outputExtension = "";
                        break;
                }
                for (int i = 0; i < OutputFilesList?.Items.Count; ++i)
                {
                    (OutputFilesList.Items[i] as TextBlock).Text = outputFiles[i] = files[i].DisplayName + outputExtension;
                }
            }
        }

        private void MaxPixelValue_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (VirtualKey.Number0 <= e.Key && e.Key <= VirtualKey.Number9)
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
            else if (e.Key == VirtualKey.Delete || e.Key == VirtualKey.Back)
            {
                return;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void ProgressBar_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var bar = (sender as ProgressBar);
            if (e.NewValue == bar.Maximum)
            {
                bar.Visibility = Visibility.Collapsed;
            }
            this.FileListPivot.SelectedIndex = 1;
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= this.Page_Unloaded;

            this.FileTypeCombo.SelectionChanged -= this.FileTypeCombo_SelectionChanged;
            this.MaxPixelValue.KeyDown -= this.MaxPixelValue_KeyDown;
            this.FileProgressBar.ValueChanged -= this.ProgressBar_ValueChanged;

            this.ChangeFolderTop.Click -= this.ChangeFolder_Click;
            this.ConvertTop.Click -= this.Convert_Click;
            this.RateTop.Click -= this.Rate_Click;
            this.AboutTop.Click -= this.About_Click;

            this.ChangeFolderBottom.Click -= this.ChangeFolder_Click;
            this.ConvertBottom.Click -= this.Convert_Click;
            this.RateBottom.Click -= this.Rate_Click;
            this.AboutBottom.Click -= this.About_Click;

            this.MobileTrigger.Detach();
            this.DesktopTrigger.Detach();
            GC.Collect();
        }
    }
}
