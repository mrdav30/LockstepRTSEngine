using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RTSLockstep;
using RTSLockstep.Data;
using FastCollections;

namespace RTSLockstep
{
    public class ResourceManager
    {
        #region properties
        public static int ScrollWidth { get { return 15; } }
        public static float ScrollSpeed { get { return 25; } }
        public static float RotateAmount { get { return 10; } }
        public static float RotateSpeed { get { return 100; } }
        public static float MinCameraHeight { get { return 10; } }
        public static float MaxCameraHeight { get { return 40; } }
        public static Vector3d InvalidPosition { get { return invalidPosition; } }
        public static GUISkin SelectBoxSkin { get { return selectBoxSkin;  } }
        public static Bounds InvalidBounds { get { return invalidBounds;  } }
        public static int HarvestSpeed { get { return 1; } }
        public static int BuildSpeed { get { return 1; } }  // used to make sure that all buildings update their progress for building Units at the same rate
        public static Texture2D HealthyTexture { get { return healthyTexture; } }
        public static Texture2D DamagedTexture { get { return damagedTexture; } }
        public static Texture2D CriticalTexture { get { return criticalTexture; } }
        public static bool MenuOpen { get; set; }
        public static float PauseMenuHeight { get { return headerHeight + 2 * buttonHeight + 4 * padding; } }
        public static float MenuWidth { get { return headerWidth + 2 * padding; } }
        public static float ButtonHeight { get { return buttonHeight; } }
        public static float ButtonWidth { get { return (MenuWidth - 3 * padding) / 2; } }
        public static float HeaderHeight { get { return headerHeight; } }
        public static float HeaderWidth { get { return headerWidth; } }
        public static float TextHeight { get { return textHeight; } }
        public static float Padding { get { return padding; } }
        public static string LevelName { get; set; }

        private static GUISkin selectBoxSkin;
        private static Vector3d invalidPosition = new Vector3d(-99999, -99999, -99999);
        private static Bounds invalidBounds = new Bounds(new Vector3(-99999, -99999, -99999), new Vector3(0, 0, 0));
      //  private static GameObjectList gameObjectList;
        private static Texture2D healthyTexture, damagedTexture, criticalTexture;
        private static Dictionary<ResourceType, Texture2D> resourceHealthBarTextures;
        private static float buttonHeight = 40;
        private static float headerHeight = 32, headerWidth = 256;
        private static float textHeight = 25, padding = 15;

        public static readonly Dictionary<string, IAgentData> AgentCodeInterfacerMap = new Dictionary<string, IAgentData>();
        public static readonly Dictionary<string, LSAgent> AgentCodeTemplateMap = new Dictionary<string, LSAgent>();
        public static IAgentData[] AgentData;

        public static Transform OrganizerObject;
        public static string[] AgentCodes;
        private static readonly Dictionary<string, ushort> AgentCodeIndexMap = new Dictionary<string, ushort>();

        public static readonly Dictionary<string, IWorldObjectData> WorldObjectCodeInterfacerMap = new Dictionary<string, IWorldObjectData>();
        public static IWorldObjectData[] WorldObjectData;
        public static string[] WorldObjectCodes;

        //public static readonly Dictionary<string, IAvatarData> AvatarCodeInterfacerMap = new Dictionary<string, IAvatarData>();
        public static IAvatarData[] AvatarData;
        public static Texture2D[] Avatars;
       // public static string[] AvatarCodes;

        #endregion

