using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIPhotoStudioImageFilter
{
    private UIPhotoStudioResult m_popup = null;

    private GameObject m_obj = null;

    private Transform m_window = null;
    private Transform m_transform = null;

    private UIScrollView m_scrollView = null;
    private UIGrid m_scrollGrid = null;
    private UIScrollBar m_scrollBar = null;

    private GameObject m_scrollItem = null;

	#region UIIcon
    private class UIIcon
    {
        private GameObject m_obj = null;
        private Transform m_transform = null;

        private UITexture m_icon = null;
        private UILabel m_name = null;
        private GameObject m_select = null;

        private UIPhotoStudioImageFilter.OnScrollItemClick m_callback = null;

        private ImageFilterInfo m_info = null;

        public UIIcon() { }
        public UIIcon(GameObject obj, ImageFilterInfo info, UIPhotoStudioImageFilter.OnScrollItemClick clickCB)
        {
            m_obj = obj;
            m_transform = obj.transform;

            m_info = info;

            m_callback = clickCB;

            m_icon = m_transform.FindChild("Icon").GetComponent<UITexture>();
            m_name = m_transform.FindChild("Text").GetComponent<UILabel>();
            m_select = m_transform.FindChild("Select").gameObject;

            m_obj.GetComponent<UIEventListener>().onClick = OnClick;

            InitIcon();
        }

        private void InitIcon()
        {
            m_icon.material = MonoBehaviour.Instantiate(AssetBundleEx.Load<Material>("[Materials]/[PhotoStudio]/Mat_PhotoStudio_FilterIcon")) as Material;
            WorldManager.instance.m_dataManager.m_imageFilterData.SetImageFilter(m_icon, m_info);

            m_name.text = m_info.name;
        }

        public void UpdateItem(int index)
        {
            m_select.gameObject.SetActive(m_info.index.Equals(index));
        }

        private void OnClick(GameObject obj)
        {
            if (m_callback != null)
                m_callback(m_info);
        }

        public void SetActive(bool b)
        {
            m_obj.SetActive(b);
        }

        ~UIIcon()
        {
            m_obj = null;
            m_transform = null;

            m_icon = null;
            m_name = null;
            m_select = null;

            m_callback = null;

            m_info = null;
        }
    }
	#endregion
    private List<UIIcon> m_scrollItemList = null;

    public delegate void OnScrollItemClick(ImageFilterInfo info);

    public UIPhotoStudioImageFilter() { }
    public UIPhotoStudioImageFilter(UIPhotoStudioResult popup, GameObject obj)
    {
        m_popup = popup;

        m_obj = obj;
        m_transform = obj.transform;
        m_window = m_transform.FindChild("Anchor/Window");

        InitScroll();
        InitCallback();

        ClosePopup();
    }

    private void InitScroll()
    {
        m_scrollView = m_window.FindChild("ScrollWindow/ScrollView").GetComponent<UIScrollView>();
        m_scrollGrid = m_scrollView.transform.FindChild("Grid").GetComponent<UIGrid>();
        m_scrollBar = m_window.transform.FindChild("ScrollWindow/ScrollBar").GetComponent<UIScrollBar>();

        m_scrollItem = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/UIImageFilterIcon");

        m_scrollItemList = new List<UIIcon>();

        List<ImageFilterInfo> infoList = null;
        if (WorldManager.instance.m_dataManager.m_imageFilterData.GetImageFilterList(out infoList))
        {
            for (int i = 0; i < infoList.Count; i++)
            {
                GameObject obj = MonoBehaviour.Instantiate(m_scrollItem) as GameObject;
                obj.transform.parent = m_scrollGrid.transform;
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localScale = Vector3.one;

                m_scrollItemList.Add(new UIIcon(obj, infoList[i], new OnScrollItemClick(OnScrollItemClickCB)));
            }
        }
    }

    private void InitCallback()
    {
        m_window.gameObject.AddComponent<UIEventListener>().onClick = OnClose;
    }

    public void OpenPopup()
    {
        SetActive(true);
        UpdateScrollView();

        Hashtable hash = new Hashtable();
        hash.Add("amount", new Vector3(0.05f, 0.05f, 0f));
        hash.Add("time", 1f);
        hash.Add("ignoretimescale", true);
        iTween.PunchScale(m_window.gameObject, hash);

        SoundManager.instance.PlayAudioClip("UI_PopupOpen");
    }

    public void ClosePopup()
    {
        SetActive(false);
    }

	#region Callback

    private void OnClose(GameObject obj)
    {
        ClosePopup();
    }

    private void OnScrollItemClickCB(ImageFilterInfo info)
    {
        m_popup.UpdateTexture(info);

        for (int i = 0; i < m_scrollItemList.Count; i++)
            m_scrollItemList[i].UpdateItem(info.index);
    }

    #endregion

    #region Scroll

    private void UpdateScrollView()
    {
        for (int i = 0; i < m_scrollItemList.Count; i++)
            m_scrollItemList[i].UpdateItem(m_popup.ImageFilterInfo.index);

        UpdateItemPosition();
    }

    private void UpdateItemPosition()
    {
        EnableScrollView();

        m_scrollGrid.Reposition();
        m_scrollView.ResetPosition();
    }

    private void EnableScrollView()
    {
        if(m_scrollView.enabled == false)
            m_scrollView.enabled = true;
    }

    private void ReleaseAllItem()
    {
        for(int i = 0; i < m_scrollItemList.Count; i++)
            m_scrollItemList[i].SetActive(false);
    }

    #endregion

    public void SetActive(bool b)
    {
        m_obj.SetActive(b);
    }

    ~UIPhotoStudioImageFilter()
    {
        m_popup = null;

        m_obj = null;

        m_window = null;
        m_transform = null;

        m_scrollView = null;
        m_scrollGrid = null;
        m_scrollBar = null;

        m_scrollItem = null;

        m_scrollItemList = null;
    }
}

