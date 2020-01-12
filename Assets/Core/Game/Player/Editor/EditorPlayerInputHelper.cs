using UnityEditor;
using UnityEngine;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.Managers.GameManagers;

namespace RTSLockstep
{
    [CustomEditor(typeof(InputHelper), true)]
    public class EditorPlayerInputHelper : Editor
    {
        private InputHelper targetValue;

        protected virtual void OnEnable()
        {
            targetValue = (InputHelper)target;

            if (targetValue.UserInputKeys.IsNull() || targetValue.UserInputKeys.Count != GameResourceManager.GameInputKeys.Length)
            {
                targetValue.UserInputKeys = new PlayerInputKeys();
                foreach (var type in GameResourceManager.GameInputKeys)
                {
                    targetValue.UserInputKeys.Add(type, KeyCode.None);
                }
            }
        }
    }
}
