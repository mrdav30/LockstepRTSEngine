using RTSLockstep.Integration;
using UnityEngine;

namespace RTSLockstep.Data
{
    [System.Serializable]
    public class UnitConfigDataItem : DataItem, IUnitConfigDataItem
    {
        [SerializeField, DataCode("Agents")]
        private string _target;
        public string Target
        {
            get
            {
                return _target;
            }
        }

        [SerializeField]
        private Stat[] _stats;
        public Stat[] Stats
        {
            get
            {
                return _stats;
            }
        }

        protected override void OnManage()
        {
            _name = Target;
        }

    }

}