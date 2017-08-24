using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#region Scroll_Item

public class UIAchieveScrollGroup
{
    private GameObject m_obj = null;

    private Transform m_transform = null;
    public Transform Transform { get { return m_transform; } }

    private UISprite m_background = null;

    private GameObject m_titleGroup = null;
    private UILabel m_titleLabel = null;

    private GameObject m_itemObj = null;
    private List<UIAchieveInfoItem> m_itemList = null;
    private int m_activeItemCount = 0;

    public bool CheckActive { get { return m_obj.activeSelf; } }
    public int GetSize { get { return m_background.height; } }

    public UIAchieveScrollGroup() { }
    public UIAchieveScrollGroup(GameObject obj)
    {
        m_obj = obj;
        m_transform = obj.transform;

        m_background = obj.GetComponent<UISprite>();

        m_titleGroup = m_transform.FindChild("Title_Group").gameObject;
        m_titleLabel = m_transform.FindChild("Title_Group/Title").GetComponent<UILabel>();

        m_itemObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UIAchieve_Item");
        m_itemList = new List<UIAchieveInfoItem>();

        SetActive(false);
    }

    public void Update(ACHIEVE_TAB_TYPE tabType, ACHIEVE_TYPE type, List<AchievementInfo> infoList)
    {
        SetActive(infoList != null && infoList.Count.Equals(0) == false);
        UpdateList(infoList, SetTitle(tabType, type));
    }

    private bool SetTitle(ACHIEVE_TAB_TYPE tabType, ACHIEVE_TYPE type)
    {
        switch (type)
        {
            case ACHIEVE_TYPE.TYPE_DAILY_RANDOM:
                m_titleGroup.SetActive(true);
                m_titleLabel.text = Str.instance.Get(674);
                return true;

            case ACHIEVE_TYPE.TYPE_WEEKLY_RANDOM:
                m_titleGroup.SetActive(true);
                m_titleLabel.text = Str.instance.Get(675);
                return true;

            case ACHIEVE_TYPE.TYPE_NONE:
                if (tabType.Equals(ACHIEVE_TAB_TYPE.TYPE_DAILY))
                {
                    m_titleGroup.SetActive(true);
                    m_titleLabel.text = Str.instance.Get(676);
                    return true;
                }
                else
                {
                    m_titleGroup.SetActive(false);
                    return false;
                }

            default:
                m_titleGroup.SetActive(false);
                return false;
        }
    }

    #region Item

    private void UpdateList(List<AchievementInfo> infoList, bool isTitleActive)
    {
        ReleaseAllItem();

        float baseHeight = isTitleActive ? -99.0f : -62.0f;
        float addHeight = -111.0f;
        int lastIndex = infoList.Count - 1;

        for (int i = 0; i < infoList.Count; i++)
            GetItem().UpdateItem(infoList[i], Vector3.up * (baseHeight + addHeight * i), i.Equals(lastIndex));

        m_background.height = Mathf.RoundToInt((isTitleActive ? 168 : 128) + (Mathf.Abs(addHeight) * (infoList.Count - 1)));
    }

    private UIAchieveInfoItem GetItem()
    {
        if (m_activeItemCount.Equals(m_itemList.Count))
        {
            GameObject obj = MonoBehaviour.Instantiate(m_itemObj) as GameObject;
            obj.transform.parent = m_transform;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;

            m_itemList.Add(new UIAchieveInfoItem(obj));
        }

        UIAchieveInfoItem item = m_itemList[m_activeItemCount];
        m_activeItemCount++;

        return item;
    }

    private void ReleaseAllItem()
    {
        for (int i = 0; i < m_itemList.Count; i++)
            m_itemList[i].SetActive(false);
    }

    #endregion

    public void SetActive(bool b)
    {
        m_obj.SetActive(b);
    }
}

public class UIAchieveInfoItem
{
    private GameObject m_obj = null;
    private Transform m_transform = null;

    private GameObject m_viewGroup = null;
    private GameObject m_hideGroup = null;

