using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if USER_SERVER
using NetWork;
#endif

#region SpecialEventInfo

public class SpecialEventInfo
{
    public int m_index = 0;

    public List<int> m_attendRewardIndexList = null;
    public List<int> m_rewardCompleteIndexList = null;

    public int m_eventStartTime = 0;
    public int m_eventEndTime = 0;

    public int m_eventRewardCount = 0;

    public int m_rewardTime = 0;

    public SpecialEventInfo() { }
    public SpecialEventInfo(RES_EVENT_ATTEND_INFO info)
    {
        this.m_index = info.idx;

        this.m_attendRewardIndexList = ParseAttendIndex(info.attend_idxs);
        this.m_rewardCompleteIndexList = ParseAttendIndex(info.uAttend_idxs);

        this.m_eventStartTime = info.s_time;
        this.m_eventEndTime = info.e_time;

        this.m_eventRewardCount = info.uDay;
        this.m_rewardTime = info.uRewardTimer;
    }

    #region Update

    public void UpdateCompleteIndex(string attendIdx, int rewardTimer)
    {
        this.m_rewardCompleteIndexList = ParseAttendIndex(attendIdx);
        this.m_rewardTime = rewardTimer;

        this.m_eventRewardCount++;
    }

    #endregion

    #region Get

    public bool GetSpecialEventList(out List<EventAttendRewardInfo> infoList)
    {
        infoList = new List<EventAttendRewardInfo>();
        if(m_attendRewardIndexList != null)
        {
            for(int i = 0; i < m_attendRewardIndexList.Count; i++)
                infoList.Add(EventManager.instance.GetEventTable.GetSpecialAttendInfo(m_attendRewardIndexList[i]));
        }

        return !infoList.Count.Equals(0);
    }

    public bool CheckRewardComplete(int index)
    {
        return m_rewardCompleteIndexList != null ? m_rewardCompleteIndexList.Contains(index) : false;
    }

    public bool CheckRewardEnable()
    {
        if(m_rewardTime.Equals(0))
            return true;
        else
        {
            DateTime rewardDTime = Util.UnixTimeToLocalDateTime(m_rewardTime);
            return !rewardDTime.Day.Equals(Util.GetNowLocalDateGameTime().Day);
        }
    }

    public int GetRewardEnableDay()
    {
        return m_rewardCompleteIndexList.Count;
    }

    public bool CheckFinalRewardEnable()
    {
        return m_eventRewardCount.Equals(m_attendRewardIndexList.Count - 1);
    }

    #endregion

    #region Util

    private List<int> ParseAttendIndex(string packet)
    {
        if(packet.Equals(""))
            return new List<int>();
        else
        {
            string[] parseArray = packet.Split('|');
            List<int> valueList = new List<int>();

            for(int i = 0; i < parseArray.Length; i++)
                valueList.Add(Convert.ToInt32(parseArray[i]));

            return valueList;
        }
    }

    #endregion
}

#endregion

#region BurningTimeInfo

public class BurningTimeInfo
{
    private Dictionary<BURNING_EVENT_TYPE, List<EventBurningTimeInfo>> m_burningTable = null;
    private List<EventBurningTimeInfo> m_burningInfoList = null;

    private float m_goldAddRatio = 1.0f;
    private float m_expAddRatio = 1.0f;

    public BurningTimeInfo() { }
    public BurningTimeInfo(int[] indexArray)
    {
        m_burningTable = new Dictionary<BURNING_EVENT_TYPE, List<EventBurningTimeInfo>>();
        if(EventManager.instance.GetEventTable.GetEventBurningList(indexArray, out m_burningInfoList))
        {
            for(int i = 0; i < m_burningInfoList.Count; i++)
            {
                if(m_burningTable.ContainsKey(m_burningInfoList[i].type) == false)
                    m_burningTable.Add(m_burningInfoList[i].type, new List<EventBurningTimeInfo>());
                m_burningTable[m_burningInfoList[i].type].Add(m_burningInfoList[i]);
            }
        }
        
        else
            m_burningInfoList = new List<EventBurningTimeInfo>();

        m_goldAddRatio = 1.0f;
        m_expAddRatio = 1.0f;
    }

    public List<BURNING_EVENT_TYPE> GetBurningEventType()
    {
        return new List<BURNING_EVENT_TYPE>(m_burningTable.Keys);
    }

    public bool GetBurningInfoList(out List<EventBurningTimeInfo> list)
    {
        list = new List<EventBurningTimeInfo>(m_burningInfoList);
        return !list.Count.Equals(0);
    }

    public float GetBurningEventValue()
    {
        float addValue = 0;
        for (int i = 0; i < m_burningInfoList.Count; i++)
            addValue = Mathf.Max(addValue, m_burningInfoList[i].addValue);

        return addValue;
    }

    public bool CheckBurningTimeEnable()
    {
        for (int i = 0; i < m_burningInfoList.Count; i++)
        {
            if (m_burningInfoList[i].CheckBurningEnableTime())
                return true;
        }

        return false;
    }

    public float GetBurningAddValue(BURNING_EVENT_TYPE type)
    {
        if(m_burningTable.ContainsKey(type))
        {
            for(int i = 0; i < m_burningTable[type].Count; i++)
            {
                if(m_burningTable[type][i].CheckBurningEnableTime())
                    return m_burningTable[type][i].addValue;
            }
        }

        return 1.0f;
    }

    public void UpdateServerAddRatio(int addGold, int addExp)
    {
        m_goldAddRatio = addGold.Equals(0) ? 1.0f : addGold * 0.01f;
        m_expAddRatio = addExp.Equals(0) ? 1.0f : addExp * 0.01f;
    }

    public float GetServerBurningAddValue(BURNING_EVENT_TYPE type)
    {
        switch(type)
        {
            case BURNING_EVENT_TYPE.TYPE_GOLD:
                return m_goldAddRatio;
            case BURNING_EVENT_TYPE.TYPE_EXP:
                return m_expAddRatio;
            default:
                return 1.0f;
        }
    }
}

