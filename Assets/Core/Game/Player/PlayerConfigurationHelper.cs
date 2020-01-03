using UnityEngine;

using RTSLockstep.Data;
using RTSLockstep.Utility.FastCollections;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Player;
using RTSLockstep.Integration;
using RTSLockstep.Agents.AgentController;

namespace RTSLockstep.Agents.Player
{
    /// <summary>
    /// At the moment a simple script that automatically creates AgentControllers at the start of games
    /// Plan to develop into player selection helper
    /// </summary>
    public class PlayerConfigurationHelper : BehaviourHelper
    {
        [SerializeField, DataCode("AgentControllers")]
        private string _environmentController;
        public string EnvironmentController { get { return _environmentController; } }
        [SerializeField, DataCode("AgentControllers")]
        public string SelectedController;

        public static PlayerConfigurationHelper Instance { get; private set; }
        BiDictionary<string, byte> CodeIDMap = new BiDictionary<string, byte>();

        protected override void OnInitialize()
        {
            if (SelectedController.Length == 0)
            {
                Debug.LogError("You need to pick a controller to control!");
            }

            Instance = this;

            if (!LSDatabaseManager.TryGetDatabase(out IAgentControllerDataProvider database))
            {
                Debug.LogError("IAgentControllerDataProvider unavailable.");
            }

            AgentControllerDataItem[] controllerItems = database.AgentControllerData;
            for (int i = 0; i < controllerItems.Length; i++)
            {
                AgentControllerDataItem item = controllerItems[i];
                LocalAgentController controller = GlobalAgentController.Create(item.DefaultAllegiance, item.Name);

                bool isPlayerManaged = item.Name == SelectedController ? true : false;

                PlayerManager.AddPlayer(controller, isPlayerManaged);
                CodeIDMap.Add(item.Name, controller.ControllerID);
            }

            PlayerManager.InitializePlayers();
        }

        public LocalAgentController GetInstanceManager(string controllerCode)
        {
            if (string.IsNullOrEmpty(controllerCode))
            {
                Debug.Log("controllerCode is null or empty.");
                return null;
            }

            if (!CodeIDMap.TryGetValue(controllerCode, out byte id))
            {
                Debug.Log("Controller name " + controllerCode + " is not valid.");
            }

            return GlobalAgentController.GetInstanceManager(id);
        }
    }
}