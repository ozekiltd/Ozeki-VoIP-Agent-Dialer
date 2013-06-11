using System;
using System.Linq;
using GalaSoft.MvvmLight.Command;
using OPSAgentDialer.Model;
using OPSAgentDialer.Model.AgentDialer;
using OPSAgentDialer.Model.Settings;
using OPSSDK;
using OzCommon.Model;
using OzCommon.Utils;
using OzCommon.Utils.Schedule;
using OzCommonBroadcasts.ViewModel;

namespace OPSAgentDialer.ViewModel
{
    class AgentDialerViewModel : BroadcastMainViewModel<CustomerEntry>
    {
        public static string ShowApiExtensionWarning = Guid.NewGuid().ToString();
        public static string ShowNoAgentsSelectedError = Guid.NewGuid().ToString();

        private AgentDialer _agentDialer;
        private IClient _client;
        private IGenericSettingsRepository<AppPreferences> _settingsRepository;

        public AgentDialerViewModel()
        {
            _client = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<IClient>();
            _settingsRepository = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<IGenericSettingsRepository<AppPreferences>>();
            _agentDialer = (AgentDialer) GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<IScheduler<CustomerEntry>>();

            InitAgentDialerCommands();
            InitSettings();
        }

        protected override object GetSettingsViewModel()
        {
            return new PreferencesViewModel();
        }

        protected override int GetMaxConcurrentWorkers()
        {
            return _agentDialer.MaxConcurrentWorkers;
        }

        protected override string GetApiExtensionID()
        {
            var settings = _settingsRepository.GetSettings();
            return settings == null ? "" : settings.ExtensionId;

        }

        private void InitAgentDialerCommands()
        {
            Start = new RelayCommand(InitStart, () => !_agentDialer.Working && Customers.Count > 0);
            Stop = new RelayCommand(InitStop, () => _agentDialer.Working);
        }

        private void InitStart()
        {
            DoneJobs = 0;
            IsReadOnly = true;
            CheckedJobs = 0;
            var settings = _settingsRepository.GetSettings();
            var enabledAgents = settings.Agents.Where(agent => agent.Operational);
            var enabledStates = settings.RetryStates.Where(state => state.Enabled).Concat(new[] { new RetryState { Enabled = true, ProgressState = ProgressState.Idle } });


            _agentDialer.MaxConcurrentWorkers = settings.MaxConcurrentCalls;
            _agentDialer.Agents = new ObservableCollectionEx<AgentEntry>();
            _agentDialer.Agents.AddItems(enabledAgents);
            _agentDialer.RetryStates = new ObservableCollectionEx<RetryState>();
            _agentDialer.RetryStates.AddItems(enabledStates);
            _agentDialer.Customers = Customers;

            _agentDialer.DialerBehaviour = settings.ChooseByOccupation
                                               ? AgentDialerBehaviour.ChooseByOccupation
                                               : AgentDialerBehaviour.ChooseByStatistics;

            _agentDialer.Start();
        }

        private void InitStop()
        {
            _agentDialer.Stop();
        }

        private void InitSettings()
        {
            var settings = _settingsRepository.GetSettings();
            var users = _client.GetPhoneBook();

            if (settings == null)
            {
                var newSettings = new AppPreferences();
                var newAgents = new ObservableCollectionEx<AgentEntry>();
                var retryStates = new ObservableCollectionEx<RetryState>()
                                      {
                                          new RetryState { Enabled = true, ProgressState = ProgressState.Error },
                                          new RetryState { Enabled =  true, ProgressState = ProgressState.Rejected },
                                          new RetryState { Enabled =  true, ProgressState = ProgressState.Aborted },
                                          new RetryState { Enabled =  true, ProgressState = ProgressState.NotFound }
                                      };

                newAgents.AddItems(users.Select(userInfo => new AgentEntry(new PhoneBookItemInfo(userInfo), AgentState.Free)));
                newSettings.Agents = newAgents;
                newSettings.RetryStates = retryStates;
                newSettings.ChooseByOccupation = true;
                newSettings.ChooseByStatistics = false;
                newSettings.MaxConcurrentCalls = 5;

                _settingsRepository.SetSettings(newSettings);
                return;
            }

            foreach (var userInfo in users.Where(userInfo => !settings.Agents.Any(agent => agent.AgentInfo.Name.Equals(userInfo.Name))))
                settings.Agents.Add(new AgentEntry(new PhoneBookItemInfo(userInfo), AgentState.Free));

            var localAgents = settings.Agents.ToList();

            foreach (var agent in localAgents.Where(agent => !users.Any(user => user.Name.Equals(agent.AgentInfo.Name))))
                settings.Agents.Remove(settings.Agents.FirstOrDefault(a => a.AgentInfo.Name.Equals(agent.AgentInfo.Name)));

            _settingsRepository.SetSettings(settings);
        }
    }
}
