using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections.Generic;
using System.IO;
using Magi;

public class ModelEditor : EditorWindow {
    public static ModelEditor Instance = null;
    public static ModelInspector eventInspector_;

    public float PlaybackTime {
        get {
            return playbackTime_;
        }
    }

    ModelEditor() {
        Instance = this;
    }

    private Model targetModel_;
    private ModelTemplate targetModelTemplate_;
    private Animator targetAnimator_;
    private AnimatorController targetController_;
    private StateMachine targetStateMachine_;
    private State oldState_;
    private State targetState_;
    private int selectedLayer_ = 0;
    private int selectedState_ = 0;
    private bool showEventName_ = true;

    private List<MagiMecanimEvent> displayEvents_;

    int dummy_ = 0;
    MagiMecanimEvent mecanimEvent_ = new MagiMecanimEvent();
    MagiMecanimEvent selectMecanimEvent_;
    MagiMecanimEvent copyMecanimEvent_;
    int eventDataType_ = 0;

    GameObject debugHitCapsule_;
    GameObject debugHitCube_;
    GameObject debugHitSphere_;
    GameObject debugNodePath_;

    private MagiReorderableListWrapper conditionList_;
    private KeyValuePair<string, MagiMecanimEventCondition.EParamTypes>[] availableParameters_;

    private MagiReorderableListWrapper functionCallList_;

    //    [MenuItem("GameAssist/Model Editor")]
    static void Init() {
        EditorWindow.GetWindow<MagiMecanimEventEditor>();
    }

    void OnEnable() {
        minSize = new Vector2(600, 320);
        Instance = this;

        debugHitCapsule_ = Resources.LoadAssetAtPath("Assets/Magi/Magi3D/Editor/Prefabs/DebugHitCapsule.prefab", typeof(GameObject)) as GameObject;
        debugHitCube_ = Resources.LoadAssetAtPath("Assets/Magi/Magi3D/Editor/Prefabs/DebugHitCube.prefab", typeof(GameObject)) as GameObject;
        debugHitSphere_ = Resources.LoadAssetAtPath("Assets/Magi/Magi3D/Editor/Prefabs/DebugHitSphere.prefab", typeof(GameObject)) as GameObject;
        debugNodePath_ = Resources.LoadAssetAtPath("Assets/Magi/Magi3D/Editor/Prefabs/DebugNodePath.prefab", typeof(GameObject)) as GameObject;
    }

    void OnDisable() {
        RestoreBackupDefaultState();

        SetPreviewMotion(null);
        //            eventInspector_.SaveData();
        if( eventInspector_ != null ) {
            eventInspector_.Model.Editor_On = false;
        }
    }

    void OnInspectorUpdate() {
        Repaint();
    }

    public void DelEvent(MagiMecanimEvent e) {
        if( displayEvents_ != null ) {
            displayEvents_.Remove(e);
            SaveState();
        }
    }

    void Reset() {
        displayEvents_ = null;
        targetAnimator_ = null;
        targetController_ = null;
        targetStateMachine_ = null;
        targetState_ = null;
        oldState_ = null;
        selectedLayer_ = 0;
        selectedState_ = 0;
    }

    private void SaveState() {
        //targetModelTemplate_.SetEvents(selectedLayer_, targetState_.uniqueNameHash, displayEvents_.ToArray());
        targetModelTemplate_.SetEvents(selectedLayer_, targetState_.uniqueNameHash, displayEvents_.ToArray());
    }

    void OnGUI() {
        var oldModel = targetModel_;

        //        EditorGUILayout.BeginHorizontal();
        //        EditorGUILayout.LabelField("Put an Model here");
        targetModel_ = EditorGUILayout.ObjectField("Put an Model here", targetModel_, typeof(Model), true) as Model;
        //        EditorGUILayout.EndHorizontal();

        bool changeModel = false;
        //모델이 바뀌면 로딩한다.
        if( targetModel_ != oldModel ) {
            if( targetModel_.mdataFileName_ == "" ) {
                string assetPath = AssetDatabase.GetAssetPath(targetModel_.gameObject).ToLower();
                if( assetPath.Contains("assets/prefabs/") == true ) {
                    string temp = "assets/prefabs/";
                    string dir = Path.GetDirectoryName(assetPath) + "/";
                    assetPath = dir.Substring(temp.Length) + Path.GetFileNameWithoutExtension(assetPath) + "_template";
                    if( EditorModelTemplate.LoadModel(assetPath.ToLower(), targetModel_) == true ) {
                        targetModelTemplate_ = targetModel_.modelTemplate_;
                    }
                }
                else {
                    targetModel_.mdataFileName_ = "";
                }
            }
            changeModel = true;
        }

        if( targetModel_ != null ) {
            targetAnimator_ = targetModel_.animator_;
        }

        if( targetAnimator_ != null ) {
            AnimatorController animatorController = AnimatorController.GetEffectiveAnimatorController((Animator)targetAnimator_);
            targetController_ = animatorController;
            if( changeModel == true ) {
                availableParameters_ = GetConditionParameters();
            }
        }
        else {
            targetController_ = null;
        }

        if( targetModel_ == null )
            return;

        targetModelTemplate_ = targetModel_.modelTemplate_;
        if( targetAnimator_ == null )
            return;

        if( targetController_ == null )
            return;

        if( targetModelTemplate_ == null || targetModel_.mdataFileName_ == "" ) {
            string assetPath = AssetDatabase.GetAssetPath(targetModel_.gameObject).ToLower();
            if( assetPath.Contains("assets/prefabs/") == true ) {
                string temp = "assets/prefabs/";
                if( targetModel_.mdataFileName_ == "" ) {
                    string dir = Path.GetDirectoryName(assetPath) + "/";
                    assetPath = dir.Substring(temp.Length) + Path.GetFileNameWithoutExtension(assetPath) + "_template";
                }
                else {
                    assetPath = targetModel_.mdataFileName_;
                }
                if( EditorModelTemplate.LoadModel(assetPath.ToLower(), targetModel_) == true ) {
                    targetModelTemplate_ = targetModel_.modelTemplate_;
                }
            }
            else {
                targetModel_.mdataFileName_ = "";
            }
        }

        if( targetModelTemplate_ == null )
            return;

        RemoveNotification();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Model Data:");
        EditorGUILayout.LabelField(targetModel_.mdataFileName_);
        EditorGUILayout.EndHorizontal();

        int layerCount = targetController_.layerCount;
        string[] layerNames = new string[layerCount];
        for( int layer = 0; layer < layerCount; layer++ ) {
            var acLayer = targetController_.GetLayer(layer);
            layerNames[layer] = "[" + layer.ToString() + "]" + acLayer.name;
        }

        selectedLayer_ = Mathf.Clamp(selectedLayer_, 0, layerCount - 1);
        selectedLayer_ = GUILayout.Toolbar(selectedLayer_, layerNames);

        var animatorLayer2 = targetController_.GetLayer(selectedLayer_);
        targetStateMachine_ = animatorLayer2.stateMachine;

        List<string> stateNames = new List<string>();
        List<string> stateNames2 = new List<string>();
        List<State> availabeStates = new List<State>();

        for( int i = 0; i < targetStateMachine_.stateCount; ++i ) {
            var s = targetStateMachine_.GetState(i);
            if( s.name == "preview_test" || s.name.ToLower() == "delete" )
                continue;
            stateNames2.Add(s.uniqueName);
            stateNames.Add(s.name);
            availabeStates.Add(s);
        }
        for( int n = 0; n < targetStateMachine_.stateMachineCount; ++n ) {
            var stateMachine = targetStateMachine_.GetStateMachine(n);
            for( int i = 0; i < stateMachine.stateCount; ++i ) {
                var s = stateMachine.GetState(i);
                if( s.name == "preview_test" || s.name.ToLower() == "delete" )
                    continue;
                stateNames2.Add(s.uniqueName);
                stateNames.Add(s.name);
                availabeStates.Add(s);
            }
        }

        if( availabeStates.Count == 0 ) {
            EditorGUILayout.LabelField("No state available in this layer.");
            return;
        }

        oldState_ = targetState_;
        selectedState_ = Mathf.Clamp(selectedState_, 0, availabeStates.Count - 1);
        selectedState_ = GUILayout.SelectionGrid(selectedState_, stateNames.ToArray(), 4);
        targetState_ = availabeStates[selectedState_];
        if( oldState_ != targetState_ ) {
            ChangeState();
        }

        if( eventInspector_ != null ) {
            eventInspector_.Model.modelTemplate_ = targetModelTemplate_;
            eventInspector_.Model.Editor_SetStateInfo(targetState_.uniqueNameHash, PlaybackNormalizedTime);

            playbackCurrentTime_ = eventInspector_.PlaybackCurrentTime;
            playbackEndTime_ = Mathf.Max(eventInspector_.PlaybackEndTime, 1.0f);
            playbackNormalizedTime_ = 0;

            //var character = eventInspector_.Model.gameObject.GetComponent<Character>();
            //if (character != null)
            //{
            //    character.enabled = false;
            //}

            var aicharacter = eventInspector_.Model.gameObject.GetComponent<AICharacter>();
            if( aicharacter != null ) {
                aicharacter.enabled = false;
            }
            var playerController = eventInspector_.Model.gameObject.GetComponent<PlayerController>();
            if( playerController != null ) {
                playerController.enabled = false;
            }
        }

        OnEventGUI();

        SaveState();
    }

