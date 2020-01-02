using UnityEngine;
using RTSLockstep.LSResources;
using RTSLockstep.Integration;

namespace RTSLockstep.Data
{
    [System.Serializable]
    public class AgentControllerDataItem : DataItem
    {
        [SerializeField]
        protected AllegianceType _defaultAllegiance;
        public AllegianceType DefaultAllegiance { get { return _defaultAllegiance; } }

        [SerializeField, DataCode("Agents")]
        private string _commanderCode;
        public string CommanderCode { get { return _commanderCode; } }

        public AgentControllerDataItem(string name)
        {
            _name = name;
        }
    }
}
