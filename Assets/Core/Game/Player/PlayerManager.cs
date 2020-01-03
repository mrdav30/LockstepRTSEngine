using UnityEngine;
using Newtonsoft.Json;
using System.IO;

using RTSLockstep.Agents.AgentController;
using RTSLockstep.Utility;
using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.BehaviourHelpers;

namespace RTSLockstep.Player
{
    public static class PlayerManager
    {
        #region Properties
        private static FastBucket<PlayerDetails> _savedPlayerProfiles = new FastBucket<PlayerDetails>();
        private static FastBucket<LSPlayer> _allPlayers = new FastBucket<LSPlayer>();
        private static PlayerDetails _currentPlayer;
        private static Texture2D[] _avatars;

        public static LocalAgentController CurrentPlayerController;
        #endregion

        #region Event Behavior
        public static void Initialize()
        {
            CurrentPlayerController = null;
            _savedPlayerProfiles.FastClear();
        }

        public static void Visualize()
        {
            for (int i = 0; i < _allPlayers.Count; i++)
            {
                _allPlayers[i].OnVisualize();
            }
        }

        public static void UpdateGUI()
        {
            for (int i = 0; i < _allPlayers.Count; i++)
            {
                _allPlayers[i].OnUpdateGUI();
            }
        }
        #endregion

        #region Public

        public static void SelectPlayer(string name, int avatar, int controllerId, int playerIndex)
        {
            //check player doesnt already exist
            bool playerExists = false;
            for (int i = 0; i < _savedPlayerProfiles.PeakCount; i++)
            {
                if (_savedPlayerProfiles.arrayAllocation[i])
                {
                    PlayerDetails player = _savedPlayerProfiles[i];

                    if (player.Name == name)
                    {
                        _currentPlayer = player;
                        playerExists = true;
                    }
                }
            }

            if (!playerExists)
            {
                PlayerDetails newPlayer = new PlayerDetails(name, avatar, controllerId, playerIndex);
                _savedPlayerProfiles.Add(newPlayer);
                _currentPlayer = newPlayer;
                Directory.CreateDirectory("SavedGames" + Path.DirectorySeparatorChar + name);
            }

            Save();
        }

        // change to accept player param
        // search through players list
        public static string GetPlayerName()
        {
            return _currentPlayer.Name == "" ? "Unknown" : _currentPlayer.Name;
        }

        public static void SetAvatarTextures(Texture2D[] avatarTextures)
        {
            _avatars = avatarTextures;
        }

        //change to accept commander param
        //search through players list
        public static Texture2D GetPlayerAvatar()
        {
            if (_avatars.IsNull())
            {
                return null;
            }

            if (_currentPlayer.Avatar >= 0 && _currentPlayer.Avatar < _avatars.Length)
            {
                return _avatars[_currentPlayer.Avatar];
            }

            return null;
        }

        public static void Save()
        {
            _ = new JsonSerializer
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            using (StreamWriter sw = new StreamWriter("SavedGames" + Path.DirectorySeparatorChar + "Players.json"))
            {
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("Players");
                    writer.WriteStartArray();
                    for (int i = 0; i < _savedPlayerProfiles.PeakCount; i++)
                    {
                        if (_savedPlayerProfiles.arrayAllocation[i])
                        {
                            SavePlayer(writer, _savedPlayerProfiles[i]);
                        }
                    }
                    writer.WriteEndArray();

                    writer.WriteEndObject();
                }
            }
        }

