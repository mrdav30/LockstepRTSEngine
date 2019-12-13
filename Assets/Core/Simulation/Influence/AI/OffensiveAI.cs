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

        public override bool ShouldMakeDecision()
        {
            if (cachedAgent.Tag == AgentTag.Offensive)
            {
                if (searchCount <= 0)
                {
                    if ((!cachedAgent.MyStats.CachedAttack.IsFocused && !cachedAgent.MyStats.CachedAttack.IsAttackMoving)
                        && cachedAgent.MyStats.CachedHealth.CurrentHealth > 0)
                    {
                        // We're ready to go but have no target
                        searchCount = SearchRate;
                        return true;
                    }
                }

                if (cachedAgent.MyStats.CachedAttack.IsFocused || cachedAgent.MyStats.CachedHealth.CurrentHealth == 0)
                {
                    // busy attacking or being dead
                    searchCount -= 1;
                    return false;
                }
            }

            // We still have the potential to be offensive
            return base.ShouldMakeDecision();
        }


        public override void DecideWhatToDo()
        {
            //determine what should be done by the agent at the current point in time
            //need sight from attack ability to be able to scan...
            base.DecideWhatToDo();
            InfluenceAttack();
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
                        return other.GlobalID != cachedAgent.GlobalID
                                && other.IsActive
                                && health.IsNotNull() 
                                && health.CanLose 
                                && CachedAgentValid(other);
                    };
                }
                else
                {
                    agentConditional = (other) =>
                    {
                        Health health = other.GetAbility<Health>();
                        return other.GlobalID != cachedAgent.GlobalID
                                && CachedAgentValid(other)
                                && health.IsNotNull() 
                                && health.CanGain ;
                    };
                }

                return agentConditional;
            }
        }

        protected override Func<byte, bool> AllianceConditional
        {
            get
            {
                bool allianceConditional(byte bite)
                {
                    return ((cachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Enemy) != 0);
                }
                return allianceConditional;
            }
        }

        public virtual void InfluenceAttack()
        {
            if (nearbyAgent.IsNotNull())
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
}