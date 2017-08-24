using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if UNITY_EDITOR
using System.IO;
#endif

public class GUIManager_PhotoStudio : MonoBehaviour
{
    private const int MAX_DOG_ICON_COUNT = 3;

    private State_PhotoStudio m_state = null;

    private Transform m_transform = null;

    private Camera m_uiCamera = null;
    public Camera GetUICamera { get { return m_uiCamera; } }

    private Camera m_dogCamera = null;
    public Camera GetDogCamera { get { return m_dogCamera; } }

    private CDirection[] m_directionGroup = null;
    private enum DIRECTION_TYPE
    {
        TYPE_NONE = -1,

        TYPE_BACK,
        TYPE_POSITION_RELEASE,
        TYPE_DOG_ICON_GROUP,
        TYPE_MOTION_GROUP,
        TYPE_BUTTON_GROUP,
        TYPE_TOP_GROUP,

        TYPE_END,
    }

    #region UIDogIcon
    private class UIDogIcon
    {
        private GameObject m_obj = null;
        private Transform m_transform = null;
        public Transform GetTransform { get { return m_transform; } }

        private GameObject m_selectObj = null;

        private UISprite m_dogIcon = null;
        private UISprite m_background = null;

        private UISlider m_slider = null;

        private PhotoStudioCharacterInfo m_dogInfo = null;
        public PhotoStudioCharacterInfo GetDogInfo { get { return m_dogInfo; } }

        public UIDogIcon() { }
        public UIDogIcon(GameObject obj)
        {
            m_obj = obj;
            m_transform = obj.transform;

            #region Object
            m_selectObj = MonoBehaviour.Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_UI_Select02")) as GameObject;
            m_selectObj.transform.parent = m_transform;
            m_selectObj.transform.localScale = Vector3.one;
            m_selectObj.transform.localPosition = Vector3.zero; // new Vector3(-1.0f, 4.0f, 0);

            m_dogIcon = m_transform.FindChild("DogIcon").GetComponent<UISprite>();
            m_background = m_transform.FindChild("Background").GetComponent<UISprite>();

            m_slider = m_transform.FindChild("Slider").GetComponent<UISlider>();
            #endregion

            #region Callback
            m_obj.GetComponent<UIEventListener>().onClick = OnItemClick;
            #endregion

            SetActive(false);
        }

        public void UpdateIcon(PhotoStudioCharacterInfo info, bool isSelect = false)
        {
            m_dogInfo = info;

            #region Object
            bool isExists = info != null;

            m_dogIcon.gameObject.SetActive(isExists);
            m_slider.gameObject.SetActive(isExists);
            
            if (isExists)
            {
                m_dogIcon.spriteName = string.Format("Icon_{0}", WorldManager.instance.m_dataManager.m_SkinTexture.GetTexureName((uint)info.m_dog.m_skinIdx));
                m_dogIcon.MakePixelPerfect();

                m_slider.value = (float)WorldManager.instance.m_player.GetDogActivePower(info.m_dog) / (float)info.m_dog.m_maxAP;
            }

            m_background.spriteName = isExists ? "Btn_Magazine_DogProfile" : "Common_AddDogBtn";
            m_selectObj.SetActive(isSelect);
            #endregion

            SetActive(true);
        }

        #region Callback

        private void OnItemClick(GameObject obj)
        {
            State_PhotoStudio state = (State_PhotoStudio)StateManager.instance.m_curState;
            if (state.m_guiManager.CheckButtonActive)
            {
                if (m_dogInfo == null)
                {
                    DogListManager.instance.m_iValue.Clear();

                    List<PhotoStudioCharacterInfo> infoList = state.GetDogInfoList;
                    for (int i = 0; i < infoList.Count; i++)
                        DogListManager.instance.m_iValue.Add(infoList[i].m_dog.m_dogID);

                    DogListManager.instance.SetOpenWalkDogSelect(SelectDogListCB, true, DOG_SELECT_TYPE.PHOTOSTUDIO);
                }
                else
                {
                    if (m_dogInfo.Equals(state.GetSelectDogInfo) == false) 
                        state.SetSelectDog(m_dogInfo);
                }
            }
        }