    void Update() {
        if( editEffectGameObject_ != null ) {
            if( IsAliveAnimationEffect() == false ) {
                CreateCurrentInstanceEffect(editEffectGameObject_);
            }
        }

        if( targetModel_ == null )
            return;

        targetModelTemplate_ = targetModel_.modelTemplate_;
        if( targetAnimator_ == null )
            return;

        if( targetController_ == null )
            return;

        if( targetModelTemplate_ == null )
            return;

        if( eventInspector_ != null ) {
            eventInspector_.Model.Editor_On = true;
            if( EditorApplication.isPlaying ) {
                eventInspector_.Model.Process();
            }
        }
    }

    void DrawGridMecanimEvent() {
        MagiMecanimEvent.EDataType selectDataType = (MagiMecanimEvent.EDataType)eventDataType_;

        var sortEvents = new List<MagiMecanimEvent>();
        for( int i = 0; i < displayEvents_.Count; ++i ) {
            if( displayEvents_[i].DataType != selectDataType )
                continue;
            sortEvents.Add(displayEvents_[i]);
        }
        sortEvents.Sort(SortList);

        int selectedIndex = 0;
        var sortEventsName = new List<string>();
        sortEventsName.Add("Unselect");
        for( int i = 0; i < sortEvents.Count; ++i ) {
            float currentFrame = sortEvents[i].normalizedTime_ * PlaybackEndTime * 30.0f;
            sortEventsName.Add(currentFrame.ToString());

            if( selectMecanimEvent_ == sortEvents[i] ) {
                selectedIndex = i + 1;
            }
        }

        selectedIndex = GUILayout.SelectionGrid(selectedIndex, sortEventsName.ToArray(), 6);
        if( selectedIndex != 0 ) {
            selectMecanimEvent_ = sortEvents[selectedIndex - 1];
        }
        else {
            selectMecanimEvent_ = null;
        }
    }

    MagiMecanimEvent oldMecanimEvent_ = null;
    MagiMecanimEvent currentMecanimEvent_ = null;
    private void OnEventGUI() {
        currentMecanimEvent_ = mecanimEvent_;
        if( selectMecanimEvent_ != null ) {
            currentMecanimEvent_ = selectMecanimEvent_;
        }

        if( oldMecanimEvent_ != currentMecanimEvent_ ) {
            conditionList_ = new MagiReorderableListWrapper(currentMecanimEvent_.condition_.conditions, typeof(MagiMecanimEventCondition.Entry));
            conditionList_.drawElementCallback = new MagiReorderableListWrapper.ElementCallbackDelegate(DrawConditionsElement);
            conditionList_.drawHeaderCallback = new MagiReorderableListWrapper.HeaderCallbackDelegate(DrawConditionsHeader);

            functionCallList_ = new MagiReorderableListWrapper(currentMecanimEvent_.functionCall_.fcParams_, typeof(MagiMecanimEventCondition.Entry));
            functionCallList_.drawElementCallback = new MagiReorderableListWrapper.ElementCallbackDelegate(DrawFunctionCallParamsElement);
            functionCallList_.drawHeaderCallback = new MagiReorderableListWrapper.HeaderCallbackDelegate(DrawFunctionCallParamsHeader);
        }

        oldMecanimEvent_ = currentMecanimEvent_;
        string[] toolbarDataStrings = new string[] { "HitData", "Effect", "FunctionCall", "CameraShake", "CameraMove" };

        if( selectMecanimEvent_ != null ) {
            eventDataType_ = (int)selectMecanimEvent_.DataType;
            GUILayout.Toolbar(eventDataType_, toolbarDataStrings);
        }
        else {
            eventDataType_ = GUILayout.Toolbar(eventDataType_, toolbarDataStrings);
        }

        float currentFrame = currentMecanimEvent_.normalizedTime_ * PlaybackEndTime * 30.0f;
        currentFrame = EditorGUILayout.FloatField("Frame:", currentFrame);
        EditorGUILayout.FloatField("NormalizedTime:", currentMecanimEvent_.normalizedTime_);

        currentMecanimEvent_.dataType_ = (MagiMecanimEvent.EDataType)eventDataType_;
        currentMecanimEvent_.normalizedTime_ = Mathf.Clamp(currentFrame / (PlaybackEndTime * 30.0f), 0.0f, 1.0f);
        currentMecanimEvent_.motionCategory_ = (MagiMecanimEvent.EMotionCategory)EditorGUILayout.EnumPopup("Category:", currentMecanimEvent_.motionCategory_);

        if( availableParameters_ != null && availableParameters_.Length > 0 ) {
            conditionList_.DoLayoutList();
        }
        else {
            currentMecanimEvent_.condition_.conditions.Clear();
        }

        switch( currentMecanimEvent_.dataType_ ) {
            case MagiMecanimEvent.EDataType.HitData: {
                    OnHitDataGUI(currentMecanimEvent_);
                }
                break;
            case MagiMecanimEvent.EDataType.Effect: {
                    OnEffectGUI(currentMecanimEvent_);
                }
                break;
            case MagiMecanimEvent.EDataType.FunctionCall: {
                    OnFunctionCall(currentMecanimEvent_);
                }
                break;
            case MagiMecanimEvent.EDataType.CameraShake: {
                    OnCameraShake(currentMecanimEvent_);
                }
                break;
            case MagiMecanimEvent.EDataType.CameraMove: {
                    OnCameraMove(currentMecanimEvent_);
                }
                break;
        }

        SetStateMechine(targetController_, selectedLayer_);
        var layer = targetController_.GetLayer(0);
        if( targetState_.GetMotion(layer) != null ) {
            SetPreviewMotion(targetState_.GetMotion(layer));

            displayEvents_ = new List<MagiMecanimEvent>(targetModelTemplate_.GetEvents(selectedLayer_, targetState_.uniqueNameHash));

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            DrawGridMecanimEvent();

            BeginWindows();
            GUI.Window(0, new Rect(0, position.height - 170, position.width, 170), TimelineWindow, targetState_.name);
            EndWindows();
        }
        else {
            SetPreviewMotion(null);
        }
    }

