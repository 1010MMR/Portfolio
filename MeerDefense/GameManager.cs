using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#region Buff

public enum eBuffType {
    Type_None = -1,

    Type_Attack,
    Type_Attack_Speed,
    Type_Attack_Range,
    Type_Move_Speed,
    Type_Gain_Gold,
    Type_Damage,
    Type_Stun,

    Type_End,
}

public class BuffParam {
    public eDefenseType defenseType = eDefenseType.Type_None;
    public eBuffType buffType = eBuffType.Type_None;

    public float buffValue = 0;
    public float buffTime = 0;

    public BuffParam(eDefenseType defenseType, float value, float time, eBuffType buffType) {
        this.defenseType = defenseType;
        this.buffValue = value;
        this.buffTime = time;
        this.buffType = buffType;
    }
}

#endregion

#region Damage

public enum eDefenseType {
    Type_None = -1,

    Type_Physical,
    Type_Laser,
    Type_Chemical,
    Type_Explosion,
    Type_Generator,

    Type_End,
}

public enum eAttackType {
    Type_None = -1,

    Type_Normal,
    Type_Piercing,
    Type_Explosion,
    Type_Snipe,
    Type_Slow,
    Type_Area,
    Type_Generator,

    Type_End,
}

public class DamageParam {
    public int damage = 0;
    public float time = 0;
    public int num = 0;
    public eDefenseType type = eDefenseType.Type_Physical;

    public DamageParam(int damage, float time, int num, eDefenseType type) {
        this.damage = damage;
        this.time = time;
        this.num = num;
        this.type = type;
    }

    public int GetDamageEffectID {
        get {
            switch( type ) {
                case eDefenseType.Type_Laser:
                    return 3002;
                default:
                    return 3001;
            }
        }
    }

    ~DamageParam() {
    }
}

#endregion

[System.Serializable]
public class GameStatus {
    public int goldValue = 0;
    public int waveCount = 0;
    public int savePoint = 0;

    public int getRewardCount = 0;

    public TowerPlacedInfo[] placedTowerArray = null;
    public HeroPlacedInfo[] placedHeroArray = null;

    public bool isGameStart = false;
    public bool isGameClear = false;

    public GameStatus() {
        this.goldValue = GameManager.START_GOLD_VALUE;
        this.waveCount = 1;
        this.savePoint = 1;

        this.getRewardCount = 0;

        this.placedTowerArray = new TowerPlacedInfo[Define.MAX_STAGE_TOWER_PLACE_COUNT];
        this.placedHeroArray = new HeroPlacedInfo[Define.MAX_STAGE_HERO_PLACE_COUNT];

        this.isGameStart = false;
        this.isGameClear = false;
    }

    public GameStatus(int goldValue, int waveCount, TowerPlacedInfo[] placedTowerArray, HeroPlacedInfo[] placedHeroArray) {
        this.goldValue = goldValue;

        this.waveCount = waveCount;
        this.savePoint = waveCount;

        StageWaveTable.TableRow stageWaveTable = null;
        if( StageWaveTable.Instance != null && 
            StageWaveTable.Instance.FindTable(waveCount, out stageWaveTable) )
            SetWaveCount(stageWaveTable);

        this.getRewardCount = 0;

        this.placedTowerArray = placedTowerArray;
        this.placedHeroArray = placedHeroArray;

        this.isGameStart = false;
        this.isGameClear = false;
    }

    public GameStatus(GameStatus status) {
        this.goldValue = status.goldValue;
        this.waveCount = status.waveCount;
        this.savePoint = status.savePoint;

        this.getRewardCount = 0;

        this.placedTowerArray = status.placedTowerArray;
        this.placedHeroArray = status.placedHeroArray;

        this.isGameStart = status.isGameStart;
        this.isGameClear = status.isGameClear;
    }

    #region Gold

    public void SetGameGold(int value) {
        goldValue = value;
    }

    public void AddGameGold(int value) {
        goldValue = Mathf.Clamp(goldValue + value, 0, int.MaxValue);
    }

