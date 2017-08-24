using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if USER_SERVER
using NetWork;
#endif

/// <summary>
/// <para>name : Quest</para>
/// <para>describe : 퀘스트 정보.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class Quest
{
    public const int MAX_SUB_QUEST = 3;

    private bool m_isInit = false;

    private Dictionary<int, SQuest> m_questDic = null;
    private Dictionary<int, List<SQuestSub>> m_questSubDic = null;
    private List<SQuest> m_exNewQuestList = null;

    private Dictionary<int, SQuestSub> m_completeQuestSubDic = null;

    public Quest()
    {
        Init();
    }

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
        if(m_questDic == null)
            m_questDic = new Dictionary<int, SQuest>();
        else
            m_questDic.Clear();

        if(m_questSubDic == null)
            m_questSubDic = new Dictionary<int, List<SQuestSub>>();
        else
            m_questSubDic.Clear();

        if(m_exNewQuestList == null)
            m_exNewQuestList = new List<SQuest>();
        else
            m_exNewQuestList.Clear();

        if(m_completeQuestSubDic == null)
            m_completeQuestSubDic = new Dictionary<int, SQuestSub>();
        else
            m_completeQuestSubDic.Clear();
    }

    #endregion

    #region Quest_Master

    /// <summary>
    /// <para>name : AddQuestMaster</para>
    /// <para>describe : 퀘스트 완료 정보 데이터 적용.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void AddQuestMaster(int[] infoArray)
    {
        if(infoArray != null)
        {
            m_questDic.Clear();
            for(int i = 0; i < infoArray.Length; i++)
            {
                if(m_questDic.ContainsKey(infoArray[i]) == false)
                    m_questDic.Add(infoArray[i], 
                        new SQuest(infoArray[i], (int)QUEST_CLEAR_STATE.QUEST_STATE_COMPLETE));
            }
        }
    }

    /// <summary>
    /// <para>name : UpdateQuestMaster</para>
    /// <para>describe : 퀘스트 정보 데이터 업데이트.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateQuestMaster(SQuest info)
    {
        if(m_questDic.ContainsKey(info.qCode))
            m_questDic[info.qCode] = info;
        else
            m_questDic.Add(info.qCode, info);
    }

    /// <summary>
    /// <para>name : GetAllQuestMasterList</para>
    /// <para>describe : 퀘스트 전체 정보 데이터 리스트를 받아감.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public bool GetAllQuestMasterList(out List<SQuest> infoList)
    {
        infoList = new List<SQuest>(m_questDic.Values);
        return !infoList.Count.Equals(0);
    }

    /// <summary>
    /// <para>name : GetDoingQuestMasterList</para>
    /// <para>describe : 진행 중인 퀘스트 정보 데이터 리스트를 받아감.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public bool GetDoingQuestMasterList(out List<SQuest> infoList)
    {
        infoList = new List<SQuest>();
        List<SQuest> questMasterList = new List<SQuest>(m_questDic.Values);

        for(int i = 0; i < questMasterList.Count; i++)
        {
            if(questMasterList[i].GetState.Equals(QUEST_CLEAR_STATE.QUEST_STATE_DOING))
                infoList.Add(questMasterList[i]);
        }

        infoList.Sort(delegate(SQuest a, SQuest b) {
            if(a.GetQuestType.Equals(QUEST_TYPE.QUEST_TYPE_MAIN) && !b.GetQuestType.Equals(QUEST_TYPE.QUEST_TYPE_MAIN))
                return -1;
            else if(!a.GetQuestType.Equals(QUEST_TYPE.QUEST_TYPE_MAIN) && b.GetQuestType.Equals(QUEST_TYPE.QUEST_TYPE_MAIN))
                return 1;
            else
                return a.qCode.CompareTo(b.qCode);
        });

        return !infoList.Count.Equals(0);
    }

    /// <summary>
    /// <para>name : GetQuestMaster</para>
    /// <para>describe : 퀘스트 정보 데이터를 받아감.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public SQuest GetQuestMaster(int key)
    {
        return m_questDic.ContainsKey(key) ? m_questDic[key] : null;
    }

    /// <summary>
    /// <para>name : GetNewQuestMasterList</para>
    /// <para>describe : 퀘스트 신규 리스트를 받아감.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public bool GetNewQuestMasterList(out List<SQuest> infoList)
    {
        infoList = new List<SQuest>();

        List<QuestMasterInfo> dataList = null;
        if(WorldManager.instance.m_dataManager.m_questMasterData.GetQuestMasterList(out dataList))
        {
            for(int i = 0; i < dataList.Count; i++)
            {
                if(m_questDic.ContainsKey(dataList[i].index) == false && WorldManager.instance.m_player.m_level >= dataList[i].userLevel)
                {
                    if(dataList[i].preQuestIndex.Equals(0))
                        infoList.Add(new SQuest(dataList[i].index, 0));
                    else
                        if(m_questDic.ContainsKey(dataList[i].preQuestIndex) &&
                            m_questDic[dataList[i].preQuestIndex].GetState.Equals(QUEST_CLEAR_STATE.QUEST_STATE_COMPLETE))
                            infoList.Add(new SQuest(dataList[i].index, 0));
                }
            }

            m_exNewQuestList.AddRange(infoList);
        }

        return !infoList.Count.Equals(0);
    }

    public bool GetExistsNewQuestIndexList(out List<int> indexList)
    {
        indexList = new List<int>();
        for(int i = 0; i < m_exNewQuestList.Count; i++)
            indexList.Add(m_exNewQuestList[i].qCode);

        return !indexList.Count.Equals(0);
    }

    #endregion

    #region Quest_Sub

    /// <summary>
    /// <para>name : AddQuestSub</para>
    /// <para>describe : 퀘스트 서브 정보 데이터 적용.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void AddQuestSub(SQuestSub[] infoArray)
    {
        if(infoArray != null)
        {
            m_questSubDic.Clear();
            for(int i = 0; i < infoArray.Length; i++)
            {
                if(m_questSubDic.ContainsKey(infoArray[i].qCode) == false)
                    m_questSubDic.Add(infoArray[i].qCode, new List<SQuestSub>());
                m_questSubDic[infoArray[i].qCode].Add(infoArray[i]);

                if(m_questDic.ContainsKey(infoArray[i].qCode) == false)
                    UpdateQuestMaster(new SQuest(infoArray[i].qCode, (int)QUEST_CLEAR_STATE.QUEST_STATE_DOING));
            }
        }
    }

    /// <summary>
    /// <para>name : UpdateQuestSub</para>
    /// <para>describe : 퀘스트 서브 정보 데이터 업데이트. (복수)</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateQuestSub(int key, SQuestSub[] infoArray)
    {
        if(m_questSubDic.ContainsKey(key))
            m_questSubDic[key].Clear();
        else
            m_questSubDic.Add(key, new List<SQuestSub>());

        for(int i = 0; i < infoArray.Length; i++)
            m_questSubDic[key].Add(infoArray[i]);
    }

    /// <summary>
    /// <para>name : UpdateQuestSub</para>
    /// <para>describe : 퀘스트 서브 정보 데이터 업데이트. (복수)</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateQuestSub(SQuestSub[] infoArray)
    {
        bool isCompleteQuest = false;
        SQuestSub completeQSub = null;

        for(int i = 0; i < infoArray.Length; i++)
        {
            isCompleteQuest = infoArray[i].GetState.Equals(QUEST_CLEAR_STATE.QUEST_STATE_CLEAR) ? true : isCompleteQuest;
            if( isCompleteQuest && completeQSub == null )
                completeQSub = infoArray[i];

            UpdateQuestSub(infoArray[i]);
        }

        StateManager.instance.m_curState.UpdateQuestList();

        if(isCompleteQuest)
            AddCompleteToast(completeQSub);
    }

    /// <summary>
    /// <para>name : UpdateQuestSub</para>
    /// <para>describe : 퀘스트 서브 정보 데이터 업데이트. (단일)</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateQuestSub(SQuestSub info)
    {
        if(m_questSubDic.ContainsKey(info.qCode))
        {
            int index = m_questSubDic[info.qCode].FindIndex(delegate(SQuestSub sub) {
                return sub.subCode.Equals(info.subCode);
            });

            if(index > -1)
                m_questSubDic[info.qCode][index] = info;
            else
                m_questSubDic[info.qCode].Add(info);
        }

        else
        {
            m_questSubDic.Add(info.qCode, new List<SQuestSub>());
            m_questSubDic[info.qCode].Add(info);
        }
    }

    /// <summary>
    /// <para>name : GetQuestSubList</para>
    /// <para>describe : 퀘스트 서브 정보 데이터 리스트를 받아감.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public bool GetQuestSubList(int key, out List<SQuestSub> infoList)
    {
        infoList = null;
        if(m_questSubDic.ContainsKey(key))
            infoList = new List<SQuestSub>(m_questSubDic[key]);

        return infoList != null;
    }

    public bool CheckQuestSubListComplete(int key)
    {
        List<SQuestSub> infoList = null;
        if(GetQuestSubList(key, out infoList))
        {
            bool isExists = infoList.Exists(delegate(SQuestSub sub) {
                return sub.GetState.Equals(QUEST_CLEAR_STATE.QUEST_STATE_CLEAR);
            });

            return isExists;
        }

        return false;
    }

    public bool CheckQuestSubListAllComplete(int key)
    {
        List<SQuestSub> infoList = null;
        if (GetQuestSubList(key, out infoList))
        {
            for (int i = 0; i < infoList.Count; i++)
            {
                if (infoList[i].GetState.Equals(QUEST_CLEAR_STATE.QUEST_STATE_CLEAR) == false)
                    return false;
            }
        }

        return true;
    }

    #endregion

    #region Complete_Toast

    private void AddCompleteToast(SQuestSub info)
    {  
        QuestSubInfo qSInfo = WorldManager.instance.m_dataManager.m_questSubData.GetInfo(info.subCode);
        QuestContentsInfo qCInfo = WorldManager.instance.m_dataManager.m_questContentsData.GetInfo(qSInfo.questContentsIndex);

        switch((QUEST_COMPLETE_TYPE)qCInfo.typeIndex)
        {
            case QUEST_COMPLETE_TYPE.TYPE_COSTUME_FIGHT_WIN:
            case QUEST_COMPLETE_TYPE.TYPE_COSTUME_FIGHT_SUCCESSION:
                if(m_completeQuestSubDic.ContainsKey(qCInfo.typeIndex) == false)
                    m_completeQuestSubDic[qCInfo.typeIndex] = info;
                else
                    m_completeQuestSubDic.Add(qCInfo.typeIndex, info);
                break;

            default:
                QuestWindow.instance.ShowToastMessage(qSInfo);
                break;
        }
    }

    public void ShowQuestCompleteToastMessage(QUEST_COMPLETE_TYPE[] typeArray)
    {
        for(int i = 0; i < typeArray.Length; i++)
            ShowQuestCompleteToastMessage(typeArray[i]);
    }

    public void ShowQuestCompleteToastMessage(QUEST_COMPLETE_TYPE type)
    {
        int index = (int)type;
        if(m_completeQuestSubDic.ContainsKey(index) && m_completeQuestSubDic[index] != null)
        {
            QuestWindow.instance.ShowToastMessage(m_completeQuestSubDic[index]);
            m_completeQuestSubDic.Remove(index);
        }
    }

    #endregion

    #region Util

    static public string GetNpcThumbSpriteName(string imgName)
    {
        return string.Format("{0}_Face", imgName);
    }

    static public string GetNpcScenarioSpriteName(string imgName)
    {
        return string.Format("{0}_Upper", imgName);
    }

    #endregion
}
