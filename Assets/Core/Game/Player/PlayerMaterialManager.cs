using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

using RTSLockstep.Agents;
using RTSLockstep.LSResources;
using RTSLockstep.Utility;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.RawMaterials;

namespace RTSLockstep.Player
{
    public class PlayerMaterialManager : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        public RawMaterialSetLimit BaseRawMaterials;

        [HideInInspector]
        public RawMaterialSetLimit CurrentRawMaterials;
        #endregion

        #region Behavior
        // Use this for initialization
        public void OnSetup()
        {
            CurrentRawMaterials = BaseRawMaterials;
        }
        #endregion

        #region Public
        public void AddRawMaterial(RawMaterialType type, long amount)
        {
            CurrentRawMaterials[type].CurrentValue += amount;
        }

        public void IncreaseRawMaterialLimit(RawMaterialType type, long amount)
        {
            CurrentRawMaterials[type].CurrentLimit += amount;
        }

        public RawMaterialSetLimit GetCurrentRawMaterials()
        {
            return CurrentRawMaterials;
        }

        public long GetRawMaterialAmount(RawMaterialType type)
        {
            return CurrentRawMaterials[type].CurrentValue;
        }

        public long GetRawMaterialLimit(RawMaterialType type)
        {
            return CurrentRawMaterials[type].CurrentLimit;
        }

        public void RemoveRawMaterial(RawMaterialType type, int amount)
        {
            CurrentRawMaterials[type].CurrentValue -= amount;
        }

        public void DecrementRawMaterialLimit(RawMaterialType type, int amount)
        {
            CurrentRawMaterials[type].CurrentLimit -= amount;
        }

        public bool CheckPlayersRawMaterials(LSAgent agent)
        {
            bool validResources = true;
            foreach (KeyValuePair<RawMaterialType, int> entry in agent.RawMaterialCost)
            {
                if (entry.Value > 0)
                {
                    switch (entry.Key.ToString())
                    {
                        case "Provision":
                            if ((entry.Value + GetRawMaterialAmount(entry.Key)) >= GetRawMaterialLimit(entry.Key))
                            {
                                validResources = false;
                                Debug.Log("not enough supplies!");
                            }
                            break;
                        default:
                            if (entry.Value > GetRawMaterialAmount(entry.Key))
                            {
                                validResources = false;
                                Debug.Log("not enough resources!");
                            }
                            break;
                    };
                };
            }

            return validResources;
        }

        public void RemovePlayersRawMaterials(LSAgent agent)
        {
            foreach (KeyValuePair<RawMaterialType, int> entry in agent.RawMaterialCost)
            {
                if (entry.Value > 0)
                {
                    switch (entry.Key.ToString())
                    {
                        case "Provision":
                            AddRawMaterial(entry.Key, entry.Value);
                            break;
                        default:
                            RemoveRawMaterial(entry.Key, entry.Value);
                            break;
                    };
                };
            }
        }

        public void LoadPlayersRawMaterials(JsonTextReader reader)
        {
            if (reader.IsNull())
            {
                return;
            }

            string currValue = "";
            while (reader.Read())
            {
                if (reader.Value.IsNotNull())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        currValue = (string)reader.Value;
                    }
                    else
                    {
                        foreach (var type in GameResourceManager.GameRawMaterials)
                        {
                            if(currValue == type.ToString())
                            {
                                CurrentRawMaterials[type].CurrentValue = (int)(long)reader.Value;
                            }
                            else if ((currValue + "_Limit") == type.ToString())
                            {
                                CurrentRawMaterials[type].CurrentLimit = (int)(long)reader.Value;
                            }
                        }                        
                    }
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    return;
                }
            }
        }
        #endregion
    }
}