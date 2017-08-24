using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#region UITutorialTalk

/// <summary>
/// <para>name : UITutorialTalk</para>
/// <para>describe : 튜토리얼 대사 시퀀스 윈도우.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UITutorialTalk : UITutorial
{
    private const float NPC_PLAY_TIME = 0.6f;
    private const float DIALOGUE_PLAY_TIME = 0.3f;

    private UIScenarioNpc[] m_uiScenarioNpcGroup = null;
    private UITutorialCloseCB m_uiTutorialCompleteCB = null;

    private GameObject m_skipButton = null;

    private TutorialInfo m_curTutorialInfo = null;

    private bool m_isActive = true;

    public override void Init()
    {
        #region NPC
        m_uiScenarioNpcGroup = new UIScenarioNpc[(int)NPC_POSITION.EMPTY];
        string[] npcPathArray = { "Anchor-Bot/Left_Npc", "Anchor-Bot/Right_Npc", "Anchor-Bot/Center_Npc" };
        for(int i = 0; i < m_uiScenarioNpcGroup.Length; i++)
        {
            m_uiScenarioNpcGroup[i] = new UIScenarioNpc((NPC_POSITION)i,
                m_transform.FindChild(npcPathArray[i]).gameObject, OnTutorialTalkNext);
            m_uiScenarioNpcGroup[i].ResetToBeginning();
        }
        #endregion

        #region Button
        m_skipButton = m_transform.FindChild("Anchor-TopRight/Skip_Button").gameObject;
        m_skipButton.GetComponent<UIEventListener>().onTutorialClick = OnSkip;

        OnOffSkipButton(false, true);
        #endregion

        m_isInitComplete = true;
    }

    public override void OpenUI(TutorialInfo info, UITutorialCloseCB completeCB = null)
    {
        m_uiTutorialCompleteCB = completeCB;

        SetActive(true);

        for(int i = 0; i < m_uiScenarioNpcGroup.Length; i++)
            m_uiScenarioNpcGroup[i].ResetToBeginning();

        PlayScenario(info);
        OnOffSkipButton(info.isSkipEnable, !info.isSkipEnable);
    }

    #region Play

    private void PlayScenario(TutorialInfo info)
    {
        m_isActive = true;

        m_curTutorialInfo = info;

        float delayTime = 0;
        for(int i = 0; i < m_uiScenarioNpcGroup.Length; i++)
        {
            bool isView = (i.Equals((int)NPC_POSITION.CENTER) || i.Equals((int)NPC_POSITION.EMPTY)) ?
                true : m_uiScenarioNpcGroup[i].CheckViewNpc;
            bool isExists = (i.Equals((int)NPC_POSITION.CENTER) || i.Equals((int)NPC_POSITION.EMPTY)) ? 
                true : !info.npcGroup[i].npcBody.Equals("0");

            if(isExists)
                m_uiScenarioNpcGroup[i].SetNpc(info.npcGroup[i]);
            m_uiScenarioNpcGroup[i].ViewNpc(isExists, isExists ? isView : false);

            delayTime = Mathf.Max(isView ?
                isExists ? 0 : NPC_PLAY_TIME :
                isExists ? NPC_PLAY_TIME : 0,
                delayTime);
        }

        for(int i = 0; i < m_uiScenarioNpcGroup.Length; i++)
            m_uiScenarioNpcGroup[i].SetNarraterNpc(i.Equals((int)info.GetNpcPosType));

        m_uiScenarioNpcGroup[(int)info.GetNpcPosType].SetDialogue(
            info.npcPosType.Equals(NPC_POSITION.EMPTY) ? "" : info.GetNpcNameString(info.npcGroup[(int)info.GetNpcPosType].npcBody),
            info.GetNpcScriptString());
        m_uiScenarioNpcGroup[(int)info.GetNpcPosType].SetNextButton(true);
    }

    private IEnumerator NextScenario(TutorialInfo info)
    {
        m_isActive = true;

        bool isEquals = m_curTutorialInfo.GetNpcPosType.Equals(info.GetNpcPosType);
        float delayTime = isEquals ? 0 : DIALOGUE_PLAY_TIME;

        if(isEquals == false && m_uiScenarioNpcGroup[(int)m_curTutorialInfo.GetNpcPosType].CheckViewDialogue)
        {
            m_uiScenarioNpcGroup[(int)m_curTutorialInfo.GetNpcPosType].ViewDialogue(false);
            m_uiScenarioNpcGroup[(int)m_curTutorialInfo.GetNpcPosType].SetNextButton(false);
        }

        float timer = delayTime + Time.realtimeSinceStartup;
        while(timer > Time.realtimeSinceStartup)
            yield return null;

        StartCoroutine("PlayScenario", info);
    }

    #endregion

    #region Close

    public override void CloseUI(bool isActiveCB = false)
    {
        m_isActive = false;
        SetActive(false);

        if(isActiveCB && m_uiTutorialCompleteCB != null)
            m_uiTutorialCompleteCB();
        m_uiTutorialCompleteCB = null;
    }

    private IEnumerator Release()
    {
        m_isActive = false;

        for(int i = 0; i < m_uiScenarioNpcGroup.Length; i++)
        {
            m_uiScenarioNpcGroup[i].SetNarraterNpc(false);
            m_uiScenarioNpcGroup[i].Release();
        }

        float timer = NPC_PLAY_TIME + Time.realtimeSinceStartup;
        while(timer > Time.realtimeSinceStartup)
            yield return null;

        CloseUI(true);

        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: Tutorial.instance.EndCurrentTutorialCode(true); break;
            case TUTORIAL_TYPE.TYPE_BUILDING: Tutorial.instance.EndSeqBuildingTrigger(true); break;
        }
    }

    #endregion

    #region Button

    private void OnSkip(GameObject obj)
    {
        Util.ButtonAnimation(obj);
        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: Tutorial.instance.OpenTutorialMessageBox(352, 281, MSGBOX_TYPE.YESNO, OnSkipButtonCB); break;
            case TUTORIAL_TYPE.TYPE_BUILDING:
                StateManager.instance.m_curState.OnTutorialSkip();
                Tutorial.instance.ReleaseSeqBuildingTrigger();
                break;
        }
    }

    private void OnTutorialTalkNext(GameObject obj)
    {
        TutorialInfo nextTutorialInfo = null;
        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC:
                {
                    nextTutorialInfo = WorldManager.instance.m_dataManager.m_tutorialData.GetTutorialInfo(m_curTutorialInfo.step, m_curTutorialInfo.nextStepID);
                    if (nextTutorialInfo != null && nextTutorialInfo.TriggerType.Equals(TUTORIAL_TRIGGER_TYPE.TYPE_TALK))
                    {
                        Tutorial.instance.UpdateNextTutorialCode(nextTutorialInfo.index);
                        StartCoroutine("NextScenario", nextTutorialInfo);
                    }

                    else
                        StartCoroutine("Release");
                }
                break;

            case TUTORIAL_TYPE.TYPE_BUILDING:
                {
                    nextTutorialInfo = WorldManager.instance.m_dataManager.m_tutorialData.GetTutorialBuildingInfo(m_curTutorialInfo.step, m_curTutorialInfo.nextStepID);
                    if (nextTutorialInfo != null && nextTutorialInfo.TriggerType.Equals(TUTORIAL_TRIGGER_TYPE.TYPE_TALK))
                    {
                        Tutorial.instance.UpdateNextTutorialBuildingInfo(nextTutorialInfo.index);
                        StartCoroutine("NextScenario", nextTutorialInfo);
                    }

                    else
                        StartCoroutine("Release");
                }
                break;
        }
    }

    private void OnSkipButtonCB(bool isYes)
    {
        StateManager.instance.m_curState.OnTutorialSkip();

        if (isYes)
        {
            switch (Tutorial.instance.TutorialType)
            {
                case TUTORIAL_TYPE.TYPE_BASIC: Tutorial.instance.TutorialSkip(); break;
                case TUTORIAL_TYPE.TYPE_BUILDING: Tutorial.instance.ReleaseSeqBuildingTrigger(); break;
            }
        }
    }

    #endregion

    #region Util

    private void OnOffSkipButton(bool b, bool isResetToBeginning = false)
    {
        iTween.Stop(m_skipButton);

        int dirNum = b ? 100000077 : 100000078;

        CDirection cDirection = m_skipButton.GetComponent<CDirection>();
        if(isResetToBeginning)
            cDirection.ResetToBeginning(dirNum);
        else
            cDirection.SetInit(dirNum, true);
    }

    #endregion

    public override void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
}

