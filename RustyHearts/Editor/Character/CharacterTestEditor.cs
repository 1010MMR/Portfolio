using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;

public class CharacterTestEditor : EditorWindow {
    public static CharacterTestEditor Instance = null;
    public Character targetCharacter_;

    //Model model_;
    //    int selectedLayer_ = 0;

    //Animator targetAnimator_;
    //AnimatorController targetController_;
    //StateMachine targetStateMachine_;
    //State oldState_;
    //State targetState_;

    [MenuItem("GameAssist/CharacterTest")]
    static void Init() {
        EditorWindow.GetWindow<CharacterTestEditor>();
    }

    void OnEnable() {
        minSize = new Vector2(600, 320);
        Instance = this;
    }

    void OnDisable() {
    }

    Character oldCharacter_ = null;
    System.Guid testCharacterUID_;
    int selectedCharacterID_ = 0;
    float buffDebuffDuration_ = 5;
    float buffDebuffValue_ = 5;
    string[] characterNames_ = new string[1];
    int[] characterIDs_ = new int[1];
    string[] filterCharacterNames_ = new string[1];
    int[] filterCharacterIDs_ = new int[1];

    string filter_ = "";

    void OnGUI() {
        if( CharacterManager.Instance == null )
            return;
        if( CharacterTableManager.Instance.IsLoadComplate == false )
            return;

        var characterTableList = CharacterTableManager.Instance.Table;
        if( characterNames_ == null || characterNames_.Length != characterTableList.Count ) {
            characterNames_ = new string[characterTableList.Count];
            characterIDs_ = new int[characterTableList.Count];

            int i = 0;
            foreach( var table in characterTableList.Values ) {
                characterNames_[i] = table.id.ToString();
                characterIDs_[i] = table.id;
                ++i;
            }
        }

        string oldFilter = filter_;
        filter_ = EditorGUILayout.TextField("filter:", filter_);
        if( filter_ == "" ) {
            filterCharacterNames_ = characterNames_;
            filterCharacterIDs_ = characterIDs_;
        }
        else {
            if( oldFilter != filter_ ) {
                int i = 0;
                selectedCharacterID_ = 0;
                List<string> filterCN = new List<string>();
                List<int> filterCIDs = new List<int>();
                foreach( var cn in characterNames_ ) {
                    if( cn.Contains(filter_) == true ) {
                        filterCN.Add(cn);
                        filterCIDs.Add(characterIDs_[i]);
                    }
                    ++i;
                }
                filterCharacterNames_ = filterCN.ToArray();
                filterCharacterIDs_ = filterCIDs.ToArray();
            }
        }

        int oldSelectedCharacterID = selectedCharacterID_;
        selectedCharacterID_ = EditorGUILayout.Popup("Select CharacterID:", selectedCharacterID_, filterCharacterNames_);
        if( CharacterManager.Instance.ActivePlayerUID == System.Guid.Empty || oldSelectedCharacterID != selectedCharacterID_ ) {
            if( testCharacterUID_ != System.Guid.Empty ) {
                CharacterManager.Instance.Remove(testCharacterUID_);
            }

            int characterID = filterCharacterIDs_[selectedCharacterID_];
            testCharacterUID_ = System.Guid.NewGuid();
            CharacterManager.Instance.ActivePlayerUID = testCharacterUID_;
            CharacterManager.Instance.AddFromTable(characterID, testCharacterUID_, "Test", CharacterTeam.Type_Player);
        }

        if( CharacterManager.Instance.ActivePlayer == null )
            return;
        if( CharacterManager.Instance.ActivePlayer.UID != testCharacterUID_ ) {
            return;
        }

        targetCharacter_ = CharacterManager.Instance.ActivePlayer;
        if( targetCharacter_ == null )
            return;

        Model model = targetCharacter_.gameObject.GetComponent<Model>();
        if( model != null ) {
            float scale = EditorGUILayout.FloatField("Scale:", model.scale);
            if( scale != model.scale ) {
                model.scale = scale;
            }

            float speed = EditorGUILayout.FloatField("Speed:", model.speed);
            if( speed != model.speed ) {
                model.speed = speed;
            }
        }

        if( targetCharacter_ != oldCharacter_ ) {
            targetCharacter_.transform.position = Vector3.zero;
            targetCharacter_.transform.localRotation = Quaternion.identity;
            selectedComboIndex_ = 0;
        }

        oldCharacter_ = targetCharacter_;

        EditorGUILayout.BeginHorizontal();
        testState_ = (TestState)EditorGUILayout.EnumPopup("Select Test:", testState_);
        switch( testState_ ) {
            case TestState.Combo: {
                    if( targetCharacter_.comboTable == null )
                        break;
                    var combotable = targetCharacter_.comboTable.combotable;
                    string[] comboids = new string[combotable.Count];
                    int i = 0;
                    foreach( var id in combotable.Keys ) {
                        comboids[i] = id.ToString();
                        ++i;
                    }
                    selectedComboIndex_ = EditorGUILayout.Popup(selectedComboIndex_, comboids);
                    selectedComboID_ = System.Convert.ToInt32(comboids[selectedComboIndex_]);
                }
                break;
        }

        if( GUILayout.Button("Test State") ) {
            targetCharacter_.transform.position = Vector3.zero;
            targetCharacter_.transform.localRotation = Quaternion.identity;

            TestCharacterState(testState_);
        }
        EditorGUILayout.EndHorizontal();

        switch( testState_ ) {
            case TestState.Combo: {
                    List<SkillTableManager.CTable> skillTable = targetCharacter_.skillTable;
                    if( skillTable == null )
                        break;

                    EditorGUILayout.BeginHorizontal();
                    foreach( var stable in skillTable ) {
                        if( stable == null )
                            continue;

                        if( GUILayout.Button(stable.id.ToString()) ) {
                            targetCharacter_.transform.position = Vector3.zero;
                            targetCharacter_.transform.localRotation = Quaternion.identity;

                            Magi.CharacterComboTable.CTable skillCombo;
                            if( targetCharacter_.GetComboSkill(stable.id, out skillCombo) == false )
                                break;

                            targetCharacter_.SetStateNone();
                            targetCharacter_.TestCombo(skillCombo.id);
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                break;
        }

        EditorGUILayout.Separator();
        EditorGUILayout.BeginHorizontal();
        buffDebuffDuration_ = EditorGUILayout.FloatField("Duration:", buffDebuffDuration_);
        buffDebuffValue_ = EditorGUILayout.FloatField("Value:", buffDebuffValue_);
        EditorGUILayout.EndHorizontal();
    }

    void Update() {
        Process();

        if( targetCharacter_ != null ) {
            //            RaycastHit besthitinfo;
            //            if (UICamera.Raycast(Input.mousePosition, out besthitinfo) == false)
            if( UICamera.Raycast(Input.mousePosition) == false ) {
                if( Input.GetButton("Fire1") == true ) {
                    PlayerController pc = targetCharacter_.GetComponent<PlayerController>();
                    pc.enabled = true;
                    pc.Click2Go();
                }
            }
        }
    }

    public enum TestState {
        Respawn,
        Stay,
        Damage,
        Death,
        BackStep,
        Guard,
        GuardBreak,
        LeftStep,
        RightStep,
        Jump,
        MidairDamage,
        Combo,
    };

    int selectedComboIndex_;
    int selectedComboID_;
    TestState testState_;
    void TestCharacterState(TestState state) {
        switch( state ) {
            case TestState.Respawn: {
                    targetCharacter_.SetStateNone();
                    targetCharacter_.SetState(Character.State.Respawn);
                }
                break;
            case TestState.Stay: {
                    targetCharacter_.SetStateNone();
                    targetCharacter_.SetStateStay();
                }
                break;
            case TestState.Damage: {
                    targetCharacter_.SetStateNone();
                    // targetCharacter_.DamageTest();
                }
                break;
            case TestState.Death: {
                    targetCharacter_.SetStateNone();
                    targetCharacter_.SetState(Character.State.Death, 1, 0);
                }
                break;
            case TestState.Jump: {
                    targetCharacter_.SetStateNone();
                }
                break;
            case TestState.MidairDamage: {
                    targetCharacter_.SetStateNone();
                    targetCharacter_.MidairDamageTest();
                }
                break;
            case TestState.Combo: {
                    targetCharacter_.SetStateNone();
                    targetCharacter_.TestCombo(selectedComboID_);
                }
                break;
        }
    }

    //    float delayAttack_ = 0;
    void ComboTestProcess() {
        if( targetCharacter_ == null )
            return;
        //        if (Time.time - delayAttack_ > 0.1f)
        //        {
        //            delayAttack_ = Time.time;
        targetCharacter_.TestComboAttack();
        //        }
    }

    void Process() {
        switch( testState_ ) {
            case TestState.Damage: {
                }
                break;
            case TestState.Jump: {
                }
                break;
            case TestState.MidairDamage: {
                }
                break;
            case TestState.Combo: {
                    ComboTestProcess();
                }
                break;
        }
    }
}
