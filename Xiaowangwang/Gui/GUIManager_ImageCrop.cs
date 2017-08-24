using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
#endif

/// <summary>
/// <para>name : ImageCropPopup</para>
/// <para>describe : 이미지를 수정, 잘라내는 UI.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
public class GUIManager_ImageCrop : MonoBehaviour
{
    private readonly float SCREEN_FACTOR = 1280.0f / 720.0f;
    private readonly Vector2 CROP_SIZE = new Vector2(150.0f, 150.0f);

#if UNITY_EDITOR
#elif UNITY_ANDROID
    private readonly float CAMERA_ZOOM_SPEED = 0.005f;
#elif UNITY_IOS
    private readonly float CAMERA_ZOOM_SPEED = 0.001f;
#endif

#if UNITY_EDITOR
    private readonly float ZOOM_SCALE = 50.0f;
#else
    private readonly float ZOOM_SCALE = 500.0f;
#endif

    private State_ImageCrop m_state = null;

    private Transform m_transform = null;
    private UITexture m_imageTx = null;

    private Transform m_lineGroup = null;

    private Camera m_camera = null;

    private List<Transform> m_uiGroup = null;

    private Vector3 m_startPos = Vector3.zero;
    private Vector3 m_endPos = Vector3.zero;

    private float m_zoomRatio = 0.0f;
    private bool m_isScaleHeight = false;

    private TOUCH_STATUS m_touchStatus = TOUCH_STATUS.TOUCH_TOUCH;

    private bool m_isButtonActive = false;
    public bool CheckButtonActive { get { return m_isButtonActive; } }

    public void Init(State_ImageCrop state)
    {
        m_state = state;

        m_transform = transform;
        
        m_imageTx = m_transform.FindChild("Anchor_Center/Texture").GetComponent<UITexture>();
        m_camera = m_transform.FindChild("Camera").GetComponent<Camera>();

        m_lineGroup = m_transform.FindChild("Anchor_Center/Background/LineGroup");

        m_uiGroup = new List<Transform>();

        string[] pathArray = { "Anchor_TopRight/BackButton", "Anchor_BotRight/SaveButton" };
        UIEventListener.VoidDelegate[] delegateArray = { OnClose, OnSave };
        for (int i = 0; i < pathArray.Length; i++)
        {
            m_uiGroup.Add(m_transform.FindChild(pathArray[i]));
            m_uiGroup[i].GetComponent<UIEventListener>().onClick = delegateArray[i];
        }
    }

    #region Window

    void LateUpdate()
    {
        if (m_isButtonActive && m_transform != null)
        {
#if UNITY_EDITOR
            MoveObject();
            ZoomObject();
#else
            switch(Input.touchCount)
            {
                case 0:
                case 1:
                    MoveObject();
                    break;

                case 2:
                    if(Input.GetTouch(0).phase.Equals(TouchPhase.Moved) && Input.GetTouch(1).phase.Equals(TouchPhase.Moved))
                        ZoomObject();
                    break;
            }
#endif
        }
    }

    #region Object

    private void MoveObject()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m_touchStatus = TOUCH_STATUS.TOUCH_TOUCH;
            m_startPos = Input.mousePosition;
        }

        if (Input.GetMouseButton(0) && m_touchStatus.Equals(TOUCH_STATUS.TOUCH_TOUCH))
        {
            m_endPos = Input.mousePosition;

            if (m_startPos != m_endPos)
            {
                Vector3 temp = m_imageTx.transform.localPosition;
                Vector3 movePos = m_startPos - m_endPos;

                temp -= movePos;
                m_imageTx.transform.localPosition = Vector3.Lerp(m_imageTx.transform.localPosition, temp, Time.smoothDeltaTime * 50.0f);

                m_startPos = m_endPos;
            }
        }
    }

    private void ZoomObject()
    {
        float zoomValue = 0;

#if UNITY_EDITOR
        zoomValue = Input.GetAxis("Mouse ScrollWheel");
#else
        if(Input.touchCount.Equals(2))
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;
            zoomValue = deltaMagnitudeDiff * CAMERA_ZOOM_SPEED;
        }
