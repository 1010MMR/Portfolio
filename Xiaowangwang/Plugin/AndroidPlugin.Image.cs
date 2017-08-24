#if UNITY_ANDROID

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

#region ImagePickerCB
public class ImagePickerCB : AndroidJavaProxy
{
    public ImagePickerCB() : base("com.h26.plugin.image.IImageCallback") { }
    public ImagePickerCB(Action<string> OnGetImageComplete, Action OnGetImageCancel, Action OnGetImageFail)
        : base("com.h26.plugin.image.IImageCallback")
    {
        this.OnGetImageComplete = OnGetImageComplete;
        this.OnGetImageCancel = OnGetImageCancel;
        this.OnGetImageFail = OnGetImageFail;
    }

    private Action<string> OnGetImageComplete = null;
    private Action OnGetImageCancel = null;
    private Action OnGetImageFail = null;

    private void GetImageComplete(string paramString)
    {
        if (OnGetImageComplete != null)
            OnGetImageComplete(paramString);
    }

    private void GetImageCancel()
    {
        if (OnGetImageCancel != null)
            OnGetImageCancel();
    }

    private void GetImageFail()
    {
        if (OnGetImageFail != null)
            OnGetImageFail();
    }
}
#endregion

public partial class AndroidPlugin
{
    private const string MEDIA_STORE_CLASS = "android.provider.MediaStore$Images$Media";

    public IEnumerator InitImage(ImagePickerCB imageCB)
    {
        if (CheckPlatform)
        {
            if (CheckPluginLoadComplete(ANDROID_PLUGIN_TYPE.TYPE_IMAGE) == false)
            {
                m_initCheckList.Add(ANDROID_PLUGIN_TYPE.TYPE_IMAGE);

                m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMAGE].CallStatic("SetDebug", DEBUG_TYPE);
                m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMAGE].CallStatic("init");

                yield return new WaitForSeconds(INIT_DELAY_TIME);
            }

            m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMAGE].CallStatic("setImagePickerCallbackListener", imageCB);
        }
    }

    public void GetImage()
    {
        if (CheckPlatform)
            m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMAGE].CallStatic("getImage");
    }

    public void SaveImageToGallery(Texture2D texture, string title, string desc)
    {
        if (CheckPlatform)
        {
            try {
            string path = string.Format("{0}/{1}.jpg", GetSDCardGalleryPath(), title);
            System.IO.File.WriteAllBytes(path, texture.EncodeToJPG());

            texture = null;

            m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMAGE].CallStatic("updateGallery", path);
            } catch { }
        }
    }

    private string GetSDCardGalleryPath()
    {
        string path = null;
        if (CheckPlatform)
            path = m_adjObjTable[(int)ANDROID_PLUGIN_TYPE.TYPE_IMAGE].CallStatic<string>("getGalleryPath");

        return path;
    }
}

#endif