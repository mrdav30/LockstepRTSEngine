//=======================================================================
// Copyright (c) 2015 John Pan
// Distributed under the MIT License.
// (See accompanying file LICENSE or copy at
// http://opensource.org/licenses/MIT)
//=======================================================================
using Newtonsoft.Json;
using RTSLockstep.Data;
using UnityEngine;

namespace RTSLockstep
{
    public abstract class Ability : MonoBehaviour//CerealBehaviour
    {
        private bool IsFirstFrame = true;

        private RTSAgent _agent;

        public RTSAgent Agent
        {
            get
            {
#if UNITY_EDITOR
                if (_agent == null)
                    return GetComponent<RTSAgent>();
#endif
                return _agent;
            }
        }

        public string MyAbilityCode { get; private set; }

        public AbilityDataItem Data { get; private set; }

        public int ID { get; private set; }

        public Transform CachedTransform { get { return Agent.CachedTransform; } }

        public GameObject CachedGameObject { get { return Agent.CachedGameObject; } }

        public int VariableContainerTicket { get; private set; }

        private LSVariableContainer _variableContainer;

        public LSVariableContainer VariableContainer { get { return _variableContainer; } }

        private bool isCasting;
        public bool IsCasting
        {
            get
            {
                return isCasting;
            }
            protected set
            {
                if (value != isCasting)
                {
                    if (value == true)
                    {
                        Agent.CheckCasting = false;
                    }
                    else
                    {
                        Agent.CheckCasting = true;
                    }
                    isCasting = value;
                }
            }
        }

        private bool isFocused;
        public bool IsFocused
        {
            get
            {
                return isFocused;
            }
            protected set
            {
                if (value != isFocused)
                {
                    if (value == true)
                    {
                        Agent.CheckFocus = false;
                    }
                    else
                    {
                        Agent.CheckFocus = true;
                    }
                    isFocused = value;
                }
            }
        }

        protected bool loadedSavedValues = false;

        internal void Setup(RTSAgent agent, int id)
        {
            System.Type mainType = this.GetType();
            if (mainType.IsSubclassOf(typeof(ActiveAbility)))
            {
                while (mainType.BaseType != typeof(ActiveAbility) &&
                       mainType.GetCustomAttributes(typeof(CustomActiveAbilityAttribute), false).Length == 0)
                {
                    mainType = mainType.BaseType;
                }
                Data = AbilityDataItem.FindInterfacer(mainType);
                if (Data == null)
                {
                    throw new System.ArgumentException("The Ability of type " + mainType + " has not been registered in database");
                }
                this.MyAbilityCode = Data.Name;
            }
            else
            {
                this.MyAbilityCode = mainType.Name;
            }
            _agent = agent;
            ID = id;
            TemplateSetup();
            OnSetup();
            this.VariableContainerTicket = LSVariableManager.Register(this);
            this._variableContainer = LSVariableManager.GetContainer(VariableContainerTicket);
        }

        internal void LateSetup()
        {
            this.OnLateSetup();
        }

        /// <summary>
        /// Override for communicating with other abilities in the setup phase
        /// </summary>
        protected virtual void OnLateSetup() { }

        protected virtual void TemplateSetup()
        {

        }

        protected virtual void OnSetup()
        {

        }

        internal void Initialize()
        {
            VariableContainer.Reset();
            IsCasting = false;
            IsFirstFrame = true;

            OnInitialize();
        }

        protected virtual void OnInitialize()
        {
        }

        private void FirstFrame()
        {
            OnFirstFrame();
        }

        protected virtual void OnFirstFrame()
        {

        }

        internal void Simulate()
        {
            if (IsFirstFrame)
            {
                FirstFrame();
            }
            TemplateSimulate();

            OnSimulate();
            if (isCasting)
            {
                OnCast();
            }
        }

        protected virtual void TemplateSimulate()
        {

        }

        protected virtual void OnSimulate()
        {
        }

        internal void LateSimulate()
        {
            OnLateSimulate();
        }

        protected virtual void OnLateSimulate()
        {

        }

        protected virtual void OnCast()
        {
        }

        internal void Visualize()
        {
            OnVisualize();
        }

        protected virtual void OnVisualize()
        {
        }

        public void LateVisualize()
        {
            this.OnLateVisualize();
        }

        protected virtual void OnLateVisualize()
        {

        }

        public void OnGUI()
        {
            doGUI();
        }

        protected virtual void doGUI() { }

        public void BeginCast()
        {
            OnBeginCast();
        }

        protected virtual void OnBeginCast()
        {
        }

        public void StopCast()
        {
            OnStopCast();
        }

        protected virtual void OnStopCast()
        {
        }

        public void Deactivate()
        {
            IsCasting = false;
            OnDeactivate();
        }

        protected virtual void OnDeactivate()
        {
        }

        public void SaveDetails(JsonWriter writer)
        {
            OnSaveDetails(writer);
        }

        protected virtual void OnSaveDetails(JsonWriter writer) { }

        public void LoadDetails(JsonTextReader reader)
        {
            while (reader.Read())
            {
                if (reader.Value != null)
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        string propertyName = (string)reader.Value;
                        reader.Read();
                        OnLoadProperty(reader, propertyName, reader.Value);
                    }
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    loadedSavedValues = true;
                    return;
                }
            }
            //loaded position invalidates the selection bounds so they must be recalculated
            loadedSavedValues = true;
        }

        protected virtual void OnLoadProperty(JsonTextReader reader, string propertyName, object readValue) { }
    }
}