#endregion

#region CollectEventInfo

public class CollectEventInfo
{
    public int MAX_VALUE_COUNT = 999;

    public int m_eventIndex = 0;

    public int m_value = 0;

    public int m_startTime = 0;
    public int m_endTime = 0;

    public int m_rewardBasicCount = 0;
    
    private List<int> m_rewardCompleteIndex = null;

    private EventCollectMasterInfo m_masterTable = null;
    public EventCollectMasterInfo GetMasterTable { get { return m_masterTable; } }

    public struct CollectResult
    {
        public int originValue;
        public int addValue;

        public CollectResult(int originValue, int addValue)
        {
            this.originValue = originValue;
            this.addValue = addValue;
        }
    }
    public Queue<CollectResult> m_rewardResultQueue = null;

    public CollectEventInfo() 
    { 
        this.m_rewardCompleteIndex = new List<int>();
        this.m_rewardResultQueue = new Queue<CollectResult>();
    }

    public CollectEventInfo(int eventIndex, int startTime, int endTime)
    {
        this.m_value = 0;

        this.m_eventIndex = Mathf.Clamp(eventIndex, 1, eventIndex);
        this.m_masterTable = EventManager.instance.GetEventTable.GetEventCollectMasterInfo(this.m_eventIndex);

        this.m_startTime = startTime;
        this.m_endTime = endTime;

        this.m_rewardCompleteIndex = new List<int>();
        this.m_rewardResultQueue = new Queue<CollectResult>();
    }

    public CollectEventInfo(RES_EVENT_COLLECT_INFO info)
    {
        this.m_value = info.userNum;

        this.m_eventIndex = Mathf.Clamp(info.masterIdx, 1, info.masterIdx);
        this.m_masterTable = EventManager.instance.GetEventTable.GetEventCollectMasterInfo(this.m_eventIndex);

        this.m_startTime = info.s_time;
        this.m_endTime = info.e_time;

        this.m_rewardBasicCount = info.rewardCount;
        this.m_rewardCompleteIndex = Util.ToList<int>(info.rewardItem);

        this.m_rewardResultQueue = new Queue<CollectResult>();
    }

    #region Check

    public int GetValue { get { return Mathf.Clamp(m_value, 0, MAX_VALUE_COUNT); } }
    public bool CheckEventEnable { get { return m_startTime < Util.GetNowGameTime() && m_endTime > Util.GetNowGameTime(); } }
    public bool CheckRewardComplete(int index)
    {
        return m_rewardCompleteIndex.Contains(index);
    }

    public bool CheckGoodsEnable { get { return m_masterTable != null ? m_value >= m_masterTable.useCount : false; } }

    #endregion

    #region Update

    public void UpdateItemValue(int value)
    {
        m_value = value;
    }

    public void UpdateRewardInfo(RES_EVENT_COLLECT_RECEIVE info)
    {
        m_value = info.userNum;

        m_rewardBasicCount = info.rewardCount;
        m_rewardCompleteIndex = Util.ToList<int>(info.rewardItem);
    }

    #endregion

    #region Reward

    public void AddRewardInfo(CollectResult info)
    {
        m_rewardResultQueue.Enqueue(info);
    }

    public CollectResult GetRewardInfo()
    {
        return m_rewardResultQueue.Count.Equals(0) ? new CollectResult() : m_rewardResultQueue.Dequeue();
    }

    #endregion

    public int GetNewRewardIndex(int[] intArray)
    {
        for (int i = 0; i < intArray.Length; i++)
        {
            if (CheckRewardComplete(intArray[i]) == false)
                return intArray[i];
        }

        return -1;
    }
}

#endregion

#region PurchaseEventInfo

public class PurchaseEventInfo
{
    public int m_startTime = 0;
    public int m_endTime = 0;

    private List<int> m_rewardCompleteIndex = null;

    public PurchaseEventInfo() 
    {
        this.m_startTime = 0;
        this.m_endTime = 0;

        this.m_rewardCompleteIndex = new List<int>();
    }

    public PurchaseEventInfo(SPurchaseEventInfo info)
    {
        this.m_startTime = info.s_time;
        this.m_endTime = info.e_time;

        this.m_rewardCompleteIndex = ParseIndex(info.rewardCodes);
    }

    public void UpdateEventInfo(string packet)
    {
        m_rewardCompleteIndex = ParseIndex(packet);
    }

    public void UpdateEventInfo(List<int> indexList)
    {
        m_rewardCompleteIndex = new List<int>(indexList);
    }

    #region Check

    public bool CheckEventEnable { get { return m_startTime < Util.GetNowGameTime() && m_endTime > Util.GetNowGameTime(); } }
    public bool CheckRewardComplete(int index)
    {
        return m_rewardCompleteIndex.Contains(index);
    }

    public List<int> GetNewRewardIndex(List<int> intList)
    {
        List<int> indexList = new List<int>();
        for (int i = 0; i < intList.Count; i++)
        {
            if (CheckRewardComplete(intList[i]) == false)
                indexList.Add(intList[i]);
        }

        return indexList;
    }

    #endregion

    #region Util

    public List<int> ParseIndex(string packet)
    {
        if(packet == null || packet.Equals(""))
            return new List<int>();
        else
        {
            string[] parseArray = packet.Split('#');
            List<int> valueList = new List<int>();

            for(int i = 0; i < parseArray.Length; i++)
                valueList.Add(Convert.ToInt32(parseArray[i]));

            return valueList;
        }
    }

    #endregion
}

#endregion

#region ReturnEventInfo

public class ReturnEventInfo
{
    public const int SORT_PACKAGE_INDEX = 10;

    public int m_groupIndex = 0;

    public bool m_rewardComplete = false;
    public int m_rewardTime = 0;

    public List<int> m_rewardCompleteIdxList = null;

