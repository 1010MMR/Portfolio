using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using NetWork;

public class UICommonMenuGroup : MonoBehaviour
{
    private const float MENU_CLOSE_DELAY = 0.15f;
    private const float EXTEND_MENU_DELAY = 0.02f;
    private const float TOWN_MENU_DELAY = 0.25f;

    private readonly ICON_TYPE[] NORMAL_MENU_GROUP = new ICON_TYPE[] { ICON_TYPE.EPLAY, ICON_TYPE.FRIEND };
    private readonly ICON_TYPE[] EXTEND_MENU_GROUP = new ICON_TYPE[] { ICON_TYPE.DOGBOOK, ICON_TYPE.CLOTH, ICON_TYPE.BREED, ICON_TYPE.INTERIOR, ICON_TYPE.DOGINFO, 
                                                                       ICON_TYPE.BASICSHOP, ICON_TYPE.DESIREREWARD, ICON_TYPE.INVENTORY };

    private Transform m_transform = null;

    private class UIIcon
    {
        private GameObject m_obj = null;

        private Transform m_transform = null;
        public Transform GetTransform { get { return m_transform; } }

        private UICommonMenuGroup.ICON_TYPE m_type = UICommonMenuGroup.ICON_TYPE.NONE;
		public UICommonMenuGroup.ICON_TYPE GetIconType { get { return m_type; } }

        private UISprite m_icon = null;
        private Transform m_new = null;

        private Transform m_lock = null;
        private UISprite m_lockSprite = null;

        private CDirection m_cDirection = null;

        public UIIcon() { }
        public UIIcon(GameObject obj, UICommonMenuGroup.ICON_TYPE type, UIEventListener.VoidDelegate deleage)
        {
            m_type = type;

            m_obj = obj;
            m_transform = obj.transform;

            m_icon = m_transform.FindChild("Icon").GetComponent<UISprite>();
            m_new = m_transform.FindChild("New");

            m_lock = m_transform.FindChild("Lock_Group");
            if (m_lock != null)
                m_lockSprite = m_lock.FindChild("Lock").GetComponent<UISprite>();

            m_transform.GetComponent<UIEventListener>().onClick = deleage;
            m_cDirection = m_obj.AddComponent<CDirection>();
        }

        public void Update()
        {
            switch (m_type)
            {

                case UICommonMenuGroup.ICON_TYPE.GROUP: OnOffNew(InGameNotification.instance.CheckNotification(new NOTI_TYPE[] { NOTI_TYPE.TYPE_NEW_CLOTH, NOTI_TYPE.TYPE_UNLOCK_CLOTH, NOTI_TYPE.TYPE_NEW_INTERIOR, 
                                                                                                                                NOTI_TYPE.TYPE_UNLOCK_INTERIOR, NOTI_TYPE.TYPE_BREED, NOTI_TYPE.TYPE_NEW_DOG })); break;
                                                                                                                                
				case UICommonMenuGroup.ICON_TYPE.FRIEND_FRIEND:
                case UICommonMenuGroup.ICON_TYPE.FRIEND: OnOffNew(InGameNotification.instance.CheckNotification(new NOTI_TYPE[] { NOTI_TYPE.TYPE_FRIEND, NOTI_TYPE.TYPE_FRIEND_REQUEST })); break;
                case UICommonMenuGroup.ICON_TYPE.COLLECT: OnOffNew(InGameNotification.instance.CheckNotification(NOTI_TYPE.TYPE_GET_COLLECTION_REWARD)); break;
                case UICommonMenuGroup.ICON_TYPE.ACHIEVE: OnOffNew(InGameNotification.instance.CheckNotification(NOTI_TYPE.TYPE_ACHIEVE)); break;
                case UICommonMenuGroup.ICON_TYPE.CLOTH: OnOffNew(InGameNotification.instance.CheckNotification(new NOTI_TYPE[] { NOTI_TYPE.TYPE_NEW_CLOTH, NOTI_TYPE.TYPE_UNLOCK_CLOTH })); break;
                case UICommonMenuGroup.ICON_TYPE.INTERIOR: OnOffNew(InGameNotification.instance.CheckNotification(new NOTI_TYPE[] { NOTI_TYPE.TYPE_NEW_INTERIOR, NOTI_TYPE.TYPE_UNLOCK_INTERIOR })); break;
                case UICommonMenuGroup.ICON_TYPE.BREED: OnOffNew(InGameNotification.instance.CheckNotification(NOTI_TYPE.TYPE_BREED)); break;
                case UICommonMenuGroup.ICON_TYPE.DOGBOOK: OnOffNew(InGameNotification.instance.CheckNotification(NOTI_TYPE.TYPE_NEW_DOG)); break;
                case UICommonMenuGroup.ICON_TYPE.DOGINFO: OnOffNew(InGameNotification.instance.CheckNotification(NOTI_TYPE.TYPE_NEW_DOG)); break;

                case UICommonMenuGroup.ICON_TYPE.TOWNSELECT:
                case UICommonMenuGroup.ICON_TYPE.FRIEND_TOWNSELECT: SetTownSelectButton(); break;

                case UICommonMenuGroup.ICON_TYPE.TOWN01:
                case UICommonMenuGroup.ICON_TYPE.FRIEND_TOWN01:
                case UICommonMenuGroup.ICON_TYPE.TOWN02:
                case UICommonMenuGroup.ICON_TYPE.FRIEND_TOWN02: SetTownButton(); break;

                default: OnOffNew(false); break;
            }
        }

