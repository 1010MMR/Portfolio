using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

using LitJson;
using NtUniSdk.Unity3d;

#if USER_SERVER
using NetWork;
#endif

#region SdkOnOffData

public class SdkModuleSwitchData
{
    public SSdkModuleSwitch ngShare = null;
    public SSdkModuleSwitch ePlay = null;
    public SSdkModuleSwitch coupon = null;

    public SGameEventSwitch[] gameEvent = null;

    #region NgShare

    public bool CheckNgShareModuleExists(string channel)
    {
        if (ngShare != null)
            return !ngShare.CheckDisable(channel);
        return false;
    }

    #endregion

    #region Eplay

    public bool CheckEplayModuleExists(string channel)
    {
        if (ePlay != null)
            return !ePlay.CheckDisable(channel);
        return false;
    }

    #endregion

    #region Coupon

    public bool CheckCouponModuleExists(string channel)
    {
        if (coupon != null)
            return !coupon.CheckDisable(channel);
        return false;
    }

    #endregion

    #region GameEvent

    public bool CheckGameEventExists(string channel, int index)
    {
        if (gameEvent != null)
        {
            for (int i = 0; i < gameEvent.Length; i++)
            {
                if (gameEvent[i].eventIdx.Equals(index))
                    return !gameEvent[i].CheckDisable(channel);
            }
        }

        return false;
    }

    #endregion
}

#endregion

#region SdkLoginData

/// <summary>
/// <para>name : SdkLoginData</para>
/// <para>describe : SDK 로그인 후 해당 데이터들을 저장하는 클래스.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class SdkLoginData
{
    public SSdkLoginRet sdkLoginAuthInfo = null;

    public string uid = "";

    public string userNo = "";
    public string accessKey = "";
    public string criptKey = "";

    public SdkLoginData()
    {
    }

    public SdkLoginData(string userNo, string uid, RES_SDK_LOGIN packet)
    {
        this.sdkLoginAuthInfo = packet.sdkLoginRet;

        this.uid = uid;

        this.userNo = userNo;
        this.accessKey = packet.accessKey;
        this.criptKey = packet.criptKey;
    }
}

#endregion

