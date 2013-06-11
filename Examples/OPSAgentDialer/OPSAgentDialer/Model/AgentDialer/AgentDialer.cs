using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using OPSAgentDialer.Model.Settings;
using OPSAgentDialer.ViewModel;
using OPSSDK;
using OPSSDKCommon.Model.Call;
using OzCommon.Model;
using OzCommon.Utils;
using OzCommon.Utils.Schedule;
using Ozeki.VoIP;

namespace OPSAgentDialer.Model.AgentDialer
{
    class AgentDialer : Scheduler<CustomerEntry>, IAgentDialer
    {
        private AutoResetEvent _eventLock;
        private IClient _client;
        private object _sync;
        private ConcurrentDictionary<string, ISession> _activeSessions;

        public ObservableCollectionEx<CustomerEntry> Customers { get; set; }
        public ObservableCollectionEx<AgentEntry> Agents { get; set; }
        public ObservableCollectionEx<RetryState> RetryStates { get; set; }
        private IGenericSettingsRepository<AppPreferences> _settingsRepository;
        private IAPIExtension apiExtension;
        private ConcurrentList<ICall> _startedCalls;

        public bool Running { get { return Working; } private set { Working = value; } }
        public AgentDialerBehaviour DialerBehaviour { get; set; }

        public void Start()
        {
            StartWorks(null);
        }

        public void Stop()
        {
            StopWorks();

            foreach (var call in _startedCalls)
            {
                if (call.CallState.IsRinging())
                {
                    call.CallStateChanged -= ApiCallStateChanged;
                    call.HangUp();
                }
            }

            _startedCalls.Clear();

            foreach (var customer in Customers)
            {
                if (customer.State.ProgressState == ProgressState.Calling)
                {
                    customer.State = new CustomerState(ProgressState.Aborted, customer.State.HandlingAgent);
                }
                else if (customer.State.ProgressState == ProgressState.InProgress)
                {
                    customer.State = new CustomerState(ProgressState.Completed, customer.State.HandlingAgent);
                }
            }

            
        }

        public AgentDialer()
            : base(null)
        {
            _eventLock = new AutoResetEvent(true);
            _sync = new object();
            _client = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<IClient>();
            _client.SessionCreated += ClientOnSessionCreated;
            _client.SessionCompleted += ClientOnSessionCompleted;
            _settingsRepository = GalaSoft.MvvmLight.Ioc.SimpleIoc.Default.GetInstance<IGenericSettingsRepository<AppPreferences>>();
            _activeSessions = new ConcurrentDictionary<string, ISession>();
            CurrentConcurrentWorkers = 0;
            CompletedWorks = 0;
            _startedCalls = new ConcurrentList<ICall>();
        }

        public override void StartWorks(IList<CustomerEntry> jobsTodo)
        {
            if (Customers == null || !Customers.Any())
                throw new ArgumentException("No customers added");

            if (MaxConcurrentWorkers == 0)
                throw new InvalidOperationException("MaxConcurrentWorkers property not set");

            if (MaxConcurrentWorkers > Agents.Count)
                MaxConcurrentWorkers = Agents.Count;

            Running = true;
            CurrentConcurrentWorkers = 0;

            Task.Factory.StartNew(() => InternalStart(null));
        }

        protected override void InternalStart(IList<CustomerEntry> jobsTodo)
        {
            if (apiExtension == null)
            {
                var extId = _settingsRepository.GetSettings().ExtensionId;
                if (string.IsNullOrEmpty(extId) || (apiExtension = _client.GetAPIExtension(extId)) == null)
                {
                    Messenger.Default.Send(new NotificationMessage(AgentDialerViewModel.ShowApiExtensionWarning));
                    Running = false;
                    CurrentConcurrentWorkers = 0;
                    return;
                }
            }

            if (!Agents.Any())
            {
                Messenger.Default.Send(new NotificationMessage(AgentDialerViewModel.ShowNoAgentsSelectedError));
                Running = false;
                CurrentConcurrentWorkers = 0;
                return;
            }

            foreach (var customer in Customers)
            {
                if (!RetryStates.Any(s => s.ProgressState == customer.State.ProgressState) || CurrentConcurrentWorkers >= MaxConcurrentWorkers)
                {
                    continue;
                }

                while (!Agents.Any(a => a.State == AgentState.Free))
                {
                    _eventLock.WaitOne();
                }

                var call = apiExtension.CreateCall(customer.PhoneNumber);

                if (call == null)
                {
                    continue;
                }

                customer.CallId = call.CallId;
                customer.State = new CustomerState(ProgressState.Calling, null);
                call.CallStateChanged += ApiCallStateChanged;
                call.CallErrorOccurred += (sender, args) => CallEnded(null, Customers.First(c => c.CallId == customer.CallId), args.Item == CallError.NotFound ? ProgressState.NotFound : ProgressState.Error);
                _startedCalls.TryAdd(call);
                call.Start();

                Interlocked.Increment(ref CurrentConcurrentWorkers);

                while (CurrentConcurrentWorkers >= MaxConcurrentWorkers)
                    _eventLock.WaitOne();
            }

            Running = false;
            OnWorksCompleted(CompletedWorks);
        }

