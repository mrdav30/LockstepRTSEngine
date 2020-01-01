using RTSLockstep.Player.Commands;
using UnityEngine;

namespace RTSLockstep.BehaviourHelpers
{
    /// <summary>
    /// Global singleton abilities. Attach to the GameManager gameobject.
    /// </summary>
    public abstract class BehaviourHelper : MonoBehaviour, ILockstepEventsHandler
    {
        [Range(1, 9999), Tooltip("Determines the order in which this helper will call it's event behavior")]
        public int BasePriority;

        private ushort CachedListenInput;

        public virtual ushort ListenInput
        {
            get { return 0; }
        }

        public ushort GetListenInput()
        {
            return CachedListenInput;
        }

        public void Initialize()
        {
            OnInitialize();
        }

        public void EarlyInitialize()
        {
            CachedListenInput = ListenInput;

            OnEarlyInitialize();
        }

        protected virtual void OnEarlyInitialize()
        {

        }

        protected virtual void OnInitialize()
        {
        }

        public void LateInitialize()
        {
            OnLateInitialize();
        }

        protected virtual void OnLateInitialize()
        {
        }

        public void Simulate()
        {
            OnSimulate();
        }

        protected virtual void OnSimulate()
        {
        }

        public void LateSimulate()
        {
            OnLateSimulate();
        }

        protected virtual void OnLateSimulate()
        {
        }

        public void Visualize()
        {
            OnVisualize();
        }

        protected virtual void OnVisualize()
        {
        }

        public void LateVisualize()
        {
            OnLateVisualize();
        }

        public void UpdateGUI()
        {
            OnUpdateGUI();
        }

        protected virtual void OnUpdateGUI()
        {
        }

        protected virtual void OnLateVisualize()
        {

        }

        public void GlobalExecute(Command com)
        {
            OnExecute(com);
        }

        protected virtual void OnExecute(Command com)
        {
        }

        public void RawExecute(Command com)
        {
            OnRawExecute(com);
        }

        protected virtual void OnRawExecute(Command com)
        {

        }

        public void GameStart()
        {
            OnGameStart();
        }

        protected virtual void OnGameStart()
        {

        }

        public void Deactivate()
        {
            OnDeactivate();
        }

        protected virtual void OnDeactivate()
        {
        }
    }
}