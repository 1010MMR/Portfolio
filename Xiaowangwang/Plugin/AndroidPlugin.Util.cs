#if UNITY_ANDROID

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class AndroidPlugin
{
    #region Immersive

    public void SetImmersive()
    {
        if (CheckPlatform)
            m_adjClassTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMMERSIVE].CallStatic<bool>("addCurrentActivity");
    }

    public void ClearImmersive()
    {
        if (CheckPlatform)
            m_adjClassTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMMERSIVE].CallStatic<bool>("clear");
    }

    public void ContainsImmersiveCurrentActivity()
    {
        if (CheckPlatform)
            m_adjClassTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMMERSIVE].CallStatic<bool>("containsCurrentActivity");
    }

    public void ImmersiveDeviceHasKey(int keyCode)
    {
        if (CheckPlatform)
            m_adjClassTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMMERSIVE].CallStatic<bool>("deviceHasKey", keyCode);
    }

    public void RemoveImmersiveCurrentActivity()
    {
        if (CheckPlatform)
            m_adjClassTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMMERSIVE].CallStatic<bool>("removeCurrentActivity");
    }

    #endregion

    #region Permission

    public bool CheckPermission(PERMISSION_TYPE type)
    {
        if (CheckPlatform)
        {
            switch (type)
            {
                case PERMISSION_TYPE.TYPE_ACCOUNT:
                    return m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_PERMISSION].CallStatic<bool>("checkPermission", "", 800);
                case PERMISSION_TYPE.TYPE_CAMERA:
                    return m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_PERMISSION].CallStatic<bool>("checkPermission", "", 801);
                default:
                    return false;
            }
        }

        return false;
    }

    #endregion

    #region Time

    public int GetServerTimeZoneUt(bool isLocalServer)
    {
        return CheckPlatform ? Util.GetNowGameTime() + ((m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_TIME].CallStatic<int>("getServerTime", (long)Util.GetNowGameTime(), isLocalServer)) / 1000) : Util.GetNowLocalGameTime();
    }

    #endregion

    #region Util

    public string[] GetRunningProcessList()
    {
        return CheckPlatform ? m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_UTIL].CallStatic<string[]>("getRunningProcessList") : new string[0];
    }

    public bool CheckRootingActive()
    {
        return CheckPlatform ? m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_UTIL].CallStatic<bool>("checkRootingActive") : false;
    }

    public bool CheckRunningImitator()
    {
        return CheckPlatform ? m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_UTIL].CallStatic<bool>("checkRootingActive") : false;
    }

    public int GetStatusBarHeight()
    {
        return CheckPlatform ? m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_UTIL].CallStatic<int>("getStatusBarHeight") : 0;
    }

    public void CopyClipboard(string text)
    {
        if (CheckPlatform)
            m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_UTIL].CallStatic("copyClipBoard", text);
    }

    #endregion
}

#endif