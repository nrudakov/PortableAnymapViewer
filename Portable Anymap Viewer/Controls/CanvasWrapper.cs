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

namespace Portable_Anymap_Viewer.Controls
{
    public class CanvasWrapper : Grid
    {
        private TranslateTransform translateTransform = new TranslateTransform();
        private DecodeResult imageInfo;
        private Single initialZoom;
        private bool isInfoSet = false;
        private bool isCanvasSet = false;

        public CanvasWrapper() {}

        public CanvasWrapper(DecodeResult imageInfo)
        {
            SetImageInfo(imageInfo);
        }

        public void SetImageInfo (DecodeResult imageInfo)
        {
            this.imageInfo = imageInfo;
            this.initialZoom = imageInfo.CurrentZoom;

            this.Background = new SolidColorBrush(Colors.Transparent);

            this.ManipulationMode =
                ManipulationModes.System |
                ManipulationModes.Scale;
            isInfoSet = true;
        }

        public bool IsInfoSet
        {
            get
            {
                return this.isInfoSet;
            }
        }

        public bool IsCanvasSet
        {
            get
            {
                return this.isCanvasSet;
            }
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
            if (canvas != null)
            {
                Single maxSize = canvas.ConvertPixelsToDips(canvas.Device.MaximumBitmapSizeInPixels);
                return (canvas.Width * scale <= maxSize &&
                    canvas.Height * scale <= maxSize);
            }
            return false;
        }

        public bool IsUnzoomable()
        {
            return !(this.imageInfo?.CurrentZoom == this.initialZoom);
        }

        public void Zoom(Single scale)
        {
            CanvasControl canvas = this.GetCanvas();
            if (this.imageInfo != null)
            {
                if (scale * this.imageInfo.CurrentZoom < this.initialZoom)
                {
                    this.ZoomReal();
                }
                else if (IsZoomable(scale))
                {

                    canvas.Width *= scale;
                    canvas.Height *= scale;
                    Double xp = this.translateTransform.X / ((canvas.ActualWidth - this.ActualWidth) / this.imageInfo.CurrentZoom);
                    Double yp = this.translateTransform.Y / ((canvas.ActualHeight - this.ActualHeight) / this.imageInfo.CurrentZoom);
                    this.imageInfo.CurrentZoom *= scale;
                    if (scale < this.initialZoom)
                    {
                        this.translateTransform.X = xp * ((canvas.Width - this.ActualWidth) / this.imageInfo.CurrentZoom);
                        this.translateTransform.Y = yp * ((canvas.Height - this.ActualHeight) / this.imageInfo.CurrentZoom);
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
            if (canvas != null)
            {
                canvas.Width = this.imageInfo.Width * this.initialZoom;
                canvas.Height = this.imageInfo.Height * this.initialZoom;
                imageInfo.CurrentZoom = this.initialZoom;
                this.translateTransform.X = 0;
                this.translateTransform.Y = 0;
                (canvas.Tag as CanvasImageBrush).Transform = Matrix3x2.CreateScale(this.imageInfo.CurrentZoom);
                canvas.Invalidate();
                this.UpdateManipulationMode();
            }
        }

        private void Shift(Point translation)
        {
            // Apply shifting
            this.translateTransform.X += translation.X / this.imageInfo.CurrentZoom;
            this.translateTransform.Y += translation.Y / this.imageInfo.CurrentZoom;
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
                else if (xRange / this.imageInfo.CurrentZoom < -this.translateTransform.X)
                {
                    this.translateTransform.X = -xRange / this.imageInfo.CurrentZoom;
                }
            }
            else
            {
                this.translateTransform.X = 0;
            }
            // at Y-axis
            Double yRange = canvas.ActualHeight - this.ActualHeight;
            if (yRange > 0)
            {
                if (this.translateTransform.Y >= 0)
                {
                    this.translateTransform.Y = 0;
                }
                else if (yRange / this.imageInfo.CurrentZoom < -this.translateTransform.Y)
                {
                    this.translateTransform.Y = -yRange / this.imageInfo.CurrentZoom;
                }
            }
            else
            {
                this.translateTransform.Y = 0;
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
            this.ManipulationDelta += CanvasWrapper_ManipulationDelta;
            SimpleOrientationSensor sensor = SimpleOrientationSensor.GetDefault();
            if (sensor != null)
            {
                sensor.OrientationChanged += Sensor_OrientationChanged;
            }
            this.isCanvasSet = true;
        }

        public void RemoveCanvas()
        {
            this.ManipulationDelta -= CanvasWrapper_ManipulationDelta;
            SimpleOrientationSensor sensor = SimpleOrientationSensor.GetDefault();
            if (sensor != null)
            {
                sensor.OrientationChanged -= Sensor_OrientationChanged;
            }
            this.GetCanvas()?.RemoveFromVisualTree();
            this.Children.Clear();
            this.isCanvasSet = false;
        }

        private CanvasControl GetCanvas()
        {
            return this.Children?[0] as CanvasControl;
        }
    }
}
