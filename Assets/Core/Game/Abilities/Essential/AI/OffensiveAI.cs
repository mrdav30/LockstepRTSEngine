using Newtonsoft.Json;
using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class OffensiveAI : DeterminismAI
    {
        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
            base.OnInitialize();
            cachedAttack = Agent.GetAbility<Attack>();
        }

        public override void CanAttack()
        {
            if (!cachedAttack.IsAttackMoving)
            {
                canAttack = false;
            }
            canAttack = true;
        }

        public override void DecideWhatToDo()
        {
            base.DecideWhatToDo();
            if (nearbyAgent != null || nearbyAgent != null && nearbyAgent == cachedAttack.Target)
            {
                // send attack command
                Command attackCom = new Command(AbilityDataItem.FindInterfacer("Attack").ListenInputID);
                attackCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
                Agent.Execute(attackCom);
            }

            //if (CanAttack())
            //{
            //    List<WorldObject> enemyObjects = new List<WorldObject>();
            //    foreach (WorldObject nearbyObject in nearbyObjects)
            //    {
            //        Resource resource = nearbyObject.GetComponent<Resource>();
            //        if (resource)
            //        {
            //            continue;
            //        }
            //        if (nearbyObject.GetPlayer() != player)
            //        {
            //            enemyObjects.Add(nearbyObject);
            //        }
            //    }
            //    WorldObject closestObject = WorkManager.FindNearestWorldObjectInListToPosition(enemyObjects, currentPosition);
            //    if (closestObject)
            //    {
            //        BeginAttack(closestObject);
            //    }
            //}
        }

        //TODO: Consolidate the checks in LSInfluencer
        protected override Func<RTSAgent, bool> AgentConditional
        {
            get
            {
                Func<RTSAgent, bool> agentConditional = null;

                if (Agent.Tag == AgentTag.Infantry)
                {
                    if (cachedAttack.Damage >= 0)
                    {
                        agentConditional = (other) =>
                        {
                            Health health = other.GetAbility<Health>();
                            return Agent.GlobalID != other.GlobalID && health != null && health.CanLose && CachedAgentValid(other);
                        };
                    }
                    else
                    {
                        agentConditional = (other) =>
                        {
                            Health health = other.GetAbility<Health>();
                            return Agent.GlobalID != other.GlobalID && health != null && health.CanGain && CachedAgentValid(other);
                        };
                    }
                }

                return agentConditional;
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