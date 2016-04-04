using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
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
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            OpenFileParams openFileParams = e.Parameter as OpenFileParams;
            flipView.Visibility = Visibility.Collapsed;
            int fileId = 0;
            foreach (StorageFile file in openFileParams.FileList)
            {
                AnymapDecoder anyDecoder = new AnymapDecoder();
                DecodeResult result = await anyDecoder.decode(file);
                if (result.Bytes == null)
                {
                    continue;
                }
                WriteableBitmap wbm = new WriteableBitmap(result.Width, result.Height);
                Image img = new Image();
                img.Source = wbm;
                img.Tag = file.Name;
                flipView.Items.Add(img);
                using (Stream streamWbm = wbm.PixelBuffer.AsStream())
                {
                    await streamWbm.WriteAsync(result.Bytes, 0, result.Bytes.Length);
                }
                wbm.Invalidate();
                if (file.Name == openFileParams.ClickedFile.Name)
                {
                    flipView.SelectedItem = flipView.Items.ElementAt(fileId);
                }
                ++fileId;
            }
            FrameworkElement element = flipView.SelectedItem as FrameworkElement;
            text.Text = element.Tag as string;
            flipView.Visibility = Visibility.Visible;
        }

        private void flipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (flipView.Visibility == Visibility.Visible)
            {
                FrameworkElement element = flipView.SelectedItem as FrameworkElement;
                text.Text = element.Tag as string;
            }
        }
    }
}
