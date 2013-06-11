using OzCommon.Utils;

namespace OPSAgentDialer.Model.Settings
{
    public class AppPreferences
    {
        public AppPreferences()
        {
            Agents = new ObservableCollectionEx<AgentEntry>();
            RetryStates = new ObservableCollectionEx<RetryState>();
        }

        public ObservableCollectionEx<AgentEntry> Agents { get; set; }
        public ObservableCollectionEx<RetryState> RetryStates { get; set; }

        public int MaxConcurrentCalls { get; set; }
        public bool ChooseByOccupation { get; set; }
        public bool ChooseByStatistics { get; set; }

        public string ExtensionId { get; set; }
    }
}