    private UILabel[] m_labelArray = null;
    private enum LABEL_TYPE
    {
        TYPE_NONE = -1,

        TYPE_TITLE,
        TYPE_DESC,
        TYPE_SLIDER_VALUE,
        TYPE_REWARD_COUNT,
        TYPE_BUTTON_TEXT,

        TYPE_HIDE_DESC,

        TYPE_END,
    }

    private UISprite[] m_spriteArray = null;
    private enum SPRITE_TYPE
    {
        TYPE_NONE = -1,

        TYPE_TITLE_ITEM_ICON,
        TYPE_TITLE_MATERIAL_ICON,
        TYPE_TITLE_QUEST_ICON,

        TYPE_REWARD_GEMS_ICON,
        TYPE_REWARD_ITEM_ICON,
        TYPE_REWARD_MATERIAL_ICON,
        TYPE_REWARD_DOG_ICON,

        TYPE_REWARD_BUTTON,
        TYPE_COMPLETE,
        TYPE_LINE,

        TYPE_END,
    }

    private UISlider m_slider = null;
    private GameObject m_button = null;

    private AchievementInfo m_achieveInfo = null;
    private SAchieveInfo m_serverInfo = null;

    private uint m_itemIndex = 0;

    public UIAchieveInfoItem() { }
    public UIAchieveInfoItem(GameObject obj)
    {
        m_obj = obj;
        m_transform = obj.transform;

        m_viewGroup = m_transform.FindChild("View_Group").gameObject;
        m_hideGroup = m_transform.FindChild("Hide_Group").gameObject;

        #region Label
        string[] labelPathArray = { "View_Group/Info_Group/Title", "View_Group/Info_Group/Desc", "View_Group/Info_Group/Slider/Value", 
                                    "View_Group/Reward_Group/Value", "View_Group/RewardButton/Text", "Hide_Group/Desc" };
        m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

        for (int i = 0; i < m_labelArray.Length; i++)
            m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
        #endregion

        #region Sprite
        string[] spritePathArray = { "View_Group/Info_Group/Item_Icon_Group/Icon", "View_Group/Info_Group/Material_Icon_Group/Icon", 
                                     "View_Group/Info_Group/Quest_Icon", "View_Group/Reward_Group/Goods_Icon", "View_Group/Reward_Group/Item_Icon_Group/Icon", 
                                     "View_Group/Reward_Group/Material_Icon_Group/Icon", "View_Group/Reward_Group/Dog_Icon_Group/Icon", 
                                     "View_Group/RewardButton/Background", "View_Group/Complete", "Line" };
        m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

        for (int i = 0; i < m_spriteArray.Length; i++)
            m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
        #endregion

        #region Object
        m_slider = m_transform.FindChild("View_Group/Info_Group/Slider").GetComponent<UISlider>();

        m_button = m_transform.FindChild("View_Group/RewardButton").gameObject;
        m_button.GetComponent<UIEventListener>().onClick = OnRewardButton;

        m_transform.FindChild("View_Group/Reward_Group").GetComponent<UIEventListener>().onPress = OnTooltip;
        #endregion

        SetActive(false);
    }

    public void UpdateItem(AchievementInfo info, Vector3 position, bool isLastPosition = false)
    {
        m_achieveInfo = info;
        m_serverInfo = AchievementManager.instance.GetServerAchieveInfo(info.index);

        m_itemIndex = info.rewardIndex;

        m_transform.localPosition = position;
        m_spriteArray[(int)SPRITE_TYPE.TYPE_LINE].gameObject.SetActive(!isLastPosition);

        bool isActive = !m_serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_NONE);
        m_viewGroup.SetActive(isActive);
        m_hideGroup.SetActive(!isActive);

        #region Info
        UpdateTitleIcon(info);

