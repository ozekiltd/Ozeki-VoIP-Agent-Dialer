using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using OPSAgentDialer.Model;
using OPSAgentDialer.Model.Settings;
using OzCommon.Model;
using OzCommon.Utils;
using OzCommon.ViewModel;

namespace OPSAgentDialer.ViewModel
{
    public class PreferencesViewModel : ViewModelBase
    {
        private IGenericSettingsRepository<AppPreferences> _preferences;

        public RelayCommand Ok { get; private set; }
        public RelayCommand Cancel { get; private set; }

        public ObservableCollectionEx<AgentEntry> Agents { get; set; }
        public ObservableCollectionEx<RetryState> RetryStates { get; set; }
        public int MaxConcurrentCalls { get; set; }

        public bool ChooseByOccupation { get; set; }
        public bool ChooseByStatistics { get; set; }

        public PreferencesViewModel()
        {
            InitCommands();
            _preferences = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<IGenericSettingsRepository<AppPreferences>>();

            var settings = _preferences.GetSettings();
            if (settings == null)
                return;

            RetryStates = settings.RetryStates;
            Agents = settings.Agents;
            MaxConcurrentCalls = settings.MaxConcurrentCalls;
            ChooseByOccupation = settings.ChooseByOccupation;
            ChooseByStatistics = settings.ChooseByStatistics;
            ApiExtensionId = settings.ExtensionId;
        }

        public string ApiExtensionId { get; set; }

        private void InitCommands()
        {

            Ok = new RelayCommand(() =>
                                      {
                                          _preferences.SetSettings(new AppPreferences
                                          {
                                              Agents = Agents,
                                              RetryStates = RetryStates,
                                              MaxConcurrentCalls = MaxConcurrentCalls,
                                              ChooseByOccupation = ChooseByOccupation,
                                              ChooseByStatistics = ChooseByStatistics,
                                              ExtensionId = ApiExtensionId
                                          });

                                          Messenger.Default.Send(new NotificationMessage(Messages.DismissSettingsWindow));
                                      });

            Cancel = new RelayCommand(() => Messenger.Default.Send(new NotificationMessage(Messages.DismissSettingsWindow)));
        }
    }
}
