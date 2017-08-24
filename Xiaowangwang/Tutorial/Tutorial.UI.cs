using UnityEngine;
using System.Collections;
using System.Collections.Generic;

#if USER_SERVER
using NetWork;
#endif

using LitJson;

public partial class Tutorial : MonoSingleton<Tutorial>
{
    private Transform m_transform = null;
    private Camera m_uiTutorialCamera = null;

    private UITutorial[] m_uiTutorial = null;
    private UITutorial.UITutorialCloseCB m_uiTutorialCloseCB = null;
	
    #region Init

    private void CreateWindow()
    {
        InitWindow();
    }

    private void InitWindow()
    {
        GameObject parent = GameObject.Find(StateManager.instance.GetSceneNameByCurState());

        m_transform = Util.CreateObject("[Prefabs]/[Gui]/UITutorialWindow", null, Vector3.zero, 1.0f);
        m_transform.localPosition = parent != null ? parent.transform.localPosition : Vector3.zero;
        m_transform.localScale = Vector3.one;
        m_transform.name = "UITutorialWindow";

        m_uiTutorialCamera = m_transform.FindChild("UITutorialCamera").GetComponent<Camera>();
        OnOffTutorialCamera(false);

        #region UITutorial
        m_uiTutorial = new UITutorial[(int)TUTORIAL_UI_TYPE.TYPE_END];

        string[] panelPathArray = { "UI_S96_Talk", "UI_S95_Mini_Talk", "UI_S94_Select", "UI_S95_Finger_View", "UI_S97_Reward_Popup", "UI_S99_Delay", "UI_S99_MessageBox" };
        for(int i = 0; i < panelPathArray.Length; i++)
            m_uiTutorial[i] = CreateComponent(m_transform.FindChild(panelPathArray[i]).gameObject, (TUTORIAL_UI_TYPE)i);
        #endregion
    }

    private UITutorial CreateComponent(GameObject obj, TUTORIAL_UI_TYPE type)
    {
        switch(type)
        {
            case TUTORIAL_UI_TYPE.TYPE_TALK:
                return obj.AddComponent<UITutorialTalk>();
            case TUTORIAL_UI_TYPE.TYPE_MINI_TALK:
                return obj.AddComponent<UITutorialMiniTalk>();
            case TUTORIAL_UI_TYPE.TYPE_TOUCH:
                return obj.AddComponent<UITutorialSelect>();
            case TUTORIAL_UI_TYPE.TYPE_FINGER:
                return obj.AddComponent<UITutorialFingerView>();
            case TUTORIAL_UI_TYPE.TYPE_REWARD:
                return obj.AddComponent<UITutorialRewardPopup>();
            case TUTORIAL_UI_TYPE.TYPE_DELAY:
                return obj.AddComponent<UITutorialDelay>();
            case TUTORIAL_UI_TYPE.TYPE_MESSAGE:
                return obj.AddComponent<UITutorialMsgBox>();
            default:
                return null;
        }
    }

    #endregion

    public void OpenTutorialWindow(TutorialInfo info)
    {
        if(m_transform == null)
            CreateWindow();
        ReleaseWindow();

        UpdateWindow(info);
    }

    private void UpdateWindow(TutorialInfo info)
    {
        switch(info.TriggerType)
        {
            case TUTORIAL_TRIGGER_TYPE.TYPE_TALK:
                m_uiTutorial[(int)TUTORIAL_UI_TYPE.TYPE_TALK].OpenUI(info, m_uiTutorialCloseCB);
                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH:
            case TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH_VIEW:
            case TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH_CALLBACK:
                m_uiTutorial[(int)TUTORIAL_UI_TYPE.TYPE_TOUCH].OpenUI(info, m_uiTutorialCloseCB);
                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_WAIT:
            case TUTORIAL_TRIGGER_TYPE.TYPE_NONSERVER_WAIT:
                if((info.highlightPos.Equals(Vector3.zero) && info.highlighSize.Equals(Vector2.zero)) == false)
                    m_uiTutorial[(int)TUTORIAL_UI_TYPE.TYPE_TOUCH].OpenUI(info, m_uiTutorialCloseCB);
                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_DELAY_TIME:
                m_uiTutorial[(int)TUTORIAL_UI_TYPE.TYPE_DELAY].OpenUI(info, m_uiTutorialCloseCB);
                break;

            case TUTORIAL_TRIGGER_TYPE.TYPE_REWARD_WINDOW:
                m_uiTutorial[(int)TUTORIAL_UI_TYPE.TYPE_REWARD].OpenUI(info, m_uiTutorialCloseCB);
                break;
        }

        if(info.TriggerType.Equals(TUTORIAL_TRIGGER_TYPE.TYPE_TOUCH_CALLBACK) || 
            info.sendMessageTarget.Equals("0") || info.sendMessageFunc.Equals("0"))
            OpenTutorialAnotherWindow(info);
    }

    public void OpenTutorialAnotherWindow(TutorialInfo info)
    {
        if(info.chatType.Equals(TUTORIAL_CHAT_TYPE.TYPE_SMALL))
            m_uiTutorial[(int)TUTORIAL_UI_TYPE.TYPE_MINI_TALK].OpenUI(info);
        if(info.fingerPos.Equals(Vector3.zero) == false)
            m_uiTutorial[(int)TUTORIAL_UI_TYPE.TYPE_FINGER].OpenUI(info);
    }

    public void OpenTutorialMessageBox(int title, int msg, MSGBOX_TYPE type = MSGBOX_TYPE.OK, UITutorial.UITutorialMsgCB msgCB = null)
    {
        m_uiTutorial[(int)TUTORIAL_UI_TYPE.TYPE_MESSAGE].OpenUI(title, msg, type, msgCB);
    }

    public UITutorial GetUITutorialWindow(TUTORIAL_UI_TYPE uiType)
    {
        return m_uiTutorial[(int)uiType];
    }

    public void SetActiveTutorialWindow(TUTORIAL_UI_TYPE uiType)
    {
        if(m_transform == null)
            CreateWindow();
        ReleaseWindow();

        GetUITutorialWindow(uiType).SetActive(true);
    }

    #region Release

    public void ReleaseWindow()
    {
        if(m_uiTutorial != null)
        {
            for(int i = 0; i < m_uiTutorial.Length; i++)
            {
                if(m_uiTutorial[i] != null)
                    m_uiTutorial[i].CloseUI(true);
            }
        }
    }

    #endregion

    #region Util

    public bool CheckActiveTutorialWindow()
    {
        if(m_transform != null)
        {
            for(int i = 0; i < m_uiTutorial.Length; i++)
            {
                if(m_uiTutorial[i] != null && m_uiTutorial[i].CheckActiveUITutorial)
                    return true;
            }
        }

        return false;
    }

    #endregion

    #region Camera

    public void OnOffTutorialCamera(bool b)
    {
        if (m_uiTutorialCamera != null)
            m_uiTutorialCamera.gameObject.SetActive(b);
        Util.SetGameObjectLayer(m_transform.gameObject, LayerMask.NameToLayer(b ? "Tutorial" : "UI"));
    }

    #endregion
}