#endregion

#region UITutorialMiniTalk

/// <summary>
/// <para>name : UITutorialMiniTalk</para>
/// <para>describe : 튜토리얼 작은 대화 시퀀스 윈도우.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UITutorialMiniTalk : UITutorial
{
    private UIAnchor m_uiAnchor = null;

    private Transform m_talkWindow = null;

    private UISprite m_npcSprite = null;
    private UILabel m_textLabel = null;

    private GameObject m_nextButton = null;
    private List<GameObject> m_guideObjList = null;

    private UITutorialCloseCB m_uiTutorialCompleteCB = null;

    public override void Init()
    {
        m_uiAnchor = m_transform.FindChild("Anchor").GetComponent<UIAnchor>();
        m_talkWindow = m_transform.FindChild("Anchor/Talk_Window");

        m_npcSprite = m_talkWindow.FindChild("Npc").GetComponent<UISprite>();
        m_textLabel = m_talkWindow.FindChild("Text_Label").GetComponent<UILabel>();

        m_nextButton = m_talkWindow.FindChild("Next_Button").gameObject;
        m_nextButton.GetComponent<UIEventListener>().onTutorialClick = OnNextButton;

        m_isInitComplete = true;
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
            CloseUI(true);
    }

    public override void OpenUI(TutorialInfo info, UITutorialCloseCB completeCB = null)
    {
        m_uiTutorialCompleteCB = completeCB;
        UpdateWindow(info);
    }

    public override void CloseUI(bool isActiveCB = false)
    {
        m_talkWindow.gameObject.SetActive(false);
        SetActive(false);

        if(m_guideObjList != null)
        {
            for(int i = 0; i < m_guideObjList.Count; i++)
            {
                if(m_guideObjList[i] != null)
                    Destroy(m_guideObjList[i]);
            }

            m_guideObjList = null;
        }

        if(isActiveCB && m_uiTutorialCompleteCB != null)
            m_uiTutorialCompleteCB();
        m_uiTutorialCompleteCB = null;
    }

    public override void UpdateCallback(UITutorial.UITutorialCloseCB completeCB)
    {
        m_uiTutorialCompleteCB = completeCB;
    }

    #region Update

    private void UpdateWindow(TutorialInfo info)
    {
        SetActive(true);

        m_uiAnchor.side = info.attachAnchorType;
        m_talkWindow.transform.localPosition = info.npcPopupPos;

        m_talkWindow.gameObject.SetActive(true);

        Hashtable hash = new Hashtable();
        hash.Add("amount", new Vector3(0.05f, 0.05f, 0f));
        hash.Add("time", 1f);
        hash.Add("ignoretimescale", true);
        iTween.PunchScale(m_talkWindow.gameObject, hash);

        #region NPC
        string spriteName = "";
        for(int i = 0; i < info.npcGroup.Length; i++)
        {
            if(info.npcGroup[i].npcBody.Equals("0") == false)
                spriteName = info.npcGroup[i].npcBody;
            else if(info.npcGroup[i].npcFace.Equals("0") == false)
                spriteName = info.npcGroup[i].npcFace;
            else
                continue;

            break;
        }

        m_npcSprite.spriteName = spriteName;
        m_npcSprite.MakePixelPerfect();
        #endregion

        #region Text
        m_textLabel.text = info.GetNpcScriptString();
        #endregion

        SoundManager.instance.PlayAudioClip("UI_PopupOpen");
    }

    #endregion

    #region Button

    private void OnNextButton(GameObject obj)
    {
        CloseUI(true);
    }

    #endregion

    #region Guide_Object

    public Transform AddGuideObject(GameObject obj)
    {
        GameObject createObj = Instantiate(obj) as GameObject;
        createObj.transform.parent = transform;
        createObj.transform.localPosition = Vector3.zero;
        createObj.transform.localScale = Vector3.one;

        if(m_guideObjList == null)
            m_guideObjList = new List<GameObject>();
        m_guideObjList.Add(createObj);

        return createObj.transform;
    }

    #endregion

    public override void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }

}

