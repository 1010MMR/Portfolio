#if UNITY_ANDROID

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#region CameraCB
public class CameraCB : AndroidJavaProxy
{
    public CameraCB() : base("com.h26.plugin.camera.ICameraCallback") { }
    public CameraCB(Action<string> OnCaptureImageComplete, Action OnCaptureImageCancel, Action OnCaptureImageFail)
        : base("com.h26.plugin.camera.ICameraCallback")
    {
        this.OnCaptureImageComplete = OnCaptureImageComplete;
        this.OnCaptureImageCancel = OnCaptureImageCancel;
        this.OnCaptureImageFail = OnCaptureImageFail;
    }

    private Action<string> OnCaptureImageComplete = null;
    private Action OnCaptureImageCancel = null;
    private Action OnCaptureImageFail = null;

    private void CaptureImageComplete(string imagePath)
    {
        if (OnCaptureImageComplete != null)
            OnCaptureImageComplete(imagePath);
    }

    private void CaptureImageCancel()
    {
        if (OnCaptureImageCancel != null)
            OnCaptureImageCancel();
    }

    private void CaptureImageFail()
    {
        if (OnCaptureImageFail != null)
            OnCaptureImageFail();
    }
}
#endregion

public partial class AndroidPlugin
{
    public IEnumerator InitCamera(CameraCB cameraCB)
    {
        if (CheckPlatform)
        {
            if (CheckPluginLoadComplete(ANDROID_PLUGIN_TYPE.TYPE_CAMERA) == false)
            {
                m_initCheckList.Add(ANDROID_PLUGIN_TYPE.TYPE_CAMERA);

                m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_CAMERA].CallStatic("SetDebug", DEBUG_TYPE);
                m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_CAMERA].CallStatic("init", "Xiaowangwang");

                yield return new WaitForSeconds(INIT_DELAY_TIME);
            }

            m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_CAMERA].CallStatic("setCameraCallbackListener", cameraCB);
        }
    }

    public void OpenCamera()
    {
        if (CheckPlatform)
            m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_CAMERA].CallStatic("openCamera");
    }
}

#endif