    private void OnHitDataGUI(MagiMecanimEvent currentMecanimEvent) {
        //        GUILayout.BeginArea(new Rect(0, 150, position.width, position.height - 150), "");
        currentMecanimEvent.HitData.attachParent = EditorGUILayout.Toggle("Attach Parent:", currentMecanimEvent_.HitData.attachParent);
        currentMecanimEvent.HitData.duration = EditorGUILayout.FloatField("Duration Time:", currentMecanimEvent_.HitData.duration);
        currentMecanimEvent.HitData.downHit = EditorGUILayout.Toggle("Down Hit", currentMecanimEvent.HitData.downHit);
        currentMecanimEvent.HitData.jumpDownHit = EditorGUILayout.Toggle("Jump Down Hit", currentMecanimEvent.HitData.jumpDownHit);
        currentMecanimEvent.HitData.additionHitID = EditorGUILayout.Toggle("addition HitID", currentMecanimEvent.HitData.additionHitID);
        currentMecanimEvent.HitData.damageWeight = EditorGUILayout.FloatField("Damage Weight:", currentMecanimEvent.HitData.damageWeight);
        currentMecanimEvent.HitData.hitAttribute = EditorGUILayout.TextField("Hit Attribute:", currentMecanimEvent.HitData.hitAttribute);

        currentMecanimEvent.HitData.hitCount = EditorGUILayout.IntField("Hit Count:", currentMecanimEvent.HitData.hitCount);
        currentMecanimEvent.HitData.hitInterval = EditorGUILayout.FloatField("Hit Interval:", currentMecanimEvent.HitData.hitInterval);

        string[] toolbarStrings = new string[] { "Sphere", "Capsule", "Box", "Weapon" };
        currentMecanimEvent.HitData.paramType = (MagiMecanimEventHit.EMagiHitType)GUILayout.Toolbar((int)currentMecanimEvent.HitData.paramType, toolbarStrings);

        string[] targetDummies = ModelInspector.GetDummyNames(targetModel_);
        List<string> dummies = new List<string>();
        dummies.Add("Center");

        if( targetDummies != null && targetDummies.Length != 0 ) {
            dummies.AddRange(targetDummies);
        }

        dummy_ = dummies.IndexOf(currentMecanimEvent.HitData.dummy);
        if( dummy_ < 0 )
            dummy_ = 0;
        dummy_ = EditorGUILayout.Popup("Dummy:", dummy_, dummies.ToArray());
        if( dummy_ == 0 ) {
            currentMecanimEvent.HitData.dummy = "";
        }
        else {
            currentMecanimEvent.HitData.dummy = dummies[dummy_];
        }
        switch( currentMecanimEvent.HitData.paramType ) {
            case MagiMecanimEventHit.EMagiHitType.Sphere: {
                    if( currentMecanimEvent.HitData.sphereParam == null )
                        currentMecanimEvent.HitData.sphereParam = new MagiMecanimEventHit.MagiSphere();

                    currentMecanimEvent.HitData.sphereParam.euler = EditorGUILayout.Vector3Field("Euler:", currentMecanimEvent.HitData.sphereParam.euler);
                    currentMecanimEvent.HitData.sphereParam.center = EditorGUILayout.Vector3Field("Center:", currentMecanimEvent.HitData.sphereParam.center);
                    currentMecanimEvent.HitData.sphereParam.radius = EditorGUILayout.FloatField("Radius:", currentMecanimEvent.HitData.sphereParam.radius);
                    currentMecanimEvent.HitData.sphereParam.push = EditorGUILayout.FloatField("Push:", currentMecanimEvent.HitData.sphereParam.push);
                    currentMecanimEvent.HitData.sphereParam.pushDuration = EditorGUILayout.FloatField("PushDuration:", currentMecanimEvent.HitData.sphereParam.pushDuration);
                }
                break;
            case MagiMecanimEventHit.EMagiHitType.Capsule: {
                    if( currentMecanimEvent.HitData.capsuleParam == null )
                        currentMecanimEvent.HitData.capsuleParam = new MagiMecanimEventHit.MagiCapsule();

                    currentMecanimEvent.HitData.capsuleParam.euler = EditorGUILayout.Vector3Field("Euler:", currentMecanimEvent.HitData.capsuleParam.euler);
                    currentMecanimEvent.HitData.capsuleParam.center = EditorGUILayout.Vector3Field("Center:", currentMecanimEvent.HitData.capsuleParam.center);
                    currentMecanimEvent.HitData.capsuleParam.radius = EditorGUILayout.FloatField("Radius:", currentMecanimEvent.HitData.capsuleParam.radius);
                    currentMecanimEvent.HitData.capsuleParam.height = EditorGUILayout.FloatField("Height:", currentMecanimEvent.HitData.capsuleParam.height);
                    currentMecanimEvent.HitData.capsuleParam.direction = (MagiMecanimEventHit.MagiCapsule.EDirection)EditorGUILayout.EnumPopup("Direction", currentMecanimEvent.HitData.capsuleParam.direction);
                    currentMecanimEvent.HitData.capsuleParam.push = EditorGUILayout.FloatField("Push:", currentMecanimEvent.HitData.capsuleParam.push);
                    currentMecanimEvent.HitData.capsuleParam.pushDuration = EditorGUILayout.FloatField("PushDuration:", currentMecanimEvent.HitData.capsuleParam.pushDuration);
                }
                break;
            case MagiMecanimEventHit.EMagiHitType.Box: {
                    if( currentMecanimEvent.HitData.boxParam == null )
                        currentMecanimEvent.HitData.boxParam = new MagiMecanimEventHit.MagiBox();

                    currentMecanimEvent.HitData.boxParam.euler = EditorGUILayout.Vector3Field("Euler:", currentMecanimEvent.HitData.boxParam.euler);
                    currentMecanimEvent.HitData.boxParam.center = EditorGUILayout.Vector3Field("Center:", currentMecanimEvent.HitData.boxParam.center);
                    currentMecanimEvent.HitData.boxParam.size = EditorGUILayout.Vector3Field("Size:", currentMecanimEvent.HitData.boxParam.size);
                    currentMecanimEvent.HitData.boxParam.push = EditorGUILayout.FloatField("Push:", currentMecanimEvent.HitData.boxParam.push);
                    currentMecanimEvent.HitData.boxParam.pushDuration = EditorGUILayout.FloatField("PushDuration:", currentMecanimEvent.HitData.boxParam.pushDuration);
                }
                break;
            case MagiMecanimEventHit.EMagiHitType.Weapon: {
                    if( currentMecanimEvent.HitData.weaponParam == null )
                        currentMecanimEvent.HitData.weaponParam = new MagiMecanimEventHit.MagiWeapon();
                }
                break;
        }
        EditorGUILayout.Space();
    }

    private void OnEffectGUI(MagiMecanimEvent currentMecanimEvent) {
        string[] dummyNames = ModelInspector.GetDummyNames(targetModel_);
        if( dummyNames != null ) {
            targetModel_.InitDummy();
        }

        List<string> dummies = new List<string>();
        dummies.Add("Center");
        if( dummyNames != null && dummyNames.Length != 0 ) {
            dummies.AddRange(dummyNames);
        }

        int selectDummy = 0;
        for( int i = 0; i < dummies.Count; ++i ) {
            if( dummies[i] == currentMecanimEvent.effectData_.dummy_ ) {
                selectDummy = i;
            }
        }

        selectDummy = EditorGUILayout.Popup(string.Format("Dummy:", dummies.ToArray().Length), selectDummy, dummies.ToArray());

        GameObject oldeffectObject = currentMecanimEvent.effectData_.effectObject_;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Effect Data:");
        currentMecanimEvent.effectData_.effectObject_ = EditorGUILayout.ObjectField(currentMecanimEvent.effectData_.effectObject_, typeof(GameObject), false) as GameObject;
        EditorGUILayout.EndHorizontal();

        if( selectDummy == 0 ) {
            currentMecanimEvent.effectData_.dummy_ = "";
        }
        else {
            currentMecanimEvent.effectData_.dummy_ = dummies[selectDummy];
        }
        currentMecanimEvent.effectData_.attachParent_ = EditorGUILayout.Toggle("Attach Parent:", currentMecanimEvent.effectData_.attachParent_);
        currentMecanimEvent.effectData_.point_ = EditorGUILayout.Vector3Field("Offset:", currentMecanimEvent.effectData_.point_);
        currentMecanimEvent.effectData_.euler_ = EditorGUILayout.Vector3Field("Euler:", currentMecanimEvent.effectData_.euler_);
        currentMecanimEvent.effectData_.scale_ = EditorGUILayout.Vector3Field("Scale:", currentMecanimEvent.effectData_.scale_);
        currentMecanimEvent.effectData_.effectHitEnable_ = EditorGUILayout.Toggle("Effect Hit Enable", currentMecanimEvent.effectData_.effectHitEnable_);

        if( currentMecanimEvent.effectData_.effectObject_ != oldeffectObject ) {
            string assetPath = AssetDatabase.GetAssetPath(currentMecanimEvent.effectData_.effectObject_).ToLower();
            if( assetPath.Contains("assets/prefabs/") == true ) {
                string temp = "assets/prefabs/";
                string dir = Path.GetDirectoryName(assetPath) + "/";
                assetPath = dir.Substring(temp.Length) + Path.GetFileNameWithoutExtension(assetPath);
                currentMecanimEvent.effectData_.effectObjectFileName_ = assetPath;
                MagiDebug.Log(string.Format("currentMecanimEvent.effectData_.effectObjectFileName_ = {0}", currentMecanimEvent.effectData_.effectObjectFileName_));
            }
            else {
                MagiDebug.LogError("assetPath.Contains(assets/prefabs/) == false");
                currentMecanimEvent.effectData_.effectObject_ = null;
                currentMecanimEvent.effectData_.effectObjectFileName_ = "";
            }
        }

        if( currentMecanimEvent.effectData_.effectObject_ != null && currentMecanimEvent.effectData_.effectObjectFileName_ == "" ) {
            string assetPath = AssetDatabase.GetAssetPath(currentMecanimEvent.effectData_.effectObject_).ToLower();
            if( assetPath.Contains("assets/prefabs/") == true ) {
                string temp = "assets/prefabs/";
                string dir = Path.GetDirectoryName(assetPath) + "/";
                assetPath = dir.Substring(temp.Length) + Path.GetFileNameWithoutExtension(assetPath);
                currentMecanimEvent.effectData_.effectObjectFileName_ = assetPath;
                MagiDebug.Log(string.Format("currentMecanimEvent.effectData_.effectObjectFileName_ = {0}", currentMecanimEvent.effectData_.effectObjectFileName_));
            }
            else {
                currentMecanimEvent.effectData_.effectObject_ = null;
                currentMecanimEvent.effectData_.effectObjectFileName_ = "";
            }
        }
        EditorGUILayout.Space();
    }

