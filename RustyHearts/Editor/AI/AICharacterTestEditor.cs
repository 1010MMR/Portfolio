using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class AICharacterTestEditor : EditorWindow
{
    public static AICharacterTestEditor Instance = null;

    [MenuItem("GameAssist/AICharacterTest")]
    static void Init()
    {
        EditorWindow.GetWindow<AICharacterTestEditor>();
    }

    void OnEnable()
    {
        minSize = new Vector2(600, 320);
        Instance = this;
    }

    void OnDisable()
    {
    }

    bool playerControl_ = true;
    bool aiEnemyDrawText_ = true;
    int selectedCharacterIndex_;
    void OnGUI()
    {
        if (CharacterManager.Instance == null)
            return;
        if( CharacterTableManager.Instance.IsLoadComplate == false )
            return;

        var characterTableList = CharacterTableManager.Instance.Table;
        string[] characterNames = new string[characterTableList.Count];
        int[] characterIDs = new int[characterTableList.Count];

        int i = 0;
        foreach (var table in characterTableList.Values)
        {
            characterNames[i] = table.id.ToString();
            characterIDs[i] = table.id;
            ++i;
        }
//        int oldSelectedCharacterIndex = selectedCharacterIndex_;

        EditorGUILayout.BeginHorizontal();
        selectedCharacterIndex_ = EditorGUILayout.Popup("Select CharacterID:", selectedCharacterIndex_, characterNames);
        if (GUILayout.Button("Create") == true)
        {
            CharacterManager.Instance.AddFromTable(characterIDs[selectedCharacterIndex_], System.Guid.Empty, characterIDs[selectedCharacterIndex_].ToString(), CharacterTeam.Type_Enemy);
        }
        EditorGUILayout.EndHorizontal();

        bool playerControl = EditorGUILayout.Toggle("PlayerControl", playerControl_);
        if (playerControl != playerControl_)
        {
            PlayerController pc = CharacterManager.Instance.ActivePlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.enabled = playerControl;
            }
        }
        playerControl_ = playerControl;

        bool aiEnemyDrawText = EditorGUILayout.Toggle("AIEnemyDrawText", aiEnemyDrawText_);
        if (aiEnemyDrawText != aiEnemyDrawText_)
        {
        }
    }
}
