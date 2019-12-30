using System;

namespace RTSLockstep.Utility
{
    public class PathObjectAttribute : UnityEngine.PropertyAttribute
    {
        public Type ObjectType { get; private set; }

        public PathObjectAttribute(Type requiredType)
        {
            ObjectType = requiredType;
            if (requiredType.IsSubclassOf(typeof(UnityEngine.Object)) == false)
            {
                throw new ArgumentException(string.Format("Type '{0}' is not a UnityEngine.Object.", requiredType));
            }
        }
    }
}