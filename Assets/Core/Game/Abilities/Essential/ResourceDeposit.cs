using Newtonsoft.Json;
using RTSLockstep.Simulation.Grid;
using RTSLockstep.Managers.GameState;
using RTSLockstep.LSResources;
using UnityEngine;

namespace RTSLockstep.Abilities.Essential
{
    public class ResourceDeposit : Ability
    {
        public long AmountLeft { get; private set; }
        private DynamicBlocker cachedBlocker;

        #region Serialized Values (Further description in properties)
        public long Capacity;
        public RawMaterialType ResourceType;
        [SerializeField]
        private GameObject resourceFull, resourceHalf, resourceQuarter, resourceEmpty;
        #endregion

        protected override void OnInitialize()
        {
            AmountLeft = Capacity;
        }

        protected override void OnVisualize()
        {
            float percentLeft = AmountLeft / (float)Capacity;
            if (percentLeft <= .5f && percentLeft > .25f)
            {
                resourceFull.GetComponent<Renderer>().enabled = false;
                if (resourceHalf.GetComponent<Renderer>().enabled == false)
                {
                    resourceHalf.GetComponent<Renderer>().enabled = true;
                }
            }
            else if (percentLeft <= .25f && percentLeft > 0)
            {
                resourceHalf.GetComponent<Renderer>().enabled = false;
                if (resourceQuarter.GetComponent<Renderer>().enabled == false)
                {
                    resourceQuarter.GetComponent<Renderer>().enabled = true;
                }
            }
            else if (percentLeft <= 0)
            {
                resourceQuarter.GetComponent<Renderer>().enabled = false;
                if (resourceEmpty.GetComponent<Renderer>().enabled == false)
                {
                    resourceEmpty.GetComponent<Renderer>().enabled = true;
                }

                // Resource is empty, delete object
                Agent.Die();
                return;
            }
        }

        public void Remove(long amount)
        {

            AmountLeft -= amount;
            if (AmountLeft < 0)
            {
                AmountLeft = 0;
            }
        }

        public bool IsEmpty()
        {
            return AmountLeft <= 0;
        }

        protected override void OnSaveDetails(JsonWriter writer)
        {
            SaveDetails(writer);
            SaveManager.WriteFloat(writer, "AmountLeft", AmountLeft);
        }

        protected override void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.OnLoadProperty(reader, propertyName, readValue);
            switch (propertyName)
            {
                case "AmountLeft":
                    AmountLeft = (long)readValue;
                    break;
                default:
                    break;
            }
        }
    }
}