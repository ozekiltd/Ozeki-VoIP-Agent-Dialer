using OPSAgentDialer.Model.Settings;
using OPSSDK;
using OPSSDKCommon.Model;

namespace OPSAgentDialer.Model
{
    public class AgentEntry
    {
        public PhoneBookItemInfo AgentInfo { get; set; }
        public AgentState State { get; set; }
        public string CurrentCallId { get; set; }
        public bool Operational { get; set; }
        public int NumberOfCalls { get; set; }

        public AgentEntry()
        {
            NumberOfCalls = 0;
        }

        public AgentEntry(PhoneBookItemInfo agentInfo, AgentState state)
            : this()
        {
            AgentInfo = agentInfo;
            State = state;
        }
    }
}
