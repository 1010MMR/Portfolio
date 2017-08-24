using UnityEngine;
using System.Collections;

[System.Serializable]
public struct LobbyPanelPath {
    public string path;
    public LobbyManager.eUIType type;
    public bool isLoadOnStart;
}

public class LobbyManager : MonoBehaviour {
    private static LobbyManager instance = null;
    public static LobbyManager Instance {
        get {
            return instance;
        }
    }

    public GameObject[] rootArray = null;
    public enum eRootType {
        Type_None = -1,

        Type_Root,
        Type_Object,
        Type_Back,

        Type_End,
    }

    public Camera[] cameraArray = null;
    public enum eCameraType {
        Type_None = -1,

        Type_Object,
        Type_Back,
        Type_Front,
        Type_Effect,

        Type_End,
    }

    public LobbyPanelPath[] lobbyPanelPathArray = null;
    public enum eUIType {
        Type_None = -1,

        Type_Lobby,
        Type_MessageBox,
        Type_Hero,
        Type_Skill,
        Type_Weapon,
        Type_Gold,
        Type_Gem,
        Type_Lobby_Button,
        Type_Waiting,

        Type_End,
    }

    void Awake() {
        instance = this;
    }

    void Start() {
        if( ClientManager.Instance != null )
            ClientManager.Instance.SceneType = eSceneType.Type_Lobby;

        StartCoroutine("Preload");
    }

    void LateUpdate() {
#if UNITY_ANDROID
        if( Input.GetKeyUp(KeyCode.Escape) ) {
            UIMessageBox.ButtonCB quitCB = new UIMessageBox.ButtonCB(OnAndroidBack);
            OpenMessageBox(SupportString.GetString(100001), eUIMessageBoxButtonType.Type_Ok_Cancel, quitCB);
        }
#endif

        Touch();
    }

    #region Preload

    private IEnumerator Preload() {
        // 이펙트
        string[] effectPathArray = { "Effect/Lobby_Touch_Effect" };
        for( int i = 0; i < effectPathArray.Length; i++ ) {
            LoadAssetbundle.LoadPrefabCB loadEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadPreloadEffectCompleteCB);
            PrefabManager.Instance.LoadPrefab(effectPathArray[i], System.Guid.NewGuid(), loadEffectPrefabCB);

            yield return null;
        }