        private void SelectDogListCB(int dogID)
        {
            if (dogID > -1)
            {
                State_PhotoStudio state = (State_PhotoStudio)StateManager.instance.m_curState;
                state.AddDogList(dogID);
            }
        }

        #endregion

        public void OnOffSelect(bool b)
        {
            m_selectObj.SetActive(b);
        }

        public void SetActive(bool b)
        {
            m_obj.SetActive(b);
        }

        ~UIDogIcon()
        {
            m_obj = null;
            m_transform = null;

            m_selectObj = null;

            m_dogIcon = null;
            m_background = null;

            m_slider = null;

            m_dogInfo = null;
        }
    }
    #endregion
    private UIDogIcon[] m_dogIconArray = null;

    #region UIBackgroundSelectPopup
    private class UIBackgroundSelectPopup
    {
        private const int MAX_BUTTON_COUNT = 3;

        private GUIManager_PhotoStudio m_guiManager = null;

        private GameObject m_obj = null;
        private Transform m_transform = null;
        private Transform m_window = null;

        private class UIButtonIcon
        {
            private GameObject m_obj = null;
            private Transform m_transform = null;

            private BoxCollider m_collider = null;

            private UILabel m_text = null;
            private UISprite m_background = null;

            private string m_linkTex = null;
            public string GetLinkTexture { get { return m_linkTex; } }

            public UIButtonIcon() { }
            public UIButtonIcon(GameObject obj, string linkTex)
            {
                m_obj = obj;
                m_transform = obj.transform;

                m_linkTex = linkTex;

                m_collider = m_obj.GetComponent<BoxCollider>();

                m_text = m_transform.FindChild("Text").GetComponent<UILabel>();
                m_background = m_transform.FindChild("Background").GetComponent<UISprite>();

                m_obj.GetComponent<UIEventListener>().onClick = OnThemeButton;
            }

            public void OnOff(string curTex)
            {
                bool isEnable = !m_linkTex.Equals(curTex);

                m_collider.enabled = isEnable;

                m_background.spriteName = isEnable ? "Btn_Orange" : "Btn_Gray";

                m_text.color = isEnable ? new Vector4(255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f, 255.0f / 255.0f) :
                                          new Vector4(166.0f / 255.0f, 162.0f / 255.0f, 162.0f / 255.0f, 255.0f / 255.0f);
                m_text.effectColor = isEnable ? new Vector4(129.0f / 255.0f, 75.0f / 255.0f, 36.0f / 255.0f, 255.0f / 255.0f) :
                                                new Vector4(81.0f / 255.0f, 81.0f / 255.0f, 81.0f / 255.0f, 255.0f / 255.0f);
            }

            private void OnThemeButton(GameObject obj)
            {
                Util.ButtonAnimation(obj);
                ((State_PhotoStudio)StateManager.instance.m_curState).m_guiManager.UpdateBackgroundTexture(m_linkTex);
            }

            ~UIButtonIcon()
            {
                m_obj = null;
                m_transform = null;

                m_collider = null;

                m_text = null;
                m_background = null;
            }
        }
        private UIButtonIcon[] m_buttonArray = null;

        public UIBackgroundSelectPopup() { }
        public UIBackgroundSelectPopup(GUIManager_PhotoStudio manager, GameObject obj)
        {
            m_guiManager = manager;

            m_obj = obj;
            m_transform = obj.transform;

            m_window = m_transform.FindChild("Window");

            #region Callback
            m_window.FindChild("CloseButton").GetComponent<UIEventListener>().onClick = OnCloseButton;

            m_buttonArray = new UIButtonIcon[MAX_BUTTON_COUNT];
            for (int i = 0; i < MAX_BUTTON_COUNT; i++)
                m_buttonArray[i] = new UIButtonIcon(m_window.FindChild(string.Format("Button_Group/Theme_Button_{0:D2}", i + 1)).gameObject, string.Format("Bg_PhotoStudio_{0:D2}", i + 1));
            #endregion

            SetActive(false);
        }

        public void OpenPopup(string textureName)
        {
            SetActive(true);
            UpdateButton(textureName);

            Hashtable hash = new Hashtable();
            hash.Add("amount", new Vector3(0.05f, 0.05f, 0f));
            hash.Add("time", 1f);
            hash.Add("ignoretimescale", true);
            iTween.PunchScale(m_window.gameObject, hash);
        }

