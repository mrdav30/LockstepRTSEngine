using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class DeterminismAI : Ability
    {
        protected bool canAttack;
        protected LSBody cachedBody;
        protected Health cachedHealth;
        protected Attack cachedAttack;
        protected Move cachedMove;
        protected Turn cachedTurn;

        //we want to restrict how many decisions are made to help with game performance
        //the default time at the moment is a tenth of a second
        private float timeSinceLastDecision = 0.0f, timeBetweenDecisions = 0.1f;
        //convert to fast list...
        protected List<RTSAgent> nearbyObjects;
        protected RTSAgent nearbyAgent;

        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
            cachedBody = Agent.Body;
            cachedHealth = Agent.GetAbility<Health>();
            cachedAttack = Agent.GetAbility<Attack>();
            cachedMove = Agent.GetAbility<Move>();
            cachedTurn = Agent.GetAbility<Turn>();
        }

        protected override void OnVisualize()
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
            if (cachedAttack && cachedAttack.IsCasting)
            {
                return false;
            }
            else if (cachedMove && cachedMove.IsMoving)
            {
                return false;
            }

            //we are not doing anything at the moment
            if (timeSinceLastDecision > timeBetweenDecisions)
            {
                timeSinceLastDecision = 0.0f;
                return true;
            }
            else
            {
                timeSinceLastDecision += Time.deltaTime;
                return false;
            }
        }

        public virtual void CanAttack()
        {
            //default behaviour needs to be overidden by children
        }

        public virtual void DecideWhatToDo()
        {
            //determine what should be done by the agent at the current point in time
            //need sight from attack ability to be able to scan...
            if(cachedAttack)
               nearbyAgent = DoScan();

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

            RTSAgent agent = InfluenceManager.Scan(
                                this.cachedBody.Position,
                                this.cachedAttack.Sight,
                                agentConditional,
                                (bite) =>
                                {
                                    return ((this.Agent.Controller.GetAllegiance(bite) & this.cachedAttack.TargetAllegiance) != 0);
                                }
                            );

            return agent;
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