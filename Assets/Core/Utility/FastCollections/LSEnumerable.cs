namespace RTSLockstep.Utility.FastCollections
{
    public interface FastEnumerable<T>
    {
        void Enumerate(FastList<T> output);
    }
}