    public List<EventReturnInfo> m_dailyRewardList = null;
    public List<EventReturnInfo> m_packageRewardList = null;

    private bool m_packageRewardComplete = false;
    private int m_rewardEnableDay = 0;

    public ReturnEventInfo() { }
    public ReturnEventInfo(RES_EVENT_COMEBACK_INFO info)
    {
        this.m_groupIndex = info.group;

        this.m_rewardComplete = info.evtConfirm;
        this.m_rewardTime = info.rewardTime;

        this.m_rewardCompleteIdxList = Util.ToList(info.rewardIdxs);

        this.m_dailyRewardList = new List<EventReturnInfo>();
        this.m_packageRewardList = new List<EventReturnInfo>();

        this.m_packageRewardComplete = false;
        this.m_rewardEnableDay = 0;

        List<EventReturnInfo> infoList = EventManager.instance.GetEventTable.GetEventReturnInfo(info.group);
        if (infoList != null)
        {
            for (int i = 0; i < infoList.Count; i++)
            {
                if (infoList[i].sort.Equals(SORT_PACKAGE_INDEX))
                {
                    m_packageRewardList.Add(infoList[i]);

                    if (m_packageRewardComplete == false && CheckRewardComplete(infoList[i].index))
                        m_packageRewardComplete = true;
                }

                else
                {
                    m_dailyRewardList.Add(infoList[i]);

                    if (CheckRewardComplete(infoList[i].index))
                        m_rewardEnableDay++;
                }
            }

            m_packageRewardList.Sort(delegate (EventReturnInfo a, EventReturnInfo b) {
                return a.index.CompareTo(b.index);
            });
            
            m_dailyRewardList.Sort(delegate (EventReturnInfo a, EventReturnInfo b) {
                return a.index.CompareTo(b.index);
            });
        }
    }

    #region Update

    public void UpdateInfo(RES_EVENT_COMEBACK_RECEIVE info)
    {
        this.m_groupIndex = info.group;

        this.m_rewardComplete = info.evtConfirm;
        this.m_rewardTime = info.rewardTime;

        this.m_rewardCompleteIdxList = Util.ToList(info.rewardIdxs);
        
        this.m_dailyRewardList = new List<EventReturnInfo>();
        this.m_packageRewardList = new List<EventReturnInfo>();

        this.m_packageRewardComplete = false;
        this.m_rewardEnableDay = 0;

        List<EventReturnInfo> infoList = EventManager.instance.GetEventTable.GetEventReturnInfo(info.group);
        if (infoList != null)
        {
            for (int i = 0; i < infoList.Count; i++)
            {
                if (infoList[i].sort.Equals(SORT_PACKAGE_INDEX))
                {
                    m_packageRewardList.Add(infoList[i]);

                    if (m_packageRewardComplete == false && CheckRewardComplete(infoList[i].index))
                        m_packageRewardComplete = true;
                }

                else
                {
                    m_dailyRewardList.Add(infoList[i]);

                    if (CheckRewardComplete(infoList[i].index))
                        m_rewardEnableDay++;
                }
            }

            m_packageRewardList.Sort(delegate (EventReturnInfo a, EventReturnInfo b) {
                return a.index.CompareTo(b.index);
            });
            
            m_dailyRewardList.Sort(delegate (EventReturnInfo a, EventReturnInfo b) {
                return a.index.CompareTo(b.index);
            });
        }
    }

    #endregion

    #region Get

    public int GetRewardEnableDay { get { return m_rewardEnableDay; } }
    public bool CheckPackageRewardComplete { get { return m_packageRewardComplete; } }

    public int GetRewardGroupLevel { get { return (m_packageRewardList != null ? m_packageRewardList[0].groupLevel : 0); } }

    public bool CheckRewardComplete(int index)
    {
        return m_rewardCompleteIdxList != null ? m_rewardCompleteIdxList.Contains(index) : false;
    }

    public bool CheckRewardEnable()
    {
        if(m_rewardTime.Equals(0))
            return true;
        else
        {
            DateTime rewardDTime = Util.UnixTimeToLocalDateTime(m_rewardTime);
            return !rewardDTime.Day.Equals(Util.GetNowLocalDateGameTime().Day);
        }
    }

    public List<int> GetNewRewardIndexList(int[] intArray)
    {
        List<int> rewardList = new List<int>();
        for (int i = 0; i < intArray.Length; i++)
        {
            if (CheckRewardComplete(intArray[i]) == false)
                rewardList.Add(intArray[i]);
        }

        return rewardList;
    }

    #endregion
}

#endregion

