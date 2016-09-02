﻿using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
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
        int editorRow;
        
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
                    editorRow = 0;
                    EditorText.Visibility = Visibility.Visible;
                    
                    var streamT = await editFileParams.File.OpenAsync(FileAccessMode.Read);
                    var dataReaderT = new DataReader(streamT.GetInputStreamAt(0));
                    uint bytesLoadedT = await dataReaderT.LoadAsync((uint)(streamT.Size));

                    byte[] bytesText = new byte[bytesLoadedT];
                    dataReaderT.ReadBytes(bytesText);

                    ASCIIEncoding ascii = new ASCIIEncoding();
                    string strAll = ascii.GetString(bytesText);
                    EditorText.Text = strAll;
                    break;
                case 4:
                case 5:
                case 6:
                    editorRow = 1;
                    EditorHex.Visibility = Visibility.Visible;
                    
                    var streamB = await editFileParams.File.OpenAsync(FileAccessMode.Read);
                    var dataReaderB = new DataReader(streamB.GetInputStreamAt(0));
                    uint bytesLoadedB = await dataReaderB.LoadAsync((uint)(streamB.Size));

                    byte[] bytesHex = new byte[bytesLoadedB];
                    dataReaderB.ReadBytes(bytesHex);
                    EditorHex.Bytes = bytesHex;
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

        private void EditorCompare_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditorPreview_Click(object sender, RoutedEventArgs e)
        {
            if (EditorCanvas.Visibility == Visibility.Collapsed)
            {
                EditorCanvas.Visibility = Visibility.Visible;
                EditorEditGrid.RowDefinitions[editorRow].Height = new GridLength(0, GridUnitType.Pixel);
                EditorEditGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
            }
            else
            {
                EditorCanvas.Visibility = Visibility.Collapsed;
                EditorEditGrid.RowDefinitions[2].Height = new GridLength(0, GridUnitType.Pixel);
                EditorEditGrid.RowDefinitions[editorRow].Height = new GridLength(1, GridUnitType.Star);
            }
        }

        private void EditorUndo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditorRedo_Click(object sender, RoutedEventArgs e)
        {

        }

        private void EditorSaveCopy_Click(object sender, RoutedEventArgs e)
        {

        }

        private async void EditorSave_Click(object sender, RoutedEventArgs e)
        {
            EditorCommandBar.IsEnabled = false;
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
            await Task.Delay(5000);
            EditorRing.IsActive = false;
            EditorRing.Visibility = Visibility.Collapsed;
            EditorCommandBar.IsEnabled = true;
        }

        private void EditorCancel_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
