using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if USER_SERVER
using NetWork;
#endif

#region UILevelUpScenario

/// <summary>
/// <para>name : UILevelUpScenario</para>
/// <para>describe : 유저 레벨업 시 시나리오 출력.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UILevelUpScenario
{
    private GameObject m_obj = null;
    private Transform m_transform = null;

    private UISprite m_background = null;
    private UITexture m_conceptTx = null;

    private UILabel m_episodeLabel = null;
    private UILabel m_titleLabel = null;

    public UILevelUpScenario() { }
    public UILevelUpScenario(GameObject obj)
    {
        m_obj = obj;
        m_transform = obj.transform;

        m_background = m_transform.FindChild("Anchor-Center/Background").GetComponent<UISprite>();
        m_conceptTx = m_transform.FindChild("Anchor-Center/Concept").GetComponent<UITexture>();

        m_episodeLabel = m_transform.FindChild("Anchor-Center/Episode").GetComponent<UILabel>();
        m_titleLabel = m_transform.FindChild("Anchor-Center/Title").GetComponent<UILabel>();

        Release();
    }

    public IEnumerator TitleTweenStart(UserLevelInfo info)
    {
        SoundManager.instance.StopBGM();

        Release();

        m_titleLabel.text = info.scenarioTitleText;
        m_conceptTx.mainTexture = GetTexture();

        TweenAlpha.Begin(m_background.gameObject, 0.5f, 1.0f);
        yield return new WaitForSeconds(1.0f);
		
		TweenAlpha.Begin(m_episodeLabel.gameObject, 0.5f, 1.0f);
        TweenAlpha.Begin(m_titleLabel.gameObject, 0.5f, 1.0f, 0.8f);

        SoundManager.instance.PlayAudioClip("Quest_Accept");

        yield return new WaitForSeconds(2.5f);

		TweenAlpha.Begin(m_background.gameObject, 0.5f, 0.5f, 0.6f);
		TweenAlpha.Begin(m_conceptTx.gameObject, 0.15f, 1.0f);

		TweenAlpha.Begin(m_episodeLabel.gameObject, 0.5f, 0.0f, 0.1f);
        TweenAlpha.Begin(m_titleLabel.gameObject, 0.5f, 0.0f);

        yield return new WaitForSeconds(0.65f);

        SoundManager.instance.PlayBGM("BGM_Story");
        ScenarioWindow.instance.StartScenario(info.scenarioGroupIndex, false, 
                                            new ScenarioWindow.ScenarioCompleteCB(UserInfo.instance.LevelUpScenarioTweenComplete));
    }

    public IEnumerator TitleTweenRelease()
    {
        SoundManager.instance.StopBGM();
        
        StateManager.instance.m_curState.OnOffGUIState(true);

		TweenAlpha.Begin(m_background.gameObject, 0.4f, 0.0f);
		TweenAlpha.Begin(m_conceptTx.gameObject, 0.4f, 0.0f);

        yield return new WaitForSeconds(0.6f);

        if (WorldManager.instance.CheckMemoryInfoExists(WORLD_MEMORY_INFO.REG_PHONE_ENABLE))
        {
            if (WorldManager.instance.m_player.m_isRegPhone)
                CertificationPhonePopup.instance.OpenPopup();

            WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.REG_PHONE_ENABLE);
        }

        else if (WorldManager.instance.CheckMemoryInfoExists(WORLD_MEMORY_INFO.SURVEY_OPEN))
        {
            SdkManager.instance.OnSurveyOpen();
            WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.SURVEY_OPEN);
        }

        switch (StateManager.instance.m_curStateType)
        {
            case STATE_TYPE.STATE_ROOM: SoundManager.instance.PlayBGM("BGM_Room"); break;
            case STATE_TYPE.STATE_VILLAGE: SoundManager.instance.PlayBGM("BGM_World1"); break;
        }

        Release();
        SetActive(false);
    }

    #region

    private Texture2D GetTexture()
    {
        string texturePath = "";
        STATE_TYPE type = StateManager.instance.m_curStateType;

        if( type.Equals(STATE_TYPE.STATE_ROOM)) texturePath = "[Textures]/[ScreenOut]/Tex_LoadingScene_04";
        else if( type.Equals(STATE_TYPE.STATE_VILLAGE)) texturePath = "[Textures]/[ScreenOut]/Tex_LoadingScene_03";
        else return null;

        return AssetBundleEx.Load<Texture2D>(texturePath);
    }

    #endregion

    private void Release()
    {
        m_background.alpha = 0.0f;

        m_conceptTx.alpha = 0.0f;
        m_conceptTx.mainTexture = null;

        m_episodeLabel.alpha = 0.0f;
        m_titleLabel.alpha = 0.0f;
    }
    
    public bool CheckUIExists
    {
        get { return m_transform != null; }
    }

    public void SetActive(bool b)
    {
        m_obj.SetActive(b);
    }
}

#endregion

#region UIUserLevelUp

