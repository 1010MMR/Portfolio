using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#if USER_SERVER
using NetWork;
#endif

public class AchievementManager : MonoSingleton<AchievementManager>
{
    private const string ACHIEVEMENTWINDOW_PATH = "[Prefabs]/[Gui]/UI_S90_Achievement";

    private AchievementWindow m_achieveWindow = null;
    public AchievementWindow GetAchieveWindow { get { return m_achieveWindow; } }

    private Dictionary<ACHIEVE_TYPE, List<AchievementInfo>> m_achieveInfoTable = null;
    private Dictionary<int, SAchieveInfo> m_sAchieveInfoTable = null;

    private int m_refreshTime = 0;

    #region Init

    public override void Init()
    {
        m_refreshTime = 0;

        InitTable();
    }

    private void InitTable()
    {
        if (m_achieveInfoTable == null) m_achieveInfoTable = new Dictionary<ACHIEVE_TYPE, List<AchievementInfo>>();
        else m_achieveInfoTable.Clear();

        if (m_sAchieveInfoTable == null) m_sAchieveInfoTable = new Dictionary<int, SAchieveInfo>();
        else m_sAchieveInfoTable.Clear();
    }

    private void CreateWindow()
    {
        GameObject createObj = Instantiate(AssetBundleEx.Load<GameObject>(ACHIEVEMENTWINDOW_PATH)) as GameObject;
        createObj.transform.localPosition = Vector3.zero;
        createObj.transform.localScale = Vector3.one;

        m_achieveWindow = createObj.GetComponent<AchievementWindow>();
    }

    #endregion

