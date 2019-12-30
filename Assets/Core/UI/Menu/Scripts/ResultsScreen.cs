using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.LSResources.Audio;
using RTSLockstep.VictoryConditions;

/*
 * look at updating this screen to include things like match statistics for Players to make it much more interesting.
 */
namespace RTSLockstep.Menu.UI
{
    public class ResultsScreen : MonoBehaviour
    {
        #region Properties
        public GUISkin skin;
        public AudioClip clickSound;
        public float clickVolume = 1.0f;

        private AudioElement audioElement;
        private LSPlayer winner;
        private VictoryCondition metVictoryCondition;
        #endregion

        #region MonoBehavior
        void Start()
        {
            List<AudioClip> sounds = new List<AudioClip>();
            List<float> volumes = new List<float>();
            sounds.Add(clickSound);
            volumes.Add(clickVolume);
            audioElement = new AudioElement(sounds, volumes, "ResultsScreen", null);
        }

        void OnGUI()
        {
            GUI.skin = skin;

            GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));

            //display 
            float padding = GameResourceManager.Padding;
            float itemHeight = GameResourceManager.ButtonHeight;
            float buttonWidth = GameResourceManager.ButtonWidth;
            float leftPos = padding;
            float topPos = padding;
            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            string message = "Game Over";
            if (winner)
            {
                message = "Congratulations " + winner.username + "! You have won by " + metVictoryCondition.GetDescription();
            }
            GUI.Label(new Rect(leftPos, topPos, Screen.width - 2 * padding, itemHeight), message);
            leftPos = Screen.width / 2 - padding / 2 - buttonWidth;
            topPos += itemHeight + padding;
            if (GUI.Button(new Rect(leftPos, topPos, buttonWidth, itemHeight), "New Game"))
            {
                PlayClick();
                //makes sure that the loaded level runs at normal speed
                Time.timeScale = 1.0f;
                GameResourceManager.MenuOpen = false;
                SceneManager.LoadScene("Map");
            }
            leftPos += padding + buttonWidth;
            if (GUI.Button(new Rect(leftPos, topPos, buttonWidth, itemHeight), "Main Menu"))
            {
                GameResourceManager.LevelName = "";
                SceneManager.LoadScene("MainMenu");
                Cursor.visible = true;
            }

            GUI.EndGroup();
        }
        #endregion

        #region Public
        public void SetMetVictoryCondition(VictoryCondition victoryCondition)
        {
            if (!victoryCondition)
            {
                return;
            }
            metVictoryCondition = victoryCondition;
            winner = metVictoryCondition.GetWinner();
        }
        #endregion

        #region Private
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