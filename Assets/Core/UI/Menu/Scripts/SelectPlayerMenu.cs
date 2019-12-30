using UnityEngine;
using System.Collections.Generic;
using RTSLockstep.Managers.GameManagers;
using RTSLockstep.Player;
using RTSLockstep.LSResources.Audio;
using RTSLockstep.Utility;

namespace RTSLockstep.Menu.UI
{
    public class SelectPlayerMenu : MonoBehaviour
    {
        #region Properties
        public GUISkin mySkin;
        public GUISkin selectionSkin;
        public AudioClip clickSound;
        public float clickVolume = 1.0f;

        private string playerName = "NewPlayer";
        private int avatarIndex = -1;
        private int controllerId = -1;
        private int playerIndex = -1;
        private Texture2D[] avatars;
        private AudioElement audioElement;
        #endregion

        #region MonoBehavior
        private void Start()
        {
            avatars = GameResourceManager.GetAvatars();
            if (avatars.Length > 0)
            {
                avatarIndex = 0;
            }
            SelectionList.LoadEntries(PlayerManager.GetPlayerNames());
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
            audioElement = new AudioElement(sounds, volumes, "SelectPlayerMenu", null);
        }

        void OnGUI()
        {
            if (SelectionList.MouseDoubleClick())
            {
                PlayClick();
                playerName = SelectionList.GetCurrentEntry();
                SelectPlayer();
            }
            GUI.skin = mySkin;

            float menuHeight = GetMenuHeight();
            float groupLeft = Screen.width / 2 - GameResourceManager.MenuWidth / 2;
            float groupTop = Screen.height / 2 - menuHeight / 2;
            Rect groupRect = new Rect(groupLeft, groupTop, GameResourceManager.MenuWidth, menuHeight);

            GUI.BeginGroup(groupRect);
            //background box
            GUI.Box(new Rect(0, 0, GameResourceManager.MenuWidth, menuHeight), "");
            //menu buttons
            float leftPos = GameResourceManager.MenuWidth / 2 - GameResourceManager.ButtonWidth / 2;
            float topPos = menuHeight - GameResourceManager.Padding - GameResourceManager.ButtonHeight;
            if (GUI.Button(new Rect(leftPos, topPos, GameResourceManager.ButtonWidth, GameResourceManager.ButtonHeight), "Select"))
            {
                PlayClick();
                SelectPlayer();
            }
            //text area for commander to type new name
            float textTop = menuHeight - 2 * GameResourceManager.Padding - GameResourceManager.ButtonHeight - GameResourceManager.TextHeight;
            float textWidth = GameResourceManager.MenuWidth - 2 * GameResourceManager.Padding;
            playerName = GUI.TextField(new Rect(GameResourceManager.Padding, textTop, textWidth, GameResourceManager.TextHeight), playerName, 14);

            SelectionList.SetCurrentEntry(playerName);

            if (avatarIndex >= 0)
            {
                float avatarLeft = GameResourceManager.MenuWidth / 2 - avatars[avatarIndex].width / 2;
                float avatarTop = textTop - GameResourceManager.Padding - avatars[avatarIndex].height;
                float avatarWidth = avatars[avatarIndex].width;
                float avatarHeight = avatars[avatarIndex].height;
                GUI.DrawTexture(new Rect(avatarLeft, avatarTop, avatarWidth, avatarHeight), avatars[avatarIndex]);
                float buttonTop = textTop - GameResourceManager.Padding - GameResourceManager.ButtonHeight;
                float buttonLeft = GameResourceManager.Padding;
                if (GUI.Button(new Rect(buttonLeft, buttonTop, GameResourceManager.ButtonHeight, GameResourceManager.ButtonHeight), "<"))
                {
                    PlayClick();
                    avatarIndex -= 1;
                    if (avatarIndex < 0)
                    {
                        avatarIndex = avatars.Length - 1;
                    }
                }
                buttonLeft = GameResourceManager.MenuWidth - GameResourceManager.Padding - GameResourceManager.ButtonHeight;
                if (GUI.Button(new Rect(buttonLeft, buttonTop, GameResourceManager.ButtonHeight, GameResourceManager.ButtonHeight), ">"))
                {
                    PlayClick();
                    avatarIndex = (avatarIndex + 1) % avatars.Length;
                }
            }
            GUI.EndGroup();

            //selection list, needs to be called outside of the group for the menu
            string prevSelection = SelectionList.GetCurrentEntry();
            float selectionLeft = groupRect.x + GameResourceManager.Padding;
            float selectionTop = groupRect.y + GameResourceManager.Padding;
            float selectionWidth = groupRect.width - 2 * GameResourceManager.Padding;
            float selectionHeight = groupRect.height - GetMenuItemsHeight() - GameResourceManager.Padding;
            SelectionList.Draw(selectionLeft, selectionTop, selectionWidth, selectionHeight, selectionSkin);
            string newSelection = SelectionList.GetCurrentEntry();
            //set saveName to be name selected in list if selection has changed
            if (prevSelection != newSelection)
            {
                playerName = newSelection;
                avatarIndex = PlayerManager.GetAvatar(playerName);
            }
        }
        #endregion

        #region Private
        //how high we want the SelectionList to be, 
        //but writing things this way does allow us to guarantee that the SelectionList will fill the available height in the Menu
        private float GetMenuHeight()
        {
            return 250 + GetMenuItemsHeight();
        }

        private float GetMenuItemsHeight()
        {
            float avatarHeight = 0;
            if (avatars.Length > 0)
            {
                avatarHeight = avatars[0].height + 2 * GameResourceManager.Padding;
            }
            return avatarHeight + GameResourceManager.ButtonHeight + GameResourceManager.TextHeight + 3 * GameResourceManager.Padding;
        }

        private void SelectPlayer()
        {
            PlayerManager.SelectPlayer(playerName, avatarIndex, controllerId, playerIndex);
            GetComponent<SelectPlayerMenu>().enabled = false;
            MainMenu main = GetComponent<MainMenu>();
            if (main)
            {
                main.enabled = true;
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