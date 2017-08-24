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

    public GameObject rootPanel = null;
    public LobbyPanelPath[] lobbyPanelPathArray = null;

    public enum eUIType {
        Type_None = -1,

        Type_Lobby,
        Type_MessageBox,
        Type_GameReady,
        Type_Hero,
        Type_Tower,
        Type_Waiting,
        Type_Base,
        Type_Option,
        Type_Shop,

        Type_End,
    }

    void Awake() {
        instance = this;
    }

    void Start() {
        if( ClientManager.Instance != null )
            ClientManager.Instance.SceneType = eSceneType.Type_Lobby;

        StartCoroutine("InitUI");
    }

    void LateUpdate() {
#if UNITY_ANDROID
        if( Input.GetKeyUp(KeyCode.Escape) ) {
            UIMessageBox.ButtonCB quitCB = new UIMessageBox.ButtonCB(SupportUtil.ApplicationQuit);
            OpenMessageBox(SupportString.GetString(3012), eUIMessageBoxButtonType.Type_Ok_Cancel, quitCB);
        }
#endif
    }

    #region UI

    [HideInInspector]
    public UILobby uiLobby = null;
    [HideInInspector]
    public UIMessageBox uiMessageBox = null;
    [HideInInspector]
    public UIGameReady uiGameReady = null;
    [HideInInspector]
    public UIHeroList uiHeroList = null;
    [HideInInspector]
    public UITowerList uiTowerList = null;
    [HideInInspector]
    public UIWaiting uiWaiting = null;
    [HideInInspector]
    public UIBaseInfo uiBaseInfo = null;
    [HideInInspector]
    public UIGameOption uiGameOption = null;
    [HideInInspector]
    public UIShop uiShop = null;

    private IEnumerator InitUI() {
        LoadAssetbundle.LoadPrefabCB loadUICompleteCB = null;
        for( int i = 0; i < lobbyPanelPathArray.Length; i++ ) {
            if( lobbyPanelPathArray[i].isLoadOnStart ) {
                loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
                PrefabManager.Instance.LoadPrefab(lobbyPanelPathArray[i].path, System.Guid.NewGuid(), loadUICompleteCB, lobbyPanelPathArray[i].type);
            }

            yield return null;
        }
    }

    private void LoadUICompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = rootPanel.transform;
            createObj.transform.localPosition = Vector3.zero;
            createObj.transform.localScale = gameObj.transform.localScale;

            switch( (eUIType)param[0] ) {
                case eUIType.Type_Lobby:
                    uiLobby = createObj.GetComponent<UILobby>();
                    break;

                case eUIType.Type_MessageBox:
                    uiMessageBox = createObj.GetComponent<UIMessageBox>();
                    break;

                case eUIType.Type_GameReady:
                    uiGameReady = createObj.GetComponent<UIGameReady>();
                    if( param.Length > 1 && (bool)param[1] )
                        uiGameReady.OpenFrame();
                    break;

                case eUIType.Type_Hero:
                    uiHeroList = createObj.GetComponent<UIHeroList>();
                    if( param.Length > 1 && (bool)param[1] )
                        uiHeroList.OpenFrame();
                    break;

                case eUIType.Type_Tower:
                    uiTowerList = createObj.GetComponent<UITowerList>();
                    if( param.Length > 1 && (bool)param[1] )
                        uiTowerList.OpenFrame();
                    break;

                case eUIType.Type_Waiting:
                    uiWaiting = createObj.GetComponent<UIWaiting>();
                    break;

                case eUIType.Type_Base:
                    uiBaseInfo = createObj.GetComponent<UIBaseInfo>();
                    if( param.Length > 1 && (bool)param[1] )
                        uiBaseInfo.OpenFrame();
                    break;

                case eUIType.Type_Option:
                    uiGameOption = createObj.GetComponent<UIGameOption>();
                    if( param.Length > 1 && (bool)param[1] )
                        uiGameOption.OpenFrame();
                    break;

                case eUIType.Type_Shop:
                    uiShop = createObj.GetComponent<UIShop>();
                    if( param.Length > 1 && (bool)param[1] )
                        uiShop.OpenFrame();
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

    public void OpenGameReady() {
        if( uiGameReady != null )
            uiGameReady.OpenFrame();
        else {
            int index = GetLobbyPanelPathIndex(eUIType.Type_GameReady);

            LoadAssetbundle.LoadPrefabCB loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
            PrefabManager.Instance.LoadPrefab(lobbyPanelPathArray[index].path,
                System.Guid.NewGuid(), loadUICompleteCB, eUIType.Type_GameReady, true);
        }
    }

    public void OpenHeroList() {
        if( uiHeroList != null )
            uiHeroList.OpenFrame();
        else {
            int index = GetLobbyPanelPathIndex(eUIType.Type_Hero);

            LoadAssetbundle.LoadPrefabCB loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
            PrefabManager.Instance.LoadPrefab(lobbyPanelPathArray[index].path,
                System.Guid.NewGuid(), loadUICompleteCB, eUIType.Type_Hero, true);
        }
    }

    public void OpenTowerList() {
        if( uiTowerList != null )
            uiTowerList.OpenFrame();
        else {
            int index = GetLobbyPanelPathIndex(eUIType.Type_Tower);

            LoadAssetbundle.LoadPrefabCB loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
            PrefabManager.Instance.LoadPrefab(lobbyPanelPathArray[index].path,
                System.Guid.NewGuid(), loadUICompleteCB, eUIType.Type_Tower, true);
        }
    }

    public void OpenBaseInfo() {
        if( uiBaseInfo != null )
            uiBaseInfo.OpenFrame();
        else {
            int index = GetLobbyPanelPathIndex(eUIType.Type_Base);

            LoadAssetbundle.LoadPrefabCB loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
            PrefabManager.Instance.LoadPrefab(lobbyPanelPathArray[index].path,
                System.Guid.NewGuid(), loadUICompleteCB, eUIType.Type_Base, true);
        }
    }

    public void OpenGameOption() {
        if( uiGameOption != null )
            uiGameOption.OpenFrame();
        else {
            int index = GetLobbyPanelPathIndex(eUIType.Type_Option);

            LoadAssetbundle.LoadPrefabCB loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
            PrefabManager.Instance.LoadPrefab(lobbyPanelPathArray[index].path,
                System.Guid.NewGuid(), loadUICompleteCB, eUIType.Type_Option, true);
        }
    }

    public void OpenShop() {
        if( uiShop != null )
            uiShop.OpenFrame();
        else {
            int index = GetLobbyPanelPathIndex(eUIType.Type_Shop);

            LoadAssetbundle.LoadPrefabCB loadUICompleteCB = new LoadAssetbundle.LoadPrefabCB(LoadUICompleteCB);
            PrefabManager.Instance.LoadPrefab(lobbyPanelPathArray[index].path,
                System.Guid.NewGuid(), loadUICompleteCB, eUIType.Type_Shop, true);
        }
    }

    private int GetLobbyPanelPathIndex(eUIType type) {
        for( int i = 0; i < lobbyPanelPathArray.Length; i++ ) {
            if( lobbyPanelPathArray[i].type.Equals(type) )
                return i;
        }

        return 0;
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

            createObj.transform.parent = rootPanel.transform;
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

    void OnDestroy() {
        instance = null;
        rootPanel = null;

        lobbyPanelPathArray = null;
        
        uiLobby = null;
        uiMessageBox = null;
        uiGameReady = null;
        uiHeroList = null;
        uiTowerList = null;
        uiWaiting = null;
        uiBaseInfo = null;
        uiGameOption = null;
        uiShop = null;
    }
}
