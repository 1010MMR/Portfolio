using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if USER_SERVER
using NetWork;
#endif

using LitJson;

/// <summary>
/// <para>name : Tutorial</para>
/// <para>describe : 튜토리얼 정보.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public partial class Tutorial : MonoSingleton<Tutorial>
{
    public const int SCENE_MAX_VALUE = 999;

    private readonly string[] COMMON_METHOD_ARRAY = { "OnNpcTextClick", "OnTutorialTalkNext", "OnLevelUpWindowClick", "OnUnlockOkButton", "OnClickDogLevelUpBtnOK" };
    private readonly string[] COMMON_PROTOCOL_ARRAY = { NetProtocol.AUTH_LOGIN, NetProtocol.AUTH_SDK_LOGIN, NetProtocol.AUTH_LOGOUT, 
                                                                            NetProtocol.GAME_INIT, NetProtocol.HEALTH_CHECK, NetProtocol.PAY_REFRESH_ORDER };

    public TUTORIAL_TYPE TutorialType { get; set; }

    private const int TUTORIAL_START_SEQUENCE = 1;

    private int m_curTutorialStep = 0;
    private int m_curTutorialCode = 0;

    private Dictionary<int, TutorialInfo> m_curTutorialTable = null;
    private TutorialInfo m_curTutorialInfo = null;

    private bool m_isActiveNext = false;

    #region Tutorial_Code

    public void InitTutorialCode(int code)
    {
        TutorialType = TUTORIAL_TYPE.TYPE_NONE;

        m_curTutorialInfo = null;
        m_curTutorialTable = null;

        m_curTutorialCode = code;
        if(CheckTutorialCodeType(code) == false && GetCurrentTutorialTable)
        {
            TutorialType = TUTORIAL_TYPE.TYPE_BASIC;

            m_curTutorialInfo = m_curTutorialTable[code];
            m_curTutorialStep = m_curTutorialInfo.step;

            Debug.Log("Tutorial.Basic.InitTutorialCode : " + m_curTutorialInfo.index);
        }
    }

    #endregion

    #region Tutorial_Value

    public bool CheckTutorialEnable
    {
        get { return CheckTutorialCodeType(m_curTutorialCode) == false && m_curTutorialTable != null && m_curTutorialInfo.CheckTutorialEnable(); }
    }

    public bool CheckSimpleTutorialEnable
    {
        get { return CheckTutorialCodeType(m_curTutorialCode) == false && m_curTutorialTable != null; }
    }

    public int GetCurrentTutorialStep
    {
        get { return WorldManager.instance.m_dataManager.m_tutorialData.GetTutorialStep(m_curTutorialCode); }
    }

    public bool GetCurrentTutorialTable
    {
        get { return WorldManager.instance.m_dataManager.m_tutorialData.GetTutorialList(GetCurrentTutorialStep, out m_curTutorialTable); }
    }

    public TutorialInfo GetCurrentTutorialInfo
    {
        get { return m_curTutorialInfo; }
    }

    public int GetCurrentTutorialSavePoint
    {
        get { return m_curTutorialInfo != null ? m_curTutorialInfo.savePointID : 0; }
    }

    public bool CheckTutorialTypeForActiveSeq(TUTORIAL_TRIGGER_TYPE type)
    {
        switch(type)
        {
            case TUTORIAL_TRIGGER_TYPE.TYPE_ROOM_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_ROOM);
            case TUTORIAL_TRIGGER_TYPE.TYPE_TOWN_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_VILLAGE);
            case TUTORIAL_TRIGGER_TYPE.TYPE_ADOPT_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_ADOPT);
            case TUTORIAL_TRIGGER_TYPE.TYPE_PETSHOP_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_PETSHOP);
            case TUTORIAL_TRIGGER_TYPE.TYPE_INTERIOR_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_INTERIORSHOP);
            case TUTORIAL_TRIGGER_TYPE.TYPE_BEAUTYSHOP_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_BEAUTYSHOP);

            case TUTORIAL_TRIGGER_TYPE.TYPE_DOGINFO_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_DOGINFO);
            case TUTORIAL_TRIGGER_TYPE.TYPE_MAKING_ROOM_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_MAKINGROOM);

            case TUTORIAL_TRIGGER_TYPE.TYPE_INTERIOR_REQUEST_LOAD:
            case TUTORIAL_TRIGGER_TYPE.TYPE_PETSHOP_REQUEST_LOAD:
            case TUTORIAL_TRIGGER_TYPE.TYPE_BEAUTY_REQUEST_LOAD:
                return StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_REQUEST);

            default:
                return true;
        }
    }

    public bool CheckTutorialLockSceneType(SCENE_TYPE type)
    {
        switch(type)
        {
            case SCENE_TYPE.SCENE_NONE:
            case SCENE_TYPE.SCENE_TITLE:
            case SCENE_TYPE.SCENE_INTRO:
                return false;

            default:
                return true;
        }
    }

    public bool CheckTutorialType(TUTORIAL_TRIGGER_TYPE type)
    {
        TutorialInfo curTutInfo = null;
        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: curTutInfo = m_curTutorialInfo; break;
            case TUTORIAL_TYPE.TYPE_BUILDING: curTutInfo = m_curTutorialBuildingInfo; break;
        }

        return curTutInfo != null ? curTutInfo.TriggerType.Equals(type) : false;
    }

    public bool CheckTutorialType(TUTORIAL_TRIGGER_TYPE[] typeArray)
    {        
        TutorialInfo curTutInfo = null;
        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: curTutInfo = m_curTutorialInfo; break;
            case TUTORIAL_TYPE.TYPE_BUILDING: curTutInfo = m_curTutorialBuildingInfo; break;
        }

        if(curTutInfo != null && typeArray != null )
        {
            for(int i = 0; i < typeArray.Length; i++)
            {
                if(typeArray[i].Equals(curTutInfo.TriggerType))
                    return true;
            }
        }

        return false;
    }

    public uint GetCurrentTutorialRewardIndex
    {
        get
        {
            if(m_curTutorialTable != null)
            {
                foreach(TutorialInfo info in m_curTutorialTable.Values)
                {
                    if(info.provideItemIndex.Equals(0) == false)
                        return info.provideItemIndex;
                }
            }

            return 0;
        }
    }

    #region ButtonActive

    public bool CheckTutorialButtonActive(List<EventDelegate> evList)
    {
        if(CheckTutorialEnable == false && CheckTutorialBuildingEnable == false)
            return false;

        bool isEnable = CheckTutorialType(new TUTORIAL_TRIGGER_TYPE[] { 
            TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH, TUTORIAL_TRIGGER_TYPE.TYPE_WAIT, 
            TUTORIAL_TRIGGER_TYPE.TYPE_NONSERVER_WAIT, TUTORIAL_TRIGGER_TYPE.TYPE_DRAG_DROP });

        TutorialInfo curTutInfo = null;
        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: curTutInfo = m_curTutorialInfo; break;
            case TUTORIAL_TYPE.TYPE_BUILDING: curTutInfo = m_curTutorialBuildingInfo; break;
        }

        if(isEnable)
        {
            if(CheckMethodListEqualsName(curTutInfo, evList))
            {
                EventDelegate.Execute(evList);
                if (CheckTutorialType(TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH))
                {
                    switch (Tutorial.instance.TutorialType)
                    {
                        case TUTORIAL_TYPE.TYPE_BASIC: EndCurrentTutorialCode(true); break;
                        case TUTORIAL_TYPE.TYPE_BUILDING: EndSeqBuildingTrigger(true); break;
                    }
                }
            }
        }

        else
        {
            switch(StateManager.instance.m_curStateType)
            {
                case STATE_TYPE.STATE_TITLE:
                case STATE_TYPE.STATE_INTRO:
                    return false;

                default:
                    return !CheckCommonMethodListEqualsName(evList);
            }
        }

        return isEnable;
    }

    public bool CheckTutorialButtonActive(GameObject buttonObj, TUTORIAL_BUTTON_TYPE type)
    {
        if(CheckTutorialEnable == false && CheckTutorialBuildingEnable == false)
            return false;

        bool isEnable = CheckTutorialType(new TUTORIAL_TRIGGER_TYPE[] { 
            TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH, TUTORIAL_TRIGGER_TYPE.TYPE_WAIT, 
            TUTORIAL_TRIGGER_TYPE.TYPE_NONSERVER_WAIT, TUTORIAL_TRIGGER_TYPE.TYPE_DRAG_DROP });

        UIPlayAnimation uiAni = buttonObj.GetComponent<UIPlayAnimation>();
        UIEventListener uiEv = buttonObj.GetComponent<UIEventListener>();
        UIEventTrigger uiTrig = buttonObj.GetComponent<UIEventTrigger>();
        UIButton uiBtn = buttonObj.GetComponent<UIButton>();

        TutorialInfo curTutInfo = null;
        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: curTutInfo = m_curTutorialInfo; break;
            case TUTORIAL_TYPE.TYPE_BUILDING: curTutInfo = m_curTutorialBuildingInfo; break;
        }

        if(isEnable)
        {
            switch(type)
            {
                case TUTORIAL_BUTTON_TYPE.TYPE_ANIM:
                    if(CheckMethodListEqualsName(curTutInfo, uiAni.onFinished))
                    {
                        EventDelegate.Execute(uiAni.onFinished);
                        if (CheckTutorialType(TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH))
                        {
                            switch (Tutorial.instance.TutorialType)
                            {
                                case TUTORIAL_TYPE.TYPE_BASIC: EndCurrentTutorialCode(true); break;
                                case TUTORIAL_TYPE.TYPE_BUILDING: EndSeqBuildingTrigger(true); break;
                            }
                        }
                    }

                    break;

                case TUTORIAL_BUTTON_TYPE.TYPE_EVENT:
                    if (uiEv.onClick != null)
                    {
                        if (curTutInfo.CheckMethodNameExists(uiEv.onClick.Method.Name))
                        {
                            uiEv.onClick(buttonObj);
                            if (CheckTutorialType(TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH))
                            {
                                switch (Tutorial.instance.TutorialType)
                                {
                                    case TUTORIAL_TYPE.TYPE_BASIC: EndCurrentTutorialCode(true); break;
                                    case TUTORIAL_TYPE.TYPE_BUILDING: EndSeqBuildingTrigger(true); break;
                                }
                            }
                        }

                        else
                        {
                            bool isExists = (uiAni != null && CheckMethodListEqualsName(curTutInfo, uiAni.onFinished)) ||
                                            (uiBtn != null && CheckMethodListEqualsName(curTutInfo, uiBtn.onClick));
                            if(isExists)
                                uiEv.onClick(buttonObj);
                        }
                    }

                    break;

                case TUTORIAL_BUTTON_TYPE.TYPE_BUTTON:
                    if(CheckMethodListEqualsName(curTutInfo, uiBtn.onClick))
                    {
                        EventDelegate.Execute(uiBtn.onClick);
                        if (CheckTutorialType(TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH))
                        {
                            switch (Tutorial.instance.TutorialType)
                            {
                                case TUTORIAL_TYPE.TYPE_BASIC: EndCurrentTutorialCode(true); break;
                                case TUTORIAL_TYPE.TYPE_BUILDING: EndSeqBuildingTrigger(true); break;
                            }
                        }
                    }

                    else
                    {
                        bool isExists = (uiAni != null && CheckMethodListEqualsName(curTutInfo, uiAni.onFinished)) ||
                                        (uiEv != null && uiEv.onClick != null && curTutInfo.CheckMethodNameExists(uiEv.onClick.Method.Name));
                        if(isExists)
                            EventDelegate.Execute(uiBtn.onClick);
                    }

                    break;
            }
        }

        else
        {
            switch(StateManager.instance.m_curStateType)
            {
                case STATE_TYPE.STATE_TITLE:
                case STATE_TYPE.STATE_INTRO:
                    return false;

                default:
                    switch(type)
                    {
                        case TUTORIAL_BUTTON_TYPE.TYPE_ANIM:
                            return !CheckCommonMethodListEqualsName(uiAni.onFinished);
                        case TUTORIAL_BUTTON_TYPE.TYPE_EVENT:
                            return !CheckCommonMethodListEqualsName(uiEv.onClick.Method.Name);
                        case TUTORIAL_BUTTON_TYPE.TYPE_BUTTON:
                            return !CheckCommonMethodListEqualsName(uiBtn.onClick);
                    }

                    return true;
            }
        }

        return isEnable;
    }

    private bool CheckMethodListEqualsName(TutorialInfo info, List<EventDelegate> evList)
    {
        if(evList != null)
        {
            for(int i = 0; i < evList.Count; i++)
            {
                if(info.CheckMethodNameExists(evList[i].methodName))
                    return true;
            }
        }

        return false;
    }

    private bool CheckCommonMethodListEqualsName(List<EventDelegate> evList)
    {
        if(evList != null)
        {
            for(int i = 0; i < evList.Count; i++)
            {
                for(int j = 0; j < COMMON_METHOD_ARRAY.Length; j++)
                {
                    if(COMMON_METHOD_ARRAY[j].Equals(evList[i].methodName))
                        return true;
                }
            }
        }

        return false;
    }

    private bool CheckCommonMethodListEqualsName(string methodName)
    {
        for(int i = 0; i < COMMON_METHOD_ARRAY.Length; i++)
        {
            if(COMMON_METHOD_ARRAY[i].Equals(methodName))
                return true;
        }

        return false;
    }

    private bool CheckTutorialCodeType(int code)
    {
        switch((TUTORIAL_CODE_TYPE)code)
        {
            case TUTORIAL_CODE_TYPE.TYPE_NONE:
            case TUTORIAL_CODE_TYPE.TYPE_COMPLETE:
            case TUTORIAL_CODE_TYPE.TYPE_SKIP:
                return true;
            default:
                return false;
        }
    }

    #endregion

    #region NetworkActive

    public bool CheckNetworkActive(string protocol)
    {
        for(int i = 0; i < COMMON_PROTOCOL_ARRAY.Length; i++)
        {
            if(protocol.Equals(COMMON_PROTOCOL_ARRAY[i]))
                return false;
        }

        return true;
    }

    #endregion

    #endregion

    #region Active_Current_Tutorial_Seq

    public bool CheckTutorialActive(bool isActiveNext = false, UITutorial.UITutorialCloseCB completeCB = null)
    {
        bool isEnable = CheckTutorialEnable;
        if (isEnable)
            ActiveTutorialSequence(isActiveNext, completeCB);

        return isEnable;
    }

    public void ActiveTutorialSequence(bool isActiveNext = false, UITutorial.UITutorialCloseCB completeCB = null)
    {
        m_isActiveNext = isActiveNext;
        m_uiTutorialCloseCB = completeCB;

        if(m_curTutorialInfo.sequence.Equals(TUTORIAL_START_SEQUENCE))
            RequestTutorialStart();
        else
            StartSequenceTrigger();
    }

    public bool CheckTutorialActive(int triggerType, bool isActiveNext = false, UITutorial.UITutorialCloseCB completeCB = null)
    {
        TutorialInfo curInfo = null;
        Dictionary<int, TutorialInfo> curTable = null;

        if (CheckTutorialCodeType(m_curTutorialStep) == false && WorldManager.instance.m_dataManager.m_tutorialData.GetTutorialList(m_curTutorialStep, triggerType, out curInfo, out curTable))
        {
            Debug.Log(curInfo.index);

            TutorialType = TUTORIAL_TYPE.TYPE_BASIC;

            m_curTutorialStep = curInfo.step;
            m_curTutorialInfo = curInfo;
            m_curTutorialCode = curInfo.index;
            m_curTutorialTable = curTable;

            bool isEnable = CheckTutorialEnable;
            if (isEnable)
                ActiveTutorialSequence(isActiveNext, completeCB);

            return isEnable;
        }

        return false;
    }

    private void StartSequenceTrigger()
    {
        switch(m_curTutorialInfo.TriggerType)
        {
            #region Event
            case TUTORIAL_TRIGGER_TYPE.TYPE_TALK:
            case TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH:
            case TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH_VIEW:
            case TUTORIAL_TRIGGER_TYPE.TYPE_DELAY_TIME:
            case TUTORIAL_TRIGGER_TYPE.TYPE_WAIT:
            case TUTORIAL_TRIGGER_TYPE.TYPE_NONSERVER_WAIT:
            case TUTORIAL_TRIGGER_TYPE.TYPE_DRAG_DROP:
            case TUTORIAL_TRIGGER_TYPE.TYPE_REWARD_WINDOW:
                OpenTutorialWindow(m_curTutorialInfo);
                if(m_curTutorialInfo.sendMessageTarget.Equals("0") == false && m_curTutorialInfo.sendMessageFunc.Equals("0") == false)
                {
                    GameObject findObj = GameObject.Find(m_curTutorialInfo.sendMessageTarget);
                    if(findObj != null)
                        findObj.SendMessage(m_curTutorialInfo.sendMessageFunc, m_curTutorialInfo.sendMessageParam, SendMessageOptions.DontRequireReceiver);
                    else
                    {
                        if (m_curTutorialInfo.sendMessageTarget.Contains("StateManager"))
                        {
                            if (StateManager.instance.m_curState.gameObject.activeSelf == false)
                                StateManager.instance.m_curState.gameObject.SetActive(true);
                            StateManager.instance.m_curState.gameObject.SendMessage(m_curTutorialInfo.sendMessageFunc, m_curTutorialInfo.sendMessageParam, SendMessageOptions.DontRequireReceiver);
                        }

                        else
                            MsgBox.instance.OpenMsgBox(127, 706, MSGBOX_TYPE.YESNO, OnTutFailCallback);
                    }
                }
                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH_CALLBACK:
                OpenTutorialWindow(m_curTutorialInfo);
                break;
            #endregion

            #region Camera
            case TUTORIAL_TRIGGER_TYPE.TYPE_CAMERA_MOVE:
                SetActiveTutorialWindow(TUTORIAL_UI_TYPE.TYPE_DELAY);

                if (m_curTutorialInfo.sendMessageTarget.Equals("0") == false && m_curTutorialInfo.sendMessageFunc.Equals("0") == false)
                {
                    GameObject findObj = GameObject.Find(m_curTutorialInfo.sendMessageTarget);
                    if (findObj != null)
                        findObj.SendMessage(m_curTutorialInfo.sendMessageFunc, m_curTutorialInfo.sendMessageParam, SendMessageOptions.DontRequireReceiver);
                    else
                    {
                        if (m_curTutorialInfo.sendMessageTarget.Contains("StateManager"))
                        {
                            if (StateManager.instance.m_curState.gameObject.activeSelf == false)
                                StateManager.instance.m_curState.gameObject.SetActive(true);
                            StateManager.instance.m_curState.gameObject.SendMessage(m_curTutorialInfo.sendMessageFunc, m_curTutorialInfo.sendMessageParam, SendMessageOptions.DontRequireReceiver);
                        }

                        else
                            MsgBox.instance.OpenMsgBox(127, 706, MSGBOX_TYPE.YESNO, OnTutFailCallback);
                    }
                }

                StateManager.instance.m_curState.OnTutorialMoveCamera(m_curTutorialInfo);
                break;
            #endregion

            #region Scene_Load
            case TUTORIAL_TRIGGER_TYPE.TYPE_ROOM_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_ROOM))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    NetworkManager.instance.SendRoomTransfer(WorldManager.instance.m_player.GetCurRoomNo());
                    EndCurrentTutorialCode(m_isActiveNext);
                }

                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_ADOPT_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_ADOPT))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    BuildingInfo info = WorldManager.instance.m_town.GetBuildingInfoNc(BUILDING_TYPE.BUILDING_ADOPT);
                    if(info != null)
                    {
                        WorldManager.instance.m_town.CurrentBuildingInfo = info;

                        StateManager.instance.SetTransition(STATE_TYPE.STATE_ADOPT);
                        EndCurrentTutorialCode(m_isActiveNext);
                    }
                }

                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_PETSHOP_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_PETSHOP))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    BuildingInfo info = WorldManager.instance.m_town.GetBuildingInfoNc(BUILDING_TYPE.BUILDING_PET);
                    if(info != null)
                    {
                        WorldManager.instance.m_town.CurrentBuildingInfo = info;

                        StateManager.instance.SetTransition(STATE_TYPE.STATE_PETSHOP);
                        EndCurrentTutorialCode(m_isActiveNext);
                    }
                }

                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_INTERIOR_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_INTERIORSHOP))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    BuildingInfo info = WorldManager.instance.m_town.GetBuildingInfoNc(BUILDING_TYPE.BUILDING_INTERIOR);
                    if(info != null)
                    {
                        WorldManager.instance.m_town.CurrentBuildingInfo = info;

                        StateManager.instance.SetTransition(STATE_TYPE.STATE_INTERIORSHOP);
                        EndCurrentTutorialCode(m_isActiveNext);
                    }
                }

                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_BEAUTYSHOP_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_BEAUTYSHOP))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    BuildingInfo info = WorldManager.instance.m_town.GetBuildingInfoNc(BUILDING_TYPE.BUILDING_BEAUTY);
                    if(info != null)
                    {
                        WorldManager.instance.m_town.CurrentBuildingInfo = info;

                        StateManager.instance.SetTransition(STATE_TYPE.STATE_BEAUTYSHOP);
                        EndCurrentTutorialCode(m_isActiveNext);
                    }
                }

                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_TOWN_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_VILLAGE))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    NetworkManager.instance.SendTownEnter(WorldManager.instance.m_player.m_lastTown);
                    EndCurrentTutorialCode(m_isActiveNext);
                }
                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_DOGINFO_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_DOGINFO))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    MapObjectPool.instance.AllReleaseObject();
                    WorldManager.instance.SetSceneDogInfo(-1, 0);

                    EndCurrentTutorialCode(m_isActiveNext);
                }

                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_MAKING_ROOM_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_MAKINGROOM))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    StateManager.instance.SetTransition(STATE_TYPE.STATE_MAKINGROOM);
                    EndCurrentTutorialCode(m_isActiveNext);
                }
                
                break;
            #endregion

            #region Request_Load
            case TUTORIAL_TRIGGER_TYPE.TYPE_INTERIOR_REQUEST_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_REQUEST) &&
                    WorldManager.instance.m_town.CurrentBuildingInfo.type.Equals(BUILDING_TYPE.BUILDING_INTERIOR))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    BuildingInfo info = WorldManager.instance.m_town.GetBuildingInfoNc(BUILDING_TYPE.BUILDING_INTERIOR);
                    if(info != null)
                    {
                        WorldManager.instance.m_town.CurrentBuildingInfo = info;

                        StateManager.instance.SetTransition(STATE_TYPE.STATE_REQUEST);
                        EndCurrentTutorialCode(m_isActiveNext);
                    }
                }

                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_PETSHOP_REQUEST_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_REQUEST) &&
                    WorldManager.instance.m_town.CurrentBuildingInfo.type.Equals(BUILDING_TYPE.BUILDING_PET))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    BuildingInfo info = WorldManager.instance.m_town.GetBuildingInfoNc(BUILDING_TYPE.BUILDING_PET);
                    if(info != null)
                    {
                        WorldManager.instance.m_town.CurrentBuildingInfo = info;

                        StateManager.instance.SetTransition(STATE_TYPE.STATE_REQUEST);
                        EndCurrentTutorialCode(m_isActiveNext);
                    }
                }

                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_BEAUTY_REQUEST_LOAD:
                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_REQUEST) &&
                    WorldManager.instance.m_town.CurrentBuildingInfo.type.Equals(BUILDING_TYPE.BUILDING_BEAUTY))
                    EndCurrentTutorialCode(m_isActiveNext);
                else
                {
                    BuildingInfo info = WorldManager.instance.m_town.GetBuildingInfoNc(BUILDING_TYPE.BUILDING_BEAUTY);
                    if(info != null)
                    {
                        WorldManager.instance.m_town.CurrentBuildingInfo = info;

                        StateManager.instance.SetTransition(STATE_TYPE.STATE_REQUEST);
                        EndCurrentTutorialCode(m_isActiveNext);
                    }
                }

                break;
            #endregion

            case TUTORIAL_TRIGGER_TYPE.TYPE_SITUATION_CHECK:
                CheckSituationExcutePossible(m_curTutorialInfo.index);
                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_TRIGGER_JUMP:
                EndCurrentTutorialCode(m_isActiveNext);
                break;

            default:
                if (m_curTutorialInfo.CheckTutorialCheckTrigger)
                    EndCurrentTutorialCode(m_isActiveNext);
                break;
        }

        m_isActiveNext = false;
    }

    private void OnTutFailCallback(MSGBOX_TYPE type, bool isYes)
    {
        if (isYes) WorldManager.instance.RestartApplication();
        else Util.ApplicationQuit();
    }

    #endregion

    #region Clear_Current_Tutorial_Seq

    public void EndCurrentTutorialCode(bool isActiveNext = false)
    {
        Debug.Log("EndCurrentTutorialCode." + m_curTutorialInfo.index + " / " + m_curTutorialInfo.TriggerType);

        if(m_curTutorialTable.ContainsKey(m_curTutorialInfo.nextStepID))
        {
            TUTORIAL_TRIGGER_TYPE beforeTriggerType = m_curTutorialInfo.TriggerType;

            m_curTutorialCode = m_curTutorialInfo.nextStepID;
            m_curTutorialInfo = m_curTutorialTable[m_curTutorialCode];

            if(isActiveNext && CheckTutorialTypeForActiveSeq(beforeTriggerType))
                ActiveTutorialSequence(isActiveNext);
            else
                ReleaseWindow();
        }

        else
        {
            if (m_curTutorialInfo.savePointID.Equals(1))
            {
                int curIndex = m_curTutorialInfo.index;

                ReleaseTutorialState();
                NetworkManager.instance.SendTutorialFinish(curIndex);
            }

            else if (m_curTutorialInfo.nextStepID.Equals(0))
            {
                m_curTutorialStep++;

                WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.TUTORIAL_FIN_TYPE, m_curTutorialInfo.index);
                ReleaseTutorialState(false);
                
                GemInfo.instance.OnOff(true);
            }

            else
            {
                m_curTutorialCode = m_curTutorialInfo.nextStepID;
                if (GetCurrentTutorialTable)
                {
                    m_curTutorialInfo = m_curTutorialTable[m_curTutorialCode];
                    m_curTutorialStep = m_curTutorialInfo.step;

                    switch (StateManager.instance.m_curStateType)
                    {
                        case STATE_TYPE.STATE_ROOM:
                        case STATE_TYPE.STATE_VILLAGE:
                            if (UserInfo.instance.CheckLevelUp == false)
                            {

                                if (isActiveNext && CheckTutorialTypeForActiveSeq(m_curTutorialInfo.TriggerType))
                                    ActiveTutorialSequence(isActiveNext);
                                else
                                    ReleaseWindow();
                            }
                            break;

                        default:
                            if (isActiveNext && CheckTutorialTypeForActiveSeq(m_curTutorialInfo.TriggerType))
                                ActiveTutorialSequence(isActiveNext);
                            else
                                ReleaseWindow();
                            break;
                    }
                }
            }
        }
    }

    public void UpdateNextTutorialCode(int nextStepID)
    {
        m_curTutorialCode = nextStepID;
        m_curTutorialInfo = m_curTutorialTable[m_curTutorialCode];
    }

    #endregion

    #region Tutorial_Start

    private void RequestTutorialStart()
    {
        SetActiveTutorialWindow(TUTORIAL_UI_TYPE.TYPE_DELAY);
        NetworkManager.instance.SendTutorialStart(m_curTutorialInfo.index);
    }

    #endregion

    #region Tutorial_Skip

    public void TutorialSkip()
    {
        NetworkManager.instance.SendTutorialSkip();
    }

    #endregion

    #region Response

    public void ResponseTutorialStart(int tutCode)
    {
        InitTutorialCode(tutCode);

        switch(StateManager.instance.m_curStateType)
        {
            case STATE_TYPE.STATE_ROOM:
            case STATE_TYPE.STATE_VILLAGE:
                if(UserInfo.instance.CheckLevelUp == false)
                    StartSequenceTrigger();
                break;

            default:
                StartSequenceTrigger();
                break;
        }
    }

    public void ResponseTutorialFinish(int tutCode)
    {
        switch((TUTORIAL_CODE_TYPE)tutCode)
        {
            case TUTORIAL_CODE_TYPE.TYPE_COMPLETE:
                InitTutorialCode(tutCode);
                GemInfo.instance.OnOff(true);
                break;

            case TUTORIAL_CODE_TYPE.TYPE_SKIP:
                InitTutorialCode(tutCode);
                ReleaseWindow();

                GemInfo.instance.OnOff(true);

                if(StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_ROOM) == false)
                    NetworkManager.instance.SendRoomTransfer(WorldManager.instance.m_player.GetCurRoomNo());
                else
                {
                    if(WorldManager.instance.CheckMemoryInfoExists(WORLD_MEMORY_INFO.EVENT_PAGE_OPEN))
                    {
                        bool isOpen = (bool)WorldManager.instance.GetMemoryInfo(WORLD_MEMORY_INFO.EVENT_PAGE_OPEN);
                        if(isOpen)
                            EventManager.instance.InitEventInfo(EventManager.instance.GetEventTable.GetFirstEventType());

                        WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.EVENT_PAGE_OPEN);
                    }
                }
                break;

            default:
                if(m_curTutorialInfo.nextStepID.Equals(0))
                    InitTutorialCode(tutCode);
                else
                {
                    switch(StateManager.instance.m_curStateType)
                    {
                        case STATE_TYPE.STATE_ROOM:
                        case STATE_TYPE.STATE_VILLAGE:
                            if(UserInfo.instance.CheckLevelUp == false)
                                StartSequenceTrigger();
                            break;

                        default:
                            StartSequenceTrigger();
                            break;
                    }
                }

                break;
        }
    }

    #endregion

    #region Release

    public void ReleaseTutorialState(bool isStepRelease = true)
    {
        if (isStepRelease)
            m_curTutorialStep = 0;
        m_curTutorialCode = 0;

        m_curTutorialInfo = null;
        m_curTutorialTable = null;

        ReleaseWindow();
    }

    #endregion
}