#endregion

#region UITutorialSelect

/// <summary>
/// <para>name : UITutorialSelect</para>
/// <para>describe : 튜토리얼 선택 시퀀스 윈도우.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UITutorialSelect : UITutorial
{
    private UIAnchor m_uiAnchor = null;

    private Transform m_objTransform = null;
    private UI2DSprite m_targetSprite = null;
    private CDirection[] m_cDirectionArray = null;

    private List<GameObject> m_guideObjList = null;

    private UIEventListener m_targetEventListener = null;

    private UITutorialCloseCB m_uiTutorialCompleteCB = null;

    private TutorialInfo m_curTutorialInfo = null;

    public override void Init()
    {
        m_uiAnchor = m_transform.FindChild("Anchor").GetComponent<UIAnchor>();

        m_objTransform = m_transform.FindChild("Anchor/TargetObject");

        GameObject targetObj = m_objTransform.FindChild("Target").gameObject;
        m_targetSprite = targetObj.GetComponent<UI2DSprite>();
        m_targetEventListener = targetObj.GetComponent<UIEventListener>();

        string[] spritePathArray = { "Target", "Left", "Right", "Top", "Bot" };
        m_cDirectionArray = new CDirection[spritePathArray.Length];
        for(int i = 0; i < spritePathArray.Length; i++)
            m_cDirectionArray[i] = m_objTransform.FindChild(spritePathArray[i]).GetComponent<CDirection>();
        
        m_isInitComplete = true;
    }

    public override void OpenUI(TutorialInfo info, UITutorialCloseCB completeCB = null)
    {
        m_curTutorialInfo = info;
        m_uiTutorialCompleteCB = completeCB;

        SetActive(true);        
        
        m_uiAnchor.side = info.attachAnchorType;
        
        m_objTransform.gameObject.SetActive(true);
        m_objTransform.localPosition = GetPosition(info.highlightPos);

        m_targetSprite.width = GetSizeValue((int)info.highlighSize.x);
        m_targetSprite.height = GetSizeValue((int)info.highlighSize.y);

        switch(info.TriggerType)
        {
            case TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH_VIEW:
                m_targetSprite.GetComponent<Collider>().enabled = true;
                m_targetEventListener.onTutorialClick = OnTouchViewClick;
                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH_CALLBACK:
                m_targetSprite.GetComponent<Collider>().enabled = true;
                m_targetEventListener.onTutorialClick = OnTouchCallbackClick;
                break;

            default:
                m_targetSprite.GetComponent<Collider>().enabled = false;
                m_targetEventListener.onTutorialClick = null;
                break;
        }

        OnOffSprite(true);
    }

    public void UpdateWindow(Vector3 position, Vector2 size)
    {
        m_objTransform.position = position;

        m_targetSprite.width = GetSizeValue((int)size.x);
        m_targetSprite.height = GetSizeValue((int)size.y);
    }

    public override void CloseUI(bool isActiveCB = false)
    {
        OnOffSprite(false, true);

        if(m_guideObjList != null)
        {
            for(int i = 0; i < m_guideObjList.Count; i++)
            {
                if(m_guideObjList[i] != null)
                    Destroy(m_guideObjList[i]);
            }

            m_guideObjList = null;
        }

        m_objTransform.gameObject.SetActive(false);
        SetActive(false);

        if(isActiveCB && m_uiTutorialCompleteCB != null)
            m_uiTutorialCompleteCB();
        m_uiTutorialCompleteCB = null;
    }

    #region Callback

    private void OnTouchViewClick(GameObject obj)
    {
        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: Tutorial.instance.EndCurrentTutorialCode(true); break;
            case TUTORIAL_TYPE.TYPE_BUILDING: Tutorial.instance.EndSeqBuildingTrigger(true); break;
        }
    }

    private void OnTouchCallbackClick(GameObject obj)
    {
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
    }

    private void OnTutFailCallback(MSGBOX_TYPE type, bool isYes)
    {
        if (isYes) WorldManager.instance.RestartApplication();
        else Util.ApplicationQuit();
    }

    #endregion

    #region Util

    private Vector3 GetPosition(Vector3 pos)
    {
        return new Vector3(Mathf.RoundToInt(pos.x) + 0.5f, Mathf.RoundToInt(pos.y) + 0.5f, 0);
    }

    private int GetSizeValue(int size)
    {
        return (size % 2).Equals(0) ? size : size + 1;
    }

    private void OnOffSprite(bool b, bool isInstantly = false)
    {
        int index = b ? 100000154 : 100000155;
        for(int i = 0; i < m_cDirectionArray.Length; i++)
        {
            if(isInstantly) m_cDirectionArray[i].ResetToBeginning(index);
            else m_cDirectionArray[i].SetInit(index, true);
        }
    }

    #endregion

    #region Guide_Object

    public Transform AddGuideObject(GameObject obj)
    {
        GameObject createObj = Instantiate(obj) as GameObject;
        createObj.transform.parent = transform;
        createObj.transform.localPosition = Vector3.zero;
        createObj.transform.localScale = Vector3.one;

        if(m_guideObjList == null)
            m_guideObjList = new List<GameObject>();
        m_guideObjList.Add(createObj);

        return createObj.transform;
    }

    #endregion

    public UIEventListener GetEventListener
    {
        get { return m_targetEventListener; }
    }

    public override void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
}

