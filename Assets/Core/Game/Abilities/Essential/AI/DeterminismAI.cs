using Newtonsoft.Json;
using RTSLockstep.Data;
using System;
using System.Collections.Generic;

namespace RTSLockstep
{
    public class DeterminismAI : Ability
    {
        public Func<RTSAgent, bool> CachedAgentValid { get; private set; }

        protected bool canAttack;
        protected LSBody cachedBody;
        protected Health cachedHealth;
        protected Attack cachedAttack;
        protected Move cachedMove;
        protected Turn cachedTurn;

        // default scan range, overriden by cached attack sight
        protected long scanRange = 5;
        // we want to restrict how many decisions are made to help with game performance
        protected const int SearchRate = LockstepManager.FrameRate / 2;
        protected int searchCount;

        // convert to fast list...
        protected List<RTSAgent> nearbyObjects;
        protected RTSAgent nearbyAgent;

        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnSetup()
        {
            base.OnSetup();
            CachedAgentValid = this.AgentValid;
        }

        protected override void OnInitialize()
        {
            cachedBody = Agent.Body;
            cachedHealth = Agent.GetAbility<Health>();
            cachedAttack = Agent.GetAbility<Attack>();
            cachedMove = Agent.GetAbility<Move>();
            cachedTurn = Agent.GetAbility<Turn>();

            searchCount = LSUtility.GetRandom(SearchRate) + 1;

            if (cachedAttack)
            {
                scanRange = cachedAttack.Sight;
            }
        }

        protected override void OnVisualize()
        {
            if (!ReplayManager.IsPlayingBack && ShouldMakeDecision())
            {
                DecideWhatToDo();
            }
        }

        /*
         * A child class should only determine other conditions under which a decision should
         * not be made. This could be 'harvesting' for a harvester, for example. Alternatively,
         * an object that never has to make decisions could just return false...or not have this ability
        */
        public virtual bool ShouldMakeDecision()
        {
            if (cachedAttack && (cachedAttack.IsCasting))
            {
                return false;
            }
            else if (cachedMove && cachedMove.IsMoving)
            {
                searchCount -= 8;
                return false;
            }
            else
            {
                searchCount -= 2;
            }

            if (searchCount <= 0)
            {
                searchCount = SearchRate;
                //we are not doing anything at the moment
                return true;
            }
            else
            {
                searchCount -= 1;
                return false;
            }
        }

        public virtual bool CanAttack()
        {
            //default behaviour needs to be overidden by children
            return false;
        }

        public virtual void DecideWhatToDo()
        {
            //determine what should be done by the agent at the current point in time
            //need sight from attack ability to be able to scan...
            nearbyAgent = DoScan();
        }

        protected virtual Func<RTSAgent, bool> AgentConditional
        {
            get
            {
                Func<RTSAgent, bool> agentConditional = null;
                return agentConditional;
            }
        }

        protected virtual RTSAgent DoScan()
        {
            Func<RTSAgent, bool> agentConditional = AgentConditional;

            RTSAgent agent = null;

            if (agentConditional.IsNotNull())
            {
                agent = InfluenceManager.Scan(
                     this.cachedBody.Position,
                     scanRange,
                     agentConditional,
                     (bite) =>
                     {
                         return ((this.Agent.Controller.GetAllegiance(bite) & this.cachedAttack.TargetAllegiance) != 0);
                     }
                 );
            }

            return agent;
        }

        public virtual void InfluenceAttack()
        {
            if (CanAttack() && (nearbyAgent != null || nearbyAgent != null && nearbyAgent == cachedAttack.Target))
            {
                // send attack command
                Command attackCom = new Command(AbilityDataItem.FindInterfacer("Attack").ListenInputID);
                attackCom.Add<DefaultData>(new DefaultData(DataType.UShort, nearbyAgent.GlobalID));
                attackCom.ControllerID = Agent.Controller.ControllerID;

                attackCom.Add<Influence>(new Influence(Agent));

                CommandManager.SendCommand(attackCom);
            }
        }

        protected virtual bool AgentValid(RTSAgent agent)
        {
            return true;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            base.SaveDetails(writer);
            SaveManager.WriteInt(writer, "SearchCount", searchCount);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "SearchCount":
                    searchCount = (int)readValue;
                    break;
            }
        }
    }
}