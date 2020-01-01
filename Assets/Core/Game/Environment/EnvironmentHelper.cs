using RTSLockstep.BehaviourHelpers;
using UnityEngine;

namespace RTSLockstep.Environment
{
    public class EnvironmentHelper : BehaviourHelper
    {
        public override ushort ListenInput
        {
            get
            {
                return 0;
            }
        }

        [SerializeField]
        private EnvironmentSaver[] _savers;
        public EnvironmentSaver[] Savers { get { return _savers; } }

#if UNITY_EDITOR
        [SerializeField]
        private GameObject _saverObject;
        GameObject SaverObject { get { return _saverObject; } }

        public void ScanAndSave()
        {
            if (SaverObject == null)
            {
                Debug.Log("Please assign 'Saver Object'");
                return;
            }
            UnityEditor.Undo.RecordObject(this, "Save environment");

            InitializeEnvironmentFromObject();
            foreach (EnvironmentSaver saver in Savers)
            {
                UnityEditor.Undo.RecordObject(saver, "Save " + saver.name);
                saver.Save();
                UnityEditor.EditorUtility.SetDirty(saver);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        }

        protected void Awake()
        {
            InitializeEnvironmentFromObject();
        }

        protected void InitializeEnvironmentFromObject()
        {
            if (SaverObject != null)
            {
                _savers = SaverObject.GetComponents<EnvironmentSaver>(); //Gets savers from SaverObject
            }
        }

        void Reset()
        {
            _saverObject = gameObject;
        }
#endif

        protected override void OnEarlyInitialize()
        {
            foreach (EnvironmentSaver saver in Savers)
            {
                if (saver == null)
                {
                    Debug.LogError("One of the EnvironmentSavers does not exist. Re-scan with the EnvironmentHelper component.");
                }
                saver.EarlyApply();
            }
        }

        protected override void OnInitialize()
        {
            foreach (EnvironmentSaver saver in Savers)
            {
                saver.Apply();
            }
        }

        protected override void OnLateInitialize()
        {
            foreach (EnvironmentSaver saver in Savers)
            {
                saver.LateApply();
            }
        }
    }
}