using Portable_Anymap_Viewer.Models;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Portable_Anymap_Viewer
{
    public class CanvasWrapper : Grid
    {
        private TranslateTransform translateTransform;
        private DecodeResult imageInfo;

        public CanvasWrapper(DecodeResult imageInfo)
        {
            this.translateTransform = new TranslateTransform();
            this.imageInfo = imageInfo;

            this.Background = new SolidColorBrush(Colors.Transparent);

            this.ManipulationMode =
                ManipulationModes.System |
                ManipulationModes.Scale;
            this.ManipulationDelta += CanvasWrapper_ManipulationDelta;
            this.Loaded += CanvasWrapper_Loaded;
        }

        private void Sensor_OrientationChanged(SimpleOrientationSensor sender, SimpleOrientationSensorOrientationChangedEventArgs args)
        {
            try
            { 
                this.UpdateManipulationMode();
                this.GetCanvas().Invalidate();
            }
            catch (Exception ex)
            {
                
            }
        }   

        private void CanvasWrapper_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= CanvasWrapper_Loaded;
        }

        private void CanvasWrapper_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (e.Delta.Scale != 1)
            {
                this.Zoom(e.Delta.Scale);
            }
            else
            {
                this.Shift(e.Delta.Translation);
            }
        }

        public bool IsZoomable(Single scale)
        {
            CanvasControl canvas = this.GetCanvas();
            Single maxSize = canvas.ConvertPixelsToDips(canvas.Device.MaximumBitmapSizeInPixels);
            return (canvas.Width * scale <= maxSize &&
                canvas.Height * scale <= maxSize);
        }

        public bool IsUnzoomable()
        {
            return !(imageInfo.CurrentZoom == 1);
        }

        public void Zoom(Single scale)
        {
            CanvasControl canvas = this.GetCanvas();
            if (scale * imageInfo.CurrentZoom < 1)
            {
                this.ZoomReal();
            }
            else
            {
                if (IsZoomable(scale))
                {
                    canvas.Width *= scale;
                    canvas.Height *= scale;
                    Double xp = this.translateTransform.X / ((canvas.ActualWidth - this.ActualWidth) / imageInfo.CurrentZoom);
                    Double yp = this.translateTransform.Y / ((canvas.ActualHeight - this.ActualHeight) / imageInfo.CurrentZoom);
                    this.imageInfo.CurrentZoom *= scale;
                    if (scale < 1)
                    {
                        this.translateTransform.X = xp * ((canvas.Width - this.ActualWidth) / imageInfo.CurrentZoom);
                        this.translateTransform.Y = yp * ((canvas.Height - this.ActualHeight) / imageInfo.CurrentZoom);
                    }
                    if (this.translateTransform.X > 0)
                    {
                        this.translateTransform.X = 0;
                    }
                    if (this.translateTransform.Y > 0)
                    {
                        this.translateTransform.Y = 0;
                    }
                    (canvas.Tag as CanvasImageBrush).Transform = 
                        Matrix3x2.CreateTranslation
                        (
                            new Vector2
                            (
                                (Single)this.translateTransform.X, 
                                (Single)this.translateTransform.Y
                            )
                        ) * 
                        Matrix3x2.CreateScale(this.imageInfo.CurrentZoom);
                    canvas.Invalidate();
                    this.UpdateManipulationMode();
                }
            }
        }

        public void ZoomReal()
        {
            CanvasControl canvas = this.GetCanvas();
            canvas.Width = this.imageInfo.Width;
            canvas.Height = this.imageInfo.Height;
            imageInfo.CurrentZoom = 1.0f;
            this.translateTransform.X = 0;
            this.translateTransform.Y = 0;
            (canvas.Tag as CanvasImageBrush).Transform = Matrix3x2.CreateScale(this.imageInfo.CurrentZoom);
            canvas.Invalidate();
            this.UpdateManipulationMode();
        }

        private void Shift(Point translation)
        {
            // Apply shifting
            this.translateTransform.X += translation.X / imageInfo.CurrentZoom;
            this.translateTransform.Y += translation.Y / imageInfo.CurrentZoom;
            this.RectifyTranslateTransform();
            CanvasControl canvas = this.GetCanvas();
            (canvas.Tag as CanvasImageBrush).Transform =
                Matrix3x2.CreateTranslation
                (
                    new Vector2
                    (
                        (Single)this.translateTransform.X,
                        (Single)this.translateTransform.Y
                    )
                ) *
                Matrix3x2.CreateScale(this.imageInfo.CurrentZoom);
            // Redraw canvas
            canvas.Invalidate();
        }

        private void RectifyTranslateTransform()
        {
            CanvasControl canvas = this.GetCanvas();
            // at X-axis
            Double xRange = canvas.ActualWidth - this.ActualWidth;
            if (xRange > 0)
            {
                if (this.translateTransform.X >= 0)
                {
                    this.translateTransform.X = 0;
                }
                else if (xRange / imageInfo.CurrentZoom < -this.translateTransform.X)
                {
                    this.translateTransform.X = -xRange / imageInfo.CurrentZoom;
                }
            }
            else
            {
                translateTransform.X = 0;
            }
            // at Y-axis
            Double yRange = canvas.ActualHeight - this.ActualHeight;
            if (yRange > 0)
            {
                if (this.translateTransform.Y >= 0)
                {
                    this.translateTransform.Y = 0;
                }
                else if (yRange / imageInfo.CurrentZoom < -this.translateTransform.Y)
                {
                    this.translateTransform.Y = -yRange / imageInfo.CurrentZoom;
                }
            }
            else
            {
                translateTransform.Y = 0;
            }
        }

        private void UpdateManipulationMode()
        {
            CanvasControl canvas = this.GetCanvas();
            if (this.imageInfo.CurrentZoom == 1)
            {
                this.ManipulationMode =
                    ManipulationModes.System |
                    ManipulationModes.Scale;
            }
            else if (canvas.Width > this.ActualWidth &&
                canvas.Height > this.ActualHeight)
            {
                this.ManipulationMode =
                    ManipulationModes.TranslateX |
                    ManipulationModes.TranslateY |
                    ManipulationModes.Scale |
                    ManipulationModes.TranslateInertia;
            }
            else if (canvas.Width > this.ActualWidth)
            {
                this.ManipulationMode =
                    ManipulationModes.TranslateX |
                    ManipulationModes.Scale |
                    ManipulationModes.TranslateInertia;
            }
            else if (canvas.Height > this.ActualHeight)
            {
                this.ManipulationMode =
                    ManipulationModes.TranslateY |
                    ManipulationModes.Scale |
                    ManipulationModes.TranslateInertia;
            }
            else
            {
                this.ManipulationMode =
                    ManipulationModes.Scale;
            }
        }

        public void SetCanvas(CanvasControl canvas)
        {
            this.Children.Clear();
            this.Children.Add(canvas);
            SimpleOrientationSensor sensor = SimpleOrientationSensor.GetDefault();
            if (sensor != null)
            {
                sensor.OrientationChanged += Sensor_OrientationChanged;
            }
        }

        private CanvasControl GetCanvas()
        {
            return this.Children?[0] as CanvasControl;
        }
    }
}