    public bool CompareGameGold(int compareValue) {
        return goldValue >= compareValue;
    }

    #endregion

    #region Wave

    public void SetWaveCount(StageWaveTable.TableRow table) {
        waveCount = table.id;
        savePoint = table.pointType.Equals(StageWaveTable.ePointType.Type_SavePoint) ?
            table.id : savePoint;
    }

    public void SetSavePoint(int point) {
        savePoint = point;
    }

    public void ConvertWavePointToSavePoint() {
        waveCount = savePoint;
    }

    #endregion

    #region Reward_Count

    public void AddRewardCount(int value) {
        getRewardCount += value;
    }

    #endregion

    #region Placed_Tower

    public bool FindPlacedTower(int index, out TowerPlacedInfo towerInfo) {
        towerInfo = null;

        if( placedTowerArray != null ) {
            if( placedTowerArray.Length > index && placedTowerArray[index] != null ) {
                towerInfo = placedTowerArray[index];
                return true;
            }
        }

        return false;
    }

    public void AddPlacedTower(int index, TowerPlacedInfo towerInfo) {
        if( placedTowerArray != null )
            placedTowerArray[index] = towerInfo;
    }

    public void RemovePlacedTower(int index) {
        if( placedTowerArray != null )
            placedTowerArray[index] = null;
    }

    public bool ExistPlacedTower() {
        if( placedTowerArray == null )
            return false;
        else {
            for( int i = 0; i < placedTowerArray.Length; i++ ) {
                if( placedTowerArray[i] != null )
                    return true;
            }

            return false;
        }
    }

    #endregion

    #region Placed_Hero

    public bool GetPlacedHeroList(out List<HeroPlacedInfo> infoList) {
        infoList = new List<HeroPlacedInfo>();

        if( placedHeroArray != null ) {
            for( int i = 0; i < placedHeroArray.Length; i++ ) {
                if( placedHeroArray[i] != null ) {
                    if( ClientManager.Instance.ExistsHeroBaseInfo(placedHeroArray[i].id) )
                        infoList.Add(placedHeroArray[i]);
                }
            }
        }

        return !infoList.Count.Equals(0);
    }

    public bool FindPlacedHero(int id, out HeroPlacedInfo heroInfo) {
        heroInfo = null;

        if( placedHeroArray != null ) {
            if( placedHeroArray.Length > id && placedTowerArray[id] != null ) {
                heroInfo = placedHeroArray[id];
                return true;
            }
        }

        return false;
    }

    public void AddPlacedHero(int id, HeroPlacedInfo heroInfo) {
        if( placedHeroArray != null )
            placedHeroArray[id] = heroInfo;
    }

    #endregion

    #region Game_Over

    public void GameOver() {
        if( isGameStart ) {
            isGameStart = false;

            ConvertWavePointToSavePoint();
            GameManager.Instance.SaveGameStatus();

            if( GameManager.Instance.uiGameResult != null )
                GameManager.Instance.uiGameResult.OpenFrame(StageWaveTable.ePointType.Type_None);
        }
    }

    #endregion

    ~GameStatus() {
        placedTowerArray = null;
        placedHeroArray = null;
    }
}

public class GameManager : MonoBehaviour {
    public const int MAX_FLOOR_INDEX = 3;
    public const int SAVE_POINT_FACTOR = 10;
    public const int START_GOLD_VALUE = 250;

    private static GameManager instance = null;
    public static GameManager Instance {
        get {
            return instance;
        }
    }

    public enum eRootPanelType {
        Type_None = -1,

        Type_Root,
        Type_BackGround,
        Type_Tower,
        Type_InGame,
        Type_Effect,

        Type_End,
    }

    public GameObject[] rootPanelArray = null;
    private GameStatus gameStatus = null;

    void Awake() {
        instance = this;
    }

