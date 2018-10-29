using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep.Data;
using RTSLockstep;
using System;
namespace RTSLockstep.Data
{
	[System.Serializable]
	public class UnitConfigElementDataItem : RTSLockstep.Data.DataItem
	{
		[SerializeField, TypeReferences.ClassExtends (typeof (Ability))]
		
		private TypeReferences.ClassTypeReference _componentType;
		public Type ComponentType {
			get {
				return _componentType.Type;
			}
		}
		[SerializeField]
		private string _field;
		public string Field { get { return _field; }}
	}
}