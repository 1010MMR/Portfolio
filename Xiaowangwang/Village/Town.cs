using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if USER_SERVER
using NetWork;
#endif

/// <summary>
/// <para>name : Town</para>
/// <para>describe : 마을 정보.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class Town
{
    private TOWN_USER_TYPE m_userType = TOWN_USER_TYPE.TOWN_USER_PLAYER;

    private bool m_isInit = false;

    private int m_supportAskCount = 0;

    private Dictionary<int, uint> m_townBuildingCodeDic = null;
    private Dictionary<int, STownBuilding> m_townBuildingDic = null;
    private Dictionary<int, STownRequest> m_townRequestDic = null;
    private Dictionary<int, STownEvent> m_townEventDic = null;
    private Dictionary<int, STownMission> m_townMissionDic = null;

    public Town()
    {
        Init();
    }

    #region TOWN_USER_TYPE

    /// <summary>
    /// <para>name : UserType</para>
    /// <para>describe : 현재 마을의 유저 타입</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public TOWN_USER_TYPE UserType
    {
        get { return m_userType; }
        set
        {
            if(m_userType.Equals(value) == false)
                InitData();
            m_userType = value;
        }
    }

    #endregion

    #region Current_Town

    /// <summary>
    /// <para>name : CurrentTownCode</para>
    /// <para>describe : 현재 선택된 마을 코드</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public int CurrentTownCode
    {
        get;
        set;
    }

    #endregion

    #region Select_Building_Info

    /// <summary>
    /// <para>name : CurrentBuildingInfo</para>
    /// <para>describe : 현재 선택한 건물의 정보</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public BuildingInfo CurrentBuildingInfo
    {
        get;
        set;
    }

    public string GetCurrentBuildingName
    {
        get { return CurrentBuildingInfo.name; }
    }

    public uint GetCurrentBuildingCode
    {
        get { return CurrentBuildingInfo.index; }
    }

    public int GetSTownBuildingNo()
    {
        if(CurrentBuildingInfo == null)
            return 0;
        
        List<STownBuilding> infoList = null;
        if(GetBuildingInfo(out infoList))
        {
            int index = infoList.FindIndex(delegate(STownBuilding info) {
                return info.bldCode.Equals((int)CurrentBuildingInfo.index);
            });

            if(index < 0)
                return GetBuildingNcNo(CurrentBuildingInfo.index);
            else
                return infoList[index].bldNo;
        }

        else
            return GetBuildingNcNo(CurrentBuildingInfo.index);
    }

    #endregion

    #region Init

    public void Init()
    {
        if(m_isInit == false)
            InitData();
    }

    /// <summary>
    /// <para>name : InitData</para>
    /// <para>describe : 데이터 초기화.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitData()
    {
        if(m_isInit == false)
        {
            if(m_townBuildingCodeDic == null)
                m_townBuildingCodeDic = new Dictionary<int, uint>();
            else
                m_townBuildingCodeDic.Clear();
        }

        if(m_townBuildingDic == null)
            m_townBuildingDic = new Dictionary<int, STownBuilding>();
        else
            m_townBuildingDic.Clear();

        if(m_townEventDic == null)
            m_townEventDic = new Dictionary<int, STownEvent>();
        else
            m_townEventDic.Clear();

        if(m_townMissionDic == null)
            m_townMissionDic = new Dictionary<int, STownMission>();
        else
            m_townMissionDic.Clear();

        if(m_townRequestDic == null)
            m_townRequestDic = new Dictionary<int, STownRequest>();
        else
            m_townRequestDic.Clear();

        m_isInit = true;
    }

    #endregion

    #region Support_Ask_Count

    /// <summary>
    /// <para>name : UpdateSupportAskCount</para>
    /// <para>describe : 후원 요청 가능한 횟수</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateSupportAskCount(int num)
    {
        m_supportAskCount = num;
    }

    public int GetSupportAskCount
    {
        get
        {
            return m_supportAskCount;
        }
    }

    #endregion

    #region Building_NC

    /// <summary>
    /// <para>name : AddBuildingCode</para>
    /// <para>describe : 건물 인덱스 데이터 적용.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void AddBuildingCode(STownBuildingNC[] infoArray)
    {
        if(infoArray != null)
        {
            m_townBuildingCodeDic.Clear();
            for(int i = 0; i < infoArray.Length; i++)
            {
                if(m_townBuildingCodeDic.ContainsKey(infoArray[i].no) == false)
                    m_townBuildingCodeDic.Add(infoArray[i].no, (uint)infoArray[i].code);
            }
        }
    }

    public void UpdateBuildingCode(int bldNo, uint bldCode)
    {
        if(m_townBuildingCodeDic.ContainsKey(bldNo))
            m_townBuildingCodeDic[bldNo] = bldCode;
        else
            m_townBuildingCodeDic.Add(bldNo, bldCode);
    }

    public bool CheckBuildingEnable(LocationInfo locInfo)
    {
        List<int> keyList = new List<int>(m_townBuildingCodeDic.Keys);
        for(int i = 0; i < keyList.Count; i++)
        {
            if((keyList[i] / 100).Equals(locInfo.villageCode))
            {
                BuildingInfo bldInfo = WorldManager.instance.m_dataManager.m_buildingData.GetBuildingInfo(m_townBuildingCodeDic[keyList[i]]);
                if(bldInfo.type.Equals((BUILDING_TYPE)locInfo.positionIndex))
                {
                    WorldManager.instance.m_town.CurrentBuildingInfo = bldInfo;
                    return true;
                }
            }
        }

        return false;
    }

    public bool CheckBuildingEnable(BUILDING_TYPE type)
    {
        List<int> keyList = new List<int>(m_townBuildingCodeDic.Keys);
        for(int i = 0; i < keyList.Count; i++)
        {
            BuildingInfo info = WorldManager.instance.m_dataManager.m_buildingData.
                GetBuildingInfo(m_townBuildingCodeDic[keyList[i]]);
            if(info.type.Equals(type))
                return true;
        }

        return false;
    }

    public int GetBuildingNcNo(uint bldCode)
    {
        List<int> keyList = new List<int>(m_townBuildingCodeDic.Keys);
        for(int i = 0; i < keyList.Count; i++)
        {
            if(m_townBuildingCodeDic[keyList[i]].Equals(bldCode))
                return keyList[i];
        }

        return 0;
    }

    public BuildingInfo GetBuildingInfoNc(int bldNo)
    {
        return m_townBuildingCodeDic.ContainsKey(bldNo) ? WorldManager.instance.m_dataManager.m_buildingData.
            GetBuildingInfo(m_townBuildingCodeDic[bldNo]) : null;
    }

    public BuildingInfo GetBuildingInfoNc(BUILDING_TYPE type)
    {
        List<int> keyList = new List<int>(m_townBuildingCodeDic.Keys);
        for(int i = 0; i < keyList.Count; i++)
        {
            BuildingInfo info = WorldManager.instance.m_dataManager.m_buildingData.
                GetBuildingInfo(m_townBuildingCodeDic[keyList[i]]);
            if(info.type.Equals(type))
                return info;
        }

        return null;
    }

    #endregion

    #region Building

    /// <summary>
    /// <para>name : AddBuilding</para>
    /// <para>describe : 건물 패킷 데이터 적용.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void AddBuilding(STownBuilding[] infoArray)
    {
        if(infoArray != null)
        {
            m_townBuildingDic.Clear();
            for(int i = 0; i < infoArray.Length; i++)
            {
                if(m_townBuildingDic.ContainsKey(infoArray[i].bldNo) == false)
                    m_townBuildingDic.Add(infoArray[i].bldNo, infoArray[i]);
            }
        }
    }

    /// <summary>
    /// <para>name : UpdateBuilding</para>
    /// <para>describe : 건물 패킷 데이터 업데이트.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateBuilding(STownBuilding info)
    {
        if(m_townBuildingDic.ContainsKey(info.bldNo))
            m_townBuildingDic[info.bldNo] = info;
        else
            m_townBuildingDic.Add(info.bldNo, info);

        UpdateBuildingCode(info.bldNo, (uint)info.bldCode);
    }

    public bool GetBuildingInfo(out List<STownBuilding> infoList)
    {
        infoList = new List<STownBuilding>(m_townBuildingDic.Values);
        return !infoList.Count.Equals(0);
    }

    public STownBuilding GetBuildingInfo(int key)
    {
        return m_townBuildingDic.ContainsKey(key) ? m_townBuildingDic[key] : null;
    }

    public STownBuilding GetBuildingInfo(BuildingInfo info)
    {
        foreach(STownBuilding value in m_townBuildingDic.Values)
        {
            if(value.bldCode.Equals((int)info.index))
                return value;
        }

        return null;
    }

    #endregion

    #region Event

    /// <summary>
    /// <para>name : AddEvent</para>
    /// <para>describe : 이벤트 패킷 데이터 적용.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void AddEvent(STownEvent[] infoArray)
    {
        if(infoArray != null)
        {
            m_townEventDic.Clear();
            for(int i = 0; i < infoArray.Length; i++)
                m_townEventDic.Add(infoArray[i].evCode, infoArray[i]);
        }
    }

    /// <summary>
    /// <para>name : UpdateEvent</para>
    /// <para>describe : 이벤트 패킷 데이터 업데이트.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateEvent(STownEvent info)
    {
        if(m_townEventDic.ContainsKey(info.evCode))
            m_townEventDic[info.evCode] = info;
        else
            m_townEventDic.Add(info.evCode, info);
    }

    public bool GetTownEvent(out List<STownEvent> infoList)
    {
        infoList = new List<STownEvent>(m_townEventDic.Values);
        return !infoList.Count.Equals(0);
    }

    public STownEvent GetTownEvent(int key)
    {
        return m_townEventDic.ContainsKey(key) ? m_townEventDic[key] : null;
    }

    public void RemoveEvent(int key)
    {
        if(m_townEventDic.ContainsKey(key))
            m_townEventDic.Remove(key);
    }

    #endregion

    #region Mission

    /// <summary>
    /// <para>name : AddMission</para>
    /// <para>describe : 미션 패킷 데이터 적용.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void AddMission(STownMission[] infoArray)
    {
        if(infoArray != null)
        {
            m_townMissionDic.Clear();
            for(int i = 0; i < infoArray.Length; i++)
                m_townMissionDic.Add(infoArray[i].miCode, infoArray[i]);
        }
    }

    /// <summary>
    /// <para>name : UpdateMission</para>
    /// <para>describe : 미션 패킷 데이터 업데이트.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateMission(STownMission info)
    {
        if(m_townMissionDic.ContainsKey(info.miCode))
            m_townMissionDic[info.miCode] = info;
        else
            m_townMissionDic.Add(info.miCode, info);
    }

    public bool GetTownMission(out List<STownMission> infoList)
    {
        infoList = new List<STownMission>(m_townMissionDic.Values);
        return !infoList.Count.Equals(0);
    }

    public STownMission GetTownMission(int key)
    {
        return m_townMissionDic.ContainsKey(key) ? m_townMissionDic[key] : null;
    }

    public void RemoveMission(int key)
    {
        if(m_townMissionDic.ContainsKey(key))
            m_townMissionDic.Remove(key);
    }

    #endregion

    #region Request

    /// <summary>
    /// <para>name : AddRequest</para>
    /// <para>describe : 의뢰 패킷 데이터 적용.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void AddRequest(STownRequest[] infoArray)
    {
        if(infoArray != null)
        {
            m_townRequestDic.Clear();
            for(int i = 0; i < infoArray.Length; i++)
            {
                if(m_townRequestDic.ContainsKey(infoArray[i].bldNo) == false)
                    m_townRequestDic.Add(infoArray[i].bldNo, infoArray[i]);
            }
        }
    }

    /// <summary>
    /// <para>name : UpdateRequest</para>
    /// <para>describe : 의뢰 패킷 데이터 업데이트.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateRequest(STownRequest info)
    {
        if(m_townRequestDic.ContainsKey(info.bldNo))
            m_townRequestDic[info.bldNo] = info;
        else
            m_townRequestDic.Add(info.bldNo, info);

        PushManager.instance.SendNotification(PUSH_TYPE.TYPE_TOWN_REQUEST_COMPLETE, info);
    }

    public void RemoveRequest(int bldNo)
    {
        if(m_townRequestDic.ContainsKey(bldNo))
            m_townRequestDic.Remove(bldNo);
        PushManager.instance.CancelNotification(PUSH_TYPE.TYPE_TOWN_REQUEST_COMPLETE, bldNo);
    }

    public STownRequest GetRequestInfo(int key)
    {
        return m_townRequestDic.ContainsKey(key) ? m_townRequestDic[key] : null;
    }

    public bool CheckRequestInfoExists(int key)
    {
        return m_townRequestDic.ContainsKey(key);
    }

    #endregion

    #region Response_Town_Enter

    /// <summary>
    /// <para>name : ResponseTownEnter</para>
    /// <para>describe : 마을 들어가기.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void ResponseTownEnter(RES_TOWN_ENTER packet)
    {
        CurrentTownCode = packet.townCode;
        m_supportAskCount = packet.supportAskCnt;

        AddBuilding(packet.buildings);
        AddRequest(packet.requests);
        AddEvent(packet.events);
        AddMission(packet.missions);
    }

    /// <summary>
    /// <para>name : ResponseTownEnter</para>
    /// <para>describe : 친구 마을 들어가기.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void ResponseSocTownEnter(RES_SOC_TOWN_ENTER packet)
    {
        CurrentTownCode = packet.townCode;

        STownBuilding[] infoArray = new STownBuilding[packet.buildings.Length];
        for(int i = 0; i < infoArray.Length; i++)
            infoArray[i] = new STownBuilding(packet.buildings[i]);

        AddBuilding(infoArray);
    }

    #endregion
}