        public void OnOffNew(bool b)
        {
            if (m_new != null)
                m_new.gameObject.SetActive(b);
        }

        public void OnOff(bool b, bool isInstantly = false)
        {
            if (m_obj.activeSelf)
            {
                iTween.Stop(m_obj);

                int index = b ? 100000180 : 100000181;

                if (isInstantly) m_cDirection.ResetToBeginning(index);
                else m_cDirection.SetInit(index, true);
            }
        }

        private void SetTownButton()
        {
            int townCode = 0;
            switch (m_type)
            {
                case UICommonMenuGroup.ICON_TYPE.TOWN01:
                case UICommonMenuGroup.ICON_TYPE.FRIEND_TOWN01: townCode = 1; break;
                case UICommonMenuGroup.ICON_TYPE.TOWN02:
                case UICommonMenuGroup.ICON_TYPE.FRIEND_TOWN02: townCode = 2; break;
            }

            TownInfo info = WorldManager.instance.m_dataManager.m_townData.GetTownInfoTable(townCode);
            if (info != null)
            {
                bool isUser = StateManager.instance.m_curState.GetIsUser();
                int curLastTown = isUser ? WorldManager.instance.m_player.m_lastTown : WorldManager.instance.m_player.m_Friend.m_curFriendLastTownNo;

                bool isLock = info.index > curLastTown;
                bool isUnlockAvailable = info.openLevel <= WorldManager.instance.m_player.m_level;

                m_lock.gameObject.SetActive(isLock);

                m_icon.color = isLock ? Color.gray : Color.white;
                m_lockSprite.spriteName = isUser && isUnlockAvailable ? "Icon_Townlock_Off" : "Icon_Townlock_On";
                
                m_lockSprite.MakePixelPerfect();
            }

            OnOffNew(false);
        }

        private void SetTownSelectButton()
        {
            List<TownInfo> infoList = WorldManager.instance.m_dataManager.m_townData.GetTownInfoList;
            if (infoList != null)
            {
                bool isUser = StateManager.instance.m_curState.GetIsUser();
                int curLastTown = isUser ? WorldManager.instance.m_player.m_lastTown : WorldManager.instance.m_player.m_Friend.m_curFriendLastTownNo;

                for (int i = 0; i < infoList.Count; i++)
                {
                    if (infoList[i].index.Equals(WorldManager.instance.m_town.CurrentTownCode) == false)
                    {
                        bool isLock = infoList[i].index > curLastTown;
                        bool isUnlockAvailable = infoList[i].openLevel <= WorldManager.instance.m_player.m_level;

                        m_lock.gameObject.SetActive(isLock);

                        m_icon.color = isLock ? Color.gray : Color.white;
                        m_icon.spriteName = string.Format("Town{0}GoBtn0", infoList[i].index);
                        m_icon.gameObject.GetComponent<ConvertLanguageUI>().UpdateConvertLanguageUI();

                        m_lockSprite.spriteName = isUser && isUnlockAvailable ? "Icon_Townlock_Off" : "Icon_Townlock_On";
                        m_lockSprite.MakePixelPerfect();

                        break;
                    }
                }
            }

            OnOffNew(false);
        }

