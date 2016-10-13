using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
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
    public sealed partial class ExplorerItem : UserControl
    {
        public ExplorerItem()
        {
            this.InitializeComponent();
        }
        
        public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register("DisplayName", typeof(String), typeof(ExplorerItem), null);
        public String DisplayName
        {
            get
            {
                return GetValue(DisplayNameProperty) as String;
            }
            set
            {
                SetValue(DisplayNameProperty, value);
            }
        }

        public String DisplayType
        {
            get
            {
                return this.ExplorerItemDisplayType.Text;
            }
            set
            {
                this.ExplorerItemDisplayType.Text = value;
            }
        }

        public String Filename
        {
            get
            {
                return this.ExplorerItemName.Text;
            }
            set
            {
                this.ExplorerItemName.Text = value;
            }
        }

        public static readonly DependencyProperty PathProperty = DependencyProperty.Register("Path", typeof(String), typeof(ExplorerItem), null);
        public String Path
        {
            get
            {
                return GetValue(PathProperty) as String;
            }
            set
            {
                SetValue(PathProperty, value);
            }
        }

        public BitmapImage Thumbnail
        {
            get
            {
                return this.ExplorerItemThumbnail.Source as BitmapImage;
            }
            set
            {
                this.ExplorerItemThumbnail.Source = value;
            }
        }

        public static readonly DependencyProperty TokenProperty = DependencyProperty.Register("Token", typeof(String), typeof(ExplorerItem), null);
        public String Token
        {
            get
            {
                return GetValue(TokenProperty) as String;
            }
            set
            {
                SetValue(TokenProperty, value);
            }
        }

        public static readonly DependencyProperty TypeProperty = DependencyProperty.Register("Type", typeof(String), typeof(ExplorerItem), null);
        public String Type
        {
            get
            {
                return GetValue(TypeProperty) as String;
            }
            set
            {
                SetValue(TypeProperty, value);
            }
        }
    }
}
