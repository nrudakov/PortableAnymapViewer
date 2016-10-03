using System;
using Windows.UI.Xaml.Controls;

namespace Portable_Anymap_Viewer.Controls
{
    public sealed partial class ConverterFilename : UserControl
    {
        public ConverterFilename()
        {
            this.InitializeComponent();
        }

        public ConverterFilename(String InputFilename, String OutputFilename)
        {
            this.InitializeComponent();
            this.SetInputFilename(InputFilename);
            this.SetOutputFilename(OutputFilename);
        }

        public void SetInputFilename(String InputFilename)
        {
            this.InputFilenameLabel.Text = InputFilename;
        }

        public void SetOutputFilename(String OutputFilename)
        {
            this.OutputFilenameLabel.Text = OutputFilename;
        }

        public void SetHalfWidth(Double HalfWidth)
        {
            this.InputFilenameLabel.Width = HalfWidth;
        }

        public Double GetHalfWidth()
        {
            return this.InputFilenameLabel.ActualWidth;
        }
    }
}