        public static void Load()
        {
            _savedPlayerProfiles.Clear();

            string filename = "SavedGames" + Path.DirectorySeparatorChar + "Players.json";
            if (File.Exists(filename))
            {
                //read contents of file
                string input;
                using (StreamReader sr = new StreamReader(filename))
                {
                    input = sr.ReadToEnd();
                }
                if (input != null)
                {
                    //parse contents of file
                    using (JsonTextReader reader = new JsonTextReader(new StringReader(input)))
                    {
                        while (reader.Read())
                        {
                            if (reader.Value.IsNotNull())
                            {
                                if (reader.TokenType == JsonToken.PropertyName)
                                {
                                    if ((string)reader.Value == "Players")
                                    {
                                        LoadPlayers(reader);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static string[] GetPlayerNames()
        {
            string[] playerNames = new string[_savedPlayerProfiles.Count];
            for (int i = 0; i < playerNames.Length; i++)
            {
                playerNames[i] = _savedPlayerProfiles[i].Name;
            }
            return playerNames;
        }

        public static int GetAvatar(string playerName)
        {
            for (int i = 0; i < _savedPlayerProfiles.PeakCount; i++)
            {
                if (_savedPlayerProfiles.arrayAllocation[i])
                {
                    if (_savedPlayerProfiles[i].Name == playerName)
                    {
                        return _savedPlayerProfiles[i].Avatar;
                    }
                }
            }

            return 0;
        }

        public static string[] GetSavedGames()
        {
            DirectoryInfo directory = new DirectoryInfo("SavedGames" + Path.DirectorySeparatorChar + _currentPlayer.Name);
            FileInfo[] files = directory.GetFiles();
            string[] savedGames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string filename = files[i].Name;
                savedGames[i] = filename.Substring(0, filename.IndexOf("."));
            }

            return savedGames;
        }

        public static void AddPlayer(LocalAgentController controller, bool isPlayerManaged = false)
        {
            if (controller.ControllingPlayer.IsNotNull())
            {
                Debug.LogError("A player called '" + controller.ControllingPlayer.gameObject.name + "' already exists for '" + controller.ToString() + "'.");
            }

            if (!Object.FindObjectOfType<RTSGameManager>())
            {
                Debug.LogError("A game manager has not been initialized!");
            }

            //load from ls db
            GameObject playerObject = Object.Instantiate(GameResourceManager.GetPlayerObject(), Object.FindObjectOfType<RTSGameManager>().GetComponentInChildren<LSPlayers>().transform);

            playerObject.name = controller.ControllerName;

            LSPlayer playerClone = playerObject.GetComponent<LSPlayer>();
            //change to user's selected username
            playerClone.Username = controller.ControllerName;
            playerClone.SetController(controller);

            if (isPlayerManaged)
            {
                GlobalAgentController.AddLocalController(controller);
                playerClone.IsCurrentPlayer = true;
                CurrentPlayerController = controller;
                SelectPlayer(playerClone.Username, 0, controller.ControllerID, controller.PlayerIndex);
            }

            controller.ControllingPlayer = playerClone;

            _allPlayers.Add(playerClone);
        }

        public static void InitializePlayers()
        {
            for(int i = 0; i < _allPlayers.Count; i++)
            {
                _allPlayers[i].OnInitialize();
            }
        }
        #endregion

        #region Private
        private static void SavePlayer(JsonWriter writer, PlayerDetails commander)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Name");
            writer.WriteValue(commander.Name);
            writer.WritePropertyName("Avatar");
            writer.WriteValue(commander.Avatar);

            writer.WriteEndObject();
        }

        private static void LoadPlayers(JsonTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    LoadPlayer(reader);
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    return;
                }
            }
        }

        private static void LoadPlayer(JsonTextReader reader)
        {
            string currValue = "", name = "";
            int avatar = 0;
            int controllerId = 0;
            int playerIndex = 0;
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        currValue = (string)reader.Value;
                    }
                    else
                    {
                        switch (currValue)
                        {
                            case "Name":
                                name = (string)reader.Value;
                                break;
                            case "Avatar":
                                avatar = (int)(long)reader.Value;
                                break;
                            case "ControllerId":
                                controllerId = (int)(long)reader.Value;
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    if (reader.TokenType == JsonToken.EndObject)
                    {
                        _savedPlayerProfiles.Add(new PlayerDetails(name, avatar, controllerId, playerIndex));
                        return;
                    }
                }
            }
        }
        #endregion
    }
}