        public void SetActive(bool b)
        {
            m_obj.SetActive(b);
        }
    }
    private UIIcon[] m_iconArray = null;

    public enum ICON_TYPE
    {
		NONE = -1,

		GROUP,
		FRIEND,
		EPLAY,
		TOWN,
		COLLECT,
		ACHIEVE,
		RANKING,
		CLOTH,
		INTERIOR,
		DOGBOOK,
		BREED,
		DOGINFO,
		INVENTORY,
		DESIREREWARD,
        BASICSHOP,

        TOWNSELECT,
        RETURNHOME,

		TOWN01,
		TOWN02,

		FRIEND_FRIEND,
		FRIEND_DOGINFO,

		FRIEND_TOWN01,
		FRIEND_TOWN02,

        FRIEND_RETURNHOME,
        FRIEND_TOWNSELECT,

		END,
    }

    private CDirection[] m_directionArray = null;
    private enum DIRECTION_TYPE
    {
		NONE = -1,

		UI,
		GROUP,

		END,
    }

    private GameObject[] m_groupArray = null;
    private enum GROUP_TYPE
    {
		NONE = -1,

		USER,
		FRIEND,

		END,
    }

    private bool m_isGroupOpen = false;
    private bool m_isExtendGroupOpen = false;

    private bool m_isCloseLock = false;
    private bool m_isTouchDown = false;

    void LateUpdate()
    {
        if (m_isCloseLock == false && (m_isGroupOpen || m_isExtendGroupOpen) && Input.GetMouseButtonDown(0))
            m_isTouchDown = true;

        if (m_isTouchDown && Input.GetMouseButtonUp(0))
        {
            m_isTouchDown = false;
            SetMenuCloseEvent(true);
        }
    }

    public void Init(Transform parent)
    {
        m_transform = transform;

        m_transform.parent = parent;
        m_transform.localPosition = Vector3.zero;
        m_transform.localScale = Vector3.one;

        m_groupArray = new GameObject[(int)GROUP_TYPE.END];

        m_iconArray = new UIIcon[(int)ICON_TYPE.END];
        m_directionArray = new CDirection[(int)DIRECTION_TYPE.END];

        string[] groupPathArray = { "UserGroup", "FriendGroup" };
        for (int i = 0; i < groupPathArray.Length; i++)
            m_groupArray[i] = m_transform.FindChild(groupPathArray[i]).gameObject;

        string[] iconPathArray = { "UserGroup/GroupMenuButton", "UserGroup/FriendButton", "UserGroup/EplayButton", "UserGroup/TownButton", "UserGroup/CollectButton", "UserGroup/AchieveButton", 
								   "UserGroup/RankingButton", "UserGroup/ClothShopButton", "UserGroup/InteriorShopButton", "UserGroup/DogBookButton", "UserGroup/BreedButton", "UserGroup/DogInfoButton", 
								   "UserGroup/InventoryButton", "UserGroup/DesireRewardButton", "UserGroup/BasicShopButton", "UserGroup/TownSelectButton", "UserGroup/ReturnHomeButton", "UserGroup/TownGroupMenu/Town01", 
                                   "UserGroup/TownGroupMenu/Town02", "FriendGroup/FriendButton", "FriendGroup/DogInfoButton", "FriendGroup/Town01", "FriendGroup/Town02", "FriendGroup/ReturnHomeButton",
                                   "FriendGroup/TownSelectButton" };
        for (int i = 0; i < iconPathArray.Length; i++)
            m_iconArray[i] = new UIIcon(m_transform.FindChild(iconPathArray[i]).gameObject, (ICON_TYPE)i, OnButtonClick);

        m_directionArray[(int)DIRECTION_TYPE.UI] = m_transform.GetComponent<CDirection>();
        m_directionArray[(int)DIRECTION_TYPE.GROUP] = m_transform.FindChild("UserGroup/TownGroupMenu").GetComponent<CDirection>();

        StartCoroutine(OnOffMenuGroup(false, true));
        StartCoroutine(OnOffExtendMenuGroup(false, true));
    }

