using Portable_Anymap_Viewer.Controls;
using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Globalization;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Pickers;
using Windows.Storage.Search;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Обеспечивает зависящее от конкретного приложения поведение, дополняющее класс Application по умолчанию.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Инициализирует одноэлементный объект приложения.  Это первая выполняемая строка разрабатываемого
        /// кода; поэтому она является логическим эквивалентом main() или WinMain().
        /// </summary>
        public App()
        {
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
            this.InitializeComponent();
            this.Suspending += OnSuspending;
        }

        /// <summary>
        /// Вызывается при обычном запуске приложения пользователем.  Будут использоваться другие точки входа,
        /// например, если приложение запускается для открытия конкретного файла.
        /// </summary>
        /// <param name="e">Сведения о запросе и обработке запуска.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Не повторяйте инициализацию приложения, если в окне уже имеется содержимое,
            // только обеспечьте активность окна
            if (rootFrame == null)
            {
                // Создание фрейма, который станет контекстом навигации, и переход к первой странице
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Navigated += RootFrame_Navigated;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Загрузить состояние из ранее приостановленного приложения
                }

                // Размещение фрейма в текущем окне
                Window.Current.Content = rootFrame;
                SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;

                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    rootFrame.CanGoBack ?
                    AppViewBackButtonVisibility.Visible :
                    AppViewBackButtonVisibility.Collapsed;

            }

            if (rootFrame.Content == null)
            {
                // Если стек навигации не восстанавливается для перехода к первой странице,
                // настройка новой страницы путем передачи необходимой информации в качестве параметра
                // параметр
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;

            // Обеспечение активности текущего окна
            Window.Current.Activate();
        }

        private Frame CreateRootFrame()
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.Language = ApplicationLanguages.Languages[0];
                rootFrame.NavigationFailed += OnNavigationFailed;
                Window.Current.Content = rootFrame;
            }
            return rootFrame;
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
            ((Frame)sender).CanGoBack ?
            AppViewBackButtonVisibility.Visible :
            AppViewBackButtonVisibility.Collapsed;
        }

        protected override async void OnFileActivated(FileActivatedEventArgs args)
        {
            base.OnFileActivated(args);
            Frame rootFrame = Window.Current.Content as Frame;
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;
            // Не повторяйте инициализацию приложения, если в окне уже имеется содержимое,
            // только обеспечьте активность окна
            if (rootFrame == null)
            {
                // Создание фрейма, который станет контекстом навигации, и переход к первой странице
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.Navigated += RootFrame_Navigated;

                // Размещение фрейма в текущем окне
                Window.Current.Content = rootFrame;
                SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;

                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                    rootFrame.CanGoBack ?
                    AppViewBackButtonVisibility.Visible :
                    AppViewBackButtonVisibility.Collapsed;

            }
            if (args.Files.Count != 1)
            {
                MessageDialog dlg = new MessageDialog("The app can handle only one activated file.");
                await dlg.ShowAsync();
                rootFrame.Navigate(typeof(MainPage));
                Window.Current.Content = rootFrame;
                Window.Current.Activate();
            }
            //switch (args.)
            IReadOnlyList<StorageFile> FileList = await args.NeighboringFilesQuery.GetFilesAsync();
            //foreach (StorageFile file in FileList)
            //{
            //    MessageDialog dlg = new MessageDialog(file.Name);
            //    await dlg.ShowAsync();
            //}

            ExplorerItem item = new ExplorerItem();
            //StorageItemThumbnail thumbnail = await openFileParams.FileList.ElementAt<StorageFile>(0).GetThumbnailAsync(ThumbnailMode.SingleItem);
            //BitmapImage thumbnailBitmap = new BitmapImage();
            //thumbnailBitmap.SetSource(thumbnail);
            item.Thumbnail = null;
            item.Filename = (args.Files.First<IStorageItem>() as StorageFile).Name;
            item.Type = (args.Files.First<IStorageItem>() as StorageFile).FileType;
            item.DisplayName = (args.Files.First<IStorageItem>() as StorageFile).DisplayName;
            item.DisplayType = (args.Files.First<IStorageItem>() as StorageFile).DisplayType;
            item.Path = (args.Files.First<IStorageItem>() as StorageFile).Path;
            item.Token = "";

            OpenFileParams openFileParams = new OpenFileParams();
            openFileParams.ClickedFile = item;
            openFileParams.FileList = FileList;

            rootFrame.Navigate(typeof(ViewerPage), openFileParams);
            Window.Current.Content = rootFrame;
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when a user issues a global back on the device.
        /// If the app has no in-app back stack left for the current view/frame the user may be navigated away
        /// back to the previous app in the system's app back stack or to the start screen.
        /// In windowed mode on desktop there is no system app back stack and the user will stay in the app even when the in-app back stack is depleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;
            
            // If we can go back and the event has not already been handled, do so.
            if (rootFrame.CanGoBack && e.Handled == false)
            {
                if (rootFrame.Content.GetType() == typeof(EditorPage))
                {
                    if ((rootFrame.Content as EditorPage).IsNeedToSave())
                    {
                        e.Handled = true;
                        var loader = new ResourceLoader();
                        var warningTilte = loader.GetString("EditorExitTitle");
                        var warningMesage = loader.GetString("EditorExitMessage");
                        var yes = loader.GetString("Yes");
                        var no = loader.GetString("No");
                        MessageDialog goBackConfirmation = new MessageDialog(warningMesage, warningTilte);
                        goBackConfirmation.Commands.Add(new UICommand(yes));
                        goBackConfirmation.Commands.Add(new UICommand(no));
                        goBackConfirmation.DefaultCommandIndex = 1;
                        var selectedCommand = await goBackConfirmation.ShowAsync();
                        if (selectedCommand.Label == yes)
                        {
                            rootFrame.GoBack();
                        }
                    }
                    else
                    {
                        e.Handled = true;
                        rootFrame.GoBack();
                    }
                }
                else
                {
                    e.Handled = true;
                    rootFrame.GoBack();
                }
            }
        }

        /// <summary>
        /// Вызывается в случае сбоя навигации на определенную страницу
        /// </summary>
        /// <param name="sender">Фрейм, для которого произошел сбой навигации</param>
        /// <param name="e">Сведения о сбое навигации</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Вызывается при приостановке выполнения приложения.  Состояние приложения сохраняется
        /// без учета информации о том, будет ли оно завершено или возобновлено с неизменным
        /// содержимым памяти.
        /// </summary>
        /// <param name="sender">Источник запроса приостановки.</param>
        /// <param name="e">Сведения о запросе приостановки.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Сохранить состояние приложения и остановить все фоновые операции
            deferral.Complete();
        }

        protected override async void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            var rootFrame = CreateRootFrame();
            if (args.ShareOperation.Data.Contains(StandardDataFormats.StorageItems))
            {
                IReadOnlyList<IStorageItem> fileList = await args.ShareOperation.Data.GetStorageItemsAsync();
                StorageFile file = fileList.First() as StorageFile;
                List<StorageFile> ff = new List<StorageFile>();
                ff.Add(file);
                rootFrame.Navigate(typeof(ConverterPage), ff);
            }
            else
            {
                //rootFrame.Navigate(typeof(ShareTargetPage), args.ShareOperation);
            }
            Window.Current.Activate();
        }
    }
}
