using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// <para>name : TownGroup</para>
/// <para>describe : 마을 배경 및 오브젝트 관리.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class TownGroup : MonoBehaviour
{
    private delegate IEnumerator Func();

    private GUIManager_Village m_villageManager = null;
    private TownInfo m_townInfo = null;

    private Transform[] m_objectGroup = null;

    private UISpriteAnimation[] m_waveAnimationGroup = null;

    private Transform[] m_cloudGroup = null;
    private List<Transform[]> m_cloudPathGroup = null;

    private Transform m_shipObj = null;
    private Transform[] m_carGroup = null;
    private List<CarPathData> m_listCarPathData = null;
    private Dictionary<int, Queue<Transform>> m_dicCarPathPool = null;

    private Dictionary<int, Transform> m_dicBuildingDummy = null;
    private Dictionary<int, Transform> m_dicLevelFlagDummy = null;
    private Dictionary<int, Transform> m_dicMissionDummy = null;
    private Dictionary<int, Transform> m_dicDogPositionDummy = null;
	private Dictionary<int, Transform> m_dicTalkDummy = null;

    private GameObject m_buildingButtonObject = null;
    private GameObject[] m_townEventObjectGroup = null;

    private List<BuildingButton> m_buildingButtonGroup = null;
    private List<TownEventTrigger> m_eventObjGroup = null;
	private List<TownTalkItem> m_talkGroup = null;

    #region Get

    public TownInfo GetTownInfo
    {
        get { return m_townInfo; }
    }

    public float GetTownMaxWidth
    {
        get { return m_townInfo != null ? m_townInfo.townWidthSize : 0.0f; }
    }

    #endregion

    #region Init

    public void Init(GUIManager_Village main, TownInfo townInfo)
    {
        m_villageManager = main;
        m_townInfo = townInfo;

        InitObjectGroup();

        InitWave();
        InitCloud();

        InitMovingObject();

        InitDogPositionDummy();
        InitMissionDummy();
		InitTalkDummy();

        InitBuildingDummy();
    }

    public void Init()
    {
        FuncStartCoroutine(waveAni);

        InitCoursePool();
        StartCar();
    }

    #endregion

    #region ResetPosition

    public void ResetPosition()
    {
        transform.localPosition = Vector3.zero;

        #region Object_Group
        if(m_objectGroup != null)
        {
            for(int i = 0; i < m_objectGroup.Length; i++)
                m_objectGroup[i].transform.localPosition = Vector3.zero;
        }
        #endregion

        #region Building
        if(m_buildingButtonGroup != null)
        {
            for(int i = 0; i < m_buildingButtonGroup.Count; i++)
                m_buildingButtonGroup[i].UpdateButton();
        }
        #endregion
    }

    #endregion

    #region Object_Group

    /// <summary>
    /// <para>name : InitObjectGroup</para>
    /// <para>describe : 오브젝트 그룹을 Root 밑으로 추가.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitObjectGroup()
    {
        string[] objectPathArray = { "S01_Group", "S03_Group", "S05_Group" };
        m_objectGroup = new Transform[objectPathArray.Length];

        for (int i = 0; i < objectPathArray.Length; i++)
        {
            Transform objTrans = transform.FindChild(objectPathArray[i]);
            if (objTrans != null)
            {
                Transform parent = m_villageManager.m_panelGroup[i];
                objTrans.parent = parent;

                Util.ChangeLayersRecursively(objTrans, parent.gameObject.layer);

                m_objectGroup[i] = objTrans;
            }
        }
    }

    #endregion

    #region Wave

    private void InitWave()
    {
        m_waveAnimationGroup = m_objectGroup[(int)GUIManager_Village.PANEL_TYPE.PANEL_S01].
            FindChild("WavePool").GetComponentsInChildren<UISpriteAnimation>();

        for (int i = 0; i < m_waveAnimationGroup.Length; i++)
        {
            m_waveAnimationGroup[i].gameObject.SetActive(false);
        }

        FuncStartCoroutine(waveAni);
    }

    private void FuncStartCoroutine(Func func)
    {
        StartCoroutine(func.Method.Name);
    }

    private IEnumerator waveAni()
    {
        while (true)
        {
            int index = Random.Range(0, m_waveAnimationGroup.Length);

            m_waveAnimationGroup[index].gameObject.SetActive(true);
            if (!m_waveAnimationGroup[index].isPlaying)
                m_waveAnimationGroup[index].Play();

            yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));
        }
    }

    #endregion

    #region Cloud

    private const int MAX_CLOUD_COUNT = 3;
    private int[] m_cloudIndexArray = null;

    private int m_cloudIndex = 0;
    private bool m_isCloudInit = false;

    private void InitCloud()
    {
        m_isCloudInit = false;

        InitCloudObject();
        InitCloudPath();

        InitCloudMove();
    }

    private void InitCloudObject()
    {
        m_cloudGroup = new Transform[MAX_CLOUD_COUNT];
        for (int i = 0; i < MAX_CLOUD_COUNT; i++)
        {
            Transform cloudObj = m_objectGroup[(int)GUIManager_Village.PANEL_TYPE.PANEL_S05].
                FindChild(string.Format("Cloud{0:D2}", i + 1));

            m_cloudGroup[i] = cloudObj;
        }
    }

    private void InitCloudPath()
    {
        m_cloudPathGroup = new List<Transform[]>();

        m_cloudIndexArray = new int[] { 0, 1, 2 };
        Util.Shuffle(m_cloudIndexArray);

        for (int i = 0; i < MAX_CLOUD_COUNT; i++)
        {
            Transform[] pathTransArray = new Transform[MAX_CLOUD_COUNT];
            for (int j = 0; j < MAX_CLOUD_COUNT; j++)
                pathTransArray[j] = transform.FindChild(string.Format("CloudPath_Group/Path{0:D2}_{1:D2}", i + 1, j + 1));

            m_cloudPathGroup.Add(pathTransArray);
        }
    }

    private void InitCloudMove()
    {
        for (int i = 0; i < m_cloudGroup.Length; i++)
        {
            CloudMove(m_cloudGroup[i].gameObject);
        }
    }

    private void CloudMove(GameObject cloudObj)
    {
        if (m_isCloudInit)
            cloudObj.transform.localPosition = m_cloudPathGroup[m_cloudIndexArray[m_cloudIndex]][0].localPosition;
        else
            cloudObj.transform.localPosition = m_cloudPathGroup[m_cloudIndexArray[m_cloudIndex]][1].localPosition;

        float time = (m_cloudPathGroup[m_cloudIndexArray[m_cloudIndex]][2].localPosition - cloudObj.transform.localPosition).sqrMagnitude;
        iTween.MoveTo(cloudObj, iTween.Hash("position", m_cloudPathGroup[m_cloudIndexArray[m_cloudIndex]][2].localPosition, "speed", Random.Range(20, 30), "delay", 0, "easetype", iTween.EaseType.linear,
                                                             "islocal", true, "oncomplete", "CompleteMoveCloud", "oncompletetarget", gameObject, "oncompleteparams", cloudObj));

        if (m_cloudIndex < m_cloudIndexArray.Length - 1)
        {
            m_cloudIndex++;
        }
        else
        {
            m_cloudIndex = 0;
            Util.Shuffle(m_cloudIndexArray);
        } 
    }

    void CompleteMoveCloud(GameObject cloudObj)
    {
        m_isCloudInit = true;
        CloudMove(cloudObj);
    }

    #endregion

    #region Car

    private const int MAX_CAR = 10;

    private void InitMovingObject()
    {
        InitMovingObjectList();
        InitCarPathData();
        InitCoursePool();

        StartCar();
    }

    /// <summary>
    /// <para>name : InitCarList</para>
    /// <para>describe : 자동차, 배 오브젝트 불러오기 및 초기화.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitMovingObjectList()
    {
        Dictionary<int, string[]> carObjectPathTable = new Dictionary<int, string[]>();
        carObjectPathTable.Add(1, new string[] { "[Prefabs]/[Others]/Prb_Bus01", "[Prefabs]/[Others]/Prb_Car01" });
        carObjectPathTable.Add(2, new string[] { "[Prefabs]/[Others]/Prb_Car02", "[Prefabs]/[Others]/Prb_Car03", "[Prefabs]/[Others]/Prb_Car04" });

        int currentTownCode = WorldManager.instance.m_town.CurrentTownCode;
        GameObject[] carObjectArray = new GameObject[carObjectPathTable.ContainsKey(currentTownCode) ?
            carObjectPathTable[currentTownCode].Length : 0];

        for(int i = 0; i < carObjectPathTable[currentTownCode].Length; i++)
            carObjectArray[i] = AssetBundleEx.Load<GameObject>(carObjectPathTable[currentTownCode][i]);

        m_carGroup = new Transform[MAX_CAR];
        for (int i = 0; i < MAX_CAR; i++)
        {
            m_carGroup[i] = Instantiate(carObjectArray[Random.Range(0, carObjectArray.Length)].transform, Vector3.one * 9999.0f, Quaternion.identity) as Transform;

            m_carGroup[i].parent = m_villageManager.m_panelGroup[(int)GUIManager_Village.PANEL_TYPE.PANEL_S01];
            m_carGroup[i].gameObject.name = string.Format("Car_{0:D2}", i);
            m_carGroup[i].localScale = Vector3.one * 360.0f;

            Util.SetGameObjectLayer(m_carGroup[i].gameObject, LayerMask.NameToLayer("Background"));
        }

        m_shipObj = m_objectGroup[(int)GUIManager_Village.PANEL_TYPE.PANEL_S01].FindChild("MoveShip");
    }

    /// <summary>
    /// <para>name : InitCarPathData</para>
    /// <para>describe : 패스 데이터를 초기화.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitCarPathData()
    {
        m_listCarPathData = new List<CarPathData>();
        Transform center = transform.FindChild("CarPath_Group");

        for (int i = 0; i < center.childCount; i++)
        {
            Transform child = center.GetChild(i);
            if (child.name.Contains("CarPath") && child.gameObject.activeSelf)
                m_listCarPathData.Add(child.GetComponent<CarPathData>());
        }
    }

    /// <summary>
    /// <para>name : InitCoursePool</para>
    /// <para>describe : 패스 데이터의 숫자만큼 풀을 만들고, 만들어진 오브젝트를 랜덤하게 넣음.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitCoursePool()
    {
        List<int> indexList = new List<int>();
        m_dicCarPathPool = new Dictionary<int, Queue<Transform>>();
        for (int i = 0; i < m_listCarPathData.Count; i++)
        {
            m_dicCarPathPool.Add(i, new Queue<Transform>());
            if (m_listCarPathData[i].m_isStartingPoint)
                indexList.Add(i);
        }

        for (int i = 0; i < m_carGroup.Length; i++)
        {
            int index = indexList[Random.Range(0, indexList.Count)];
            m_dicCarPathPool[index].Enqueue(m_carGroup[i]);
        }
    }

    /// <summary>
    /// <para>name : ReturnCoursePool</para>
    /// <para>describe : 본래의 코스의 풀에 오브젝트를 돌려넣음.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void ReturnCoursePool(int index, Transform tf)
    {
        List<int> keyIndexList = new List<int>(m_dicCarPathPool.Keys);
        int selectIndex = keyIndexList.FindIndex(delegate(int key)
        {
            return key.Equals(index);
        });
        selectIndex++;
        if (keyIndexList.Count.Equals(selectIndex))
            selectIndex = 0;

        m_dicCarPathPool[keyIndexList[selectIndex]].Enqueue(tf);
    }

    private void StartCar()
    {
        for (int i = 0; i < m_dicCarPathPool.Keys.Count; i++)
        {
            if (m_dicCarPathPool[i].Count > 0)
            {
                Transform carTF = m_dicCarPathPool[i].Dequeue();

                if (null == carTF.gameObject.GetComponent<CarDirection>())
                    carTF.gameObject.AddComponent<CarDirection>().Init(this, i, m_listCarPathData[i]);
                else
                    carTF.gameObject.GetComponent<CarDirection>().Init(this, i, m_listCarPathData[i]);
            }
        }

        Invoke("StartCar", Random.Range(5, 8));
    }

    #endregion

    #region Mission

    /// <summary>
    /// <para>name : InitMissionDummy</para>
    /// <para>describe : 미션 더미 위치 데이터를 초기화.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitMissionDummy()
    {
        m_dicMissionDummy = new Dictionary<int, Transform>();
        Transform missionGroup = transform.FindChild("Mission_Group");
        for (int i = 0; i < missionGroup.childCount; i++)
        {
            Transform child = missionGroup.GetChild(i);
            if (child.name.Contains("Mission_"))
            {
                int index = 0;
                string indexStr = child.name.Replace("Mission_", "");
                if (int.TryParse(indexStr, out index))
                {
                    if (m_dicMissionDummy.ContainsKey(index))
                        m_dicMissionDummy[index] = child;
                    else
                        m_dicMissionDummy.Add(index, child);
                }
            }
        }

        m_eventObjGroup = new List<TownEventTrigger>();

        string[] townEventObjPath = { "", "[Prefabs]/[Gui]/TownEventTrigger_Gold", "[Prefabs]/[Gui]/TownEventTrigger_Event", "[Prefabs]/[Gui]/TownEventTrigger_Gold", 
                                      "[Prefabs]/[Gui]/TownEventTrigger_Event", "[Prefabs]/[Gui]/TownEventTrigger_Event" };
        m_townEventObjectGroup = new GameObject[(int)TOWN_OBJ_TYPE.TYPE_END];

        for(int i=0; i<townEventObjPath.Length; i++)
            m_townEventObjectGroup[i] = AssetBundleEx.Load<GameObject>(townEventObjPath[i]);
    }

    public void SetTownEvent(STownEvent townEvent)
    {
        TownEventObjInfo info = WorldManager.instance.m_dataManager.m_townEventObjData.GetTownEventObjTable((uint)townEvent.evCode);

        if(info != null)
        {
            Transform targetTrans = info.positionIndex.Equals(0) ? GetTargetTransform(info.type) : m_dicMissionDummy[info.positionIndex];
            if(targetTrans != null)
            {
                int index = m_eventObjGroup.FindIndex(delegate(TownEventTrigger trigger) {
                    return !trigger.TriggerActive && trigger.ObjectType.Equals(info.type);
                });

                if(index > -1)
                    m_eventObjGroup[index].Init(info, targetTrans);
                else
                {
                    GameObject obj = Instantiate(m_townEventObjectGroup[(int)info.type]) as GameObject;
                    obj.transform.parent = m_villageManager.m_panelGroup[(int)GUIManager_Village.PANEL_TYPE.PANEL_S09];
                    obj.transform.localScale = Vector3.one;
                    Util.SetGameObjectLayer(obj, LayerMask.NameToLayer("UIBackground"));

                    TownEventTrigger eventTrigger = obj.GetComponent<TownEventTrigger>();
                    eventTrigger.Init(info, targetTrans);

                    m_eventObjGroup.Add(eventTrigger);
                }
            }
        }
    }

    public void SetTownMission(STownMission townMission)
    {
        TownMissionObjInfo info = WorldManager.instance.m_dataManager.m_townMissionObjData.GetTownMissionObjTable((uint)townMission.miCode);

        if(info != null)
        {
            Transform targetTrans = m_dicMissionDummy[townMission.pos];
            if(targetTrans != null)
            {
                int index = m_eventObjGroup.FindIndex(delegate(TownEventTrigger trigger) {
                    return !trigger.TriggerActive && trigger.ObjectType.Equals(info.type);
                });

                if(index > -1)
                    m_eventObjGroup[index].Init(info, targetTrans);
                else
                {
                    GameObject obj = Instantiate(m_townEventObjectGroup[(int)info.type]) as GameObject;
                    obj.transform.parent = m_villageManager.m_panelGroup[(int)GUIManager_Village.PANEL_TYPE.PANEL_S09];
                    obj.transform.localScale = Vector3.one;
                    Util.SetGameObjectLayer(obj, LayerMask.NameToLayer("UIBackground"));

                    TownEventTrigger eventTrigger = obj.GetComponent<TownEventTrigger>();
                    eventTrigger.Init(info, targetTrans);

                    m_eventObjGroup.Add(eventTrigger);
                }
            }
        }
    }

    public TownEventTrigger GetTownEventTrigger(int evCode)
    {
        int index = m_eventObjGroup.FindIndex(delegate(TownEventTrigger trigger) {
            return trigger.GetCode().Equals(evCode);
        });

        return index > -1 ? m_eventObjGroup[index] : null;
    }

    public TownEventTrigger GetActiveTownEventTrigger(TOWN_OBJ_TYPE type)
    {
        for(int i = 0; i < m_eventObjGroup.Count; i++)
        {
            if(m_eventObjGroup[i] != null && m_eventObjGroup[i].ObjectType.Equals(type) && m_eventObjGroup[i].CheckEventTime)
                return m_eventObjGroup[i];
        }

        return null;
    }

    private Transform GetTargetTransform(TOWN_OBJ_TYPE type)
    {
        List<Transform> existsTargetList = new List<Transform>();
        List<Transform> targetList = new List<Transform>();

        for(int i = 0; i < m_eventObjGroup.Count; i++)
        {
            if(m_eventObjGroup[i].TargetTransform != null)
                existsTargetList.Add(m_eventObjGroup[i].TargetTransform);
        }

        switch(type)
        {
            case TOWN_OBJ_TYPE.TYPE_CAR:
                for(int i = 0; i < m_carGroup.Length; i++)
                    if(existsTargetList.Contains(m_carGroup[i]) == false)
                        targetList.Add(m_carGroup[i]);
                return targetList[Random.Range(0, targetList.Count)];

            case TOWN_OBJ_TYPE.TYPE_SHIP:
                return existsTargetList.Contains(m_shipObj) == false && m_shipObj != null ? m_shipObj : null;

            default:
                return null;
        }
    }

    /// <summary>
    /// <para>name : SetMissionTriggerActive</para>
    /// <para>describe : 미션 트리거를 ON/OFF</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetMissionTriggerActive(bool isSwitch)
    {
        if(m_eventObjGroup != null)
        {
            for(int i = 0; i < m_eventObjGroup.Count; i++)
                m_eventObjGroup[i].SetActive(isSwitch);
        }
    }

    #endregion

    #region Building

    /// <summary>
    /// <para>name : InitBuildingDummy</para>
    /// <para>describe : 빌딩/레벨 표지판 더미 위치 데이터를 초기화.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitBuildingDummy()
    {
        m_dicBuildingDummy = new Dictionary<int, Transform>();
        m_dicLevelFlagDummy = new Dictionary<int, Transform>();

        Transform buildingGroup = transform.FindChild("Building_Group");
        for (int i = 0; i < buildingGroup.childCount; i++)
        {
            Transform child = buildingGroup.GetChild(i);
            if (child.name.Contains("Building_"))
            {
                int index = 0;
                string indexStr = child.name.Replace("Building_", "");
                if (int.TryParse(indexStr, out index))
                {
                    if (m_dicBuildingDummy.ContainsKey(index))
                        m_dicBuildingDummy[index] = child;
                    else
                        m_dicBuildingDummy.Add(index, child);

                    Transform levelChild = child.FindChild("LevelFlag");
                    if(m_dicLevelFlagDummy.ContainsKey(index))
                        m_dicLevelFlagDummy[index] = levelChild;
                    else
                        m_dicLevelFlagDummy.Add(index, levelChild);
                }
            }
        }

        m_buildingButtonGroup = new List<BuildingButton>();
        m_buildingButtonObject = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/Town_Building_Button");
    }

    /// <summary>
    /// <para>name : SetBuildingButton</para>
    /// <para>describe : 마을 건물을 배치.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetBuildingButton(STownBuilding info)
    {
        int index = m_buildingButtonGroup.FindIndex(delegate(BuildingButton button) {
            return button.TownBuilding.bldNo.Equals(info.bldNo);
        });

        if(WorldManager.instance.m_dataManager.m_buildingData.CheckBuildingInfoExists((uint)info.bldCode))
        {
            if(index > -1)
                m_buildingButtonGroup[index].Init(info);
            else
            {
                GameObject obj = Instantiate(m_buildingButtonObject) as GameObject;
                obj.transform.parent = m_villageManager.m_panelGroup[(int)GUIManager_Village.PANEL_TYPE.PANEL_S05];
                obj.transform.localScale = Vector3.one;
                Util.SetGameObjectLayer(obj, LayerMask.NameToLayer("UIBackground"));

                BuildingButton buildingButton = obj.GetComponent<BuildingButton>();
                buildingButton.Init(info);

                m_buildingButtonGroup.Add(buildingButton);
            }
        }
    }

    /// <summary>
    /// <para>name : UpdateBuildingButton</para>
    /// <para>describe : 마을 건물을 업데이트.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public BuildingButton UpdateBuildingButton(STownBuilding info)
    {
        int index = m_buildingButtonGroup.FindIndex(delegate(BuildingButton button) {
            return button.TownBuilding.bldNo.Equals(info.bldNo);
        });

        if(index > -1)
            m_buildingButtonGroup[index].Init(info);
        else
            return null;

        return m_buildingButtonGroup[index];
    }

    public BuildingButton GetBuildingButton(int buildingNo)
    {
        int index = m_buildingButtonGroup.FindIndex(delegate(BuildingButton button) {
            return button.TownBuilding.bldNo.Equals(buildingNo);
        });

        if(index > -1)
            return m_buildingButtonGroup[index];
        else
            return null;
    }

    public Vector3 GetBuildingPosition(int index)
    {
        return m_dicBuildingDummy.ContainsKey(index) ? transform.localPosition + m_dicBuildingDummy[index].localPosition : Vector3.one * 9999.0f;
    }

    public Vector3 GetLevelFlagPosition(int index)
    {
        return m_dicLevelFlagDummy.ContainsKey(index) ? m_dicLevelFlagDummy[index].localPosition : Vector3.zero;
    }

    /// <summary>
    /// <para>name : SetBuildingBannerActive</para>
    /// <para>describe : 건물의 배너를 ON/OFF</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetBuildingBannerActive(bool isSwitch)
    {
        if (m_buildingButtonGroup != null)
        {
            for (int i = 0; i < m_buildingButtonGroup.Count; i++)
                m_buildingButtonGroup[i].SetTextGroup(isSwitch);
        }
    }

    /// <summary>
    /// <para>name : SetBuildingInfoPopupActive</para>
    /// <para>describe : 건물의 말풍선을 ON/OFF</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetBuildingInfoPopupActive(bool isSwitch)
    {
        if (m_buildingButtonGroup != null)
        {
            for (int i = 0; i < m_buildingButtonGroup.Count; i++)
                m_buildingButtonGroup[i].SetInfoPopup(isSwitch);
        }
    }

    /// <summary>
    /// <para>name : SetBuildingRequestPopupActive</para>
    /// <para>describe : 의뢰 말풍선을 ON/OFF</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetBuildingRequestPopupActive(bool isSwitch)
    {
        if(m_buildingButtonGroup != null)
        {
            for(int i = 0; i < m_buildingButtonGroup.Count; i++)
                m_buildingButtonGroup[i].SetRequestPopup(isSwitch);
        }
    }

    /// <summary>
    /// <para>name : UpdateAllBuilding</para>
    /// <para>describe : 모든 빌딩 업데이트.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void UpdateAllBuilding()
    {
        for(int i = 0; i < m_buildingButtonGroup.Count; i++)
            m_buildingButtonGroup[i].UpdateButton();
    }

    public BuildingButton GetNextButton(BuildingButton button, bool isSwitch)
    {
        int index = m_buildingButtonGroup.FindIndex(delegate(BuildingButton btn) {
            return btn.Equals(button);
        });

        if (index < 0)
            return null;
        else
        {
            int nextIndex = GetNextIndex(index, isSwitch);
            return m_buildingButtonGroup[nextIndex];
        }
    }

    private int GetNextIndex(int index, bool isSwitch)
    {
        int nextIndex = isSwitch ? ++index : --index;
        if(nextIndex.Equals(m_buildingButtonGroup.Count))
            nextIndex = 0;
        else if(nextIndex < 0)
            nextIndex = m_buildingButtonGroup.Count - 1;

        if(m_buildingButtonGroup[nextIndex].TownBuilding.CheckOpen == false ||
            m_buildingButtonGroup[nextIndex].TownBuilding.CheckUpgradeState)
            return GetNextIndex(nextIndex, isSwitch);
        else
            return nextIndex;
    }

    #endregion

    #region DogPosition

    /// <summary>
    /// <para>name : InitDogPositionDummy</para>
    /// <para>describe : 강아지 더미 위치 데이터를 초기화.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitDogPositionDummy()
    {
        m_dicDogPositionDummy = new Dictionary<int, Transform>();
        Transform dummyGroup = transform.FindChild("DogPosition_Group");
        for(int i = 0; i < dummyGroup.childCount; i++)
        {
            Transform child = dummyGroup.GetChild(i);
            if(child.name.Contains("Position_"))
            {
                int index = 0;
                string indexStr = child.name.Replace("Position_", "");
                if(int.TryParse(indexStr, out index))
                {
                    if(m_dicDogPositionDummy.ContainsKey(index))
                        m_dicDogPositionDummy[index] = child;
                    else
                        m_dicDogPositionDummy.Add(index, child);
                }
            }
        }
    }

    public Transform GetDogPositionTransform(int index)
    {
        return m_dicDogPositionDummy.ContainsKey(index) ? m_dicDogPositionDummy[index] : null;
    }

	#endregion

	#region Talk

	private void InitTalkDummy()
	{
		m_dicTalkDummy = new Dictionary<int, Transform>();
		Transform talkGroup = transform.FindChild("Talk_Group");
		for(int i = 0; i < talkGroup.childCount; i++)
		{
			Transform child = talkGroup.GetChild(i);
			if(child.name.Contains("Talk_"))
			{
				int index = 0;
				string indexStr = child.name.Replace("Talk_", "");
				if(int.TryParse(indexStr, out index))
				{
					if(m_dicTalkDummy.ContainsKey(index))
						m_dicTalkDummy[index] = child;
					else
						m_dicTalkDummy.Add(index, child);
				}
			}
		}

		GameObject talkObj = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UITownTalkItem");
		m_talkGroup = new List<TownTalkItem>();
		for(int i = 0; i < 15; i++)
		{
			GameObject obj = Instantiate(talkObj) as GameObject;
			obj.transform.parent = m_villageManager.m_panelGroup[(int)GUIManager_Village.PANEL_TYPE.PANEL_S13];
			obj.transform.localScale = Vector3.one;
			Util.SetGameObjectLayer(obj, LayerMask.NameToLayer("UIBackground"));

			m_talkGroup.Add(obj.GetComponent<TownTalkItem>());
		}
	}

	private Vector3 GetTalkDummyPosition(int key)
	{
		return m_dicTalkDummy.ContainsKey(key) ? m_dicTalkDummy[key].localPosition : Vector3.zero; 
	}

	public void StartTownTalk()
	{
		StopCoroutine("OnTownTalk");
		StartCoroutine("OnTownTalk");
	}

	public void StopTownTalk()
	{
		StopCoroutine("OnTownTalk");
        
        if (m_talkGroup != null)
        {
            for (int i = 0; i < m_talkGroup.Count; i++)
                m_talkGroup[i].Release();
        }
    }

	private IEnumerator OnTownTalk()
	{
		float waitTime = 0;
		List<TownTalkInfo> infoList = WorldManager.instance.m_dataManager.m_townTalkData.GetRandomTownTalkList();
		for(int i = 0; i < infoList.Count; i++)
		{
			if(m_talkGroup.Count > i)
				m_talkGroup[i].Init(i, infoList[i], GetTalkDummyPosition(infoList[i].location));
			waitTime = Mathf.Max(waitTime, infoList[i].timer);
		}

		yield return new WaitForSeconds(waitTime);

		for(int i = 0; i < m_talkGroup.Count; i++)
			m_talkGroup[i].Release();
		StartTownTalk();
	}

	#endregion

	#region Release

	public void StopTownGroup()
    {
        StopAllCoroutines();
        CancelInvoke();

        #region Mission
        if(m_eventObjGroup != null)
        {
            for(int i = 0; i < m_eventObjGroup.Count; i++)
                DestroyImmediate(m_eventObjGroup[i].gameObject);
            m_eventObjGroup = null;
        }
        #endregion

        #region Car
        if(m_carGroup != null)
        {
            for(int i = 0; i < m_carGroup.Length; i++)
                DestroyImmediate(m_carGroup[i].gameObject);
            m_carGroup = null;
        }
        #endregion

        #region Object
        if(m_objectGroup != null)
        {
            for(int i = 0; i < m_objectGroup.Length; i++)
                DestroyImmediate(m_objectGroup[i].gameObject);
            m_objectGroup = null;
        }
        #endregion

		#region TownTalk
		if(m_talkGroup != null)
		{
			for(int i = 0; i < m_talkGroup.Count; i++)
				DestroyImmediate(m_talkGroup[i].gameObject);
			m_talkGroup = null;
		}
		#endregion
    }

    public void Release()
    {
        StopAllCoroutines();
        CancelInvoke();

        #region Mission
        if(m_eventObjGroup != null)
        {
            for(int i = 0; i < m_eventObjGroup.Count; i++)
                DestroyImmediate(m_eventObjGroup[i].gameObject);
            m_eventObjGroup = null;
        }
        #endregion

        #region Car
        if(m_carGroup != null)
        {
            for(int i = 0; i < m_carGroup.Length; i++)
                DestroyImmediate(m_carGroup[i].gameObject);
            m_carGroup = null;
        }
        #endregion

        #region Object
        if(m_objectGroup != null)
        {
            for(int i = 0; i < m_objectGroup.Length; i++)
                DestroyImmediate(m_objectGroup[i].gameObject);
            m_objectGroup = null;
        }
        #endregion

        #region Building
        if(m_buildingButtonGroup != null)
        {
            for(int i = 0; i < m_buildingButtonGroup.Count; i++)
            {
                m_buildingButtonGroup[i].Release();
                DestroyImmediate(m_buildingButtonGroup[i].gameObject);
            }
            m_buildingButtonGroup = null;
        }
		#endregion

		#region TownTalk
		if(m_talkGroup != null)
		{
			for(int i = 0; i < m_talkGroup.Count; i++)
				DestroyImmediate(m_talkGroup[i].gameObject);
			m_talkGroup = null;
		}
		#endregion

		#region Map
		DestroyImmediate(gameObject);
        #endregion
    }

    #endregion

    #region EDITOR_ONLY