    //    private GUIContent[] booleanPopup = new GUIContent[] { new GUIContent("False"), new GUIContent("True") };
    private void OnFunctionCall(MagiMecanimEvent currentMecanimEvent) {
        currentMecanimEvent.functionCall_.functionName_ = EditorGUILayout.TextField("Function:", currentMecanimEvent.functionCall_.functionName_);
        functionCallList_.DoLayoutList();

        GUILayout.Space(10);
        GUILayout.FlexibleSpace();
    }

    private void OnCameraShake(MagiMecanimEvent currentMecanimEvent) {
        currentMecanimEvent.cameraShake_.shakeType = (CameraRumble.ShakeType)EditorGUILayout.EnumPopup("Shake type:", currentMecanimEvent.cameraShake_.shakeType);
        currentMecanimEvent.cameraShake_.numberOfShakes = EditorGUILayout.IntField("Number of shakes:", currentMecanimEvent.cameraShake_.numberOfShakes);
        currentMecanimEvent.cameraShake_.shakeAmount = EditorGUILayout.Vector3Field("Shake amount:", currentMecanimEvent.cameraShake_.shakeAmount);
        currentMecanimEvent.cameraShake_.distance = EditorGUILayout.FloatField("Distance:", currentMecanimEvent.cameraShake_.distance);
        currentMecanimEvent.cameraShake_.speed = EditorGUILayout.FloatField("Speed:", currentMecanimEvent.cameraShake_.speed);
        currentMecanimEvent.cameraShake_.decay = EditorGUILayout.FloatField("Decay:", currentMecanimEvent.cameraShake_.decay);
        currentMecanimEvent.cameraShake_.guiShakeModifier = EditorGUILayout.FloatField("Gui shake modifier:", currentMecanimEvent.cameraShake_.guiShakeModifier);
        currentMecanimEvent.cameraShake_.multiplyByTimeScale = EditorGUILayout.Toggle("Multiply by time scale:", currentMecanimEvent.cameraShake_.multiplyByTimeScale);

        if( GUILayout.Button("Reset") == true ) {
            currentMecanimEvent.cameraShake_ = new MagiMecanimEventCameraShake();
        }
    }

    MagiMecanimEvent.ECameraType nodeCategory = MagiMecanimEvent.ECameraType.linear;
    private void OnCameraMove(MagiMecanimEvent currentMecanimEvent) {
        currentMecanimEvent.cameraMove_.speed = EditorGUILayout.FloatField("Path Speed : ", currentMecanimEvent.cameraMove_.speed);
        nodeCategory = (MagiMecanimEvent.ECameraType)EditorGUILayout.EnumPopup("Node Type : ", nodeCategory);
        currentMecanimEvent.cameraMove_.nodeType = nodeCategory.ToString();

        currentMecanimEvent.cameraMove_.endPoint = EditorGUILayout.Toggle("End Point : ", currentMecanimEvent.cameraMove_.endPoint);
        currentMecanimEvent.cameraMove_.nodeCount = EditorGUILayout.IntField("Node Count : ", currentMecanimEvent.cameraMove_.nodeCount);

        for( int i = 0; i < currentMecanimEvent.cameraMove_.nodeList.Count; i++ ) {
            currentMecanimEvent.cameraMove_.nodeList[i] = EditorGUILayout.Vector3Field(string.Format("Node{0:D2}", i + 1), currentMecanimEvent.cameraMove_.nodeList[i]);
        }
    }

    private void DrawConditionsElement(Rect rect, int index, bool selected, bool focused) {
        if( currentMecanimEvent_.condition_.conditions.Count < 1 )
            return;
        var conditionAtIndex = currentMecanimEvent_.condition_.conditions[index];
        EditorGUIUtility.LookLikeControls();
        Rect paramRect = new Rect(rect.x, rect.y, rect.width / 3, rect.height);

        string[] paramPopup = new string[availableParameters_.Length];
        int paramSelected = 0;

        for( int i = 0; i < availableParameters_.Length; i++ ) {
            paramPopup[i] = availableParameters_[i].Key;

            if( paramPopup[i] == conditionAtIndex.conditionParam )
                paramSelected = i;
        }

        paramSelected = EditorGUI.Popup(paramRect, paramSelected, paramPopup);
        conditionAtIndex.conditionParam = paramPopup[paramSelected];

        switch( availableParameters_[paramSelected].Value ) {
            case MagiMecanimEventCondition.EParamTypes.Int: {
                    conditionAtIndex.conditionParamType = MagiMecanimEventCondition.EParamTypes.Int;
                    Rect modeRect = new Rect(rect.x + rect.width / 3, rect.y, rect.width / 3, rect.height);
                    Rect valueRect = new Rect(rect.x + rect.width * 2 / 3, rect.y, rect.width / 3, rect.height - 4);

                    conditionAtIndex.conditionMode = (MagiMecanimEventCondition.EModes)EditorGUI.EnumPopup(modeRect, conditionAtIndex.conditionMode);
                    conditionAtIndex.intValue = EditorGUI.IntField(valueRect, conditionAtIndex.intValue);
                }

                break;
            case MagiMecanimEventCondition.EParamTypes.Float: {
                    conditionAtIndex.conditionParamType = MagiMecanimEventCondition.EParamTypes.Float;
                    Rect modeRect = new Rect(rect.x + rect.width / 3, rect.y, rect.width / 3, rect.height);
                    Rect valueRect = new Rect(rect.x + rect.width * 2 / 3, rect.y, rect.width / 3, rect.height - 4);

                    string[] floatConditionMode = new string[] { MagiMecanimEventCondition.EModes.GreaterThan.ToString(), MagiMecanimEventCondition.EModes.LessThan.ToString() };
                    int selectMode = conditionAtIndex.conditionMode == MagiMecanimEventCondition.EModes.LessThan ? 1 : 0;
                    selectMode = EditorGUI.Popup(modeRect, selectMode, floatConditionMode);
                    conditionAtIndex.conditionMode = selectMode == 0 ? MagiMecanimEventCondition.EModes.GreaterThan : MagiMecanimEventCondition.EModes.LessThan;
                    conditionAtIndex.floatValue = EditorGUI.FloatField(valueRect, conditionAtIndex.floatValue);
                }
                break;
            case MagiMecanimEventCondition.EParamTypes.Boolean: {
                    conditionAtIndex.conditionParamType = MagiMecanimEventCondition.EParamTypes.Boolean;
                    Rect valueRect = new Rect(rect.x + rect.width / 3, rect.y, rect.width * 2 / 3, rect.height - 4);

                    string[] boolConditionValue = new string[] { "true", "false" };
                    conditionAtIndex.conditionMode = MagiMecanimEventCondition.EModes.Equal;
                    int selectedValue = conditionAtIndex.boolValue ? 0 : 1;
                    conditionAtIndex.boolValue = EditorGUI.Popup(valueRect, selectedValue, boolConditionValue) == 0 ? true : false;
                }
                break;
        }
    }