/// <summary>
/// <para>name : UIUserLevelUp</para>
/// <para>describe : 유저 레벨업 UI.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UIUserLevelUp
{
    private UIUserLevelUnlockView m_uiUnlockView = null;

    private GameObject m_obj = null;
    private Transform m_transform = null;

    private UILabel[] m_labelArray = null;
    private enum LABEL_TYPE
    {
        TYPE_NONE = -1,

        TYPE_LEVEL,
        TYPE_GOODS_VALUE,

        TYPE_END,
    }

    private UISprite[] m_spriteArray = null;
    private enum SPRITE_TYPE
    {
        TYPE_NONE = -1,

        TYPE_ITEM,
        TYPE_GOODS,

        TYPE_END,
    }

    private GameObject[] m_objArray = null;
    private enum OBJECT_TYPE
    {
        TYPE_NONE = -1,

        TYPE_TITLE,
        TYPE_REWARD,
        TYPE_ITEM_GROUP,
        TYPE_GOODS_GROUP,

        TYPE_EFFECT,
        TYPE_EFFECT_CREATE,

        TYPE_END,
    }

    private UserLevelInfo m_levelInfo = null;
    private bool isOpenComplete = false;

    public UIUserLevelUp()
    {
    }

    public UIUserLevelUp(GameObject obj)
    {
        m_obj = obj;
        m_transform = obj.transform.FindChild("Anchor_Center/Window");

        #region Label
        m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];
        string[] labelPathArray = { "Level_Group/Level", "Reward_Group/Gems_Group/Value" };
        for(int i = 0; i < labelPathArray.Length; i++)
            m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
        #endregion

        #region Sprite
        m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];
        string[] spritePathArray = { "Reward_Group/Item_Group/Item_Icon_Group/Icon", "Reward_Group/Gems_Group/Icon" };
        for(int i = 0; i < spritePathArray.Length; i++)
            m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
        #endregion

        #region Object
        string[] objPathArray = { "Title_Group", "Reward_Group", "Reward_Group/Item_Group", "Reward_Group/Gems_Group" };
        m_objArray = new GameObject[(int)OBJECT_TYPE.TYPE_END];

        for(int i = 0; i < objPathArray.Length; i++)
            m_objArray[i] = m_transform.FindChild(objPathArray[i]).gameObject;
        m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT] = AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_LevelUp");
        #endregion

        #region Callback
        m_transform.GetComponent<UIEventListener>().onTutorialClick = OnLevelUpWindowClick;
        #endregion

        OnOffTitleGroup(false, true);
        OnOffRewardGroup(false, true);
    }

    public void OpenPopup()
    {
        isOpenComplete = false;

        UpdatePopup();

        m_obj.SetActive(true);
        m_transform.gameObject.SetActive(true);

        OnOffTitleGroup(true);

        Hashtable hash = new Hashtable();
        hash.Add("amount", new Vector3(0.3f, 0.3f, 0f));
        hash.Add("time", 1f);
        hash.Add("ignoretimescale", true);
        iTween.PunchScale(m_transform.gameObject, hash);

        if(m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT] != null)
        {
            m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT_CREATE] = MonoBehaviour.Instantiate(m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT]) as GameObject;

            m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT_CREATE].transform.parent = m_obj.transform.FindChild("Anchor_Center");
            m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT_CREATE].transform.localPosition = Vector3.up * 35.0f;
            m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT_CREATE].transform.localScale = Vector3.one;
        }

        SoundManager.instance.PlayAudioClip("Eff_PlayerLevelup");
    }

    public void ClosePopup()
    {
        isOpenComplete = false;

        OnOffTitleGroup(false, true);
        OnOffRewardGroup(false, true);

        if(m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT_CREATE] != null)
            MonoBehaviour.Destroy(m_objArray[(int)OBJECT_TYPE.TYPE_EFFECT_CREATE]);

        m_obj.SetActive(false);
    }

    #region Update

    private void UpdatePopup()
    {
        int level = WorldManager.instance.m_player.m_level;
        m_levelInfo = WorldManager.instance.m_dataManager.m_userLevelData.GetLevelInfo(level);

        m_labelArray[(int)LABEL_TYPE.TYPE_LEVEL].text = level.ToString();

        UpdateReward(m_levelInfo.rewardIndex);
    }

    private void UpdateReward(uint rewardIndex)
    {
        RewardInfo[] infoArray = WorldManager.instance.m_dataManager.m_RewardListData.GetRewardList(rewardIndex);

        m_objArray[(int)OBJECT_TYPE.TYPE_GOODS_GROUP].SetActive(false);
        m_objArray[(int)OBJECT_TYPE.TYPE_ITEM_GROUP].SetActive(false);

        uint itemIndex = infoArray[0].item_Index;

        switch(Util.ParseItemMainType(itemIndex))
        {
            case ITEM_TYPE.CLOTHES:
            case ITEM_TYPE.INTERIOR:
            case ITEM_TYPE.MATERIAL:
            case ITEM_TYPE.DIRECTION:
            case ITEM_TYPE.REMODELING:
            case ITEM_TYPE.RECIPE:
            case ITEM_TYPE.OWN:
            case ITEM_TYPE.PETEGG:
            case ITEM_TYPE.PETUPGRADE:
            case ITEM_TYPE.DOGTICKET:
                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM].spriteName = WorldManager.instance.GetGUISpriteName(itemIndex);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM].MakePixelPerfect();

                m_objArray[(int)OBJECT_TYPE.TYPE_ITEM_GROUP].SetActive(true);
                break;

            default:
                m_spriteArray[(int)SPRITE_TYPE.TYPE_GOODS].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)itemIndex));
                m_spriteArray[(int)SPRITE_TYPE.TYPE_GOODS].MakePixelPerfect();

                m_labelArray[(int)LABEL_TYPE.TYPE_GOODS_VALUE].text = string.Format("{0}", infoArray[0].Count.ToString());

                m_objArray[(int)OBJECT_TYPE.TYPE_GOODS_GROUP].SetActive(true);
                break;
        }
    }

    #endregion

    #region Callback

    private void OnLevelUpWindowClick(GameObject obj)
    {
        if(isOpenComplete == false)
            return;

        for(int i = 0; i < m_levelInfo.levelUpRefArray.Length; i++)
        {
            if(m_levelInfo.levelUpRefArray[i].index.Equals(0) == false)
            {
                if(m_uiUnlockView == null || m_uiUnlockView.CheckUIExists == false)
                {
                    GameObject view = MonoBehaviour.Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UI_S108_Contents_Unlock")) as GameObject;
                    view.transform.parent = m_obj.transform;
                    view.transform.localPosition = Vector3.zero;
                    view.transform.localScale = Vector3.one;

                    m_uiUnlockView = new UIUserLevelUnlockView(view);
                }

                m_uiUnlockView.OpenPopup();
                m_transform.gameObject.SetActive(false);

                return;
            }
        }

        UserInfo.instance.CloseLevelUp();
    }

    public void TitleTweenComplete()
    {
        isOpenComplete = true;
        OnOffRewardGroup(true);
    }

    #endregion

    #region OnOff

    private void OnOffTitleGroup(bool b, bool isInstantly = false)
    {
        iTween.Stop(m_objArray[(int)OBJECT_TYPE.TYPE_TITLE]);
        CDirection cDirection = m_objArray[(int)OBJECT_TYPE.TYPE_TITLE].GetComponent<CDirection>();

        if(isInstantly) cDirection.ResetToBeginning(b ? 100000129 : 100000130);
        else cDirection.SetInit(b ? 100000129 : 100000130, true);
    }

    private void OnOffRewardGroup(bool b, bool isInstantly = false)
    {
        iTween.Stop(m_objArray[(int)OBJECT_TYPE.TYPE_REWARD]);
        CDirection cDirection = m_objArray[(int)OBJECT_TYPE.TYPE_REWARD].GetComponent<CDirection>();

        if(isInstantly) cDirection.ResetToBeginning(b ? 100000131 : 100000132);
        else cDirection.SetInit(b ? 100000131 : 100000132, true);
    }

    #endregion

    public bool CheckUIExists
    {
        get { return m_transform != null; }
    }

    public bool CheckOpenComplete
    {
        get { return isOpenComplete; }
    }

    public void SetActive(bool b)
    {
        m_obj.SetActive(b);
    }
}

