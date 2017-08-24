#if UNITY_ANDROID

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class AndroidPlugin
{
    public IEnumerator InitPushNotification()
    {
        if (CheckPlatform)
        {
            if (CheckPluginLoadComplete(ANDROID_PLUGIN_TYPE.TYPE_PUSH) == false)
            {
                m_initCheckList.Add(ANDROID_PLUGIN_TYPE.TYPE_PUSH);
                m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_PUSH].CallStatic("init");

                yield return new WaitForSeconds(INIT_DELAY_TIME);
            }
        }
    }

    public void SendNotification(PushNotificationData data)
    {
        if (CheckPlatform)
            m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_PUSH].CallStatic("sendNativePush", data.id.ToString(), data.title, data.message, data.tickerMsg, (long)Util.GetDateTimeToUnixTime(data.dateTime));
    }

    public void ClearAllNotification()
    {
        if (CheckPlatform)
            m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_PUSH].CallStatic<bool>("removeAllNativePush");
    }

    #region Util

    public string GetPushDeviceId()
    {
        return CheckPlatform ? m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_PUSH].CallStatic<string>("getDeviceId") : "null";
    }

    #endregion
}

#endif
