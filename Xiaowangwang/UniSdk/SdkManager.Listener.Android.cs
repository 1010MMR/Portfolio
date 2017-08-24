#if UNITY_ANDROID

using UnityEngine;
using System;
using System.Collections;

using NtUniSdk.Unity3d;
using LitJson;

#if USER_SERVER
using NetWork;
#endif

#region ProtocolFinishCB
public class ProtocolFinishCB : AndroidJavaProxy
{
    public ProtocolFinishCB() : base("com.netease.ntunisdk.base.OnProtocolFinishListener") { }
    public ProtocolFinishCB(Action<int> OnProtocolFinish) : base("com.netease.ntunisdk.base.OnProtocolFinishListener")
    {
        this.OnProtocolFinish = OnProtocolFinish;
    }

    private Action<int> OnProtocolFinish = null;

    public void onProtocolFinish(int paramInt)
    {
        if (OnProtocolFinish != null)
            OnProtocolFinish(paramInt);
    }
}
#endregion

#region GMPageTokenCB
public class GMPageTokenCB : AndroidJavaProxy
{
    public GMPageTokenCB() : base("com.h26.gmbridge.GmBridgeITokenRequest") { }
    public GMPageTokenCB(Func<string> ITokenRequest) : base("com.h26.gmbridge.GmBridgeITokenRequest")
    {
        this.ITokenRequest = ITokenRequest;
    }

    private Func<string> ITokenRequest = null;

    public string getToken()
    {
        if (ITokenRequest != null) return ITokenRequest();
        else return "";
    }
}
#endregion

public partial class SdkManager : MonoSingleton<SdkManager>
{
    private GMPageTokenCB m_gmPageTokenCB = null;
    public GMPageTokenCB GetGMPageTokenCB { get { return m_gmPageTokenCB != null ? m_gmPageTokenCB : new GMPageTokenCB(OnITokenRequest); } }

    private void InitListenerCallback(int value)
    {
        SdkU3d.setOnProtocolFinishListener(value, new ProtocolFinishCB(OnProtocolFinishCB));
    }

    #region Callback

    private void OnProtocolFinishCB(int paramInt)
    {
        Debug.Log("OnProtocolFinishCB : " + paramInt);

        PlayerPrefs.SetInt("IsUserProtocolState", paramInt);
        if (paramInt.Equals((int)USER_PROTOCOL_STATE.TYPE_ACCEPT))
            StartCoroutine("WaitForLoadWorldManager");
    }

    private string OnITokenRequest()
    {
        return m_gmToken;
    }

    #endregion
}

#endif