#endregion

#region UITutorialFingerView

/// <summary>
/// <para>name : UITutorialFingerView</para>
/// <para>describe : 튜토리얼 손가락 시퀀스 윈도우.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UITutorialFingerView : UITutorial
{
    private UIAnchor m_uiAnchor = null;

    private Transform m_objTransform = null;
    private Transform m_icon = null;

    private UITutorialCloseCB m_uiTutorialCompleteCB = null;

    public override void Init()
    {
        m_uiAnchor = m_transform.FindChild("Anchor").GetComponent<UIAnchor>();

        m_objTransform = m_transform.FindChild("Anchor/FingerObject");
        m_icon = m_objTransform.FindChild("Icon");
        
        m_isInitComplete = true;
    }

    public override void OpenUI(TutorialInfo info, UITutorialCloseCB completeCB = null)
    {
        m_uiTutorialCompleteCB = completeCB;

        SetActive(true);        
        
        m_uiAnchor.side = info.attachAnchorType;
        m_objTransform.localPosition = new Vector3(3000.0f, 3000.0f, 0);

        m_objTransform.gameObject.SetActive(true);
        m_objTransform.localPosition = info.fingerPos;

        m_icon.localRotation = Quaternion.Euler(info.fingerAngle * Vector3.forward);
    }

    public override void CloseUI(bool isActiveCB = false)
    {
        m_objTransform.gameObject.SetActive(false);
        SetActive(false);

        if(isActiveCB && m_uiTutorialCompleteCB != null)
            m_uiTutorialCompleteCB();
        m_uiTutorialCompleteCB = null;
    }

    public override void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
}

