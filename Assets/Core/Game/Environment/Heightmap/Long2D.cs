using RTSLockstep.Utility;

namespace RTSLockstep.Environment
{
    [System.Serializable]
    public class Long2D : Array2D<long>
    {
        public Long2D()
        {

        }
        public Long2D(int width, int height) : base(width, height)
        {

        }
    }
}