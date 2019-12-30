using System;

namespace RTSLockstep.Determinism
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class LockstepAttribute : Attribute
    {
        public bool DoReset { get; private set; }
        public LockstepAttribute()
        {
            DoReset = false;
        }

        public LockstepAttribute(bool doReset)
        {
            DoReset = doReset;
        }
    }
}