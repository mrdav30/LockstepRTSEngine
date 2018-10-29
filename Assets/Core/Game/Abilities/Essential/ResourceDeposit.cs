using Newtonsoft.Json;
using RTSLockstep;
using UnityEngine;

namespace RTSLockstep
{
    public class ResourceDeposit : Ability
    {
        public long AmountLeft { get; private set; }

        #region Serialized Values (Further description in properties)
        public long Capacity;
        public ResourceType ResourceType;
        [SerializeField]
        private GameObject resourceFull, resourceHalf, resourceQuarter, resourceEmpty;
        #endregion

        protected override void OnInitialize()
        {
            AmountLeft = Capacity;
        }

        protected override void OnVisualize()
        {
            float percentLeft = (float)AmountLeft / (float)Capacity;
            if (percentLeft <= .5f && percentLeft > .25f)
            {
                percentLeft = .5f;
                resourceFull.GetComponent<Renderer>().enabled = false;
            }
            else if (percentLeft <= .25f && percentLeft > 0)
            {
                percentLeft = .25f;
                resourceHalf.GetComponent<Renderer>().enabled = false;
            }
            else if (percentLeft <= 0)
            {
                percentLeft = 0;
                resourceQuarter.GetComponent<Renderer>().enabled = false;
            }
            Agent.Body.CalculateBounds();
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
            base.SaveDetails(writer);
            SaveManager.WriteFloat(writer, "AmountLeft", AmountLeft);
        }

        protected override void HandleLoadedProperty(JsonTextReader reader, string propertyName, object readValue)
        {
            base.HandleLoadedProperty(reader, propertyName, readValue);
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