        public void ClosePopup()
        {
            SetActive(false);
        }

        #region UI

        private void UpdateButton(string currentTexture)
        {
            for (int i = 0; i < m_buttonArray.Length; i++)
                m_buttonArray[i].OnOff(currentTexture);
        }

        #endregion

        #region Callback

        private void OnCloseButton(GameObject obj)
        {
            Util.ButtonAnimation(obj);
            ClosePopup();
        }

        private void OnCameraButton(GameObject obj)
        {
            Util.ButtonAnimation(obj);

            ((State_PhotoStudio)StateManager.instance.m_curState).OpenCamera();
            ClosePopup();
        }

        private void OnAlbumButton(GameObject obj)
        {
            Util.ButtonAnimation(obj);

            ((State_PhotoStudio)StateManager.instance.m_curState).OpenImageCrop();
            ClosePopup();
        }

        #endregion

        public void SetActive(bool b)
        {
            m_obj.SetActive(b);
        }

        ~UIBackgroundSelectPopup()
        {
            m_guiManager = null;

            m_obj = null;
            m_transform = null;
            m_window = null;

            m_buttonArray = null;
        }
    }
    #endregion
    private UIBackgroundSelectPopup m_uiBackgroundSelectPopup = null;

    private UIPhotoStudioResult m_uiPhotoStudioResult = null;
    public UIPhotoStudioResult GetUIPhotoStudioResult { get { return m_uiPhotoStudioResult; } }

    private UILabel m_titleLabel = null;

    public bool CheckButtonActive { get; set; }

    public void Init(State_PhotoStudio state)
    {
        m_state = state;

        m_transform = transform;

        m_uiCamera = m_transform.FindChild("UICamera").GetComponent<Camera>();
        m_dogCamera = GameObject.Find("DirectionCamera").GetComponent<Camera>();

        m_titleLabel = m_transform.FindChild("UI_S02_Panel/Top/Title_Group/Title/Title").GetComponent<UILabel>();

        InitDogIcon();
        InitButton();
        InitDirection();
        InitPopup();

        UpdateText("");
    }

    #region Init

    private void InitDogIcon()
    {
        m_dogIconArray = new UIDogIcon[MAX_DOG_ICON_COUNT];
        for (int i = 0; i < MAX_DOG_ICON_COUNT; i++)
            m_dogIconArray[i] = new UIDogIcon(m_transform.FindChild(string.Format("UI_S02_Panel/BottomLeft/Dog_Group/DogButton{0:D2}", i + 1)).gameObject);
    }

    private void InitButton()
    {
        m_transform.FindChild("UI_S02_Panel/TopLeft/BackButton").GetComponent<UIEventListener>().onClick = OnBackButton;
        m_transform.FindChild("UI_S02_Panel/TopRight/ButtonGroup/DogRemoveButton").GetComponent<UIEventListener>().onClick = OnDogReleaseButton;
        m_transform.FindChild("UI_S02_Panel/TopRight/ButtonGroup/ResetButton").GetComponent<UIEventListener>().onClick = OnDogResetButton;
        m_transform.FindChild("UI_S02_Panel/Bottom/Button_Group/Left_Button").GetComponent<UIEventListener>().onClick = OnDogAILeftButton;
        m_transform.FindChild("UI_S02_Panel/Bottom/Button_Group/Right_Button").GetComponent<UIEventListener>().onClick = OnDogAIRightButton;
        m_transform.FindChild("UI_S02_Panel/BottomRight/Button_Group/Confirm_Button").GetComponent<UIEventListener>().onClick = OnScreenPhotoButton;
        m_transform.FindChild("UI_S02_Panel/BottomRight/Button_Group/Change_Button").GetComponent<UIEventListener>().onClick = OnBackgroundSettionButton;
    }

    private void InitDirection()
    {
        string[] pathArray = {"UI_S02_Panel/TopLeft/BackButton", "UI_S02_Panel/TopRight/ButtonGroup", "UI_S02_Panel/BottomLeft/Dog_Group", 
                            "UI_S02_Panel/Bottom/Button_Group", "UI_S02_Panel/BottomRight/Button_Group", "UI_S02_Panel/Top/Title_Group" };
        m_directionGroup = new CDirection[(int)DIRECTION_TYPE.TYPE_END];
        for (int i = 0; i < m_directionGroup.Length; i++)
            m_directionGroup[i] = m_transform.FindChild(pathArray[i]).GetComponent<CDirection>();

        OnOffUI(false, true);
    }

