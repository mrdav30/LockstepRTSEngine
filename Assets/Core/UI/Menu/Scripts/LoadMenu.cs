using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.LSResources.Audio;
using RTSLockstep.Utility;

namespace RTSLockstep.Menu.UI
{
    public class LoadMenu : MonoBehaviour
    {
        #region Properties
        public GUISkin mainSkin, selectionSkin;
        public AudioClip clickSound;
        public float clickVolume = 1.0f;

        private AudioElement audioElement;
        #endregion

        #region MonoBehavior
        void Start()
        {
            Activate();
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
            audioElement = new AudioElement(sounds, volumes, "LoadMenu", null);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelLoad();
            }
        }

        void OnGUI()
        {
            if (SelectionList.MouseDoubleClick())
            {
                PlayClick();
                StartLoad();
            }

            GUI.skin = mainSkin;
            float menuHeight = GetMenuHeight();
            float groupLeft = Screen.width / 2 - GameResourceManager.MenuWidth / 2;
            float groupTop = Screen.height / 2 - menuHeight / 2;
            Rect groupRect = new Rect(groupLeft, groupTop, GameResourceManager.MenuWidth, menuHeight);
            GUI.BeginGroup(groupRect);
            //background box
            GUI.Box(new Rect(0, 0, GameResourceManager.MenuWidth, menuHeight), "");
            //menu buttons
            float leftPos = GameResourceManager.Padding;
            float topPos = menuHeight - GameResourceManager.Padding - GameResourceManager.ButtonHeight;
            if (GUI.Button(new Rect(leftPos, topPos, GameResourceManager.ButtonWidth, GameResourceManager.ButtonHeight), "Load Game"))
            {
                PlayClick();
                StartLoad();
            }
            leftPos += GameResourceManager.ButtonWidth + GameResourceManager.Padding;
            if (GUI.Button(new Rect(leftPos, topPos, GameResourceManager.ButtonWidth, GameResourceManager.ButtonHeight), "Cancel"))
            {
                PlayClick();
                CancelLoad();
            }
            GUI.EndGroup();

            //selection list, needs to be called outside of the group for the menu
            float selectionLeft = groupRect.x + GameResourceManager.Padding;
            float selectionTop = groupRect.y + GameResourceManager.Padding;
            float selectionWidth = groupRect.width - 2 * GameResourceManager.Padding;
            float selectionHeight = groupRect.height - GetMenuItemsHeight() - GameResourceManager.Padding;
            SelectionList.Draw(selectionLeft, selectionTop, selectionWidth, selectionHeight, selectionSkin);
        }
        #endregion

        #region Private
        private float GetMenuHeight()
        {
            return 250 + GetMenuItemsHeight();
        }

        private float GetMenuItemsHeight()
        {
            return GameResourceManager.ButtonHeight + 2 * GameResourceManager.Padding;
        }

        public void Activate()
        {
            SelectionList.LoadEntries(PlayerManager.GetSavedGames());
        }

        private void StartLoad()
        {
            string newLevel = SelectionList.GetCurrentEntry();
            if (newLevel != "")
            {
                GameResourceManager.LevelName = newLevel;
                // make use of a pair of empty scenes to load details into. 
                // We need two of these for the scenario where we load a game into one and then want to load a game again
                if (SceneManager.GetActiveScene().name != "BlankMap1")
                {
                    SceneManager.LoadScene("BlankMap1");
                }
                else if (SceneManager.GetActiveScene().name != "BlankMap2")
                {
                    SceneManager.LoadScene("BlankMap2");
                }
                //makes sure that the loaded level runs at normal speed
                Time.timeScale = 1.0f;
            }
        }

        private void CancelLoad()
        {
            GetComponent<LoadMenu>().enabled = false;
            PauseMenu pause = GetComponent<PauseMenu>();
            if (pause)
            {
                pause.enabled = true;
            }
            else
            {
                MainMenu main = GetComponent<MainMenu>();
                if (main)
                {
                    main.enabled = true;
                }
            }
        }

        private void PlayClick()
        {
            if (audioElement != null)
            {
                audioElement.Play(clickSound);
            }
        }
        #endregion
    }
}