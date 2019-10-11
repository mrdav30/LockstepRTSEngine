using System;
using FastCollections;

namespace RTSLockstep
{
    public static class Selector
    {
        public static event Action OnChange;
        public static event Action<RTSAgent> OnAdd;
        public static event Action<RTSAgent> OnRemove;
        public static event Action OnClear;

        public static RTSAgent MainSelectedAgent { get; private set; }
        public static FastSorter<RTSAgent> SelectedAgents { get; private set; }

        static Selector()
        {
            OnAdd += (a) => Change();
            OnRemove += (a) => Change();
            OnClear += () => Change();
        }

        public static void Initialize()
        {
            SelectedAgents = new FastSorter<RTSAgent>();
        }

        private static void Change()
        {
            OnChange?.Invoke();
        }

        public static void Add(RTSAgent agent)
        {
            if (!agent.IsSelected)
            {
                if (MainSelectedAgent.IsNull())
                {
                    MainSelectedAgent = agent;
                }

                //only add agents of the same owner
                if (agent.MyAgentType == MainSelectedAgent.MyAgentType
                    && agent.IsOwnedBy(MainSelectedAgent.Controller))
                {
                    PlayerManager.MainController.AddToSelection(agent);
                    agent.IsSelected = true;
                    OnAdd(agent);
                }
            }
        }

        public static void Remove(RTSAgent agent)
        {
            PlayerManager.MainController.RemoveFromSelection(agent);
            agent.IsSelected = false;
            if (agent == MainSelectedAgent)
            {
                agent = SelectedAgents.Count > 0 ? SelectedAgents.PopMax() : null;
            }
            OnRemove(agent);
        }

        public static void Clear()
        {
            for (int i = 0; i < PlayerManager.AgentControllers.PeakCount; i++)
            {
                if (PlayerManager.AgentControllers.arrayAllocation[i])
                {
                    FastBucket<RTSAgent> selectedAgents = PlayerManager.AgentControllers[i].SelectedAgents;
                    for (int j = 0; j < selectedAgents.PeakCount; j++)
                    {
                        if (selectedAgents.arrayAllocation[j])
                        {
                            selectedAgents[j].IsSelected = false;
                            OnRemove(selectedAgents[j]);
                        }
                    }
                    selectedAgents.FastClear();
                }
            }
            MainSelectedAgent = null;
            OnClear();
        }
    }
}