    private void InitPopup()
    {
        m_uiBackgroundSelectPopup = new UIBackgroundSelectPopup(this, m_transform.FindChild("UI_S07_Panel").gameObject);

        m_uiPhotoStudioResult = m_transform.FindChild("UI_S16_Panel").GetComponent<UIPhotoStudioResult>();
        m_uiPhotoStudioResult.Init();
    }

    #endregion

    public void UpdateGUI()
    {
        PhotoStudioCharacterInfo selectDogInfo = m_state.GetSelectDogInfo;
        List<PhotoStudioCharacterInfo> dogInfoList = m_state.GetDogInfoList;

        for (int i = 0; i < m_dogIconArray.Length; i++)
        {
            if (i.Equals(dogInfoList.Count)) m_dogIconArray[i].UpdateIcon(null);
            else if (i < dogInfoList.Count) m_dogIconArray[i].UpdateIcon(dogInfoList[i], selectDogInfo.Equals(dogInfoList[i]));
            else m_dogIconArray[i].SetActive(false);
        }

        OnOffUI(DIRECTION_TYPE.TYPE_BACK, true);
        OnOffUI(DIRECTION_TYPE.TYPE_DOG_ICON_GROUP, true);
        OnOffUI(DIRECTION_TYPE.TYPE_BUTTON_GROUP, true);
        OnOffUI(DIRECTION_TYPE.TYPE_TOP_GROUP, true);

        OnOffUI(DIRECTION_TYPE.TYPE_MOTION_GROUP, selectDogInfo != null);
        OnOffUI(DIRECTION_TYPE.TYPE_POSITION_RELEASE, selectDogInfo != null);

        CheckButtonActive = true;
    }

    #region Title

    public void UpdateText(string text)
    {
        m_titleLabel.text = text;
    }

    #endregion

    #region Callback

    private void OnBackButton(GameObject obj)
    {
        if (CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            StateManager.instance.SetTransitionBack();
        }
    }

    private void OnDogReleaseButton(GameObject obj)
    {
        if (CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            if (m_state.GetSelectDogInfo != null)
                m_state.RemoveDogList(m_state.GetSelectDogInfo);
        }
    }

    private void OnDogResetButton(GameObject obj)
    {
        if (CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            m_state.ResetDogPosition();
        }
    }

    private void OnDogAILeftButton(GameObject obj)
    {
        if (CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            if (m_state.GetSelectDogInfo != null)
                m_state.UpdateDogAI(m_state.GetSelectDogInfo, false);
        }
    }

    private void OnDogAIRightButton(GameObject obj)
    {
        if (CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            if (m_state.GetSelectDogInfo != null)
                m_state.UpdateDogAI(m_state.GetSelectDogInfo, true);
        }
    }

    private void OnScreenPhotoButton(GameObject obj)
    {
        if (CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            StartCoroutine("OnScreenShot");
        }
    }

    private void OnBackgroundSettionButton(GameObject obj)
    {
        if (CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            m_uiBackgroundSelectPopup.OpenPopup(m_state.GetBackgroundTextureName);
        }
    }

    #endregion

    #region Direction

    public void OnOffUI(bool b, bool isInstantly = false)
    {
        for (int i = 0; i < m_directionGroup.Length; i++)
            OnOffUI((DIRECTION_TYPE)i, b, isInstantly);
    }

    private void OnOffUI(DIRECTION_TYPE type, bool b, bool isInstantly = false)
    {
        int index = 0;
        switch (type)
        {
            case DIRECTION_TYPE.TYPE_BACK: index = b ? 100000169 : 100000170; break;
            case DIRECTION_TYPE.TYPE_POSITION_RELEASE: index = b ? 100000034 : 100000035; break;
            case DIRECTION_TYPE.TYPE_TOP_GROUP: index = b ? 100000018 : 100000019; break;
            case DIRECTION_TYPE.TYPE_DOG_ICON_GROUP:
            case DIRECTION_TYPE.TYPE_MOTION_GROUP:
            case DIRECTION_TYPE.TYPE_BUTTON_GROUP: index = b ? 100000148 : 100000173; break;
        }

        if (isInstantly == false) m_directionGroup[(int)type].SetInit(index, true);
        else m_directionGroup[(int)type].ResetToBeginning(index);
    }