    public void UpdateUIGroup()
    {
        switch (StateManager.instance.m_curStateType)
        {
            case STATE_TYPE.STATE_ROOM:
                {
                    bool isUser = StateManager.instance.m_curState.GetIsUser();
                    SetUserGroup(isUser);

                    if (isUser)
                    {
                        m_iconArray[(int)ICON_TYPE.RANKING].SetActive(false);
                        m_iconArray[(int)ICON_TYPE.RETURNHOME].SetActive(false);
                        m_iconArray[(int)ICON_TYPE.TOWNSELECT].SetActive(false);
                        m_iconArray[(int)ICON_TYPE.EPLAY].SetActive(SdkManager.instance.CheckEplayModuleExists());
                    }

                    else
                    {
                        m_iconArray[(int)ICON_TYPE.FRIEND_FRIEND].GetTransform.localPosition = new Vector3(-54.0f, 52.0f, 0);
                        m_iconArray[(int)ICON_TYPE.FRIEND_RETURNHOME].SetActive(false);
                        m_iconArray[(int)ICON_TYPE.FRIEND_TOWNSELECT].SetActive(false);
                    }
                }
                break;
            case STATE_TYPE.STATE_VILLAGE:
                {
                    bool isUser = StateManager.instance.m_curState.GetIsUser();
                    SetUserGroup(isUser);

                    if (isUser)
                    {
                        m_iconArray[(int)ICON_TYPE.RANKING].SetActive(isUser && WorldManager.instance.m_town.CheckBuildingEnable(BUILDING_TYPE.BUILDING_PARK));
                        m_iconArray[(int)ICON_TYPE.GROUP].SetActive(false);
                        m_iconArray[(int)ICON_TYPE.TOWN].SetActive(false);
                        m_iconArray[(int)ICON_TYPE.EPLAY].SetActive(SdkManager.instance.CheckEplayModuleExists());
                    }

                    else
                    {
                        m_iconArray[(int)ICON_TYPE.FRIEND_FRIEND].GetTransform.localPosition = new Vector3(-166.0f, 52.0f, 0);
                        m_iconArray[(int)ICON_TYPE.FRIEND_DOGINFO].SetActive(false);
                        m_iconArray[(int)ICON_TYPE.FRIEND_TOWN01].SetActive(false);
                        m_iconArray[(int)ICON_TYPE.FRIEND_TOWN02].SetActive(false);
                    }
                }
                break;
        }

        for (int i = 0; i < m_iconArray.Length; i++)
            m_iconArray[i].Update();

        OnOffMenuGroup(false, true);
        StartCoroutine(OnOffExtendMenuGroup(false, true));
    }

	#region OnOff

    public void OnOff(bool b, bool isInstantly = false)
    {
        OnOffUIGroup(b, isInstantly);
        UpdateUIGroup();
    }

    private void OnOffUIGroup(bool b, bool isInstantly = false)
    {
        iTween.Stop(m_directionArray[(int)DIRECTION_TYPE.UI].gameObject);

        int index = 0;
        switch (StateManager.instance.m_curStateType)
        {
            case STATE_TYPE.STATE_ROOM: index = b ? (WorldManager.instance.m_player.GetRoomLoadType().Equals(ROOM_LOAD_TYPE.NOEDIT) || 
                                                    WorldManager.instance.m_player.GetRoomLoadType().Equals(ROOM_LOAD_TYPE.EDIT)) ? 100000182 : 100000184 : 100000183; break;
            default: index = b ? 100000182 : 100000183; break;
        }

        if (isInstantly) m_directionArray[(int)DIRECTION_TYPE.UI].ResetToBeginning(index);
        else m_directionArray[(int)DIRECTION_TYPE.UI].SetInit(index, true);
    }

    private IEnumerator OnOffMenuGroup(bool b, bool isInstantly = false)
    {
        iTween.Stop(m_directionArray[(int)DIRECTION_TYPE.GROUP].gameObject);

        int index = b ? 100000178 : 100000179;

        if (isInstantly) m_directionArray[(int)DIRECTION_TYPE.GROUP].ResetToBeginning(index);
        else m_directionArray[(int)DIRECTION_TYPE.GROUP].SetInit(index, true);

        if (isInstantly == false)
            yield return new WaitForSeconds(TOWN_MENU_DELAY);

        m_isGroupOpen = b;
    }

