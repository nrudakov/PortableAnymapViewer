using System;
using Windows.ApplicationModel.Resources.Core;
using System.Diagnostics;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace Portable_Anymap_Viewer.Triggers
{
    /// <summary>
    /// Detecting the current app screen mode (Mobile or Desktop)
    /// </summary>
    public class DisplayModeTrigger : StateTriggerBase
    {
        private bool isInDesktopMode;

        public DisplayModeTrigger()
        {
            Window.Current.SizeChanged += Window_SizeChanged;
            ProjectionManager.ProjectionDisplayAvailableChanged += ProjectionManager_ProjectionDisplayAvailableChanged;
        }

        public void Detach()
        {
            Window.Current.SizeChanged -= Window_SizeChanged;
            ProjectionManager.ProjectionDisplayAvailableChanged -= ProjectionManager_ProjectionDisplayAvailableChanged;
        }

        public bool IsInDesktopMode
        {
            get
            {
                return this.isInDesktopMode;
            }
            set
            {
                this.isInDesktopMode = value;
                UpdateTrigger();
            }
        }

        private async void Window_SizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            try
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.UpdateTrigger();
                });
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Trigger Window_SizeChanged: {0}", ex.Message);
            }
        }

        private async void ProjectionManager_ProjectionDisplayAvailableChanged(object sender, object e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.UpdateTrigger();
            });
        }

        private void UpdateTrigger()
        {
            try
            {
                var qualifiers = ResourceContext.GetForCurrentView().QualifierValues;
                if (qualifiers.ContainsKey("DeviceFamily") && qualifiers["DeviceFamily"] == "Mobile")
                {
                    if (UIViewSettings.GetForCurrentView().UserInteractionMode == UserInteractionMode.Mouse ||
                        ProjectionManager.ProjectionDisplayAvailable)
                    {
                        SetActive(this.IsInDesktopMode);
                    }
                    else
                    {
                        SetActive(!this.IsInDesktopMode);
                    }
                }
                else if (qualifiers.ContainsKey("DeviceFamily") && qualifiers["DeviceFamily"] == "Desktop")
                {
                    SetActive(this.IsInDesktopMode);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Trigger Update: {0}", ex.Message);
            }
        }
    }
}