    private void DrawConditionsHeader(Rect headerRect) {
        EditorGUIUtility.LookLikeControls();
        GUI.Label(headerRect, new GUIContent("Conditions"));
    }

    private void DrawFunctionCallParamsElement(Rect rect, int index, bool selected, bool focused) {
        if( currentMecanimEvent_.functionCall_.fcParams_.Count < 1 )
            return;
        var functionCallParamsAtIndex = currentMecanimEvent_.functionCall_.fcParams_[index];
        EditorGUIUtility.LookLikeControls();
        Rect paramRect = new Rect(rect.x, rect.y, rect.width / 3, rect.height);

        functionCallParamsAtIndex.paramType = (MagiMecanimEventFunctionCallParam.EParamTypes)EditorGUI.EnumPopup(paramRect, functionCallParamsAtIndex.paramType);
        Rect valueRect = new Rect(rect.x + rect.width / 3 + 2, rect.y, rect.width * 2 / 3, rect.height - 4);
        switch( functionCallParamsAtIndex.paramType ) {
            case MagiMecanimEventFunctionCallParam.EParamTypes.Int32: {
                    functionCallParamsAtIndex.intParam = EditorGUI.IntField(valueRect, functionCallParamsAtIndex.intParam);
                }
                break;
            case MagiMecanimEventFunctionCallParam.EParamTypes.Float: {
                    functionCallParamsAtIndex.floatParam = EditorGUI.FloatField(valueRect, functionCallParamsAtIndex.floatParam);
                }
                break;
            case MagiMecanimEventFunctionCallParam.EParamTypes.String: {
                    functionCallParamsAtIndex.stringParam = EditorGUI.TextField(valueRect, functionCallParamsAtIndex.stringParam);
                }
                break;
            case MagiMecanimEventFunctionCallParam.EParamTypes.Boolean: {
                    string[] boolConditionValue = new string[] { "true", "false" };
                    int selectedValue = functionCallParamsAtIndex.boolParam ? 0 : 1;
                    functionCallParamsAtIndex.boolParam = EditorGUI.Popup(valueRect, selectedValue, boolConditionValue) == 0 ? true : false;
                }
                break;
        }
    }

    private void DrawFunctionCallParamsHeader(Rect headerRect) {
        EditorGUIUtility.LookLikeControls();
        GUI.Label(headerRect, new GUIContent("Params"));
    }

    private float playbackTime_ = 0.0f;
    private static int timelineHash_ = "timelinecontrol".GetHashCode();
    private static int eventKeyHash_ = "eventkeycontrol".GetHashCode();
    private void DragNewCollider(Collider newCollider) {
        MagiMecanimEvent mecanimEvent = null;
        if( selectMecanimEvent_ != null ) {
            mecanimEvent = selectMecanimEvent_;
        }
        else {
            mecanimEvent = new MagiMecanimEvent(mecanimEvent_);
            displayEvents_.Add(mecanimEvent);
        }
        Transform transform = newCollider.gameObject.transform;

        Vector3 modelScale = new Vector3(1.0f / targetModel_.transform.localScale.x, 1.0f / targetModel_.transform.localScale.y, 1.0f / targetModel_.transform.localScale.z);
        Vector3 localScale = Vector3.Scale(transform.localScale, modelScale);

        if( newCollider.GetType() == typeof(SphereCollider) ) {
            SphereCollider sphereCollider = newCollider as SphereCollider;
            mecanimEvent.HitData.paramType = MagiMecanimEventHit.EMagiHitType.Sphere;
            mecanimEvent.HitData.sphereParam = new MagiMecanimEventHit.MagiSphere();
            mecanimEvent.HitData.sphereParam.euler = transform.rotation.eulerAngles;
            mecanimEvent.HitData.sphereParam.center = targetModel_.transform.InverseTransformPoint(transform.position + sphereCollider.center);

            float scale = Mathf.Max(Mathf.Max(localScale.x, localScale.y), localScale.z);
            mecanimEvent.HitData.sphereParam.radius = scale * sphereCollider.radius;
        }
        if( newCollider.GetType() == typeof(CapsuleCollider) ) {
            CapsuleCollider capsuleCollider = newCollider as CapsuleCollider;
            mecanimEvent.HitData.paramType = MagiMecanimEventHit.EMagiHitType.Capsule;
            mecanimEvent.HitData.capsuleParam = new MagiMecanimEventHit.MagiCapsule();
            mecanimEvent.HitData.capsuleParam.euler = transform.rotation.eulerAngles;
            mecanimEvent.HitData.capsuleParam.center = targetModel_.transform.InverseTransformPoint(transform.position + capsuleCollider.center);

            float scale = Mathf.Max(localScale.x, localScale.z);
            mecanimEvent.HitData.capsuleParam.radius = capsuleCollider.radius * scale;
            mecanimEvent.HitData.capsuleParam.height = capsuleCollider.height * localScale.y;
            mecanimEvent.HitData.capsuleParam.direction = (MagiMecanimEventHit.MagiCapsule.EDirection)capsuleCollider.direction;
        }
        if( newCollider.GetType() == typeof(BoxCollider) ) {
            BoxCollider boxCollider = newCollider as BoxCollider;
            mecanimEvent.HitData.paramType = MagiMecanimEventHit.EMagiHitType.Box;
            mecanimEvent.HitData.boxParam = new MagiMecanimEventHit.MagiBox();
            mecanimEvent.HitData.boxParam.euler = transform.rotation.eulerAngles;
            mecanimEvent.HitData.boxParam.center = targetModel_.transform.InverseTransformPoint(transform.position + boxCollider.center);
            mecanimEvent.HitData.boxParam.size = Vector3.Scale(localScale, boxCollider.size);
        }
    }

    void DragNewNodePath(GameObject go) {
        MagiMecanimEvent mecanimEvent = null;
        if( selectMecanimEvent_ != null ) {
            mecanimEvent = selectMecanimEvent_;
        }
        else {
            mecanimEvent = new MagiMecanimEvent(mecanimEvent_);
            displayEvents_.Add(mecanimEvent);
        }

        if( go != null ) {
            iTweenPath itweenPath_ = go.GetComponent<iTweenPath>();

            if( itweenPath_ != null ) {
                mecanimEvent.cameraMove_.nodeCount = itweenPath_.nodeCount;
                mecanimEvent.cameraMove_.nodeList = itweenPath_.nodes;
            }

            else {
                List<Vector3> nodeVec_ = new List<Vector3>();
                nodeVec_.Add(go.transform.position);
                mecanimEvent.cameraMove_.nodeList = nodeVec_;
            }
        }
    }

    void DragNewEffect(GameObject go) {
        MagiMecanimEvent mecanimEvent = null;
        if( selectMecanimEvent_ != null ) {
            mecanimEvent = selectMecanimEvent_;
        }
        else {
            mecanimEvent = new MagiMecanimEvent(mecanimEvent_);
            mecanimEvent.effectData_.effectObject_ = go;

            displayEvents_.Add(mecanimEvent);
        }

        if( go != null ) {
            var effect = go;
            string assetPath = AssetDatabase.GetAssetPath(effect).ToLower();
            if( assetPath.Contains("assets/prefabs/") == true ) {
                string temp = "assets/prefabs/";
                string dir = Path.GetDirectoryName(assetPath) + "/";
                assetPath = dir.Substring(temp.Length) + Path.GetFileNameWithoutExtension(assetPath);
                mecanimEvent.effectData_.effectObject_ = effect;
                mecanimEvent.effectData_.effectObjectFileName_ = assetPath;
                MagiDebug.Log(string.Format("currentMecanimEvent.effectData_.effectObjectFileName_ = {0}", mecanimEvent.effectData_.effectObjectFileName_));
            }
            //else
            //{
            //    MagiDebug.LogError(string.Format("effect file path error.{0}", assetPath));
            //    mecanimEvent.effectData_.effectObject_ = null;
            //    mecanimEvent.effectData_.effectObjectFileName_ = "";
            //}
        }
        Transform transform = go.transform;
        mecanimEvent.effectData_.euler_ = transform.rotation.eulerAngles;
        mecanimEvent.effectData_.point_ = targetModel_.transform.InverseTransformPoint(transform.position);

        Vector3 modelScale = new Vector3(1.0f / targetModel_.transform.localScale.x, 1.0f / targetModel_.transform.localScale.y, 1.0f / targetModel_.transform.localScale.z);
        mecanimEvent.effectData_.scale_ = Vector3.Scale(transform.localScale, modelScale);
    }

