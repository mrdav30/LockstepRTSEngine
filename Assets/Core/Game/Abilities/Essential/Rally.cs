using Newtonsoft.Json;
using RTSLockstep.Managers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Managers.GameState;
using RTSLockstep.Player;
using RTSLockstep.Player.Commands;
using RTSLockstep.Player.Utility;
using RTSLockstep.LSResources;
using UnityEngine;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Abilities.Essential
{
    [DisallowMultipleComponent]
    public class Rally : ActiveAbility
    {
        public Vector3 SpawnPoint { get; private set; }
        public Vector3 RallyPoint { get; private set; }
        private FlagState _flagState;

        #region Serialized Values (Further description in properties)
        public Texture2D rallyPointImage;
        #endregion

        protected override void OnSetup()
        {
            Agent.OnSelectedChange += HandleSelectedChange;
            SetSpawnPoint();
        }

        public void HandleSelectedChange()
        {
            if (Agent.GetControllingPlayer() == PlayerManager.MainController.ControllingPlayer)
            {
                RallyPoint flag = Agent.GetControllingPlayer().GetComponentInChildren<RallyPoint>();
                if (Agent.IsSelected)
                {
                    if (flag && SpawnPoint != GameResourceManager.InvalidPosition.ToVector3() && RallyPoint != GameResourceManager.InvalidPosition.ToVector3())
                    {
                        if (_flagState == FlagState.FlagSet)
                        {
                            flag.transform.localPosition = RallyPoint;
                            flag.transform.forward = transform.forward;
                            flag.Enable();
                        }
                        else
                        {
                            flag.transform.localPosition = Agent.Body.Position3d.ToVector3();
                            flag.Disable();
                        }
                    }
                }
                else
                {
                    if (flag)
                    {
                        flag.Disable();
                    }
                }
            }
        }

        public void SetRallyPoint(Vector3 position)
        {
            RallyPoint = position;
            if (Agent.GetControllingPlayer() && Agent.IsSelected)
            {
                RallyPoint flag = Agent.GetControllingPlayer().GetComponentInChildren<RallyPoint>();
                if (flag)
                {
                    if (!flag.ActiveStatus)
                    {
                        flag.Enable();
                    }
                    flag.transform.localPosition = RallyPoint;
                    _flagState = FlagState.FlagSet;
                    Agent.Controller.GetPlayerHUD().SetCursorLock(false);
                    Agent.Controller.GetPlayerHUD().SetCursorState(CursorState.Select);
                    SelectionManager.SetSelectionLock(false);
                }
            }
        }

        public void SetSpawnPoint()
        {
            int spawnX = (int)(Agent.Body.XMin.CeilToInt() + transform.forward.x * Agent.Body.XMax.CeilToInt() + transform.forward.x * 20);
            int spawnZ = (int)(Agent.Body.YMin.CeilToInt() + transform.forward.z * Agent.Body.YMax.CeilToInt() + transform.forward.z * 20);
            SpawnPoint = new Vector3(spawnX, 0, spawnZ);
            RallyPoint = SpawnPoint;
        }

        protected override void OnExecute(Command com)
        {
            if (com.TryGetData(out Vector2d pos))
            {
                if (pos.ToVector3d() != GameResourceManager.InvalidPosition)
                {
                    SetRallyPoint(pos.ToVector3());
                }
            } 
        }

        public bool hasSpawnPoint()
        {
            return SpawnPoint != GameResourceManager.InvalidPosition.ToVector3() && RallyPoint != GameResourceManager.InvalidPosition.ToVector3();
        }

        public FlagState GetFlagState()
        {
            return _flagState;
        }

        public void SetFlagState(FlagState value)
        {
            _flagState = value;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            SaveDetails(writer);
            SaveManager.WriteVector(writer, "SpawnPoint", SpawnPoint);
            SaveManager.WriteVector(writer, "RallyPoint", RallyPoint);
            SaveManager.WriteString(writer, "FlagState", _flagState.ToString());
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "SpawnPoint":
                    SpawnPoint = LoadManager.LoadVector(reader);
                    break;
                case "RallyPoint":
                    RallyPoint = LoadManager.LoadVector(reader);
                    break;
                case "FlagState":
                    _flagState = WorkManager.GetFlagState((string)readValue);
                    break;
                default: break;
            }
        }
    }
}