using Portable_Anymap_Viewer.Classes;
using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
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
                    TextBlock inputTextBlock = new TextBlock()
                    {
                        Text = file.Name
                    };
                    TextBlock outputTextBlock = new TextBlock()
                    {
                        Text = file.DisplayName + outputExtension,
                        Visibility = Visibility.Collapsed
                    };
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
            this.Depth8_16.Visibility = Visibility.Collapsed;
            this.FileTypeCombo.Visibility = Visibility.Collapsed;
            this.MaxPixelValue.Visibility = Visibility.Collapsed;
            this.ThresholdLevelTxt8.Visibility = Visibility.Collapsed;
            this.ThresholdLevelTxt16.Visibility = Visibility.Collapsed;
            this.NameCollisionCombo.Visibility = Visibility.Collapsed;
            if (outputFolder == null)
            {
                await this.ChangeFolder();
            }
            var anymapType = (AnymapType)FileTypeCombo.SelectedIndex;
            var maxValue = Convert.ToUInt32(MaxPixelValue.Text);
            var threshold8 = Convert.ToByte(ThresholdLevelTxt8.Text);
            var threshold16 = Convert.ToUInt16(ThresholdLevelTxt16.Text);
            var bytesPerColor = Depth8_16.IsOn ? 2u : 1u;
            var creationCollisionOption = (CreationCollisionOption)NameCollisionCombo.SelectedIndex;
            var filesNum = InputFilesList.Items.Count;
            var isBinary = this.IsBinary.IsOn;
            Parallel.For(0, filesNum, async (i, state) =>
            {
                using (var stream = await RandomAccessStreamReference.CreateFromFile(files[i]).OpenReadAsync())
                {
                    var imageDecoder = await BitmapDecoder.CreateAsync(stream);

                    AnymapProperties properties = new AnymapProperties()
                    {
                        AnymapType = anymapType,
                        Width = imageDecoder.OrientedPixelWidth,
                        Height = imageDecoder.OrientedPixelHeight,
                        MaxValue = maxValue,
                        Threshold8 = threshold8,
                        Threshold16 = threshold16,
                        BytesPerColor = bytesPerColor,
                        StreamPosition = 0,
                    };
                    try
                    {
                        StorageFile newFile = await outputFolder.CreateFileAsync(outputFiles[i], creationCollisionOption);
                        if (isBinary)
                        {
                            byte[] anymapBytes = await anymapEncoder.EncodeBinary(imageDecoder, properties);
                            await FileIO.WriteBytesAsync(newFile, anymapBytes);
                        }
                        else
                        {
                            await FileIO.WriteTextAsync(newFile, await anymapEncoder.EncodeText(imageDecoder, properties));
                        }

                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            (this.InputFilesList.Items[i] as TextBlock).Visibility = Visibility.Collapsed;
                            (this.OutputFilesList.Items[i] as TextBlock).Visibility = Visibility.Visible;
                            this.FileProgressBar.Value = this.FileProgressBar.Value + 1;
                        });
                    }
                    catch (Exception ex)
                    {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                        {
                            (this.InputFilesList.Items[i] as TextBlock).Visibility = Visibility.Collapsed;
                            this.FileProgressBar.Value = this.FileProgressBar.Value + 1;
                            //ContentDialog convert
                        });
                        Debug.WriteLine(ex.Message);
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

        private void Depth8_16_Toggled(object sender, RoutedEventArgs e)
        {
            this.UpdateTxtMaxPixelValue();
        }

        private void UpdateTxtMaxPixelValue()
        {
            var loader = new ResourceLoader();
            if (this.Depth8_16.IsOn)
            {
                this.MaxPixelValue.Text = "65535";
                this.MaxPixelValue.Header = loader.GetString("TxtMaxPixelValue16");
                this.MaxPixelValue.MaxLength = 5;
            }
            else
            {
                this.MaxPixelValue.Text = "255";
                this.MaxPixelValue.Header = loader.GetString("TxtMaxPixelValue8");
                this.MaxPixelValue.MaxLength = 3;
            }
        }

        private void FileTypeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MaxPixelValue != null)
            {
                var loader = new ResourceLoader();
                switch (((sender as ComboBox).SelectedValue as ComboBoxItem).Content as String)
                {
                    case "Bitmap":
                        outputExtension = ".pbm";
                        this.Depth8_16.Visibility = Visibility.Collapsed;
                        this.MaxPixelValue.Visibility = Visibility.Collapsed;
                        this.ThresholdLevelTxt8.Visibility = Visibility.Visible;
                        this.ThresholdLevelTxt16.Visibility = Visibility.Visible;
                        break;
                    case "Graymap":
                        outputExtension = ".pgm";
                        this.UpdateTxtMaxPixelValue();
                        this.Depth8_16.Visibility = Visibility.Visible;
                        this.MaxPixelValue.Visibility = Visibility.Visible;
                        this.ThresholdLevelTxt8.Visibility = Visibility.Collapsed;
                        this.ThresholdLevelTxt16.Visibility = Visibility.Collapsed;
                        break;
                    case "Pixmap":
                        outputExtension = ".ppm";
                        this.UpdateTxtMaxPixelValue();
                        this.Depth8_16.Visibility = Visibility.Visible;
                        this.MaxPixelValue.Visibility = Visibility.Visible;
                        this.ThresholdLevelTxt8.Visibility = Visibility.Collapsed;
                        this.ThresholdLevelTxt16.Visibility = Visibility.Collapsed;
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
                    if (this.Depth8_16.IsOn)
                    {
                        UInt16 i = Convert.ToUInt16(tb.Text + Convert.ToChar(e.Key));
                    }
                    else
                    {
                        Byte i = Convert.ToByte(tb.Text + Convert.ToChar(e.Key));
                    }
                }
                catch (FormatException)
                {
                    e.Handled = true;
                }
                catch (OverflowException)
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

        private void ThresholdLevelTxt8_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (VirtualKey.Number0 <= e.Key && e.Key <= VirtualKey.Number9)
            {
                String str = tb.Text + e.Key.ToString();
                try
                {
                    Byte i = Convert.ToByte(tb.Text + Convert.ToChar(e.Key));
                }
                catch (FormatException)
                {
                    e.Handled = true;
                }
                catch (OverflowException)
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

        private void ThresholdLevelTxt16_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (VirtualKey.Number0 <= e.Key && e.Key <= VirtualKey.Number9)
            {
                String str = tb.Text + e.Key.ToString();
                try
                {
                    UInt16 i = Convert.ToUInt16(tb.Text + Convert.ToChar(e.Key));
                }
                catch (FormatException)
                {
                    e.Handled = true;
                }
                catch (OverflowException)
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

        private void MaxPixelValue_LostFocus(object sender, RoutedEventArgs e)
        {
            var txt = sender as TextBox;
            if (txt.Text == "")
            {
                if (this.Depth8_16.IsOn)
                {
                    txt.Text = "65535";
                }
                else
                {
                    txt.Text = "255";
                }
            }
        }

        private void ThresholdLevelTxt8_LostFocus(object sender, RoutedEventArgs e)
        {
            var txt = sender as TextBox;
            if (txt.Text == "")
            {
                txt.Text = "127";
            }
        }

        private void ThresholdLevelTxt16_LostFocus(object sender, RoutedEventArgs e)
        {
            var txt = sender as TextBox;
            if (txt.Text == "")
            {
                txt.Text = "32767";
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

            this.Depth8_16.Toggled -= this.Depth8_16_Toggled;
            this.FileTypeCombo.SelectionChanged -= this.FileTypeCombo_SelectionChanged;
            this.MaxPixelValue.KeyDown -= this.MaxPixelValue_KeyDown;
            this.ThresholdLevelTxt8.KeyDown -= this.ThresholdLevelTxt8_KeyDown;
            this.ThresholdLevelTxt16.KeyDown -= this.ThresholdLevelTxt16_KeyDown;
            this.MaxPixelValue.LostFocus -= this.MaxPixelValue_LostFocus;
            this.ThresholdLevelTxt8.LostFocus -= this.ThresholdLevelTxt8_LostFocus;
            this.ThresholdLevelTxt16.LostFocus -= this.ThresholdLevelTxt16_LostFocus;
            this.FileProgressBar.ValueChanged -= this.ProgressBar_ValueChanged;

            this.ChangeFolderTop.Click -= this.ChangeFolder_Click;
            this.ConvertTop.Click -= this.Convert_Click;

            this.ChangeFolderBottom.Click -= this.ChangeFolder_Click;
            this.ConvertBottom.Click -= this.Convert_Click;

            this.MobileTrigger.Detach();
            this.DesktopTrigger.Detach();
            GC.Collect();
        }
    }
}