public class PhotoStudioWorkTimeline
{
    private UIPhotoStudioResult m_popup = null;

    public class Timeline
    {
        public PHOTOSTUDIO_WORK_TYPE m_type = PHOTOSTUDIO_WORK_TYPE.TYPE_NONE;

        public PhotoStudioEmojiItem m_item = null;

        public Vector3 m_position = Vector3.zero;
        public Quaternion m_rotate = Quaternion.identity;

        public string m_spriteName = null;
        public int m_spriteSize = 0;
        public int m_depth = 0;

        public ImageFilterInfo m_filter = null;

        public Timeline() { }
        public Timeline(Timeline item)
        {            
            this.m_type = item.m_type;

            this.m_item = item.m_item;

            this.m_position = item.m_position;
            this.m_rotate = item.m_rotate;

            this.m_spriteName = item.m_spriteName;
            this.m_spriteSize = item.m_spriteSize;
            this.m_depth = item.m_depth;

            this.m_filter = item.m_filter;
        }

        public Timeline(PHOTOSTUDIO_WORK_TYPE type, ImageFilterInfo filter)
        {
            this.m_type = type;
            this.m_filter = filter;
        }

        public Timeline(PHOTOSTUDIO_WORK_TYPE type, PhotoStudioEmojiItem item)
        {
            this.m_type = type;

            this.m_item = item;

            this.m_position = item.transform.localPosition;
            this.m_rotate = item.GetIcon.transform.localRotation;

            this.m_spriteName = item.GetIcon.spriteName;
            this.m_spriteSize = Mathf.Max(item.GetIcon.width, item.GetIcon.height);
            this.m_depth = item.GetIcon.depth;
        }

        public Timeline(PHOTOSTUDIO_WORK_TYPE type, PhotoStudioEmojiItem item, Vector3 position, Quaternion rotate, 
                        string spriteName, int spriteSize, int depth, ImageFilterInfo filter)
        {
            this.m_type = type;

            this.m_item = item;

            this.m_position = position;
            this.m_rotate = rotate;

            this.m_spriteName = spriteName;
            this.m_spriteSize = spriteSize;
            this.m_depth = depth;

            this.m_filter = filter;
        }

        ~Timeline()
        {
            m_item = null;
            m_filter = null;
        }
    }
    private Stack<Timeline> m_beforeTimeline = null;
    private Stack<Timeline> m_afterTimeline = null;

    public bool CheckBeforeTimeline { get { return !m_beforeTimeline.Count.Equals(0); } }
    public bool CheckAfterTimeline { get { return !m_afterTimeline.Count.Equals(0); } }

    public PhotoStudioWorkTimeline() { }
    public PhotoStudioWorkTimeline(UIPhotoStudioResult popup)
    {
        m_popup = popup;

        m_beforeTimeline = new Stack<Timeline>();
        m_afterTimeline = new Stack<Timeline>();
    }