#endregion

#region UITutorialRewardPopup

/// <summary>
/// <para>name : UITutorialRewardPopup</para>
/// <para>describe : 튜토리얼 보상창 윈도우.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UITutorialRewardPopup : UITutorial
{
    private Transform m_window = null;

    private UILabel[] m_labelArray = null;
    private enum LABEL_TYPE
    {
        TYPE_NONE = -1,

        TYPE_NAME,
        TYPE_VALUE,
        TYPE_TEXT,

        TYPE_END,
    }

    private UISprite[] m_spriteArray = null;
    private enum SPRITE_TYPE
    {
        TYPE_NONE = -1,

        TYPE_GEMS,
        TYPE_ITEM,
        TYPE_MATERIAL,

        TYPE_END,
    }

    private UITutorialCloseCB m_uiTutorialCompleteCB = null;

    public override void Init()
    {
        m_window = m_transform.FindChild("Anchor-Center/Window");

        InitLabel();
        InitSprite();
        InitButton();
        
        m_isInitComplete = true;
    }

    public override void OpenUI(TutorialInfo info, UITutorial.UITutorialCloseCB completeCB = null)
    {
        UpdateInfo(info.cliendRewardInfo);
        SetActive(true);

        Hashtable hash = new Hashtable();
        hash.Add("amount", new Vector3(0.05f, 0.05f, 0f));
        hash.Add("time", 1f);
        hash.Add("ignoretimescale", true);
        iTween.PunchScale(m_window.gameObject, hash);

        SoundManager.instance.PlayAudioClip("UI_PopupOpen");
    }

    public override void CloseUI(bool isActiveCB = false)
    {
        SetActive(false);

        if(isActiveCB && m_uiTutorialCompleteCB != null)
            m_uiTutorialCompleteCB();
        m_uiTutorialCompleteCB = null;
    }

    #region Init

    private void InitLabel()
    {
        string[] labelPathArray = { "Item_Title_Label", "Item_Value_Label", "Help_Label" };

        m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];
        for(int i = 0; i < labelPathArray.Length; i++)
        {
            m_labelArray[i] = m_window.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            m_labelArray[i].text = "";
        }
    }

    private void InitSprite()
    {
        string[] spritePathArray = { "Goods_Icon_Group/Icon", "Item_Icon_Group/Icon", "Material_Icon_Group/Icon" };

        m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];
        for(int i = 0; i < spritePathArray.Length; i++)
            m_spriteArray[i] = m_window.FindChild(spritePathArray[i]).GetComponent<UISprite>();
    }

    private void InitButton()
    {
        UIEventListener eventListener = m_window.FindChild("Reward_Button").GetComponent<UIEventListener>();
        if(eventListener != null)
            eventListener.onTutorialClick = OnOkButton;
    }

    #endregion

    #region Update

    private void UpdateInfo(TutorialClientRewardInfo rewardInfo)
    {
        switch(rewardInfo.rewardType)
        {
            case TUTORIAL_CLIENT_REWARD_TYPE.TYPE_GEMS:
                GEMS_TYPE gemsType = Util.GetGoodsTypeByIndex((int)rewardInfo.rewardIndex);

                UpdateSprite(gemsType);
                m_labelArray[(int)LABEL_TYPE.TYPE_NAME].text = Util.GetGoodsNameString(gemsType);
                break;

            default:
                UpdateInfo(rewardInfo.rewardIndex);
                break;
        }

        m_labelArray[(int)LABEL_TYPE.TYPE_TEXT].text = rewardInfo.descString;
        m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = string.Format("{0}{1}", rewardInfo.rewardCount, Str.instance.Get(600028));
    }

    private void UpdateInfo(uint rewardIndex)
    {
        ITEM_TYPE type = Util.ParseItemMainType(rewardIndex);

        string spriteName = "";
        string itemName = "";
        WorldManager.instance.GetItemTextInfo(rewardIndex, out itemName, out spriteName);

        UpdateSprite(type, spriteName);
        m_labelArray[(int)LABEL_TYPE.TYPE_NAME].text = itemName;
    }

    private void UpdateSprite(GEMS_TYPE type)
    {
        for(int i = 0; i < m_spriteArray.Length; i++)
            m_spriteArray[i].gameObject.SetActive(false);

        UISprite sprite = m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS];

        sprite.gameObject.SetActive(true);
        sprite.spriteName = Util.GetGoodsIconName(type);
        sprite.MakePixelPerfect();
    }

    private void UpdateSprite(ITEM_TYPE type, string spriteName)
    {
        for(int i = 0; i < m_spriteArray.Length; i++)
            m_spriteArray[i].gameObject.SetActive(false);

        bool isMaterialType = Util.CheckAtlasByItemType(type);
        UISprite sprite = m_spriteArray[isMaterialType ? (int)SPRITE_TYPE.TYPE_MATERIAL : (int)SPRITE_TYPE.TYPE_ITEM];

        sprite.gameObject.SetActive(true);
        sprite.spriteName = spriteName;
        sprite.MakePixelPerfect();
    }

    #endregion

    #region Button

    private void OnOkButton(GameObject obj)
    {
        Util.ButtonAnimation(obj);
        CloseUI(true);

        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: Tutorial.instance.EndCurrentTutorialCode(true); break;
            case TUTORIAL_TYPE.TYPE_BUILDING: Tutorial.instance.EndSeqBuildingTrigger(true); break;
        }
    }

    #endregion

    public override void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
}