    void Start() {
        if( ClientManager.Instance != null ) {
            ClientManager.Instance.SceneType = eSceneType.Type_InGame;

            if( ClientManager.Instance.GetGameStatus != null )
                gameStatus = new GameStatus(ClientManager.Instance.GetGameStatus);
            ClientManager.Instance.ReleaseGameStatus();
        }

        if( OptionManager.Instance != null && OptionManager.Instance.GetGameOption != null &&
            TimeManager.Instance != null )
            TimeManager.Instance.SetGameTimeScale(OptionManager.Instance.GetGameOption.gameSpeed);

        StartCoroutine("Preload");
    }

    void LateUpdate() {
#if UNITY_ANDROID
        if( Input.GetKeyUp(KeyCode.Escape) ) {
            OpenMessageBox(SupportString.GetString(1014), eUIMessageBoxButtonType.Type_Ok_Cancel,
                new UIMessageBox.ButtonCB(HomeButtonOkCB));
        }
#endif
    }

    #region UI

    private bool isLoadUIComplete = false;
    public bool CheckLoadUIComplete {
        get {
            return isLoadUIComplete;
        }
    }

    [HideInInspector]
    public UIInGame uiInGame = null;
    [HideInInspector]
    public UITowerPlacement uiTowerPlacement = null;
    [HideInInspector]
    public UITowerBuy uiTowerBuy = null;
    [HideInInspector]
    public UIMessageBox uiMessageBox = null;
    [HideInInspector]
    public UIGameResult uiGameResult = null;
    [HideInInspector]
    public UIWaiting uiWaiting = null;

    private string[] uiPathArray = { "UI/UI_InGame/UI_InGame", "UI/UI_Tower_Placement/UI_Tower_Placement", "UI/UI_Tower_Buy/UI_Tower_Buy",
                                   "UI/UI_MessageBox/UI_MessageBox", "UI/UI_Game_Result/UI_Game_Result", "UI/UI_Common/UI_Waiting" };
    private enum eUIType {
        Type_None = -1,

        Type_InGame,
        Type_TowerPlacement,
        Type_TowerBuy,
        Type_MessageBox,
        Type_GameResult,
        Type_Waiting,

        Type_End,
    }

    private IEnumerator InitUI() {
        LoadAssetbundle.LoadPrefabCB loadUICompleteCB = null;
        for( int i = 0; i < uiPathArray.Length; i++ ) {
            loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
            PrefabManager.Instance.LoadPrefab(uiPathArray[i], System.Guid.NewGuid(), loadUICompleteCB, i);

            yield return null;
        }

        isLoadUIComplete = true;
    }

    private void LoadUICompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = rootPanelArray[(int)eRootPanelType.Type_Root].transform;
            createObj.transform.localPosition = Vector3.zero;
            createObj.transform.localScale = gameObj.transform.localScale;