    public void Release()
    {
        ReleaseBeforeTimeline();
        ReleaseAfterTimeline();
    }

    public void ReleaseBeforeTimeline()
    {
        m_beforeTimeline.Clear();
        m_popup.GetUIWindow.UpdateUI();
    }

    public void ReleaseAfterTimeline()
    {
        m_afterTimeline.Clear();
        m_popup.GetUIWindow.UpdateUI();
    }

    public void AddBeforeTimeline(Timeline item)
    {
        m_beforeTimeline.Push(item);
        m_popup.GetUIWindow.UpdateUI();
    }

    public Timeline GetBeforeTimeline()
    {
        Timeline item = m_beforeTimeline.Pop();
        m_popup.GetUIWindow.UpdateUI();

        return item;
    }

    public void AddAfterTimeline(Timeline item)
    {
        m_afterTimeline.Push(item);
        m_popup.GetUIWindow.UpdateUI();
    }

    public Timeline GetAfterTimeline()
    {
        Timeline item = m_afterTimeline.Pop();
        m_popup.GetUIWindow.UpdateUI();

        return item;
    }

    ~PhotoStudioWorkTimeline()
    {
        m_popup = null;

        m_beforeTimeline = null;
        m_afterTimeline = null;
    }
}

public class UIPhotoStudioResult : MonoBehaviour
{
    private readonly Vector3 RENDER_SIZE = new Vector2(1280, 720);
    private readonly float SCREEN_FACTOR = 1280.0f / 720.0f;
    private readonly float BASE_SCALE = 0.75f;

    private const int MAX_EMOJI_COUNT = 20;

    private State_PhotoStudio m_state = null;
    private GUIManager_PhotoStudio m_guiManager = null;

    private Transform m_transform = null;

    private Transform m_window = null;
    private Transform m_emojiItemGroup = null;

    public class UIWindow
    {
        private UIPhotoStudioResult m_popup = null;

        private GameObject m_obj = null;

        private Transform m_transform = null;
        public Transform GetTransform { get { return m_transform; } }

        private Transform m_topButtonGroup = null;
        private CDirection m_topButtonGroupDir = null;

        private Transform m_botButtonGroup = null;
        private CDirection m_botButtonGroupDir = null;

        private UIPhotoStudioImageFilter m_uiImageFilter = null;
        private EmojiPopup m_uiEmojiPopup = null;

        private UISprite[] m_spriteArray = null;
        private enum SPRITE_TYPE
        {
            TYPE_NONE = -1,

            TYPE_BEFORE,
            TYPE_AFTER,

            TYPE_END,
        }

        private GameObject[] m_buttonArray = null;
        private enum BUTTON_TYPE
        {
           TYPE_NONE = -1,

            TYPE_BEFORE,
            TYPE_AFTER,

            TYPE_SAVE,
            TYPE_CANCEL,
            TYPE_EMOJI,
            TYPE_FILTER,

            TYPE_CONFIRM,

            TYPE_END,
        }

        public UIWindow() { }
        public UIWindow(UIPhotoStudioResult popup, GameObject obj)
        {
            m_popup = popup;

            m_obj = obj;
            m_transform = obj.transform;

            m_topButtonGroup = m_transform.FindChild("Anchor_Top/Button_Group");
            m_topButtonGroupDir = m_topButtonGroup.GetComponent<CDirection>();

            m_botButtonGroup = m_transform.FindChild("Anchor_Bottom/Button_Group");
            m_botButtonGroupDir = m_botButtonGroup.GetComponent<CDirection>();

            m_uiImageFilter = new UIPhotoStudioImageFilter(popup, m_transform.FindChild("Anchor_Bottom/UIPhotoStudioImageFilter").gameObject);
            m_uiEmojiPopup = m_transform.FindChild("Anchor_Bottom/UIPhotoStudioEmoji").GetComponent<EmojiPopup>();

            m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];
            string[] spritePathArray = { "Anchor_Top/Button_Group/BeforeButton", "Anchor_Top/Button_Group/AfterButton" };
            for (int i = 0; i < spritePathArray.Length; i++)
                m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();

