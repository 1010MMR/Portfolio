#if UNITY_IOS

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public partial class iPhonePlugin
{
    public IEnumerator InitPushNotification()
    {
        if (CheckPlatform)
        {
            UnityEngine.iOS.NotificationServices.RegisterForNotifications(
                UnityEngine.iOS.NotificationType.Alert |
                UnityEngine.iOS.NotificationType.Badge |
                UnityEngine.iOS.NotificationType.Sound);
            
            yield return new WaitForSeconds(INIT_DELAY_TIME);
        }
    }

    public void SendNotification(PushNotificationData data)
    {
        if (CheckPlatform)
        {
            UnityEngine.iOS.LocalNotification iosNoti = new UnityEngine.iOS.LocalNotification();
            iosNoti.fireDate = data.dateTime;
            iosNoti.alertAction = data.title;
            iosNoti.alertBody = data.message;
            iosNoti.hasAction = true;
            iosNoti.userInfo.Add(PushManager.NOTIFICATION_ID, data.id);

            UnityEngine.iOS.NotificationServices.ScheduleLocalNotification(iosNoti);
        }
    }

    public void ClearAllNotification()
    {
        if (CheckPlatform)
            UnityEngine.iOS.NotificationServices.CancelAllLocalNotifications();
    }
    
    #region Util

    public string GetPushDeviceId()
    {
        string deviceId = "null";
        byte[] token = UnityEngine.iOS.NotificationServices.deviceToken;
        if (token != null)
            deviceId = System.BitConverter.ToString(token).Replace("-", "");

        return CheckPlatform ? deviceId : "null";
    }

    #endregion
}

#endif