/// <summary>
/// <para>name : SdkManager</para>
/// <para>describe : SDK 매니저 클래스.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public partial class SdkManager : MonoSingleton<SdkManager>
{
    private const string GAME_PARAM_URL = "https://h26.update.netease.com/pl/gameparam";
    private readonly string[] CHANNEL_NAME_ARRAY = { "netease", "uc_platform", "xiaomi_app", "360_assistant", "oppo", "nearme_vivo", 
                                                     "lenovo_open", "huawei", "gionee", "coolpad_sdk", "baidu", "37yyb", "flyme", "4399com", 
                                                     "dangle", "pps", "sogou", "yyxx" };

    private SdkLoginData m_sdkLoginData = null;
    private SdkModuleSwitchData m_sdkModuleSwitchData = null;

    private bool m_isNotification = false;
    private bool m_isSdkLoginFlag = false;
    private bool m_isFirstSdkLogin = false;

    private APP_CHANNEL_TYPE m_appChannelType = APP_CHANNEL_TYPE.TYPE_NETEASE;

    private float m_elapsedTime = 0.0f;

    #region Init

    public void InitSdkManager()
    {
        Util.RemoveDontDestroyObject(gameObject);
        InitUniSdk();
    }

    /// <summary>
    /// <para>name : InitUniSdk</para>
    /// <para>describe : UniSDK 초기화.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private void InitUniSdk()
    {
        CheckLoadComplete = false;

        m_elapsedTime = Time.realtimeSinceStartup;

        InitBase();
        InitPayload();
        InitFloat();
        InitCrashHunter();

        SdkU3d.setCallbackModule("SdkManager");
        SdkU3d.init();
    }

    private void InitBase()
    {
#if SDK_DEBUG
        // SdkU3d.setPropInt(ConstProp.DEBUG_MODE, 1);
        SdkU3d.setPropInt(ConstProp.DEBUG_LOG, 1);
#endif

        SdkU3d.setPropStr(ConstProp.GAME_REGION, "CN");
        SdkU3d.setPropStr(ConstProp.GAME_LANGUAGE, "CN");

        SdkU3d.setPropStr(ConstProp.JF_GAMEID, "h26");
        SdkU3d.setPropStr(ConstProp.JF_OPEN_LOG_URL, "http://applog.game.netease.com:9990/open_log");
        SdkU3d.setPropStr(ConstProp.JF_PAY_LOG_URL, "http://applog.game.netease.com:9990/pay_log");
        SdkU3d.setPropStr(ConstProp.JF_LOG_KEY, "CfW5sdg4xqrmvBh_c0dFx7rInfKlV_iw");
    }

    private void InitPayload()
    {
        SdkU3d.setPropInt(ConstProp.UNISDK_JF_GAS3, 1);
        SdkU3d.setPropStr(ConstProp.UNISDK_JF_GAS3_URL, "https://testgbsdk.nie.netease.com/h26/sdk/");
        SdkU3d.setPropStr(ConstProp.UNISDK_CREATEORDER_URL, "https://testgbsdk.nie.netease.com/h26/sdk/create_order");
        SdkU3d.setPropStr(ConstProp.UNISDK_QUERYORDER_URL, "https://testgbsdk.nie.netease.com/h26/sdk/query_order");
    }

    private void InitFloat()
    {
        SdkU3d.setPropInt("FLOAT_BTN_POS_Y", PluginManager.instance.GetStatusBarHeight());
    }

    public bool CheckLoadComplete
    {
        get;
        set;
    }

    /// <summary>
    /// <para>name : SetSdkLoginAuthInfo</para>
    /// <para>describe : 서버에서 내려받은 SDK Login 패킷을 저장.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void SetSdkLoginAuthData(string userNo, RES_SDK_LOGIN packet)
    {
        m_sdkLoginData = new SdkLoginData(userNo, SdkU3d.getPropStr(ConstProp.UID), packet);

        OldSdkAccountID = m_sdkLoginData.sdkLoginAuthInfo != null ? m_sdkLoginData.sdkLoginAuthInfo.aid : 0;
        SdkGameLoginSucess(m_sdkLoginData.sdkLoginAuthInfo);
    }

    /// <summary>
    /// <para>name : ResetSdkLoginAuthData</para>
    /// <para>describe : SDK 로그인 데이터 초기화.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void ResetSdkLoginAuthData()
    {
        m_sdkLoginData = null;
        OldSdkAccountID = 0;

        WorldManager.instance.m_player.m_userNo = null;
        WorldManager.instance.m_player.m_accessKey = null;
        WorldManager.instance.m_player.m_cryptKey = null;

        ReleaseGMBridge();

        if (StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_TITLE))
        {
            if (CheckHasUserCenter)
                ((State_Title)StateManager.instance.m_curState).UpdateLoginUI(true);
            else
            {
                State_Title sTitle = (State_Title)StateManager.instance.m_curState;
                sTitle.m_guiTitleManager.OnOffLoginButton(false);

                NetworkManager.instance.SendSdkLogin(SdkManager.instance.MakeSSdkLoginParam());
            }
        }
    }

    #endregion

    #region SdkModuleSwitch

    public IEnumerator LoadSdkModuleData()
    {
        WWW www = new WWW(GAME_PARAM_URL);
        yield return www;

        if (www.error == null)
        {
#if UNITY_EDITOR
            Debug.Log(www.text);
#endif

            try {
                SdkModuleSwitchData data = JsonMapper.ToObject<SdkModuleSwitchData>(www.text);
                if (data != null)
                    SetSdkModuleSwitchData(data);
            } catch {
                Util.TRACE(string.Format("GAME_PARAM_URL_PARSE_ERROR."));
                SetSdkModuleSwitchData(null);
            }
        }
    }

    private void SetSdkModuleSwitchData(SdkModuleSwitchData data)
    {
        m_sdkModuleSwitchData = data;
    }

    public bool CheckSdkModuleDataExists()
    {
        return m_sdkModuleSwitchData != null;
    }

    public bool CheckNgShareModuleExists()
    {
#if UNITY_EDITOR
        return true;
#else
        return CheckSdkModuleDataExists() ? m_sdkModuleSwitchData.CheckNgShareModuleExists(SdkU3d.getAppChannel()) : false;
#endif
    }

    public bool CheckEplayModuleExists()
    {
        return CheckSdkModuleDataExists() ? m_sdkModuleSwitchData.CheckEplayModuleExists(SdkU3d.getAppChannel()) : false;
    }

    public bool CheckCouponModuleExists()
    {
        return CheckSdkModuleDataExists() ? m_sdkModuleSwitchData.CheckCouponModuleExists(SdkU3d.getAppChannel()) : false;
    }

    public bool CheckGameEventExists(int index)
    {
        return CheckSdkModuleDataExists() ? m_sdkModuleSwitchData.CheckGameEventExists(SdkU3d.getAppChannel(), index) :
#if UNITY_EDITOR
                true;
#else
                false;
#endif
    }

    #endregion

    #region UniSDK

    #region Login

    public void SdkLogin()
    {
        string strSession = SdkU3d.getPropStr(ConstProp.SESSION);
        Debug.Log("strSession=  " + strSession);

        m_elapsedTime = Time.realtimeSinceStartup;

        if (CheckUniSdkLogin == false || strSession == null || "".Equals(strSession))
            SdkU3d.ntLogin();
        else
        {
            if (StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_TITLE))
            {
                if (CheckHasUserCenter)
                    ((State_Title)StateManager.instance.m_curState).UpdateLoginUI(true);
                else
                    NetworkManager.instance.SendSdkLogin(MakeSSdkLoginParam());
            }
        }
    }

    public void SdkLogout()
    {
        if (SdkU3d.hasFeature(ConstProp.MODE_HAS_LOGOUT) && CheckUniSdkLogin)
            SdkU3d.ntLogout();
    }

    // Step11. 게임 클라이언트에서 sdkUid 및 access_token를 UniSDK에게 전달..
    public void SdkGameLoginSucess(SSdkLoginRet packet)
    {
        string[] parseUserName = packet.username.Split('@');
        SdkU3d.setUserInfo(ConstProp.UID, parseUserName.Length > 0 ? parseUserName[0] : "");

        switch (m_appChannelType)
        {
            case APP_CHANNEL_TYPE.TYPE_UC:
            case APP_CHANNEL_TYPE.TYPE_360:
            case APP_CHANNEL_TYPE.TYPE_COOLPAD:
                SetTokenInfo(packet.message);
                break;
                
            case APP_CHANNEL_TYPE.TYPE_PPS:
                SdkU3d.setUserInfo(ConstProp.USERINFO_HOSTID, WorldManager.instance.m_player.m_hostId);
                break;
        }

        SdkU3d.ntGameLoginSuccess();
    }

    public void OnQueryMyAccount()
    {
        SdkU3d.ntQueryMyAccount();
    }

    public void OnSurveyOpen()
    {
        MsgBox.instance.OpenMsgBox(352, 703, MSGBOX_TYPE.YESNO, OnSurveyOpenCB);
    }

    private void OnSurveyOpenCB(MSGBOX_TYPE type, bool isYes)
    {
        if (isYes)
            NetworkManager.instance.SendStartSurvey();
        else
            NetworkManager.instance.SendFinishSurvey(true, "");
    }

    #endregion

    #region Float

    public void SetFloat(bool b)
    {
        if (b)
            SdkU3d.ntSetFloatBtnVisible(b);
    }

    #endregion

    #region Popup

    public void OpenPauseView()
    {
        SdkU3d.ntOpenPauseView();
    }

    public void OpenExit()
    {
        if (CheckUniSdkLoginFlag && SdkU3d.hasFeature(ConstProp.MODE_EXIT_VIEW))
            SdkU3d.ntOpenExitView();
        else
            MsgBox.instance.OpenMsgBox(134, 135, MSGBOX_TYPE.YESNO, new MsgBox.CALLBACK(OnApplicationExit));
    }

    private void OnApplicationExit(MSGBOX_TYPE type, bool isYes)
    {
        if (isYes)
            Util.ApplicationQuit();
    }

    public void OpenManager()
    {
        WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.SDK_UPLOADINFO_STOP, true);
        SdkU3d.ntOpenManager();
    }

    public void SwitchAccount()
    {
        WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.SDK_UPLOADINFO_STOP, true);
        SdkU3d.ntSwitchAccount();
    }

    public void OpenDaren()
    {
        SdkU3d.ntShowDaren();
    }

    public void OpenCompactView(bool isRead)
    {
        SdkU3d.setPropStr(ConstProp.COMPACT_FORCE_OPEN, "1");
        
#if UNITY_ANDROID
        InitListenerCallback(ConstProp.LISTENER_CB_UI);
#elif UNITY_IOS
        StartCoroutine("WaitForLoadWorldManager");
#endif

        SdkU3d.ntShowCompactView(isRead);
    }

    public void OpenNotification()
    {
        SdkU3d.ntHasNotification();
    }

    public void OpenWebView(String url)
    {
        SdkU3d.ntOpenWebView(url);
    }

    #endregion

    #endregion

    #region UniSDK_Callback

    /// <summary>
    /// <para>name : OnSdkMsgCallback</para>
    /// <para>describe : UniSDK 콜백.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void OnSdkMsgCallback(string jsonstr)
    {
        string debugStr = string.Format("OnSdkMsgCallback : jsonstr={0}", jsonstr);
        Debug.Log(debugStr);

        JsonData json = JsonMapper.ToObject(jsonstr);
        string callbackType = (string)json["callbackType"];
        int code = (int)json["code"];
        JsonData data = json["data"];

        switch (callbackType)
        {
            case ConstProp.CALLBACKTYPE_OnFinishInit: OnFinishInit(code, (string)data); break;
            case ConstProp.CALLBACKTYPE_OnLoginDone: OnLoginDone(code, (string)data); break;
            case ConstProp.CALLBACKTYPE_OnLogoutDone: OnLogoutDone(code, (string)data); break;
            case ConstProp.CALLBACKTYPE_OnLeaveSdk: OnLeaveSdk(code, (string)data); break;
            case ConstProp.CALLBACKTYPE_OnOrderCheck: OnOrderCheck(code, data); break;
            case ConstProp.CALLBACKTYPE_OnContinue: OnContinue(code, (string)data); break;
            case ConstProp.CALLBACKTYPE_OnExitView: OnExitView(code, (string)data); break;
            case ConstProp.CALLBACKTYPE_OnQueryFriendList: OnQueryFriendList(code, data); break;
            case ConstProp.CALLBACKTYPE_OnQueryAvailablesInvitees: OnQueryAvailablesInvitees(code, data); break;
            case ConstProp.CALLBACKTYPE_OnQueryMyAccount: OnQueryMyAccount(code, data); break;
            case ConstProp.CALLBACKTYPE_OnApplyFriend: OnApplyFriend(code, (bool)data); break;
            case ConstProp.CALLBACKTYPE_OnIsDarenUpdated: OnIsDarenUpdated(code, (bool)data); break;
            case ConstProp.CALLBACKTYPE_OnQueryRank: OnQueryRank(code, data); break;
            case ConstProp.CALLBACKTYPE_OnShare: OnShare(code, (bool)data); break;
            case ConstProp.CALLBACKTYPE_OnReceivedNotification: OnReceivedNotification(code, (bool)data); break;
            case ConstProp.CALLBACKTYPE_OnExtendFuncCall: OnExtendFuncCall(code, (string)data); break;
            case ConstProp.CALLBACKTYPE_OnWebViewNativeCall: OnWebViewNativeCall((string)data); break;
            case ConstProp.CALLBACKTYPE_OnQuerySkuDetails: OnQuerySkuDetails((string)data); break;
        }
    }

    private void OnFinishInit(int code, string msg)
    {
        Debug.Log(string.Format("OnFinishInit : code={0} msd={1}", code, msg));

        m_appChannelType = GetAppChannelType;
#if UNITY_ANDROID
        if (m_appChannelType.Equals(APP_CHANNEL_TYPE.TYPE_BAIDU))
            SdkU3d.ntGetAnnouncementInfo();
#endif
        CheckLoadComplete = true;

        SendDRPF(DRPF_LOG_TYPE.ACTIVATION);
        SetDetectLog((code.Equals(0) ? SDK_DCTOOL_TYPE.UNISDK_INIT_SUCCESS : SDK_DCTOOL_TYPE.UNISDK_INIT_FAIL), 
                        Mathf.Clamp(Time.realtimeSinceStartup - m_elapsedTime, 0, Time.realtimeSinceStartup));
    }

    // Step04. 로그인 완료..
    private void OnLoginDone(int code, string msg)
    {
        Debug.Log(string.Format("OnLoginDone : {0}", msg));
        Debug.Log(string.Format("HasLogin : {0}", CheckUniSdkLogin));

        if ((LOGIN_DONE)code == LOGIN_DONE.OK)
        {
            SendDRPF(DRPF_LOG_TYPE.IDENTIFICATION);
            SetDetectLog(SDK_DCTOOL_TYPE.CHANNEL_LOGIN_SUCCESS, Mathf.Clamp(Time.realtimeSinceStartup - m_elapsedTime, 0, Time.realtimeSinceStartup));
            SetFloat(true);

            InitGMBridge();

            if (StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_TITLE))
            {
                if (m_isFirstSdkLogin)
                    WorldManager.instance.RestartApplication();
                else
                {
                    m_isFirstSdkLogin = true;

                    ((State_Title)StateManager.instance.m_curState).m_guiTitleManager.OnOffLoginButton(false);
                    if (PlayerPrefs.GetInt("IsUserProtocolState").Equals((int)USER_PROTOCOL_STATE.TYPE_ACCEPT))
                        StartCoroutine("WaitForLoadWorldManager");
                    else
                        OpenCompactView(false);
                }
            }

            else
            {
                if (m_isFirstSdkLogin)
                    m_isFirstSdkLogin = false;

                WorldManager.instance.RestartApplication();
            }
        }

        else
        {
            SetDetectLog(SDK_DCTOOL_TYPE.CHANNEL_LOGIN_FAIL);

            if (StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_TITLE))
                ((State_Title)StateManager.instance.m_curState).UpdateLoginUI(true);
            else
                WorldManager.instance.RestartApplication();    
        }
    }

    /// <summary>
    /// <para>name : WaitForLoadWorldManager</para>
    /// <para>describe : SdkLoginData가 null이 아닌 경우는 
    ///                         1. 게임 서버에 로그인하여 SdkLoginData를 생성하고, 
    ///                         2. 옵션에서 SDK 로그아웃 후 타이틀 화면으로 돌아와 다시 SDK 로그인을 했을 때
    ///                            입니다. 이 시점에서 게임 서버에 로그아웃을 보냅니다. </para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    private IEnumerator WaitForLoadWorldManager()
    {
        while (WorldManager.instance.CheckInitComplete == false)
            yield return null;

        if (m_sdkLoginData != null)
            NetworkManager.instance.SendAuthLogout(m_sdkLoginData);
        else
        {
            if (CheckHasUserCenter)
                ((State_Title)StateManager.instance.m_curState).UpdateLoginUI(true);
            else
                NetworkManager.instance.SendSdkLogin(MakeSSdkLoginParam());
        }
    }

    private void OnLogoutDone(int code, string msg)
    {
        Debug.Log(string.Format("OnLogoutDone : {0} code, {1}", code, msg));

        switch (m_appChannelType)
        {
            case APP_CHANNEL_TYPE.TYPE_YYXX:
                m_isFirstSdkLogin = false;

                if (StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_TITLE))
                    ((State_Title)StateManager.instance.m_curState).UpdateLoginUI(true);
                else
                    WorldManager.instance.RestartApplication();
                break;

            default:
                break;
        }
    }

    private void OnLeaveSdk(int code, string msg)
    {
        Debug.Log(string.Format("OnLeaveSdk : {0}", msg));

#if UNITY_IOS
        if(SdkManager.instance.CheckUniSdkLogin == false)
        {
            switch(StateManager.instance.m_curStateType)
            {
                case STATE_TYPE.STATE_TITLE:
                    ((State_Title)StateManager.instance.m_curState).UpdateLoginUI(true);
                    break;
                case STATE_TYPE.STATE_ROOM:
                case STATE_TYPE.STATE_VILLAGE:
                    WorldManager.instance.RestartApplication();
                    break;
            }
        }
#endif
    }

    private void OnOrderCheck(int code, JsonData jsonOrder)
    {
        NtOrderInfo orderInfo = NtOrderInfo.FromJsonData(jsonOrder);
        Debug.Log(string.Format("OrderStatus : {0}", orderInfo.orderStatus));

        switch (orderInfo.orderStatus)
        {
            case OrderStatus.OS_SDK_CHECK_OK:
            case OrderStatus.OS_GS_CHECK_OK:
                NetworkManager.instance.SendPayRefreshOrder();
                break;

            case OrderStatus.OS_SDK_CHECKING:
            case OrderStatus.OS_GS_CHECKING:
                break;

            default:
                MsgBox.instance.OpenMsgBox(168, 169, MSGBOX_TYPE.OK, null);
                break;
        }

        LoadingManager.instance.SetActiveProgressBar(false, false);
    }

    private void OnContinue(int code, string msg)
    {
        Debug.Log(string.Format("OnContinue : {0}", msg));
    }

    private void OnExitView(int code, string msg)
    {
        Debug.Log(string.Format("OnExitView : {0}", msg));
        Util.ApplicationQuit();
    }

    private void OnQueryFriendList(int code, JsonData data)
    {
        Debug.Log(string.Format("OnQueryFriendList : {0}", data));
    }

    private void OnQueryAvailablesInvitees(int code, JsonData data)
    {
        Debug.Log(string.Format("OnQueryAvailablesInvitees : {0}", data));
    }

    private void OnQueryMyAccount(int code, JsonData data)
    {
        Debug.Log(string.Format("OnQueryMyAccount : {0}", data));
    }

    private void OnApplyFriend(int code, bool result)
    {
        Debug.Log(string.Format("OnApplyFriend : {0}", result));
    }

    private void OnIsDarenUpdated(int code, bool result)
    {
        Debug.Log(string.Format("OnIsDarenUpdated : {0}", result));
        m_isNotification = result;
    }

    private void OnQueryRank(int code, JsonData data)
    {
        Debug.Log(string.Format("OnQueryRank : {0}", data));
    }

    private void OnShare(int code, bool result)
    {
        Debug.Log(string.Format("OnShare : {0}", result));
    }

    private void OnReceivedNotification(int code, bool result)
    {
        Debug.Log(string.Format("OnReceivedNotification : {0}", result));
        m_isNotification = result;
    }

    private void OnExtendFuncCall(int code, string jsonStr)
    {
        Debug.Log(string.Format("OnExtendFuncCall : {0}", jsonStr));
    }

    private void OnWebViewNativeCall(string jsonStr)
    {
        Debug.Log(string.Format("OnWebViewNativeCall : {0}", jsonStr));

        JsonData jData = JsonMapper.ToObject(jsonStr);

        string action = (string)jData["action"];
        string surveyId = (string)jData["surveyId"];

        bool isComplete = WorldManager.instance.CheckMemoryInfoExists(WORLD_MEMORY_INFO.SURVEY_COMPLETE);

        switch (action)
        {
            case "close":
                if (isComplete == false)
                    NetworkManager.instance.SendFinishSurvey(true, surveyId);
                break;

            case "finish":
                WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.SURVEY_COMPLETE, true);
                NetworkManager.instance.SendFinishSurvey(false, surveyId);
                break;
        }
    }

    private void OnQuerySkuDetails(string jsonStr)
    {
        Debug.Log(string.Format("OnQuerySkuDetails : {0}", jsonStr));
    }

    #endregion

    #region Util

    /// <summary>
    /// <para>name : GetAppChannelType</para>
    /// <para>describe : 현재 SDK의 채널 타입을 받아감.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public APP_CHANNEL_TYPE GetAppChannelType
    {
        get
        {
            string appChannelType = SdkU3d.getAppChannel();
            for (int i = 0; i < CHANNEL_NAME_ARRAY.Length; i++)
            {
                if (CHANNEL_NAME_ARRAY[i].Equals(appChannelType))
                    return (APP_CHANNEL_TYPE)i;
            }

            return APP_CHANNEL_TYPE.TYPE_NETEASE;
        }
    }

    public bool CheckUniSdkLogin
    {
        get { return SdkU3d.hasLogin(); }
    }

    public string GetSession
    {
        get { return SdkU3d.getPropStr(ConstProp.SESSION); }
    }

    public string GetUserName
    {
        get
        {
#if UNITY_EDITOR
            return WorldManager.instance.m_player.m_UserName;
#else
            return string.Format("{0}@{1}.{2}.win.163.com",
                            SdkU3d.getPropStr(ConstProp.UID),
                            SdkU3d.getPlatform(),
                            SdkU3d.getChannel());
#endif
        }
    }

    public string GetUID
    {
        get
        {
#if UNITY_EDITOR
            return WorldManager.instance.m_player.m_id;
#else
            return SdkU3d.getPropStr(ConstProp.UID);
#endif
        }
    }

    public bool CheckHasUserCenter
    {
        get { return SdkU3d.getPropInt(ConstProp.MODE_HAS_MANAGER, 0).Equals(1); }
    }

    public string GetNetworkState
    {
        get { return SdkU3d.getPropStr(ConstProp.APP_NETWORK_STATE); }
    }

    public string GetMacAddress
    {
        get { return SdkU3d.getPropStr(ConstProp.MAC_ADDR); }
    }

    public string GetISP
    {
        get { return SdkU3d.getPropStr(ConstProp.APP_ISP); }
    }

    /// <summary>
    /// <para>name : CheckSdkNotification</para>
    /// <para>describe : SdkAuthLogin 후, 알림 또는 공지가 있는 지 체크.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public bool CheckSdkNotification
    {
        get
        {
#if UNITY_EDITOR
            m_isNotification = false;
#elif UNITY_ANDROID
            SdkU3d.ntIsDarenUpdated();
#elif UNITY_IOS
            m_isNotification = SdkU3d.ntHasNotification();
#endif

            return m_isNotification;
        }
    }

    /// <summary>
    /// <para>name : GetAccountID</para>
    /// <para>describe : SdkAuthLogin를 통해 얻은 aid.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public int GetAccountID
    {
        get { return (m_sdkLoginData != null && m_sdkLoginData.sdkLoginAuthInfo != null) ? m_sdkLoginData.sdkLoginAuthInfo.aid : -1; }
    }

    /// <summary>
    /// <para>name : GetLoginSession</para>
    /// <para>describe : SdkAuthLogin를 통해 얻은 이번 로그인의 세션 Number.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public string GetLoginSession
    {
        get { return (m_sdkLoginData != null && m_sdkLoginData.sdkLoginAuthInfo != null) ? m_sdkLoginData.sdkLoginAuthInfo.SN : null; }
    }

    public bool CheckAuthGuest
    {
#if UNITY_EDITOR
        get { return false; }
#else
        get { return SdkU3d.getAuthType().Equals(ConstProp.AUTH_GUEST); }
#endif
    }

    /// <summary>
    /// <para>name : MakeSSdkLoginParam</para>
    /// <para>describe : SdkAuthLogin을 위한 SSdkLoginParam을 만든다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public SSdkLoginParam MakeSSdkLoginParam()
    {
#if UNITY_EDITOR
        return new SSdkLoginParam();
#else
        return new SSdkLoginParam(
            SdkU3d.getPropStr(ConstProp.USERINFO_HOSTID),
            SdkU3d.getPropStr(ConstProp.UID),
            SdkU3d.getPropStr(ConstProp.ORIGIN_GUEST_UID),
            CheckAuthGuest ? 1 : 0,
            SdkU3d.getChannel(),
            SdkU3d.getAppChannel(),
            SdkU3d.getPlatform(),
            SdkU3d.getUdid(),
            GetSession,
            SdkU3d.getSDKVersion(SdkU3d.getChannel()),
            SdkU3d.getPropStr(ConstProp.DEVICE_ID),
            Define.PROJECT_ID,
            GetUserName,
            "",
            SdkU3d.getPropStr(ConstProp.TIMESTAMP),
            SdkU3d.getPropStr(ConstProp.CPID),
            SdkU3d.getPropStr(ConstProp.N_SDK_CHANNEL),
            SdkU3d.getPropStr(ConstProp.UID),
            SdkU3d.getPropStr(ConstProp.REDIRECT_URI),
            "",
            SdkU3d.getPropStr(ConstProp.DEVICE_MODEL_NALE),
            SdkU3d.getPropStr(ConstProp.OS_NAME),
            SdkU3d.getPropStr(ConstProp.OS_VER));
#endif
    }

    public void Exit()
    {
        SdkU3d.exit();
    }

    public void ShowDebugDefaultInfo()
    {
        Debug.Log(string.Format("App Channel : {0}", SdkU3d.getAppChannel()));
        Debug.Log(string.Format("Auth Type : {0}", SdkU3d.getAuthType()));
        Debug.Log(string.Format("Channel : {0}", SdkU3d.getChannel()));
        Debug.Log(string.Format("Platform : {0}", SdkU3d.getPlatform()));
        Debug.Log(string.Format("UDID : {0}", SdkU3d.getUdid()));

        Debug.Log(string.Format("[SAUTH PARAMETER]"));

        Debug.Log(string.Format("App ID : {0}", SdkU3d.getPropStr(ConstProp.APPID)));
        Debug.Log(string.Format("Game Name : {0}", SdkU3d.getPropStr(ConstProp.GAME_NAME)));
        Debug.Log(string.Format("Host ID : {0}", SdkU3d.getPropInt(ConstProp.USERINFO_HOSTID, 0)));
        Debug.Log(string.Format("App Channel : {0}", SdkU3d.getPropStr(ConstProp.APP_CHANNEL)));
        Debug.Log(string.Format("UID : {0}", SdkU3d.getPropStr(ConstProp.UID)));
        Debug.Log(string.Format("User Name : {0}@{1}.{2}.win.163.com",
                            SdkU3d.getPropStr(ConstProp.UID),
                            SdkU3d.getPlatform(),
                            SdkU3d.getChannel()));
        Debug.Log(string.Format("SessionID : {0}", SdkU3d.getPropStr(ConstProp.SESSION)));
        Debug.Log(string.Format("SDK Version : {0}", SdkU3d.getSDKVersion(SdkU3d.getChannel())));
        Debug.Log(string.Format("DeviceID : {0}", SdkU3d.getPropStr(ConstProp.DEVICE_ID)));
        Debug.Log(string.Format("CPID : {0}", SdkU3d.getPropStr(ConstProp.CPID)));

        Debug.Log(string.Format("OS_NAME : {0}", SdkU3d.getPropStr(ConstProp.OS_NAME)));
        Debug.Log(string.Format("OS_VER : {0}", SdkU3d.getPropStr(ConstProp.OS_VER)));
        Debug.Log(string.Format("DEVICE_MODEL_NALE : {0}", SdkU3d.getPropStr(ConstProp.DEVICE_MODEL_NALE)));

        Debug.Log(string.Format("SDK_CHANNEL_ID : {0}", SdkU3d.getPropStr(ConstProp.SDK_CHANNEL_ID)));
        Debug.Log(string.Format("REDIRECT_URI : {0}", SdkU3d.getPropStr(ConstProp.REDIRECT_URI)));

        Debug.Log(string.Format("APP_NETWORK_STATE : {0}", SdkU3d.getPropStr(ConstProp.APP_NETWORK_STATE)));
        Debug.Log(string.Format("MAC_ADDR : {0}", SdkU3d.getPropStr(ConstProp.MAC_ADDR)));
        Debug.Log(string.Format("APP_ISP : {0}", SdkU3d.getPropStr(ConstProp.APP_ISP)));
    }

    public bool CheckUniSdkLoginFlag
    {
        get { return m_isSdkLoginFlag; }
        set
        {
#if UNITY_EDITOR
            m_isSdkLoginFlag = false;
#else
#if SERVER_TEST
            m_isSdkLoginFlag = value;
#elif SERVER_ALPHA
            m_isSdkLoginFlag = value;
#elif SERVER_LIVE
            m_isSdkLoginFlag = true;
#else
            m_isSdkLoginFlag = false;
#endif
#endif
        }
    }

    #endregion

    #region UpLoadUserInfo

    public void UpLoadUserInfo(UPLOAD_USERINFO_STATE state)
    {
        Debug.Log("SdkManager.UpLoadUserInfo." + state);

        if (CheckUniSdkLoginFlag == false)
            return;

        if (CheckUniSdkLogin && WorldManager.instance.CheckGameLogin())
        {
            if (WorldManager.instance.CheckMemoryInfoExists(WORLD_MEMORY_INFO.SDK_UPLOADINFO_STOP))
            {
                WorldManager.instance.DelMemoryInfo(WORLD_MEMORY_INFO.SDK_UPLOADINFO_STOP);
                return;
            }

            if (state.Equals(UPLOAD_USERINFO_STATE.TYPE_CREATE_ROLE)) { SdkU3d.setUserInfo(ConstProp.USERINFO_STAGE, ConstProp.USERINFO_STAGE_CREATE_ROLE); }
            else if (state.Equals(UPLOAD_USERINFO_STATE.TYPE_LOGIN)) { SdkU3d.setUserInfo(ConstProp.USERINFO_STAGE, ConstProp.USERINFO_STAGE_ENTER_SERVER); }
            else if (state.Equals(UPLOAD_USERINFO_STATE.TYPE_LOGOUT)) { SdkU3d.setUserInfo(ConstProp.USERINFO_STAGE, ConstProp.USERINFO_STAGE_LEAVE_SERVER); }
            else if (state.Equals(UPLOAD_USERINFO_STATE.TYPE_LEVEL_UP))
            {
                SdkU3d.setUserInfo(ConstProp.USERINFO_STAGE, ConstProp.USERINFO_STAGE_LEVEL_UP);
                SdkU3d.setUserInfo(ConstProp.USERINFO_ROLE_LEVELMTIME, Util.GetNowGameTime().ToString());
            }
            else { }

            SdkU3d.setUserInfo(ConstProp.USERINFO_UID, WorldManager.instance.m_player.m_userNo);
            SdkU3d.setUserInfo(ConstProp.USERINFO_AID, GetAccountID.ToString());
            SdkU3d.setUserInfo(ConstProp.USERINFO_NAME, WorldManager.instance.m_player.m_UserName);
            SdkU3d.setUserInfo(ConstProp.USERINFO_GRADE, WorldManager.instance.m_player.m_level.ToString());

            CharacterBase mainCb = WorldManager.instance.m_player.FindDog(WorldManager.instance.m_player.m_mainDog);

            // 소속 서버
            SdkU3d.setUserInfo(ConstProp.USERINFO_HOSTID, WorldManager.instance.m_player.m_hostId);
            SdkU3d.setUserInfo(ConstProp.USERINFO_HOSTNAME, NetworkManager.instance.NetworkInfoClass.GetServerRealName);

            // 강아지 종류 ID, 강아지 종류명
            SdkU3d.setUserInfo(ConstProp.USERINFO_ROLE_TYPE_ID, mainCb != null ? mainCb.m_dogInfo.index.ToString() : "0");
            SdkU3d.setUserInfo(ConstProp.USERINFO_ROLE_TYPE_NAME, mainCb != null ? mainCb.m_dogInfo.kindName : "0");

            // 캐릭터 직업
            SdkU3d.setUserInfo(ConstProp.USERINFO_MENPAI_ID, mainCb != null ? mainCb.m_dogInfo.index.ToString() : "0");
            SdkU3d.setUserInfo(ConstProp.USERINFO_MENPAI_NAME, mainCb != null ? mainCb.m_dogInfo.kindName : "0");

            // 캐릭터 전투력, VIP 등급
            SdkU3d.setUserInfo(ConstProp.USERINFO_CAPABILITY, mainCb != null ? mainCb.m_mainStat.ToString() : "0");
            SdkU3d.setUserInfo(ConstProp.USERINFO_VIP, "0");

            // 서버 구역명
            SdkU3d.setUserInfo(ConstProp.SERVER_ID, "001");
            SdkU3d.setUserInfo(ConstProp.USERINFO_REGION_ID, "0");
            SdkU3d.setUserInfo(ConstProp.USERINFO_REGION_NAME, "0");
            SdkU3d.setUserInfo(ConstProp.USERINFO_ORG, "0");

            // 현재 시간과 레벨업 시간
            SdkU3d.setUserInfo(ConstProp.USERINFO_ROLE_CTIME, Util.GetNowGameTime().ToString());

            SdkU3d.ntUpLoadUserInfo();
            ShowDebugDefaultInfo();

            // if (m_appChannelType.Equals(APP_CHANNEL_TYPE.TYPE_360))
            //     StartCoroutine(Update360CharacterPort());
        }

        switch(state)
        {
            case UPLOAD_USERINFO_STATE.TYPE_CREATE_ROLE:
            case UPLOAD_USERINFO_STATE.TYPE_LOGIN:
                SdkManager.instance.SetCrashHunter();
                break;
                
            case UPLOAD_USERINFO_STATE.TYPE_CHARACTER_CHANGE:
            case UPLOAD_USERINFO_STATE.TYPE_APP_BACKGROUND:
            case UPLOAD_USERINFO_STATE.TYPE_LEVEL_UP:
                break;

            case UPLOAD_USERINFO_STATE.TYPE_APP_QUIT:
                if (WorldManager.instance.CheckGameLogin() && (Tutorial.instance.CheckTutorialEnable == false && 
                                                               Tutorial.instance.CheckTutorialBuildingEnable == false))
                    NetworkManager.instance.SendAuthLogout();

                SdkU3d.resetCommonProp();
                SdkU3d.exit();

                Application.Quit();
                break;
        }
    }

    private void UpdateUserInfo()
    {
        SdkU3d.setUserInfo(ConstProp.USERINFO_UID, WorldManager.instance.m_player.m_userNo);
        SdkU3d.setUserInfo(ConstProp.USERINFO_AID, GetAccountID.ToString());
        SdkU3d.setUserInfo(ConstProp.USERINFO_NAME, WorldManager.instance.m_player.m_UserName);
        SdkU3d.setUserInfo(ConstProp.USERINFO_GRADE, WorldManager.instance.m_player.m_level.ToString());

        SdkU3d.setUserInfo(ConstProp.USERINFO_HOSTID, WorldManager.instance.m_player.m_hostId);
        SdkU3d.setUserInfo(ConstProp.USERINFO_HOSTNAME, NetworkManager.instance.NetworkInfoClass.GetServerRealName);
        SdkU3d.setUserInfo(ConstProp.USERINFO_VIP, "0");

        SdkU3d.setUserInfo(ConstProp.SERVER_ID, "001");
        SdkU3d.setUserInfo(ConstProp.USERINFO_REGION_ID, "0");
        SdkU3d.setUserInfo(ConstProp.USERINFO_REGION_NAME, "0");
        SdkU3d.setUserInfo(ConstProp.USERINFO_ORG, "0");

        SdkU3d.setUserInfo(ConstProp.USERINFO_ROLE_CTIME, Util.GetNowGameTime().ToString());
    }

    #endregion

    #region Json

    private void SetTokenInfo(string message)
    {
        JsonData jData = JsonMapper.ToObject(message);
        for (int i = 0; i < jData.Count; i++)
        {
            if (jData[i] != null && jData[i].IsObject)
            {
                IDictionary dictionary = jData[i] as IDictionary;
                if (dictionary != null)
                {
                    if (dictionary.Contains("openid"))
                        SdkU3d.setPropStr(ConstProp.UID, dictionary["openid"].ToString());
                    if (dictionary.Contains("access_token"))
                        SdkU3d.setPropStr(ConstProp.SESSION, dictionary["access_token"].ToString());
                    if (dictionary.Contains("expires_in"))
                        SdkU3d.setPropStr(ConstProp.TIMESTAMP, dictionary["expires_in"].ToString());
                    if (dictionary.Contains("refresh_token"))
                        SdkU3d.setPropStr(ConstProp.REFRESH_TOKEN, dictionary["refresh_token"].ToString());

                    Debug.Log("UID : " + SdkU3d.getPropStr(ConstProp.UID));
                    Debug.Log("SESSION : " + SdkU3d.getPropStr(ConstProp.SESSION));
                    Debug.Log("TIMESTAMP : " + SdkU3d.getPropStr(ConstProp.TIMESTAMP));
                    Debug.Log("REFRESH_TOKEN : " + SdkU3d.getPropStr(ConstProp.REFRESH_TOKEN));

                    break;
                }
            }
        }
    }

    #endregion

    #region MD5

    private IEnumerator Update360CharacterPort()
    {
        Debug.Log("Update360CharacterPort.");

        string token = string.Format("qid={0}&package={1}&lt={2}",
                            SdkU3d.getPropStr(ConstProp.UID),
                            "com.netease.xiaowangwang.qihoo",
                            Util.GetNowGameTime());
        string sign = MD5HashFunc(token);
        string url = string.Format("http://next.gamebox.360.cn/7/role/rolecheck?qid={0}&package={1}&lt={2}&sign={3}",
                            SdkU3d.getPropStr(ConstProp.UID),
                            "com.netease.xiaowangwang.qihoo",
                            Util.GetNowGameTime(),
                            sign);

        WWW www = new WWW(url);
        yield return www;

        if (www.error == null) Debug.Log(string.Format("Update360CharacterPort.Success : {0}", www.text));
        else Debug.Log(string.Format("Update360CharacterPort.Error : {0}", www.error));
    }

    private string MD5HashFunc(string token)
    {
        MD5 md5 = new MD5CryptoServiceProvider();
        byte[] hash = md5.ComputeHash(Encoding.ASCII.GetBytes(token));

        StringBuilder sBuilder = new StringBuilder();
        foreach (byte b in hash)
            sBuilder.AppendFormat("{0:x2}", b);

        return sBuilder.ToString();
    }

    #endregion
}