            m_buttonArray = new GameObject[(int)BUTTON_TYPE.TYPE_END];
            string[] objectPathArray = { "Anchor_Top/Button_Group/BeforeButton", "Anchor_Top/Button_Group/AfterButton", "Anchor_Bottom/Button_Group/SaveButton", 
                                         "Anchor_Bottom/Button_Group/CancelButton", "Anchor_Bottom/Button_Group/EmojiButton", "Anchor_Bottom/Button_Group/FilterButton", 
                                         "Anchor_Bottom/Button_Group/ConfirmButton" };
            UIEventListener.VoidDelegate[] eventArray = { popup.OnBeforeButton, popup.OnAfterButton, popup.OnSaveButton, popup.OnCloseButton, popup.OnEmojiButton, 
                                                          popup.OnFilterButton, popup.OnConfirmButton };

            for (int i = 0; i < objectPathArray.Length; i++)
            {
                m_buttonArray[i] = m_transform.FindChild(objectPathArray[i]).gameObject;
                m_buttonArray[i].GetComponent<UIEventListener>().onClick = eventArray[i];
            }
        }

        #region Update

        public void UpdateUI(bool isComplete = false)
        {
            Color[] iconColorArray = { new Color(63.0f / 255.0f, 42.0f / 255.0f, 34.0f / 255.0f), new Color(159.0f / 255.0f, 159.0f / 255.0f, 159.0f / 255.0f) };
            m_spriteArray[(int)SPRITE_TYPE.TYPE_BEFORE].color = iconColorArray[m_popup.GetTimeline.CheckBeforeTimeline ? 0 : 1];
            m_spriteArray[(int)SPRITE_TYPE.TYPE_AFTER].color = iconColorArray[m_popup.GetTimeline.CheckAfterTimeline ? 0 : 1];

            SwitchBotButtonGroup(isComplete);
        }

        public void SwitchBotButtonGroup(bool isComplete)
        {
            m_buttonArray[(int)BUTTON_TYPE.TYPE_CANCEL].SetActive(!isComplete);
            m_buttonArray[(int)BUTTON_TYPE.TYPE_SAVE].SetActive(!isComplete);
            m_buttonArray[(int)BUTTON_TYPE.TYPE_EMOJI].SetActive(!isComplete);
            m_buttonArray[(int)BUTTON_TYPE.TYPE_FILTER].SetActive(!isComplete);

            m_buttonArray[(int)BUTTON_TYPE.TYPE_CONFIRM].SetActive(isComplete);
        }

        #endregion

        #region Callback

        public void OpenImageFilter()
        {
            m_uiImageFilter.OpenPopup();
        }

        public void CloseImageFilter()
        {
            m_uiImageFilter.ClosePopup();
        }

        public void OpenEmojiPopup()
        {
            m_uiEmojiPopup.OpenPopup();
        }

        public void CloseEmojiPopup()
        {
            m_uiEmojiPopup.ClosePopup();
        }

        #endregion

        #region OnOff

        public void OnOffButtonGroup(bool b, bool isInstantly = false)
        {
            OnOffTopButtonGroup(b, isInstantly);
            OnOffBotButtonGroup(b, isInstantly);
        }

        public void OnOffTopButtonGroup(bool b, bool isInstantly = false)
        {
            iTween.Stop(m_topButtonGroupDir.gameObject);

            if (isInstantly) m_topButtonGroupDir.ResetToBeginning(b ? 100000054 : 100000019);
            else m_topButtonGroupDir.SetInit(b ? 100000054 : 100000019, true);
        }

        public void OnOffBotButtonGroup(bool b, bool isInstantly = false)
        {
            iTween.Stop(m_botButtonGroupDir.gameObject);

            if (isInstantly) m_botButtonGroupDir.ResetToBeginning(b ? 100000054 : 100000055);
            else m_botButtonGroupDir.SetInit(b ? 100000054 : 100000055, true);
        }

        #endregion

        public void SetActive(bool b)
        {
            m_obj.SetActive(b);
        }

