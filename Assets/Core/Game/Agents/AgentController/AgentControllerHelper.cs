using UnityEngine;
using RTSLockstep.Data;
using RTSLockstep.Utility.FastCollections;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Player;
using RTSLockstep.Integration;

namespace RTSLockstep.Agents.AgentControllerSystem
{
    /// <summary>
    /// At the moment a simple script that automatically creates AgentControllers at the start of games
    /// </summary>
    public class AgentControllerHelper : BehaviourHelper
    {
        [SerializeField, DataCode("AgentControllers")]
        private string _environmentController;
        public string EnvironmentController { get { return _environmentController; } }
        [SerializeField, DataCode("AgentControllers")]
        private string _defaultController;
        public string DefaultController { get { return _defaultController; } }

        public static AgentControllerHelper Instance { get; private set; }
        BiDictionary<string, byte> CodeIDMap = new BiDictionary<string, byte>();

        protected override void OnInitialize()
        {
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
                if (item.PlayerManaged)
                {
                    PlayerManager.AddController(controller);
                }

                PlayerManager.CreatePlayer(controller);
                CodeIDMap.Add(item.Name, controller.ControllerID);
            }
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