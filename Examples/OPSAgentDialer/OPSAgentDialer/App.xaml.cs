using System;
using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using OPSAgentDialer.Model;
using OPSAgentDialer.Model.AgentDialer;
using OPSAgentDialer.Model.Settings;
using OPSAgentDialer.ViewModel;
using OzCommon.Model;
using OzCommon.Utils;
using OzCommon.Utils.DialogService;
using OzCommon.Utils.Schedule;
using OzCommon.View;
using OzCommon.ViewModel;
using OzCommonBroadcasts.Model;
using OzCommonBroadcasts.Model.Csv;
using OzCommonBroadcasts.View;
using OzCommonBroadcasts.ViewModel;

namespace OPSAgentDialer
{
    public partial class App : Application
    {
        private readonly SingletonApp _singletonApp;

        public App()
        {
            _singletonApp = new SingletonApp("OPSAgentDialer");
            InitDependencies();
        }

        void InitDependencies()
        {
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<ICsvImporter<CustomerEntry>>(() => new CsvImporter<CustomerEntry>());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<ICsvExporter<CustomerEntry>>(() => new CsvExporter<CustomerEntry>());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<IDialogService>(() => new DialogService());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<IBroadcastMainViewModel>(() => new AgentDialerViewModel());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<IGenericSettingsRepository<AppPreferences>>(() => new SettingsRepository());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<IScheduler<CustomerEntry>>(() => new AgentDialer());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<IUserInfoSettingsRepository>(() => new UserInfoSettingsRepository());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<IClient>(() => new Client());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register<IExtensionContainer>(() => new ExtensionContainer());
            GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.Register(() => new ApplicationInformation("Agent Dialer"));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            Messenger.Default.Register<NotificationMessage>(this, MessageReceived);

            _singletonApp.OnStartup(e);

            base.OnStartup(e);

            MainWindow = new LoginWindow();
            MainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Messenger.Default.Unregister<NotificationMessage>(this, MessageReceived);
            base.OnExit(e);
        }

        private void MessageReceived(NotificationMessage notificationMessage)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (notificationMessage.Notification == Messages.NavigateToMainWindow)
                {
                    MainWindow = new BroadcastMainWindow();
                    MainWindow.Show();

                }
                else if (notificationMessage.Notification == AgentDialerViewModel.ShowApiExtensionWarning)
                {
                    MessageBox.Show(
                        "API extension ID is not set in General settings or it is invalid.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (notificationMessage.Notification == AgentDialerViewModel.ShowNoAgentsSelectedError)
                {
                    MessageBox.Show(
                        "No agents selected to handle calls.",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }));
        }
    }
}