        #region MonoBehavior
        public static void Setup()
        {
            IAgentDataProvider agentDatabase;
            if (LSDatabaseManager.TryGetDatabase<IAgentDataProvider>(out agentDatabase))
            {
                AgentData = agentDatabase.AgentData;
                AgentCodes = new string[AgentData.Length];

                AgentController.CachedAgents = new Dictionary<string, FastStack<LSAgent>>(AgentData.Length);

                OrganizerObject = LSUtility.CreateEmpty().transform;
                OrganizerObject.gameObject.name = "OrganizerObject";
                OrganizerObject.gameObject.SetActive(false);

                GameObject.DontDestroyOnLoad(OrganizerObject);
                for (int i = 0; i < AgentData.Length; i++)
                {
                    IAgentData interfacer = AgentData[i];
                    string agentCode = interfacer.Name;
                    AgentCodes[i] = agentCode;

                    AgentController.CachedAgents.Add(agentCode, new FastStack<LSAgent>(2));
                    AgentCodeInterfacerMap.Add(agentCode, interfacer);
                    AgentCodeIndexMap.Add(agentCode, (ushort)i);
                }
            }
            else
            {
                Debug.Log("Database does not provide AgentData. Make sure it implements IAgentDataProvider.");
            }

            IWorldObjectDataProvider worldObjectDatabase;
            if (LSDatabaseManager.TryGetDatabase<IWorldObjectDataProvider>(out worldObjectDatabase))
            {
                WorldObjectData = worldObjectDatabase.WorldObjectData;
                WorldObjectCodes = new string[AgentData.Length];

                for (int i = 0; i < WorldObjectData.Length; i++)
                {
                    IWorldObjectData interfacer = WorldObjectData[i];
                    string worldObjectCode = interfacer.Name;
                    WorldObjectCodes[i] = worldObjectCode;
                    
                    WorldObjectCodeInterfacerMap.Add(worldObjectCode, interfacer);
                }
            }
            else
            {
                Debug.Log("Database does not provide WorldObjectData. Make sure it implements IWorldObjectDataProvider.");
            }

            IAvatarDataProvider avatarDatabase;
            if (LSDatabaseManager.TryGetDatabase<IAvatarDataProvider>(out avatarDatabase))
            {
                AvatarData = avatarDatabase.AvatarData;
                Avatars = new Texture2D[AvatarData.Length];

                for (int i = 0; i < AvatarData.Length; i++)
                {
                    IAvatarData interfacer = AvatarData[i];
                    Texture2D avatar = interfacer.GetAvatar();
                    //  AvatarCodes[i] = avatarCode;
                    Avatars[i] = avatar;
                   // AvatarCodeInterfacerMap.Add(avatarCode, interfacer);
                }
            }
            else
            {
                Debug.Log("Database does not provide WorldObjectData. Make sure it implements IWorldObjectDataProvider.");
            }
        }

        void Initialize()
        {
            //if (this != Instance)
            //{
            //    return;
            //}
         //   ResourceManager.SetGameObjectList(this);
            RTSLockstep.PlayerManager.Load();
            RTSLockstep.PlayerManager.SetAvatarTextures(Avatars);
        }
        #endregion

        #region Public
        public static IAgentData GetAgentInterfacer(string agentCode)
        {
            return ResourceManager.AgentCodeInterfacerMap[agentCode];
        }

        public static LSAgent GetAgentTemplate(string agentCode)
        {
            LSAgent template;
            if (!AgentCodeTemplateMap.TryGetValue(agentCode, out template))
            {
                template = GameObject.Instantiate(ResourceManager.GetAgentSource(agentCode));
                AgentCodeTemplateMap.Add(agentCode, template);
                template.transform.parent = OrganizerObject.transform;
            }

            return template;
        }

        public static ushort GetAgentCodeIndex(string agentCode)
        {
            return AgentCodeIndexMap[agentCode];
        }

        public static string GetAgentCode(ushort id)
        {
            return AgentCodes[id];
        }

        public static bool IsValidAgentCode(string code)
        {
            return AgentCodeInterfacerMap.ContainsKey(code);
        }

        public static LSAgent GetAgentSource(string agentCode)
        {
            IAgentData interfacer = ResourceManager.AgentCodeInterfacerMap[agentCode];
            return interfacer.GetAgent();
        }

        public static GameObject GetWorldObject(string worldObjectCode)
        {
            IWorldObjectData interfacer = ResourceManager.WorldObjectCodeInterfacerMap[worldObjectCode];
            return interfacer.GetWorldObject();
        }

        public static GameObject GetCommanderObject()
        {
            return GetWorldObject("AgentCommander");
        }

        public static Texture2D GetBuildImage(string name)
        {
            IAgentData interfacer = ResourceManager.AgentCodeInterfacerMap[name];
            return interfacer.GetAgentIcon();
        }

        public static Texture2D[] GetAvatars()
        {
            //Texture2D[] avatars;
            return Avatars;
        }

        public static void StoreSelectBoxItems(GUISkin skin, Texture2D healthy, Texture2D damaged, Texture2D critical)
        {
            selectBoxSkin = skin;
            healthyTexture = healthy;
            damagedTexture = damaged;
            criticalTexture = critical;
        }

        public static Texture2D GetResourceHealthBar(ResourceType resourceType)
        {
            if (resourceHealthBarTextures != null && resourceHealthBarTextures.ContainsKey(resourceType))
            {
                return resourceHealthBarTextures[resourceType];
            }
            return null;
        }

        public static void SetResourceHealthBarTextures(Dictionary<ResourceType, Texture2D> images)
        {
            resourceHealthBarTextures = images;
        }

        //public static int GetNewObjectId()
        //{
        //    LevelLoader loader = (LevelLoader)GameObject.FindObjectOfType(typeof(LevelLoader));
        //    if (loader)
        //    {
        //        return loader.GetNewObjectId();
        //    }
        //    return -1;
        //}
        #endregion
    }
}