#endregion

#region UIUserLevelUnlockView

/// <summary>
/// <para>name : UIUserLevelUnlockView</para>
/// <para>describe : 레벨 업 언락 정보.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UIUserLevelUnlockView
{
    private GameObject m_obj = null;
    private Transform m_transform = null;

    public UIPanel m_scrollPanel = null;
    public UIScrollView m_scrollView = null;
    public UIGrid m_scrollGrid = null;

    #region UIScrollItem
    public class UIScrollItem
    {
        private GameObject m_obj = null;
        private Transform m_transform = null;

        private UILabel m_titleLabel = null;

        private UISprite m_itemIcon = null;
        private UISprite m_buildingIcon = null;
        private UISprite m_townIcon = null;

        public UIScrollItem()
        {
        }

        public UIScrollItem(GameObject obj)
        {
            m_obj = obj;
            m_transform = m_obj.transform;

            #region Label
            m_titleLabel = m_transform.FindChild("Item_Name").GetComponent<UILabel>();
            #endregion

            #region Sprite
            m_itemIcon = m_transform.FindChild("Item_Icon/Icon").GetComponent<UISprite>();
            m_buildingIcon = m_transform.FindChild("Building_Icon/Icon").GetComponent<UISprite>();
            m_townIcon = m_transform.FindChild("Town_Icon/Icon").GetComponent<UISprite>();
            #endregion

            SetActive(false);
        }

        public void Init(LevelUpRefInfo info)
        {
            ReleaseIcon();

            m_titleLabel.text = info.descString;

            switch(info.type)
            {
                case LEVEL_UNLOCK_TYPE.TYPE_TOWN:
                    m_townIcon.gameObject.SetActive(true);
                    m_townIcon.spriteName = info.imageName;
                    m_townIcon.MakePixelPerfect();
                    break;

                case LEVEL_UNLOCK_TYPE.TYPE_BUILDING:
                    m_buildingIcon.gameObject.SetActive(true);
                    m_buildingIcon.spriteName = info.imageName;
                    m_buildingIcon.MakePixelPerfect();

                    BuildingInfo bInfo = WorldManager.instance.m_dataManager.m_buildingData.GetBuildingInfoBySpriteName(info.imageName);
                    if (bInfo != null) m_buildingIcon.transform.localPosition = bInfo.uiIconPosition;
                    break;

                case LEVEL_UNLOCK_TYPE.TYPE_FUNITURE:
                case LEVEL_UNLOCK_TYPE.TYPE_CLOTHES:
                    m_itemIcon.gameObject.SetActive(true);
                    m_itemIcon.spriteName = info.imageName;
                    m_itemIcon.MakePixelPerfect();
                    break;
            }

            SetActive(true);
        }

        private void ReleaseIcon()
        {
            m_itemIcon.gameObject.SetActive(false);
            m_buildingIcon.gameObject.SetActive(false);
            m_townIcon.gameObject.SetActive(false);
        }

        public void SetActive(bool b)
        {
            m_obj.SetActive(b);
        }
    }
    #endregion

    private GameObject m_itemObj = null;
    private List<UIScrollItem> m_itemList = null;

    public UIUserLevelUnlockView()
    {
    }

    public UIUserLevelUnlockView(GameObject obj)
    {
        m_obj = obj;
        m_transform = m_obj.transform.FindChild("Anchor_Center/Window");

        InitScroll();
        InitButton();

        m_obj.SetActive(false);
    }

    #region Init

    private void InitScroll()
    {
        m_itemObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/LevelUnlock_ScrollItem");
        m_itemList = new List<UIScrollItem>();

        m_scrollPanel = m_transform.FindChild("ScrollWindow/ScrollView").GetComponent<UIPanel>();
        m_scrollView = m_scrollPanel.GetComponent<UIScrollView>();
        m_scrollGrid = m_scrollPanel.transform.FindChild("Grid").GetComponent<UIGrid>();

        InitItem();
    }

    private void InitButton()
    {
        m_transform.FindChild("Ok_Button").GetComponent<UIEventListener>().onTutorialClick = OnUnlockOkButton;
    }

    #endregion

    public void OpenPopup()
    {
        m_obj.SetActive(true);
        UpdateList();

        Hashtable hash = new Hashtable();
        hash.Add("amount", new Vector3(0.05f, 0.05f, 0f));
        hash.Add("time", 1f);
        hash.Add("ignoretimescale", true);
        iTween.PunchScale(m_transform.gameObject, hash);

        SoundManager.instance.PlayAudioClip("UI_PopupOpen");
    }

    public void ClosePopup()
    {
        iTween[] tweenArray = m_obj.GetComponents<iTween>();
        for(int i = 0; i < tweenArray.Length; i++)
            MonoBehaviour.Destroy(tweenArray[i]);

        m_obj.SetActive(false);

        if (UserInfo.instance.CheckLevelUp)
            UserInfo.instance.CloseLevelUp();
    }

    #region Scroll

    #region Item

    private void InitItem()
    {
        for(int i = 0; i < (int)UserLevelData.MAX_REF_COUNT; i++)
        {
            GameObject obj = MonoBehaviour.Instantiate(m_itemObj) as GameObject;
            obj.transform.parent = m_scrollGrid.transform;
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;

            UIScrollItem item = new UIScrollItem(obj);
            m_itemList.Add(item);
        }
    }

    private UIScrollItem GetItem(int index)
    {
        return m_itemList.Count > index ? m_itemList[index] : null;
    }

    private void ReleaseAllCardItem()
    {
        for(int i = 0; i < m_itemList.Count; i++)
            m_itemList[i].SetActive(false);
    }

    #endregion

    private void UpdateList()
    {
        ReleaseAllCardItem();

        UserLevelInfo info = WorldManager.instance.m_dataManager.
            m_userLevelData.GetLevelInfo(WorldManager.instance.m_player.m_level);
        InGameNotification.instance.SetLevelUpUnlockNotification(info.levelUpRefArray);

        for(int i = 0; i < info.levelUpRefArray.Length; i++)
        {
            UIScrollItem item = GetItem(i);
            if(info.levelUpRefArray[i].index.Equals(0) == false)
                item.Init(info.levelUpRefArray[i]);
        }

        UpdateItemPosition();
    }

    private void UpdateItemPosition()
    {
        EnableScrollView();

        m_scrollGrid.Reposition();
        m_scrollView.ResetPosition();
    }

    private void EnableScrollView()
    {
        if(m_scrollView.enabled == false)
            m_scrollView.enabled = true;
    }

    #endregion

    #region Button

    private void OnUnlockOkButton(GameObject obj)
    {
        Util.ButtonAnimation(obj);
        ClosePopup();
    }

    #endregion

    public bool CheckUIExists
    {
        get { return m_transform != null; }
    }

    public void SetActive(bool b)
    {
        m_obj.SetActive(b);
    }
}