        int completeValue = m_serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE) ? info.value : m_serverInfo.achVal;

        m_labelArray[(int)LABEL_TYPE.TYPE_TITLE].text = info.GetTitle;
        m_labelArray[(int)LABEL_TYPE.TYPE_DESC].text = info.GetDesc;
        m_labelArray[(int)LABEL_TYPE.TYPE_HIDE_DESC].text = info.hintDesc;
        m_labelArray[(int)LABEL_TYPE.TYPE_SLIDER_VALUE].text = string.Format("{0}/{1}", completeValue, info.value);

        m_slider.value = (float)completeValue / (float)info.value;
        #endregion

        #region Reward
        UpdateRewardIcon(info);
        UpdateCompleteButton(m_serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR), 
                            m_serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE));
        #endregion

        SetActive(true);
    }

    private void UpdateTitleIcon(AchievementInfo info)
    {
        m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_QUEST_ICON].gameObject.SetActive(false);
        m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_MATERIAL_ICON].gameObject.SetActive(false);
        m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_ITEM_ICON].gameObject.SetActive(false);

        if (info.image.Contains("Quest_"))
        {
            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_QUEST_ICON].gameObject.SetActive(true);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_QUEST_ICON].spriteName = info.image;

            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_QUEST_ICON].MakePixelPerfect();
        }
        else if (info.image.Contains("Icon_Shop_"))
        {
            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_MATERIAL_ICON].gameObject.SetActive(true);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_MATERIAL_ICON].spriteName = info.image;

            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_MATERIAL_ICON].MakePixelPerfect();
        }
        else
        {
            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_ITEM_ICON].gameObject.SetActive(true);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_ITEM_ICON].spriteName = info.image;

            m_spriteArray[(int)SPRITE_TYPE.TYPE_TITLE_ITEM_ICON].MakePixelPerfect();
        }
    }

    private void UpdateRewardIcon(AchievementInfo info)
    {
        m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_GEMS_ICON].gameObject.SetActive(false);
        m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_ITEM_ICON].gameObject.SetActive(false);
        m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_MATERIAL_ICON].gameObject.SetActive(false);
        m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_DOG_ICON].gameObject.SetActive(false);

        if (info.CheckGoodsType)
        {
            m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_GEMS_ICON].gameObject.SetActive(true);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_GEMS_ICON].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)info.rewardIndex));
            m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_GEMS_ICON].MakePixelPerfect();

            m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_COUNT].text = string.Format("{0}", info.rewardCount);
        }

        else if (info.CheckDogType)
        {
            DogInfo dogInfo = WorldManager.instance.m_dataManager.m_dogData.GetDogData(info.rewardIndex);

            m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_DOG_ICON].gameObject.SetActive(true);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_DOG_ICON].spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName(dogInfo.basicSkin));
            m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_DOG_ICON].MakePixelPerfect();

            m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_COUNT].text = "";
        }

        else
        {
            ITEM_TYPE parseType = Util.ParseItemMainType(info.rewardIndex);
            if (parseType.Equals(ITEM_TYPE.DOGTICKET))
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_DOG_ICON].gameObject.SetActive(true);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_DOG_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_DOG_ICON].MakePixelPerfect();

                m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_COUNT].text = "";
            }
            else if (Util.CheckAtlasByItemType(parseType))
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_MATERIAL_ICON].gameObject.SetActive(true);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_MATERIAL_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_MATERIAL_ICON].MakePixelPerfect();
            }

            else
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_ITEM_ICON].gameObject.SetActive(true);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_ITEM_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_ITEM_ICON].MakePixelPerfect();
            }

            m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_COUNT].text = string.Format("{0}", info.rewardCount);
        }
    }

    private void UpdateCompleteButton(bool isEnable, bool isComplete)
    {
        m_spriteArray[(int)SPRITE_TYPE.TYPE_COMPLETE].gameObject.SetActive(isComplete);
        m_button.SetActive(!isComplete);

        if (isComplete == false)
        {
            m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].effectColor = isEnable ? new Vector4(129.0f / 255.0f, 75.0f / 255.0f, 36.0f / 255.0f, 1.0f) : 
                                                                                    new Vector4(110.0f / 255.0f, 102.0f / 255.0f, 116.0f / 255.0f, 1.0f);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_BUTTON].spriteName = isEnable ? "Btn_Orange" : "Btn_Gray";
        }
    }

    #region Callback

    private void OnRewardButton(GameObject obj)
    {
        if (m_serverInfo != null && m_serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
        {
            Util.ButtonAnimation(obj);
            AchievementManager.instance.GetAchieveWindow.OnSendAchieveFinish(m_serverInfo.achCode);
        }
    }

    private void OnTooltip(GameObject obj, bool isPress)
    {
        UIItemTooltip tooltip = AchievementManager.instance.GetAchieveWindow.GetItemTooptip;

        tooltip.OnOffTooltip(isPress);
        if (isPress)
        {
            SoundManager.instance.PlayAudioClip("UI_Click");
            tooltip.UpdateTooltip(m_itemIndex, obj.transform);
        }
    }

    #endregion

    public void SetActive(bool b)
    {
        m_obj.SetActive(b);
    }
}

