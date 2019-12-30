using RTSLockstep.Environment;

namespace RTSLockstep.Networking
{
    public class LSNetworkSettingsSaver : EnvironmentSaver
    {
        public LSNetworkSettings SavedSettings = new LSNetworkSettings();
        protected override void OnEarlyApply()
        {
            LSNetworkSettings.Settings = SavedSettings;
        }
    }
}
