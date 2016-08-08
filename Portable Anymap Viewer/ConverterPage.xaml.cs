using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
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
    public sealed partial class ConverterPage : Page
    {
        public ConverterPage()
        {
            this.InitializeComponent();
        }

        IReadOnlyList<StorageFile> files;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            files = e.Parameter as IReadOnlyList<StorageFile>;
            StorageFolder folder = await files.First().GetParentAsync();
            ConverterDirOutText.Text = folder.Path;
            foreach (StorageFile file in files)
            {
                ConverterListFileIn.Items.Add(file.Name);
                ConverterListFileOut.Items.Add(file.DisplayName + ".ppm");
            }
        }

        private void ConverterSubmit_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ConverterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String outputExtension;
            switch ((((sender as ComboBox).SelectedValue as ComboBoxItem).Content as TextBlock).Text)
            {
                case "Bitmap":
                    outputExtension = ".pbm";
                    break;
                case "Graymap":
                    outputExtension = ".pgm";
                    break;
                case "Pixmap":
                    outputExtension = ".ppm";
                    break;
                default:
                    outputExtension = "";
                    break;
            }
            if (ConverterListFileOut != null)
            {
                for (int i = 0; i < ConverterListFileOut.Items.Count; ++i)
                {
                    ConverterListFileOut.Items[i] = files[i].DisplayName + outputExtension;
                }
            }
        }
    }
}