#endregion

#region UIUserInfo

/// <summary>
/// <para>name : UIUserInfo</para>
/// <para>describe : 유저 정보 HUD</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UIUserInfo
{
    readonly private Vector3 MY_LEVEL_POS = new Vector3(227.0f, -46.0f, 0);
    readonly private Vector3 FRIEND_LEVEL_POS = new Vector3(152.0f, -46.0f, 0);

    private GameObject m_obj = null;
    private Transform m_transform = null;

    private CDirection m_cDirection = null;

    private UILabel[] m_labelArray = null;
    private enum LABEL_TYPE
    {
        TYPE_NONE = -1,

        TYPE_LEVEL,
        TYPE_INTERIOR_POINT,
        TYPE_SOCIAL_POINT,
        TYPE_NAME,

        TYPE_END,
    }

    private Transform[] m_transArray = null;
    public enum TRANSFORM_TYPE
    {
        TYPE_NONE = -1,

        TYPE_LEVEL,
        TYPE_INTERIOR_POINT,
        TYPE_SOCIAL_POINT,

        TYPE_NORMAL_BACK,
        TYPE_SPECIAL_BACK,

        TYPE_POINT_GROUP,
        TYPE_FRIEND_BUTTON,

        TYPE_DOG_BUTTON,

        TYPE_PROFILE_NEW,

        TYPE_END,
    }

    private UIProfileTextureIcon m_icon = null;
    private UISlider m_levelSlider = null;

    private bool m_isOnOff = false;
    private int saveSocialPoint = 0;

    public UIUserInfo()
    {
    }

    public UIUserInfo(GameObject obj)
    {
        m_obj = obj;
        m_transform = obj.transform;
        m_transform.name = "UIUserInfo";

        #region Callback
        m_obj.GetComponent<UIEventListener>().onClick = OnInfoClick;
        #endregion
    }

    #region Init

    public void Init(Transform parent)
    {
        SetParentPanelOption(parent);

        m_transform.parent = parent;
        m_transform.localPosition = Vector3.zero;
        m_transform.localScale = Vector3.one;

        m_cDirection = m_obj.GetComponent<CDirection>();

        #region Label
        m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END]; 
        string[] labelPathArray = { "Level", "Point_Group/InteriorPoint_Value", "Point_Group/SocialPoint_Value", "Name" };
        for(int i = 0; i < labelPathArray.Length; i++)
            m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
        #endregion

        #region Object
        m_icon = m_transform.FindChild("Photo").GetComponent<UIProfileTextureIcon>();
        m_levelSlider = m_transform.FindChild("Level_Slider").GetComponent<UISlider>();
        m_transArray = new Transform[(int)TRANSFORM_TYPE.TYPE_END]; 
        string[] transPathArray = { "Level_Slider/Star", "Point_Group/InteriorPoint_Icon", "Point_Group/SocialPoint_Icon", "Normal_Background", "Special_Background",
                                    "Point_Group", "Friend_Button", "BtnMainDog", "Frame/SpriteNew" };
        for(int i = 0; i < transPathArray.Length; i++)
            m_transArray[i] = m_transform.FindChild(transPathArray[i]);

        m_transArray[(int)TRANSFORM_TYPE.TYPE_FRIEND_BUTTON].GetComponent<UIEventListener>().onClick = OnSendUserDogInfo;
        m_transArray[(int)TRANSFORM_TYPE.TYPE_DOG_BUTTON].gameObject.AddComponent<UIEventListener>().onClick = OnClickMainDog;
        #endregion

        SetActive(parent != null);
    }

    public bool CheckUIUserInfoExists
    {
        get { return m_transform != null; }
    }

    #endregion

    #region Update

    public void UpdateParent(Transform parent)
    {
        SetParentPanelOption(parent);

        m_transform.parent = parent;
        m_transform.localPosition = Vector3.zero;
        m_transform.localScale = Vector3.one;

        SetActive(parent != null);
    }

    public void UpdateInfo(bool isUser)
    {
        int userNo = isUser ? int.Parse(WorldManager.instance.m_player.m_userNo) : WorldManager.instance.m_player.m_Friend.m_CurFriendNo;
        string userName = isUser ? WorldManager.instance.m_player.m_UserName : WorldManager.instance.m_player.m_Friend.m_FriendProfile.userName;
        int userLevel = isUser ? WorldManager.instance.m_player.m_level : WorldManager.instance.m_player.m_Friend.m_FriendProfile.userLv;
        string proImg = isUser ? WorldManager.instance.m_player.m_ProImgPath : WorldManager.instance.m_player.m_Friend.m_FriendProfile.proImg;

        if(isUser)
            ProfileTxManager.instance.GetTexture(proImg, m_icon);
        else
        {
            if(WorldManager.instance.m_dataManager.m_npcData.CheckIsNpcFriend(WorldManager.instance.m_player.m_Friend.m_FriendProfile.socUserNo))
                ProfileTxManager.instance.GetNpcTexture(WorldManager.instance.m_dataManager.m_npcData.GetNpcIcon(WorldManager.instance.m_player.m_Friend.m_FriendProfile.socUserNo), m_icon);
            else
                ProfileTxManager.instance.GetTexture(proImg, m_icon);
        }

        m_labelArray[(int)LABEL_TYPE.TYPE_LEVEL].text = string.Format("Lv.{0}", userLevel.ToString());

        m_transArray[(int)TRANSFORM_TYPE.TYPE_POINT_GROUP].gameObject.SetActive(isUser);
        m_levelSlider.gameObject.SetActive(isUser);

        m_transArray[(int)TRANSFORM_TYPE.TYPE_PROFILE_NEW].gameObject.SetActive(isUser && InGameNotification.instance.CheckNotification(NOTI_TYPE.TYPE_GUEST_BOOK));

        if(isUser)
        {
            saveSocialPoint = int.Parse(m_labelArray[(int)LABEL_TYPE.TYPE_SOCIAL_POINT].text);

            m_labelArray[(int)LABEL_TYPE.TYPE_NAME].text = "";
            m_labelArray[(int)LABEL_TYPE.TYPE_LEVEL].transform.localPosition = MY_LEVEL_POS;

            m_levelSlider.value = Mathf.Clamp((float)WorldManager.instance.m_player.m_exp / (float)WorldManager.instance.m_player.m_maxExp, 0, 1.0f);

            m_labelArray[(int)LABEL_TYPE.TYPE_INTERIOR_POINT].text = WorldManager.instance.m_player.GetTotalRoomFurPoint().ToString();
            m_labelArray[(int)LABEL_TYPE.TYPE_SOCIAL_POINT].text = WorldManager.instance.m_player.m_SocPoint.ToString();

            m_transArray[(int)TRANSFORM_TYPE.TYPE_FRIEND_BUTTON].gameObject.SetActive(false);
        }

        else
        {
            m_labelArray[(int)LABEL_TYPE.TYPE_NAME].text = userName;
            m_labelArray[(int)LABEL_TYPE.TYPE_LEVEL].transform.localPosition = FRIEND_LEVEL_POS;

            m_transArray[(int)TRANSFORM_TYPE.TYPE_FRIEND_BUTTON].gameObject.SetActive(!WorldManager.instance.m_player.CheckFriendPropListContain(WorldManager.instance.m_player.m_Friend.m_CurFriendNo) && 
                                                                                      !WorldManager.instance.m_player.m_Friend.m_isFriend);
        }

        SetBackground(!WorldManager.instance.CheckTrendRoomIDRank(userNo));

        switch(StateManager.instance.GetSceneTypeByCurState())
        {
            case SCENE_TYPE.SCENE_ROOM:
                if(WorldManager.instance.m_player.CheckRoomLoadType(ROOM_LOAD_TYPE.NOEDIT))
                    OnOffMainDog(false);
                else if(WorldManager.instance.m_player.CheckRoomLoadType(ROOM_LOAD_TYPE.FRIEND) || 
                    WorldManager.instance.m_player.CheckRoomLoadType(ROOM_LOAD_TYPE.NOFRIEND))
                {
                    bool isActive = !WorldManager.instance.m_dataManager.m_npcData.CheckIsNpcFriend(WorldManager.instance.m_player.m_Friend.m_CurFriendNo) &&
                                        WorldManager.instance.m_player.m_Friend.m_FriendMainDog.dogNo.Equals(0) == false;
                    OnOffMainDog(isActive);
                }
                else
                    OnOffMainDog(false);
                break;

            case SCENE_TYPE.SCENE_VILLAGE:
                OnOffMainDog(false);
                break;

            default:
                OnOffMainDog(false);
                break;
        }
    }

    #endregion

    #region OnOff

    public void OnOff(bool b, bool isInstantly = false)
    {
        if(m_obj == null)
            return;

        m_isOnOff = b;

        iTween.Stop(m_obj);
        if(isInstantly) m_cDirection.ResetToBeginning(b ? 100000018 : 100000019);
        else m_cDirection.SetInit(b ? CheckIsUser() ? 100000018 : 100000158 : 100000019, true);
    }

    public bool CheckOnOff
    {
        get { return m_isOnOff; }
    }

    public void OnOffFriendButton(bool b)
    {
        m_transArray[(int)TRANSFORM_TYPE.TYPE_FRIEND_BUTTON].gameObject.SetActive(b);
    }

    public void OnOffMainDog(bool b)
    {
        m_transArray[(int)TRANSFORM_TYPE.TYPE_DOG_BUTTON].gameObject.SetActive(b);
        if (b)
        {
            UISprite dogIcon = m_transArray[(int)TRANSFORM_TYPE.TYPE_DOG_BUTTON].FindChild("Dog").GetComponent<UISprite>();
            dogIcon.MakePixelPerfect();
            dogIcon.spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName((uint)(WorldManager.instance.m_player.m_Friend.m_FriendMainDog.skin)));
        }
    }

    /// <summary>
    /// <para>name : SetBackground</para>
    /// <para>describe : 유저 HUD의 뒷배경을 바꿉니다. TRUE=일반 FALSE=스페셜</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void SetBackground(bool b)
    {
        m_transArray[(int)TRANSFORM_TYPE.TYPE_NORMAL_BACK].gameObject.SetActive(b);
        m_transArray[(int)TRANSFORM_TYPE.TYPE_SPECIAL_BACK].gameObject.SetActive(!b);
    }

    #endregion

    #region Callback

    private void OnInfoClick(GameObject obj)
    {
        if (StateManager.instance.m_curState.CheckGUIButtonActive() == false)
            return;

        StateManager.instance.m_curState.OpenGuestBook();
    }

    private void OnSendUserDogInfo(GameObject obj)
    {
        if (StateManager.instance.m_curState.CheckGUIButtonActive() == false)
            return;

#if UNITY_EDITOR
        Debug.Log(" OnSendUserDogInfo ");
#endif

        SoundManager.instance.PlayAudioClip("UI_Click");

        WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.USER_INFO_FRIEND_ADD, true);
        NetworkManager.instance.SendSocPropFriend(WorldManager.instance.m_player.m_Friend.m_CurFriendNo);
    }

    private void OnClickMainDog(GameObject obj)
    {
        SoundManager.instance.PlayAudioClip("UI_Click");

        WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.PROFILE_ICON_CLICK_TYPE, PROFILE_ICON_CLICK_TYPE.ROOM_USERINFO );
        WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.DOG_BUTTON, (object)this);
        NetworkManager.instance.SendUserDogInfo( WorldManager.instance.m_player.m_Friend.m_CurFriendNo );

     //   Debug.Log("OnClickMainDog");
    }

    #endregion

    #region Util

    private void SetParentPanelOption(Transform objTransform)
    {
        //Transform parent = objTransform;
        //while(parent != null)
        //{
        //    UIPanel uiPanel = parent.GetComponent<UIPanel>();
        //    if(uiPanel == null)
        //        parent = parent.parent;
        //    else
        //    {
        //        uiPanel.generateNormals = true;
        //        Debug.Log(string.Format("{0} Panel's On generateNormals.", uiPanel.gameObject.name));

        //        break;
        //    }
        //}
    }

    private bool CheckIsUser()
    {
        switch(StateManager.instance.m_curStateType)
        {
            case STATE_TYPE.STATE_ROOM:
                return WorldManager.instance.m_player.CheckRoomLoadType(ROOM_LOAD_TYPE.NOEDIT);
            default:
                return true;
        }
    }

    public Vector3 GetIconPos(TRANSFORM_TYPE type)
    {
        return m_transArray[(int)type].position;
    }

    public int GetSaveSocialPoint()
    {
        return saveSocialPoint;
    }

    #endregion

    #region Effect

    public void PunchScale(TRANSFORM_TYPE type)
    {
        iTween.Stop(m_transArray[(int)type].gameObject);
        m_transArray[(int)type].transform.localScale = Vector3.one;

        iTween.PunchScale(m_transArray[(int)type].gameObject, Vector3.one * 0.5f, 0.3f);
    }

    public void PunchScale(TRANSFORM_TYPE type, float scaleTime)
    {
        iTween.Stop(m_transArray[(int)type].gameObject);
        m_transArray[(int)type].transform.localScale = Vector3.one;

        iTween.PunchScale(m_transArray[(int)type].gameObject, Vector3.one * 0.5f, scaleTime);
    }

    #endregion

    public void SetActive(bool b)
    {
        m_obj.SetActive(b);
    }

    public void SetUserDogInfo( RES_USERDOGINFO packet )
    {
        GameObject pUI = MonoBehaviour.Instantiate( AssetBundleEx.Load<GameObject>( "[Prefabs]/[Gui]/OtherPlayerProfile" ), Vector3.zero, Quaternion.identity) as GameObject;
        OtherPlayerProfile pProfile = pUI.GetComponent<OtherPlayerProfile>();
        
        pProfile.Init( OnProfileExit, null, packet.userDogInfo, WorldManager.instance.m_player.CheckRoomLoadType(ROOM_LOAD_TYPE.NOFRIEND), true, packet.rateCntD, packet.isRatedD, null, 1 );
    }

    public void OnProfileExit( int no, bool add )
    {
    }
}

