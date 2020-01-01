using UnityEngine;
using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Networking;
using RTSLockstep.Utility;

namespace RTSLockstep.Managers.GameManagers
{
    public class GameManager : MonoBehaviour
    {
        public BehaviourHelper[] BehaviourHelpers;

        private bool hasQuit;

        protected virtual void Awake()
        {
            NetworkHelper networkHelper = gameObject.GetComponent<NetworkHelper>();
            if (networkHelper.IsNull())
            {
                networkHelper = gameObject.AddComponent<DefaultNetworkHelper>();
            }

            LockstepManager.Initialize(BehaviourHelpers, networkHelper);
        }

        private void FixedUpdate()
        {
            LockstepManager.Simulate();
        }

        protected virtual void Update()
        {
            LockstepManager.Visualize();
        }

        private void LateUpdate()
        {
            LockstepManager.LateVisualize();
        }

        protected virtual void OnGUI()
        {
            LockstepManager.UpdateGUI();
        }

        protected virtual void OnDisable()
        {
            if (hasQuit)
            {
                return;
            }

            LockstepManager.Deactivate();
        }

        private void OnApplicationQuit()
        {
            hasQuit = true;
            LockstepManager.Quit();
        }
    }
}