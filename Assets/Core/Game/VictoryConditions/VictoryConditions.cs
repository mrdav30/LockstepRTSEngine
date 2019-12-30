using RTSLockstep.BehaviourHelpers;
using RTSLockstep.Player;
using RTSLockstep.Utility;

namespace RTSLockstep.VictoryConditions
{
    public abstract class VictoryCondition : BehaviourHelper
    {
        private HUD hud;
        private static bool Setted = false;

        protected LSPlayer[] Players;

        protected void Setup()
        {
            LoadDetails();
            Setted = true;
        }

        protected override void OnInitialize()
        {
            if (!Setted)
                Setup();
        }

        protected override void OnSimulate()
        {
            if (GameFinished())
            {
                //ResultsScreen resultsScreen = hud.GetComponent<ResultsScreen>();
                //resultsScreen.SetMetVictoryCondition(this);
                //resultsScreen.enabled = true;
                //Time.timeScale = 0.0f;
                //Cursor.visible = true;
                //ResourceManager.MenuOpen = true;
                //hud.enabled = false;
            }
        }

        public void SetCommanders(LSPlayer[] players)
        {
            Players = players;
        }

        public LSPlayer[] GetCommanders()
        {
            return Players;
        }

        public virtual bool GameFinished()
        {
            if (Players.IsNull())
            {
                return true;
            }
            foreach (LSPlayer palyer in Players)
            {
                if (PlayerMeetsConditions(palyer))
                {
                    return true;
                }
            }
            return false;
        }

        public LSPlayer GetWinner()
        {
            if (Players.IsNull())
            {
                return null;
            }
            foreach (LSPlayer player in Players)
            {
                if (PlayerMeetsConditions(player))
                {
                    return player;
                }
            }
            return null;
        }

        public abstract string GetDescription();

        // Any child class that extends must provide an implementation for this method. 
        public abstract bool PlayerMeetsConditions(LSPlayer player);

        private void LoadDetails()
        {
            LSPlayer[] players = FindObjectsOfType(typeof(LSPlayer)) as LSPlayer[];
            hud = PlayerManager.MainController.Player.GetComponentInChildren<HUD>();

            SetCommanders(players);
        }
    }
}