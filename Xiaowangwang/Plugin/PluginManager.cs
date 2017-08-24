using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using LitJson;

public partial class PluginManager : MonoSingleton<PluginManager>
{
    public override void Init()
    {
#if UNITY_ANDROID
        InitAndroidPlugin();
#elif UNITY_IOS
        InitiOSPlugin();
#else
#endif
    }

    #region Push

    public IEnumerator InitPushNotification()
    {
#if UNITY_ANDROID
        yield return StartCoroutine(m_adPlugin.InitPushNotification());
#elif UNITY_IOS
        yield return StartCoroutine(m_ipPlugin.InitPushNotification());
#else
        yield return null;
#endif
    }

    public void SendNotification(PushNotificationData data)
    {
        Debug.Log(string.Format("{0} >> PushMessage : {1} - {2} - {3}", Util.GetNowDateGameTime(), data.id, data.message, data.dateTime));

#if UNITY_ANDROID
        m_adPlugin.SendNotification(data);
#elif UNITY_IOS
        m_ipPlugin.SendNotification(data);
#else
#endif
    }

    public void ClearAllNotification()
    {
#if UNITY_ANDROID
        m_adPlugin.ClearAllNotification();
#elif UNITY_IOS
        m_ipPlugin.ClearAllNotification();
#else
#endif
    }

    public string GetPushDeviceId()
    {
#if UNITY_ANDROID
        return m_adPlugin.GetPushDeviceId();
#elif UNITY_IOS
        return m_ipPlugin.GetPushDeviceId();
#else
        return "null";
#endif
    }

    #endregion

    #region Image

    public IEnumerator InitImage(object param)
    {
#if UNITY_ANDROID
        yield return StartCoroutine(m_adPlugin.InitImage((ImagePickerCB)param));
#elif UNITY_IOS
        yield return StartCoroutine(m_ipPlugin.InitImage((ImagePickerCB)param));
#else
        yield return null;
#endif
    }

    public void GetImage()
    {
#if UNITY_ANDROID
        m_adPlugin.GetImage();
#elif UNITY_IOS
        m_ipPlugin.GetImage();
#else
#endif
    }

    public void SaveImageToGallery(Texture2D texture, string title, string desc)
    {
#if UNITY_ANDROID
        m_adPlugin.SaveImageToGallery(texture, title, desc);
#elif UNITY_IOS
        m_ipPlugin.SaveImageToGallery(texture);
#else
#endif

        texture = null;
    }

    #endregion

    #region Camera

    public IEnumerator InitCamera(object param)
    {
#if UNITY_ANDROID
        yield return StartCoroutine(m_adPlugin.InitCamera((CameraCB)param));
#elif UNITY_IOS
        yield return null;
#else
        yield return null;
#endif
    }

    public void OpenCamera()
    {
#if UNITY_ANDROID
        m_adPlugin.OpenCamera();
#elif UNITY_IOS
#else
#endif
    }

    #endregion

    #region Time

    public int GetServerTimeZoneUt(bool isLocalServer)
    {
#if UNITY_EDITOR
        return Util.GetServerTimeZoneUt(isLocalServer);
#elif UNITY_ANDROID
        return m_adPlugin.GetServerTimeZoneUt(isLocalServer);
#else
        return Util.GetServerTimeZoneUt(isLocalServer);
#endif
    }

    #endregion
    
    #region Eplay

    public IEnumerator InitEplay()
    {
#if UNITY_ANDROID
        yield return StartCoroutine(m_adPlugin.InitEplay());
#else
        yield return null;
#endif
    }

    public void LoginEplay()
    {
#if UNITY_ANDROID
        m_adPlugin.LoginEplay();
#else
#endif
    }

    public void OpenEplay(int page)
    {
#if UNITY_ANDROID
        m_adPlugin.OpenEplay(page);
#else
#endif
    }

    #endregion

    #region Environment

    public bool ReviewNickname(string nickName)
    {
#if UNITY_EDITOR
        return true;
#elif UNITY_ANDROID
        return m_adPlugin.ReviewNickname(nickName);
#elif UNITY_IOS
        return m_ipPlugin.ReviewNickname(nickName);
#else
        return true;
#endif
    }

    public bool ReviewMessage(string message, int level = 0, int channel = 0)
    {
#if UNITY_EDITOR
        return true;
#elif UNITY_ANDROID
        return m_adPlugin.ReviewMessage(message, level, channel);
#elif UNITY_IOS
        return m_ipPlugin.ReviewMessage(message, level, channel);
#else
        return true;
#endif
    }

    public bool CheckReviewResult(string result)
    {
        Debug.Log("ReviewResult : " + result);

        JsonData jData = JsonMapper.ToObject(result);

        string regularId = (string)jData["regularId"];
        int code = (int)jData["code"];
        string message = (string)jData["message"];

        switch (code)
        {
            case 202:
            case 205: return false;
            default: return true;
        }
    }

    #endregion

    #region Util

    public string[] GetRunningProcessList()
    {
#if UNITY_ANDROID
        return m_adPlugin.GetRunningProcessList();
#else
        return new string[0];
#endif
    }

    public bool CheckRootingActive()
    {
#if UNITY_ANDROID
        return m_adPlugin.CheckRootingActive();
#else
        return false;
#endif
    }

    public bool CheckRunningImitator()
    {
#if UNITY_ANDROID
        return m_adPlugin.CheckRunningImitator();
#else
        return false;
#endif
    }

    public int GetStatusBarHeight()
    {
#if UNITY_ANDROID
        return m_adPlugin.GetStatusBarHeight();
#else
        return 0;
#endif
    }

    public void CopyClipboard(string text)
    {
#if UNITY_ANDROID
        m_adPlugin.CopyClipboard(text);
#elif UNITY_IOS
#else
#endif
    }

    #endregion
}