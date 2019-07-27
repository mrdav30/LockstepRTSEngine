using Newtonsoft.Json;
using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class OffensiveAI : DeterminismAI
    {
        #region Serialized Values (Further description in properties)
        #endregion

        public override void OnInitialize()
        {
            base.OnInitialize();
            cachedAttack = cachedInfluencer.Agent.GetAbility<Attack>();
        }

        private bool CanAttack()
        {
            if (cachedAttack)
            {
                if (cachedAttack.IsAttackMoving || cachedHealth.HealthAmount == 0)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        public override void DecideWhatToDo()
        {
            //determine what should be done by the agent at the current point in time
            //need sight from attack ability to be able to scan...
            base.DecideWhatToDo();
            if (CanAttack() && (nearbyAgent != null || nearbyAgent != null && nearbyAgent == cachedAttack.Target))
            {
                InfluenceAttack();
            }
        }

        //TODO: Consolidate the checks in LSInfluencer
        protected override Func<RTSAgent, bool> AgentConditional
        {
            get
            {
                Func<RTSAgent, bool> agentConditional = null;

                if (cachedAttack.Damage >= 0)
                {
                    agentConditional = (other) =>
                    {
                        Health health = other.GetAbility<Health>();
                        return cachedInfluencer.Agent.GlobalID != other.GlobalID && health != null && health.CanLose && CachedAgentValid(other);
                    };
                }
                else
                {
                    agentConditional = (other) =>
                    {
                        Health health = other.GetAbility<Health>();
                        return cachedInfluencer.Agent.GlobalID != other.GlobalID && health != null && health.CanGain && CachedAgentValid(other);
                    };
                }

                return agentConditional;
            }
        }

        public virtual void InfluenceAttack()
        {
            // send attack command
            Command attackCom = new Command(AbilityDataItem.FindInterfacer("Attack").ListenInputID);
            attackCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
            attackCom.ControllerID = cachedInfluencer.Agent.Controller.ControllerID;

            attackCom.Add<Influence>(new Influence(cachedInfluencer.Agent));

            CommandManager.SendCommand(attackCom);
        }

        public override void OnSaveDetails(JsonWriter writer)
        {
            base.OnSaveDetails(writer);
        }

        public override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);

        }
    }
}