    private void EditNodePath(MagiMecanimEvent mecanimEvent) {
        GameObject igo = null;

        if( mecanimEvent.cameraMove_.nodeCount < 2 ) {
            igo = new GameObject();
            igo.gameObject.name = "DebugNodePath";

            if( mecanimEvent.cameraMove_.nodeList.Count <= 0 )
                igo.transform.position = Vector3.zero;
            else
                igo.transform.position = mecanimEvent.cameraMove_.nodeList[0];
        }

        else {
            igo = Instantiate(debugNodePath_) as GameObject;
            igo.gameObject.name = "DebugNodePath";

            iTweenPath itweenPath_ = igo.GetComponent<iTweenPath>();

            if( itweenPath_ != null ) {
                if( mecanimEvent.cameraMove_.nodeCount < 2 ) {
                    Destroy(igo.GetComponent<iTweenPath>());

                    if( mecanimEvent.cameraMove_.nodeList.Count <= 0 )
                        igo.transform.position = Vector3.zero;
                    else
                        igo.transform.position = mecanimEvent.cameraMove_.nodeList[0];

                    return;
                }

                itweenPath_.pathName += mecanimEvent.normalizedTime_.ToString();
                itweenPath_.nodeCount = mecanimEvent.cameraMove_.nodeCount;
                itweenPath_.nodes = mecanimEvent.cameraMove_.nodeList;
            }
        }
    }

    private void EditCollider(MagiMecanimEvent mecanimEvent) {
        switch( mecanimEvent.HitData.paramType ) {
            case MagiMecanimEventHit.EMagiHitType.Sphere: {
                    GameObject igo = Instantiate(debugHitSphere_) as GameObject;
                    DebugMagiMecanimHit debugMagiMecanimHit = igo.GetComponent<DebugMagiMecanimHit>();
                    if( debugMagiMecanimHit != null ) {
                        debugMagiMecanimHit.SetMagiMecanimEvent(mecanimEvent, targetModel_.transform);
                    }
                }
                break;
            case MagiMecanimEventHit.EMagiHitType.Capsule: {
                    GameObject igo = Instantiate(debugHitCapsule_) as GameObject;
                    DebugMagiMecanimHit debugMagiMecanimHit = igo.GetComponent<DebugMagiMecanimHit>();
                    if( debugMagiMecanimHit != null ) {
                        debugMagiMecanimHit.SetMagiMecanimEvent(mecanimEvent, targetModel_.transform);
                    }
                }
                break;
            case MagiMecanimEventHit.EMagiHitType.Box: {
                    GameObject igo = Instantiate(debugHitCube_) as GameObject;
                    DebugMagiMecanimHit debugMagiMecanimHit = igo.GetComponent<DebugMagiMecanimHit>();
                    if( debugMagiMecanimHit != null ) {
                        debugMagiMecanimHit.SetMagiMecanimEvent(mecanimEvent, targetModel_.transform);
                    }
                }
                break;
        }
        //        GameObject igo = Instantiate(go) as GameObject;
    }

