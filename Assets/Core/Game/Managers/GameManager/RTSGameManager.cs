using UnityEngine;
using RTSLockstep;
using UnityEngine.SceneManagement;

public class RTSGameManager : GameManager
{
    #region Properties
    private VictoryCondition[] victoryConditions;
    private HUD hud;

    static Replay LastSave = new Replay();
    #endregion

    #region MonoBehavior
    protected override void Awake()
    {
        base.Awake();
      //  LoadDetails();
    }

    protected override void Update()
    {
        base.Update();
        if (victoryConditions != null)
        {
            foreach (VictoryCondition victoryCondition in victoryConditions)
            {
                if (victoryCondition.GameFinished())
                {
                    ResultsScreen resultsScreen = hud.GetComponent<ResultsScreen>();
                    resultsScreen.SetMetVictoryCondition(victoryCondition);
                    resultsScreen.enabled = true;
                    Time.timeScale = 0.0f;
                    Cursor.visible = true;
                    ResourceManager.MenuOpen = true;
                    hud.enabled = false;
                }
            }
        }
    }

    void OnEnable()
    {
        //Tell our 'OnLevelFinishedLoading' function to start listening for a scene change as soon as this script is enabled.
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        //Tell our 'OnLevelFinishedLoading' function to stop listening for a scene change as soon as this script is disabled. Remember to always have an unsubscription for every delegate you subscribe to!
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
         //   LoadDetails();
    }

    void OnGUI()
    {
        //GUI.matrix = Matrix4x4.TRS(new Vector3(0, 0, 0), Quaternion.identity, new Vector3(2.5f, 2.5f, 1));

        //if (GUILayout.Button("Restart"))
        //{
        //    ReplayManager.Stop();
        //    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //}

        //if (GUILayout.Button("Playback"))
        //{
        //    LastSave = ReplayManager.SerializeCurrent();
        //    SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        //    ReplayManager.Play(LastSave);

        //}

    }
    #endregion

    #region Private
    private void LoadDetails()
    {
        AgentCommander[] commanders = GameObject.FindObjectsOfType(typeof(AgentCommander)) as AgentCommander[];
        foreach (AgentCommander commander in commanders)
        {
            if (commander.human)
            {
                hud = commander.GetComponentInChildren<HUD>();
            }
        }
        victoryConditions = GameObject.FindObjectsOfType(typeof(VictoryCondition)) as VictoryCondition[];
        if (victoryConditions != null)
        {
            foreach (VictoryCondition victoryCondition in victoryConditions)
            {
                victoryCondition.SetCommanders(commanders);
            }
        }
    }
    #endregion
}