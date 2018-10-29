using UnityEngine;
using System;
using FastCollections;

namespace RTSLockstep
{
	public static class Selector
	{
		public static event Action onChange;
		public static event Action<LSAgent> onAdd;
		public static event Action<LSAgent> onRemove;
		public static event Action onClear;

		private static LSAgent _mainAgent;

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

		public static LSAgent MainSelectedAgent { get { return _mainAgent; } private set { _mainAgent = value; } }

		private static FastSorter<LSAgent> _selectedAgents;

		private static FastSorter<LSAgent> SelectedAgents { get { return _selectedAgents; } }

		public static void Initialize ()
		{
			_selectedAgents = new FastSorter<LSAgent> ();
		}

		public static void Add (LSAgent agent)
		{
			if (agent.IsSelected == false) {
                if (MainSelectedAgent == null)
                    MainSelectedAgent = agent;
                //	agent.Controller.AddToSelection (agent);
                if (agent.MyAgentType == MainSelectedAgent.MyAgentType)
                {
                    PlayerManager.MainController.AddToSelection(agent);
                    agent.IsSelected = true;
                    onAdd(agent);
                }
			}
			else {
			}
		}

		public static void Remove (LSAgent agent)
		{
			agent.Controller.RemoveFromSelection (agent);
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
					FastBucket<LSAgent> selectedAgents = PlayerManager.AgentControllers[i].SelectedAgents;
					for (int j = 0; j < selectedAgents.PeakCount; j++) {
						if (selectedAgents.arrayAllocation [j]) {
							selectedAgents [j].IsSelected = false;
							onRemove (selectedAgents [j]);
						}
					}
					selectedAgents.FastClear ();
				}
			}
            //if (MainSelectedAgent && MainSelectedAgent.IsSelected)
            //{
            //    MainSelectedAgent.IsSelected = false;
            //}
			MainSelectedAgent = null;
			onClear ();

		}
	}

}