    private void TimelineWindow(int id) {
        float startY = 30;
        Rect rect = new Rect(10, 40 + startY, position.width - 20, 150);

        playbackTime_ = PlaybackNormalizedTime;
        float oldTime = playbackTime_;
        playbackTime_ = Mathf.Clamp(Timeline(rect, playbackTime_), 0.0f, 1.0f);

        string playTime = "";
        float currentFrame = 0;
        float endFrame = 0;
        if( playbackTime_ != oldTime ) {
            PlaybackNormalizedTime = playbackTime_;
        }
        currentFrame = PlaybackCurrentTime * 30.0f;
        endFrame = PlaybackEndTime * 30.0f;
        playbackTime_ = Mathf.Clamp(playbackTime_, 0, 1.0f);

        playTime = currentFrame.ToString("0.00") + " / " + endFrame.ToString("0.00");
        mecanimEvent_.normalizedTime_ = Mathf.Clamp(PlaybackNormalizedTime, 0, 1.0f);
        //        string normalizedTimeString = mecanimEvent_.normalizedTime_.ToString();
        GUI.Label(new Rect(position.width - playTime.Length * 7, 30 + startY, playTime.Length * 7, 15), playTime);

        bool play = Play;
        string playString = play ? "Stop" : "Play";
        if( GUI.Button(new Rect(15, 82 + startY, 80, 20), playString) ) {
            Play = !play;
        }

        switch( eventDataType_ ) {
            case 0:  //HitData
                {
                    Collider newCollider = null;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Drag Collider:");
                    newCollider = EditorGUILayout.ObjectField(newCollider, typeof(Collider), true) as Collider;
                    if( selectMecanimEvent_ != null ) {
                        if( GUILayout.Button("Edit Collider") == true ) {
                            EditCollider(selectMecanimEvent_);
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if( newCollider != null ) {
                        DragNewCollider(newCollider);
                    }
                }
                break;
            case 1:  //Effect
                {
                    GameObject newGameObject = null;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Drag Effect:");
                    newGameObject = EditorGUILayout.ObjectField(newGameObject, typeof(GameObject), true) as GameObject;
                    if( selectMecanimEvent_ != null ) {
                        if( GUILayout.Button("Edit Effect") == true ) {
                            EditEffect(selectMecanimEvent_);
                        }
                    }
                    else {
                        if( GUILayout.Button("Clean Edit Effect") == true ) {
                            editEffectGameObject_ = null;
                        }
                    }
                    EditorGUILayout.EndHorizontal();

                    if( newGameObject != null ) {
                        DragNewEffect(newGameObject);
                    }
                }
                break;
            case 2: // FunctionCall
                {
                }
                break;
            case 3: // camera shake
                {
                }
                break;
            case 4: { // camera move
                    GameObject newGameObject = null;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Drag Node Path :");
                    newGameObject = EditorGUILayout.ObjectField(newGameObject, typeof(GameObject), true) as GameObject;
                    if( selectMecanimEvent_ != null ) {
                        if( GUILayout.Button("Edit Node") == true ) {
                            EditNodePath(selectMecanimEvent_);
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    if( newGameObject != null ) {
                        DragNewNodePath(newGameObject);
                    }
                }
                break;
        }

        if( GUI.Button(new Rect(15, 82 + startY, 80, 20), playString) ) {
            Play = !play;
        }

        if( GUI.Button(new Rect(15, 105 + startY, 80, 20), "Add Event") ) {
            MagiMecanimEvent copyMecanimEvent = new MagiMecanimEvent(mecanimEvent_);
            displayEvents_.Add(copyMecanimEvent);
            SaveState();
        }

        if( GUI.Button(new Rect(100, 105 + startY, 105, 20), "Remove Event") ) {
            if( selectMecanimEvent_ != null ) {
                DelEvent(selectMecanimEvent_);
                selectMecanimEvent_ = null;
            }
        }

        if( GUI.Button(new Rect(220, 105 + startY, 80, 20), "Save") ) {
            ModelInspector.MakeMecanimTransition(targetModel_, targetController_);
            ModelInspector.MakeVisibilityKey(targetModel_, targetController_);
            ModelInspector.MakeStateInfo(targetModel_, targetController_);

            targetModelTemplate_.SaveTemplate();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("", "Save Complate", "OK");
        }

        if( selectMecanimEvent_ != null ) {
            if( GUI.Button(new Rect(310, 105 + startY, 80, 20), "Copy") ) {
                copyMecanimEvent_ = new MagiMecanimEvent(selectMecanimEvent_);
            }
        }

        if( copyMecanimEvent_ != null ) {
            if( GUI.Button(new Rect(392, 105 + startY, 80, 20), "Paste") ) {
                if( selectMecanimEvent_ == null ) {
                    MagiMecanimEvent copyMecanimEvent = new MagiMecanimEvent(copyMecanimEvent_);
                    copyMecanimEvent.normalizedTime_ = playbackTime_;
                    displayEvents_.Add(copyMecanimEvent);
                }
                else {
                    MagiMecanimEvent.Clone(ref selectMecanimEvent_, copyMecanimEvent_);
                    selectMecanimEvent_.normalizedTime_ = playbackTime_;
                }
                SaveState();
            }
        }

        int selectIndex = GetSelectIndex();
        if( GUI.Button(new Rect(position.width - 150, 105 + startY, 30, 20), "<<") ) {
            if( selectMecanimEvent_ == null ) {
                SelectMecanimEvent(0);
            }
            else {
                SelectMecanimEvent(selectIndex - 1);
            }
        }

        if( GUI.Button(new Rect(position.width - 120, 105 + startY, 30, 20), ">>") ) {
            if( selectMecanimEvent_ == null ) {
                SelectMecanimEvent(0);
            }
            else {
                SelectMecanimEvent(selectIndex + 1);
            }
        }

        DrawEventKey(rect);

        EditorGUI.LabelField(new Rect(position.width - 150, 80 + startY, 150, 20), string.Format("{0}:Show Event Name", displayEvents_.Count));
        showEventName_ = EditorGUI.Toggle(new Rect(position.width - 30, 80 + startY, 30, 20), showEventName_);
    }

    int SortList(MagiMecanimEvent p1, MagiMecanimEvent p2) {
        if( p1.normalizedTime_ < p2.normalizedTime_ )
            return -1;
        if( p1.normalizedTime_ > p2.normalizedTime_ )
            return 1;

        return 0;
    }

    int GetSelectIndex() {
        if( selectMecanimEvent_ == null )
            return -1;

        int index = 0;
        MagiMecanimEvent.EDataType selectDataType = (MagiMecanimEvent.EDataType)eventDataType_;
        var sortEvents = new List<MagiMecanimEvent>();
        for( int i = 0; i < displayEvents_.Count; ++i ) {
            if( displayEvents_[i].DataType != selectDataType )
                continue;
            sortEvents.Add(displayEvents_[i]);
        }
        sortEvents.Sort(SortList);

        for( int i = 0; i < sortEvents.Count; ++i ) {
            if( selectMecanimEvent_ == sortEvents[i] )
                return index;
            ++index;
        }
        selectMecanimEvent_ = null;
        return -1;
    }

    void SelectMecanimEvent(int index) {
        if( index < 0 || index >= displayEvents_.Count )
            return;

        int count = 0;
        MagiMecanimEvent.EDataType selectDataType = (MagiMecanimEvent.EDataType)eventDataType_;

        var sortEvents = new List<MagiMecanimEvent>();
        for( int i = 0; i < displayEvents_.Count; ++i ) {
            if( displayEvents_[i].DataType != selectDataType )
                continue;
            sortEvents.Add(displayEvents_[i]);
        }
        sortEvents.Sort(SortList);

        if( index < 0 || index >= sortEvents.Count )
            return;

        for( int i = 0; i < sortEvents.Count; ++i ) {
            if( index == count ) {
                selectMecanimEvent_ = sortEvents[i];
                return;
            }
            ++count;
        }
    }

    private float Timeline(Rect rect, float time) {
        int timelineId = GUIUtility.GetControlID(timelineHash_, FocusType.Native);

        Rect thumbRect = new Rect(rect.x + rect.width * time - 5, rect.y + 2, 10, 10);
        Event e = Event.current;

        if( selectMecanimEvent_ != null ) {
            time = selectMecanimEvent_.normalizedTime_;
        }

        switch( e.type ) {
            case EventType.Repaint:
                GUI.skin.horizontalSlider.Draw(rect, new GUIContent(), timelineId);
                GUI.skin.horizontalSliderThumb.Draw(thumbRect, new GUIContent(), timelineId);
                break;
            case EventType.MouseDown:
                if( thumbRect.Contains(e.mousePosition) ) {
                    GUIUtility.hotControl = timelineId;
                    selectMecanimEvent_ = null;
                    e.Use();
                }
                break;
            case EventType.MouseUp:
                if( GUIUtility.hotControl == timelineId ) {
                    GUIUtility.hotControl = 0;
                    e.Use();
                }
                break;
            case EventType.MouseDrag:
                if( GUIUtility.hotControl == timelineId ) {
                    Vector2 guiPos = e.mousePosition;

                    float clampedX = Mathf.Clamp(guiPos.x, rect.x, rect.x + rect.width);
                    time = (clampedX - rect.x) / rect.width;
                    selectMecanimEvent_ = null;
                    e.Use();
                }
                break;
        }
        return time;
    }

    private void RepaintEventKey(Rect rect, Event e, bool select) {
        foreach( MagiMecanimEvent key in displayEvents_ ) {
            if( (selectMecanimEvent_ == key) != select )
                continue;

            if( key.DataType != (MagiMecanimEvent.EDataType)eventDataType_ )
                continue;

            float keyTime = key.normalizedTime_;
            int eventKeyCtrl = GUIUtility.GetControlID(eventKeyHash_, FocusType.Native);
            Rect keyRect = new Rect(rect.x + rect.width * keyTime - 3, rect.y, 6, 25);

            switch( e.type ) {
                case EventType.Repaint: {
                        Color savedColor = GUI.color;
                        GUI.color = selectMecanimEvent_ == key ? Color.red : Color.white;
                        GUI.skin.button.Draw(keyRect, new GUIContent(), eventKeyCtrl);

                        if( showEventName_ || keyRect.Contains(e.mousePosition) ) {
                            float frame = key.normalizedTime_ * PlaybackEndTime * 30.0f;
                            int width = 8 * frame.ToString("0.00").Length;
                            Rect infoRect = new Rect(rect.x + rect.width * keyTime - width / 2, rect.y - 30, width, 18);
                            GUI.skin.textArea.Draw(infoRect, new GUIContent(frame.ToString("0.00")), eventKeyCtrl);
                        }
                        GUI.color = savedColor;
                    }
                    break;
            }
        }
    }

    void RemoveCallBack(object obj) {
        if( selectMecanimEvent_ != null ) {
            DelEvent(selectMecanimEvent_);
            selectMecanimEvent_ = null;
        }
    }

    private void DrawEventKey(Rect rect) {
        if( displayEvents_ == null )
            return;
        Event e = Event.current;
        int unselect = 0;
        int currentCount = 0;
        foreach( MagiMecanimEvent key in displayEvents_ ) {
            if( eventDataType_ != (int)key.DataType )
                continue;

            currentCount++;
            float keyTime = key.normalizedTime_;
            Rect keyRect = new Rect(rect.x + rect.width * keyTime - 3, rect.y, 6, 25);

            switch( e.type ) {
                case EventType.MouseDown:
                    if( keyRect.Contains(e.mousePosition) ) {
                        mecanimEvent_ = new MagiMecanimEvent(key);
                        selectMecanimEvent_ = key;
                        e.Use();
                    }
                    else {
                        ++unselect;
                    }
                    break;
                case EventType.MouseDrag:
                    if( key == selectMecanimEvent_ ) {
                        Vector2 guiPos = e.mousePosition;
                        float clampedX = Mathf.Clamp(guiPos.x, rect.x, rect.x + rect.width);
                        key.normalizedTime_ = (clampedX - rect.x) / rect.width;
                        e.Use();
                    }
                    break;
                case EventType.ContextClick:
                    if( keyRect.Contains(e.mousePosition) ) {
                        selectMecanimEvent_ = key;
                        var menu = new GenericMenu();
                        GenericMenu.MenuFunction2 removeCallBack = new GenericMenu.MenuFunction2(RemoveCallBack);
                        menu.AddItem(new GUIContent("Remove"), false, removeCallBack, null);
                        menu.ShowAsContext();
                        e.Use();
                    }
                    break;
            }
        }

        if( unselect == currentCount )
            selectMecanimEvent_ = null;

        RepaintEventKey(rect, e, false);
        RepaintEventKey(rect, e, true);
    }

    public void SetPreviewMotion(UnityEngine.Motion motion) {
        if( eventInspector_ == null )
            return;
        eventInspector_.SetPreviewMotion(motion);
    }

    private void ChangeState() {
        if( eventInspector_ == null )
            return;

    }

    float playbackCurrentTime_;
    float playbackEndTime_ = 1;
    float playbackNormalizedTime_ = 0;
    public float PlaybackNormalizedTime {
        get {
            if( eventInspector_ == null )
                return playbackNormalizedTime_;
            playbackNormalizedTime_ = eventInspector_.PlaybackNormalizedTime;
            return eventInspector_.PlaybackNormalizedTime;
        }
        set {
            if( eventInspector_ == null )
                return;
            eventInspector_.PlaybackNormalizedTime = value;
            playbackNormalizedTime_ = value;
        }
    }

    public float PlaybackCurrentTime {
        get {
            return playbackCurrentTime_;
        }
    }

    public float PlaybackEndTime {
        get {
            return playbackEndTime_;
        }
    }

    public bool Play {
        get {
            if( eventInspector_ == null )
                return false;
            return eventInspector_.Play;
        }
        set {
            if( eventInspector_ == null )
                return;
            eventInspector_.Play = value;
        }
    }

    public void SetStateMechine(AnimatorController controller, int selectedLayer) {
        if( eventInspector_ == null )
            return;
        eventInspector_.SetStateMechine(controller, selectedLayer);
    }

    Dictionary<int, Dictionary<int, int>> backupDefaultState_ = new Dictionary<int, Dictionary<int, int>>();
    public void SetBackupDefaultState(AnimatorController controller, int selectedLayer) {
        if( controller == null )
            return;

        var animatorLayer = controller.GetLayer(selectedLayer);
        StateMachine stateMachine = animatorLayer.stateMachine;
        State defaultState = stateMachine.defaultState;

        if( defaultState == null ) {
            MagiDebug.LogError("defaultState == null");
            return;
        }

        int controllerID = controller.GetInstanceID();
        if( backupDefaultState_.ContainsKey(controllerID) == false ) {
            backupDefaultState_[controllerID] = new Dictionary<int, int>();
        }

        if( backupDefaultState_[controllerID].ContainsKey(selectedLayer) == false ) {
            backupDefaultState_[controllerID][selectedLayer] = defaultState.uniqueNameHash;
        }
        MagiDebug.Log(defaultState.uniqueName);
    }

    State FindState(StateMachine stateMachine, int uniqueNameHash) {
        for( int i = 0; i < stateMachine.stateCount; ++i ) {
            State state = stateMachine.GetState(i);
            if( state.uniqueNameHash == uniqueNameHash ) {
                return state;
            }
        }
        return null;
    }

    public void RestoreBackupDefaultState() {
        foreach( var p in backupDefaultState_ ) {
            AnimatorController controller = EditorUtility.InstanceIDToObject(p.Key) as AnimatorController;
            foreach( var p2 in p.Value ) {
                var animatorLayer = controller.GetLayer(p2.Key);
                StateMachine stateMachine = animatorLayer.stateMachine;
                if( p2.Value != 0 ) {
                    State defaultState = FindState(stateMachine, p2.Value);
                    if( defaultState != null ) {
                        stateMachine.defaultState = defaultState;
                    }
                }

                //string name = stateMachine.name + "." + "preview_test";
                //State testState = stateMachine.FindState(name);
                //if (testState != null)
                //{
                //    stateMachine.RemoveState(testState);
                //    Object.DestroyImmediate(testState);
                //}
            }
        }
        backupDefaultState_.Clear();
    }

    public KeyValuePair<string, MagiMecanimEventCondition.EParamTypes>[] GetConditionParameters() {
        List<KeyValuePair<string, MagiMecanimEventCondition.EParamTypes>> ret = new List<KeyValuePair<string, MagiMecanimEventCondition.EParamTypes>>();
        if( targetController_ != null ) {
            for( int i = 0; i < targetController_.parameterCount; i++ ) {
                var parameter = targetController_.GetParameter(i);
                switch( parameter.type ) {
                    case AnimatorControllerParameterType.Float:
                        ret.Add(new KeyValuePair<string, MagiMecanimEventCondition.EParamTypes>(parameter.name, MagiMecanimEventCondition.EParamTypes.Float));
                        break;
                    case AnimatorControllerParameterType.Int:
                        ret.Add(new KeyValuePair<string, MagiMecanimEventCondition.EParamTypes>(parameter.name, MagiMecanimEventCondition.EParamTypes.Int));
                        break;
                    case AnimatorControllerParameterType.Bool:
                        ret.Add(new KeyValuePair<string, MagiMecanimEventCondition.EParamTypes>(parameter.name, MagiMecanimEventCondition.EParamTypes.Boolean));
                        break;
                }
            }
        }
        return ret.ToArray();
    }

    // Effect
    private static GameObject m_RootInstanceEffect_;
    public static GameObject GetRootInstanceEffect() {
        if( m_RootInstanceEffect_ == null ) {
            m_RootInstanceEffect_ = GameObject.Find("RootInstanceEffect_");
            if( m_RootInstanceEffect_ == null )
                m_RootInstanceEffect_ = new GameObject("RootInstanceEffect_");
        }
        return m_RootInstanceEffect_;
    }

    bool IsAliveAnimationEffect() {
        GameObject rootInstObj = GetRootInstanceEffect();

        Transform[] tranComs = rootInstObj.GetComponentsInChildren<Transform>(true);
        foreach( Transform trans in tranComs ) {
            int bNcAni = -1;	// -1:None, 0:End, 1:Alive
            int bParticle = -1;
            bool bRen = false;

            // Check Animation
            NcEffectBehaviour[] effList = trans.GetComponents<NcEffectBehaviour>();
            foreach( NcEffectBehaviour eff in effList ) {
                switch( eff.GetAnimationState() ) {
                    case 1:
                        bNcAni = 1;
                        break;
                    case 0:
                        bNcAni = 0;
                        break;
                }
            }

            // Check ParticleSystem
            if( trans.particleSystem != null ) {
                bParticle = 0;
                if( NgObject.IsActive(trans.gameObject) && ((trans.particleSystem.enableEmission && trans.particleSystem.IsAlive()) || 0 < trans.particleSystem.particleCount) )
                    bParticle = 1;
            }

            // Check ParticleSystem
            if( bParticle < 1 ) {
                if( trans.particleEmitter != null ) {
                    bParticle = 0;
                    if( NgObject.IsActive(trans.gameObject) && (trans.particleEmitter.emit || 0 < trans.particleEmitter.particleCount) )
                        bParticle = 1;
                }
            }

            // Check Renderer
            if( trans.renderer != null ) {
                if( trans.GetComponent<FXMakerWireframe>() == null && trans.renderer.enabled && NgObject.IsActive(trans.gameObject) )
                    bRen = true;
            }

            //   			MagiDebug.Log("bNcAni " + bNcAni + ", bParticle " + bParticle + ", bRen " + bRen);
            if( 0 < bNcAni )
                return true;
            if( bParticle == 1 )
                return true;
            if( bRen && (trans.GetComponent<MeshFilter>() != null || trans.GetComponent<TrailRenderer>() != null || trans.GetComponent<LineRenderer>() != null) )
                return true;
        }
        return false;
    }

    GameObject editEffectGameObject_;
    bool CreateCurrentInstanceEffect(GameObject effectObject) {
        GameObject parentObj = GetRootInstanceEffect();

        // 이전거 삭제
        NgObject.RemoveAllChildObject(parentObj, true);

        // 새로 생성
        if( effectObject != null ) {
            GameObject createObj = (GameObject)Instantiate(effectObject);
            //createObj.transform.eulerAngles = mecanimEvent.effectData_.euler_;
            //createObj.transform.position = mecanimEvent.effectData_.point_;
            //createObj.transform.localScale = mecanimEvent.effectData_.scale_;
            NcEffectBehaviour.PreloadTexture(createObj);

            createObj.transform.parent = parentObj.transform;
            createObj.transform.position = parentObj.transform.position;
            createObj.transform.rotation = parentObj.transform.rotation;
            //            createObj.transform.localScale = parentObj.transform.localScale;
            return true;
        }
        return false;
    }

    void EditEffect(MagiMecanimEvent mecanimEvent) {
        if( mecanimEvent.effectData_.effectObject_ == null )
            return;
        editEffectGameObject_ = mecanimEvent.effectData_.effectObject_;

        GameObject parentObj = GetRootInstanceEffect();

        parentObj.transform.eulerAngles = mecanimEvent.effectData_.euler_;
        parentObj.transform.position = targetModel_.transform.TransformPoint(mecanimEvent.effectData_.point_);
        parentObj.transform.localScale = Vector3.Scale(targetModel_.transform.localScale, mecanimEvent.effectData_.scale_);
    }

    void OnApplicationQuit() {
        ModelInspector.MakeMecanimTransition(targetModel_, targetController_);
        ModelInspector.MakeVisibilityKey(targetModel_, targetController_);
        ModelInspector.MakeStateInfo(targetModel_, targetController_);
        targetModelTemplate_.SaveTemplate();

        MagiDebug.Log("SaveTemplate ------------");
    }
}
