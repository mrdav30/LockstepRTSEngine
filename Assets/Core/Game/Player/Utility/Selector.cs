using UnityEngine;
using System;
using FastCollections;

namespace RTSLockstep
{
	public static class Selector
	{
		public static event Action onChange;
		public static event Action<RTSAgent> onAdd;
		public static event Action<RTSAgent> onRemove;
		public static event Action onClear;

		private static RTSAgent _mainAgent;
        public static RTSAgent MainSelectedAgent { get { return _mainAgent; } private set { _mainAgent = value; } }

        static Selector ()
		{
			onAdd += (a) => Change ();
			onRemove += (a) => Change ();
			onClear += () => Change ();
		}

		private static void Change ()
		{
			if (onChange != null)
				onChange ();
		}

		private static FastSorter<RTSAgent> _selectedAgents;

		private static FastSorter<RTSAgent> SelectedAgents { get { return _selectedAgents; } }

		public static void Initialize ()
		{
			_selectedAgents = new FastSorter<RTSAgent> ();
		}

		public static void Add (RTSAgent agent)
		{
			if (agent.IsSelected == false) {
                if (MainSelectedAgent == null)
                {
                    MainSelectedAgent = agent;
                }

                //only add agents of the same owner
                if (agent.MyAgentType == MainSelectedAgent.MyAgentType
                    && agent.IsOwnedBy(MainSelectedAgent.Controller))
                {
                    PlayerManager.MainController.AddToSelection(agent);
                    agent.IsSelected = true;
                    onAdd(agent);
                }
			}
		}

		public static void Remove (RTSAgent agent)
		{
            PlayerManager.MainController.RemoveFromSelection (agent);
			agent.IsSelected = false;
			if (agent == MainSelectedAgent) {
				agent = SelectedAgents.Count > 0 ? SelectedAgents.PopMax () : null;
			}
			onRemove (agent);
		}

		public static void Clear ()
		{
			for (int i = 0; i < PlayerManager.AgentControllers.PeakCount; i++) {
				if (PlayerManager.AgentControllers.arrayAllocation [i]) {
					FastBucket<RTSAgent> selectedAgents = PlayerManager.AgentControllers[i].SelectedAgents;
					for (int j = 0; j < selectedAgents.PeakCount; j++) {
						if (selectedAgents.arrayAllocation [j]) {
							selectedAgents [j].IsSelected = false;
							onRemove (selectedAgents [j]);
						}
					}
					selectedAgents.FastClear ();
				}
			}
			MainSelectedAgent = null;
			onClear ();
		}
	}

}