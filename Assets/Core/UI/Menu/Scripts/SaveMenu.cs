using UnityEngine;
using RTSLockstep;
using System.Collections.Generic;

public class SaveMenu : MonoBehaviour
{
    #region Properties
    public GUISkin mySkin, selectionSkin;
    public AudioClip clickSound;
    public float clickVolume = 1.0f;

    private string saveName = "NewGame";
    private ConfirmDialog confirmDialog = new ConfirmDialog();
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
        audioElement = new AudioElement(sounds, volumes, "SaveMenu", null);
    }

    void Update()
    {
        //handle escape key 
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (confirmDialog.IsConfirming())
            {
                confirmDialog.EndConfirmation();
            }
            else
            {
                CancelSave();
            }
        }
        //handle enter key in confirmation dialog
        if (Input.GetKeyDown(KeyCode.Return) && confirmDialog.IsConfirming())
        {
            confirmDialog.EndConfirmation();
            SaveGame();
        }
    }

    void OnGUI()
    {
        if (confirmDialog.IsConfirming())
        {
            string message = "\"" + saveName + "\" already exists. Do you wish to continue?";
            confirmDialog.Show(message, mySkin);
        }
        else if (confirmDialog.MadeChoice())
        {
            if (confirmDialog.ClickedYes())
            {
                SaveGame();
            }
            confirmDialog.EndConfirmation();
        }
        else
        {
            if (SelectionList.MouseDoubleClick())
            {
                PlayClick();
                saveName = SelectionList.GetCurrentEntry();
                StartSave();
            }
            GUI.skin = mySkin;
            DrawMenu();
            //handle enter being hit when typing in the text field
            if (Event.current.keyCode == KeyCode.Return)
            {
                StartSave();
            }
            // if typing and cancel is hit, nothing happens ...
            // doesn't appear to be a quick fix either ...
        }
    }
    #endregion

    #region Public
    public void Activate()
    {
        SelectionList.LoadEntries(RTSLockstep.PlayerManager.GetSavedGames());
        if (GameResourceManager.LevelName != null && GameResourceManager.LevelName != "")
        {
            saveName = GameResourceManager.LevelName;
        }
    }
    #endregion

    #region Private
    private void DrawMenu()
    {
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
        if (GUI.Button(new Rect(leftPos, topPos, GameResourceManager.ButtonWidth, GameResourceManager.ButtonHeight), "Save Game"))
        {
            PlayClick();
            StartSave();
        }
        leftPos += GameResourceManager.ButtonWidth + GameResourceManager.Padding;
        if (GUI.Button(new Rect(leftPos, topPos, GameResourceManager.ButtonWidth, GameResourceManager.ButtonHeight), "Cancel"))
        {
            PlayClick();
            CancelSave();
        }
        //text area for commander to type new name
        float textTop = menuHeight - 2 * GameResourceManager.Padding - GameResourceManager.ButtonHeight - GameResourceManager.TextHeight;
        float textWidth = GameResourceManager.MenuWidth - 2 * GameResourceManager.Padding;
        saveName = GUI.TextField(new Rect(GameResourceManager.Padding, textTop, textWidth, GameResourceManager.TextHeight), saveName, 60);
        SelectionList.SetCurrentEntry(saveName);
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
            saveName = newSelection;
        }
    }

    private float GetMenuHeight()
    {
        return 250 + GetMenuItemsHeight();
    }

    private float GetMenuItemsHeight()
    {
        return GameResourceManager.ButtonHeight + GameResourceManager.TextHeight + 3 * GameResourceManager.Padding;
    }

    private void StartSave()
    {
        //prompt for override of name if necessary
        if (SelectionList.Contains(saveName))
        {
            confirmDialog.StartConfirmation(clickSound, audioElement);
        }
        else {
            SaveGame();
        }
    }

    private void CancelSave()
    {
        GetComponent<SaveMenu>().enabled = false;
        PauseMenu pause = GetComponent<PauseMenu>();
        if (pause)
        {
            pause.enabled = true;
        }
    }

    private void SaveGame()
    {
        SaveManager.SaveGame(saveName);
        GameResourceManager.LevelName = saveName;
        GetComponent<SaveMenu>().enabled = false;
        PauseMenu pause = GetComponent<PauseMenu>();
        if (pause)
        {
            pause.enabled = true;
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