using FastCollections;
using RTSLockstep.Data;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public static class GameResourceManager
    {
        #region properties
        public static int ScrollWidth { get { return 15; } }
        public static float ScrollSpeed { get { return 25; } }
        //camera rotate speed
        public static float RotateSpeedH { get { return 5f; } }
        public static float RotateSpeedV { get { return 5f; } }
        // limit away from ground movement to be between a minimum and maximum distance
        public static float MinCameraHeight { get { return 10; } }
        public static float MaxCameraHeight { get { return 40; } }
        public static Vector3d InvalidPosition { get { return _invalidPosition; } }
        public static GUISkin SelectBoxSkin { get { return _selectBoxSkin; } }
        public static Bounds InvalidBounds { get { return _invalidBounds; } }
        // used to make sure that all buildings update their progress for building Units at the same rate
        //public static int GlobalHarvestSpeed { get { return 1; } }
        //public static int GlobalBuildSpeed { get { return 1; } }
        public static Texture2D HealthyTexture { get { return _healthyTexture; } }
        public static Texture2D DamagedTexture { get { return _damagedTexture; } }
        public static Texture2D CriticalTexture { get { return _criticalTexture; } }
        public static Material NotAllowedMaterial { get { return _notAllowedMaterial; } }
        public static Material AllowedMaterial { get { return _allowedMaterial; } }
        public static bool MenuOpen { get; set; }
        public static float PauseMenuHeight { get { return _headerHeight + 2 * _buttonHeight + 4 * _padding; } }
        public static float MenuWidth { get { return _headerWidth + 2 * _padding; } }
        public static float ButtonHeight { get { return _buttonHeight; } }
        public static float ButtonWidth { get { return (MenuWidth - 3 * _padding) / 2; } }
        public static float HeaderHeight { get { return _headerHeight; } }
        public static float HeaderWidth { get { return _headerWidth; } }
        public static float TextHeight { get { return _textHeight; } }
        public static float Padding { get { return _padding; } }
        public static string LevelName { get; set; }

        private static GUISkin _selectBoxSkin;
        private static Vector3d _invalidPosition = new Vector3d(-99999, -99999, -99999);
        private static Bounds _invalidBounds = new Bounds(new Vector3(-99999, -99999, -99999), new Vector3(0, 0, 0));
        //  private static GameObjectList gameObjectList;
        private static Texture2D _healthyTexture, _damagedTexture, _criticalTexture;
        private static Material _allowedMaterial, _notAllowedMaterial;
        private static Dictionary<ResourceType, Texture2D> _resourceHealthBarTextures;
        private static float _buttonHeight = 40;
        private static float _headerHeight = 32, _headerWidth = 256;
        private static float _textHeight = 25, _padding = 15;

        public static readonly Dictionary<string, IAgentData> AgentCodeInterfacerMap = new Dictionary<string, IAgentData>();
        public static readonly Dictionary<string, RTSAgent> AgentCodeTemplateMap = new Dictionary<string, RTSAgent>();
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

        public static FastList<Color> AssignedTeamColors = new FastList<Color>();

        #endregion

        #region MonoBehavior
        public static void Setup()
        {
            IAgentDataProvider agentDatabase;
            if (LSDatabaseManager.TryGetDatabase<IAgentDataProvider>(out agentDatabase))
            {
                AgentData = agentDatabase.AgentData;
                AgentCodes = new string[AgentData.Length];

                AgentController.CachedAgents = new Dictionary<string, FastStack<RTSAgent>>(AgentData.Length);

                OrganizerObject = LSUtility.CreateEmpty().transform;
                OrganizerObject.gameObject.name = "OrganizerObject";
                OrganizerObject.gameObject.SetActive(false);

                GameObject.DontDestroyOnLoad(OrganizerObject);
                for (int i = 0; i < AgentData.Length; i++)
                {
                    IAgentData interfacer = AgentData[i];
                    string agentCode = interfacer.Name;
                    AgentCodes[i] = agentCode;

                    AgentController.CachedAgents.Add(agentCode, new FastStack<RTSAgent>(2));
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

        public static void Initialize()
        {
            PlayerManager.Load();
            PlayerManager.SetAvatarTextures(Avatars);
        }
        #endregion

        #region Public
        public static IAgentData GetAgentInterfacer(string agentCode)
        {
            return AgentCodeInterfacerMap[agentCode];
        }

        public static RTSAgent GetAgentTemplate(string agentCode)
        {
            RTSAgent template;
            if (!AgentCodeTemplateMap.TryGetValue(agentCode, out template))
            {
                template = Object.Instantiate(GetAgentSource(agentCode));
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

        public static RTSAgent GetAgentSource(string agentCode)
        {
            IAgentData interfacer = GameResourceManager.AgentCodeInterfacerMap[agentCode];
            return interfacer.GetAgent();
        }

        public static GameObject GetWorldObject(string worldObjectCode)
        {
            IWorldObjectData interfacer = GameResourceManager.WorldObjectCodeInterfacerMap[worldObjectCode];
            return interfacer.GetWorldObject();
        }

        public static GameObject GetCommanderObject()
        {
            return GetWorldObject("AgentCommander");
        }

        public static Texture2D GetBuildImage(string name)
        {
            IAgentData interfacer = AgentCodeInterfacerMap[name];
            return interfacer.GetAgentIcon();
        }

        public static Texture2D[] GetAvatars()
        {
            return Avatars;
        }

        public static void StoreSelectBoxItems(GUISkin skin, Texture2D healthy, Texture2D damaged, Texture2D critical)
        {
            _selectBoxSkin = skin;
            _healthyTexture = healthy;
            _damagedTexture = damaged;
            _criticalTexture = critical;
        }

        public static void StoreConstructionMaterials(Material allowed, Material notAllowed)
        {
            _allowedMaterial = allowed;
            _notAllowedMaterial = notAllowed;
        }

        public static Texture2D GetResourceHealthBar(ResourceType resourceType)
        {
            if (_resourceHealthBarTextures != null && _resourceHealthBarTextures.ContainsKey(resourceType))
            {
                return _resourceHealthBarTextures[resourceType];
            }
            return null;
        }

        public static void SetResourceHealthBarTextures(Dictionary<ResourceType, Texture2D> images)
        {
            _resourceHealthBarTextures = images;
        }
        #endregion
    }
}