#endif

        if (zoomValue.Equals(0) == false)
        {
            m_touchStatus = TOUCH_STATUS.TOUCH_TOUCHZOOM;

            if (m_isScaleHeight)
            {
                m_imageTx.height = Mathf.Clamp(m_imageTx.height - (int)(zoomValue * ZOOM_SCALE), 200, 3000);
                m_imageTx.width = Mathf.RoundToInt(m_imageTx.height * m_zoomRatio);
            }

            else
            {
                m_imageTx.width = Mathf.Clamp(m_imageTx.width - (int)(zoomValue * ZOOM_SCALE), 200, 3000);
                m_imageTx.height = Mathf.RoundToInt(m_imageTx.width * m_zoomRatio);
            }

            StopCoroutine("WaitForZoom");
            StartCoroutine("WaitForZoom");
        }
    }

    private IEnumerator WaitForZoom()
    {
        yield return new WaitForSeconds(0.1f);
        m_touchStatus = TOUCH_STATUS.TOUCH_NONE;
    }

    #endregion

    #endregion

    public void UpdateGUI(Texture2D texture)
    {
        m_isButtonActive = true;

        SetLine();
        OnOffUI(true);

        #region Texture
        if (texture != null)
        {
            m_imageTx.transform.localPosition = Vector3.zero;

            m_imageTx.mainTexture = texture;
            m_imageTx.width = texture.width;
            m_imageTx.height = texture.height;

            m_isScaleHeight = texture.width >= texture.height;
            m_zoomRatio = m_isScaleHeight ? ((float)texture.width / (float)texture.height) : ((float)texture.height / (float)texture.width);
        }
        #endregion
    }

    #region Callback

    private void OnClose(GameObject obj)
    {
        if (m_isButtonActive)
        {
            m_isButtonActive = false;
            StateManager.instance.SetTransition(m_state.GetBeforeStateType);
        }
    }

    private void OnSave(GameObject obj)
    {
        if (m_isButtonActive)
            StartCoroutine("WaitForScreenSave");
    }

    private IEnumerator WaitForScreenSave()
    {
        yield return new WaitForSeconds(ScreenOut.instance.StartScreenOut(SCREEN_OUT_TYPE.SCREEN_WHITE_OUT));
        OnOffUI(false);
        yield return null;

        Vector2 renderSize = new Vector2(400, 400);

        m_isButtonActive = false;

        float cameraFactor = (float)Screen.width / (float)Screen.height;
        m_camera.orthographicSize = renderSize.y / (720.0f * (SCREEN_FACTOR / cameraFactor));

        Texture2D screenShot = new Texture2D((int)renderSize.x, (int)renderSize.y, TextureFormat.RGB24, true);

        RenderTexture rt = new RenderTexture((int)renderSize.x, (int)renderSize.y, 0);
        m_camera.targetTexture = rt;
        m_camera.Render();

        RenderTexture.active = rt;

        screenShot.ReadPixels(new Rect(0, 0, (int)renderSize.x, (int)renderSize.y), 0, 0);
        screenShot.Apply();

        m_camera.targetTexture = null;
        RenderTexture.active = null;

        Destroy(rt);

        m_imageTx.mainTexture = screenShot;
        m_imageTx.transform.localPosition = Vector3.zero;
        m_imageTx.width = (int)renderSize.x;
        m_imageTx.height = (int)renderSize.y;

        m_camera.orthographicSize = 1.0f;

        OnOffUI(true);
        yield return new WaitForSeconds(ScreenOut.instance.StartScreenIn());

        try
        {
#if UNITY_EDITOR
            string path = string.Format("{0}/Pictures", System.Environment.CurrentDirectory);
            DirectoryInfo dInfo = new DirectoryInfo(path);
            if (dInfo.Exists == false)
                dInfo.Create();

            File.WriteAllBytes(string.Format("{0}/Xiaowangwang_Profile_{1}.png", path, Util.GetNowGameTime()), screenShot.EncodeToPNG());
#else
        PluginManager.instance.SaveImageToGallery(screenShot, string.Format("Xiaowangwang_Profile_{0}", Util.GetNowGameTime()), "");
#endif
        } catch { }

        MsgBox.instance.OpenMsgToast(533);

        switch (m_state.GetBeforeStateType)
        {
            case STATE_TYPE.STATE_ROOM:
            case STATE_TYPE.STATE_VILLAGE:
                m_state.SendUploadProfile(screenShot);
                break;

            case STATE_TYPE.STATE_PHOTOSTUDIO:
                break;
        }

        screenShot = null;
        LoadingManager.instance.SetActiveProgressBar(true, true);
    }

    #endregion

    #region Util

    private void SetLine()
    {
        m_lineGroup.FindChild("LeftLine").localPosition = Vector3.left * 200.0f;
        m_lineGroup.FindChild("RightLine").localPosition = Vector3.right * 200.0f;
        m_lineGroup.FindChild("TopLine").localPosition = Vector3.up * 200.0f;
        m_lineGroup.FindChild("BotLine").localPosition = Vector3.down * 200.0f;
    }

    private void OnOffUI(bool b)
    {
        for (int i = 0; i < m_uiGroup.Count; i++)
            m_uiGroup[i].gameObject.SetActive(b);
        m_lineGroup.gameObject.SetActive(b);
    }

    #endregion

    public void Release()
    {
        m_transform = null;
        m_imageTx = null;

        m_lineGroup = null;
        m_camera = null;
        m_uiGroup = null;
    }
}
