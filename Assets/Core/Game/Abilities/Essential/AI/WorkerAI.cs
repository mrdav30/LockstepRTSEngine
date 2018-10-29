using RTSLockstep.Data;
using Newtonsoft.Json;
using RTSLockstep;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class WorkerAI: DeterminismAI
    {
        protected Construct cachedBuild;
        protected Harvest cachedHarvest;

        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
            base.OnInitialize();
            cachedBuild = Agent.GetAbility<Construct>();
            cachedHarvest = Agent.GetAbility<Harvest>();
        }

        protected override void OnVisualize()
        {
          
        }

        public override void CanAttack()
        {
            if (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying || cachedBuild.IsBuilding)
            {
                cachedAttack.CanAttack = false;
            }
            cachedAttack.CanAttack = true;
        }

        public override bool ShouldMakeDecision()
        {
            if (Agent.Tag == AgentTag.Harvester && (cachedHarvest.IsHarvesting || cachedHarvest.IsEmptying))
            {
                return false;
            }
            else if (Agent.Tag == AgentTag.Builder && cachedBuild.IsBuilding)
            {
                return false;
            }
            return base.ShouldMakeDecision();
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();
            if (Agent.Tag == AgentTag.Harvester && cachedHarvest.IsFocused)
            {
                //convert to fast list...
                List<RTSAgent> resources = new List<RTSAgent>();
                foreach (RTSAgent nearbyObject in nearbyObjects)
                {
                    ResourceDeposit resource = nearbyObject.GetAbility<ResourceDeposit>();
                    if (resource && !resource.IsEmpty())
                    {
                        resources.Add(nearbyObject);
                    }
                }
                RTSAgent nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(resources, transform.position);
                if (nearestObject)
                {
                    ResourceDeposit closestResource = nearestObject.GetAbility<ResourceDeposit>();
                    // only harvest resources the worker is assigned to
                    if (closestResource && closestResource.ResourceType == cachedHarvest.HarvestType)
                    {
                        // send harvest command
                        Command harvestCom = new Command(AbilityDataItem.FindInterfacer("Harvest").ListenInputID);
                        harvestCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearestObject.GlobalID));
                        UserInputHelper.SendCommand(harvestCom);
                    }
                }
            }
            if (Agent.Tag == AgentTag.Builder && cachedBuild.IsFocused)
            {
                //convert to fast array
                List<RTSAgent> buildings = new List<RTSAgent>();
                foreach (RTSAgent nearbyObject in nearbyObjects)
                {
                    if (nearbyObject.GetCommander() != Agent.Controller.Commander)
                    {
                        continue;
                    }
                    RTSAgent nearbyBuilding = nearbyObject.GetComponent<RTSAgent>();
                    if (nearbyBuilding && nearbyBuilding.GetAbility<Structure>().UnderConstruction())
                    {
                        buildings.Add(nearbyObject);
                    }
                }
                RTSAgent nearestObject = WorkManager.FindNearestWorldObjectInListToPosition(buildings, transform.position);
                if (nearestObject)
                {
                    RTSAgent closestBuilding = nearestObject.GetComponent<RTSAgent>();
                    if (closestBuilding)
                    {
                        // send build command
                        Command buildCom = new Command(AbilityDataItem.FindInterfacer("Construct").ListenInputID);
                        buildCom.Add<DefaultData>(new DefaultData(DataType.UShort, closestBuilding.GlobalID));
                        UserInputHelper.SendCommand(buildCom);
                    }
                }
                else
                {
                    cachedBuild.SetCurrentProject(null);
                }
            }
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);

        }
    }
}