        StartCoroutine("InitUI");
    }

    private void LoadPreloadEffectCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = rootArray[(int)eRootType.Type_Object].transform;
            createObj.transform.localScale = gameObj.transform.localScale;
            createObj.transform.localPosition = Vector3.one * 9999.0f;

            SkeletonEffect effect = createObj.GetComponent<SkeletonEffect>();
            if( effect != null )
                effect.Init(null);
        }
    }

    #endregion

    #region UI

    [HideInInspector]
    public UILobby uiLobby = null;
    [HideInInspector]
    public UIMessageBox uiMessageBox = null;
    [HideInInspector]
    public UIWaiting uiWaiting = null;

    [HideInInspector]
    public UIHero uiHero = null;
    [HideInInspector]
    public UISkill uiSkill = null;
    [HideInInspector]
    public UIBullet uiBullet = null;
    [HideInInspector]
    public UIGoldShop uiGoldShop = null;
    [HideInInspector]
    public UICashShop uiCashShop = null;

    [HideInInspector]
    public UILobbyButton uiLobbyButton = null;

    private eUILobbyTab selectUlType = eUILobbyTab.Type_None;
    private LobbyState[] lobbyTabGroup = null;

    private IEnumerator InitUI() {
        lobbyTabGroup = new LobbyState[(int)eUILobbyTab.Type_End];

        LoadAssetbundle.LoadPrefabCB loadUICompleteCB = null;
        for( int i = 0; i < lobbyPanelPathArray.Length; i++ ) {
            if( lobbyPanelPathArray[i].isLoadOnStart ) {
                loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
                PrefabManager.Instance.LoadPrefab(lobbyPanelPathArray[i].path, System.Guid.NewGuid(), loadUICompleteCB, lobbyPanelPathArray[i].type);
            }

            yield return null;
        }

        if( uiLobby != null )
            uiLobby.OpenFrame();
    }

    private void LoadUICompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = rootArray[(int)eRootType.Type_Back].transform;
            createObj.transform.localPosition = Vector3.zero;
            createObj.transform.localScale = gameObj.transform.localScale;

            switch( (eUIType)param[0] ) {
                case eUIType.Type_Lobby:
                    uiLobby = createObj.GetComponent<UILobby>();
                    break;

                case eUIType.Type_Lobby_Button:
                    uiLobbyButton = createObj.GetComponent<UILobbyButton>();
                    break;

                case eUIType.Type_MessageBox:
                    uiMessageBox = createObj.GetComponent<UIMessageBox>();
                    break;

                case eUIType.Type_Waiting:
                    uiWaiting = createObj.GetComponent<UIWaiting>();
                    break;

                case eUIType.Type_Hero:
                    uiHero = createObj.GetComponent<UIHero>();
                    if( lobbyTabGroup != null )
                        lobbyTabGroup[(int)eUILobbyTab.Type_Hero] = uiHero;
                    break;

                case eUIType.Type_Skill:
                    uiSkill = createObj.GetComponent<UISkill>();
                    if( lobbyTabGroup != null )
                        lobbyTabGroup[(int)eUILobbyTab.Type_Skill] = uiSkill;
                    break;

                case eUIType.Type_Weapon:
                    uiBullet = createObj.GetComponent<UIBullet>();
                    if( lobbyTabGroup != null )
                        lobbyTabGroup[(int)eUILobbyTab.Type_Weapon] = uiBullet;
                    break;

                case eUIType.Type_Gold:
                    uiGoldShop = createObj.GetComponent<UIGoldShop>();
                    if( lobbyTabGroup != null )
                        lobbyTabGroup[(int)eUILobbyTab.Type_Gold] = uiGoldShop;
                    break;

                case eUIType.Type_Gem:
                    uiCashShop = createObj.GetComponent<UICashShop>();
                    if( lobbyTabGroup != null )
                        lobbyTabGroup[(int)eUILobbyTab.Type_Gem] = uiCashShop;
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

    public void SetLobbyTab(eUILobbyTab type) {
        selectUlType = type;
        for( int i = 0; i < lobbyTabGroup.Length; i++ ) {
            if( lobbyTabGroup[i] != null ) {
                if( i.Equals((int)type) )
                    lobbyTabGroup[i].OpenFrame();
                else
                    lobbyTabGroup[i].CloseFrame();
            }
        }
    }

    #endregion

    #region Touch

    private bool isTouchDown = false;

    private void Touch() {
        if( Input.GetMouseButtonDown(0) ) {
            if( isTouchDown )
                return;

            Camera frontCamera = cameraArray[(int)eCameraType.Type_Front];
            Ray ray = frontCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hitArray = Physics.RaycastAll(ray);

            for( int i = 0; i < hitArray.Length; i++ ) {
                if( hitArray[i].collider != null && hitArray[i].collider.gameObject != null ) {
                    if( hitArray[i].collider.gameObject.CompareTag("UI") )
                        return;
                }
            }

            Vector3 screenPoint = frontCamera.ScreenToWorldPoint(Input.mousePosition);
            SetTouchEffect(screenPoint);

            isTouchDown = true;
        }

        if( Input.GetMouseButtonUp(0) ) {
            if( isTouchDown == false )
                return;

            isTouchDown = false;
        }
    }

    #endregion

    #region Effect

    readonly private Vector3 GOLD_GET_POSITION = new Vector3(229.0f, 335.0f, 0);
    readonly private Vector3 GEM_GET_POSITION = new Vector3(58.3f, 335.0f, 0);
    readonly private Vector3 SKILL_GET_POSITION = new Vector3(398.4f, 335.0f, 0);

    public void SetGemEffect() {
        SkeletonEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(eEffectType.Type_Add_Gem, out effect) )
            effect.Init(GEM_GET_POSITION, true);
        else {
            LoadAssetbundle.LoadPrefabCB loadEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadEffectCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Get_Gem_Effect", System.Guid.NewGuid(), loadEffectPrefabCB, GEM_GET_POSITION, true);
        }
    }

    public void SetGoldEffect() {
        SkeletonEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(eEffectType.Type_Add_Gold, out effect) )
            effect.Init(GOLD_GET_POSITION, true);
        else {
            LoadAssetbundle.LoadPrefabCB loadEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadEffectCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Get_Gold_Effect", System.Guid.NewGuid(), loadEffectPrefabCB, GOLD_GET_POSITION, true);
        }
    }

    public void SetSkillEffect() {
        SkeletonEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(eEffectType.Type_Add_Skill_Point, out effect) )
            effect.Init(SKILL_GET_POSITION, true);
        else {
            LoadAssetbundle.LoadPrefabCB loadEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadEffectCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Get_Skill_Effect", System.Guid.NewGuid(), loadEffectPrefabCB, SKILL_GET_POSITION, true);
        }
    }

    private void SetTouchEffect(Vector3 touchPos) {
        SkeletonEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(eEffectType.Type_Touch, out effect) )
            effect.Init(touchPos);
        else {
            LoadAssetbundle.LoadPrefabCB loadEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadEffectCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Lobby_Touch_Effect", System.Guid.NewGuid(), loadEffectPrefabCB, touchPos, false);
        }
    }

    private void LoadEffectCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = rootArray[(int)eRootType.Type_Object].transform;
            createObj.transform.localScale = gameObj.transform.localScale;

            SkeletonEffect effect = createObj.GetComponent<SkeletonEffect>();
            if( effect != null )
                effect.Init((Vector3)param[0], (bool)param[1]);
        }
    }

    #endregion

    #region Response

    public void ResponseBuy(ShopTable.TableRow table) {
        lobbyTabGroup[(int)selectUlType].ResponseBuy(table);
    }

    #endregion

    #region Callback

    private void OnAndroidBack(params object[] param) {
        if( AdManager.Instance != null )
            AdManager.Instance.OpenGoogleAds();
    }

    #endregion

    void OnDestroy() {
        instance = null;

        rootArray = null;
        cameraArray = null;
        lobbyPanelPathArray = null;
        
        uiLobby = null;
        uiLobbyButton = null;
        uiMessageBox = null;
        uiHero = null;
        uiSkill = null;
        uiBullet = null;
        uiGoldShop = null;
        uiCashShop = null;
        
        uiLobbyButton = null;
        lobbyTabGroup = null;
    }
}