#endregion

#region UITutorialDelay

/// <summary>
/// <para>name : UITutorialDelay</para>
/// <para>describe : 튜토리얼 딜레이 시퀀스 윈도우.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UITutorialDelay : UITutorial
{
    private UITutorialCloseCB m_uiTutorialCompleteCB = null;

    public override void Init()
    {
        m_isInitComplete = true;
    }

    public override void OpenUI(TutorialInfo info, UITutorialCloseCB completeCB = null)
    {
        m_uiTutorialCompleteCB = completeCB;

        SetActive(true);
        StartCoroutine("StartDelayTime", info.delayTime);
    }

    private IEnumerator StartDelayTime(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);

        switch (Tutorial.instance.TutorialType)
        {
            case TUTORIAL_TYPE.TYPE_BASIC: Tutorial.instance.EndCurrentTutorialCode(true); break;
            case TUTORIAL_TYPE.TYPE_BUILDING: Tutorial.instance.EndSeqBuildingTrigger(true); break;
        }

        CloseUI();
    }

    public override void CloseUI(bool isActiveCB = false)
    {
        StopAllCoroutines();
        SetActive(false);

        if(isActiveCB && m_uiTutorialCompleteCB != null)
            m_uiTutorialCompleteCB();
        m_uiTutorialCompleteCB = null;
    }

    public override void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
}