        private void ApiCallStateChanged(object sender, VoIPEventArgs<CallState> e)
        {
            lock (_sync)
            {
                var call = (ICall)sender;
                if (e.Item == CallState.InCall)
                {
                    var handlingAgent = DialerBehaviour == AgentDialerBehaviour.ChooseByOccupation
                                            ? Agents.FirstOrDefault(agent => agent.State == AgentState.Free)
                                            : Agents.Where(agent => agent.State == AgentState.Free)
                                                    .OrderBy(agent => agent.NumberOfCalls)
                                                    .FirstOrDefault();

                    if (handlingAgent != null)
                    {
                        handlingAgent.State = AgentState.Occupied;
                        handlingAgent.CurrentCallId = call.CallId;
                        handlingAgent.NumberOfCalls++;

                        var target = handlingAgent.AgentInfo.PhoneNumber;
                       call.BlindTransfer(target);

                        var handledCustomer = Customers.FirstOrDefault(customer => customer.CallId == call.CallId);
                        if (handledCustomer != null)
                            handledCustomer.State = new CustomerState(ProgressState.InProgress, handlingAgent);
                    }
                }
                else if (e.Item.IsCallEnded())
                {
                    var customer = Customers.FirstOrDefault(c => c.CallId == call.CallId);
                    if (customer != null)
                    {
                        var agent = Agents.FirstOrDefault(a => customer.State.HandlingAgent == a);
                        if (agent == null)
                        {
                            CallEnded(agent, customer, e.Item == CallState.Busy ? ProgressState.Rejected : ProgressState.Completed);
                        }
                    }

                    _startedCalls.TryRemove(call);
                }
            }
        }

        private void CallEnded(AgentEntry agent, CustomerEntry customer, ProgressState state)
        {

            Interlocked.Decrement(ref CurrentConcurrentWorkers);
            Interlocked.Increment(ref CompletedWorks);

            if (agent != null)
            {
                agent.State = AgentState.Free;
            }

            if (customer != null)
            {
                customer.State = new CustomerState(state, customer.State.HandlingAgent);
            }

            _eventLock.Set();
            OnOneWorkCompleted(new WorkResult { IsSuccess  = true });

            if (CurrentConcurrentWorkers < MaxConcurrentWorkers)
                _eventLock.Set();
        }

        private void ClientOnSessionCreated(object sender, VoIPEventArgs<ISession> e)
        {
            _activeSessions.TryAdd(e.Item.SessionID, e.Item);
            Debug.WriteLine("Session created " + e.Item.Caller + " -> " + e.Item.Callee);
        }

        private void ClientOnSessionCompleted(object sender, VoIPEventArgs<ISession> e)
        {
            ISession session;
            _activeSessions.TryRemove(e.Item.SessionID, out session);


            var agentFinished = Agents.FirstOrDefault(a => a.AgentInfo.Extensions.Contains(session.Callee));
            if (agentFinished != null && !_activeSessions.Any(s => agentFinished.AgentInfo.Extensions.Contains(s.Value.Callee)))
            {
                CallEnded(agentFinished, Customers.FirstOrDefault(customer => customer.State.HandlingAgent == agentFinished
                    && customer.State.ProgressState == ProgressState.InProgress), ProgressState.Completed);
            }

            Debug.WriteLine("Session completed " + e.Item.Caller + " -> " + e.Item.Callee);
        }
    }
}
