using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
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
    public sealed partial class EditorPage : Page
    {
        public EditorPage()
        {
            this.InitializeComponent();
        }
        private int fileType;
        private StorageFile file;
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            EditFileParams editFileParams = (e.Parameter as EditFileParams);
            fileType = editFileParams.type;
            file = editFileParams.file;
            WriteableBitmap wbm = editFileParams.image.Source as WriteableBitmap;
            EditorImage.Source = wbm;
            EditorImage.Stretch = Stretch.Uniform;
            EditorImage.Visibility = Visibility.Visible;
            switch (fileType)
            {
                case 1:
                case 2:
                case 3:
                    var stream = await file.OpenAsync(FileAccessMode.Read);
                    ulong size = stream.Size;
                    var dataReader = new DataReader(stream.GetInputStreamAt(0));
                    uint bytesLoaded = await dataReader.LoadAsync((uint)(stream.Size));

                    byte[] bytes = new byte[bytesLoaded];
                    dataReader.ReadBytes(bytes);

                    ASCIIEncoding ascii = new ASCIIEncoding();
                    string strAll = ascii.GetString(bytes);

                    EditorText.Document.SetText(Windows.UI.Text.TextSetOptions.None, strAll);
                    break;
                case 4:
                case 5:
                case 6:
                    break;
            }
        }
    }
}
