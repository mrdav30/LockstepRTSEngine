using RTSLockstep;
using UnityEngine;
using RTSLockstep.NetworkHelpers;

namespace RTSLockstep
{
	public class GameManager: MonoBehaviour
    {
        // private static int nextObjectId = 0;
        // change to serialized array property
        //  [SerializeField]
        //private BehaviourHelper[] helpers;

        protected virtual void Awake()
		{
            NetworkHelper networkHelper = gameObject.GetComponent<NetworkHelper>();
			if (networkHelper == null)
            {
				networkHelper = gameObject.AddComponent<DefaultNetworkHelper>();
            }

			//Currently deterministic but not guaranteed by Unity
			// may be add as serialized Array as property?  [SerializeField] private BehaviourHelper[] helpers; ?
			BehaviourHelper[] helpers = this.gameObject.GetComponentsInChildren<BehaviourHelper>();
			LockstepManager.Initialize(helpers, networkHelper);
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

		bool Quited = false;

        protected virtual void OnDisable()
		{
			if (Quited)
				return;
			LockstepManager.Deactivate();
		}

        private void OnApplicationQuit()
		{
			Quited = true;
			LockstepManager.Quit();
		}

	}
}