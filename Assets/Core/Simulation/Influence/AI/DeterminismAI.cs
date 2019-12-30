using Newtonsoft.Json;
using RTSLockstep.Agents;
using RTSLockstep.Managers;
using RTSLockstep.Managers.GameState;
using RTSLockstep.Utility;
using System;

namespace RTSLockstep.Simulation.Influence
{
    public class DeterminismAI
    {
        protected LSAgent CachedAgent { get; private set; }
        protected Func<LSAgent, bool> CachedAgentValid { get; private set; }

        // we want to restrict how many decisions are made to help with game performance
        protected const int SearchRate = LockstepManager.FrameRate / 2;
        protected int searchCount;

        // convert to fast list...
        protected LSAgent nearbyAgent;

        #region Serialized Values (Further description in properties)
        #endregion

        public virtual void OnSetup(LSAgent agent)
        {
            CachedAgent = agent;

            CachedAgentValid = AgentValid;
        }

        public virtual void OnInitialize()
        {
            searchCount = LSUtility.GetRandom(SearchRate) + 1;
        }

        public virtual void OnSimulate()
        {
            if (ShouldMakeDecision())
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
            if (CachedAgent.IsCasting)
            {
                searchCount = -1;
                return false;
            }
            else if (CachedAgent.MyStats.CachedMove && CachedAgent.MyStats.CachedMove.IsMoving)
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

        public virtual void DecideWhatToDo()
        {
            //determine what should be done by the agent at the current point in time
            nearbyAgent = DoScan();
        }

        protected virtual Func<LSAgent, bool> AgentConditional
        {
            get
            {
                Func<LSAgent, bool> agentConditional = null;
                return agentConditional;
            }
        }

        protected virtual Func<byte, bool> AllianceConditional
        {
            get
            {
                Func<byte, bool> allianceConditional = null;
                return allianceConditional;
            }
        }

        protected virtual LSAgent DoScan()
        {
            Func<LSAgent, bool> agentConditional = AgentConditional;
            Func<byte, bool> allianceConditional = AllianceConditional;

            LSAgent agent = null;

            if (agentConditional.IsNotNull())
            {
                agent = AgentLOSManager.Scan(
                     CachedAgent.Body.Position,
                     CachedAgent.MyStats.Sight,
                     agentConditional,
                     allianceConditional
                 );
            }

            return agent;
        }

        protected virtual void ResetAwareness()
        {
            nearbyAgent = null;
        }

        protected virtual bool AgentValid(LSAgent agent)
        {
            return true;
        }

        public virtual void OnSaveDetails(JsonWriter writer)
        {
            SaveManager.WriteInt(writer, "SearchCount", searchCount);
        }

        public virtual void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            switch (propertyName)
            {
                case "SearchCount":
                    searchCount = (int)readValue;
                    break;
            }
        }
    }
}