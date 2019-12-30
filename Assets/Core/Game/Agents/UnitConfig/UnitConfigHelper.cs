using UnityEngine;
using RTSLockstep.Data;
using System.Collections.Generic;
using System;
using System.Reflection;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Agents.UnitConfig
{
    public class UnitConfigHelper : BehaviourHelper
    {
        private static bool setted = false;
        private UnitConfigElementDataItem[] ConfigElementData;
        private IUnitConfigDataItem[] ConfigData;
        private Dictionary<string, UnitConfigElementDataItem> ConfigElementMap;

        protected override void OnInitialize()
        {
            if (!setted)
            {
                SetupConfigs();
                setted = true;
            }
        }

        private void SetupConfigs()
        {
            //todo guard
            if (LSDatabaseManager.TryGetDatabase(out IUnitConfigDataProvider database))
            {
                ConfigElementData = database.UnitConfigElementData;
                ConfigElementMap = new Dictionary<string, UnitConfigElementDataItem>();
                for (int i = 0; i < ConfigElementData.Length; i++)
                {
                    var item = ConfigElementData[i];
                    ConfigElementMap.Add(item.Name, item);
                }
                ConfigData = database.UnitConfigData;
                for (int i = 0; i < ConfigData.Length; i++)
                {
                    IUnitConfigDataItem item = ConfigData[i];
                    LSAgent agent = GameResourceManager.GetAgentTemplate(item.Target);
                    for (int j = 0; j < item.Stats.Length; j++)
                    {
                        Stat stat = item.Stats[j];
                        //todo guard
                        var element = ConfigElementMap[stat.ConfigElement];
                        Component component = agent.GetComponent(element.ComponentType);
                        SetField(component, element.Field, stat.Value);
                    }
                }
            }
        }

        private void SetField(object obj, string fieldName, long value)
        {
            Type objType = obj.GetType();

            FieldInfo fieldInfo = objType.GetField(fieldName, (BindingFlags)~0);

            if (fieldInfo.FieldType == typeof(long))
            {
                fieldInfo.SetValue(obj, value);
            }
            else if (fieldInfo.FieldType == typeof(int))
            {
                fieldInfo.SetValue(obj, FixedMath.RoundToInt(value));
            }
            else
            {
                Debug.Log(string.Format("Field '{0}' of type '{1}' is not valid", fieldName, objType));
            }
        }
    }
}