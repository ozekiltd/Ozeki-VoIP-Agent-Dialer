using System.ComponentModel;

namespace OPSAgentDialer.Model
{
    public class CustomerState : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ProgressState _progressState;
        private AgentEntry _handlingAgent;

        public ProgressState ProgressState
        {
            get { return _progressState; }
            set
            {
                _progressState = value;
                OnPropertyChanged("ProgressState");
            }
        }

        public AgentEntry HandlingAgent
        {
            get { return _handlingAgent; }
            set { _handlingAgent = value; OnPropertyChanged("HandlingAgent"); }
        }

        public CustomerState()
        {
            ProgressState = ProgressState.Idle;
        }

        public CustomerState(ProgressState state, AgentEntry agent)
        {
            ProgressState = state;
            HandlingAgent = agent;
        }

        public override string ToString()
        {
            return ProgressState + (HandlingAgent == null ? "" : string.Format("(Agent {0})", HandlingAgent.AgentInfo.Name));
        }

        public void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
