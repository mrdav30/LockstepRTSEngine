using UnityEngine;
using RTSLockstep.Utility;

namespace RTSLockstep.Managers
{
    public class UnityInstance : MonoBehaviour
    {
        private static UnityInstance instance;

        public static UnityInstance Instance
        {
            get
            {
                if (instance.IsNull())
                {
                    instance = new GameObject("UnityInstance").AddComponent<UnityInstance>();
                    DontDestroyOnLoad(instance.gameObject);
                }
                return instance;
            }
        }

    }
}