            switch( (eUIType)param[0] ) {
                case eUIType.Type_InGame:
                    uiInGame = createObj.GetComponent<UIInGame>();
                    break;

                case eUIType.Type_TowerBuy:
                    uiTowerBuy = createObj.GetComponent<UITowerBuy>();
                    break;

                case eUIType.Type_TowerPlacement:
                    uiTowerPlacement = createObj.GetComponent<UITowerPlacement>();
                    break;

                case eUIType.Type_MessageBox:
                    uiMessageBox = createObj.GetComponent<UIMessageBox>();
                    break;

                case eUIType.Type_GameResult:
                    uiGameResult = createObj.GetComponent<UIGameResult>();
                    break;

                case eUIType.Type_Waiting:
                    uiWaiting = createObj.GetComponent<UIWaiting>();
                    break;
            }
        }
    }

    public void OpenMessageBox(string message, eUIMessageBoxButtonType type, UIMessageBox.ButtonCB onButtonCB, params object[] param) {
        if( uiMessageBox != null )
            uiMessageBox.OpenFrame(message, type, onButtonCB, param);
    }

    public void SetWaiting(bool isSwitch) {
        if( uiWaiting != null )
            uiWaiting.SetActive(isSwitch);
    }

    public void OpenEndingScene() {
        SetCameraFade(new SupportUtil.CameraFadeCB(LoadEndingSceneCB));
    }

    private void LoadEndingSceneCB(params object[] param) {
        if( ClientManager.Instance != null )
            ClientManager.Instance.LoadLevel(eSceneType.Type_Ending);
    }

    #endregion

    #region Manage_Enemy

    private List<Enemy> enemyList = null;

    public void MakeEnemy(int level, EnemyTable.TableRow table) {
        Enemy enemy = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEnemyFromPool(table.id, out enemy) )
            enemy.Init(level, table);
        else {
            LoadAssetbundle.LoadPrefabCB loadPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadEnemyCompleteCB);
            PrefabManager.Instance.LoadPrefab(table.path, System.Guid.NewGuid(), loadPrefabCB, level, table);
        }
    }

    private void LoadEnemyCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = rootPanelArray[(int)eRootPanelType.Type_InGame].transform;
            createObj.transform.localScale = gameObj.transform.localScale;

            Enemy enemy = createObj.GetComponent<Enemy>();
            if( enemy != null ) {
                int level = (int)param[0];
                EnemyTable.TableRow table = (EnemyTable.TableRow)param[1];

                enemy.Init(level, table);
                AddEnemyList(enemy);
            }
        }
    }

    public void AddEnemyList(Enemy enemy) {
        if( enemyList != null && enemyList.Contains(enemy) == false )
            enemyList.Add(enemy);
    }

    public void RemoveEnemyList(Enemy enemy) {
        if( enemyList != null && enemyList.Contains(enemy) )
            enemyList.Remove(enemy);
    }

    public bool CheckEnemyExist(Enemy enemy) {
        if( enemy != null )
            return enemyList.Contains(enemy);
        else
            return false;
    }

    public void ClearEnemyList() {
        if( enemyList != null )
            enemyList.Clear();
        else
            enemyList = new List<Enemy>();
    }

    public bool GetEnemyList(out List<Enemy> list) {
        list = new List<Enemy>();

        if( enemyList != null ) {
            for( int i = 0; i < enemyList.Count; i++ ) {
                if( enemyList[i].GetStatus.GetHP.Equals(0) == false )
                    list.Add(enemyList[i]);
            }
        }

        return !list.Count.Equals(0);
    }

    public bool GetEnemyList(int floorIndex, out List<Enemy> list) {
        list = new List<Enemy>();

        if( enemyList != null ) {
            for( int i = 0; i < enemyList.Count; i++ ) {
                if( enemyList[i].GetStatus.GetFloorIndex.Equals(floorIndex) &&
                    enemyList[i].GetStatus.GetHP.Equals(0) == false )
                    list.Add(enemyList[i]);
            }
        }

        return !list.Count.Equals(0);
    }

    public bool CheckEnemyListClear() {
        return enemyList != null ? enemyList.Count.Equals(0) : true;
    }

    #endregion

    #region Point

    private Vector3[] startPointArray = { new Vector3(-710.0f, -143.0f, 0),
                                            new Vector3(710.0f, 32.0f, 0),
                                            new Vector3(-710.0f, 206.0f, 0) };
    private Vector3[] endPointArray = { new Vector3(710.0f, -143.0f, 0),
                                          new Vector3(-710.0f, 32.0f, 0),
                                          new Vector3(710.0f, 206.0f, 0) };

    public Vector3 GetStartPoint(int floorIndex) {
        return startPointArray[Mathf.Clamp(floorIndex, 0, startPointArray.Length - 1)];
    }

    public Vector3 GetEndPoint(int floorIndex) {
        return endPointArray[Mathf.Clamp(floorIndex, 0, endPointArray.Length - 1)];
    }

    #endregion

    #region Game_Status

    public GameStatus GetGameStatus {
        get {
            return gameStatus;
        }
    }

    public void SetGameGold(int value) {
        if( gameStatus != null )
            gameStatus.SetGameGold(value);

        if( uiInGame != null )
            uiInGame.UpdateGoldValue();
    }

    public void AddGameGold(int value) {
        if( gameStatus != null && gameStatus.isGameStart ) {
            gameStatus.AddGameGold(value);

            if( uiInGame != null )
                uiInGame.UpdateGoldValue();
        }
    }

    public void SaveGameStatus() {
        if( gameStatus != null )
            SupportPlayerPref.SetPlayerPref(Define.GAME_STATUS, gameStatus);
    }

    #endregion

    #region Camera_Fade

    public void SetCameraFade(SupportUtil.CameraFadeCB fadeCB, params object[] param) {
        LoadAssetbundle.LoadPrefabCB loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadCameraFadeCompleteCB);
        PrefabManager.Instance.LoadPrefab("Object/CameraFade", System.Guid.NewGuid(), loadUICompleteCB, fadeCB, param);
    }

    private void LoadCameraFadeCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = rootPanelArray[(int)eRootPanelType.Type_Root].transform;
            createObj.transform.localPosition = Vector3.zero;
            createObj.transform.localScale = Vector3.one;

            CameraFade cameraFade = createObj.GetComponent<CameraFade>();
            if( cameraFade != null ) {
                SupportUtil.CameraFadeCB fadeCB = (SupportUtil.CameraFadeCB)param[0];
                object[] objectArray = (object[])param[1];

                System.Action action = delegate() {
                    fadeCB(objectArray);
                };

                cameraFade.StartAlphaFade(Color.black, true, 1.5f, action);
            }
        }
    }

    #endregion

    #region Preload

    private IEnumerator Preload() {
        // 이펙트
        List<EffectTable.TableRow> effectTable = null;
        if( EffectTable.Instance != null && EffectTable.Instance.GetTable(out effectTable) ) {
            for( int i = 0; i < effectTable.Count; i++ ) {
                LoadAssetbundle.LoadPrefabCB loadDamageEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadDamageEffectCompleteCB);
                PrefabManager.Instance.LoadPrefab(effectTable[i].effectPath, System.Guid.NewGuid(), loadDamageEffectPrefabCB, effectTable[i]);

                yield return null;
            }
        }

        // 텍스트
        LoadAssetbundle.LoadPrefabCB loadDamageTextPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadDamageTextCompleteCB);
        PrefabManager.Instance.LoadPrefab("Effect/Text_Damage", System.Guid.NewGuid(), loadDamageTextPrefabCB);

        StartCoroutine("InitUI");
    }

    private void LoadDamageEffectCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootPanelArray[(int)GameManager.eRootPanelType.Type_Effect].transform;
            createObj.transform.localScale = gameObj.transform.localScale;
            createObj.transform.localPosition = Vector3.one * 9999.0f;

            SpriteEffect effect = createObj.GetComponent<SpriteEffect>();
            if( effect != null )
                effect.Init(null);
        }
    }

    private void LoadDamageTextCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootPanelArray[(int)GameManager.eRootPanelType.Type_Effect].transform;
            createObj.transform.localScale = gameObj.transform.localScale;
            createObj.transform.localPosition = Vector3.one * 9999.0f;

            UIDamageText damageText = createObj.GetComponent<UIDamageText>();
            if( damageText != null )
                damageText.Init(null, "");
        }
    }

    #endregion

    #region Home

    private void HomeButtonOkCB(params object[] param) {
        if( gameStatus != null ) {
            gameStatus.ConvertWavePointToSavePoint();
            SaveGameStatus();

            ClientManager.Instance.SetGameStatus(gameStatus);
        }

        SetCameraFade(new SupportUtil.CameraFadeCB(HomeFameCB));
    }

    private void HomeFameCB(params object[] param) {
        if( ClientManager.Instance != null )
            ClientManager.Instance.LoadLevel(eSceneType.Type_Lobby);
    }

    #endregion

    void OnDestroy() {
        instance = null;

        uiInGame = null;
        uiTowerPlacement = null;
        uiTowerBuy = null;
        uiPathArray = null;
        uiMessageBox = null;
        uiWaiting = null;

        rootPanelArray = null;
        gameStatus = null;

        enemyList = null;

        startPointArray = null;
        endPointArray = null;
    }
}
