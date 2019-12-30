namespace RTSLockstep.Utility
{
    public abstract class BaseMessageStop
    {
        public BaseMessageStop()
        {

        }

        public abstract BaseMessageChannel GetChannel(string channelID);
        public abstract void Clear();
    }
}