using UnityEngine;

namespace RTSLockstep.Environment
{
    public class EnvironmentObject : MonoBehaviour
    {
        internal void Initialize()
        {
            OnInitialize();
        }
        protected virtual void OnInitialize()
        {

        }
        internal void LateInitialize()
        {
            OnLateInitialize();
        }
        protected virtual void OnLateInitialize()
        {

        }

        internal void Save()
        {
            OnSave();
        }
        protected virtual void OnSave()
        {

        }
    }
}