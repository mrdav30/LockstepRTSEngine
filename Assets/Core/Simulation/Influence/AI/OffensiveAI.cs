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
        }

        private bool CanAttack()
        {
            if (cachedAgent.MyStats.CachedAttack)
            {
                if (cachedAgent.MyStats.CachedAttack.IsAttackMoving || cachedAgent.MyStats.CachedHealth.CurrentHealth == 0)
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
            if (CanAttack() && (nearbyAgent.IsNotNull() || nearbyAgent.IsNotNull() && nearbyAgent == cachedAgent.MyStats.CachedAttack.CurrentTarget))
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

                if (cachedAgent.MyStats.CachedAttack.Damage >= 0)
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

        protected override Func<byte, bool> AllianceConditional
        {
            get
            {
                Func<byte, bool> allianceConditional = (bite) =>
                {
                    return ((cachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Enemy) != 0);
                };
                return allianceConditional;
            }
        }

        public virtual void InfluenceAttack()
        {
            // send attack command
            Command attackCom = new Command(AbilityDataItem.FindInterfacer("Attack").ListenInputID);
            attackCom.Add(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
            attackCom.ControllerID = cachedAgent.Controller.ControllerID;

            attackCom.Add(new Influence(cachedAgent));

            CommandManager.SendCommand(attackCom);
        }
    }
}