#if UNITY_EDITOR

    private const int EDITOR_MAX_CAR = 10;

    private void EditorInitMovingObject()
    {
        EditorInitMovingObjectList();
        EditorInitCarPathData();
        EditorInitCoursePool();

        EditorStartCar();
    }

    private void EditorInitMovingObjectList()
    {
        GameObject[] carObject = new GameObject[3];
        carObject[0] = AssetBundleEx.Load<GameObject>("[Prefabs]/[Others]/Prb_Car02");
        carObject[1] = AssetBundleEx.Load<GameObject>("[Prefabs]/[Others]/Prb_Car03");
        carObject[2] = AssetBundleEx.Load<GameObject>("[Prefabs]/[Others]/Prb_Car04");

        m_carGroup = new Transform[EDITOR_MAX_CAR];
        for(int i = 0; i < EDITOR_MAX_CAR; i++)
        {
            m_carGroup[i] = Instantiate(carObject[Random.Range(0, carObject.Length)].transform, Vector3.one * 9999.0f, Quaternion.identity) as Transform;

            m_carGroup[i].parent = transform.FindChild("S01_Group");
            m_carGroup[i].gameObject.name = string.Format("Car_{0:D2}", i);
            m_carGroup[i].localScale = Vector3.one * 360.0f;

            Util.SetGameObjectLayer(m_carGroup[i].gameObject, LayerMask.NameToLayer("Background"));
        }
    }

    private void EditorInitCarPathData()
    {
        m_listCarPathData = new List<CarPathData>();
        Transform center = transform.FindChild("CarPath_Group");

        for(int i = 0; i < center.childCount; i++)
        {
            Transform child = center.GetChild(i);
            if(child.name.Contains("CarPath") && child.gameObject.activeSelf)
                m_listCarPathData.Add(child.GetComponent<CarPathData>());
        }
    }

    private void EditorInitCoursePool()
    {
        List<int> indexList = new List<int>();
        m_dicCarPathPool = new Dictionary<int, Queue<Transform>>();
        for(int i = 0; i < m_listCarPathData.Count; i++)
        {
            m_dicCarPathPool.Add(i, new Queue<Transform>());
            if(m_listCarPathData[i].m_isStartingPoint)
                indexList.Add(i);
        }

        for(int i = 0; i < m_carGroup.Length; i++)
        {
            int index = indexList[Random.Range(0, indexList.Count)];
            m_dicCarPathPool[index].Enqueue(m_carGroup[i]);
        }
    }

    private void EditorStartCar()
    {
        for(int i = 0; i < m_dicCarPathPool.Keys.Count; i++)
        {
            if(m_dicCarPathPool[i].Count > 0)
            {
                Transform carTF = m_dicCarPathPool[i].Dequeue();

                if(null == carTF.gameObject.GetComponent<CarDirection>())
                    carTF.gameObject.AddComponent<CarDirection>().Init(this, i, m_listCarPathData[i]);
                else
                    carTF.gameObject.GetComponent<CarDirection>().Init(this, i, m_listCarPathData[i]);
            }
        }

        Invoke("EditorStartCar", Random.Range(5, 8));
    }

#endif
    #endregion
}