#endregion

public class AchievementWindow : MonoBehaviour
{
    private const int SCROLL_GROUP_SIZE = 5;

    private Transform m_transform = null;
    private Transform m_window = null;

    private UIPanel m_scrollPanel = null;
    private UIScrollView m_scrollView = null;
    private Transform m_scrollGrid = null;
    private UIScrollBar m_scrollBar = null;

    private GameObject m_emptyView = null;

    private ACHIEVE_TAB_TYPE m_selectTabType = ACHIEVE_TAB_TYPE.TYPE_NONE;
    public ACHIEVE_TAB_TYPE GetSelectTabType { get { return m_selectTabType; } }

    #region UITab
    private class UITab
    {
        private ACHIEVE_TAB_TYPE m_tabType = ACHIEVE_TAB_TYPE.TYPE_NONE;
        public ACHIEVE_TAB_TYPE GetTabType { get { return m_tabType; } }

        private GameObject m_obj = null;
        private Transform m_transform = null;

        private UISprite m_background = null;
        private UILabel m_text = null;

        private GameObject m_newIcon = null;

        private AchievementWindow.SelectTab m_selectTab = null;

        public UITab() { }
        public UITab(GameObject obj, ACHIEVE_TAB_TYPE type, AchievementWindow.SelectTab selectTab)
        {
            m_obj = obj;
            m_transform = m_obj.transform;

            m_tabType = type;
            m_selectTab = selectTab;

            m_background = m_transform.FindChild("Background").GetComponent<UISprite>();
            m_text = m_transform.FindChild("Text").GetComponent<UILabel>();

            m_newIcon = m_transform.FindChild("New").gameObject;

            UIEventListener eventListener = m_obj.GetComponent<UIEventListener>();
            if (eventListener != null)
                eventListener.onClick = OnTabClick;
        }

        #region Button

        private void OnTabClick(GameObject obj)
        {
            if (m_selectTab != null)
                m_selectTab(m_tabType);
        }

        #endregion

        #region OnOff

        public void OnOff(bool b)
        {
            UpdateNew();

            m_background.spriteName = b ? "Btn_FriendTab_On" : "Btn_FriendTab_Off";
            m_background.depth = b ? 3 : 1;

            m_text.color = b ? new Color(255.0f / 255.0f, 247.0f / 255.0f, 236.0f / 255.0f) : new Color(156.0f / 255.0f, 149.0f / 255.0f, 138.0f / 255.0f);
        }

        public void UpdateNew()
        {
            m_newIcon.SetActive(AchievementManager.instance.CheckAchieveRewardEnable(m_tabType));
        }

        #endregion
    }
    #endregion
    private UITab[] m_tabGroup = null;

    private UIItemTooltip m_uiItemTooltip = null;
    public UIItemTooltip GetItemTooptip { get { return m_uiItemTooltip; } }

    private bool m_isButtonActive = false;
    public bool CheckButtonActive { get { return m_isButtonActive; } }

    private List<UIAchieveScrollGroup> m_scrollGroupList = null;

    public delegate void SelectTab(ACHIEVE_TAB_TYPE type);

