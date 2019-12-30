using UnityEngine;
using System;
using RTSLockstep.Abilities;

namespace RTSLockstep.Data
{
    [System.Serializable]
    public class UnitConfigElementDataItem : DataItem
    {
        [SerializeField, TypeReferences.ClassExtends(typeof(Ability))]

        private TypeReferences.ClassTypeReference _componentType;
        public Type ComponentType
        {
            get
            {
                return _componentType.Type;
            }
        }
        [SerializeField]
        private string _field;
        public string Field { get { return _field; } }
    }
}