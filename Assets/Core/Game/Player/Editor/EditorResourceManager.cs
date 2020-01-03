using UnityEditor;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.LSResources;
using System;

namespace RTSLockstep
{
    [CustomEditor(typeof(PlayerResourceManager))]
    public class EditorPlayerResourceManager : Editor
    {
        private PlayerResourceManager targetValue;

        private void OnEnable()
        {
            targetValue = (PlayerResourceManager)target;

            EnvironmentResourceType[] values = (EnvironmentResourceType[])Enum.GetValues(typeof(EnvironmentResourceType));
            if (targetValue.CurrentResources.IsNull() || targetValue.CurrentResources.Count != values.Length)
            {
                foreach (var type in values)
                {
                    targetValue.CurrentResources.Add(type, null);
                }
            }
        }
    }
}
