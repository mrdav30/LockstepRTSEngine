using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep;

namespace RTSLockstep.Data
{
	public interface IUnitConfigDataItem : INamedData
	{
		string Target { get; }
		Stat [] Stats { get; }
	}
}