    private IEnumerator OnOffExtendMenuGroup(bool b, bool isInstantly = false)
    {
        ICON_TYPE[] typeArray = null;

        // Close
        typeArray = b ? NORMAL_MENU_GROUP : EXTEND_MENU_GROUP;
        for (int i = 0; i < typeArray.Length; i++)
            m_iconArray[(int)typeArray[i]].OnOff(false, true);

        // Open
        typeArray = b ? EXTEND_MENU_GROUP : NORMAL_MENU_GROUP;
        for (int i = 0; i < typeArray.Length; i++)
        {
            m_iconArray[(int)typeArray[i]].OnOff(true);
            yield return new WaitForSeconds(EXTEND_MENU_DELAY);
        }

        m_isExtendGroupOpen = b;
    }

    private void SetMenuCloseEvent(bool isActive = true)
    {
        StopCoroutine("WaitForMenuCloseEvent");
        if (isActive)
            StartCoroutine("WaitForMenuCloseEvent");
    }

    private IEnumerator WaitForMenuCloseEvent()
    {
        yield return new WaitForSeconds(MENU_CLOSE_DELAY);

        if (m_isGroupOpen) StartCoroutine(OnOffMenuGroup(false));
        if (m_isExtendGroupOpen) StartCoroutine(OnOffExtendMenuGroup(false));
    }

    #endregion

    #region Callback

    private void OnButtonClick(GameObject obj)
    {
        if (StateManager.instance.m_curState.CheckGUIButtonActive() == false)
            return;

        UIButtonEvent(GetIconType(obj.transform));
    }

    public void UIButtonEvent(ICON_TYPE type)
    {
        UIIcon icon = GetIcon(type);

        if (icon != null)
        {
            Util.ButtonAnimation(icon.GetTransform.gameObject);
            SetMenuCloseEvent(false);

            switch (icon.GetIconType)
            {
                case ICON_TYPE.GROUP:
                    {
                        if (m_isGroupOpen)
                            StartCoroutine(OnOffMenuGroup(false, true));
                        StartCoroutine(OnOffExtendMenuGroup(!m_isExtendGroupOpen));
                    }
                    break;

                case ICON_TYPE.FRIEND:
                case ICON_TYPE.FRIEND_FRIEND:
                    {
                        StateManager.instance.m_curState.OpenFriendWindow();
                    }
                    break;

                case ICON_TYPE.TOWN:
                    {
                        if (m_isExtendGroupOpen)
                            StartCoroutine(OnOffExtendMenuGroup(false, true));
                        StartCoroutine(OnOffMenuGroup(!m_isGroupOpen));
                    }
                    break;

                case ICON_TYPE.COLLECT:
                    {
                        StateManager.instance.m_curState.OpenCollection();
                    }
                    break;

                case ICON_TYPE.ACHIEVE:
                    {
                        AchievementManager.instance.OpenAchieveWindow();
                    }
                    break;

                case ICON_TYPE.RANKING:
                    {
                        ((State_Village)StateManager.instance.m_curState).m_guiVillageManager.OpenParkRank();
                    }
                    break;

                case ICON_TYPE.CLOTH:
                    {
                        WorldManager.instance.SetSceneDogInfo(WorldManager.instance.m_player.m_mainDog, 3, true, StateManager.instance.m_curStateType);
                    }
                    break;

                case ICON_TYPE.INTERIOR:
                    {
                        WorldManager.instance.SetReservMakingRoom(ItrItemWindow.WINDOWTYPE.MARKET, ItrItemWindow.TAPTYPE.TAP_THEME);
                        ((State_Room)StateManager.instance.m_curState).m_guiManager.OnClickMakingRoom();
                    }
                    break;

                case ICON_TYPE.DOGBOOK:
                    {
                        GUIManager_Room guiRoom = ((State_Room)StateManager.instance.m_curState).m_guiManager;
                        guiRoom.OnOffDogGuide(true);
                    }
                    break;

                case ICON_TYPE.BREED:
                    {
                        WorldManager.instance.SetSceneDogInfo(0, 2, false, STATE_TYPE.STATE_ROOM);
                    }
                    break;

                case ICON_TYPE.DOGINFO:
                    {
                        WorldManager.instance.SetSceneDogInfo(0, 1, false, STATE_TYPE.STATE_ROOM);
                    }
                    break;

                case ICON_TYPE.FRIEND_DOGINFO:
                    {
                        ((State_Room)StateManager.instance.m_curState).m_guiManager.OnFriendDogInfo();
                    }
                    break;

                case ICON_TYPE.INVENTORY:
                    {
                        InventoryWindow.instance.OpenInventory();
                    }
                    break;

                case ICON_TYPE.DESIREREWARD:
                    {
                        GUIManager_Room guiRoom = ((State_Room)StateManager.instance.m_curState).m_guiManager;
                        guiRoom.GetStateRoom().m_camManager.OrderCameraActive(false);
                        //guiRoom.m_RewardListPopup.OpenRewardListPopup();
                        guiRoom.OpenRewardListPopup();
                    }
                    break;

                case ICON_TYPE.BASICSHOP:
                    {
                        ShopWindow.instance.OpenShopWindow(SHOP_TAB_TYPE.TAB_MANAGETOOL, PRODUCT_TYPE.PRODUCT_MANAGE_ALL);
                    }
                    break;

                case ICON_TYPE.RETURNHOME:
                    {
                        NetworkManager.instance.SendRoomTransfer(WorldManager.instance.m_player.GetCurRoomNo());
                    }
                    break;

                case ICON_TYPE.FRIEND_RETURNHOME:
                    {
                        NetworkManager.instance.SendSocVisit(WorldManager.instance.m_player.m_Friend.m_CurFriendNo);
                    }
                    break;

                case ICON_TYPE.EPLAY:
                    {
                        PluginManager.instance.OpenEplay(0);
                    }
                    break;

                case ICON_TYPE.TOWN01:
                case ICON_TYPE.TOWN02:
                case ICON_TYPE.FRIEND_TOWN01:
                case ICON_TYPE.FRIEND_TOWN02:
                case ICON_TYPE.TOWNSELECT:
                case ICON_TYPE.FRIEND_TOWNSELECT:
                    {
                        OnTownButtonClick(icon.GetIconType);
                    }
                    break;
            }
        }
    }