#endregion

/// <summary>
/// <para>name : UserInfo</para>
/// <para>describe : 유저 정보 HUD</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class UserInfo : MonoSingleton<UserInfo>
{
    #region Data..
    private const string USERINFO_PATH = "[Prefabs]/[Gui]/UIUserInfo";

    private UIUserInfo m_uiUserInfo = null;
    private UIUserLevelUp m_uiUserLevelUp = null;
    private UILevelUpScenario m_uiLevelUpScenario = null;
    private UIDogLevelUp m_UIDogLevelUp = null;
    #endregion

    #region Init

    public void Init()
    {
    }

    /// <summary>
    /// <para>name : Init</para>
    /// <para>describe : 유저 정보 HUD를 생성하고, parent 밑에 붙입니다. parent가 null값일 시, Active가 꺼진 상태가 됩니다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void Init(Transform parent)
    {
        if( m_uiUserInfo == null || m_uiUserInfo.CheckUIUserInfoExists == false )
            m_uiUserInfo = new UIUserInfo(Instantiate(AssetBundleEx.Load<GameObject>(USERINFO_PATH)) as GameObject);

        m_uiUserInfo.Init(parent);

        InitUIDogLevelUp();
    }

    public void InitUIDogLevelUp()
    {
        if(m_UIDogLevelUp == null || m_UIDogLevelUp.GetTransform == null)
        {
            GameObject obj = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UI_S109_DogLevelUp"), Vector3.zero, Quaternion.identity) as GameObject;
            obj.transform.name = "UI_S109_DogLevelUp";
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;

            m_UIDogLevelUp = obj.AddComponent<UIDogLevelUp>();
            m_UIDogLevelUp.InitUIDogLevelUp();
        }
    }

    /// <summary>
    /// <para>name : AddExtraUserInfo</para>
    /// <para>describe : 메인 재화 HUD와 별개로, 따로 표시와 업데이트가 가능한 재화 HUD를 생성하고, parent 밑에 붙입니다. 
    ///                      (주의!) 이 HUD 오브젝트는 리턴받은 UIUserInfo로 따로 관리해주세요. UserInfo.Instance로 관리되지 않습니다!
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public UIUserInfo AddExtraUserInfo(Transform parent, bool isUser = true)
    {
        UIUserInfo uiUserInfo = null;

        #region Instantiate
        uiUserInfo = new UIUserInfo(Instantiate(AssetBundleEx.Load<GameObject>(USERINFO_PATH)) as GameObject);
        #endregion

        #region InitUserInfo
        uiUserInfo.Init(parent);
        uiUserInfo.UpdateInfo(isUser);
        #endregion

        return uiUserInfo;
    }

    /// <summary>
    /// <para>name : UpdateParent</para>
    /// <para>describe : 유저 정보 HUD의 parent를 옮기고, 위치를 다시 잡아줍니다. parent가 null값일 시, Active가 꺼진 상태가 됩니다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateParent(Transform parent)
    {
        if(m_uiUserInfo != null)
            m_uiUserInfo.UpdateParent(parent);
    }

    #endregion

    #region Update

    /// <summary>
    /// <para>name : UpdateInfo</para>
    /// <para>describe : 유저 정보를 업데이트합니다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateInfo(bool isUser = true)
    {
        isUser = StateManager.instance.m_curState.GetIsUser();
        if(m_uiUserInfo != null && m_uiUserInfo.CheckUIUserInfoExists )
            m_uiUserInfo.UpdateInfo(isUser);
    }

    #endregion

    #region OnOff

    /// <summary>
    /// <para>name : OnOff</para>
    /// <para>describe : (bool)값의 위치로 Tween을 시작합니다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void OnOff(bool b, bool isInstantly = false)
    {
        if(m_uiUserInfo != null && m_uiUserInfo.CheckUIUserInfoExists)
            m_uiUserInfo.OnOff(b, isInstantly);
    }

    /// <summary>
    /// <para>name : SetActive</para>
    /// <para>describe : 유저 정보 HUD의 Active를 (bool)값으로 켜거나 끕니다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetActive(bool b)
    {
        if(m_uiUserInfo != null && m_uiUserInfo.CheckUIUserInfoExists)
            m_uiUserInfo.SetActive(b);
    }

    /// <summary>
    /// <para>name : CheckOnOff</para>
    /// <para>describe : 유저 정보 HUD의 Tween 상태를 체크합니다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public bool CheckOnOff
    {
        get { return m_uiUserInfo != null && m_uiUserInfo.CheckUIUserInfoExists ? m_uiUserInfo.CheckOnOff : false; }
    }

    #endregion

    #region LevelUp

    private bool m_isCheckLevelUp = false;
    public bool CheckLevelUp
    {
        get { return m_isCheckLevelUp; }
        set
        {
            m_isCheckLevelUp = value ? value : m_isCheckLevelUp;
            if (m_isCheckLevelUp)
            {
                UserLevelInfo info = WorldManager.instance.m_dataManager.m_userLevelData.GetLevelInfo(WorldManager.instance.m_player.m_level);
                if (info.scenarioGroupIndex.Equals(0) == false)
                    WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.LEVEL_UP_SCENARIO, true);
                if (WorldManager.instance.m_player.m_isRegPhone && info.isVerifyMobile)
                    WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.REG_PHONE_ENABLE, true);
                if (info.isSurvey)
                    WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.SURVEY_OPEN, true);

                switch (StateManager.instance.m_curStateType)
                {
                    case STATE_TYPE.STATE_ROOM: StartCoroutine("WaitForRoomState"); break;
                    case STATE_TYPE.STATE_VILLAGE: OpenLevelUp(); break;
                }
                
                SdkManager.instance.UpLoadUserInfo(UPLOAD_USERINFO_STATE.TYPE_LEVEL_UP);
            }
        }
    }

    public bool CheckLevelUpOpen
    {
        get { return (m_uiUserLevelUp == null || m_uiUserLevelUp.CheckUIExists == false) ? false : m_uiUserLevelUp.CheckOpenComplete; }
    }

    private IEnumerator WaitForRoomState()
    {
        State_Room roomState = (State_Room)StateManager.instance.m_curState;
        while(roomState.GetRoomState().Equals(ROOM_STATE.NONE) == false || 
            roomState.GetOrderState().Equals(State_Room.ORDERSTATE.ORDER_READY) == false)
            yield return null;

        OpenLevelUp();
    }

    public void OpenLevelUp()
    {
        if(m_uiUserLevelUp == null || m_uiUserLevelUp.CheckUIExists == false)
        {
            GameObject levelUpWindow = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UI_S107_LevelUp")) as GameObject;
            levelUpWindow.name = "UI_S99_LevelUp";

            m_uiUserLevelUp = new UIUserLevelUp(levelUpWindow);
        }

        m_uiUserLevelUp.OpenPopup();
    }

    public void CloseLevelUp()
    {
        m_isCheckLevelUp = false;

        if(m_uiUserLevelUp != null)
            m_uiUserLevelUp.ClosePopup();

        StateManager.instance.m_curState.UpdateQuestList();
        StateManager.instance.m_curState.UpdateGUIState();

        if (GetIsDogLevelUp())
            OpenUIDogLevelUp();
        else
        {
            if (Tutorial.instance.CheckTutorialActive(true) == false)
            {
                UserLevelInfo info = WorldManager.instance.m_dataManager.m_userLevelData.GetLevelInfo(WorldManager.instance.m_player.m_level);
                if (info.scenarioGroupIndex.Equals(0) == false)
                {
                    OpenLevelUpScenario(info);
                    WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.LEVEL_UP_SCENARIO);
                }
                else
                {
                    if (WorldManager.instance.m_player.m_isRegPhone && info.isVerifyMobile)
                    {
                        CertificationPhonePopup.instance.OpenPopup();
                        WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.REG_PHONE_ENABLE);
                    }

                    else if (info.isSurvey)
                    {
                        SdkManager.instance.OnSurveyOpen();
                        WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.SURVEY_OPEN);
                    }
                }
            }
        }
    }

    private void OnTitleTweenComplete()
    {
        if(m_uiUserLevelUp != null && m_uiUserLevelUp.CheckUIExists)
            m_uiUserLevelUp.TitleTweenComplete();
    }

    #endregion

    #region DogLevelUP

    public void SetIsDogLevelUp(bool b)
    {
        if(null != m_UIDogLevelUp)
        {
            m_UIDogLevelUp.SetIsDogLevelUp(b);
        }
    }

    public void SetDogID(int dogNo)
    {
        if(null != m_UIDogLevelUp)
        {
            m_UIDogLevelUp.SetDogID(dogNo);
        }
    }

    public void SetDogOldStat(int maxAP, int[] statPoints)
    {
        if(null != m_UIDogLevelUp)
        {
            m_UIDogLevelUp.SetDogOldStat(maxAP, statPoints);
        }
    }

    public bool GetIsDogLevelUp()
    {
        if(null != m_UIDogLevelUp)
        {
           return m_UIDogLevelUp.GetIsDogLevelUp();
        }

        return false;
    }

    public void OpenUIDogLevelUp()
    {
        if(StateManager.instance.GetSceneTypeByCurState() == SCENE_TYPE.SCENE_WALK)
            return;

        if(null != m_UIDogLevelUp)
        {
            m_UIDogLevelUp.OpenUIDogLevelUp();
        }
    }

    #endregion

    #region LevelUpScenario

    public void OpenLevelUpScenario(UserLevelInfo info)
    {
        if(m_uiLevelUpScenario == null || m_uiLevelUpScenario.CheckUIExists == false)
        {
            GameObject levelUpScenario = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UI_S99_LevelUpTitle")) as GameObject;
            levelUpScenario.name = "UI_S99_LevelUpTitle";

            m_uiLevelUpScenario = new UILevelUpScenario(levelUpScenario);
        }

        StateManager.instance.m_curState.OnOffGUIState(false);

        m_uiLevelUpScenario.SetActive(true);
        StartCoroutine(m_uiLevelUpScenario.TitleTweenStart(info));
    }

    public void LevelUpScenarioTweenComplete(params object[] param)
    {
        StartCoroutine(m_uiLevelUpScenario.TitleTweenRelease());
    }

    #endregion

    #region Friend_Add

    public void OnResponsePropFriend(bool isSuccess, RES_SOC_PROP_FRIEND packet)
    {
        if(isSuccess)
            MsgBox.instance.OpenMsgToast(Str.instance.Get(440, STR_RULE.STR_FRIENDNAME, WorldManager.instance.m_player.m_Friend.m_FriendProfile.userName));
        if(m_uiUserInfo != null && m_uiUserInfo.CheckUIUserInfoExists)
            m_uiUserInfo.OnOffFriendButton(false);
    }

    #endregion

    #region Util

    public Vector3 GetUserInfoIconPos(UIUserInfo.TRANSFORM_TYPE type)
    {
        return m_uiUserInfo.GetIconPos(type);
    }

    public int GetSaveSocialPoint()
    {
        return m_uiUserInfo.GetSaveSocialPoint();
    }

    #endregion
    
    #region Effect

    public void PunchScale(UIUserInfo.TRANSFORM_TYPE type)
    {
        if(m_uiUserInfo != null)
            m_uiUserInfo.PunchScale(type);
    }

    public void PunchScale(UIUserInfo.TRANSFORM_TYPE type, float scaleTime)
    {
        if(m_uiUserInfo != null)
            m_uiUserInfo.PunchScale(type, scaleTime);
    }

    #endregion
}
