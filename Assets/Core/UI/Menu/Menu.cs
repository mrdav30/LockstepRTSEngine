using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.LSResources.Audio;

namespace RTSLockstep.Menu.UI
{
    public class Menu : MonoBehaviour
    {
        #region Properties
        public GUISkin mySkin;
        public Texture2D header;
        public AudioClip clickSound;
        public float clickVolume = 1.0f;

        protected string[] buttons;
        private AudioElement audioElement;
        #endregion

        #region MonoBehavior
        protected virtual void Start()
        {
            SetButtons();
            if (clickVolume < 0.0f)
            {
                clickVolume = 0.0f;
            }
            if (clickVolume > 1.0f)
            {
                clickVolume = 1.0f;
            }
            List<AudioClip> sounds = new List<AudioClip>();
            List<float> volumes = new List<float>();
            sounds.Add(clickSound);
            volumes.Add(clickVolume);
            audioElement = new AudioElement(sounds, volumes, "Menu", null);
        }

        protected virtual void OnGUI()
        {
            DrawMenu();
        }
        #endregion

        #region Private
        protected virtual void DrawMenu()
        {
            //default implementation for a menu consisting of a vertical list of buttons
            GUI.skin = mySkin;
            float menuHeight = GetMenuHeight();

            float groupLeft = Screen.width / 2 - GameResourceManager.MenuWidth / 2;
            float groupTop = Screen.height / 2 - menuHeight / 2;
            GUI.BeginGroup(new Rect(groupLeft, groupTop, GameResourceManager.MenuWidth, menuHeight));

            //background box
            GUI.Box(new Rect(0, 0, GameResourceManager.MenuWidth, menuHeight), "");
            //header image
            GUI.DrawTexture(new Rect(GameResourceManager.Padding, GameResourceManager.Padding, GameResourceManager.HeaderWidth, GameResourceManager.HeaderHeight), header);

            //welcome message
            float leftPos = GameResourceManager.Padding;
            float topPos = 2 * GameResourceManager.Padding + header.height;
            GUI.Label(new Rect(leftPos, topPos, GameResourceManager.MenuWidth - 2 * GameResourceManager.Padding, GameResourceManager.TextHeight), "Welcome " + PlayerManager.GetPlayerName());

            //menu buttons
            if (buttons != null)
            {
                leftPos = GameResourceManager.MenuWidth / 2 - GameResourceManager.ButtonWidth / 2;
                topPos += GameResourceManager.TextHeight + GameResourceManager.Padding;
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (i > 0)
                    {
                        topPos += GameResourceManager.ButtonHeight + GameResourceManager.Padding;
                    }
                    if (GUI.Button(new Rect(leftPos, topPos, GameResourceManager.ButtonWidth, GameResourceManager.ButtonHeight), buttons[i]))
                    {
                        HandleButton(buttons[i]);
                    }
                }
            }

            GUI.EndGroup();
        }

        protected virtual void SetButtons()
        {
            //a child class needs to set this for buttons to appear
        }

        protected virtual void HandleButton(string text)
        {
            if (audioElement != null)
            {
                audioElement.Play(clickSound);
            }
            //a child class needs to set this to handle button clicks
        }

        protected virtual float GetMenuHeight()
        {
            float buttonHeight = 0;
            float paddingHeight = 2 * GameResourceManager.Padding;
            float messageHeight = GameResourceManager.TextHeight + GameResourceManager.Padding;
            if (buttons != null)
            {
                buttonHeight = buttons.Length * GameResourceManager.ButtonHeight;
                paddingHeight += buttons.Length * GameResourceManager.Padding;
            }
            return GameResourceManager.HeaderHeight + buttonHeight + paddingHeight + messageHeight;
        }

        protected void ExitGame()
        {
            Application.Quit();
        }

        protected void ReturnToMainMenu()
        {

            GameResourceManager.LevelName = "";
            SceneManager.LoadScene("MainMenu");
        }

        protected void LoadGame()
        {
            HideCurrentMenu();
            LoadMenu loadMenu = GetComponent<LoadMenu>();
            if (loadMenu)
            {
                loadMenu.enabled = true;
                loadMenu.Activate();
            }
        }

        protected virtual void HideCurrentMenu()
        {
            //a child class needs to set this to hide itself when appropriate
        }
        #endregion
    }
}