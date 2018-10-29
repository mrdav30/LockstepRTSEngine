using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep;

namespace RTSLockstep.Data
{
	public interface IUnitConfigDataProvider
	{
		IUnitConfigDataItem [] UnitConfigData { get; }
		UnitConfigElementDataItem [] UnitConfigElementData { get; }
	}
}