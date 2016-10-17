using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Portable_Anymap_Viewer.Controls
{
    public sealed partial class HexView : UserControl
    {
        public HexView()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += this.CoreWindow_KeyDown;
        }

        public void Detach()
        {
            Window.Current.CoreWindow.KeyDown -= this.CoreWindow_KeyDown;

            this.PrimaryOffsets.Draw -= this.PrimaryOffsets_Draw;
            this.PrimaryOffsets.SizeChanged -= this.PrimaryOffsets_SizeChanged;
            this.PrimaryOffsets.ManipulationDelta -= this.HexDump_ManipulationDelta;
            this.PrimaryOffsets.PointerWheelChanged -= this.HexDump_PointerWheelChanged;

            this.HexDump.Draw -= this.HexDump_Draw;
            this.HexDump.ManipulationDelta -= this.HexDump_ManipulationDelta;
            this.HexDump.PointerWheelChanged -= this.HexDump_PointerWheelChanged;
            this.HexDump.PointerPressed -= this.HexDump_PointerPressed;

            this.AsciiDump.Draw -= this.AsciiDump_Draw;
            this.AsciiDump.ManipulationDelta -= this.HexDump_ManipulationDelta;
            this.AsciiDump.PointerWheelChanged -= this.HexDump_PointerWheelChanged;
            this.AsciiDump.PointerPressed -= this.AsciiDump_PointerPressed;

            this.Scroll.Scroll -= this.Scroll_Scroll;
        }

        public bool IsInputModeInsert
        {
            get;
            set;
        }

        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (this.selectedCanvas == 0 &&
                this.selectedIndex != -1 &&
                (this.selectedSubIndex == 0 ||
                this.selectedSubIndex == 1))
            {
                byte key = (byte)args.VirtualKey;
                if (VirtualKey.Number0 <= args.VirtualKey && args.VirtualKey <= VirtualKey.Number9)
                {
                    key -= 0x30;
                }
                else if (VirtualKey.A <= args.VirtualKey && args.VirtualKey <= VirtualKey.F)
                {
                    key -= 0x37;
                }
                else if (args.VirtualKey == VirtualKey.Delete)
                {
                    this.Bytes = HexView.RemoveFrom(this.Bytes, this.selectedIndex, 1);
                    if (this.selectedIndex >= this.Bytes.Length)
                    {
                        this.selectedIndex = this.Bytes.Length - 1;
                    }
                    Debug.WriteLine("Delete");
                    this.Invalidate();
                    return;
                }
                else if (args.VirtualKey == VirtualKey.Back)
                {
                    this.Bytes = HexView.RemoveFrom(this.Bytes, this.selectedIndex, 1);
                    if (this.selectedIndex > 0)
                    {
                        --this.selectedIndex;
                    }
                    Debug.WriteLine("Backspace");
                    this.Invalidate();
                    return;
                }
                else if (args.VirtualKey == VirtualKey.Left)
                {
                    if (this.selectedSubIndex == 0)
                    {
                        if (1 <= this.selectedIndex)
                        {
                            this.selectedSubIndex = 1;
                            --this.selectedIndex;
                        }
                    }
                    else if (this.selectedSubIndex == 1)
                    {
                        this.selectedSubIndex = 0;
                    }
                    if (this.selectedIndex < this.offset)
                    {
                        --this.Scroll.Value;
                    }
                    this.Invalidate();
                    return;
                }
                else if (args.VirtualKey == VirtualKey.Right)
                {
                    if (this.selectedSubIndex == 0)
                    {
                        this.selectedSubIndex = 1;
                    }
                    else if (this.selectedSubIndex == 1)
                    {
                        if (this.selectedIndex < (this.Bytes.Length - 1))
                        {
                            this.selectedSubIndex = 0;
                            ++this.selectedIndex;
                        }
                    }
                    if (this.selectedIndex >= this.offset + this.visibleRowsNum * 16)
                    {
                        ++this.Scroll.Value;
                    }
                    this.Invalidate();
                    return;
                }
                else if (args.VirtualKey == VirtualKey.Up)
                {
                    if (this.selectedIndex >= 0x10)
                    {
                        this.selectedIndex -= 0x10;
                    }
                    if (this.selectedIndex < this.offset)
                    {
                        --this.Scroll.Value;
                    }
                    this.Invalidate();
                    return;
                }
                else if (args.VirtualKey == VirtualKey.Down)
                {
                    if (this.selectedIndex < this.Bytes.Length - 0x10)
                    {
                        this.selectedIndex += 0x10;
                    }
                    if (this.selectedIndex >= this.offset + this.visibleRowsNum * 16)
                    {
                        ++this.Scroll.Value;
                    }
                    this.Invalidate();
                    return;
                }
                else
                {
                    return;
                }
                if (selectedSubIndex == 0)
                {
                    this.Bytes[this.selectedIndex] &= 0x0F;
                    this.Bytes[this.selectedIndex] |= (key <<= 0x04);
                    this.selectedSubIndex = 1;
                }
                else if (this.selectedSubIndex == 1)
                {
                    this.Bytes[this.selectedIndex] &= 0xF0;
                    this.Bytes[this.selectedIndex] |= key;
                    if (this.selectedIndex < (this.Bytes.Length - 1))
                    {
                        this.selectedSubIndex = 0;
                        ++this.selectedIndex;
                    }
                }
                if (this.selectedIndex >= this.offset + this.visibleRowsNum * 16)
                {
                    ++this.Scroll.Value;
                }
            }
            this.Invalidate();
        }

        private static byte[] RemoveFrom(byte[] source, int startpos, int length)
        {
            byte[] head = new byte[startpos];
            byte[] tail = new byte[source.Length - (startpos + length)];
            Array.Copy(source, 0, head, 0, startpos);
            Array.Copy(source, startpos + length, tail, 0, source.Length - (startpos + length));
            byte[] res = head.Concat(tail).ToArray();
            return res;
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
            }
        }

        private int selectedCanvas = -1;
        private int selectedIndex = -1;
        private int selectedSubIndex = -1;
        private int visibleRowsNum;
        private Vector2 letterSize;
        private Vector2 blockSize;
        private CanvasTextFormat regularFormat = new CanvasTextFormat
        {
            FontFamily = "Assets/consola.ttf#Consolas",
            FontSize = 16,
            FontStretch = Windows.UI.Text.FontStretch.Normal,
            WordWrapping = CanvasWordWrapping.NoWrap
        };
        private CanvasTextFormat workingFormat = new CanvasTextFormat
        {
            FontFamily = "Assets/consolab.ttf#Consolas",
            FontSize = 16,
            FontStretch = Windows.UI.Text.FontStretch.Normal,
            FontWeight = Windows.UI.Text.FontWeights.Bold,
            WordWrapping = CanvasWordWrapping.NoWrap
        };
        private Int32 offset;
        
        public void Invalidate()
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
            this.blockSize = new Vector2(
                Convert.ToSingle(this.letterSize.X * 3),
                Convert.ToSingle(this.letterSize.Y + 8));
            this.visibleRowsNum = Convert.ToInt32(Math.Floor(sender.ActualHeight / this.blockSize.Y));
            offset = Convert.ToInt32(Math.Floor(this.Scroll.Value)) * 16;

            // Actual PrimaryOffsets drawing
            for (int i = offset, j = 0; i < visibleRowsNum * 16 + offset && i < this.Bytes.Length; i += 16, ++j)
            {
                var position = this.ComputePrimaryOffsetPosition(j);
                session.DrawText(i.ToString("X8"), position, Colors.White, this.regularFormat);
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
                    if (selectedIndex == iOffset && selectedSubIndex != -1 && selectedCanvas != -1)
                    {
                        session.DrawText(" ", position, Colors.White, regularFormat);
                        position.X += letterSize.X;
                        if (selectedSubIndex == 0)
                        {
                            session.DrawText((Bytes[iOffset] >> 4).ToString("X1"), position, Colors.Violet, workingFormat);
                            position.X += letterSize.X;
                            session.DrawText((Bytes[iOffset] & 0x0F).ToString("X1"), position, Colors.White, workingFormat);
                        }
                        else if (selectedSubIndex == 1)
                        {
                            session.DrawText((Bytes[iOffset] >> 4).ToString("X1"), position, Colors.White, workingFormat);
                            position.X += letterSize.X;
                            session.DrawText((Bytes[iOffset] & 0x0F).ToString("X1"), position, Colors.Violet, workingFormat);
                        }
                    }
                    else
                    {
                        session.DrawText(" " + Bytes[iOffset].ToString("X2"), position, Colors.White, regularFormat);
                    }
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
                        if (iOffset == selectedIndex)
                        {
                            session.DrawText(Convert.ToChar(0x2E).ToString(), position, Colors.Violet, workingFormat);
                        }
                        else
                        {
                            session.DrawText(Convert.ToChar(0x2E).ToString(), position, Colors.DarkGoldenrod, regularFormat);
                        }
                    }
                    else
                    {
                        if (iOffset == selectedIndex)
                        {
                            session.DrawText(Convert.ToChar(this.Bytes[iOffset]).ToString(), position, Colors.Violet, workingFormat);
                        }
                        else
                        {
                            session.DrawText(Convert.ToChar(this.Bytes[iOffset]).ToString(), position, Colors.White, regularFormat);
                        }
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
            var point = e.GetCurrentPoint(this.HexDump);
            var column = Convert.ToInt32(Math.Floor((point.Position.X - 0.5*letterSize.X) / this.blockSize.X));
            var row = Convert.ToInt32(Math.Floor((point.Position.Y + 4) / this.blockSize.Y));
            selectedCanvas = 0;
            bool bb = HexDump.Focus(FocusState.Programmatic);
            var preSelectedIndex = 16 * row + column + offset;
            if (0 <= preSelectedIndex && preSelectedIndex < Bytes.Length)
            {
                selectedIndex = preSelectedIndex;
                var preSelectedSubIndex = Convert.ToInt32(point.Position.X - blockSize.X * (selectedIndex % 16));
                if (10 <= preSelectedSubIndex && preSelectedSubIndex <= 20)
                {
                    selectedSubIndex = 0;
                }
                else if (20 < preSelectedSubIndex && preSelectedSubIndex <= 30)
                {
                    selectedSubIndex = 1;
                }
                else
                {
                    selectedIndex = -1;
                    selectedCanvas = -1;
                    selectedSubIndex = -1;
                }
            }
            else
            {
                HexDump.Focus(FocusState.Unfocused);
                selectedIndex = -1;
                selectedCanvas = -1;
                selectedSubIndex = -1;
            }
            this.Invalidate();
        }

        private void AsciiDump_PointerPressed(object sender, PointerRoutedEventArgs e)
        {

        }

        private void PrimaryOffsets_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Invalidate();
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

        private void HexDump_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            Scroll.Value -= e.Delta.Translation.Y * 0.04;
            this.Invalidate();
        }

        private void HexDump_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            Scroll.Value -= e.GetCurrentPoint(sender as CanvasControl).Properties.MouseWheelDelta * 0.025;
            this.Invalidate();
        }
    }
}
