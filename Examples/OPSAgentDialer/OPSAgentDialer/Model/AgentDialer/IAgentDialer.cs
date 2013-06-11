namespace OPSAgentDialer.Model.AgentDialer
{
    interface IAgentDialer
    {
        void Start();
        void Stop();

        bool Running { get; }
    }
}