        ~UIWindow()
        {
            m_popup = null;
            m_obj = null;
            m_transform = null;

            m_topButtonGroup = null;
            m_topButtonGroupDir = null;

            m_botButtonGroup = null;
            m_botButtonGroupDir = null;

            m_uiImageFilter = null;
            m_uiEmojiPopup = null;

            m_spriteArray = null;

            m_buttonArray = null;
        }
    }
    private UIWindow m_uiWindow = null;
    public UIWindow GetUIWindow { get { return m_uiWindow; } }

    private UITexture m_uiTexture = null;
    private UISprite m_edge = null;

    private Material m_baseMaterial = null;

    private Texture2D m_screenShot = null;
    public Texture2D GetScreenShot { get { return m_screenShot; } }

    private float m_orthographicSize = 0.0f;

    private ImageFilterInfo m_selectFilter = null;
    public ImageFilterInfo ImageFilterInfo { get { return m_selectFilter; } }

    private PhotoStudioWorkTimeline m_workTimeline = null;
    public PhotoStudioWorkTimeline GetTimeline { get { return m_workTimeline; } }

    private GameObject m_emojiItem = null;
    private List<PhotoStudioEmojiItem> m_emojiItemList = null;
    private List<PhotoStudioEmojiItem> m_installItemList = null;

    private int m_getItemCount = 0;

    public void Init()
    {
        m_state = (State_PhotoStudio)StateManager.instance.m_curState;
        m_guiManager = m_state.m_guiManager;

        InitUI();
        InitEmojiList();

        ClosePopup();
    }

    private void InitUI()
    {
        m_transform = transform;

        m_window = m_transform.FindChild("ScreenShot/Anchor_Center/Window");
        m_uiWindow = new UIWindow(this, m_transform.FindChild("UI").gameObject);

        m_baseMaterial = AssetBundleEx.Load<Material>("[Materials]/[PhotoStudio]/Mat_PhotoStudio_Result");

        m_uiTexture = m_window.FindChild("ScreenShot").GetComponent<UITexture>();
        m_uiTexture.material = Instantiate(m_baseMaterial) as Material;

        m_edge = m_window.FindChild("Edge").GetComponent<UISprite>();

        SetUISize(BASE_SCALE);

        m_workTimeline = new PhotoStudioWorkTimeline(this);
    }

    public IEnumerator OnDirectionWindow(Texture2D texture)
    {
        yield return new WaitForSeconds(LoadingManager.instance.StartScreenOut(0.35f, SCREEN_OUT_TYPE.SCREEN_WHITE_OUT));
        yield return new WaitForSeconds(LoadingManager.instance.StartScreenIn());

        SoundManager.instance.PlayAudioClip("Studio_Shutter");
        
        m_window.gameObject.SetActive(true);

        SetUISize(BASE_SCALE);
        m_edge.gameObject.SetActive(true);

        m_uiTexture.material = Instantiate(m_baseMaterial) as Material;
        m_uiTexture.material.mainTexture = texture;

        texture = null;

        m_selectFilter = WorldManager.instance.m_dataManager.m_imageFilterData.GetBaseImageFilter();

        Hashtable hash = new Hashtable();
        hash.Add("amount", new Vector3(0.04f, 0.04f, 0f));
        hash.Add("time", 0.65f);
        hash.Add("ignoretimescale", true);
        iTween.PunchScale(m_window.gameObject, hash);

        yield return new WaitForSeconds(0.85f);

        Hashtable value = new Hashtable();
        value.Add("from", BASE_SCALE);
        value.Add("to", 1.0f);
        value.Add("time", 0.5f);
        value.Add("easetype", iTween.EaseType.easeOutBack);
        value.Add("onupdate", "UpdateScreenShotScale");
        iTween.ValueTo(gameObject, value);

        yield return new WaitForSeconds(0.65f);

        m_edge.gameObject.SetActive(false);

        m_uiWindow.UpdateUI();
        m_uiWindow.OnOffButtonGroup(true);
    }

    private void UpdateScreenShotScale(float value)
    {
        SetUISize(value);
    }

    public void ClosePopup()
    {
        m_screenShot = null;

        m_uiTexture.mainTexture = null;
        m_uiTexture.material = null;

        m_selectFilter = null;

        m_window.gameObject.SetActive(false);

        m_workTimeline.Release();

        m_uiWindow.OnOffButtonGroup(false, true);
        ReleaseAllEmojiItem();

        MsgBox.instance.CloseSceenShare();

        SetActive(false);
    }

    #region Callback

    public void OnBeforeButton(GameObject obj)
    {
        if (m_guiManager.CheckButtonActive && m_workTimeline.CheckBeforeTimeline)
        {
            Util.ButtonAnimation(obj);

            PhotoStudioWorkTimeline.Timeline item = m_workTimeline.GetBeforeTimeline();

            switch (item.m_type)
            {
                case PHOTOSTUDIO_WORK_TYPE.TYPE_ADD:
                    m_workTimeline.AddAfterTimeline(item);
                    ReleaseEmojiItem(item);
                    break;
                case PHOTOSTUDIO_WORK_TYPE.TYPE_DELETE:
                    m_workTimeline.AddAfterTimeline(item);
                    SetEmojiItem(item); 
                    break;

                case PHOTOSTUDIO_WORK_TYPE.TYPE_MOVE:
                    m_workTimeline.AddAfterTimeline(new PhotoStudioWorkTimeline.Timeline(item.m_type, item.m_item));
                    item.m_item.UpdatePosition(item.m_position); 
                    break;
                case PHOTOSTUDIO_WORK_TYPE.TYPE_SCALE: 
                    m_workTimeline.AddAfterTimeline(new PhotoStudioWorkTimeline.Timeline(item.m_type, item.m_item));
                    item.m_item.UpdateScale(item.m_spriteSize, item.m_rotate); 
                    break;

                case PHOTOSTUDIO_WORK_TYPE.TYPE_SORT:
                    m_workTimeline.AddAfterTimeline(new PhotoStudioWorkTimeline.Timeline(item.m_type, item.m_item));
                    SetEmojiItemSortTop(item);
                    break;

                case PHOTOSTUDIO_WORK_TYPE.TYPE_FILTER:
                    m_workTimeline.AddAfterTimeline(new PhotoStudioWorkTimeline.Timeline(PHOTOSTUDIO_WORK_TYPE.TYPE_FILTER, m_selectFilter));
                    UpdateTexture(item);
                    break;
            }
        }
    }

    public void OnAfterButton(GameObject obj)
    {
        if (m_guiManager.CheckButtonActive && m_workTimeline.CheckAfterTimeline)
        {
            Util.ButtonAnimation(obj);

            PhotoStudioWorkTimeline.Timeline item = m_workTimeline.GetAfterTimeline();

            switch (item.m_type)
            {
                case PHOTOSTUDIO_WORK_TYPE.TYPE_ADD:
                    m_workTimeline.AddBeforeTimeline(item);
                    SetEmojiItem(item);
                    break;
                case PHOTOSTUDIO_WORK_TYPE.TYPE_DELETE:
                    m_workTimeline.AddBeforeTimeline(item);
                    ReleaseEmojiItem(item);
                    break;

                case PHOTOSTUDIO_WORK_TYPE.TYPE_MOVE:
                    m_workTimeline.AddBeforeTimeline(new PhotoStudioWorkTimeline.Timeline(item.m_type, item.m_item));
                    item.m_item.UpdatePosition(item.m_position);
                    break;
                case PHOTOSTUDIO_WORK_TYPE.TYPE_SCALE:
                    m_workTimeline.AddBeforeTimeline(new PhotoStudioWorkTimeline.Timeline(item.m_type, item.m_item));
                    item.m_item.UpdateScale(item.m_spriteSize, item.m_rotate);
                    break;

                case PHOTOSTUDIO_WORK_TYPE.TYPE_SORT:
                    m_workTimeline.AddBeforeTimeline(new PhotoStudioWorkTimeline.Timeline(item.m_type, item.m_item));
                    SetEmojiItemSortTop(item);
                    break;

                case PHOTOSTUDIO_WORK_TYPE.TYPE_FILTER:
                    m_workTimeline.AddBeforeTimeline(new PhotoStudioWorkTimeline.Timeline(PHOTOSTUDIO_WORK_TYPE.TYPE_FILTER, m_selectFilter));
                    UpdateTexture(item);
                    break;
            }
        }
    }

    public void OnSaveButton(GameObject obj)
    {
        if (m_guiManager.CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            StartCoroutine("OnScreenShot");
        }
    }

    public void OnCloseButton(GameObject obj)
    {
        if (m_guiManager.CheckButtonActive)
        {
            Util.ButtonAnimation(obj);

            ClosePopup();
            m_guiManager.OnScreenShotResultPopupClose();
        }
    }

    public void OnConfirmButton(GameObject obj)
    {
        if (m_guiManager.CheckButtonActive)
        {
            Util.ButtonAnimation(obj);

            ClosePopup();
            m_guiManager.OnScreenShotResultPopupClose();
        }
    }

    public void OnFilterButton(GameObject obj)
    {
        if (m_guiManager.CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            m_uiWindow.OpenImageFilter();
        }
    }

    public void OnEmojiButton(GameObject obj)
    {
        if (m_guiManager.CheckButtonActive)
        {
            Util.ButtonAnimation(obj);
            m_uiWindow.OpenEmojiPopup();
        }
    }

    #endregion

    #region ScreenShot

    public void UpdateTexture(ImageFilterInfo info)
    {
        if (m_selectFilter.index.Equals(info.index) == false)
        {
            m_workTimeline.AddBeforeTimeline(new PhotoStudioWorkTimeline.Timeline(PHOTOSTUDIO_WORK_TYPE.TYPE_FILTER, m_selectFilter));

            m_selectFilter = info;
            WorldManager.instance.m_dataManager.m_imageFilterData.SetImageFilter(m_uiTexture, info);
        }
    }

    public void UpdateTexture(PhotoStudioWorkTimeline.Timeline info)
    {
        if (m_selectFilter.Equals(info.m_filter) == false)
        {
            m_selectFilter = info.m_filter;
            WorldManager.instance.m_dataManager.m_imageFilterData.SetImageFilter(m_uiTexture, info.m_filter);
        }
    }

    private IEnumerator OnScreenShot()
    {
        m_state.m_guiManager.CheckButtonActive = false;
        m_state.CheckTouchActive = false;

        m_uiWindow.OnOffButtonGroup(false);

        yield return new WaitForSeconds(0.75f);

        m_screenShot = null;

        if (m_state.CheckProfileImageChnage)
        {          
            RenderImageCallback renderImage = StateManager.instance.m_curState.GetUICamera().gameObject.AddComponent<RenderImageCallback>();
            bool isDepthOnly = StateManager.instance.m_curState.GetUICamera().gameObject.GetComponent<Camera>().clearFlags.Equals(CameraClearFlags.Depth);
            renderImage.Init(isDepthOnly, delegate (Texture2D texture) {
                m_screenShot = texture;
            });

            while (m_screenShot == null)
                yield return null;

            WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.IMAGECROP_TEXTURE, m_screenShot);
            WorldManager.instance.AddMemoryInfo(WORLD_MEMORY_INFO.IMAGECROP_STATE, m_state.GetBeforeStateType);

            m_screenShot = null;
            StateManager.instance.SetTransition(STATE_TYPE.STATE_IMAGECROP);
        }
        else
        {
            UINgShareLogo logo = MsgBox.instance.MakeLogoPanel();
            yield return null;

            RenderImageCallback renderImage = MsgBox.instance.GetCameraTransform.gameObject.AddComponent<RenderImageCallback>();
            bool isDepthOnly = MsgBox.instance.GetCameraTransform.gameObject.GetComponent<Camera>().clearFlags.Equals(CameraClearFlags.Depth);
            renderImage.Init(isDepthOnly, delegate (Texture2D texture) {
                m_screenShot = texture;
            });

            while (m_screenShot == null)
                yield return null;

            yield return StartCoroutine(PluginManager.instance.InitImage(null));

            try
            {
#if UNITY_EDITOR
                string path = string.Format("{0}/Pictures", System.Environment.CurrentDirectory);
                System.IO.DirectoryInfo dInfo = new System.IO.DirectoryInfo(path);
                if (dInfo.Exists == false)
                    dInfo.Create();

                System.IO.File.WriteAllBytes(string.Format("{0}/Xiaowangwang_{1}.png", path, Util.GetNowGameTime()), m_screenShot.EncodeToPNG());
#else
            PluginManager.instance.SaveImageToGallery(m_screenShot, string.Format("Xiaowangwang_{0}", Util.GetNowGameTime()), "");
#endif
            } catch { }

            MsgBox.instance.OpenMsgToast(533);

            yield return new WaitForSeconds(1.85f);

            if (logo != null)
                logo.Release();

            m_uiWindow.SwitchBotButtonGroup(true);
            m_uiWindow.OnOffBotButtonGroup(true);

            MsgBox.instance.OpenScreenShare();

            m_state.m_guiManager.CheckButtonActive = true;
        }
    }

    #endregion

    #region SetEmoji

    private void InitEmojiList()
    {
        m_getItemCount = 0;

        m_emojiItemGroup = m_window.FindChild("PhotoEmojiGroup");

        m_emojiItem = AssetBundleEx.Load<GameObject>("[Prefabs]/[Gui]/PhotoEmoji");
        m_emojiItemList = new List<PhotoStudioEmojiItem>();
        m_installItemList = new List<PhotoStudioEmojiItem>();

        for (int i = 0; i < MAX_EMOJI_COUNT; i++)
        {
            GameObject obj = Instantiate(m_emojiItem) as GameObject;
            obj.transform.parent = m_emojiItemGroup;
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localScale = Vector3.one;
            obj.name = string.Format("EmojiItem_{0:D2}", i);

            m_emojiItemList.Add(obj.GetComponent<PhotoStudioEmojiItem>());
        }
    }

    public void SetEmojiItem(EmojiInfo info)
    {
        if (m_installItemList.Count < MAX_EMOJI_COUNT)
        {
            int depth = 0;
            for (int i = 0; i < m_installItemList.Count; i++)
                depth = Mathf.Max(depth, m_installItemList[i].GetIcon.depth);

            PhotoStudioEmojiItem item = m_emojiItemList[m_getItemCount];
            m_getItemCount = (m_getItemCount + 1).Equals(MAX_EMOJI_COUNT) ? 0 : m_getItemCount + 1;

            m_installItemList.Add(item);

            item.ActiveItem(info, depth + 1);

            m_workTimeline.AddBeforeTimeline(new PhotoStudioWorkTimeline.Timeline(PHOTOSTUDIO_WORK_TYPE.TYPE_ADD, item));
            m_workTimeline.ReleaseAfterTimeline();

            m_uiWindow.CloseEmojiPopup();
        }
    }

    public void SetEmojiItem(PhotoStudioWorkTimeline.Timeline info)
    {
        if (m_installItemList.Count < MAX_EMOJI_COUNT)
        {
            m_installItemList.Add(info.m_item);
            info.m_item.ActiveItem(info);
        }
    }

    public void SetEmojiItemSortTop(PhotoStudioEmojiItem item)
    {
        int depth = 0;
        for (int i = 0; i < m_installItemList.Count; i++)
            depth = Mathf.Max(depth, m_installItemList[i].GetIcon.depth);

        m_workTimeline.AddBeforeTimeline(new PhotoStudioWorkTimeline.Timeline(PHOTOSTUDIO_WORK_TYPE.TYPE_SORT, item));
        item.UpdateDepth(depth + 1);
    }

    public void SetEmojiItemSortTop(PhotoStudioWorkTimeline.Timeline info)
    {
        info.m_item.UpdateDepth(info.m_depth);
    }

    public void SetEmojiItemEditorMode(PhotoStudioEmojiItem item, bool isActive = true)
    {
        for (int i = 0; i < m_installItemList.Count; i++)
            m_installItemList[i].EditorMode = m_installItemList[i].Equals(item) ? isActive : false;
    }

    public void ReleaseEmojiItem(int i)
    {
        PhotoStudioEmojiItem item = m_installItemList[i];

        item.ReleaseItem();
        m_installItemList.RemoveAt(i);
    }

    public void ReleaseEmojiItem(PhotoStudioEmojiItem item)
    {
        item.ReleaseItem();
        if (m_installItemList.Contains(item))
            m_installItemList.Remove(item);

        m_workTimeline.AddBeforeTimeline(new PhotoStudioWorkTimeline.Timeline(PHOTOSTUDIO_WORK_TYPE.TYPE_DELETE, item));
    }

    public void ReleaseEmojiItem(PhotoStudioWorkTimeline.Timeline info)
    {
        info.m_item.ReleaseItem();
        if( m_installItemList.Contains(info.m_item))
            m_installItemList.Remove(info.m_item);
    }

    public void ReleaseAllEmojiItem()
    {
        m_getItemCount = 0;

        for (int i = m_installItemList.Count - 1; i >= 0; --i)
            ReleaseEmojiItem(i);
    }

    #endregion

    private void SetUISize(float scale)
    {
        m_uiTexture.transform.localScale = Vector3.one * scale;
        m_uiTexture.transform.rotation = Quaternion.Euler(Vector3.zero);
        m_uiTexture.flip = UIBasicSprite.Flip.Nothing;
    }

    public void SetActive(bool b)
    {
        gameObject.SetActive(b);
        m_uiWindow.SetActive(b);
    }

    public void Release()
    {
        m_state = null;
        m_guiManager = null;

        m_transform = null;

        m_window = null;
        m_emojiItemGroup = null;

        m_uiWindow = null;

        m_uiTexture = null;
        m_edge = null;

        m_baseMaterial = null;

        m_screenShot = null;

        m_selectFilter = null;
        m_workTimeline = null;

        m_emojiItem = null;

        m_emojiItemList = null;
        m_installItemList = null;
    }
}