/// <summary>
/// <para>name : EventManager</para>
/// <para>describe : 게임 이벤트 관련 매니저.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class EventManager : MonoSingleton<EventManager>
{
    private const string EVENTWINDOW_PATH = "[Prefabs]/[Gui]/UI_S98_Event";
    public const int TYPE_FACTOR = 100000;

    private EventWindow m_eventWindow = null;
    public EventWindow GetEventWindow { get { return m_eventWindow; } }

    private EventTableClass m_eventTable = null;
    public EventTableClass GetEventTable
    {
        get
        {
            if (m_eventTable == null)
                m_eventTable = new EventTableClass();
            return m_eventTable;
        }
    }

    public List<SEventInfo> m_sEventList = null;

    #region Init

    public override void Init()
    {
    }

    private void CreateWindow()
    {
        GameObject createObj = Instantiate(AssetBundleEx.Load<GameObject>(EVENTWINDOW_PATH)) as GameObject;
        createObj.transform.localPosition = Vector3.zero;
        createObj.transform.localScale = Vector3.one;

        m_eventWindow = createObj.GetComponent<EventWindow>();
    }

    private void OpenEventWindow(EVENT_TYPE type = EVENT_TYPE.EVENT_CHARGE_REWARD)
    {
        if (m_eventWindow == null)
            CreateWindow();
        m_eventWindow.OpenWindow(type);
    }

    private bool CheckWindowActive()
    {
        return m_eventWindow != null ? m_eventWindow.CheckWindowActive : false;
    }

    public void SetEventInfoUpdate(bool b)
    {
        SetSpecialEventUpdate = false;
        SetTimeRewardEventUpdate = false;
        SetCollectEventUpdate = false;
        SetReturnEventUpdate = false;
    }

    #endregion

    public void InitEventInfo(EVENT_TYPE type = EVENT_TYPE.EVENT_CHARGE_REWARD)
    {
        WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.EVENT_PAGE_TYPE, type);
        NetworkManager.instance.SendEventInfo();
    }

    #region Event_Info

    public void ResponseEventInfo(SEventInfo[] infoArray)
    {
        m_sEventList = Util.ToList<SEventInfo>(infoArray);

        EVENT_TYPE type = EVENT_TYPE.EVENT_CHARGE_REWARD;
        if (WorldManager.instance.CheckMemoryInfoExists(WORLD_MEMORY_INFO.EVENT_PAGE_TYPE))
        {
            type = (EVENT_TYPE)WorldManager.instance.GetMemoryInfo(WORLD_MEMORY_INFO.EVENT_PAGE_TYPE);
            type = CheckEventExists((int)type) ? type : GetExistsFirstEventType();

            WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.EVENT_PAGE_TYPE);
        }

        switch (type)
        {
            case EVENT_TYPE.EVENT_NONE: StartCoroutine(WaitForLoadSdkModuleData()); break;
            case EVENT_TYPE.EVENT_SPECIAL_REWARD: EventManager.instance.SendEventAttendInfo(); return;
            case EVENT_TYPE.EVENT_TIME_REWARD: EventManager.instance.SendEventTimeInfo(); return;
            case EVENT_TYPE.EVENT_COLLECT: EventManager.instance.SendEventCollectInfo(); return;
            case EVENT_TYPE.EVENT_COMEBACK: EventManager.instance.SendEventReturnInfo(); return;
            case EVENT_TYPE.EVENT_PROMOTION_COUPON01:
            case EVENT_TYPE.EVENT_PROMOTION_COUPON02:
            case EVENT_TYPE.EVENT_PROMOTION_COUPON03: EventManager.instance.SendEventPromoteCoupon(type); return;
            default: OpenEventWindow(type); return;
        }
    }

    public SEventInfo GetEventInfo(int type)
    {
        int index = m_sEventList.FindIndex(delegate (SEventInfo info)
        {
            return info.eventType.Equals(type);
        });

        return index < 0 ? null : m_sEventList[index];
    }

    public bool CheckEventExists(int type)
    {
        if (m_sEventList != null)
        {
            // bool isExists = m_sEventList.Exists(delegate (SEventInfo info) {
            //     return info.eventType.Equals(type);
            // });

            // if (isExists == false)
            //     isExists = m_eventTable.CheckEventMasterActiveClient((EVENT_TYPE)type);
            bool isExists = false;
            for (int i = 0; i < m_sEventList.Count; i++)
            {
                if (m_sEventList[i].eventType.Equals(type))
                {
                    EventMasterInfo eMInfo = GetEventTable.GetEventMasterInfo((EVENT_TYPE)type);
                    isExists = SdkManager.instance.CheckGameEventExists(eMInfo.index) && GetEventTable.CheckEventMasterActiveClient((EVENT_TYPE)type);
                    break;
                }
            }

            return isExists;
        }

        return false;
    }

    public bool CheckEventExists(EventMasterInfo info)
    {
        if (m_sEventList != null)
        {
            bool isExists = m_sEventList.Exists(delegate (SEventInfo sInfo)
            {
                return sInfo.eventType.Equals((int)info.type);
            });

            if (isExists == false)
                isExists = info.CheckClientEnable();
            isExists = isExists ? SdkManager.instance.CheckGameEventExists(info.index) : isExists;

            return isExists;
        }

        return false;
    }

    public EVENT_TYPE GetExistsFirstEventType()
    {
        EVENT_TYPE type = EVENT_TYPE.EVENT_NONE;
        if (m_sEventList != null)
        {
            List<EventMasterInfo> infoList = null;
            if (GetEventTable.GetEventMasterList(out infoList))
            {
                for (int i = 0; i < infoList.Count; i++)
                {
                    if (CheckEventExists(infoList[i]))
                    {
                        type = infoList[i].type;
                        break;
                    }
                }
            }
        }

        return type;
    }

    private void UpdateEventInfoNew(int type, bool isNew = false)
    {
        #region Button
        for (int i = 0; i < m_sEventList.Count; i++)
        {
            if (m_sEventList[i].eventType.Equals(type))
            {
                m_sEventList[i].confirm = isNew;
                break;
            }
        }
        #endregion

        #region Window
        m_eventWindow.UpdateNew();
        #endregion

        #region NoticeTR
        StateManager.instance.m_curState.UpdateGUIState();
        #endregion
    }

    public bool CheckEventNew(EVENT_TYPE type)
    {
        SEventInfo sInfo = GetEventInfo((int)type);
        if (sInfo != null)
        {
            switch (type)
            {
                case EVENT_TYPE.EVENT_DAILY_REWARD:
                case EVENT_TYPE.EVENT_SPECIAL_REWARD:
                case EVENT_TYPE.EVENT_TIME_REWARD: return sInfo.confirm;

                case EVENT_TYPE.EVENT_SINGLE_ACHIEVE: return AchievementManager.instance.CheckAchieveRewardEnable(ACHIEVE_TYPE.TYPE_SINGLE_EVENT);
                case EVENT_TYPE.EVENT_MULTY_ACHIEVE: return AchievementManager.instance.CheckAchieveRewardEnable(ACHIEVE_TYPE.TYPE_MULTY_EVENT);
                case EVENT_TYPE.EVENT_COLLECT: return CheckCollectGoodsEnable();
            }
        }

        return false;
    }

    private IEnumerator WaitForLoadSdkModuleData()
    {
        MsgBox.instance.OpenMsgBox(125, 574, MSGBOX_TYPE.OK, null);
        yield return StartCoroutine(SdkManager.instance.LoadSdkModuleData());
    }

    #endregion

    #region EventWindow_ViewCheck

    public bool CheckWindowViewInfo()
    {
        bool isView = true;

        if (PlayerPrefs.HasKey(EventWindow.DAILY_VIEW_CHECK) == false)
            PlayerPrefs.SetString(EventWindow.DAILY_VIEW_CHECK, LitJson.JsonMapper.ToJson(new WindowViewCheckInfo(true, Util.GetTimeAddDay(1) + 18000)));
        else
        {
            try
            {
                WindowViewCheckInfo viewInfo = LitJson.JsonMapper.ToObject<WindowViewCheckInfo>(PlayerPrefs.GetString(EventWindow.DAILY_VIEW_CHECK));
                if (viewInfo.isDisable)
                {
                    if (viewInfo.nextViewTime < Util.GetNowLocalGameTime())
                        PlayerPrefs.SetString(EventWindow.DAILY_VIEW_CHECK, LitJson.JsonMapper.ToJson(new WindowViewCheckInfo(true, Util.GetTimeAddDay(1) + 18000)));
                    else
                        isView = false;
                }
            }
            catch
            {
                PlayerPrefs.SetString(EventWindow.DAILY_VIEW_CHECK, LitJson.JsonMapper.ToJson(new WindowViewCheckInfo(true, Util.GetTimeAddDay(1) + 18000)));
            }
        }

        return isView;
    }

    public void SetWindowViewInfo(bool b)
    {
        PlayerPrefs.SetString(EventWindow.DAILY_VIEW_CHECK, LitJson.JsonMapper.ToJson(new WindowViewCheckInfo(b, Util.GetTimeAddDay(1) + 18000)));
    }

    public bool GetWindowViewDisableValue()
    {
        bool isDisable = true;
        try
        {
            WindowViewCheckInfo viewInfo = LitJson.JsonMapper.ToObject<WindowViewCheckInfo>(PlayerPrefs.GetString(EventWindow.DAILY_VIEW_CHECK));
            isDisable = viewInfo.isDisable;
        }
        catch { }

        return isDisable;
    }

    #endregion

    #region Normal_Attend

    private int m_attendDay = 0;
    private int m_rewardTime = 0;

    public int GetAttendDay { get { return m_attendDay; } }
    public int GetRewardTime { get { return m_rewardTime; } }

    public void UpdateAttendDay(int attendDay, int rewardTime)
    {
        m_attendDay = attendDay;
        m_rewardTime = rewardTime;
    }

    public bool CheckNormalRewardEnable()
    {
        if (m_rewardTime.Equals(0))
            return true;
        else
        {
            DateTime rewardDTime = Util.UnixTimeToLocalDateTime(m_rewardTime);
            return !rewardDTime.Day.Equals(Util.GetNowLocalDateGameTime().Day);
        }
    }

    public void SendAttendReceive(EventAttendRewardInfo info)
    {
        NetworkManager.instance.SendAttendReceive(info.index);
    }

    public void ResponseAttendReceive(int attendDay, int rewardTime)
    {
        InGameNotification.instance.UpdateCheckNotification(NOTI_TYPE.TYPE_EVENT, false, true);
        UpdateEventInfoNew((int)EVENT_TYPE.EVENT_DAILY_REWARD);

        UpdateAttendDay(attendDay, rewardTime);
        m_eventWindow.UIWindowResponse(EVENT_TYPE.EVENT_DAILY_REWARD, attendDay);
    }

    #endregion

    #region Special_Attend

    private bool m_isSpecialEventUpdate = false;
    public bool SetSpecialEventUpdate { set { m_isSpecialEventUpdate = value; } }

    private SpecialEventInfo m_specialEventInfo = null;
    public SpecialEventInfo GetSpecialEventInfo { get { return m_specialEventInfo; } }

    public void SendEventAttendInfo()
    {
        if (m_isSpecialEventUpdate == false)
            NetworkManager.instance.SendEventAttendInfo();
        else
            m_eventWindow.SetEventWindowGroup(EVENT_TYPE.EVENT_SPECIAL_REWARD, false);
    }

    public void SendEventAttendReceive(int attendIdx)
    {
        NetworkManager.instance.SendEventAttendReceive(m_specialEventInfo.m_index, attendIdx);
    }

    public void ResponseEventAttendInfo(RES_EVENT_ATTEND_INFO info)
    {
        m_isSpecialEventUpdate = true;

        m_specialEventInfo = new SpecialEventInfo(info);

        if (CheckWindowActive()) m_eventWindow.SetEventWindowGroup(EVENT_TYPE.EVENT_SPECIAL_REWARD, false);
        else OpenEventWindow(EVENT_TYPE.EVENT_SPECIAL_REWARD);
    }

    public void ResponseEventAttendReceive(string attendIdx, int rewardTimer)
    {
        m_isSpecialEventUpdate = true;

        if (m_specialEventInfo != null)
        {
            List<int> oldRewardList = new List<int>(m_specialEventInfo.m_rewardCompleteIndexList);
            m_specialEventInfo.UpdateCompleteIndex(attendIdx, rewardTimer);

            for (int i = 0; i < m_specialEventInfo.m_rewardCompleteIndexList.Count; i++)
            {
                if (oldRewardList.Contains(m_specialEventInfo.m_rewardCompleteIndexList[i]) == false)
                {
                    UpdateEventInfoNew((int)EVENT_TYPE.EVENT_SPECIAL_REWARD);

                    m_eventWindow.UIWindowResponse(EVENT_TYPE.EVENT_SPECIAL_REWARD, m_specialEventInfo.m_rewardCompleteIndexList[i]);
                    break;
                }
            }
        }
    }

    #endregion

    #region Time_Reward

    private bool m_isTimeRewardEventUpdate = false;
    public bool SetTimeRewardEventUpdate { set { m_isTimeRewardEventUpdate = value; } }

    private List<int> m_timeRewardIndexList = null;

    public void SendEventTimeInfo()
    {
        if (m_isTimeRewardEventUpdate == false)
            NetworkManager.instance.SendEventTimeInfo();
        else
            m_eventWindow.SetEventWindowGroup(EVENT_TYPE.EVENT_TIME_REWARD, false);
    }

    public void SendEventTimeReceive(int index)
    {
        NetworkManager.instance.SendEventTimeReceive(index);
    }

    public bool CheckEventRewardComplete(int index)
    {
        return m_timeRewardIndexList != null ? m_timeRewardIndexList.Contains(index) : false;
    }

    public void ResponseEventTimeInfo(int[] indexArray)
    {
        m_isTimeRewardEventUpdate = true;

        if (indexArray == null) m_timeRewardIndexList = new List<int>();
        else m_timeRewardIndexList = Util.ToList<int>(indexArray);

        if (CheckWindowActive()) m_eventWindow.SetEventWindowGroup(EVENT_TYPE.EVENT_TIME_REWARD, false);
        else OpenEventWindow(EVENT_TYPE.EVENT_TIME_REWARD);
    }

    public void ResponseEventTimeReceive(int[] indexArray)
    {
        m_isTimeRewardEventUpdate = true;

        int newIndex = 0;
        for (int i = 0; i < indexArray.Length; i++)
        {
            if (m_timeRewardIndexList.Contains(indexArray[i]) == false)
            {
                newIndex = indexArray[i];
                break;
            }
        }

        UpdateEventInfoNew((int)EVENT_TYPE.EVENT_TIME_REWARD);

        m_timeRewardIndexList = Util.ToList<int>(indexArray);
        m_eventWindow.UIWindowResponse(EVENT_TYPE.EVENT_TIME_REWARD, newIndex);
    }

    #endregion

    #region Burning_Time

    private BurningTimeInfo m_burningTimeInfo = null;
    public BurningTimeInfo GetBurningTimeInfo { get { return m_burningTimeInfo; } }

    public void UpdateBurningTimeInfo(int key)
    {
        m_burningTimeInfo = new BurningTimeInfo(ConvertBitArray(key));
    }

    public void UpdateServerBurningRatio(int addGold, int addExp)
    {
        if (m_burningTimeInfo != null)
            m_burningTimeInfo.UpdateServerAddRatio(addGold, addExp);
    }

    /// <summary>
    /// <para>name : CheckBurningTimeEnable</para>
    /// <para>describe : 버닝 타임이 진행 중인지 확인합니다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public bool CheckBurningTimeEnable()
    {
        return m_burningTimeInfo != null ? m_burningTimeInfo.CheckBurningTimeEnable() : false;
    }

    /// <summary>
    /// <para>name : GetBurningTimeAddRatio</para>
    /// <para>describe : 버닝 타임 시 추가 적용되는 %값.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public float GetBurningTimeAddRatio(BURNING_EVENT_TYPE type)
    {
        return m_burningTimeInfo != null ? m_burningTimeInfo.GetBurningAddValue(type) : 1.0f;
    }

    /// <summary>
    /// <para>name : GetBurningTimeSpriteName</para>
    /// <para>describe : 버닝 타임 시 추가되는 값을 나타내는 스프라이트 이름.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public string GetBurningTimeSpriteName(BURNING_EVENT_TYPE type)
    {
        float value = GetBurningTimeAddRatio(type);
        if (value <= 1.0f)
            return "";
        if (value <= 1.5f)
            return "Icon_BurningTime_x1.5";
        else if (value <= 1.8f)
            return "Icon_BurningTime_x1.8";
        else if (value <= 2.0f)
            return "Icon_BurningTime_x2";
        else
            return "Icon_BurningTime_x2.5";
    }

    /// <summary>
    /// <para>name : SetUISpriteBurningTime</para>
    /// <para>describe : 버닝 타임 시 스프라이트 세팅.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetUISpriteBurningTime(UISprite sprite, BURNING_EVENT_TYPE type)
    {
        sprite.gameObject.SetActive(!GetBurningTimeAddRatio(type).Equals(1.0f));
        sprite.spriteName = GetBurningTimeSpriteName(type);
    }

    /// <summary>
    /// <para>name : GetBurningTimeAddRatio</para>
    /// <para>describe : 버닝 타임 시 추가 적용되는 값을 더한 결과값.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public int GetBurningTimeAddValue(int value, BURNING_EVENT_TYPE type)
    {
        return (int)(value * GetBurningTimeAddRatio(type));
    }

    public bool CheckServerBurningTimeEnable(BURNING_EVENT_TYPE type)
    {
        return m_burningTimeInfo != null ? m_burningTimeInfo.GetServerBurningAddValue(type) > 1.0f : false;
    }

    /// <summary>
    /// <para>name : GetServerBurningTimeAddRatio</para>
    /// <para>describe : 서버에서 받은, 버닝 타임 시 추가 적용되는 %값.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public float GetServerBurningTimeAddRatio(BURNING_EVENT_TYPE type)
    {
        return m_burningTimeInfo != null ? m_burningTimeInfo.GetServerBurningAddValue(type) : 1.0f;
    }

    /// <summary>
    /// <para>name : GetBurningTimeSpriteName</para>
    /// <para>describe : 서버에서 받은, 버닝 타임 시 추가되는 값을 나타내는 스프라이트 이름.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public string GetServerBurningTimeSpriteName(BURNING_EVENT_TYPE type)
    {
        float value = GetServerBurningTimeAddRatio(type);
        if (value <= 1.0f)
            return "";
        else if (value <= 1.5f)
            return "Icon_BurningTime_x1.5";
        else if (value <= 1.8f)
            return "Icon_BurningTime_x1.8";
        else if (value <= 2.0f)
            return "Icon_BurningTime_x2";
        else
            return "Icon_BurningTime_x2.5";
    }

    /// <summary>
    /// <para>name : SetUISpriteBurningTime</para>
    /// <para>describe : 버닝 타임 시 스프라이트 세팅.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetUISpriteServerBurningTime(UISprite sprite, BURNING_EVENT_TYPE type)
    {
        sprite.gameObject.SetActive(!GetServerBurningTimeAddRatio(type).Equals(1.0f));
        sprite.spriteName = GetServerBurningTimeSpriteName(type);
    }

    /// <summary>
    /// <para>name : GetServerBurningTimeAddValue</para>
    /// <para>describe : 서버에서 받은, 버닝 타임 시 추가 적용되는 값을 더한 결과값.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public int GetServerBurningTimeAddValue(int value, BURNING_EVENT_TYPE type)
    {
        return (int)(value * GetServerBurningTimeAddRatio(type));
    }

    #region Util

    private int[] ConvertBitArray(int key)
    {
        int[] resultArray = { -1, -1, -1, -1, -1, -1 };

        int clampValue = (int)((byte)key & 0xfffffff);
        int bitWise = 1;

        int tempKey = 0;
        for (int i = 0; i < 30; i++)
        {
            if ((bitWise & clampValue) > 0)
            {
                resultArray[tempKey] = i + 1;
                ++tempKey;
            }

            bitWise = (int)((bitWise << 1) & 0xfffffffe);
        }

        return resultArray;
    }

    #endregion

    #endregion

    #region Collect

    private bool m_isCollectEventUpdate = false;
    public bool SetCollectEventUpdate { set { m_isCollectEventUpdate = value; } }

    private CollectEventInfo m_collectEventInfo = null;
    public CollectEventInfo GetCollectEventInfo { get { return m_collectEventInfo; } }

    public void SendEventCollectInfo()
    {
        if (m_isCollectEventUpdate == false)
            NetworkManager.instance.SendEventCollectInfo();
        else
            m_eventWindow.SetEventWindowGroup(EVENT_TYPE.EVENT_COLLECT, false);
    }

    public void SendEventCollectReceive()
    {
        NetworkManager.instance.SendEventCollectReceive();
    }

    public void ResponseEventCollectInfo(RES_EVENT_COLLECT_INFO info)
    {
        m_isCollectEventUpdate = true;

        m_collectEventInfo = new CollectEventInfo(info);

        if (CheckWindowActive()) m_eventWindow.SetEventWindowGroup(EVENT_TYPE.EVENT_COLLECT, false);
        else OpenEventWindow(EVENT_TYPE.EVENT_COLLECT);
    }

    public void ResponseEventCollectReceive(RES_EVENT_COLLECT_RECEIVE info)
    {
        m_isCollectEventUpdate = true;

        int newIndex = m_collectEventInfo.GetNewRewardIndex(info.rewardItem);

        UpdateEventInfoNew((int)EVENT_TYPE.EVENT_COLLECT);
        m_collectEventInfo.UpdateRewardInfo(info);

        m_eventWindow.UIWindowResponse(EVENT_TYPE.EVENT_COLLECT, newIndex, (uint)info.itemCode, info.itemValue);
    }

    public bool CheckCollectEventComplete(int index)
    {
        return m_collectEventInfo != null ? m_collectEventInfo.CheckRewardComplete(index) : false;
    }

    public bool CheckCollectEventEnable()
    {
        return m_collectEventInfo != null ? m_collectEventInfo.CheckEventEnable : false;
    }

    public bool CheckCollectGoodsEnable()
    {
        return m_collectEventInfo != null ? m_collectEventInfo.CheckGoodsEnable : false;
    }

    public void UpdateCollectEventInfo(int evtCollType, int evtMasterIdx, int evtCollsTime, int evtColleTime)
    {
        m_collectEventInfo = new CollectEventInfo(evtMasterIdx, evtCollsTime, evtColleTime);
    }

    public void UpdateCollectEventItemValue(int value)
    {
        if (m_collectEventInfo != null)
            m_collectEventInfo.UpdateItemValue(value);
    }

    public void UpdateCollectEventItemValue(SCollectEventReward info)
    {
        if (info != null && info.itemNum > 0)
        {
            if (m_collectEventInfo != null)
            {
                m_collectEventInfo.UpdateItemValue(info.itemSum);
                m_collectEventInfo.AddRewardInfo(new CollectEventInfo.CollectResult(info.itemSum - info.itemNum, info.itemNum));
            }
        }
    }

    public void OpenCollectEventResultToast()
    {
        if (CheckCollectEventEnable())
        {
            CollectEventInfo.CollectResult info = m_collectEventInfo.GetRewardInfo();
            if (info.originValue.Equals(0) == false)
                MsgBox.instance.OpenEventToast(info.originValue, info.addValue);
        }
    }

    #endregion

    #region Achieve

    public void SendEventAchieveFinish(int achCode)
    {
        NetworkManager.instance.SendEventAchieveFinish(achCode);
    }

    public void ResponseEventAchieveFinish(RES_ACHIEVE_FINISH info)
    {
        switch (m_eventWindow.GetSelectEventType)
        {
            case EVENT_TYPE.EVENT_SINGLE_ACHIEVE:
                InGameNotification.instance.UpdateCheckNotification(NOTI_TYPE.TYPE_POST, true, true);

                UpdateEventInfoNew((int)EVENT_TYPE.EVENT_SINGLE_ACHIEVE);
                m_eventWindow.UIWindowResponse(EVENT_TYPE.EVENT_SINGLE_ACHIEVE);
                break;

            case EVENT_TYPE.EVENT_MULTY_ACHIEVE:
                InGameNotification.instance.UpdateCheckNotification(NOTI_TYPE.TYPE_POST, true, true);

                UpdateEventInfoNew((int)EVENT_TYPE.EVENT_MULTY_ACHIEVE);
                m_eventWindow.UIWindowResponse(EVENT_TYPE.EVENT_MULTY_ACHIEVE, info.achCode);
                break;

            case EVENT_TYPE.EVENT_PROMOTION_COUPON01:
            case EVENT_TYPE.EVENT_PROMOTION_COUPON02:
            case EVENT_TYPE.EVENT_PROMOTION_COUPON03:
                SendEventPromoteCoupon(m_eventWindow.GetSelectEventType);
                break;
        }
    }

    #endregion

    #region Coupon

    public void SendEventReqCoupon(string value)
    {
        NetworkManager.instance.SendEventReqCoupon(value);
    }

    public void ResponseEventReqCoupon()
    {
        MsgBox.instance.OpenMsgBox(69, 176, MSGBOX_TYPE.OK, null);
        StateManager.instance.m_curState.UpdateGUIState();
    }

    #endregion

    #region Purchase

    private PurchaseEventInfo m_purchaseEventInfo = null;
    public PurchaseEventInfo GetPurchaseEventInfo { get { return m_purchaseEventInfo; } }

    public void UpdatePurchaseEventInfo(SPurchaseEventInfo info)
    {
        if (info != null) m_purchaseEventInfo = new PurchaseEventInfo(info);
        else m_purchaseEventInfo = new PurchaseEventInfo();
    }

    public bool UpdatePurchaseEventInfo(string packet)
    {
        bool isExists = false;
        if (m_purchaseEventInfo != null)
        {
            List<int> indexList = m_purchaseEventInfo.ParseIndex(packet);
            List<int> rewardIdxList = m_purchaseEventInfo.GetNewRewardIndex(indexList);

            isExists = !rewardIdxList.Count.Equals(0);
            if (isExists)
            {
                uint[] itemIndex = new uint[rewardIdxList.Count];
                int[] itemCount = new int[rewardIdxList.Count];

                for (int i = 0; i < rewardIdxList.Count; i++)
                {
                    EventPurchaseRewardInfo purchaseInfo = GetEventTable.GetPurchaseRewardInfo(rewardIdxList[i]);

                    itemIndex[i] = purchaseInfo.itemIndex;
                    itemCount[i] = purchaseInfo.itemValue;
                }

                MsgBox.instance.OpenRewardBox("", Str.instance.Get(377), itemIndex, itemCount);
            }

            m_purchaseEventInfo.UpdateEventInfo(indexList);
        }

        return isExists;
    }

    public bool CheckPurchaseEventComplete(int index)
    {
        return m_purchaseEventInfo != null ? m_purchaseEventInfo.CheckRewardComplete(index) : false;
    }

    public bool CheckPurchaseEventEnable()
    {
        return m_purchaseEventInfo != null ? m_purchaseEventInfo.CheckEventEnable : false;
    }

    #endregion

    #region Return

    private bool m_isReturnEventUpdate = false;
    public bool SetReturnEventUpdate { set { m_isReturnEventUpdate = value; } }

    private ReturnEventInfo m_returnEventInfo = null;
    public ReturnEventInfo GetReturnEventInfo { get { return m_returnEventInfo; } }

    public void SendEventReturnInfo()
    {
        if (m_isReturnEventUpdate == false)
            NetworkManager.instance.SendEventComebackInfo();
        else
            m_eventWindow.SetEventWindowGroup(EVENT_TYPE.EVENT_COMEBACK, false);
    }

    public void SendEventReturnReceive(int[] idxArray)
    {
        NetworkManager.instance.SendEventComebackReceive(idxArray);
    }

    public void ResponseEventReturnInfo(RES_EVENT_COMEBACK_INFO info)
    {
        m_isReturnEventUpdate = true;

        m_returnEventInfo = new ReturnEventInfo(info);

        if (CheckWindowActive()) m_eventWindow.SetEventWindowGroup(EVENT_TYPE.EVENT_COMEBACK, false);
        else OpenEventWindow(EVENT_TYPE.EVENT_COMEBACK);
    }

    public void ResponseEventReturnReceive(RES_EVENT_COMEBACK_RECEIVE info)
    {
        m_isReturnEventUpdate = true;

        List<int> newIndexList = m_returnEventInfo.GetNewRewardIndexList(info.rewardIdxs);

        UpdateEventInfoNew((int)EVENT_TYPE.EVENT_COMEBACK);
        m_returnEventInfo.UpdateInfo(info);

        m_eventWindow.UIWindowResponse(EVENT_TYPE.EVENT_COMEBACK, newIndexList);
    }

    #endregion

    #region PromoteCoupon

    private RES_EVENT_PROMOTE_COUPON m_promoteCouponInfo = null;
    public RES_EVENT_PROMOTE_COUPON GetPromoteCouponInfo { get { return m_promoteCouponInfo; } }

    public void SendEventPromoteCoupon(EVENT_TYPE type)
    {
        List<EventAchieveInfo> infoList = null;
        if (EventManager.instance.GetEventTable.GetEventAchieveInfoList(type, out infoList))
        {
            WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.EVENT_PAGE_TYPE, type);
            NetworkManager.instance.SendEventPromoteCoupon(infoList[0].achieveIndex);
        }
    }

    public void ResponseEventPromoteCoupon(RES_EVENT_PROMOTE_COUPON info)
    {
        m_promoteCouponInfo = info;
        
        EVENT_TYPE type = (EVENT_TYPE)WorldManager.instance.GetMemoryInfo(WORLD_MEMORY_INFO.EVENT_PAGE_TYPE);
        WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.EVENT_PAGE_TYPE);

        if (CheckWindowActive()) m_eventWindow.SetEventWindowGroup(type, false);
        else OpenEventWindow(type);
    }

    #endregion
}
