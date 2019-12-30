using System;

namespace RTSLockstep.Data
{
    [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public sealed class RegisterDataAttribute : System.Attribute
    {
        public RegisterDataAttribute(string displayName)
        {
            _dataName = displayName;
        }

        private string _dataName;
        public string DataName { get { return _dataName; } }
    }
}