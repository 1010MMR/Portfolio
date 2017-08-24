using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public partial class EventWindow : MonoBehaviour
{
    public class UIEventWindowGroup
    {
        #region UIEventWindowScrollItem
        public class UIEventWindowScrollItem
        {
            private EVENT_TYPE m_eventType = EVENT_TYPE.EVENT_NONE;

            private GameObject m_obj = null;

            private Transform m_transform = null;
            public Transform Transform { get { return m_transform; } }

            private UILabel[] m_labelArray = null;
            private enum LABEL_TYPE
            {
                TYPE_TITLE,
                TYPE_VALUE,

                TYPE_END,
            }

            private UISprite[] m_spriteArray = null;
            private enum SPRITE_TYPE
            {
                TYPE_GEMS_ICON,
                TYPE_ITEM_ICON,
                TYPE_MATERIAL_ICON,
                TYPE_DOG_ICON,
                TYPE_LOCK_BACKGROUND,

                TYPE_END,
            }

            private GameObject m_checkObj = null;

            private int m_dayCount = 0;
            public int GetDayCount { get { return m_dayCount; } }

            private uint m_itemIndex = 0;

            private EventAttendRewardInfo m_attendInfo = null;
            public EventAttendRewardInfo GetAttendInfo { get { return m_attendInfo; } }

            private EventCollectMasterInfo.RewardItemInfo m_collectRewardInfo = null;
            public EventCollectMasterInfo.RewardItemInfo GetCollectRewardInfo { get { return m_collectRewardInfo; } }

            private EventReturnInfo m_returnInfo = null;
            public EventReturnInfo GetReturnInfo { get { return m_returnInfo; } }

            public UIEventWindowScrollItem() { }
            public UIEventWindowScrollItem(EVENT_TYPE type, GameObject obj)
            {
                m_eventType = type;

                m_obj = obj;
                m_transform = m_obj.transform;

                #region Label
                string[] labelPathArray = { "LabelRewardCount", "LabelCount" };
                m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

                for(int i = 0; i < m_labelArray.Length; i++)
                    m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
                #endregion

                #region Sprite
                string[] spritePathArray = { "Gem_Icon", "Item_Icon_Group/Icon", "Material_Icon_Group/Icon", "Dog_Icon_Group/Icon", "CheckBackground" };
                m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

                for(int i = 0; i < m_spriteArray.Length; i++)
                    m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
                #endregion

                #region Object
                m_checkObj = m_transform.FindChild("SpriteIconCheck").gameObject;
                UIEventListener eventListener = m_transform.GetComponent<UIEventListener>();
                if(eventListener != null)
                {
                    eventListener.onClick = OnClick;
                    eventListener.onPress = OnPress;
                }
                #endregion

                SetActive(false);
            }

            public void Init(int count, EventAttendRewardInfo info, bool isComplete = false)
            {
                m_dayCount = count;
                m_attendInfo = info;

                m_itemIndex = info.rewardCode;

                #region Label
                m_labelArray[(int)LABEL_TYPE.TYPE_TITLE].text = string.Format("{0}{1}", Str.instance.Get(410018), count);
                m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = info.rewardValue.ToString();
                #endregion

                #region Sprite
                ReleaseIconSprite();

                if(info.CheckGoodsType)
                {
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)info.rewardCode));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].MakePixelPerfect();
                }

                else if (info.CheckDogType)
                {
                    DogInfo dogInfo = WorldManager.instance.m_dataManager.m_dogData.GetDogData(info.rewardCode);

                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName(dogInfo.basicSkin));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                    m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                }

                else
                {
                    switch (Util.ParseItemMainType(info.rewardCode))
                    {
                        case ITEM_TYPE.DOGTICKET:
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardCode);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                            m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                            break;

                        default:
                            if (Util.CheckAtlasByItemType(Util.ParseItemMainType(info.rewardCode)))
                            {
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(true);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardCode);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].MakePixelPerfect();
                            }
                            else
                            {
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(true);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardCode);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].MakePixelPerfect();
                            }
                            break;
                    }
                }
                #endregion

                SetCheckObject(isComplete);
                SetActive(true);
            }

            public void Init(EventCollectMasterInfo.RewardItemInfo info, bool isComplete = false)
            {
                m_itemIndex = info.index;
                m_collectRewardInfo = info;

                #region Label
                m_labelArray[(int)LABEL_TYPE.TYPE_TITLE].text = WorldManager.instance.GetItemName(info.index);
                m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = info.count.ToString();
                #endregion

                #region Sprite
                ReleaseIconSprite();

                if(info.CheckGoodsType)
                {
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)info.index));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].MakePixelPerfect();
                }

                else if (info.CheckDogType)
                {
                    DogInfo dogInfo = WorldManager.instance.m_dataManager.m_dogData.GetDogData(info.index);

                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName(dogInfo.basicSkin));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                    m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                }

                else
                {
                    switch(Util.ParseItemMainType(info.index))
                    {
                        case ITEM_TYPE.DOGTICKET:
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                            m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                            break;

                        default:
                            if (Util.CheckAtlasByItemType(Util.ParseItemMainType(info.index)))
                            {
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(true);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].MakePixelPerfect();
                            }
                            else
                            {
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(true);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].MakePixelPerfect();
                            }
                            break;
                            
                    }
                }
                #endregion

                SetCheckObject(isComplete);
                SetActive(true);
            }

            public void Init(int count, EventReturnInfo info, bool isComplete = false)
            {
                m_dayCount = count;

                m_itemIndex = info.rewardIndex;
                m_returnInfo = info;

                #region Label
                m_labelArray[(int)LABEL_TYPE.TYPE_TITLE].text = info.sort.Equals(ReturnEventInfo.SORT_PACKAGE_INDEX) ? "" : string.Format("{0}{1}", Str.instance.Get(410018), count);
                m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = info.rewardValue.ToString();
                #endregion

                #region Sprite
                ReleaseIconSprite();

                if(info.CheckGoodsType)
                {
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)info.rewardIndex));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].MakePixelPerfect();
                }

                else if (info.CheckDogType)
                {
                    DogInfo dogInfo = WorldManager.instance.m_dataManager.m_dogData.GetDogData(info.rewardIndex);

                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName(dogInfo.basicSkin));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                    m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                }

                else
                {
                    switch (Util.ParseItemMainType(info.rewardIndex))
                    {
                        case ITEM_TYPE.DOGTICKET:
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                            m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                            break;

                        default:
                            if (Util.CheckAtlasByItemType(Util.ParseItemMainType(info.rewardIndex)))
                            {
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(true);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].MakePixelPerfect();
                            }
                            else
                            {
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(true);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].MakePixelPerfect();
                            }
                            break;
                    }
                }
                #endregion

                SetCheckObject(isComplete);
                SetActive(true);
            }

            #region Callback

            private void OnClick(GameObject obj)
            {
                switch(m_eventType)
                {
                    case EVENT_TYPE.EVENT_SPECIAL_REWARD:
                        if(EventManager.instance.GetSpecialEventInfo.CheckRewardEnable() &&
                            (EventManager.instance.GetSpecialEventInfo.GetRewardEnableDay() + 1).Equals(m_dayCount) &&
                            EventManager.instance.GetSpecialEventInfo.CheckRewardComplete(m_attendInfo.index) == false)
                        {
                            SoundManager.instance.PlayAudioClip("UI_Click");
                            EventManager.instance.SendEventAttendReceive(m_attendInfo.index);
                        }
                        break;

                    case EVENT_TYPE.EVENT_DAILY_REWARD:
                        if(EventManager.instance.CheckNormalRewardEnable() &&
                            (EventManager.instance.GetAttendDay + 1).Equals(m_dayCount))
                        {
                            SoundManager.instance.PlayAudioClip("UI_Click");
                            EventManager.instance.SendAttendReceive(m_attendInfo);
                        }

                        break;

                    case EVENT_TYPE.EVENT_COMEBACK:
                        {
                            ReturnEventInfo rEInfo = EventManager.instance.GetReturnEventInfo;
                            if (rEInfo != null)
                            {
                                if (rEInfo.m_dailyRewardList.Contains(m_returnInfo) && rEInfo.CheckRewardEnable() && rEInfo.GetRewardEnableDay.Equals(m_dayCount - 1))
                                {
                                    SoundManager.instance.PlayAudioClip("UI_Click");
                                    EventManager.instance.SendEventReturnReceive(new int[] { m_returnInfo.index });
                                }
                            }
                        }
                        break;
                }
            }

            private void OnPress(GameObject obj, bool isPress)
            {
                switch (m_eventType)
                {
                    case EVENT_TYPE.EVENT_SPECIAL_REWARD:
                        if (EventManager.instance.GetSpecialEventInfo.CheckRewardEnable() &&
                            (EventManager.instance.GetSpecialEventInfo.GetRewardEnableDay() + 1).Equals(m_dayCount) &&
                            EventManager.instance.GetSpecialEventInfo.CheckRewardComplete(m_attendInfo.index) == false)
                            return;
                        break;
                    case EVENT_TYPE.EVENT_DAILY_REWARD:
                        if (EventManager.instance.CheckNormalRewardEnable() &&
                            (EventManager.instance.GetAttendDay + 1).Equals(m_dayCount))
                            return;
                        break;
                    case EVENT_TYPE.EVENT_COMEBACK:
                        {
                            ReturnEventInfo rEInfo = EventManager.instance.GetReturnEventInfo;
                            if (rEInfo != null)
                            {
                                if (rEInfo.m_dailyRewardList.Contains(m_returnInfo) && rEInfo.CheckRewardEnable() && rEInfo.GetRewardEnableDay.Equals(m_dayCount - 1))
                                    return;
                            }
                        }
                        break;
                }

                UIItemTooltip tooltip = EventManager.instance.GetEventWindow.GetItemTooptip;

                tooltip.OnOffTooltip(isPress);
                if (isPress)
                {
                    SoundManager.instance.PlayAudioClip("UI_Click");
                    tooltip.UpdateTooltip(m_itemIndex, m_transform);
                }
            }

            #endregion

            #region Mark

            public void SetCheckObject(bool b, bool isInstantly = true)
            {
                m_checkObj.SetActive(b);
                if(b && isInstantly == false)
                {
                    m_checkObj.transform.localScale = Vector3.one * 1.3f;

                    Hashtable hash = new Hashtable();
                    hash.Add("scale", Vector3.one);
                    hash.Add("time", 1f);
                    hash.Add("ignoretimescale", true);
                    iTween.ScaleTo(m_checkObj, hash);
                }

                m_spriteArray[(int)SPRITE_TYPE.TYPE_LOCK_BACKGROUND].gameObject.SetActive(b);
            }

            #endregion

            #region Util

            private void ReleaseIconSprite()
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(false);
            }

            #endregion

            public void SetActive(bool b)
            {
                m_obj.SetActive(b);
            }
        }
        #endregion

        public GameObject m_obj = null;
        public Transform m_transform = null;

        public EventMasterInfo m_masterInfo = null;

        public UIEventWindowGroup() { }
        public UIEventWindowGroup(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;

            m_obj = obj;
            m_transform = obj.transform;
        }

        public virtual void Init()
        {
        }

        public virtual void UpdateWindow(params object[] param)
        {
        }

        public virtual void Response(params object[] param)
        {
        }

        public virtual void SetActive(bool b)
        {
            m_obj.SetActive(b);
        }
    }

    #region UIEventChargeReward

    private class UIEventChargeReward : UIEventWindowGroup
    {
        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_CHARGE_VALUE,
            TYPE_REWARD_VALUE,
            TYPE_TERM_VALUE,

            TYPE_END,
        }

        private UISprite[] m_spriteArray = null;
        private enum SPRITE_TYPE
        {
            TYPE_GEMS_ICON,

            TYPE_END,
        }

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,
            TYPE_TITLE,
            TYPE_DESC,

            TYPE_END,
        }

        public UIEventChargeReward(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;

            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "Info_Group/ChargeValue", "Info_Group/RewardValue", "Term_Value" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Sprite
            string[] spritePathArray = { "Info_Group/RewardIcon" };
            m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

            for(int i = 0; i < m_spriteArray.Length; i++)
                m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
            #endregion

            #region UITexture
            string[] texturePathArray = { "Background", "Title", "Desc01" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            SEventInfo eventInfo = EventManager.instance.GetEventInfo((int)EVENT_TYPE.EVENT_CHARGE_REWARD);
            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }

            m_labelArray[(int)LABEL_TYPE.TYPE_CHARGE_VALUE].text = string.Format("{0:F2}", OrderManager.instance.GetOrderResultInfo.m_payMoney);
            m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_VALUE].text = string.Format("{0}", OrderManager.instance.GetOrderResultInfo.m_payRewardCash * 2);
            m_labelArray[(int)LABEL_TYPE.TYPE_TERM_VALUE].text = string.Format("[FFFFFF]{0} : {1} ~ {2}[-]", Str.instance.Get(482), Util.GetTimeStringYear(eventInfo.sTime, false), Util.GetTimeStringYear(eventInfo.eTime, false));
        }
    }

    #endregion

    #region UIEventDailyReward

    private class UIEventDailyReward : UIEventWindowGroup
    {
        private const int MAX_ITEM_COUNT = 32;

        private GameObject m_itemObj = null;
        private List<UIEventWindowScrollItem> m_itemList = null;

        private UIPanel m_scrollPanel = null;
        private UIScrollView m_scrollView = null;
        private UIGrid m_scrollGrid = null;

        private GameObject m_curEffectObj = null;
        private GameObject m_nextEffectObj = null;

        private UILabel m_countLabel = null;

        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_COUNT,
            TYPE_TERMS,

            TYPE_END,
        }

        public UIEventDailyReward(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;

            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "RewardCount", "EventTerm" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Scroll
            m_itemObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/EventWindow_ScrollItem");
            m_itemList = new List<UIEventWindowScrollItem>();

            m_scrollPanel = m_transform.FindChild("ScrollWindow/ScrollView").GetComponent<UIPanel>();
            m_scrollView = m_scrollPanel.GetComponent<UIScrollView>();
            m_scrollGrid = m_scrollPanel.transform.FindChild("Grid").GetComponent<UIGrid>();

            InitItem();
            #endregion

            #region Object
            m_curEffectObj = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_Reward01")) as GameObject;
            m_nextEffectObj = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_Reward00")) as GameObject;

            SetCurrentTarget(false, null);
            SetNextTarget(false, null);
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            UpdateEventDay();
            UpdateList();
        }

        #region Scroll

        #region Item

        private void InitItem()
        {
            for(int i = 0; i < MAX_ITEM_COUNT; i++)
            {
                GameObject obj = MonoBehaviour.Instantiate(m_itemObj) as GameObject;
                obj.transform.parent = m_scrollGrid.transform;
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = Vector3.zero;

                UIEventWindowScrollItem item = new UIEventWindowScrollItem(EVENT_TYPE.EVENT_DAILY_REWARD, obj);
                m_itemList.Add(item);
            }
        }

        private UIEventWindowScrollItem GetItem(int index)
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

            SetCurrentTarget(false, null);
            SetNextTarget(false, null);

            List<EventAttendRewardInfo> infoList = null;
            if(EventManager.instance.GetEventTable.GetAttendRewardList(Util.UnixTimeToDateTime(Util.GetNowGameTime()).Month, out infoList))
            {
                for(int i = 0; i < infoList.Count; i++)
                {
                    UIEventWindowScrollItem item = GetItem(i);
                    if(item != null)
                    {
                        item.Init(i + 1, infoList[i], EventManager.instance.GetAttendDay > i);
                        if(EventManager.instance.GetAttendDay.Equals(i))
                        {
                            if(EventManager.instance.CheckNormalRewardEnable())
                                SetCurrentTarget(true, item.Transform);
                            else
                                SetNextTarget(true, item.Transform);
                        }
                    }
                }
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

        #region Target

        private void SetCurrentTarget(bool b, Transform target)
        {
            if(m_curEffectObj != null)
            {
                m_curEffectObj.transform.parent = target;
                m_curEffectObj.transform.localPosition = Vector3.zero;
                m_curEffectObj.transform.localScale = Vector3.one;

                m_curEffectObj.SetActive(b);
            }
        }

        private void SetNextTarget(bool b, Transform target)
        {
            if(m_nextEffectObj != null)
            {
                m_nextEffectObj.transform.parent = target;
                m_nextEffectObj.transform.localPosition = Vector3.zero;
                m_nextEffectObj.transform.localScale = Vector3.one;

                m_nextEffectObj.SetActive(b);
            }
        }

        #endregion

        public override void Response(params object[] param)
        {
            int attendDay = (int)param[0];
            for(int i = 0; i < m_itemList.Count; i++)
            {
                if(m_itemList[i].GetDayCount.Equals(attendDay))
                {
                    m_itemList[i].SetCheckObject(true, false);

                    SetCurrentTarget(false, null);
                    if(m_itemList.Count > i + 1)
                        SetNextTarget(true, m_itemList[i + 1].Transform);

                    MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), m_itemList[i].GetAttendInfo.rewardCode, 
                                                m_itemList[i].GetAttendInfo.rewardValue);
                    break;
                }
            }

            UpdateEventDay();
        }

        private void UpdateEventDay()
        {
            SEventInfo eventInfo = EventManager.instance.GetEventInfo((int)EVENT_TYPE.EVENT_DAILY_REWARD);

            m_labelArray[(int)LABEL_TYPE.TYPE_COUNT].text = string.Format("{0} : {1}", Str.instance.Get(485), Str.instance.Get(662, "%COUNT%", EventManager.instance.GetAttendDay.ToString()));
            m_labelArray[(int)LABEL_TYPE.TYPE_TERMS].text = string.Format("{0} : {1} ~ {2}", Str.instance.Get(482), Util.GetTimeStringYear(eventInfo.sTime, false), Util.GetTimeStringYear(eventInfo.eTime, false));
        }
    }

    #endregion

    #region UIEventSpecialReward

    private class UIEventSpecialReward : UIEventWindowGroup
    {
        private const int MAX_ITEM_COUNT = 7;
        private readonly Vector3[] DAILY_ICON_POS = { new Vector3(0, 0, 0), new Vector3(108, 0, 0), new Vector3(216, 0, 0), 
                                                        new Vector3(-54, -144, 0), new Vector3(54, -144, 0), new Vector3(162, -144, 0), new Vector3(270, -144, 0) };

        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_REWARD_VALUE,

            TYPE_END,
        }

        private UISprite[] m_spriteArray = null;
        private enum SPRITE_TYPE
        {
            TYPE_GEMS_ICON,
            TYPE_ITEM_ICON,
            TYPE_MATERIAL_ICON,
            TYPE_DOG_ICON,
            TYPE_SELECT_ICON,

            TYPE_END,
        }

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_TITLE,

            TYPE_END,
        }

        private GameObject[] m_EffectArray = null;
        private enum EFFECT_TYPE
        {
            TYPE_CURRENT,
            TYPE_NEXT,
            TYPE_FINAL,

            TYPE_END,
        }

        private Transform m_finalRewardObj = null;
        private EventAttendRewardInfo m_finalRewardInfo = null;

        private List<UIEventWindowScrollItem> m_itemList = null;

        public UIEventSpecialReward(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "Special_Reward/Value" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Sprite
            string[] spritePathArray = { "Special_Reward/Gems_Icon_Group/Icon", "Special_Reward/Item_Icon_Group/Icon", "Special_Reward/Material_Icon_Group/Icon", 
                                         "Special_Reward/Dog_Icon_Group/Icon", "Special_Reward/SpriteIconCheck" };
            m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

            for(int i = 0; i < m_spriteArray.Length; i++)
                m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
            #endregion

            #region UITexture
            string[] texturePathArray = { "Title" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Object
            GameObject createObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/EventWindow_ScrollItem");
            Transform parent = m_transform.FindChild("Daily_Reward_Group");

            m_itemList = new List<UIEventWindowScrollItem>();

            for(int i = 0; i < MAX_ITEM_COUNT; i++)
            {
                GameObject obj = Instantiate(createObj) as GameObject;
                obj.transform.parent = parent;
                obj.transform.localPosition = DAILY_ICON_POS[i];
                obj.transform.localScale = Vector3.one;

                m_itemList.Add(new UIEventWindowScrollItem(EVENT_TYPE.EVENT_SPECIAL_REWARD, obj));
            }

            m_finalRewardObj = m_transform.FindChild("Special_Reward");
            UIEventListener eventListener = m_finalRewardObj.GetComponent<UIEventListener>();
            if(eventListener != null)
            {
                eventListener.onClick = OnFinalRewardClick;
                eventListener.onPress = OnFinalRewardPress;
            }
            #endregion

            #region Effect
            m_EffectArray = new GameObject[(int)EFFECT_TYPE.TYPE_END];

            m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT] = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_Reward01")) as GameObject;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.parent = parent;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.localScale = Vector3.one;
            SetCurrentEffect(false, null);

            m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT] = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_Reward00")) as GameObject;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.parent = parent;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.localScale = Vector3.one;
            SetNextEffect(false, null);

            m_EffectArray[(int)EFFECT_TYPE.TYPE_FINAL] = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_Reward02")) as GameObject;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_FINAL].transform.parent = m_transform.FindChild("Special_Reward");
            m_EffectArray[(int)EFFECT_TYPE.TYPE_FINAL].transform.localPosition = Vector3.zero;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_FINAL].transform.localScale = Vector3.one;
            SetFinalEffect(false);
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            SetCurrentEffect(false, null);
            SetNextEffect(false, null);

            List<EventAttendRewardInfo> infoList = null;
            if(EventManager.instance.GetSpecialEventInfo.GetSpecialEventList(out infoList))
            {
                #region Daily
                int dailyCount = -1;
                for(int i = 0; i < infoList.Count; i++)
                {
                    switch(infoList[i].GetSpecialIndex)
                    {
                        case 1:
                            dailyCount++;
                            if(m_itemList.Count > dailyCount)
                            {
                                m_itemList[i].Init(i + 1, infoList[i], EventManager.instance.GetSpecialEventInfo.CheckRewardComplete(infoList[i].index));
                                if(EventManager.instance.GetSpecialEventInfo.m_eventRewardCount.Equals(i))
                                {
                                    if(EventManager.instance.GetSpecialEventInfo.CheckRewardEnable())
                                        SetCurrentEffect(true, m_itemList[i].Transform);
                                    else
                                        SetNextEffect(true, m_itemList[i].Transform);
                                }
                            }
                            break;
                        case 2:
                            m_finalRewardInfo = infoList[i];
                            break;
                    }
                }
                #endregion

                #region Final
                if(m_finalRewardInfo != null)
                {
                    ReleaseIconSprite();

                    m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_VALUE].text = string.Format("{0}", m_finalRewardInfo.rewardValue.ToString());

                    if(m_finalRewardInfo.CheckGoodsType)
                    {
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)m_finalRewardInfo.rewardCode));
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].MakePixelPerfect();
                    }

                    else if (m_finalRewardInfo.CheckDogType)
                    {
                        DogInfo dogInfo = WorldManager.instance.m_dataManager.m_dogData.GetDogData(m_finalRewardInfo.rewardCode);

                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName(dogInfo.basicSkin));
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                        m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_VALUE].text = "";
                    }

                    else
                    {
                        ITEM_TYPE parseType = Util.ParseItemMainType(m_finalRewardInfo.rewardCode);
                        if (parseType.Equals(ITEM_TYPE.DOGTICKET))
                        {
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = WorldManager.instance.GetGUISpriteName(m_finalRewardInfo.rewardCode);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                            m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_VALUE].text = "";
                        }
                        else if (Util.CheckAtlasByItemType(parseType))
                        {
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].spriteName = WorldManager.instance.GetGUISpriteName(m_finalRewardInfo.rewardCode);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].MakePixelPerfect();
                        }

                        else
                        {
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].spriteName = WorldManager.instance.GetGUISpriteName(m_finalRewardInfo.rewardCode);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].MakePixelPerfect();
                        }
                    }

                    bool isFinalRewardComplete = EventManager.instance.GetSpecialEventInfo.CheckRewardComplete(m_finalRewardInfo.index);

                    SetFinalEffect(EventManager.instance.GetSpecialEventInfo.CheckFinalRewardEnable() && !isFinalRewardComplete);
                    SetFinalRewardCheck(isFinalRewardComplete);
                }
                #endregion
            }
            
            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        public override void Response(params object[] param)
        {
            int rewardindex = (int)param[0];
            EventAttendRewardInfo info = EventManager.instance.GetEventTable.GetSpecialAttendInfo(rewardindex);

            switch(info.GetSpecialIndex)
            {
                case 1:
                    for(int i = 0; i < m_itemList.Count; i++)
                    {
                        if(m_itemList[i].GetAttendInfo.index.Equals(info.index))
                        {
                            m_itemList[i].SetCheckObject(true, false);
                            SetCurrentEffect(false, null);
                            if(m_itemList.Count > i + 1)
                                SetNextEffect(true, m_itemList[i + 1].Transform);

                            MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), m_itemList[i].GetAttendInfo.rewardCode, 
                                                        m_itemList[i].GetAttendInfo.rewardValue);
                            break;
                        }
                    }
                    break;

                case 2:
                    SetFinalEffect(false);
                    SetFinalRewardCheck(true, false);

                    MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), m_finalRewardInfo.rewardCode, m_finalRewardInfo.rewardValue);
                    break;
            }
        }

        #region Effect

        private void SetCurrentEffect(bool b, Transform target)
        {
            if(m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT] != null)
            {
                m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.parent = target;
                m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.localPosition = Vector3.zero;
                m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.localScale = Vector3.one;

                m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].SetActive(b);
            }
        }

        private void SetNextEffect(bool b, Transform target)
        {
            if(m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT] != null)
            {
                m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.parent = target;
                m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.localPosition = Vector3.zero;
                m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.localScale = Vector3.one;

                m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].SetActive(b);
            }
        }

        private void SetFinalEffect(bool b)
        {
            if(m_EffectArray[(int)EFFECT_TYPE.TYPE_FINAL] != null)
                m_EffectArray[(int)EFFECT_TYPE.TYPE_FINAL].SetActive(b);
        }

        #endregion

        #region Final_Reward

        private void OnFinalRewardClick(GameObject obj)
        {
            if(m_finalRewardInfo != null && EventManager.instance.GetSpecialEventInfo.CheckFinalRewardEnable())
            {
                SoundManager.instance.PlayAudioClip("UI_Click");
                EventManager.instance.SendEventAttendReceive(m_finalRewardInfo.index);
            }
        }

        private void OnFinalRewardPress(GameObject obj, bool isPress)
        {
            if(m_finalRewardInfo != null && EventManager.instance.GetSpecialEventInfo.CheckFinalRewardEnable() == false)
            {
                UIItemTooltip tooltip = EventManager.instance.GetEventWindow.GetItemTooptip;

                tooltip.OnOffTooltip(isPress);
                if (isPress)
                {
                    SoundManager.instance.PlayAudioClip("UI_Click");
                    tooltip.UpdateTooltip(m_finalRewardInfo.rewardCode, m_finalRewardObj);
                }
            }
        }

        private void SetFinalRewardCheck(bool b, bool isInstantly = true)
        {
            m_spriteArray[(int)SPRITE_TYPE.TYPE_SELECT_ICON].gameObject.SetActive(b);
            if(b && isInstantly == false)
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_SELECT_ICON].gameObject.transform.localScale = Vector3.one * 1.3f;

                Hashtable hash = new Hashtable();
                hash.Add("scale", Vector3.one);
                hash.Add("time", 1f);
                hash.Add("ignoretimescale", true);
                iTween.ScaleTo(m_spriteArray[(int)SPRITE_TYPE.TYPE_SELECT_ICON].gameObject, hash);
            }
        }

        #endregion

        #region Util

        private void ReleaseIconSprite()
        {
            m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(false);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(false);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(false);
            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(false);
        }

        #endregion
    }

    #endregion

    #region UIEventBurningTime

    private class UIEventBurningTime : UIEventWindowGroup
    {
        private int MAX_INFO_COUNT = 3;

        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_DESC,
            TYPE_TERM_VALUE,
            TYPE_VIEW_VALUE,

            TYPE_END,
        }

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,
            TYPE_TITLE,

            TYPE_END,
        }

        #region UITimeInfo
        private class UITimeInfo
        {
            private GameObject m_obj = null;
            private Transform m_transform = null;

            private UILabel[] m_labelArray = null;
            private enum LABEL_TYPE
            {
                TYPE_TIME01,
                TYPE_TIME02,

                TYPE_END,
            }

            public UITimeInfo() { }
            public UITimeInfo(GameObject obj)
            {
                m_obj = obj;
                m_transform = obj.transform;

                #region Label
                string[] labelPathArray = { "Time01", "Time02" };
                m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

                for(int i = 0; i < m_labelArray.Length; i++)
                    m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
                #endregion
            }

            public void UpdateInfo(EventBurningTimeInfo info)
            {
                m_labelArray[(int)LABEL_TYPE.TYPE_TIME01].text = Str.instance.Get(562,
                    new string[] { "%AMPM%", "%TIME%" },
                    new string[] { Str.instance.Get(info.GetStartTime > 12 ? 497 : 496), string.Format("{0:D2}", info.GetStartTime > 12 ? info.GetStartTime - 12 : info.GetStartTime) });
                m_labelArray[(int)LABEL_TYPE.TYPE_TIME02].text = Str.instance.Get(562,
                    new string[] { "%AMPM%", "%TIME%" },
                    new string[] { Str.instance.Get(info.GetEndTime > 12 ? 497 : 496), string.Format("{0:D2}", info.GetEndTime > 12 ? info.GetEndTime - 12 : info.GetEndTime) });

                SetActive(true);
            }

            public void SetActive(bool b)
            {
                m_obj.SetActive(b);
            }
        }
        #endregion
        private UITimeInfo[] m_uiTimeInfoGroup = null;

        public UIEventBurningTime(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "Desc", "Term_Value", "ViewDesc" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region UITexture
            string[] texturePathArray = { "Background", "Title" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region InfoGroup
            m_uiTimeInfoGroup = new UITimeInfo[MAX_INFO_COUNT];
            for (int i = 0; i < m_uiTimeInfoGroup.Length; i++)
                m_uiTimeInfoGroup[i] = new UITimeInfo(m_transform.FindChild(string.Format("Info_Group/Info_{0:D2}", i + 1)).gameObject);
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            ReleaseInfoGroup();

            SEventInfo eventInfo = EventManager.instance.GetEventInfo((int)EVENT_TYPE.EVENT_BURNING_TIME);

            m_labelArray[(int)LABEL_TYPE.TYPE_DESC].text = GetBurningDesc();
            m_labelArray[(int)LABEL_TYPE.TYPE_VIEW_VALUE].text = Str.instance.Get(572);
            m_labelArray[(int)LABEL_TYPE.TYPE_TERM_VALUE].text = string.Format("{0} ~ {1}", Util.GetTimeStringYear(eventInfo.sTime, false), Util.GetTimeStringYear(eventInfo.eTime, false));

            List<EventBurningTimeInfo> infoList = null;
            if (EventManager.instance.GetBurningTimeInfo.GetBurningInfoList(out infoList))
            {
                int count = 0;
                List<int> startTimeCheck = new List<int>();

                for (int i = 0; i < infoList.Count; i++)
                {
                    if (startTimeCheck.Contains(infoList[i].startTime) == false)
                    {
                        m_uiTimeInfoGroup[count].UpdateInfo(infoList[i]);

                        startTimeCheck.Add(infoList[i].startTime);
                        count++;
                    }
                }
            }
            
            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        private string GetBurningDesc()
        {
            List<BURNING_EVENT_TYPE> typeList = EventManager.instance.GetBurningTimeInfo.GetBurningEventType();
            float addValue = EventManager.instance.GetBurningTimeInfo.GetBurningEventValue();

            if (typeList.Count.Equals(1))
            {
                switch (typeList[0])
                {
                    case BURNING_EVENT_TYPE.TYPE_GOLD:
                        return Str.instance.Get(557, "%VALUE%", addValue.ToString());
                    case BURNING_EVENT_TYPE.TYPE_EXP:
                        return Str.instance.Get(558, "%VALUE%", addValue.ToString());
                    default:
                        return "";
                }
            }

            else
                return Str.instance.Get(559, "%VALUE%", addValue.ToString());
        }

        private void ReleaseInfoGroup()
        {
            for (int i = 0; i < m_uiTimeInfoGroup.Length; i++)
                m_uiTimeInfoGroup[i].SetActive(false);
        }
    }

    #endregion

    #region UIEventTimeReward

    private class UIEventTimeReward : UIEventWindowGroup
    {
        private const int MAX_ITEM_COUNT = 3;

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_TITLE,

            TYPE_END,
        }

        private UIPanel m_scrollPanel = null;
        private UIScrollView m_scrollView = null;
        private UIGrid m_scrollGrid = null;

        #region RewardGroup
        private class UIScrollItem
        {
            private GameObject m_obj = null;
            private Transform m_transform = null;

            private UILabel[] m_labelArray = null;
            private enum LABEL_TYPE
            {
                TYPE_NAME,
                TYPE_COUNT,
                TYPE_DESC,
                TYPE_BUTTON_TEXT,

                TYPE_END,
            }

            private UISprite[] m_spriteArray = null;
            private enum SPRITE_TYPE
            {
                TYPE_GEMS_ICON,
                TYPE_ITEM_ICON,
                TYPE_MATERIAL_ICON,
                TYPE_DOG_ICON,
                TYPE_CHECK,
                TYPE_BUTTON_BACKGROUND,

                TYPE_END,
            }

            private GameObject m_button = null;
            private Transform m_tooltipTarget = null;

            private EventTimeRewardInfo m_info = null;
            public EventTimeRewardInfo GetTimeRewardInfo { get { return m_info; } }

            public UIScrollItem() { }
            public UIScrollItem(GameObject obj)
            {
                m_obj = obj;
                m_transform = obj.transform;

                #region Label
                string[] labelPathArray = { "Name", "Count", "Desc", "Reward_Button/Text" };
                m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

                for(int i = 0; i < m_labelArray.Length; i++)
                    m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
                #endregion

                #region Sprite
                string[] spritePathArray = { "Gem_Icon", "Item_Icon_Group/Icon", "Material_Icon_Group/Icon", "Dog_Icon_Group/Icon", "Complete", "Reward_Button/Background" };
                m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

                for(int i = 0; i < m_spriteArray.Length; i++)
                    m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
                #endregion

                #region Object
                m_button = m_transform.FindChild("Reward_Button").gameObject;
                m_button.GetComponent<UIEventListener>().onClick = OnClick;

                m_tooltipTarget = m_transform.FindChild("Tooltip");
                m_tooltipTarget.GetComponent<UIEventListener>().onPress = OnPress;
                #endregion

                SetActive(false);
            }

            public void UpdateRewardGroup(EventTimeRewardInfo info)
            {
                m_info = info;

                bool isExists = info != null;
                if(isExists)
                    UpdateItem(info);

                SetActive(isExists);
            }

            public void Refresh()
            {
                if(m_info != null)
                    UpdateItem(m_info);
            }

            #region Callback

            private void OnClick(GameObject obj)
            {
                Util.ButtonAnimation(obj);
                EventManager.instance.SendEventTimeReceive(m_info.index);
            }

            private void OnPress(GameObject obj, bool isPress)
            {
                UIItemTooltip tooltip = EventManager.instance.GetEventWindow.GetItemTooptip;

                tooltip.OnOffTooltip(isPress);
                if(isPress)
                {
                    SoundManager.instance.PlayAudioClip("UI_Click");
                    tooltip.UpdateTooltip(m_info.rewardIndex, m_tooltipTarget);
                }
            }

            #endregion

            #region Util

            private void UpdateItem(EventTimeRewardInfo info)
            {
                #region Label
                m_labelArray[(int)LABEL_TYPE.TYPE_DESC].text = Str.instance.Get(491,
                    new string[] { "%AMPM01%", "%TIME01%", "%AMPM02%", "%TIME02%" },
                    new string[] { Str.instance.Get(info.GetStartTime > 12 ? 497 : 496), string.Format("{0:D2}", info.GetStartTime > 12 ? info.GetStartTime - 12 : info.GetStartTime),
                                         Str.instance.Get(info.GetEndTime > 12 ? 497 : 496), string.Format("{0:D2}", info.GetEndTime > 12 ? info.GetEndTime - 12 : info.GetEndTime) });
                #endregion

                #region Info
                ReleaseIconSprite();

                if(info.CheckGoodsType)
                {
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)info.rewardIndex));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].MakePixelPerfect();

                    m_labelArray[(int)LABEL_TYPE.TYPE_NAME].text = Util.GetGoodsNameString(Util.GetGoodsTypeByIndex((int)info.rewardIndex));
                    m_labelArray[(int)LABEL_TYPE.TYPE_COUNT].text = string.Format("X{0}", info.rewardValue);
                }

                else if (info.CheckDogType)
                {
                    DogInfo dogInfo = WorldManager.instance.m_dataManager.m_dogData.GetDogData(info.rewardIndex);

                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName(dogInfo.basicSkin));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                    m_labelArray[(int)LABEL_TYPE.TYPE_COUNT].text = "";
                }

                else
                {
                    ITEM_TYPE parseType = Util.ParseItemMainType(info.rewardIndex);
                    if (parseType.Equals(ITEM_TYPE.DOGTICKET))
                    {
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                        m_labelArray[(int)LABEL_TYPE.TYPE_COUNT].text = "";
                    }
                    else if (Util.CheckAtlasByItemType(parseType))
                    {
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].MakePixelPerfect();

                        m_labelArray[(int)LABEL_TYPE.TYPE_NAME].text = WorldManager.instance.GetItemName(info.rewardIndex);
                        m_labelArray[(int)LABEL_TYPE.TYPE_COUNT].text = string.Format("X{0}", info.rewardValue);
                    }

                    else
                    {
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.rewardIndex);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].MakePixelPerfect();

                        m_labelArray[(int)LABEL_TYPE.TYPE_NAME].text = WorldManager.instance.GetItemName(info.rewardIndex);
                        m_labelArray[(int)LABEL_TYPE.TYPE_COUNT].text = string.Format("X{0}", info.rewardValue);
                    }
                }
                #endregion

                #region Button
                SetButtonActive(EventManager.instance.CheckEventRewardComplete(info.index), info.CheckGetRewardEnableTime());
                #endregion
            }

            private void ReleaseIconSprite()
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(false);
            }

            private void SetButtonActive(bool isComplete, bool isActive = true)
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_CHECK].gameObject.SetActive(isComplete);
                m_button.SetActive(!isComplete);

                if(isComplete == false)
                {
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_BUTTON_BACKGROUND].spriteName = isActive ? "Btn_Green" : "Btn_Gray";

                    m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].color = isActive ? new Vector4(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f) : new Vector4(206.0f / 255.0f, 206.0f / 255.0f, 206.0f / 255.0f, 255.0f / 255.0f);
                    m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].effectColor = isActive ? new Vector4(91.0f / 255.0f, 97.0f / 255.0f, 65.0f / 255.0f, 255.0f / 255.0f) : new Vector4(88.0f / 255.0f, 77.0f / 255.0f, 77.0f / 255.0f, 255.0f / 255.0f);

                    m_button.GetComponent<Collider>().enabled = isActive;
                }
            }

            #endregion

            public void SetActive(bool b)
            {
                m_obj.SetActive(b);
            }
        }
        #endregion
        private GameObject m_itemObj = null;
        private List<UIScrollItem> m_itemList = null;

        public UIEventTimeReward(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region UITexture
            string[] texturePathArray = { "Title" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Scroll
            m_itemObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/EventWindow_TimeRewardItem");
            m_itemList = new List<UIScrollItem>();

            m_scrollPanel = m_transform.FindChild("ScrollWindow/ScrollView").GetComponent<UIPanel>();
            m_scrollView = m_scrollPanel.GetComponent<UIScrollView>();
            m_scrollGrid = m_scrollPanel.transform.FindChild("Grid").GetComponent<UIGrid>();

            InitItem();
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            UpdateList();

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        public override void Response(params object[] param)
        {
            int newIndex = (int)param[0];
            for (int i = 0; i < m_itemList.Count; i++)
            {
                if (m_itemList[i].GetTimeRewardInfo != null &&
                    m_itemList[i].GetTimeRewardInfo.index.Equals(newIndex))
                {
                    m_itemList[i].Refresh();
                    MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), m_itemList[i].GetTimeRewardInfo.rewardIndex,
                                                m_itemList[i].GetTimeRewardInfo.rewardValue);
                    break;
                }

            }
        }

        #region Scroll

        #region Item

        private void InitItem()
        {
            for(int i = 0; i < MAX_ITEM_COUNT; i++)
                MakeItem();
        }

        private UIScrollItem MakeItem()
        {
            GameObject obj = MonoBehaviour.Instantiate(m_itemObj) as GameObject;
            obj.transform.parent = m_scrollGrid.transform;
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;

            UIScrollItem item = new UIScrollItem(obj);
            m_itemList.Add(item);

            return item;
        }

        private UIScrollItem GetItem(int index)
        {
            return m_itemList.Count > index ? m_itemList[index] : MakeItem();
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

            List<EventTimeRewardInfo> infoList = null;
            if(EventManager.instance.GetEventTable.GetEventTimeRewardList((int)Util.GetNowDateGameTime().DayOfWeek + 1, out infoList))
            {
                for (int i = 0; i < infoList.Count; i++)
                {
                    UIScrollItem item = GetItem(i);
                    if (item != null)
                        item.UpdateRewardGroup(infoList[i]);
                }
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
    }

    #endregion

    #region UIEventWeb

    private class UIEventWeb : UIEventWindowGroup
    {
        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,

            TYPE_END,
        }

        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_TERM_VALUE,

            TYPE_END,
        }

        public UIEventWeb(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region UITexture
            string[] texturePathArray = { "Background" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Label
            string[] labelPathArray = { "Term_Value" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for (int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            m_textureArray[(int)TEXTURE_TYPE.TYPE_BACGROUND].GetComponent<UIEventListener>().onClick = OnLinkButton;
        }

        public override void UpdateWindow(params object[] param)
        {
            m_masterInfo.clientTimeText.SetTimeText(m_labelArray[(int)LABEL_TYPE.TYPE_TERM_VALUE]);

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        #region Callback

        private void OnLinkButton(GameObject obj)
        {
            if (m_masterInfo.url != null && m_masterInfo.url.Equals("0") == false)
                Application.OpenURL(m_masterInfo.url);
            else if (m_masterInfo.locationIndex.Equals(0) == false)
            {
                LocationManager.instance.MoveDirection(m_masterInfo.locationIndex);
                EventManager.instance.GetEventWindow.CloseWindow();
            }
        }

        #endregion
    }

    #endregion

    #region UIEventSingleAchieve

    private class UIEventSingleAchieve : UIEventWindowGroup
    {
        private int MAX_INFO_COUNT = 3;

        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_COMPLETE_VALUE,
            TYPE_BUTTON_TEXT,
            TYPE_TERMS,

            TYPE_END,
        }

        private UISprite[] m_spriteArray = null;
        private enum SPRITE_TYPE
        {
            TYPE_BUTTON_BACKGROUND,

            TYPE_END,
        }

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,
            TYPE_TITLE,
            TYPE_DESC,

            TYPE_END,
        }

        #region UIRewardInfo
        private class UIRewardInfo
        {
            private GameObject m_obj = null;
            private Transform m_transform = null;

            private UILabel[] m_labelArray = null;
            private enum LABEL_TYPE
            {
                TYPE_TITLE,
                TYPE_VALUE,

                TYPE_END,
            }

            private UISprite[] m_spriteArray = null;
            private enum SPRITE_TYPE
            {
                TYPE_GEMS_ICON,
                TYPE_ITEM_ICON,
                TYPE_MATERIAL_ICON,
                TYPE_DOG_ICON,

                TYPE_END,
            }

            private EventAchieveInfo.RewardItemInfo m_info = null;

            public UIRewardInfo() { }
            public UIRewardInfo(GameObject obj)
            {
                m_obj = obj;
                m_transform = obj.transform;

                #region Label
                string[] labelPathArray = { "Title", "Count" };
                m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

                for(int i = 0; i < m_labelArray.Length; i++)
                    m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
                #endregion

                #region Sprite
                string[] spritePathArray = { "Gem_Icon", "Item_Icon_Group/Icon", "Material_Icon_Group/Icon", "Dog_Icon_Group/Icon" };
                m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

                for(int i = 0; i < m_spriteArray.Length; i++)
                    m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
                #endregion

                m_transform.GetComponent<UIEventListener>().onPress = OnPress;

                SetActive(false);
            }

            public void UpdateInfo(EventAchieveInfo.RewardItemInfo info)
            {
                m_info = info;
                
                #region Label
                m_labelArray[(int)LABEL_TYPE.TYPE_TITLE].text = WorldManager.instance.GetItemName(info.index);
                m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = info.count.ToString();
                #endregion

                #region Info
                ReleaseIconSprite();

                if(info.CheckGoodsType)
                {
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)info.index));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].MakePixelPerfect();
                }

                else if (info.CheckDogType)
                {
                    DogInfo dogInfo = WorldManager.instance.m_dataManager.m_dogData.GetDogData(info.index);

                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName(dogInfo.basicSkin));
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                    m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                }

                else
                {
                    ITEM_TYPE parseType = Util.ParseItemMainType(info.index);
                    if (parseType.Equals(ITEM_TYPE.DOGTICKET))
                    {
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                        m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                    }
                    else if (Util.CheckAtlasByItemType(parseType))
                    {
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].MakePixelPerfect();
                    }

                    else
                    {
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].MakePixelPerfect();
                    }
                }
                #endregion

                SetActive(true);
            }

            private void OnPress(GameObject obj, bool isPress)
            {
                UIItemTooltip tooltip = EventManager.instance.GetEventWindow.GetItemTooptip;

                tooltip.OnOffTooltip(isPress);
                if(isPress)
                {
                    SoundManager.instance.PlayAudioClip("UI_Click");
                    tooltip.UpdateTooltip(m_info.index, obj.transform);
                }
            }

            #region Util

            private void ReleaseIconSprite()
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(false);
                m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(false);
            }

            #endregion

            public void SetActive(bool b)
            {
                m_obj.SetActive(b);
            }
        }
        #endregion
        private UIRewardInfo[] m_uiRewardGroup = null;
        private GameObject m_button = null;

        private EventAchieveInfo m_achieveInfo = null;

        public UIEventSingleAchieve(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "Complete_Value", "ReceiveButton/LabelText", "Term_Value" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Sprite
            string[] spritePathArray = { "ReceiveButton/Background" };
            m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

            for(int i = 0; i < m_spriteArray.Length; i++)
                m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
            #endregion

            #region UITexture
            string[] texturePathArray = { "Background", "Title", "Desc" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region InfoGroup
            m_uiRewardGroup = new UIRewardInfo[MAX_INFO_COUNT];
            for (int i = 0; i < m_uiRewardGroup.Length; i++)
                m_uiRewardGroup[i] = new UIRewardInfo(m_transform.FindChild(string.Format("Reward_Group/Reward{0:D2}", i + 1)).gameObject);
            #endregion

            #region Object
            m_button = m_transform.FindChild("ReceiveButton").gameObject;
            m_button.GetComponent<UIEventListener>().onClick = OnReceiveButton;
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            ReleaseInfoGroup();

            SEventInfo eventInfo = EventManager.instance.GetEventInfo((int)EVENT_TYPE.EVENT_SINGLE_ACHIEVE);
            List<EventAchieveInfo> infoList = null;

            if (EventManager.instance.GetEventTable.GetEventAchieveInfoList(EVENT_TYPE.EVENT_SINGLE_ACHIEVE, out infoList))
            {
                m_achieveInfo = infoList[0];

                SAchieveInfo serverInfo = AchievementManager.instance.GetServerAchieveInfo(m_achieveInfo.achieveIndex);
                AchievementInfo achieveInfo = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(m_achieveInfo.achieveIndex);

                int count = 0;
                for (int i = 0; i < m_achieveInfo.rewardInfoArray.Length; i++)
                {
                    if (m_achieveInfo.rewardInfoArray[i].index.Equals(0) == false)
                    {
                        m_uiRewardGroup[count].UpdateInfo(m_achieveInfo.rewardInfoArray[i]);
                        count = Mathf.Clamp(count + 1, 0, m_uiRewardGroup.Length - 1);
                    }
                }

                int curValue = 0;
                if (serverInfo != null) curValue = serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE) ? achieveInfo.value : serverInfo.achVal;
                else curValue = AchievementManager.instance.CheckAchieveRewardComplete(achieveInfo) ? achieveInfo.value : 0;

                m_labelArray[(int)LABEL_TYPE.TYPE_COMPLETE_VALUE].text = string.Format("({0}/{1})", curValue, achieveInfo.value);

                SetButtonActive(serverInfo != null ? serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE) : false, 
                                serverInfo != null ? serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR) : false);
            }

            m_labelArray[(int)LABEL_TYPE.TYPE_TERMS].text = string.Format("{0} : {1} ~ {2}", Str.instance.Get(482), Util.GetTimeStringYear(eventInfo.sTime, false), Util.GetTimeStringYear(eventInfo.eTime, false));

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        public override void Response(params object[] param)
        {
            UpdateWindow();
            StartViewEventReward(m_achieveInfo);
        }

        private void StartViewEventReward(EventAchieveInfo info)
        {
            uint[] itemIndex = new uint[info.rewardInfoArray.Length];
            int[] itemCount = new int[info.rewardInfoArray.Length];

            for (int i = 0; i < info.rewardInfoArray.Length; i++)
            {
                itemIndex[i] = info.rewardInfoArray[i].index;
                itemCount[i] = info.rewardInfoArray[i].count;
            }

            MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), itemIndex, itemCount);
        }

        #region Callback

        private void OnReceiveButton(GameObject obj)
        {
            Util.ButtonAnimation(obj);
            EventManager.instance.SendEventAchieveFinish(m_achieveInfo.achieveIndex);
        }

        #endregion

        private void ReleaseInfoGroup()
        {
            for (int i = 0; i < m_uiRewardGroup.Length; i++)
                m_uiRewardGroup[i].SetActive(false);
        }

        private void SetButtonActive(bool isComplete, bool isActive)
        {
            if (isComplete)
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_BUTTON_BACKGROUND].spriteName = "Btn_InactiveOrange";

                m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].text = Str.instance.Get(408);
                m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].color = new Vector4(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f);
                m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].effectColor = new Vector4(154.0f / 255.0f, 117.0f / 255.0f, 64.0f / 255.0f, 255.0f / 255.0f);

                m_button.GetComponent<Collider>().enabled = false;
            }

            else
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_BUTTON_BACKGROUND].spriteName = isActive ? "Btn_Orange" : "Btn_Gray";

                m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].text = Str.instance.Get(133);
                m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].color = isActive ? new Vector4(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f) : new Vector4(206.0f / 255.0f, 206.0f / 255.0f, 206.0f / 255.0f, 255.0f / 255.0f);
                m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].effectColor = isActive ? new Vector4(129.0f / 255.0f, 75.0f / 255.0f, 36.0f / 255.0f, 255.0f / 255.0f) : new Vector4(88.0f / 255.0f, 77.0f / 255.0f, 77.0f / 255.0f, 255.0f / 255.0f);

                m_button.GetComponent<Collider>().enabled = isActive;
            }
        }
    }

    #endregion

    #region UIEventMultiAchieve

    private class UIEventMultiAchieve : UIEventWindowGroup
    {
        private const int MAX_ITEM_COUNT = 3;

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,
            TYPE_TITLE,

            TYPE_END,
        }

        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_TERM_VALUE,

            TYPE_END,
        }

        private UIPanel m_scrollPanel = null;
        private UIScrollView m_scrollView = null;
        private UIGrid m_scrollGrid = null;

        #region RewardGroup
        private class UIScrollItem
        {
            private const int MAX_REWARD_COUNT = 2;

            private GameObject m_obj = null;
            private Transform m_transform = null;

            private UILabel[] m_labelArray = null;
            private enum LABEL_TYPE
            {
                TYPE_TITLE,
                TYPE_COMPLETE_VALUE,
                TYPE_BUTTON_TEXT,

                TYPE_END,
            }

            private UISprite[] m_spriteArray = null;
            private enum SPRITE_TYPE
            {
                TYPE_NONE = -1,

                TYPE_TITLE_ITEM_ICON,
                TYPE_TITLE_MATERIAL_ICON,
                TYPE_TITLE_QUEST_ICON,

                TYPE_REWARD_BUTTON,
                TYPE_COMPLETE,

                TYPE_END,
            }

            #region UIRewardInfo
            private class UIRewardInfo
            {
                private GameObject m_obj = null;
                private Transform m_transform = null;

                private UILabel[] m_labelArray = null;
                private enum LABEL_TYPE
                {
                    TYPE_VALUE,

                    TYPE_END,
                }

                private UISprite[] m_spriteArray = null;
                private enum SPRITE_TYPE
                {
                    TYPE_GEMS_ICON,
                    TYPE_ITEM_ICON,
                    TYPE_MATERIAL_ICON,
                    TYPE_DOG_ICON,

                    TYPE_END,
                }

                private EventAchieveInfo.RewardItemInfo m_info = null;

                public UIRewardInfo() { }
                public UIRewardInfo(GameObject obj)
                {
                    m_obj = obj;
                    m_transform = obj.transform;

                    #region Label
                    string[] labelPathArray = { "Value_Label" };
                    m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

                    for (int i = 0; i < m_labelArray.Length; i++)
                        m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
                    #endregion

                    #region Sprite
                    string[] spritePathArray = { "Goods_Icon", "Item_Icon_Group/Icon", "Material_Icon_Group/Icon", "Dog_Icon_Group/Icon" };
                    m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

                    for (int i = 0; i < m_spriteArray.Length; i++)
                        m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
                    #endregion

                    m_transform.GetComponent<UIEventListener>().onPress = OnPress;

                    SetActive(false);
                }

                public void UpdateInfo(EventAchieveInfo.RewardItemInfo info)
                {
                    m_info = info;

                    #region Label
                    m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = info.count.ToString();
                    #endregion

                    #region Info
                    ReleaseIconSprite();

                    if (info.CheckGoodsType)
                    {
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].spriteName = Util.GetGoodsIconName(Util.GetGoodsTypeByIndex((int)info.index));
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].MakePixelPerfect();
                    }

                    else if (info.CheckDogType)
                    {
                        DogInfo dogInfo = WorldManager.instance.m_dataManager.m_dogData.GetDogData(info.index);

                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName(dogInfo.basicSkin));
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                        m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                    }

                    else
                    {
                        ITEM_TYPE parseType = Util.ParseItemMainType(info.index);
                        if (parseType.Equals(ITEM_TYPE.DOGTICKET))
                        {
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].MakePixelPerfect();

                            m_labelArray[(int)LABEL_TYPE.TYPE_VALUE].text = "";
                        }
                        else if (Util.CheckAtlasByItemType(parseType))
                        {
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].MakePixelPerfect();
                        }

                        else
                        {
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(true);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].spriteName = WorldManager.instance.GetGUISpriteName(info.index);
                            m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].MakePixelPerfect();
                        }
                    }
                    #endregion

                    SetActive(true);
                }

                private void OnPress(GameObject obj, bool isPress)
                {
                    UIItemTooltip tooltip = EventManager.instance.GetEventWindow.GetItemTooptip;

                    tooltip.OnOffTooltip(isPress);
                    if (isPress)
                    {
                        SoundManager.instance.PlayAudioClip("UI_Click");
                        tooltip.UpdateTooltip(m_info.index, obj.transform);
                    }
                }

                #region Util

                private void ReleaseIconSprite()
                {
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_GEMS_ICON].gameObject.SetActive(false);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_ITEM_ICON].gameObject.SetActive(false);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_MATERIAL_ICON].gameObject.SetActive(false);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DOG_ICON].gameObject.SetActive(false);
                }

                #endregion

                public void SetActive(bool b)
                {
                    m_obj.SetActive(b);
                }
            }
            #endregion
            private UIRewardInfo[] m_uiRewardGroup = null;

            private GameObject m_button = null;

            private EventAchieveInfo m_info = null;
            public EventAchieveInfo GetEventAchieveInfo { get { return m_info; } }

            public UIScrollItem() { }
            public UIScrollItem(GameObject obj)
            {
                m_obj = obj;
                m_transform = obj.transform;

                #region Label
                string[] labelPathArray = { "Title", "CompleteValue", "RewardButton/Text" };
                m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

                for(int i = 0; i < m_labelArray.Length; i++)
                    m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
                #endregion

                #region Sprite
                string[] spritePathArray = { "Icon_Group/Item_Icon_Group/Icon", "Icon_Group/Material_Icon_Group/Icon", "Icon_Group/Quest_Icon", "RewardButton/Background", "Complete" };
                m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

                for (int i = 0; i < m_spriteArray.Length; i++)
                    m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
                #endregion

                #region InfoGroup
                m_uiRewardGroup = new UIRewardInfo[MAX_REWARD_COUNT];
                for (int i = 0; i < m_uiRewardGroup.Length; i++)
                    m_uiRewardGroup[i] = new UIRewardInfo(m_transform.FindChild(string.Format("Reward{0:D2}", i + 1)).gameObject);
                #endregion

                #region Object
                m_button = m_transform.FindChild("RewardButton").gameObject;
                m_button.GetComponent<UIEventListener>().onClick = OnClick;
                #endregion

                SetActive(false);
            }

            public void UpdateItem(EventAchieveInfo info)
            {
                m_info = info;

                bool isExists = info != null;
                if(isExists)
                    SetItem(info);

                SetActive(isExists);
            }

            public void Refresh()
            {
                if (m_info != null)
                    SetItem(m_info);
            }

            #region Callback

            private void OnClick(GameObject obj)
            {
                Util.ButtonAnimation(obj);
                EventManager.instance.SendEventAchieveFinish(m_info.achieveIndex);
            }

            #endregion

            #region Util

            private void SetItem(EventAchieveInfo info)
            {
                AchievementInfo achieveInfo = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(info.achieveIndex);
                SAchieveInfo serverInfo = AchievementManager.instance.GetServerAchieveInfo(info.achieveIndex);

                UpdateTitle(achieveInfo, serverInfo);
                UpdateRewardGroup(info);

                bool isComplete = false;
                bool isActive = false;
                int curValue = 0;

                if (serverInfo != null)
                {
                    isComplete = serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE);
                    isActive = serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR);
                    curValue = isComplete ? achieveInfo.value : serverInfo.achVal;
                }

                else
                {
                    isComplete = AchievementManager.instance.CheckAchieveRewardComplete(achieveInfo);
                    isActive = false;
                    curValue = isComplete ? achieveInfo.value : 0;
                }

                m_labelArray[(int)LABEL_TYPE.TYPE_COMPLETE_VALUE].text = string.Format("({0}/{1})", curValue, achieveInfo.value);
                SetButtonActive(isComplete, isActive);
            }

            private void UpdateTitle(AchievementInfo info, SAchieveInfo sInfo)
            {
                m_labelArray[(int)LABEL_TYPE.TYPE_TITLE].text = info.GetTitle;

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

            private void UpdateRewardGroup(EventAchieveInfo info)
            {
                int count = 0;
                for (int i = 0; i < info.rewardInfoArray.Length; i++)
                {
                    if (info.rewardInfoArray[i].index.Equals(0) == false)
                    {
                        m_uiRewardGroup[count].UpdateInfo(info.rewardInfoArray[i]);
                        count = Mathf.Clamp(count + 1, 0, m_uiRewardGroup.Length - 1);
                    }
                }
            }

            private void SetButtonActive(bool isComplete, bool isActive = true)
            {
                m_spriteArray[(int)SPRITE_TYPE.TYPE_COMPLETE].gameObject.SetActive(isComplete);
                m_button.SetActive(!isComplete);

                if(isComplete == false)
                {
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_REWARD_BUTTON].spriteName = isActive ? "Btn_Green" : "Btn_Gray";

                    m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].color = isActive ? new Vector4(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f) : new Vector4(206.0f / 255.0f, 206.0f / 255.0f, 206.0f / 255.0f, 255.0f / 255.0f);
                    m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].effectColor = isActive ? new Vector4(91.0f / 255.0f, 97.0f / 255.0f, 65.0f / 255.0f, 255.0f / 255.0f) : new Vector4(88.0f / 255.0f, 77.0f / 255.0f, 77.0f / 255.0f, 255.0f / 255.0f);

                    m_button.GetComponent<Collider>().enabled = isActive;
                }
            }

            private void ReleaseRewardGroup()
            {
                for (int i = 0; i < m_uiRewardGroup.Length; i++)
                    m_uiRewardGroup[i].SetActive(false);
            }
            
            #endregion

            public void SetActive(bool b)
            {
                m_obj.SetActive(b);
            }
        }
        #endregion
        private GameObject m_itemObj = null;
        private List<UIScrollItem> m_itemList = null;

        public UIEventMultiAchieve(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region UITexture
            string[] texturePathArray = { "Background", "Title" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Label
            string[] labelPathArray = { "Term_Value" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for (int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Scroll
            m_itemObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/EventWindow_AchieveItem");
            m_itemList = new List<UIScrollItem>();

            m_scrollPanel = m_transform.FindChild("ScrollWindow/ScrollView").GetComponent<UIPanel>();
            m_scrollView = m_scrollPanel.GetComponent<UIScrollView>();
            m_scrollGrid = m_scrollPanel.transform.FindChild("Grid").GetComponent<UIGrid>();

            InitItem();
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            UpdateList();

            SEventInfo eventInfo = EventManager.instance.GetEventInfo((int)EVENT_TYPE.EVENT_MULTY_ACHIEVE);
            m_labelArray[(int)LABEL_TYPE.TYPE_TERM_VALUE].text = string.Format("{0} : {1} ~ {2}", Str.instance.Get(482), Util.GetTimeStringYear(eventInfo.sTime, false), Util.GetTimeStringYear(eventInfo.eTime, false));

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        public override void Response(params object[] param)
        {
            int newIndex = (int)param[0];
            for (int i = 0; i < m_itemList.Count; i++)
            {
                if (m_itemList[i].GetEventAchieveInfo != null && m_itemList[i].GetEventAchieveInfo.achieveIndex.Equals(newIndex))
                    StartViewEventReward(m_itemList[i].GetEventAchieveInfo);

                m_itemList[i].Refresh();
            }
        }

        private void StartViewEventReward(EventAchieveInfo info)
        {
            uint[] itemIndex = new uint[info.rewardInfoArray.Length];
            int[] itemCount = new int[info.rewardInfoArray.Length];

            for (int i = 0; i < info.rewardInfoArray.Length; i++)
            {
                itemIndex[i] = info.rewardInfoArray[i].index;
                itemCount[i] = info.rewardInfoArray[i].count;
            }

            MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), itemIndex, itemCount);
        }

        #region Scroll

        #region Item

        private void InitItem()
        {
            for(int i = 0; i < MAX_ITEM_COUNT; i++)
                MakeItem();
        }

        private UIScrollItem MakeItem()
        {
            GameObject obj = MonoBehaviour.Instantiate(m_itemObj) as GameObject;
            obj.transform.parent = m_scrollGrid.transform;
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = Vector3.zero;

            UIScrollItem item = new UIScrollItem(obj);
            m_itemList.Add(item);

            return item;
        }

        private UIScrollItem GetItem(int index)
        {
            return m_itemList.Count > index ? m_itemList[index] : MakeItem();
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

            List<EventAchieveInfo> infoList = null;
            if(EventManager.instance.GetEventTable.GetEventAchieveInfoList(EVENT_TYPE.EVENT_MULTY_ACHIEVE, out infoList))
            {
                for (int i = 0; i < infoList.Count; i++)
                {
                    UIScrollItem item = GetItem(i);
                    if (item != null)
                        item.UpdateItem(infoList[i]);
                }
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
    }

    #endregion

    #region UIEventCollection

    private class UIEventCollection : UIEventWindowGroup
    {
        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_DESC,
            TYPE_COUNT_TITLE,
            TYPE_COUNT0,
            TYPE_COUNT10,
            TYPE_COUNT100,
            TYPE_USE_COUNT,
            TYPE_TERM_VALUE,
            TYPE_REWARD_DESC,

            TYPE_END,
        }

        private UISprite m_useIcon = null;

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,
            TYPE_TITLE,

            TYPE_END,
        }

        private List<UIEventWindowScrollItem> m_itemList = null;

        private Transform m_itemGroup = null;

        private UICollectBonusPanel m_collectBonusPanel = null;

        public UIEventCollection(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "Desc", "Collection_Group/Title", "Collection_Group/Value0", "Collection_Group/Value10", "Collection_Group/Value100", 
                                        "ReceiveButton/LabelPrice", "Term_Value", "Reward_Group/Desc" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Sprite
            m_useIcon = m_transform.FindChild("ReceiveButton/SpriteGem").GetComponent<UISprite>();
            #endregion

            #region Texture
            string[] texturePathArray = { "Background", "Title" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Object
            m_itemGroup = m_transform.FindChild("Reward_Group");
            m_itemList = new List<UIEventWindowScrollItem>();

            GameObject itemObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/EventWindow_CollectionItem");
            for(int i = 0; i < EventCollectMasterInfo.MAX_REWARD_COUNT; i++)
            {
                int width = i % 4;
                int height = i / 4;

                GameObject obj = MonoBehaviour.Instantiate(itemObj) as GameObject;
                obj.transform.name = string.Format("Item_{0:D2}", i);
                obj.transform.parent = m_itemGroup;
                obj.transform.localScale = Vector3.one;
                obj.transform.localPosition = new Vector3(width * 104.0f, height * -99.0f, 0);

                UIEventWindowScrollItem item = new UIEventWindowScrollItem(EVENT_TYPE.EVENT_COLLECT, obj);
                m_itemList.Add(item);
            }

            m_transform.FindChild("ReceiveButton").GetComponent<UIEventListener>().onClick = OnReceiveButton;
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            UpdateEventInfo();
            UpdateRewardInfo();
        }

        public override void Response(params object[] param)
        {
            UpdateEventInfo();

            int index = (int)param[0];
            if (index >= 0)
            {
                for (int i = 0; i < m_itemList.Count; i++)
                {
                    if (i.Equals(index))
                    {
                        OpenCollectBonusPanel(index);
                        break;
                    }
                }
            }

            else
            {
                uint rewardCode = (uint)param[1];
                int rewardCount = (int)param[2];
                MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), rewardCode, rewardCount);
            }
        }

        private void UpdateRewardInfo()
        {
            EventCollectMasterInfo info = EventManager.instance.GetCollectEventInfo.GetMasterTable;
            for (int i = 0; i < info.rewardInfoArray.Length; i++)
                m_itemList[i].Init(info.rewardInfoArray[i], EventManager.instance.CheckCollectEventComplete(i));

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        private void UpdateEventInfo()
        {
            CollectEventInfo info = EventManager.instance.GetCollectEventInfo;
            int itemIndex = (int)EventManager.instance.GetCollectEventInfo.GetMasterTable.eventItemIndex;

            int curValue = EventManager.instance.GetCollectEventInfo.m_rewardBasicCount;
            int maxValue = EventManager.instance.GetCollectEventInfo.GetMasterTable.basicRewardCount;

            int v100 = Mathf.RoundToInt(info.GetValue / 100);
            int v10 = Mathf.RoundToInt((info.GetValue - (v100 * 100)) / 10);
            int v1 = Mathf.RoundToInt(info.GetValue % 10);

            m_labelArray[(int)LABEL_TYPE.TYPE_DESC].text = Str.instance.Get(565, "%NAME%", Str.instance.Get(itemIndex));
            m_labelArray[(int)LABEL_TYPE.TYPE_COUNT_TITLE].text = Str.instance.Get(566, "%NAME%", Str.instance.Get(itemIndex));

            m_labelArray[(int)LABEL_TYPE.TYPE_COUNT0].text = string.Format("{0}", Mathf.Clamp(v1, 0, 9));
            m_labelArray[(int)LABEL_TYPE.TYPE_COUNT10].text = string.Format("{0}", Mathf.Clamp(v10, 0, 9));
            m_labelArray[(int)LABEL_TYPE.TYPE_COUNT100].text = string.Format("{0}", Mathf.Clamp(v100, 0, 9));

            m_labelArray[(int)LABEL_TYPE.TYPE_TERM_VALUE].text = string.Format("{0} : {1} ~ {2}", Str.instance.Get(482), Util.GetTimeStringYear(info.m_startTime, false), Util.GetTimeStringYear(info.m_endTime, false));
            m_labelArray[(int)LABEL_TYPE.TYPE_USE_COUNT].text = string.Format("{0}", EventManager.instance.GetCollectEventInfo.GetMasterTable.useCount);

            m_labelArray[(int)LABEL_TYPE.TYPE_REWARD_DESC].text = Str.instance.Get(567, new string[] { "%NUMBER%", "%MAX%" }, new string[] { curValue.ToString(), maxValue.ToString() });

            m_useIcon.spriteName = EventManager.instance.GetCollectEventInfo.GetMasterTable.eventItemIcon;
            m_useIcon.MakePixelPerfect();
        }

        #region Callback

        private void OnReceiveButton(GameObject obj)
        {
            Util.ButtonAnimation(obj);

            if (EventManager.instance.CheckCollectGoodsEnable())
                EventManager.instance.SendEventCollectReceive();
        }

        private void OnBonusPanelCloseCallback(params object[] param)
        {
            if (param != null && param.Length.Equals(0) == false)
            {
                int index = (int)param[0];
                m_itemList[index].SetCheckObject(true, false);
            }
        }

        #endregion

        private void OpenCollectBonusPanel(int selectIndex)
        {
            if (m_collectBonusPanel == null)
            {
                GameObject panel = MonoBehaviour.Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UICollectBonusPanel")) as GameObject;
                panel.transform.parent = m_transform;
                panel.transform.localPosition = Vector3.zero;
                panel.transform.localScale = Vector3.one;

                m_collectBonusPanel = panel.GetComponent<UICollectBonusPanel>();
            }

            m_collectBonusPanel.OpenPopup(selectIndex, OnBonusPanelCloseCallback, selectIndex);
        }
    }

    #endregion

    #region UIEventCoupon

    private class UIEventCoupon : UIEventWindowGroup
    {
        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_BUTTON_TEXT,

            TYPE_END,
        }

        private UISprite[] m_spriteArray = null;
        private enum SPRITE_TYPE
        {
            TYPE_BUTTON_BACKGROUND,

            TYPE_END,
        }

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,
            TYPE_TITLE,

            TYPE_END,
        }

        private UIInput m_input = null;

        public UIEventCoupon(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "OkButton/Text" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Sprite
            string[] spritePathArray = { "OkButton/Background" };
            m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

            for(int i = 0; i < m_spriteArray.Length; i++)
                m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
            #endregion

            #region UITexture
            string[] texturePathArray = { "Background", "Title" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Object
            m_transform.FindChild("OkButton").GetComponent<UIEventListener>().onClick = OnOkButton;

            m_input = m_transform.FindChild("InputNumber").GetComponent<UIInput>();
            EventDelegate.Add(m_input.onChange, OnChangeInput);
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            ReleaseInput();

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        #region Callback

        private void OnOkButton(GameObject obj)
        {
            if (CheckStringLength())
            {
                Util.ButtonAnimation(obj);

                EventManager.instance.SendEventReqCoupon(m_input.value);
                ReleaseInput();
            }
        }

        private void OnChangeInput()
        {
            ButtonSetActive(CheckStringLength());
        }

        #endregion

        #region Util

        private void ReleaseInput()
        {
            m_input.value = "";
            ButtonSetActive(false);
        }

        private bool CheckStringLength()
        {
            string trimStr = m_input.value.Trim();
            if (m_input.value.Length != trimStr.Length)
                return false;

            return !m_input.value.Length.Equals(0);
        }

        private void ButtonSetActive(bool b)
        {
            m_spriteArray[(int)SPRITE_TYPE.TYPE_BUTTON_BACKGROUND].spriteName = b ? "Btn_Green" : "Btn_Gray";
            m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].color = b ? new Vector4(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f) :
                                                                       new Vector4(166.0f / 255.0f, 162.0f / 255.0f, 162.0f / 255.0f, 255.0f / 255.0f);
            m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].effectColor = b ? new Vector4(91.0f / 255.0f, 97.0f / 255.0f, 65.0f / 255.0f, 255.0f / 255.0f) :
                                                                             new Vector4(81.0f / 255.0f, 81.0f / 255.0f, 81.0f / 255.0f, 255.0f / 255.0f);
        }

        #endregion
    }

    #endregion

    #region UIEventBuyCash

    private class UIEventBuyCash : UIEventWindowGroup
    {
        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,
            TYPE_TITLE,
            TYPE_DESC,

            TYPE_END,
        }

        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_TERM_VALUE,

            TYPE_END,
        }

        public UIEventBuyCash(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region UITexture
            string[] texturePathArray = { "Background", "Title", "Desc" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Label
            string[] labelPathArray = { "Term_Value" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for (int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            m_textureArray[(int)TEXTURE_TYPE.TYPE_BACGROUND].GetComponent<UIEventListener>().onClick = OnLinkButton;
        }

        public override void UpdateWindow(params object[] param)
        {
            m_masterInfo.clientTimeText.SetTimeText(m_labelArray[(int)LABEL_TYPE.TYPE_TERM_VALUE]);

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        #region Callback

        private void OnLinkButton(GameObject obj)
        {
            EventManager.instance.GetEventWindow.CloseWindow();
            ShopWindow.instance.OpenShopWindow(SHOP_TAB_TYPE.TAB_GEMS, PRODUCT_TYPE.PRODUCT_CASH, new ShopWindow.ShopCloseCB(OnShopWindowCloseCB));
        }

        private void OnShopWindowCloseCB(bool isEnoughGems)
        {
            EventManager.instance.InitEventInfo(EVENT_TYPE.EVENT_BUY_CASH);
        }

        #endregion
    }

    #endregion

    #region UIEventReturn

    private class UIEventReturn : UIEventWindowGroup
    {
        private const int MAX_PACKAGE_ITEM_COUNT = 3;
        private const int MAX_DAILY_ITEM_COUNT = 6;

        private readonly Vector3[] PACKAGE_ICON_POS = { new Vector3(0, 0, 0), new Vector3(99, 0, 0), new Vector3(198, 0, 0) };
        private readonly Vector3[] DAILY_ICON_POS = { new Vector3(0, 0, 0), new Vector3(103, 0, 0), new Vector3(206, 0, 0),
                                                      new Vector3(0, -114, 0), new Vector3(103, -114, 0), new Vector3(206, -114, 0) };

        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_TITLE,
            TYPE_PACKAGE_REWARD,

            TYPE_END,
        }

        private UISprite[] m_spriteArray = null;
        private enum SPRITE_TYPE
        {
            TYPE_PACKAGE_REWARD,

            TYPE_END,
        }

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_TITLE,

            TYPE_END,
        }

        private GameObject[] m_EffectArray = null;
        private enum EFFECT_TYPE
        {
            TYPE_CURRENT,
            TYPE_NEXT,

            TYPE_END,
        }

        private GameObject m_packageRewardButton = null;

        private List<UIEventWindowScrollItem> m_packageItemList = null;
        private List<UIEventWindowScrollItem> m_dailyItemList = null;

        public UIEventReturn(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "Title", "Package_Reward_Group/RewardButton/Text" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Sprite
            string[] spritePathArray = { "Package_Reward_Group/RewardButton/Background" };
            m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

            for(int i = 0; i < m_spriteArray.Length; i++)
                m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
            #endregion

            #region UITexture
            string[] texturePathArray = { "Background" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Object
            GameObject createGroupObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/EventWindow_ReturnItem");
            GameObject createObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/EventWindow_ScrollItem");

            Transform packageParent = m_transform.FindChild("Package_Reward_Group");
            Transform dailyParent = m_transform.FindChild("Daily_Reward_Group");

            m_packageItemList = new List<UIEventWindowScrollItem>();
            for(int i = 0; i < MAX_PACKAGE_ITEM_COUNT; i++)
            {
                GameObject obj = Instantiate(createGroupObj) as GameObject;
                obj.transform.parent = packageParent;
                obj.transform.localPosition = PACKAGE_ICON_POS[i];
                obj.transform.localScale = Vector3.one;

                m_packageItemList.Add(new UIEventWindowScrollItem(EVENT_TYPE.EVENT_COMEBACK, obj));
            }

            m_dailyItemList = new List<UIEventWindowScrollItem>();
            for(int i = 0; i < MAX_DAILY_ITEM_COUNT; i++)
            {
                GameObject obj = Instantiate(createObj) as GameObject;
                obj.transform.parent = dailyParent;
                obj.transform.localPosition = DAILY_ICON_POS[i];
                obj.transform.localScale = Vector3.one;

                m_dailyItemList.Add(new UIEventWindowScrollItem(EVENT_TYPE.EVENT_COMEBACK, obj));
            }
            #endregion

            #region Effect
            m_EffectArray = new GameObject[(int)EFFECT_TYPE.TYPE_END];

            m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT] = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_Reward01")) as GameObject;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.parent = dailyParent;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.localScale = Vector3.one;
            SetCurrentEffect(false, null);

            m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT] = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_Reward00")) as GameObject;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.parent = dailyParent;
            m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.localScale = Vector3.one;
            SetNextEffect(false, null);
            #endregion

            m_packageRewardButton = m_transform.FindChild("Package_Reward_Group/RewardButton").gameObject;
            m_packageRewardButton.GetComponent<UIEventListener>().onClick = OnPackageRewardButton;
        }

        public override void UpdateWindow(params object[] param)
        {
            SetCurrentEffect(false, null);
            SetNextEffect(false, null);

            ReturnEventInfo eventInfo = EventManager.instance.GetReturnEventInfo;
            if (eventInfo != null)
            {
                #region Label
                m_labelArray[(int)LABEL_TYPE.TYPE_TITLE].text = Str.instance.Get(575, "%LEVEL%", eventInfo.GetRewardGroupLevel.ToString());
                #endregion

                #region Package
                for (int i = 0; i < eventInfo.m_packageRewardList.Count; i++)
                {
                    if (m_packageItemList.Count > i)
                        m_packageItemList[i].Init(i + 1, eventInfo.m_packageRewardList[i], eventInfo.CheckRewardComplete(eventInfo.m_packageRewardList[i].index));
                }
                #endregion

                #region Daily
                for (int i = 0; i < eventInfo.m_dailyRewardList.Count; i++)
                {
                    if (m_dailyItemList.Count > i)
                    {
                        m_dailyItemList[i].Init(i + 1, eventInfo.m_dailyRewardList[i], eventInfo.CheckRewardComplete(eventInfo.m_dailyRewardList[i].index));
                        if (eventInfo.GetRewardEnableDay.Equals(i))
                        {
                            if (eventInfo.CheckRewardEnable()) SetCurrentEffect(true, m_dailyItemList[i].Transform);
                            else SetNextEffect(true, m_dailyItemList[i].Transform);
                        }
                    }
                }
                #endregion

                SetButtonActive(!eventInfo.CheckPackageRewardComplete);
            }

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        public override void Response(params object[] param)
        {
            ReturnEventInfo eventInfo = EventManager.instance.GetReturnEventInfo;
            List<int> newIndexList = (List<int>)param[0];

            if (newIndexList.Count.Equals(1))
            {
                for (int i = 0; i < m_dailyItemList.Count; i++)
                {
                    if (m_dailyItemList[i].GetReturnInfo.index.Equals(newIndexList[0]))
                    {
                        m_dailyItemList[i].SetCheckObject(true, false);

                        SetCurrentEffect(false, null);
                        if (m_dailyItemList.Count > i + 1)
                            SetNextEffect(true, m_dailyItemList[i + 1].Transform);

                        MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), m_dailyItemList[i].GetReturnInfo.rewardIndex, m_dailyItemList[i].GetReturnInfo.rewardValue);
                        break;
                    }
                }
            }

            else
            {
                for (int i = 0; i < m_packageItemList.Count; i++)
                    m_packageItemList[i].SetCheckObject(true, false);

                SetButtonActive(false);
                StartViewEventReward(eventInfo.m_packageRewardList);
            }
        }

        private void StartViewEventReward(List<EventReturnInfo> infoList)
        {            
            uint[] itemIndex = new uint[infoList.Count];
            int[] itemCount = new int[infoList.Count];

            for (int i = 0; i < infoList.Count; i++)
            {
                itemIndex[i] = infoList[i].rewardIndex;
                itemCount[i] = infoList[i].rewardValue;
            }

            MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), itemIndex, itemCount);
        }

        #region Effect

        private void SetCurrentEffect(bool b, Transform target)
        {
            if(m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT] != null)
            {
                m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.parent = target;
                m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.localPosition = Vector3.zero;
                m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].transform.localScale = Vector3.one;

                m_EffectArray[(int)EFFECT_TYPE.TYPE_CURRENT].SetActive(b);
            }
        }

        private void SetNextEffect(bool b, Transform target)
        {
            if(m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT] != null)
            {
                m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.parent = target;
                m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.localPosition = Vector3.zero;
                m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].transform.localScale = Vector3.one;

                m_EffectArray[(int)EFFECT_TYPE.TYPE_NEXT].SetActive(b);
            }
        }

        #endregion

        #region Package

        private void OnPackageRewardButton(GameObject obj)
        {
            ReturnEventInfo eventInfo = EventManager.instance.GetReturnEventInfo;
            if (eventInfo != null && eventInfo.CheckPackageRewardComplete == false)
            {
                List<int> idxList = new List<int>();
                for (int i = 0; i < eventInfo.m_packageRewardList.Count; i++)
                    idxList.Add(eventInfo.m_packageRewardList[i].index);

                Util.ButtonAnimation(obj);
                EventManager.instance.SendEventReturnReceive(idxList.ToArray());
            }
        }

        private void SetButtonActive(bool isEnable)
        {
            m_spriteArray[(int)SPRITE_TYPE.TYPE_PACKAGE_REWARD].spriteName = isEnable ? "Btn_Orange" : "Btn_Gray";

            m_labelArray[(int)LABEL_TYPE.TYPE_PACKAGE_REWARD].text = Str.instance.Get(133);
            m_labelArray[(int)LABEL_TYPE.TYPE_PACKAGE_REWARD].color = isEnable ? new Vector4(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f) : new Vector4(206.0f / 255.0f, 206.0f / 255.0f, 206.0f / 255.0f, 255.0f / 255.0f);
            m_labelArray[(int)LABEL_TYPE.TYPE_PACKAGE_REWARD].effectColor = isEnable ? new Vector4(129.0f / 255.0f, 75.0f / 255.0f, 36.0f / 255.0f, 255.0f / 255.0f) : new Vector4(88.0f / 255.0f, 77.0f / 255.0f, 77.0f / 255.0f, 255.0f / 255.0f);

            m_packageRewardButton.GetComponent<Collider>().enabled = isEnable;
        }

        #endregion
    }

    #endregion

    #region UIEventPromotionCoupon

    private class UIEventPromotionCoupon : UIEventWindowGroup
    {
        private UILabel[] m_labelArray = null;
        private enum LABEL_TYPE
        {
            TYPE_COUPON_TEXT,
            TYPE_REMAIN_VALUE,
            TYPE_BUTTON_TEXT,
            TYPE_DESC_TEXT,

            TYPE_END,
        }

        private UISprite[] m_spriteArray = null;
        private enum SPRITE_TYPE
        {
            TYPE_BUTTON_BACKGROUND,

            TYPE_END,
        }

        private UITexture[] m_textureArray = null;
        private enum TEXTURE_TYPE
        {
            TYPE_BACGROUND,
            TYPE_TITLE,

            TYPE_END,
        }

        private BoxCollider m_okButtonCd = null;

        private EventAchieveInfo m_achieveInfo = null;

        public UIEventPromotionCoupon(EventMasterInfo info, GameObject obj)
        {
            m_masterInfo = info;
            
            m_obj = obj;
            m_transform = obj.transform;

            Init();
        }

        public override void Init()
        {
            base.Init();

            #region Label
            string[] labelPathArray = { "TextValue", "RemainValue", "OkButton/Text", "DescText" };
            m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];

            for(int i = 0; i < m_labelArray.Length; i++)
                m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
            #endregion

            #region Sprite
            string[] spritePathArray = { "OkButton/Background" };
            m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];

            for(int i = 0; i < m_spriteArray.Length; i++)
                m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
            #endregion

            #region UITexture
            string[] texturePathArray = { "Background", "Title" };
            m_textureArray = new UITexture[(int)TEXTURE_TYPE.TYPE_END];

            for (int i = 0; i < m_textureArray.Length; i++)
                m_textureArray[i] = m_transform.FindChild(texturePathArray[i]).GetComponent<UITexture>();
            #endregion

            #region Object
            m_okButtonCd = m_transform.FindChild("OkButton").GetComponent<BoxCollider>();

            m_transform.FindChild("OkButton").GetComponent<UIEventListener>().onClick = OnOkButton;
            m_transform.FindChild("LinkButton").GetComponent<UIEventListener>().onClick = OnWebLinkButton;
            #endregion
        }

        public override void UpdateWindow(params object[] param)
        {
            RES_EVENT_PROMOTE_COUPON promoteInfo = EventManager.instance.GetPromoteCouponInfo;

            SEventInfo eventInfo = EventManager.instance.GetEventInfo((int)m_masterInfo.type);
            List<EventAchieveInfo> infoList = null;

            if (EventManager.instance.GetEventTable.GetEventAchieveInfoList(m_masterInfo.type, out infoList))
            {
                m_achieveInfo = infoList[0];

                SAchieveInfo serverInfo = AchievementManager.instance.GetServerAchieveInfo(m_achieveInfo.achieveIndex);
                AchievementInfo achieveInfo = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(m_achieveInfo.achieveIndex);

                int curValue = 0;
                bool isButtonActive = false;

                if (serverInfo != null)
                {
                    curValue = serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE) ? achieveInfo.value : serverInfo.achVal;

                    switch (serverInfo.GetProgressType)
                    {
                        case ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR: 
                            isButtonActive = true; 

                            m_labelArray[(int)LABEL_TYPE.TYPE_COUPON_TEXT].text = string.Format("{0} {1}/{2}", Str.instance.Get(12203), curValue, achieveInfo.value);
                            m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = "";
                            break;

                        case ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE: 
                            isButtonActive = !promoteInfo.cpNo.Equals(""); 

                            m_labelArray[(int)LABEL_TYPE.TYPE_COUPON_TEXT].text = isButtonActive ? promoteInfo.cpNo : string.Format("{0} {1}/{2}", Str.instance.Get(12203), curValue, achieveInfo.value);
                            m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = isButtonActive ? "" : Str.instance.Get(12205);
                            break;

                        default:
                            isButtonActive = false;

                            m_labelArray[(int)LABEL_TYPE.TYPE_COUPON_TEXT].text = string.Format("{0} {1}/{2}", Str.instance.Get(12203), curValue, achieveInfo.value);
                            m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = "";
                            break;
                    }
                }

                else
                {
                    curValue = AchievementManager.instance.CheckAchieveRewardComplete(achieveInfo) ? achieveInfo.value : 0;

                    m_labelArray[(int)LABEL_TYPE.TYPE_COUPON_TEXT].text = string.Format("{0} {1}/{2}", Str.instance.Get(12203), curValue, achieveInfo.value);
                    m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = "";
                }

                SetButtonActive(isButtonActive, serverInfo);
            }

            m_labelArray[(int)LABEL_TYPE.TYPE_REMAIN_VALUE].text = string.Format("{0} {1}", Str.instance.Get(12201), promoteInfo.cpCnt);

            for (int i = 0; i < m_textureArray.Length; i++)
            {
                m_textureArray[i].mainTexture = AssetBundleEx.Load<Texture>(string.Format("{0}{1}", EventWindow.TEXTUER_PATH, m_masterInfo.GetBannerString(i)));
                m_textureArray[i].MakePixelPerfect();
            }
        }

        #region Callback

        private void OnOkButton(GameObject obj)
        {
            Util.ButtonAnimation(obj);

            SAchieveInfo serverInfo = AchievementManager.instance.GetServerAchieveInfo(m_achieveInfo.achieveIndex);
            if (serverInfo != null)
            {
                if (serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR)) EventManager.instance.SendEventAchieveFinish(m_achieveInfo.achieveIndex);
                else
                {
                    PluginManager.instance.CopyClipboard(EventManager.instance.GetPromoteCouponInfo.cpNo);
                    m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = Str.instance.Get(12204);
                }
            }
        }

        private void OnWebLinkButton(GameObject obj)
        {
            if (m_masterInfo.url != null && m_masterInfo.url.Equals("0") == false)
                Application.OpenURL(m_masterInfo.url);
            else if (m_masterInfo.locationIndex.Equals(0) == false)
            {
                LocationManager.instance.MoveDirection(m_masterInfo.locationIndex);
                EventManager.instance.GetEventWindow.CloseWindow();
            }
        }

        #endregion

        #region Util

        private void SetButtonActive(bool b, SAchieveInfo serverInfo)
        {
            m_okButtonCd.enabled = b;

            m_spriteArray[(int)SPRITE_TYPE.TYPE_BUTTON_BACKGROUND].spriteName = b ? "Btn_Green" : "Btn_Gray";

            bool isCopyEnable = serverInfo != null ? serverInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE) : false;
            m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].text = Str.instance.Get(isCopyEnable ? 12207 : 470);

            m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].color = b ? new Vector4(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f) :
                                                                       new Vector4(166.0f / 255.0f, 162.0f / 255.0f, 162.0f / 255.0f, 255.0f / 255.0f);
            m_labelArray[(int)LABEL_TYPE.TYPE_BUTTON_TEXT].effectColor = b ? new Vector4(91.0f / 255.0f, 97.0f / 255.0f, 65.0f / 255.0f, 255.0f / 255.0f) :
                                                                             new Vector4(81.0f / 255.0f, 81.0f / 255.0f, 81.0f / 255.0f, 255.0f / 255.0f);
        }

        #endregion
    }

    #endregion
}
