using RTSLockstep.Utility;

namespace RTSLockstep.Environment
{
    [System.Serializable]
    public class Short2D : Array2D<short>
    {
        public Short2D () {

        }
        public Short2D (int width, int height) :base (width,height) {

        }
    }
}