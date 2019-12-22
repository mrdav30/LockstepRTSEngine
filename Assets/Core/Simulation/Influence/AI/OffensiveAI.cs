using Newtonsoft.Json;
using RTSLockstep.Data;
using System;

namespace RTSLockstep
{
    public class OffensiveAI : DeterminismAI
    {
        public override void OnInitialize()
        {
            base.OnInitialize();
        }

        public override bool ShouldMakeDecision()
        {
            if (CachedAgent.Tag == AgentTag.Offensive)
            {
                if (searchCount <= 0)
                {
                    searchCount = SearchRate;
                    if ((!CachedAgent.MyStats.CachedAttack.IsFocused && !CachedAgent.MyStats.CachedAttack.IsAttackMoving)
                        && CachedAgent.MyStats.CachedHealth.CurrentHealth > 0)
                    {
                        // We're ready to go but have no target
                        return true;
                    }
                }

                if (CachedAgent.MyStats.CachedAttack.IsFocused || CachedAgent.MyStats.CachedAttack.IsAttackMoving || CachedAgent.MyStats.CachedHealth.CurrentHealth <= 0)
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

                if (CachedAgent.MyStats.CachedAttack.Damage >= 0)
                {
                    agentConditional = (other) =>
                    {
                        Health targetHealth = other.GetAbility<Health>();
                        return other.GlobalID != CachedAgent.GlobalID
                                && other.IsActive
                                && targetHealth.IsNotNull()
                                && targetHealth.CanLose
                                && CachedAgentValid(other);
                    };
                }
                else
                {
                    agentConditional = (other) =>
                    {
                        Health targetHealth = other.GetAbility<Health>();
                        return other.GlobalID != CachedAgent.GlobalID
                                && CachedAgentValid(other)
                                && targetHealth.IsNotNull()
                                && targetHealth.CanGain;
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
                    return ((CachedAgent.Controller.GetAllegiance(bite) & AllegianceType.Enemy) != 0);
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
                attackCom.ControllerID = CachedAgent.Controller.ControllerID;

                attackCom.Add(new Influence(CachedAgent));

                CommandManager.SendCommand(attackCom);

                base.ResetAwareness();
            }
        }
    }
}