#endregion

#region UITutorialMsgBox

public class UITutorialMsgBox : UITutorial
{
    private Transform m_window = null;

    private UILabel[] m_labelArray = null;
    private enum LABEL_TYPE
    {
        TYPE_NONE = -1,

        TYPE_TITLE,
        TYPE_TEXT,

        TYPE_END,
    }

    private GameObject[] m_buttonArray = null;
    private enum BUTTON_TYPE
    {
        TYPE_NONE = -1,

        TYPE_YES,
        TYPE_NO,
        TYPE_OK,

        TYPE_END,
    }

    private UITutorial.UITutorialMsgCB m_uiTutorialMsgCB = null;

    public override void Init()
    {
        m_window = m_transform.FindChild("Anchor-Center/Window");

        InitLabel();
        InitButton();
        
        m_isInitComplete = true;
    }

    public override void OpenUI(int title, int msg, MSGBOX_TYPE type = MSGBOX_TYPE.OK, UITutorial.UITutorialMsgCB msgCB = null)
    {
        #region Update
        m_uiTutorialMsgCB = msgCB;

        m_labelArray[(int)LABEL_TYPE.TYPE_TITLE].text = Str.instance.Get(title);
        m_labelArray[(int)LABEL_TYPE.TYPE_TEXT].text = Str.instance.Get(msg);

        switch(type)
        {
            case MSGBOX_TYPE.OK:
                m_buttonArray[(int)BUTTON_TYPE.TYPE_YES].SetActive(false);
                m_buttonArray[(int)BUTTON_TYPE.TYPE_NO].SetActive(false);
                m_buttonArray[(int)BUTTON_TYPE.TYPE_OK].SetActive(true);
                break;
            case MSGBOX_TYPE.YESNO:
                m_buttonArray[(int)BUTTON_TYPE.TYPE_YES].SetActive(true);
                m_buttonArray[(int)BUTTON_TYPE.TYPE_NO].SetActive(true);
                m_buttonArray[(int)BUTTON_TYPE.TYPE_OK].SetActive(false);
                break;
        }

        #endregion

        SetActive(true);

        Hashtable hash = new Hashtable();
        hash.Add("amount", new Vector3(0.05f, 0.05f, 0f));
        hash.Add("time", 1f);
        hash.Add("ignoretimescale", true);
        iTween.PunchScale(m_window.gameObject, hash);

        SoundManager.instance.PlayAudioClip("UI_PopupOpen");
    }

