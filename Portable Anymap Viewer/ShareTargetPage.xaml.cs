using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class ShareTargetPage : Page
    {
        ShareOperation shareOperation;

        public ShareTargetPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.shareOperation = e.Parameter as ShareOperation;
            await Task.Factory.StartNew(async () =>
            {
                if (this.shareOperation.Data.Contains(StandardDataFormats.StorageItems))
                {
                    var file = await shareOperation.Data.GetStorageItemsAsync();

                }
                if (shareOperation.Data.Contains(StandardDataFormats.Bitmap))
                {
                    var stream = await shareOperation.Data.GetBitmapAsync();
                }
            });
        }
    }
}
