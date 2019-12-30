namespace RTSLockstep.Data
{
	public interface IUnitConfigDataItem : INamedData
	{
		string Target { get; }
		Stat [] Stats { get; }
	}
}