    public override void CloseUI(bool isActiveCB = false)
    {
        SetActive(false);
    }

    #region Init

    private void InitLabel()
    {
        string[] labelPathArray = { "Title", "Text" };

        m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];
        for(int i = 0; i < labelPathArray.Length; i++)
        {
            m_labelArray[i] = m_window.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            m_labelArray[i].text = "";
        }
    }

    private void InitButton()
    {
        string[] buttonPathArray = { "ButtonYes", "ButtonNo", "ButtonOK" };
        UIEventListener.VoidDelegate[] delArray = { OnYesButton, OnNoButton, OnOkButton };

        m_buttonArray = new GameObject[(int)BUTTON_TYPE.TYPE_END];
        for(int i = 0; i < buttonPathArray.Length; i++)
        {
            m_buttonArray[i] = m_window.FindChild(buttonPathArray[i]).gameObject;
            m_buttonArray[i].GetComponent<UIEventListener>().onTutorialClick = delArray[i];
        }
    }

    #endregion

    #region Button

    private void OnOkButton(GameObject obj)
    {
        Util.ButtonAnimation(obj);

        if(m_uiTutorialMsgCB != null)
            m_uiTutorialMsgCB(true);
        CloseUI();
    }

    private void OnYesButton(GameObject obj)
    {
        Util.ButtonAnimation(obj);

        if(m_uiTutorialMsgCB != null)
            m_uiTutorialMsgCB(true);
        CloseUI();
    }

    private void OnNoButton(GameObject obj)
    {
        Util.ButtonAnimation(obj);

        if(m_uiTutorialMsgCB != null)
            m_uiTutorialMsgCB(false);
        CloseUI();
    }

    #endregion

    public override void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
}

#endregion

public class UITutorial : MonoBehaviour 
{
    public delegate void UITutorialCloseCB();
    public delegate void UITutorialMsgCB(bool isSwitch);

    public Transform m_transform = null;

    public bool m_isInitComplete = false;

    void Awake()
    {
        m_transform = gameObject.transform;
        Init();
    }

    public virtual void Init()
    {
    }

    public virtual void OpenUI(TutorialInfo info, UITutorialCloseCB completeCB = null)
    {
    }

    public virtual void OpenUI(int title, int msg, MSGBOX_TYPE type = MSGBOX_TYPE.OK, UITutorialMsgCB msgCB = null)
    {
    }

    public virtual void CloseUI(bool isActiveCB = false)
    {
    }

    public virtual void UpdateCallback(UITutorialCloseCB completeCB)
    {
    }

    public virtual void SetActive(bool b)
    {
    }

    public bool CheckActiveUITutorial
    {
        get { return gameObject.activeSelf; }
    }
}
