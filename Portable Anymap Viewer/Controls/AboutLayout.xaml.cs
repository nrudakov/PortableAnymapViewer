using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Portable_Anymap_Viewer.Controls
{
    public sealed partial class AboutLayout : UserControl
    {
        public AboutLayout()
        {
            this.InitializeComponent();
            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            BitmapImage bitmap = new BitmapImage();
            bitmap.UriSource = package.Logo;
            this.AboutLogo.Width = bitmap.DecodePixelWidth = 50;
            this.AboutLogo.Source = bitmap;

            this.AboutDisplayName.Text = package.DisplayName;
            this.AboutVersion.Text = String.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);
            this.AboutArchitecture.Text = packageId.Architecture.ToString();

            this.AboutInstalledDate.Text = package.InstalledDate.ToString();
            this.AboutInstalledLocation.Text = package.InstalledLocation.Path;
            this.AboutPublisher.Text = package.PublisherDisplayName;
        }

        

        private async void AboutBug_Click(object sender, RoutedEventArgs e)
        {
            var mailto = new Uri("mailto:?to=nickolay-zerkalny@yandex.ru&subject=Bug report for Portable Anymap Viewer app");
            await Windows.System.Launcher.LaunchUriAsync(mailto);
        }

        private async void AboutFeedback_Click(object sender, RoutedEventArgs e)
        {
            var mailto = new Uri("mailto:?to=nickolay-zerkalny@yandex.ru&subject=Feedback for Portable Anymap Viewer app");
            await Windows.System.Launcher.LaunchUriAsync(mailto);
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.AboutBug.Click -= this.AboutBug_Click;
            this.AboutFeedback.Click -= this.AboutFeedback_Click;
        }
    }
}