    void Awake()
    {
        m_transform = transform;
        m_window = m_transform.FindChild("Anchor_Center/Window");

        #region Button
        m_window.FindChild("CloseButton").GetComponent<UIEventListener>().onClick = OnClose;
        m_window.FindChild("HelpButton").GetComponent<UIEventListener>().onClick = OnHelp;
        #endregion

        #region Tab
        SelectTab selectTab = new SelectTab(OnSelectTab);
        m_tabGroup = new UITab[(int)ACHIEVE_TAB_TYPE.TYPE_END];
        string[] tabPathArray = { "Tab_Group/Daily_Tab", "Tab_Group/Progress_Tab", "Tab_Group/Complete_Tab" };

        for (int i = 0; i < tabPathArray.Length; i++)
            m_tabGroup[i] = new UITab(m_window.FindChild(tabPathArray[i]).gameObject, (ACHIEVE_TAB_TYPE)i, selectTab);
        #endregion

        #region Scroll
        m_scrollPanel = m_window.FindChild("ScrollWindow/ScrollView").GetComponent<UIPanel>();
        m_scrollView = m_scrollPanel.GetComponent<UIScrollView>();
        m_scrollGrid = m_scrollPanel.transform.FindChild("Grid");
        m_scrollBar = m_window.FindChild("ScrollWindow/ScrollBar").GetComponent<UIScrollBar>();

        m_emptyView = m_window.FindChild("ScrollWindow/EmptyView").gameObject;

        InitItem();
        #endregion

        #region Object
        GameObject tooltipObj = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UIItemTooltip")) as GameObject;
        tooltipObj.transform.parent = m_window;
        tooltipObj.transform.localPosition = Vector3.zero;
        tooltipObj.transform.localScale = Vector3.one;
        m_uiItemTooltip = tooltipObj.GetComponent<UIItemTooltip>();
        #endregion
    }

    #region Window

    public void OpenWindow(ACHIEVE_TAB_TYPE type = ACHIEVE_TAB_TYPE.TYPE_DAILY)
    {
        m_isButtonActive = true;
        gameObject.SetActive(true);

        Hashtable hash = new Hashtable();
        hash.Add("amount", new Vector3(0.05f, 0.05f, 0f));
        hash.Add("time", 1f);
        hash.Add("ignoretimescale", true);
        iTween.PunchScale(m_window.gameObject, hash);

        UpdateWindow(type);

        SoundManager.instance.PlayAudioClip("UI_PopupOpen");
    }

    public void CloseWindow()
    {
        StopAllCoroutines();

        iTween[] tweenArray = m_window.gameObject.GetComponents<iTween>();
        for (int i = 0; i < tweenArray.Length; i++)
            MonoBehaviour.Destroy(tweenArray[i]);

        gameObject.SetActive(false);
    }

    public void RefreshWindow()
    {
        m_isButtonActive = true;

        UpdateSelectTab(m_selectTabType);
        UpdateList(false);
    }

    private void UpdateWindow(ACHIEVE_TAB_TYPE type)
    {
        m_selectTabType = type;

        UpdateSelectTab(type);
        UpdateList();
    }

    private void UpdateSelectTab(ACHIEVE_TAB_TYPE type)
    {
        int mainTabIndex = (int)type;
        for (int i = 0; i < (int)ACHIEVE_TAB_TYPE.TYPE_END; i++)
            m_tabGroup[i].OnOff(mainTabIndex.Equals(i));
    }

    #endregion

    #region Callback

    private void OnClose(GameObject obj)
    {
        Util.ButtonAnimation(obj);

        if (m_isButtonActive)
            CloseWindow();
    }

    private void OnHelp(GameObject obj)
    {
        Util.ButtonAnimation(obj);

        if (m_isButtonActive)
            MsgBox.instance.OpenMsgText(679, 680);
    }

    private void OnSelectTab(ACHIEVE_TAB_TYPE type)
    {
        UpdateWindow(type);
    }

