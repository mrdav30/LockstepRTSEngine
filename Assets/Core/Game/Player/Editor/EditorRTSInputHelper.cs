using UnityEditor;
using UnityEngine;

using RTSLockstep.Utility;
using RTSLockstep.Player;
using RTSLockstep.Managers.GameManagers;

namespace RTSLockstep
{
    [CustomEditor(typeof(EditorRTSInputHelper), true)]
    public class EditorRTSInputHelper : EditorPlayerInputHelper
    {
        protected override void OnEnable()
        {
            base.OnEnable();
        }
    }
}