    public void OpenAchieveWindow(ACHIEVE_TAB_TYPE type = ACHIEVE_TAB_TYPE.TYPE_DAILY)
    {
        if (m_refreshTime > Util.GetNowGameTime())
        {
            if (m_achieveWindow == null)
                CreateWindow();
            m_achieveWindow.OpenWindow(type);
        }
        else
        {
            WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.ACHIEVE_PAGE_TYPE, type);
            NetworkManager.instance.SendAchieveInfo();
        }

    }

    #region Achievement

    public void UpdateAchieveInfoTable(RES_ACHIEVE_INFO packet)
    {
        InitTable();

        m_refreshTime = packet.refreshTime;

        #region SAchieveInfo
        for (int i = 0; i < packet.achInfos.Length; i++)
            m_sAchieveInfoTable.Add(packet.achInfos[i].achCode, packet.achInfos[i]);
        #endregion

        #region Day
        if (m_achieveInfoTable.ContainsKey(ACHIEVE_TYPE.TYPE_DAILY_RANDOM) == false)
            m_achieveInfoTable.Add(ACHIEVE_TYPE.TYPE_DAILY_RANDOM, new List<AchievementInfo>());

        for (int i = 0; i < packet.dayAchs.Length; i++)
        {
            AchievementInfo info = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(packet.dayAchs[i]);
            if (info != null)
            {
                m_achieveInfoTable[ACHIEVE_TYPE.TYPE_DAILY_RANDOM].Add(info);
                if (m_sAchieveInfoTable.ContainsKey(info.index) == false)
                    m_sAchieveInfoTable.Add(info.index, new SAchieveInfo(info.index, (int)ACHIEVE_PROGRESS_TYPE.TYPE_PROGRESS, 0));
            }
        }
        #endregion

        #region Week
        if (m_achieveInfoTable.ContainsKey(ACHIEVE_TYPE.TYPE_WEEKLY_RANDOM) == false)
            m_achieveInfoTable.Add(ACHIEVE_TYPE.TYPE_WEEKLY_RANDOM, new List<AchievementInfo>());

        for (int i = 0; i < packet.weekAchs.Length; i++)
        {
            AchievementInfo info = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(packet.weekAchs[i]);
            if (info != null)
            {
                m_achieveInfoTable[ACHIEVE_TYPE.TYPE_WEEKLY_RANDOM].Add(info);
                if (m_sAchieveInfoTable.ContainsKey(info.index) == false)
                    m_sAchieveInfoTable.Add(info.index, new SAchieveInfo(info.index, (int)ACHIEVE_PROGRESS_TYPE.TYPE_PROGRESS, 0));
            }
        }
        #endregion

        #region Finish
        if (m_achieveInfoTable.ContainsKey(ACHIEVE_TYPE.TYPE_NONE) == false)
            m_achieveInfoTable.Add(ACHIEVE_TYPE.TYPE_NONE, new List<AchievementInfo>());

        for (int i = 0; i < packet.finAchs.Length; i++)
        {
            AchievementInfo info = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(packet.finAchs[i]);
            if (info != null)
            {
                if (info.type.Equals(ACHIEVE_TYPE.TYPE_PROGRESS))
                    m_achieveInfoTable[ACHIEVE_TYPE.TYPE_NONE].Add(info);
                if (m_sAchieveInfoTable.ContainsKey(packet.finAchs[i]) == false)
                    m_sAchieveInfoTable.Add(packet.finAchs[i], new SAchieveInfo(packet.finAchs[i], (int)ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE, 0));
                else
                    m_sAchieveInfoTable[packet.finAchs[i]] = new SAchieveInfo(packet.finAchs[i], (int)ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE, 0);
            }
        }
        #endregion

        #region Progress
        if (m_achieveInfoTable.ContainsKey(ACHIEVE_TYPE.TYPE_PROGRESS) == false)
            m_achieveInfoTable.Add(ACHIEVE_TYPE.TYPE_PROGRESS, new List<AchievementInfo>());

        List<int> groupIndexList = new List<int>();
        List<SAchieveInfo> sInfoList = new List<SAchieveInfo>(m_sAchieveInfoTable.Values);
        for (int i = 0; i < sInfoList.Count; i++)
        {
            if (sInfoList[i].GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_COMPLETE))
                groupIndexList.Add(WorldManager.instance.m_dataManager.m_achievementData.GetInfo(sInfoList[i].achCode).group);
        }
        sInfoList = null;

        List<AchievementInfo> progressInfoList = null;
        if (WorldManager.instance.m_dataManager.m_achievementData.GetProgressAchieveList(new List<int>(m_sAchieveInfoTable.Keys), groupIndexList, out progressInfoList))
        {
            for (int i = 0; i < progressInfoList.Count; i++)
            {
                m_achieveInfoTable[ACHIEVE_TYPE.TYPE_PROGRESS].Add(progressInfoList[i]);

                if (m_sAchieveInfoTable.ContainsKey(progressInfoList[i].index) == false)
                    m_sAchieveInfoTable.Add(progressInfoList[i].index, new SAchieveInfo(progressInfoList[i].index, (int)ACHIEVE_PROGRESS_TYPE.TYPE_NONE, 0));
            }
        }
        #endregion
    }

    public void UpdateAchieveInfoTable(SAchieveInfo[] infoArray)
    {
        for (int i = 0; i < infoArray.Length; i++)
            UpdateAchieveInfoTable(infoArray[i]);

        for (int i = 0; i < infoArray.Length; i++)
        {
            if (infoArray[i].GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
            {
                ACHIEVE_TYPE aType = infoArray[i].GetAchieveType;
                if (aType.Equals(ACHIEVE_TYPE.TYPE_DAILY_RANDOM) || aType.Equals(ACHIEVE_TYPE.TYPE_WEEKLY_RANDOM))
                {
                    AchievementInfo info = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(infoArray[i].achCode);
                    MsgBox.instance.OpenMsgToast(Str.instance.Get(683, "%INDEX%", info.GetTitle));
                    break;
                }
            }
        }

        StateManager.instance.m_curState.UpdateGUIState();
    }

    public void UpdateAchieveInfoTable(SAchieveInfo info)
    {
        if (m_sAchieveInfoTable.ContainsKey(info.achCode)) m_sAchieveInfoTable[info.achCode] = info;
        else m_sAchieveInfoTable.Add(info.achCode, info);
    }

    public SAchieveInfo GetServerAchieveInfo(int code)
    {
        return m_sAchieveInfoTable.ContainsKey(code) ? m_sAchieveInfoTable[code] : null;
    }

    public bool GetAchieveInfoList(ACHIEVE_TYPE type, out List<AchievementInfo> infoList)
    {
        return m_achieveInfoTable.TryGetValue(type, out infoList);
    }

    public bool GetDailyTabAchieveInfoList(out List<AchievementInfo>[] infoListArray)
    {
        infoListArray = new List<AchievementInfo>[3];

        infoListArray[0] = new List<AchievementInfo>();
        infoListArray[1] = new List<AchievementInfo>(m_achieveInfoTable[ACHIEVE_TYPE.TYPE_DAILY_RANDOM]);
        infoListArray[2] = new List<AchievementInfo>(m_achieveInfoTable[ACHIEVE_TYPE.TYPE_WEEKLY_RANDOM]);

        for (int i = 1; i < 3; i++)
        {
            for (int j = infoListArray[i].Count - 1; j >= 0; j--)
            {
                SAchieveInfo sInfo = GetServerAchieveInfo(infoListArray[i][j].index);
                if (sInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
                {
                    infoListArray[0].Add(infoListArray[i][j]);
                    infoListArray[i].RemoveAt(j);
                }
            }
        }

        return true;
    }

    public bool CheckAchieveRewardEnable(ACHIEVE_TAB_TYPE type = ACHIEVE_TAB_TYPE.TYPE_NONE)
    {
        List<SAchieveInfo> infoList = new List<SAchieveInfo>(m_sAchieveInfoTable.Values);

        switch (type)
        {
            case ACHIEVE_TAB_TYPE.TYPE_NONE:
                for (int i = 0; i < infoList.Count; i++)
                {
                    if (infoList[i].GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
                    {
                        ACHIEVE_TYPE aType = infoList[i].GetAchieveType;
                        if (aType.Equals(ACHIEVE_TYPE.TYPE_SINGLE_EVENT) == false && aType.Equals(ACHIEVE_TYPE.TYPE_MULTY_EVENT) == false)
                            return true;
                    }
                }
                return false;

            case ACHIEVE_TAB_TYPE.TYPE_DAILY:
                for (int i = 0; i < infoList.Count; i++)
                {
                    if (infoList[i].GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
                    {
                        ACHIEVE_TYPE aType = infoList[i].GetAchieveType;
                        if (aType.Equals(ACHIEVE_TYPE.TYPE_DAILY_RANDOM) || aType.Equals(ACHIEVE_TYPE.TYPE_WEEKLY_RANDOM))
                            return true;
                    }
                }
                return false;

            case ACHIEVE_TAB_TYPE.TYPE_PROGRESS:
                for (int i = 0; i < infoList.Count; i++)
                {
                    if (infoList[i].GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
                    {
                        ACHIEVE_TYPE aType = infoList[i].GetAchieveType;
                        if (aType.Equals(ACHIEVE_TYPE.TYPE_PROGRESS))
                            return true;
                    }
                }
                return false;

            default:
                return false;
        }
    }

    public bool CheckAchieveRewardEnable(ACHIEVE_TYPE type)
    {
        List<SAchieveInfo> infoList = new List<SAchieveInfo>(m_sAchieveInfoTable.Values);
        for (int i = 0; i < infoList.Count; i++)
        {
            if (infoList[i].GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
            {
                ACHIEVE_TYPE aType = infoList[i].GetAchieveType;
                if (aType.Equals(type))
                    return true;
            }
        }

        return false;
    }

    public bool CheckAchieveRewardEnable(ACHIEVE_TYPE[] typeArray)
    {
        if (typeArray != null)
        {
            List<SAchieveInfo> infoList = new List<SAchieveInfo>(m_sAchieveInfoTable.Values);
            for (int i = 0; i < infoList.Count; i++)
            {
                if (infoList[i].GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
                {
                    ACHIEVE_TYPE aType = infoList[i].GetAchieveType;
                    for (int j = 0; j < typeArray.Length; j++)
                    {
                        if (aType.Equals(typeArray[j]))
                            return true;
                    }
                }
            }
        }

        return false;
    }

    public bool CheckAchieveRewardComplete(AchievementInfo info)
    {
        foreach (SAchieveInfo sInfo in m_sAchieveInfoTable.Values)
        {
            AchievementInfo aInfo = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(sInfo.achCode);
            if (aInfo != null && aInfo.achIndex.Equals(info.achIndex) && aInfo.index >= info.index)
                return true;
        }

        return false;
    }

    public int GetCompleteAchievementCount()
    {
        int count = 0;
        foreach (SAchieveInfo sInfo in m_sAchieveInfoTable.Values)
        {
            if (sInfo.GetProgressType.Equals(ACHIEVE_PROGRESS_TYPE.TYPE_CLEAR))
                count++;
        }

        return count;
    }

    #endregion

    #region Network

    public void SendAchieveFinish(int achCode)
    {
        NetworkManager.instance.SendAchieveFinish(achCode);
    }

    public void ResponseAchieveFinish(RES_ACHIEVE_FINISH packet, bool isMsgView = true)
    {
        AchievementInfo info = WorldManager.instance.m_dataManager.m_achievementData.GetInfo(packet.achCode);

        switch (info.type)
        {
            case ACHIEVE_TYPE.TYPE_PROGRESS:
                m_achieveInfoTable[ACHIEVE_TYPE.TYPE_PROGRESS].Remove(info);

                if (info.index.Equals(packet.achInfo.achCode))
                    m_achieveInfoTable[ACHIEVE_TYPE.TYPE_NONE].Add(info);
                else
                {
                    m_sAchieveInfoTable.Remove(info.index);
                    m_achieveInfoTable[ACHIEVE_TYPE.TYPE_PROGRESS].Add(WorldManager.instance.m_dataManager.m_achievementData.GetInfo(packet.achInfo.achCode));
                }
                break;

            case ACHIEVE_TYPE.TYPE_MULTY_EVENT:
                m_achieveInfoTable[ACHIEVE_TYPE.TYPE_PROGRESS].Remove(info);

                if (info.index.Equals(packet.achInfo.achCode)) 
                    m_achieveInfoTable[ACHIEVE_TYPE.TYPE_NONE].Add(info);
                else 
                    m_sAchieveInfoTable.Remove(info.index);
                break;
        }

        UpdateAchieveInfoTable(packet.achInfo);

        if (isMsgView)
            MsgBox.instance.OpenRewardBox("", Str.instance.Get(220), info.rewardIndex, info.rewardCount);
        if (m_achieveWindow != null)
            m_achieveWindow.RefreshWindow();

        StateManager.instance.m_curState.UpdateGUIState();
    }

    #endregion
}
