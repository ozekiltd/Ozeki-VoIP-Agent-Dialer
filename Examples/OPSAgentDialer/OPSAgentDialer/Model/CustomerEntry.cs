using System;
using System.ComponentModel;
using OzCommonBroadcasts.Model;

namespace OPSAgentDialer.Model
{
    public class CustomerEntry : EventArgs, INotifyPropertyChanged, ICompletedWork
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private CustomerState _state;
        private string _callId;

        public string Name { get; private set; }
        public string PhoneNumber { get; private set; }

        public CustomerEntry()
        {
            _state = new CustomerState();
        }

        public CustomerEntry(string name, string phoneNumber)
        {
            Name = name;
            PhoneNumber = phoneNumber;
            _state = new CustomerState();
            _state.PropertyChanged += PropertyChanged;
        }

        [InvisibleProperty]
        public string CallId
        {
            get { return _callId; }
            set
            {
                _callId = value;
                OnPropertyChanged("CallId");
            }
        }

        [ExportIgnoreProperty]
        [ReadOnlyProperty]
        public CustomerState State
        {
            get { return _state; }
            set
            {
                _state = value;
                OnPropertyChanged("State");
            }
        }

        public virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        [InvisibleProperty]
        [ExportIgnoreProperty]
        public bool IsCompleted { get; set; }

        [InvisibleProperty]
        [ExportIgnoreProperty]
        public bool IsValid { get { return true; } }
    }
}