    #endregion

    #region ScreenShot

    private IEnumerator OnScreenShot()
    {
        CheckButtonActive = false;
        m_state.CheckTouchActive = false;

        OnOffUI(false);
        m_state.SetDogAnimation(false);

        yield return new WaitForSeconds(0.75f);

        Texture2D screenShot = null;

        RenderImageCallback renderImage = m_state.GetCurCamera().gameObject.AddComponent<RenderImageCallback>();
        bool isDepthOnly = m_state.GetCurCamera().gameObject.GetComponent<Camera>().clearFlags.Equals(CameraClearFlags.Depth);
        renderImage.Init(isDepthOnly, delegate (Texture2D texture) {
            screenShot = texture;
        });

        while (screenShot == null)
            yield return null;

        m_uiPhotoStudioResult.SetActive(true);
        yield return StartCoroutine(m_uiPhotoStudioResult.OnDirectionWindow(screenShot));

        CheckButtonActive = true;
    }

    public void OnScreenShotResultPopupClose()
    {
        CheckButtonActive = true;

        m_state.CheckTouchActive = true;
        m_state.SetDogAnimation(true);

        UpdateGUI();
    }

    public void OnScreenShotResultPopupClose(Texture2D texture)
    {
        StartCoroutine("OnCloseScreenShotResultPopup", texture);
    }

    private IEnumerator OnCloseScreenShotResultPopup(Texture2D texture)
    {
        CheckButtonActive = false;

        yield return StartCoroutine(PluginManager.instance.InitImage(null));

#if UNITY_EDITOR
        string path = string.Format("{0}/Pictures", System.Environment.CurrentDirectory);
        DirectoryInfo dInfo = new DirectoryInfo(path);
        if (dInfo.Exists == false)
            dInfo.Create();

        File.WriteAllBytes(string.Format("{0}/Xiaowangwang_{1}.png", path, Util.GetNowGameTime()), texture.EncodeToPNG());
#else
        PluginManager.instance.SaveImageToGallery(texture, string.Format("Xiaowangwang_{0}", Util.GetNowGameTime()), "");
#endif

        MsgBox.instance.OpenMsgToast(533);

        yield return new WaitForSeconds(2.0f);
        m_uiPhotoStudioResult.ClosePopup();

        texture = null;

        CheckButtonActive = true;
        m_state.CheckTouchActive = true;
        m_state.SetDogAnimation(true);

        UpdateGUI();
    }

    #endregion

    #region Background

    public void UpdateBackgroundTexture(string textureName)
    {
        m_uiBackgroundSelectPopup.ClosePopup();
        if (textureName.Equals(m_state.GetBackgroundTextureName) == false)
        {
            Texture texture = AssetBundleEx.Load<Texture>(string.Format("[Textures]/[PhotoStudio]/{0}", textureName));
            StartCoroutine("OnUpdateBackgroundTexture", texture);

            texture = null;
        }
    }

    private IEnumerator OnUpdateBackgroundTexture(Texture texture)
    {
        CheckButtonActive = false;

        float delayTime = LoadingManager.instance.StartScreenOut(SCREEN_OUT_TYPE.SCREEN_CURTAIN);
        yield return new WaitForSeconds(delayTime);

        m_state.UpdateBackgroundTexture(texture);
        texture = null;

        System.GC.Collect();
        yield return Resources.UnloadUnusedAssets();

        delayTime = LoadingManager.instance.StartScreenIn();
        yield return new WaitForSeconds(delayTime);

        CheckButtonActive = true;
    }

    #endregion

    public void Release()
    {
        m_state = null;
        m_transform = null;

        m_uiCamera = null;
        m_dogCamera = null;

        m_directionGroup = null;
        m_dogIconArray = null;

        m_uiBackgroundSelectPopup = null;

        if (m_uiPhotoStudioResult != null)
            m_uiPhotoStudioResult.Release();
        m_uiPhotoStudioResult = null;

        m_titleLabel = null;
    }
}
