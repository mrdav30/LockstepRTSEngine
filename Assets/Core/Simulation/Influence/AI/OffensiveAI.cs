using Newtonsoft.Json;
using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class OffensiveAI : DeterminismAI
    {
        protected Attack cachedAttack;

        #region Serialized Values (Further description in properties)
        #endregion

        public override void OnInitialize()
        {
            base.OnInitialize();
            _targetAllegiance = AllegianceType.Enemy;

            cachedAttack = cachedAgent.GetAbility<Attack>();

            if (cachedAttack)
            {
                scanRange = cachedAttack.Sight;
            }
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



        public override bool ShouldMakeDecision()
        {
            if (cachedAgent.IsCasting)
            {
                searchCount -= 1;
                return false;
            }

            return base.ShouldMakeDecision();
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
                        return cachedAgent.GlobalID != other.GlobalID && health != null && health.CanLose && CachedAgentValid(other);
                    };
                }
                else
                {
                    agentConditional = (other) =>
                    {
                        Health health = other.GetAbility<Health>();
                        return cachedAgent.GlobalID != other.GlobalID && health != null && health.CanGain && CachedAgentValid(other);
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
            attackCom.ControllerID = cachedAgent.Controller.ControllerID;

            attackCom.Add<Influence>(new Influence(cachedAgent));

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