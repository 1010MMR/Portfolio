#if UNITY_IOS

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

#region ImagePickerCB
public class ImagePickerCB
{
    public ImagePickerCB(Action<Texture2D> OnImagePickedEvent, Action OnImageSaveFailed, Action OnImageSaveSuccess, Action<string> OnVideoPickedEvent)
    {
        this.OnImagePickedEvent = OnImagePickedEvent;
        this.OnImageSaveFailed = OnImageSaveFailed;
        this.OnImageSaveSuccess = OnImageSaveSuccess;
        this.OnVideoPickedEvent = OnVideoPickedEvent;
    }

    private Action<Texture2D> OnImagePickedEvent = null;
    private Action OnImageSaveFailed = null;
    private Action OnImageSaveSuccess = null;
    private Action<string> OnVideoPickedEvent = null;

    public void OnImagePickedEventCB(string paramString)
    {
		byte[] decode = System.Convert.FromBase64String(paramString);
        Texture2D texture = new Texture2D(1, 1);
		texture.LoadImage(decode);
		texture.hideFlags = HideFlags.DontSave;

        if (OnImagePickedEvent != null)
            OnImagePickedEvent(texture);
    }

    public void OnImageSaveFailedCB()
    {
        if (OnImageSaveFailed != null)
            OnImageSaveFailed();
    }

    public void OnImageSaveSuccessCB()
    {
        if (OnImageSaveSuccess != null)
            OnImageSaveSuccess();
    }

    public void OnVideoPickedEventCB(string paramString)
    {
        if (OnVideoPickedEvent != null)
            OnVideoPickedEvent(paramString);
    }
}
#endregion

public partial class iPhonePlugin 
{
	private const int  MAX_IMAGE_LOAD_SIZE = 512;
	private const  float JPEG_COMPRESSION_RATE = 0.8f;
    private const GALLERY_IMAGE_FORMAT IMAGE_FORMAT = GALLERY_IMAGE_FORMAT.PNG;

    [DllImport ("__Internal")]
	private static extern void _ISN_SaveToCameraRoll(string encodedMedia);
	[DllImport ("__Internal")]
	private static extern void _ISN_GetVideoPathFromAlbum();
	[DllImport ("__Internal")]
	private static extern void _ISN_PickImage(int source);
	[DllImport ("__Internal")]
	private static extern void _ISN_InitCameraAPI(float compressionRate, int maxSize, int encodingType);

    private ImagePickerCB m_imagePickerCB = null;
    public ImagePickerCB GetImagePickerCB { get { return m_imagePickerCB; } }

    public IEnumerator InitImage(ImagePickerCB imageCB)
    {
        if (CheckPlatform)
        {
            m_imagePickerCB = imageCB;

            if (CheckPluginLoadComplete(IOS_PLUGIN_TYPE.TYPE_IMAGE) == false)
            {
                m_initCheckList.Add(IOS_PLUGIN_TYPE.TYPE_IMAGE);
                _ISN_InitCameraAPI(JPEG_COMPRESSION_RATE, MAX_IMAGE_LOAD_SIZE, (int)IMAGE_FORMAT);

                yield return new WaitForSeconds(INIT_DELAY_TIME);
            }
        }
    }

    public void GetImage()
    {
        if( CheckPlatform)
            _ISN_PickImage((int)IOS_IMAGE_SOURCE.ALBUM);
    }

    public void SaveImageToGallery(Texture2D texture)
    {
        if (CheckPlatform)
        {
            try {
                byte[] txToByte = texture.EncodeToPNG();
                string byteString = Convert.ToBase64String(txToByte);

                texture = null;

                _ISN_SaveToCameraRoll(byteString);
            } catch (Exception e) { }
        }
    }
}

#endif
