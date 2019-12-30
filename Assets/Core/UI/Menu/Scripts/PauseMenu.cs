using UnityEngine;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;

namespace RTSLockstep.Menu.UI
{
    public class PauseMenu : Menu
    {
        #region Properties
        private LSPlayer player;
        #endregion

        #region MonoBehavior
        protected override void Start()
        {
            base.Start();
            player = transform.root.GetComponent<LSPlayer>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Resume();
            }
        }
        #endregion

        #region Private
        protected override void SetButtons()
        {
            buttons = new string[] { "Resume", "Save Game", "Load Game", "Main Menu", "Exit Game" };
        }

        protected override void HandleButton(string text)
        {
            base.HandleButton(text);
            switch (text)
            {
                case "Resume":
                    Resume();
                    break;
                case "Save Game":
                    SaveGame();
                    break;
                case "Load Game":
                    LoadGame();
                    break;
                case "Main Menu":
                    ReturnToMainMenu();
                    break;
                case "Exit Game":
                    ExitGame();
                    break;
                default: break;
            }
        }

        private void Resume()
        {
            Time.timeScale = 1.0f;
            GetComponent<PauseMenu>().enabled = false;
            if (player)
            {
                player.GetComponent<PlayerInputHelper>().enabled = true;
            }
            Cursor.visible = false;
            GameResourceManager.MenuOpen = false;
        }

        private void SaveGame()
        {
            GetComponent<PauseMenu>().enabled = false;
            SaveMenu saveMenu = GetComponent<SaveMenu>();
            if (saveMenu)
            {
                saveMenu.enabled = true;
                saveMenu.Activate();
            }
        }

        protected override void HideCurrentMenu()
        {
            GetComponent<PauseMenu>().enabled = false;
        }
        #endregion
    }
}