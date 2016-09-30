using System;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.ViewManagement;

namespace Portable_Anymap_Viewer.Triggers
{
    /// <summary>
    /// Detecting if the mobile app is connected to a secondary screen
    /// </summary>
    public class ContinuumMobileAvailableTrigger : StateTriggerBase
    {
        public ContinuumMobileAvailableTrigger()
        {
            ProjectionManager.ProjectionDisplayAvailableChanged += ProjectionManager_ProjectionDisplayAvailableChanged;
            this.UpdateTrigger();
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
            this.SetActive(
                ProjectionManager.ProjectionDisplayAvailable &&
                AnalyticsInfo.VersionInfo.DeviceFamily.Equals("Windows Mobile", StringComparison.CurrentCultureIgnoreCase)
            );
        }
    }
}