    private void OnTownButtonClick(ICON_TYPE type)
    {
        TownInfo info = WorldManager.instance.m_dataManager.m_townData.GetTownInfoTable(GetTownCode(type));
        if (info != null)
        {
            bool isUser = StateManager.instance.m_curState.GetIsUser();
            int curLastTown = isUser ? WorldManager.instance.m_player.m_lastTown : WorldManager.instance.m_player.m_Friend.m_curFriendLastTownNo;

            bool isLock = info.index > curLastTown;
            bool isUnlockAvailable = info.openLevel <= WorldManager.instance.m_player.m_level;

            if (isLock)
            {
                if (isUnlockAvailable && isUser)
                {
                    switch (StateManager.instance.m_curStateType)
                    {
                        case STATE_TYPE.STATE_ROOM:
                            GUIManager_Room guiRoom = ((State_Room)StateManager.instance.m_curState).m_guiManager;
                            guiRoom.GetStateRoom().m_camManager.OrderCameraActive(false);
                            guiRoom.OnOffClickLcokBtnCollider(true);
                            break;

                        case STATE_TYPE.STATE_VILLAGE:
                            GUIManager_Village guiVillage = ((State_Village)StateManager.instance.m_curState).m_guiVillageManager;
                            guiVillage.SetControlActive(false);
                            break;
                    }

                    NetworkManager.instance.SendTownOpen(info.index);
                }
            }

            else
            {
                switch (StateManager.instance.m_curStateType)
                {
                    case STATE_TYPE.STATE_ROOM:
                        GUIManager_Room guiRoom = ((State_Room)StateManager.instance.m_curState).m_guiManager;
                        guiRoom.GetStateRoom().m_camManager.OrderCameraActive(false);
                        guiRoom.OnOffClickLcokBtnCollider(true);
                        break;

                    case STATE_TYPE.STATE_VILLAGE:
                        if (WorldManager.instance.m_town.CurrentTownCode.Equals(info.index))
                            return;

                        GUIManager_Village guiVillage = ((State_Village)StateManager.instance.m_curState).m_guiVillageManager;
                        guiVillage.HIdeAndLockUI();
                        break;
                }

                if (isUser) NetworkManager.instance.SendTownEnter(info.index);
                else NetworkManager.instance.SendSocTownEnter(WorldManager.instance.m_player.m_Friend.m_CurFriendNo, info.index);

            }
        }
    }

