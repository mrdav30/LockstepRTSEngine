using UnityEngine;
using System.Collections;

// Generic singleton class for MonoBehaviours.
// Subclass from this instead of Monobehaviour if you want the component to be a singleton.
// Note:
// Any subclasses need to make sure that the instance is valid before doing any work.
// See the comments for Awake().

namespace RTSLockstep
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {

        // Make sure that Instance can only be accessed by one thread at a time.
        private static object mLock = new object();

        private static T mInstance;
        public static T Instance
        {
            get
            {
                lock (mLock)
                { // only one thread at a time
                  // if mInstance is not already set, then initialize
                    if (mInstance == null)
                    {
                        // find the component in the scene
                        mInstance = GameObject.FindObjectOfType<T>();
                        if (mInstance == null)
                        {
                            UnityEngine.Debug.LogError("Expected to find an instance of the " + typeof(T) + " component in the hierarchy but none found!\n"
                            + "Attach the " + typeof(T) + " component to a GameObject in the hierarchy.");
                            Abort();
                        }
                        DontDestroyOnLoad(mInstance.transform.gameObject);
                    }
                    return mInstance;
                }
            }
        }

        // Any subclasses should make sure that (Instance == this) before doing anything in the Awake() method.
        // eg.
        // private void Awake() {
        // if (Instance != this) return;
        // // do stuff
        // }
        //
        private void Awake()
        {
            if (Instance != this)
            {
                //Debug.Log("Destroyed duplicate instance of " + typeof(T));
                Destroy(this.gameObject);
                return;
            }
        }

        // Stops the game from running.
        // In the editor, the game is stopped.
        // In a standalone application, quit the application.
        private static void Abort()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        }

    }
}