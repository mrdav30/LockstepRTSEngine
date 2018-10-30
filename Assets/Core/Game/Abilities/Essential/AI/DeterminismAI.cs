using Newtonsoft.Json;
using RTSLockstep;
using System.Collections.Generic;
using UnityEngine;

namespace RTSLockstep
{
    public class DeterminismAI : Ability
    {
        protected bool canAttack;
        protected Health cachedHealth;
        protected Attack cachedAttack;
        protected Move cachedMove;
        protected Turn cachedTurn;

        //we want to restrict how many decisions are made to help with game performance
        //the default time at the moment is a tenth of a second
        private float timeSinceLastDecision = 0.0f, timeBetweenDecisions = 0.1f;
        //convert to fast list...
        protected List<RTSAgent> nearbyObjects;

        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
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

        public virtual bool ShouldMakeDecision()
        {
            if (cachedAttack && !cachedAttack.IsCasting && cachedMove && !cachedMove.IsMoving)
            {
                //we are not doing anything at the moment
                if (timeSinceLastDecision > timeBetweenDecisions)
                {
                    timeSinceLastDecision = 0.0f;
                    return true;
                }
                timeSinceLastDecision += Time.deltaTime;
            }
            return false;
        }

        public virtual void CanAttack()
        {
            //default behaviour needs to be overidden by children
        }

        /*
         * A child class should only determine other conditions under which a decision should
         * not be made. This could be 'harvesting' for a harvester, for example. Alternatively,
         * an object that never has to make decisions could just return false.
        */

        protected void GetNearbyObject()
        {
            Vector3 currentPosition = transform.position;
            nearbyObjects = WorkManager.FindNearbyObjects(currentPosition, cachedAttack.Range);
        }

        public virtual void DecideWhatToDo()
        {
            //determine what should be done by the world object at the current point in time
            GetNearbyObject();

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