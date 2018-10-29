using Newtonsoft.Json;
using RTSLockstep;
using UnityEngine;

namespace RTSLockstep
{
    public class Convoy : Ability
    {
        #region Serialized Values (Further description in properties)
        #endregion

        protected override void OnInitialize()
        {
        }

        protected override void OnVisualize()
        {
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