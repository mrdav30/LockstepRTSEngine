using System;

using RTSLockstep.Agents;
using RTSLockstep.Agents.AgentController;
using RTSLockstep.Utility;
using RTSLockstep.Utility.FastCollections;

namespace RTSLockstep.Player.Utility
{
    public static class Selector
    {
        public static event Action OnChange;
        public static event Action<LSAgent> OnAdd;
        public static event Action<LSAgent> OnRemove;
        public static event Action OnClear;

        public static LSAgent MainSelectedAgent { get; private set; }
        public static FastSorter<LSAgent> SelectedAgents { get; private set; }

        static Selector()
        {
            OnAdd += (a) => Change();
            OnRemove += (a) => Change();
            OnClear += () => Change();
        }

        public static void Initialize()
        {
            SelectedAgents = new FastSorter<LSAgent>();
        }

        private static void Change()
        {
            OnChange?.Invoke();
        }

        public static void Add(LSAgent agent)
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
                    PlayerManager.CurrentPlayerController.AddToSelection(agent);
                    agent.IsSelected = true;
                    OnAdd(agent);
                }
            }
        }

        public static void Remove(LSAgent agent)
        {
            PlayerManager.CurrentPlayerController.RemoveFromSelection(agent);
            agent.IsSelected = false;
            if (agent == MainSelectedAgent)
            {
                agent = SelectedAgents.Count > 0 ? SelectedAgents.PopMax() : null;
            }
            OnRemove(agent);
        }

        public static void Clear()
        {
            for (int i = 0; i < GlobalAgentController.LocalAgentControllers.PeakCount; i++)
            {
                if (GlobalAgentController.LocalAgentControllers.arrayAllocation[i])
                {
                    FastBucket<LSAgent> selectedAgents = GlobalAgentController.LocalAgentControllers[i].SelectedAgents;
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