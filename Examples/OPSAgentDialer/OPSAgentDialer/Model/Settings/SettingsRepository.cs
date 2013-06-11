using OzCommon.Model;
using OzCommon.Utils;

namespace OPSAgentDialer.Model.Settings
{
    public class SettingsRepository : GenericSettingsRepository<AppPreferences>
    {
        public SettingsRepository()
            : base("OPSAgentDialer")
        { }

        public void Merge(AppPreferences preferences)
        {
            var currentSettings = GetSettings();

            if (preferences.Agents != null)
                currentSettings.Agents = preferences.Agents;

            if (preferences.RetryStates != null)
                currentSettings.RetryStates = preferences.RetryStates;

            SetSettings(currentSettings);
        }
    }
}
