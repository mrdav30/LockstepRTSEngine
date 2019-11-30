using Newtonsoft.Json;
using RTSLockstep.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    [UnityEngine.DisallowMultipleComponent]
    public class Rally : ActiveAbility
    {
        public Vector3 spawnPoint { get; private set; }
        public Vector3 rallyPoint { get; private set; }
        private FlagState _flagState;

        private LSBody CachedBody { get { return Agent.Body; } }

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
            if (Agent.GetCommander() == PlayerManager.MainController.Commander)
            {
                RallyPoint flag = Agent.GetCommander().GetComponentInChildren<RallyPoint>();
                if (Agent.IsSelected)
                {
                    if (flag && spawnPoint != GameResourceManager.InvalidPosition.ToVector3() && rallyPoint != GameResourceManager.InvalidPosition.ToVector3())
                    {
                        if (_flagState == FlagState.FlagSet)
                        {
                            flag.transform.localPosition = rallyPoint;
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
            rallyPoint = position;
            if (Agent.GetCommander() && Agent.IsSelected)
            {
                RallyPoint flag = Agent.GetCommander().GetComponentInChildren<RallyPoint>();
                if (flag)
                {
                    if (!flag.ActiveStatus)
                    {
                        flag.Enable();
                    }
                    flag.transform.localPosition = rallyPoint;
                    _flagState = FlagState.FlagSet;
                    Agent.Controller.GetCommanderHUD().SetCursorLock(false);
                    Agent.Controller.GetCommanderHUD().SetCursorState(CursorState.Select);
                    SelectionManager.SetSelectionLock(false);
                }
            }
        }

        public void SetSpawnPoint()
        {
            int spawnX = (int)(Agent.Body.XMin.CeilToInt() + transform.forward.x * Agent.Body.XMax.CeilToInt() + transform.forward.x * 20);
            int spawnZ = (int)(Agent.Body.YMin.CeilToInt() + transform.forward.z * Agent.Body.YMax.CeilToInt() + transform.forward.z * 20);
            spawnPoint = new Vector3(spawnX, 0, spawnZ);
            rallyPoint = spawnPoint;
        }

        protected override void OnExecute(Command com)
        {
            Vector2d pos;
            if (com.TryGetData(out pos))
            {
                if (pos.ToVector3d() != GameResourceManager.InvalidPosition)
                {
                    SetRallyPoint(pos.ToVector3());
                }
            } 
        }

        public bool hasSpawnPoint()
        {
            return spawnPoint != GameResourceManager.InvalidPosition.ToVector3() && rallyPoint != GameResourceManager.InvalidPosition.ToVector3();
        }

        public FlagState GetFlagState()
        {
            return this._flagState;
        }

        public void SetFlagState(FlagState value)
        {
            this._flagState = value;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteVector(writer, "SpawnPoint", spawnPoint);
            SaveManager.WriteVector(writer, "RallyPoint", rallyPoint);
            SaveManager.WriteString(writer, "FlagState", _flagState.ToString());
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "SpawnPoint":
                    spawnPoint = LoadManager.LoadVector(reader);
                    break;
                case "RallyPoint":
                    rallyPoint = LoadManager.LoadVector(reader);
                    break;
                case "FlagState":
                    _flagState = WorkManager.GetFlagState((string)readValue);
                    break;
                default: break;
            }
        }
    }
}