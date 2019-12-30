using RTSLockstep.Settings;
using System;
using TypeReferences;
using UnityEditor;
using UnityEngine;

namespace RTSLockstep.Data
{
    [System.Serializable]
    public sealed class EditorLSDatabaseWindow : EditorWindow
    {
        public static EditorLSDatabaseWindow Window { get; private set; }
        public string DatabasePath { get; private set; }
        public bool IsLoaded { get; private set; }
        public EditorLSDatabase DatabaseEditor
        {
            get
            {
                return _databaseEditor;
            }
            set
            {
                _databaseEditor = value;
            }
        }
        public LSDatabase Database { get { return _database; } }
        public static bool CanSave { get { return Application.isPlaying == false; } }

        [SerializeField, ClassImplements(typeof(IDatabase))]
        private ClassTypeReference _databaseType;
        private Type DatabaseType { get { return _databaseType; } }
        private LSDatabase _database;
        private EditorLSDatabase _databaseEditor;
        private Vector2 scrollPos;
        private bool settingsFoldout = false;
        //Rect windowRect = new Rect (0, 0, 500, 500);
        private TextAsset jsonFile;
        private bool loadInited = false;

        [MenuItem("Lockstep/Database %#l")]
        public static void Menu()
        {
            EditorLSDatabaseWindow window = EditorWindow.GetWindow<EditorLSDatabaseWindow>();
            window.titleContent = new GUIContent("Lockstep Database");
            window.minSize = new Vector2(400, 100);
            window.Show();
        }

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                LoadInit();
            }
            else
            {
                loadInited = false;
                IsLoaded = false;
            }
        }

        void LoadInit()
        {
            Window = this;
            this.LoadDatabase(LSFSettingsManager.GetSettings().Database);
            if (this.Database != null)
            {
                _databaseType = LSFSettingsManager.GetSettings().Database.GetType();
                if (_databaseType.Type == null)
                {
                    _databaseType = typeof(DefaultLSDatabase);
                }
            }
            loadInited = true;
        }

        void OnGUI()
        {
            // prevent changing db values during gameplay
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Values cannot be modified during gameplay.", EditorStyles.boldLabel);
                //return;
            }
            else
            {
                if (!loadInited)
                {
                    LoadInit();
                }

                DrawSettings();
                if (DatabaseEditor != null)
                {
                    DrawDatabase();
                }
                else
                {
                    EditorGUILayout.LabelField("No database loaded");
                }
            }
        }

        private void DrawSettings()
        {
            settingsFoldout = EditorGUILayout.Foldout(settingsFoldout, "Data Settings");
            if (settingsFoldout)
            {
                GUILayout.BeginHorizontal();

                /*
                const int maxDirLength = 28;
                if (GUILayout.Button (DatabasePath.Length > maxDirLength ? "..." + DatabasePath.Substring (DatabasePath.Length - maxDirLength) : DatabasePath, GUILayout.ExpandWidth (true))) {
                }*/

                SerializedObject obj = new SerializedObject(this);

                SerializedProperty databaseTypeProp = obj.FindProperty("_databaseType");
                EditorGUILayout.PropertyField(databaseTypeProp, new GUIContent("Database Type"));

                float settingsButtonWidth = 70f;
                if (GUILayout.Button("Load", GUILayout.MaxWidth(settingsButtonWidth)))
                {
                    DatabasePath = EditorUtility.OpenFilePanel("Database File", Application.dataPath, "asset");
                    if (!string.IsNullOrEmpty(DatabasePath))
                    {

                        LSFSettingsModifier.Save();
                        if (LoadDatabaseFromPath(DatabasePath) == false)
                        {
                            Debug.LogFormat("Database was not found at path of '{0}'.", DatabasePath);
                        }
                    }
                }
                if (GUILayout.Button("Create", GUILayout.MaxWidth(settingsButtonWidth)))
                {
                    DatabasePath = EditorUtility.SaveFilePanel("Database File", Application.dataPath, "NewDatabase", "asset");
                    if (!string.IsNullOrEmpty(DatabasePath))
                    {
                        if (CreateDatabase(DatabasePath))
                        {
                            Debug.Log("Database creation succesful!");
                        }
                        else
                        {
                            Debug.Log("Database creation unsuccesful");
                        }
                    }
                }
                GUILayout.EndHorizontal();

                //json stuff
                GUILayout.BeginHorizontal();
                jsonFile = (TextAsset)EditorGUILayout.ObjectField("Json", jsonFile, typeof(TextAsset), false);
                if (GUILayout.Button("Load", GUILayout.MaxWidth(settingsButtonWidth)))
                {
                    LSDatabaseManager.ApplyJson(jsonFile.text, Database);
                }
                if (GUILayout.Button("Save", GUILayout.MaxWidth(settingsButtonWidth)))
                {
                    System.IO.File.WriteAllText(
                        AssetDatabase.GetAssetPath(jsonFile),
                        LSDatabaseManager.ToJson(Database)
                    );
                }
                GUILayout.EndHorizontal();
                if (CanSave)
                {
                    obj.ApplyModifiedProperties();
                }
            }
        }

        private void DrawDatabase()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            DatabaseEditor.Draw();

            EditorGUILayout.EndScrollView();
        }

        private bool LoadDatabaseFromPath(string absolutePath)
        {
            string relativePath = absolutePath.GetRelativeUnityAssetPath();
            LSDatabase database = AssetDatabase.LoadAssetAtPath<LSDatabase>(relativePath);
            if (database != null)
            {
                LoadDatabase(database);
                return true;
            }
            DatabaseEditor = null;

            return false;
        }

        private void LoadDatabase(LSDatabase database)
        {
            _database = database;
            bool isValid = false;
            if (_database != null)
            {
                if (database.GetType() != DatabaseType)
                {
                    //Note: A hacky fix for changing the type of a previously saved database is to turn on Debug mode
                    //and change the script type of the database asset in the inspector. Back it up before attempting!
                    Debug.Log("Loaded database type does not match DatabaseType.");
                }
                DatabaseEditor = new EditorLSDatabase();
                DatabaseEditor.Initialize(this, Database, out isValid);
            }
            if (!isValid)
            {
                Debug.Log("Load unsuccesful");
                this.DatabaseEditor = null;
                this._database = null;
                IsLoaded = false;
                return;
            }
            LSFSettingsManager.GetSettings().Database = database;
            LSFSettingsModifier.Save();
            IsLoaded = true;
        }

        private bool CreateDatabase(string absolutePath)
        {
            LSDatabase database = (LSDatabase)ScriptableObject.CreateInstance(DatabaseType);
            if (database == null)
            {
                return false;
            }

            string relativePath = absolutePath.GetRelativeUnityAssetPath();

            AssetDatabase.CreateAsset(database, relativePath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LoadDatabase(database);

            return true;
        }

        private void Save()
        {
            if (Application.isPlaying)
            {
                return;
            }

            DatabaseEditor.Save();
            EditorUtility.SetDirty(DatabaseEditor.Database);
            AssetDatabase.SaveAssets();
        }
    }
}
