using UnityEngine;

using RTSLockstep.Abilities.Essential;
using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Agents.Player;

namespace RTSLockstep.Managers.GameManagers
{
    public class LevelLoader : BehaviourHelper
    {
        [SerializeField]
        private LSAgentSpawnInfo[] Spawns;
        public bool AutoCommand = true;

        protected override void OnInitialize()
        {
        }

        protected override void OnVisualize()
        {
            //if (Input.GetKeyDown(KeyCode.M))
            //{
            //    LaunchSpawns();
            //}
        }

        protected override void OnGameStart()
        {
            LaunchSpawns();
        }

        //integrate into LSF...
        //void OnEnable()
        //{
        //    //Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
        //    SceneManager.sceneLoaded += OnLevelFinishedLoading;
        //}

        //void OnDisable()
        //{
        //    //Tell our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled. Remember to always have an unsubscription for every delegate you subscribe to!
        //    SceneManager.sceneLoaded -= OnLevelFinishedLoading;
        //}

        //void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
        //{
        //    if (ResourceManager.LevelName != null && ResourceManager.LevelName != "")
        //    {
        //        LoadManager.LoadGame(ResourceManager.LevelName);
        //    }
        //    Time.timeScale = 1.0f;
        //    ResourceManager.MenuOpen = false;
        //}

        public void LaunchSpawns()
        {
            foreach (LSAgentSpawnInfo info in Spawns)
            {
                LocalAgentController controller = PlayerConfigurationHelper.Instance.GetInstanceManager(info.ControllerCode);

                for (int j = 0; j < info.Count; j++)
                {
                    LSAgent agent = controller.CreateAgent(info.AgentCode, info.Position);
                    // remove the clone tag and replace with global ID
                    agent.gameObject.name = agent.gameObject.name.Replace("(Clone)", "") + "_" + agent.GlobalID;
                    if (AutoCommand)
                    {
                        Selector.Add(agent);
                    }
                }
            }

            if (AutoCommand)
            {
                //Find average of spawn positions
                Vector2d battlePos = Vector2d.zero;
                foreach (LSAgentSpawnInfo info in Spawns)
                {
                    battlePos += info.Position;
                }

                battlePos /= Spawns.Length;
                Command com = new Command(Data.AbilityDataItem.FindInterfacer<Attack>().ListenInputID);
                com.Add(battlePos);

                GlobalAgentController.SendCommand(com);
                Selector.Clear();
            }
        }
    }
}