    public void OnSendAchieveFinish(int achCode)
    {
        if (m_isButtonActive)
        {
            m_isButtonActive = false;
            AchievementManager.instance.SendAchieveFinish(achCode);
        }
    }

    #endregion

    #region ScrollView

    private void UpdateList(bool isReleaseScrollBar = true)
    {
        ReleaseAllItem();

        switch (m_selectTabType)
        {
            case ACHIEVE_TAB_TYPE.TYPE_DAILY:
                {
                    List<AchievementInfo>[] infoListArray = null;
                    if (AchievementManager.instance.GetDailyTabAchieveInfoList(out infoListArray))
                    {
                        GetItem().Update(m_selectTabType, ACHIEVE_TYPE.TYPE_NONE, infoListArray[0]);
                        GetItem().Update(m_selectTabType, ACHIEVE_TYPE.TYPE_DAILY_RANDOM, infoListArray[1]);
                        GetItem().Update(m_selectTabType, ACHIEVE_TYPE.TYPE_WEEKLY_RANDOM, infoListArray[2]);
                    }
                }
                break;
            case ACHIEVE_TAB_TYPE.TYPE_PROGRESS:
                {
                    List<AchievementInfo> infoList = null;
                    if (AchievementManager.instance.GetAchieveInfoList(ACHIEVE_TYPE.TYPE_PROGRESS, out infoList))
                    {
                        infoList.Sort(delegate (AchievementInfo a, AchievementInfo b)
                        {
                            float aValue = (float)AchievementManager.instance.GetServerAchieveInfo(a.index).achVal / a.value;
                            float bValue = (float)AchievementManager.instance.GetServerAchieveInfo(b.index).achVal / b.value;

                            if (aValue > bValue) return -1;
                            else if (aValue < bValue) return 1;
                            else return a.index.CompareTo(b.index);
                        });

                        GetItem().Update(m_selectTabType, ACHIEVE_TYPE.TYPE_PROGRESS, infoList);
                    }
                }
                break;
            case ACHIEVE_TAB_TYPE.TYPE_COMPLETE:
                {
                    List<AchievementInfo> infoList = null;
                    if (AchievementManager.instance.GetAchieveInfoList(ACHIEVE_TYPE.TYPE_NONE, out infoList))
                    {
                        if (infoList.Count.Equals(0)) m_emptyView.SetActive(true);
                        else GetItem().Update(m_selectTabType, ACHIEVE_TYPE.TYPE_NONE, infoList);
                    }
                }
                break;
        }

        UpdateItemPosition(isReleaseScrollBar);
    }

    private void UpdateItemPosition(bool isReleaseScrollBar = true)
    {
        int addHeight = 0;
        for (int i = 0; i < m_scrollGroupList.Count; i++)
        {
            m_scrollGroupList[i].Transform.localPosition = Vector3.down * addHeight;
            if (m_scrollGroupList[i].CheckActive)
                addHeight += m_scrollGroupList[i].GetSize;
        }

        if (isReleaseScrollBar)
            m_scrollBar.value = 0.0f;
    }

    private void InitItem()
    {
        GameObject createObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UIAchieve_ScrollGroup");
        m_scrollGroupList = new List<UIAchieveScrollGroup>();

        for (int i = 0; i < SCROLL_GROUP_SIZE; i++)
        {
            GameObject obj = MonoBehaviour.Instantiate(createObj) as GameObject;
            obj.transform.parent = m_scrollGrid;
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;

            UIAchieveScrollGroup item = new UIAchieveScrollGroup(obj);
            m_scrollGroupList.Add(item);
        }
    }

    private UIAchieveScrollGroup GetItem()
    {
        int index = m_scrollGroupList.FindIndex(delegate (UIAchieveScrollGroup item) {
            return !item.CheckActive;
        });

        return index >= 0 ? m_scrollGroupList[index] : null;
    }

    private void ReleaseAllItem()
    {
        for (int i = 0; i < m_scrollGroupList.Count; i++)
            m_scrollGroupList[i].SetActive(false);
        m_emptyView.SetActive(false);
    }

    #endregion
}