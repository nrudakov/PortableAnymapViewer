using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
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
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class EditorPage : Page
    {
        public EditorPage()
        {
            this.InitializeComponent();
        }

        EditFileParams editFileParams;
        CanvasBitmap cbm;
        CanvasImageBrush brush;
        
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (String.Equals(AnalyticsInfo.VersionInfo.DeviceFamily, "Windows.Desktop"))
            {
                EditorCommandBar.VerticalAlignment = VerticalAlignment.Top;
            }
            editFileParams = (e.Parameter as EditFileParams);
            switch (editFileParams.Type)
            {
                case 1:
                case 2:
                case 3:
                    var stream = await editFileParams.File.OpenAsync(FileAccessMode.Read);
                    var dataReader = new DataReader(stream.GetInputStreamAt(0));
                    uint bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size));

                    byte[] bytesText = new byte[bytesLoaded];
                    dataReader.ReadBytes(bytesText);

                    ASCIIEncoding ascii = new ASCIIEncoding();
                    string strAll = ascii.GetString(bytesText);
                    EditorText.Text = strAll;
                    break;
                case 4:
                case 5:
                case 6:
                    break;
            }
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

        private void EditorCompare_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditorSaveCopy_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void EditorSave_Click(object sender, RoutedEventArgs e)
        {
            var button = (sender as AppBarButton);
            button.IsEnabled = false;
            var stream = await editFileParams.File.OpenAsync(FileAccessMode.ReadWrite);
            var dataWriter = new DataWriter(stream);
            uint i = dataWriter.WriteString(EditorText.Text);

            button.IsEnabled = true;
        }

        private void EditorCancel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Pivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as Pivot).SelectedIndex == 1)
            {

            }
        }
    }
}