    private int GetTownCode(ICON_TYPE type)
    {
        switch (type)
        {
            case ICON_TYPE.TOWN01:
            case ICON_TYPE.FRIEND_TOWN01: return 1;
            case ICON_TYPE.TOWN02:
            case ICON_TYPE.FRIEND_TOWN02: return 2;

            case ICON_TYPE.TOWNSELECT:
            case ICON_TYPE.FRIEND_TOWNSELECT:
                List<TownInfo> infoList = WorldManager.instance.m_dataManager.m_townData.GetTownInfoList;
                if (infoList != null)
                {
                    for (int i = 0; i < infoList.Count; i++)
                    {
                        if (infoList[i].index.Equals(WorldManager.instance.m_town.CurrentTownCode) == false)
                            return infoList[i].index;
                    }
                }
                break;
        }

        return 0;
    }

    #endregion

    #region Effect

    public void TownOpenEffect(int townCode)
    {
        bool isUser = StateManager.instance.m_curState.GetIsUser();
        ICON_TYPE selectIconType = ICON_TYPE.NONE;

        if (StateManager.instance.m_curStateType.Equals(STATE_TYPE.STATE_VILLAGE))
        {
            selectIconType = ICON_TYPE.TOWNSELECT;
        }

        else
        {
            switch (townCode)
            {
                case 1: selectIconType = isUser ? ICON_TYPE.TOWN01 : ICON_TYPE.FRIEND_TOWN01; break;
                case 2: selectIconType = isUser ? ICON_TYPE.TOWN02 : ICON_TYPE.FRIEND_TOWN02; break;
            }
        }

        if (selectIconType.Equals(ICON_TYPE.NONE) == false)
            StartCoroutine(WaitForTownOpenEffect(m_iconArray[(int)selectIconType]));
    }

    private IEnumerator WaitForTownOpenEffect(UIIcon icon)
    {
        m_isCloseLock = true;

        SoundManager.instance.PlayAudioClip("UI_Opentown");

        GameObject effect = Instantiate(AssetBundleEx.Load<GameObject>("[Prefabs]/[Effects]/FX_town_building_open")) as GameObject;
        effect.transform.position = icon.GetTransform.position;
        Util.SetGameObjectLayer(effect, LayerMask.NameToLayer("UIEffect"));

        yield return new WaitForSeconds(1.5f);

        icon.Update();
        DestroyImmediate(effect);

        switch(StateManager.instance.m_curStateType)
        {
            case STATE_TYPE.STATE_ROOM:
                break;

            case STATE_TYPE.STATE_VILLAGE:
                GUIManager_Village guiVillage = ((State_Village)StateManager.instance.m_curState).m_guiVillageManager;
                guiVillage.HIdeAndLockUI();

                yield return new WaitForSeconds(0.5f);
                break;
        }

        m_isCloseLock = false;

        NetworkManager.instance.SendTownEnter(WorldManager.instance.m_player.m_lastTown);
    }

    #endregion

    #region Util

    public Vector3 GetButtonPos(ICON_TYPE type)
    {
        return m_iconArray[(int)type].GetTransform.position;
    }

    public void PunchScale(ICON_TYPE type, float scaleTime = 0.3f)
    {
        iTween.Stop(m_iconArray[(int)type].GetTransform.gameObject);
        m_iconArray[(int)type].GetTransform.localScale = Vector3.one;

        iTween.PunchScale(m_iconArray[(int)type].GetTransform.gameObject, Vector3.one * 0.5f, scaleTime);
    }

    private void SetUserGroup(bool isUser)
    {
        m_groupArray[(int)GROUP_TYPE.USER].SetActive(isUser);
        m_groupArray[(int)GROUP_TYPE.FRIEND].SetActive(!isUser);
    }

    private ICON_TYPE GetIconType(Transform trans)
    {
        for (int i = 0; i < m_iconArray.Length; i++)
        {
            if (m_iconArray[i].GetTransform.Equals(trans))
                return (ICON_TYPE)i;
        }

        return ICON_TYPE.NONE;
    }

    private UIIcon GetIcon(Transform trans)
    {
        for (int i = 0; i < m_iconArray.Length; i++)
        {
            if (m_iconArray[i].GetTransform.Equals(trans))
                return m_iconArray[i];
        }

        return null;
    }

    private UIIcon GetIcon(ICON_TYPE type)
    {
        return m_iconArray[(int)type];
    }

    #endregion

    public void SetActive(bool b)
    {
        gameObject.SetActive(b);
    }
}
