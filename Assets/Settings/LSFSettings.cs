using UnityEngine;
using System.Collections; using FastCollections;
using RTSLockstep.Data;
namespace RTSLockstep {
    [System.Serializable]
    public class LSFSettings : ScriptableObject {
        [SerializeField]
        private LSDatabase _database;
        public LSDatabase Database {
            get {return _database;}
#if UNITY_EDITOR
            set {_database = value;}
#endif
        }
    }
}