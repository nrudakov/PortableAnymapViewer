using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Portable_Anymap_Viewer
{
    public sealed partial class HexView : UserControl
    {
        public HexView()
        {
            this.InitializeComponent();
        }

        public static readonly DependencyProperty BytesProperty =
            DependencyProperty.Register("Bytes", typeof(byte[]), typeof(HexView), new PropertyMetadata(new byte[] { }));

        public byte[] Bytes
        {
            get
            {
                return (byte[])GetValue(BytesProperty);
            }
            set
            {
                SetValue(BytesProperty, value);
                this.Invalidate();
            }
        }
        
        private int visibleRowsNum;
        private Vector2 letterSize;
        private Vector2 blockSize;
        private CanvasTextFormat regularFormat = new CanvasTextFormat
        {
            FontSize = 16,
            WordWrapping = CanvasWordWrapping.NoWrap,
            FontFamily = "Assets/consola.ttf#Consolas",
            FontStretch = Windows.UI.Text.FontStretch.Normal
        };
        private Int32 offset;

        private void Invalidate()
        {
            this.PrimaryOffsets.Invalidate();
            this.HexDump.Invalidate();
            this.AsciiDump.Invalidate();
        }

        private void PrimaryOffsets_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var session = args.DrawingSession;
            // Computings for all three canvas drawings
            var textLayout = new CanvasTextLayout(session, "A", this.regularFormat, 0.0f, 0.0f);
            this.letterSize = new Vector2(
                Convert.ToSingle(textLayout.LayoutBounds.Width),
                Convert.ToSingle(textLayout.LayoutBounds.Height));
            this.Width = letterSize.X * 77;
            blockSize = new Vector2(
                Convert.ToSingle(letterSize.X * 3),
                Convert.ToSingle(letterSize.Y + 8));
            this.visibleRowsNum = Convert.ToInt32(Math.Floor(sender.ActualHeight / this.blockSize.Y));
            offset = Convert.ToInt32(Math.Floor(this.Scroll.Value)) * 16;

            // Actual PrimaryOffsets drawing
            for (int i = offset, j = 0; i < visibleRowsNum * 16 + offset && i < this.Bytes.Length; i += 16, ++j)
            {
                var position = this.ComputePrimaryOffsetPosition(j);
                session.DrawText(i.ToString("X8"), position, Colors.White, regularFormat);
            }
        }

        private void HexDump_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var session = args.DrawingSession;
            var bytesRenderNum = this.visibleRowsNum * 16;
            
            var bytesNum = 0L;
            if (this.Bytes != null)
            {
                bytesNum = this.Bytes.Length;
            }

            for (int i = 0; i < bytesRenderNum; ++i)
            {
                var position = this.ComputeHexPosition(i);
                var iOffset = i + offset;
                if (iOffset < bytesNum)
                {
                    session.DrawText(" " + Bytes[iOffset].ToString("X2"), position, Colors.White, regularFormat);
                }
                else
                {
                    break;
                }
            }
            this.UpdateScrollbarProperties();
        }

        private void AsciiDump_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var session = args.DrawingSession;
            var bytesRenderNum = this.visibleRowsNum * 16;

            var bytesNum = 0L;
            if (this.Bytes != null)
            {
                bytesNum = this.Bytes.Length;
            }

            for (int i = 0; i < bytesRenderNum; ++i)
            {
                var position = this.ComputeAsciiPosition(i);
                var iOffset = i + offset;
                if (iOffset < bytesNum)
                {
                    if (iOffset % 16 == 0)
                    {
                        session.DrawText("  ", new Vector2(0, iOffset / 16 * this.blockSize.Y), Colors.White, regularFormat);
                    }
                    if (this.Bytes[iOffset] <= 0x1F || 0x7F <= this.Bytes[iOffset] && this.Bytes[iOffset] <= 0xA0)
                    {
                        session.DrawText(Convert.ToChar(0x2E).ToString(), position, Colors.DarkGoldenrod, regularFormat);
                    }
                    else
                    {
                        session.DrawText(Convert.ToChar(this.Bytes[iOffset]).ToString(), position, Colors.White, regularFormat);
                    }
                }
                else
                {
                    break;
                }
            }
            this.UpdateScrollbarProperties();
        }

        private Vector2 ComputePrimaryOffsetPosition(int index)
        {
            return new Vector2(
                Convert.ToSingle(0),
                Convert.ToSingle(index * this.blockSize.Y));
        }

        private Vector2 ComputeHexPosition(int index)
        {
            return new Vector2(
                Convert.ToSingle(index % 16 * this.blockSize.X),
                Convert.ToSingle(index / 16 * this.blockSize.Y));
        }
        private Vector2 ComputeAsciiPosition(int index)
        {
            return new Vector2(
                Convert.ToSingle((index % 16 + 2) * this.letterSize.X),
                Convert.ToSingle(index / 16 * this.blockSize.Y));
        }

        private void HexDump_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }

        private void AsciiDump_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }

        private void Scroll_Scroll(object sender, ScrollEventArgs e)
        {
            this.Invalidate();
        }

        private void UpdateScrollbarProperties()
        {
            var totalRowsNum = this.Bytes.Length / 16;
            if (this.Bytes == null || totalRowsNum <= this.visibleRowsNum)
            {
                this.Scroll.Visibility = Visibility.Collapsed;
                return;
            }

            this.Scroll.Visibility = Visibility.Visible;
            this.Scroll.Minimum = 0;
            this.Scroll.Maximum = totalRowsNum;
            this.Scroll.LargeChange = Math.Max(this.visibleRowsNum - 1, 1);
            this.Scroll.SmallChange = 1;
            this.Scroll.ViewportSize = this.visibleRowsNum;
        }
    }
}
