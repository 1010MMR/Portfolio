using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

using Magi;

public enum ChatGetItemFrom {
    Type_None = -1,
    
    Type_Summon = 0,
    Type_SleepMode = 1,

    Type_Adventure = 2,
    Type_Expedition = 3,
    Type_Infinity = 5,

    Type_Rebirth = 6,
    Type_Evolution = 7,

    Type_Gear_Compose = 8,
    Type_Gear_Promote = 9,

    Type_End,
}

public enum SendMessageType {
    Type_None = -1,
    
    Type_UserConnect = 1001,
    Type_ChangeChannel = 1002,
    Type_Message = 1003,
    Type_GetItem = 1004,
    Type_Disconnect = 1005,

    Type_GetMessage = 2000,
    Type_GetItemMessage = 2001,
    Type_GetNoticeMessage = 2002,
    
    Type_ErrorMessage = 9999,

    Type_End,
}

public struct UIMessage {
    public SendMessageType type;
    public string message;
}

/// <summary>
/// <para>name : ChatManager</para>
/// <para>describe : Management chating module.</para>
/// </summary>
public class ChatManager : MonoBehaviour {
    public const int NOTICE_MAX_GRADE = 5;

    private Socket tcpSocket;
    private Socket callBackSocket;

    private byte[] receiveBuffer;
    private const int MAXSIZE = 4096;

    string ipAddress = "";
    int port = 0;

    int channel = 0;

    bool doConnect = false;

    Queue<string> messageQueue = new Queue<string>();
    Queue<int> errorCodeQueue = new Queue<int>();

    private static ChatManager instance = null;
    public static ChatManager Instance {
        get {
            return instance;
        }
    }

    public bool CheckConnect {
        get {
            return tcpSocket.Connected;
        }
    }

    public int GetChannel {
        get {
            return channel;
        }
    }

    void Awake() {
        doConnect = false;
        isUserConnectComplete = false;

        receiveBuffer = new byte[MAXSIZE];
        OptionManager.Instance.GetChatServerPath(out ipAddress, out port);

        instance = this;
    }

    void FixedUpdate() {
        if( doConnect ) {
            if( Application.internetReachability.Equals(NetworkReachability.NotReachable) )
                return;

            BeginConnect();
        }

        if( messageQueue.Count.Equals(0) == false )
            GetMessage(messageQueue.Dequeue());
        if( errorCodeQueue.Count.Equals(0) == false )
            CheckErrorCode(errorCodeQueue.Dequeue());
    }

    /// <summary>
    /// <para>name : InitSocket</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Initialize TCP soket.</para>
    /// </summary>
    public void InitSocket(bool isSwitch = true) {
        tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        doConnect = isSwitch;
    }

    /// <summary>
    /// <para>name : BeginConnect</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Begin connect if socket is not connected.</para>
    /// </summary>
    void BeginConnect() {
        doConnect = false;

        if( tcpSocket == null || 
            tcpSocket.Connected )
            return;

        try {
            tcpSocket.BeginConnect(ipAddress, port, new System.AsyncCallback(TcpConnectCallBack), tcpSocket);
        } catch( SocketException ex ) {
            MagiDebug.LogError(string.Format("Connect Fail. {0}:{1}, {2}", ipAddress, port, ex));
            InitSocket();
        } catch( System.ObjectDisposedException obEx ) {
            MagiDebug.LogError(string.Format("Socket DisposedException, {0}", obEx));
            InitSocket(false);
        }
    }

    /// <summary>
    /// <para>name : Send</para>
    /// <para>parameter : string</para>
    /// <para>return : void</para>
    /// <para>describe : Send message if socket is not closed.</para>
    /// </summary>
    void Send(string message) {
        try {
            byte[] sendBuffer = new UTF8Encoding().GetBytes(message);
            tcpSocket.BeginSend(sendBuffer, 0, sendBuffer.Length, SocketFlags.None, new System.AsyncCallback(TcpSendCallBack), message);
        } catch( SocketException ex ) {
            MagiDebug.LogError(string.Format("Send Fail. {0}", ex.Message));

            CloseSocket(SendMessageErrorType.RES_SEND_RETRY);
            InitSocket();
        } catch( System.ObjectDisposedException obEx ) {
            MagiDebug.LogError(string.Format("Socket DisposedException, {0}", obEx));
            InitSocket(false);
        }
    }

    /// <summary>
    /// <para>name : Receive</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Receive message.</para>
    /// </summary>
    void Receive() {
        callBackSocket.BeginReceive(receiveBuffer, 0, MAXSIZE, SocketFlags.None, new System.AsyncCallback(TcpReceiveCallBack), callBackSocket);
    }

    /// <summary>
    /// <para>name : TcpConnectCallBack</para>
    /// <para>parameter : System.IAsyncResult</para>
    /// <para>return : void</para>
    /// <para>describe : Socket connect result callback.</para>
    /// </summary>
    private void TcpConnectCallBack(System.IAsyncResult asynResult) {
        try {
            Socket socket = asynResult.AsyncState as Socket;

            MagiDebug.Log(string.Format("Connect Chat Server > {0}", socket.RemoteEndPoint));

            socket.EndConnect(asynResult);

            callBackSocket = socket;
            callBackSocket.BeginReceive(receiveBuffer, 0, MAXSIZE, SocketFlags.None, new System.AsyncCallback(TcpReceiveCallBack), callBackSocket);

            if( isUserConnectComplete )
                UserConnect();

        } catch( SocketException ex ) {
            MagiDebug.Log(string.Format("Cannot Connect Chat Server > {0}", ex.Message));

            CloseSocket(SendMessageErrorType.RES_CONNECT_RETRY);
            InitSocket(false);
        }
    }

    /// <summary>
    /// <para>name : TcpSendCallBack</para>
    /// <para>parameter : System.IAsyncResult</para>
    /// <para>return : void</para>
    /// <para>describe : Send message result callback.</para>
    /// </summary>
    private void TcpSendCallBack(System.IAsyncResult asynResult) {
        try {
            string sendMessage = (string)asynResult.AsyncState;
            Hashtable result = sendMessage.hashtableFromJson();

            int pno = 0;
            if( result.ContainsKey("pno") )
                int.TryParse(result["pno"].ToString(), out pno);

            if( pno.Equals((int)SendMessageType.Type_Disconnect) )
                CloseSocket();
        } catch( SocketException ex ) {
            MagiDebug.Log(string.Format("Cannot Send Chat Server > {0}", ex.Message));

            CloseSocket(SendMessageErrorType.RES_RECEIVE_RETRY);
            InitSocket();
        }
    }

    /// <summary>
    /// <para>name : TcpReceiveCallBack</para>
    /// <para>parameter : System.IAsyncResult</para>
    /// <para>return : void</para>
    /// <para>describe : Receive message result callback.</para>
    /// </summary>
    private void TcpReceiveCallBack(System.IAsyncResult asynResult) {
        try {
            Socket socket = asynResult.AsyncState as Socket;
            int readSize = socket.EndReceive(asynResult);

            if( readSize > 0 ) {
                messageQueue.Enqueue(new UTF8Encoding().GetString(receiveBuffer, 0, MAXSIZE));

                receiveBuffer = new byte[MAXSIZE];
                Receive();
            }

            else {
                MagiDebug.Log(string.Format("Close Server Connect."));

                CloseSocket(SendMessageErrorType.RES_CONNECT_RETRY);
                InitSocket();
            }

        } catch( SocketException ex ) {
            MagiDebug.Log(string.Format("Cannot Receive Chat Server > {0}", ex.Message));

            CloseSocket(SendMessageErrorType.RES_RECEIVE_RETRY);
            InitSocket();
        }
    }

    void OnDestroy() {
        if( isUserConnect )
            Disconnect();

        tcpSocket = null;
        callBackSocket = null;
        receiveBuffer = null;
        messageQueue = null;
        instance = null;
        uiMessageList = null;
    }

    #region SEND_CHAT_MESSAGE

    static bool isUserConnectComplete = false;
    bool isUserConnect = false;

    /// <summary>
    /// <para>name : UserConnect</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Send user connect info.</para>
    /// </summary>
    public void UserConnect() {
        if( isUserConnect )
            return;

        isUserConnect = true;
        if( isUserConnectComplete == false )
            isUserConnectComplete = true;

        string pno = ((int)SendMessageType.Type_UserConnect).ToString();
        string accID = string.Format("{0}", ClientDataManager.Instance.account_ID);
        string nickName = ClientDataManager.Instance.OwnName;

        if( accID.Equals(0) || nickName == null ) {
            isUserConnect = false;
            isUserConnectComplete = false;

            return;
        }

        Dictionary<string, string> post = new Dictionary<string, string>();
        post.Add("pno", pno);
        post.Add("AccID", accID);
        post.Add("Nickname", nickName);

        if( ClientDataManager.Instance != null &&
            ClientDataManager.Instance.SessionCode != "" )
            post.Add("SessionKey", ClientDataManager.Instance.SessionCode);

        Send(post.toJson());
    }

    /// <summary>
    /// <para>name : ChangeChannel</para>
    /// <para>parameter : int</para>
    /// <para>return : void</para>
    /// <para>describe : Change channel number to (int)parameter.</para>
    /// </summary>
    public void ChangeChannel(int channelNumber) {
        string pno = ((int)SendMessageType.Type_ChangeChannel).ToString();
        string number = channelNumber.ToString();

        Dictionary<string, string> post = new Dictionary<string, string>();
        post.Add("pno", pno);
        post.Add("RoomNo", number);

        if( ClientDataManager.Instance != null &&
            ClientDataManager.Instance.SessionCode != "" )
            post.Add("SessionKey", ClientDataManager.Instance.SessionCode);

        Send(post.toJson());
    }

    /// <summary>
    /// <para>name : SendChatMessage</para>
    /// <para>parameter : string</para>
    /// <para>return : void</para>
    /// <para>describe : Send message to server - (string)parameter.</para>
    /// </summary>
    public void SendChatMessage(string message) {
        string pno = ((int)SendMessageType.Type_Message).ToString();

        Dictionary<string, string> post = new Dictionary<string, string>();
        post.Add("pno", pno);
        post.Add("Msg", message);

        if( ClientDataManager.Instance != null &&
            ClientDataManager.Instance.SessionCode != "" )
            post.Add("SessionKey", ClientDataManager.Instance.SessionCode);

        Send(post.toJson());
    }

    /// <summary>
    /// <para>name : SendGetItem</para>
    /// <para>parameter : int</para>
    /// <para>return : void</para>
    /// <para>describe : Send get item "notice" message to server - when (int)parameter index.</para>
    /// </summary>
    public void SendGetItem(int getWhat) {
        int getFrom = GetFromType();
        if( getFrom.Equals((int)ChatGetItemFrom.Type_None) )
            return;

        string pno = ((int)SendMessageType.Type_GetItem).ToString();
        string from = string.Format("{0}", getFrom.ToString());
        string what = string.Format("{0}", getWhat.ToString());

        Dictionary<string, string> post = new Dictionary<string, string>();
        post.Add("pno", pno);
        post.Add("From", from);
        post.Add("What", what);

        if( ClientDataManager.Instance != null &&
            ClientDataManager.Instance.SessionCode != "" )
            post.Add("SessionKey", ClientDataManager.Instance.SessionCode);

        Send(post.toJson());
    }

    /// <summary>
    /// <para>name : SendGetItem</para>
    /// <para>parameter : int</para>
    /// <para>return : void</para>
    /// <para>describe : Send get item "notice" message to server - from, and when (int)parameter index.</para>
    /// </summary>
    public void SendGetItem(int getFrom, int getWhat) {
        string pno = ((int)SendMessageType.Type_GetItem).ToString();
        string from = string.Format("{0}", getFrom.ToString());
        string what = string.Format("{0}", getWhat.ToString());

        Dictionary<string, string> post = new Dictionary<string, string>();
        post.Add("pno", pno);
        post.Add("From", from);
        post.Add("What", what);

        if( ClientDataManager.Instance != null &&
            ClientDataManager.Instance.SessionCode != "" )
            post.Add("SessionKey", ClientDataManager.Instance.SessionCode);

        Send(post.toJson());
    }

    /// <summary>
    /// <para>name : Disconnect</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Disconnect socket and clear connect info.</para>
    /// </summary>
    public void Disconnect() {
        if( tcpSocket == null )
            return;
        if( tcpSocket.Connected == false )
            return;

        isUserConnectComplete = false;

        string pno = ((int)SendMessageType.Type_Disconnect).ToString();

        Dictionary<string, string> post = new Dictionary<string, string>();
        post.Add("pno", pno);

        if( ClientDataManager.Instance != null &&
            ClientDataManager.Instance.SessionCode != "" )
            post.Add("SessionKey", ClientDataManager.Instance.SessionCode);

        Send(post.toJson());
    }

    #endregion

    #region CHECK_MESSAGE

    public enum SendMessageErrorType {
        RES_NONE = -1,

        RES_NOT_LOGIN = 1000,

        RES_ROOM_NO_ERROR = 2000,
        RES_ROOM_FULL = 2001,

        RES_NOT_EXIST_MSG = 3000,
        RES_NOT_EXIST_NICK = 3001,
        RES_NOT_EXIST_ACCID = 3002,
        RES_NOT_EXIST_FROM = 3003,
        RES_NOT_EXIST_WHAT = 3004,

        RES_MSG_REPEAT = 4000,

        RES_SESSION_CHECK_FAIL = 9000,

        RES_CONNECT_RETRY = 9001,
        RES_SEND_RETRY = 9002,
        RES_RECEIVE_RETRY = 9003,

        RES_FAIL = 9999,

        RES_END,
    }

    /// <summary>
    /// <para>name : GetMessage</para>
    /// <para>parameter : string</para>
    /// <para>return : void</para>
    /// <para>describe : Get (string)parameter and parsing it.</para>
    /// </summary>
    void GetMessage(string message) {
        Hashtable resultHash;
        string[] personArray = MagiUtil.GetTcpMessage(message);
        for( int i = 0; i < personArray.Length; i++ ) {
            if( CheckMessage(personArray[i], out resultHash) )
                ShowMessage(resultHash);
        }
    }

    /// <summary>
    /// <para>name : ShowMessage</para>
    /// <para>parameter : Hashtable</para>
    /// <para>return : void</para>
    /// <para>describe : Parsing (Hashtable)parameter and show message.</para>
    /// </summary>
    void ShowMessage(Hashtable resultHash) {
        int pno = 0;
        if( resultHash.ContainsKey("pno") )
            int.TryParse(resultHash["pno"].ToString(), out pno);

        SendMessageType messageType = (SendMessageType)pno;
        switch( messageType ) {
            case SendMessageType.Type_UserConnect:
            case SendMessageType.Type_ChangeChannel: {
                    if( resultHash.ContainsKey("roomNo") ) {
                        int.TryParse(resultHash["roomNo"].ToString(), out channel);
                        switch( ClientDataManager.Instance.SelectSceneMode ) {
                            case ClientDataManager.SceneMode.Lobby:
                                if( LobbySceneScriptManager.Instance == null )
                                    break;

                                if( LobbySceneScriptManager.Instance.m_ChatEvent != null )
                                    LobbySceneScriptManager.Instance.m_ChatEvent.ChangeChannel(channel);

                                break;

                            case ClientDataManager.SceneMode.Dungeon:
                                if( DungeonSceneScriptManager.Instance == null )
                                    break;

                                if( DungeonSceneScriptManager.Instance.m_ChatEvent != null )
                                    DungeonSceneScriptManager.Instance.m_ChatEvent.ChangeChannel(channel);

                                break;
                        }
                    }
                }

                break;

            case SendMessageType.Type_GetMessage: {
                    string szMessage = "";
                    string nickName = "";

                    if( resultHash.ContainsKey("Msg") )
                        szMessage = resultHash["Msg"].ToString();
                    if( resultHash.ContainsKey("Nickname") )
                        nickName = resultHash["Nickname"].ToString();

                    UIMessage uiMessage = new UIMessage();
                    uiMessage.type = messageType;
                    uiMessage.message = MagiStringUtil.GetReplaceString(nickName.Equals(ClientDataManager.Instance.OwnName) ? 10005006 : 10005007, "#@Name@#", nickName,
                                "#@Message@#", MagiUtil.MessageCutByte(szMessage, 80));

                    switch( ClientDataManager.Instance.SelectSceneMode ) {
                        case ClientDataManager.SceneMode.Lobby:
                            if( LobbySceneScriptManager.Instance == null )
                                break;

                            if( LobbySceneScriptManager.Instance.m_ChatEvent != null )
                                LobbySceneScriptManager.Instance.m_ChatEvent.AddUserMessage(uiMessage);
                            else
                                AddMessage(uiMessage);

                            if( LobbySceneScriptManager.Instance.m_LobbyEvent != null && LobbySceneScriptManager.Instance.m_LobbyEvent.GetLobbyMenuFrame != null ) {
                                uiMessage.message = MagiStringUtil.GetReplaceString(nickName.Equals(ClientDataManager.Instance.OwnName) ? 10005006 : 10005007, "#@Name@#", nickName, 
                                    "#@Message@#", MagiUtil.MessageCutByte(szMessage, 35));
                                LobbySceneScriptManager.Instance.m_LobbyEvent.GetLobbyMenuFrame.UpdateLobbyChat(uiMessage);
                            }

                            break;

                        case ClientDataManager.SceneMode.Dungeon:
                            if( DungeonSceneScriptManager.Instance == null )
                                break;

                            if( DungeonSceneScriptManager.Instance.m_ChatEvent != null )
                                DungeonSceneScriptManager.Instance.m_ChatEvent.AddUserMessage(uiMessage);
                            else
                                AddMessage(uiMessage);

                            if( DungeonSceneScriptManager.Instance.m_DungeonPlayEvent != null ) {
                                uiMessage.message = MagiStringUtil.GetReplaceString(nickName.Equals(ClientDataManager.Instance.OwnName) ? 10005006 : 10005007, "#@Name@#", nickName,
                                    "#@Message@#", MagiUtil.MessageCutByte(szMessage, 25));
                                DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.UpdateInGameChat(uiMessage);
                            }

                            break;

                        default:
                            AddMessage(uiMessage);
                            break;
                    }
                }

                break;

            case SendMessageType.Type_GetItemMessage: {
                    int from = 0;
                    int what = 0;
                    string nickName = "";

                    if( resultHash.ContainsKey("From") )
                        int.TryParse(resultHash["From"].ToString(), out from);
                    if( resultHash.ContainsKey("What") )
                        int.TryParse(resultHash["What"].ToString(), out what);
                    if( resultHash.ContainsKey("Nickname") )
                        nickName = resultHash["Nickname"].ToString();

                    UIMessage uiMessage = new UIMessage();
                    uiMessage.type = messageType;
                    uiMessage.message = MagiStringUtil.GetReplaceString(10005010, "#@Name@#", nickName, "#@Where@#", GetFromTypeText(from), "#@ItemName@#", GetItemNameText(what));

                    switch( ClientDataManager.Instance.SelectSceneMode ) {
                        case ClientDataManager.SceneMode.Lobby:
                            if( LobbySceneScriptManager.Instance == null )
                                break;

                            if( LobbySceneScriptManager.Instance.m_ChatEvent != null )
                                LobbySceneScriptManager.Instance.m_ChatEvent.AddMessage(uiMessage);
                            else
                                AddMessage(uiMessage);

                            break;

                        case ClientDataManager.SceneMode.Dungeon:
                            if( DungeonSceneScriptManager.Instance == null )
                                break;

                            if( DungeonSceneScriptManager.Instance.m_ChatEvent != null )
                                DungeonSceneScriptManager.Instance.m_ChatEvent.AddMessage(uiMessage);
                            else
                                AddMessage(uiMessage);

                            break;

                        default:
                            AddMessage(uiMessage);
                            break;
                    }
                }

                break;

            case SendMessageType.Type_GetNoticeMessage: {
                    string szMessage = "";

                    if( resultHash.ContainsKey("Msg") )
                        szMessage = resultHash["Msg"].ToString();

                    UIMessage uiMessage = new UIMessage();
                    uiMessage.type = messageType;
                    uiMessage.message = MagiStringUtil.GetReplaceString(10005009, "#@Message@#", MagiUtil.MessageCutByte(szMessage, 80));

                    switch( ClientDataManager.Instance.SelectSceneMode ) {
                        case ClientDataManager.SceneMode.Lobby:
                            if( LobbySceneScriptManager.Instance == null )
                                break;

                            if( LobbySceneScriptManager.Instance.m_ChatEvent != null )
                                LobbySceneScriptManager.Instance.m_ChatEvent.AddUserMessage(uiMessage);
                            else
                                AddMessage(uiMessage);

                            if( LobbySceneScriptManager.Instance.m_LobbyEvent != null && LobbySceneScriptManager.Instance.m_LobbyEvent.GetLobbyMenuFrame != null ) {
                                uiMessage.message = MagiStringUtil.GetReplaceString(10005009, "#@Message@#", MagiUtil.MessageCutByte(szMessage, 35));
                                LobbySceneScriptManager.Instance.m_LobbyEvent.GetLobbyMenuFrame.UpdateLobbyChat(uiMessage);
                            }

                            break;

                        case ClientDataManager.SceneMode.Dungeon:
                            if( DungeonSceneScriptManager.Instance == null )
                                break;

                            if( DungeonSceneScriptManager.Instance.m_ChatEvent != null )
                                DungeonSceneScriptManager.Instance.m_ChatEvent.AddUserMessage(uiMessage);
                            else
                                AddMessage(uiMessage);

                            if( DungeonSceneScriptManager.Instance.m_DungeonPlayEvent != null ) {
                                uiMessage.message = MagiStringUtil.GetReplaceString(10005009, "#@Message@#", MagiUtil.MessageCutByte(szMessage, 25));
                                DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.UpdateInGameChat(uiMessage);
                            }

                            break;

                        default:
                            AddMessage(uiMessage);
                            break;
                    }
                }

                break;
        }
    }

    /// <summary>
    /// <para>name : CheckMessage</para>
    /// <para>parameter : string, out Hashtable</para>
    /// <para>return : bool</para>
    /// <para>describe : Parsing (Hashtable)parameter and check error if error code exist.</para>
    /// </summary>
    bool CheckMessage(string message, out Hashtable result) {
        result = message.hashtableFromJson();

        if( result != null ) {
            int errorCode = 0;
            if( result.ContainsKey("error") )
                int.TryParse(result["error"].ToString(), out errorCode);

            return CheckErrorCode(errorCode);
        }

        return false;
    }

    /// <summary>
    /// <para>name : CheckErrorCode</para>
    /// <para>parameter : int</para>
    /// <para>return : bool</para>
    /// <para>describe : Check error code - (int)parameter.</para>
    /// </summary>
    bool CheckErrorCode(int errorCode) {
        UIMessage uiMessage = new UIMessage();
        uiMessage.type = SendMessageType.Type_None;

        SendMessageErrorType errorType = (SendMessageErrorType)errorCode;
        switch( errorType ) {
            case SendMessageErrorType.RES_NOT_LOGIN:
                return false;

            case SendMessageErrorType.RES_ROOM_NO_ERROR:
                uiMessage.message = MagiStringUtil.GetString(10005003);
                AddErrorMessage(uiMessage);
                return false;

            case SendMessageErrorType.RES_ROOM_FULL:
                uiMessage.message = MagiStringUtil.GetString(10005004);
                AddErrorMessage(uiMessage);
                return false;

            case SendMessageErrorType.RES_MSG_REPEAT:
                uiMessage.message = MagiStringUtil.GetString(10005005);
                AddErrorMessage(uiMessage);
                return false;

            case SendMessageErrorType.RES_NOT_EXIST_MSG:
            case SendMessageErrorType.RES_NOT_EXIST_NICK:
            case SendMessageErrorType.RES_NOT_EXIST_ACCID:
            case SendMessageErrorType.RES_NOT_EXIST_FROM:
            case SendMessageErrorType.RES_NOT_EXIST_WHAT:
            case SendMessageErrorType.RES_FAIL:
                return false;

            case SendMessageErrorType.RES_SESSION_CHECK_FAIL:
                uiMessage.message = MagiStringUtil.GetString(10005027);
                AddErrorMessage(uiMessage);

                CloseSocket();
                return false;

            case SendMessageErrorType.RES_CONNECT_RETRY:
                uiMessage.message = MagiStringUtil.GetString(10005025);
                AddErrorMessage(uiMessage);
                return false;

            case SendMessageErrorType.RES_RECEIVE_RETRY:
                uiMessage.message = MagiStringUtil.GetString(10005026);
                AddErrorMessage(uiMessage);
                return false;

            case SendMessageErrorType.RES_SEND_RETRY:
                uiMessage.message = MagiStringUtil.GetString(10005027);
                AddErrorMessage(uiMessage);
                return false;

            default:
                return true;
        }
    }

    #endregion

    #region MESSAGE_STORAGE

    List<UIMessage> uiMessageList = new List<UIMessage>();

    /// <summary>
    /// <para>name : AddErrorMessage</para>
    /// <para>parameter : UIMessage</para>
    /// <para>return : void</para>
    /// <para>describe : Show error message on Panel UI.</para>
    /// </summary>
    private void AddErrorMessage(UIMessage uiMessage) {
        if( ClientDataManager.Instance.SelectSceneMode.Equals(ClientDataManager.SceneMode.Lobby) &&
            LobbySceneScriptManager.Instance != null && LobbySceneScriptManager.Instance.m_ChatEvent != null )
            LobbySceneScriptManager.Instance.m_ChatEvent.AddMessage(uiMessage);
        else if( ClientDataManager.Instance.SelectSceneMode.Equals(ClientDataManager.SceneMode.Dungeon) &&
            DungeonSceneScriptManager.Instance != null && DungeonSceneScriptManager.Instance.m_ChatEvent != null )
            DungeonSceneScriptManager.Instance.m_ChatEvent.AddMessage(uiMessage);
        else
            AddMessage(uiMessage);
    }

    /// <summary>
    /// <para>name : AddMessage</para>
    /// <para>parameter : UIMessage</para>
    /// <para>return : void</para>
    /// <para>describe : Add message on message list.</para>
    /// </summary>
    private void AddMessage(UIMessage uiMessage) {
        uiMessageList.Add(uiMessage);
    }

    /// <summary>
    /// <para>name : AddMessage</para>
    /// <para>parameter : UIMessage</para>
    /// <para>return : void</para>
    /// <para>describe : Add message on message list.</para>
    /// </summary>
    public void AddMessageList(List<UIMessage> messageList) {
        uiMessageList.Clear();
        uiMessageList.AddRange(messageList);
    }

    /// <summary>
    /// <para>name : GetMessageList</para>
    /// <para>parameter : ref (UIMessage)List</para>
    /// <para>return : bool</para>
    /// <para>describe : Get message list.</para>
    /// </summary>
    public bool GetMessageList(ref List<UIMessage> messageList) {
        messageList.Clear();
        messageList.AddRange(uiMessageList);

        uiMessageList.Clear();

        return !messageList.Count.Equals(0);
    }

    #endregion

    #region UTIL

    /// <summary>
    /// <para>name : CloseSocket</para>
    /// <para>parameter : SendMessageErrorType</para>
    /// <para>return : void</para>
    /// <para>describe : Close socket and clear connect info.</para>
    /// </summary>
    public void CloseSocket(SendMessageErrorType type = SendMessageErrorType.RES_NONE) {
        isUserConnect = false;

        tcpSocket.Close();

        messageQueue.Clear();
        receiveBuffer = new byte[MAXSIZE];

        if( type.Equals(SendMessageErrorType.RES_NONE) == false )
            errorCodeQueue.Enqueue((int)type);
    }

    /// <summary>
    /// <para>name : GetFromType</para>
    /// <para>parameter : </para>
    /// <para>return : int</para>
    /// <para>describe : Return (ChatGetItemFrom) type to (int), by select foward Panel UI.</para>
    /// </summary>
    public int GetFromType() {
        if( ClientDataManager.Instance.SelectSceneMode.Equals(ClientDataManager.SceneMode.Lobby) ) {
            switch( ClientDataManager.Instance.CurrentTopPanel ) {
                case ClientDataManager.Panel_Type.Panel_Sleep:
                    return (int)ChatGetItemFrom.Type_SleepMode;

                case ClientDataManager.Panel_Type.Panel_Shop:
                    return (int)ChatGetItemFrom.Type_Summon;
            }
        }

        else {
            switch( ClientDataManager.Instance.SelectGameMode ) {
                case ClientDataManager.GameMode.NormalMode:
                    return (int)ChatGetItemFrom.Type_Adventure;

                case ClientDataManager.GameMode.ExpeditionMode:
                    return (int)ChatGetItemFrom.Type_Expedition;

                case ClientDataManager.GameMode.InfinityMode:
                    return (int)ChatGetItemFrom.Type_Infinity;
            }
        }

        return (int)ChatGetItemFrom.Type_None;
    }

    /// <summary>
    /// <para>name : GetFromTypeText</para>
    /// <para>parameter : int</para>
    /// <para>return : string</para>
    /// <para>describe : Return (string)text from (int)parameter type.</para>
    /// </summary>
    public string GetFromTypeText(int type) {
        return MagiStringUtil.GetString(type + 10005011);
    }

    /// <summary>
    /// <para>name : GetItemNameText</para>
    /// <para>parameter : int</para>
    /// <para>return : string</para>
    /// <para>describe : Return (string)item name text from (int)parameter type.</para>
    /// </summary>
    public string GetItemNameText(int itemID) {
        CharacterTableManager.CTable characterTable;
        ItemTableManager.STable itemTable;

        string itemName = "";

        if( CharacterTableManager.Instance.FindTable(itemID, out characterTable) )
            itemName = MagiStringUtil.GetReplaceString(10005029, "#@Star@#", characterTable.grade.ToString(), "#@Name@#", MagiStringUtil.GetString(characterTable.destString));
        else if( ItemTableManager.Instance.FindTable(itemID, out itemTable) )
            itemName = MagiStringUtil.GetReplaceString(10005029, "#@Star@#", itemTable.grade.ToString(), "#@Name@#", MagiStringUtil.GetString(itemTable.descString));

        return itemName;
    }

    /// <summary>
    /// <para>name : CheckIgnoreText</para>
    /// <para>parameter : string</para>
    /// <para>return : string</para>
    /// <para>describe : Return (string)text, if contains ignore word - change null.</para>
    /// </summary>
    public bool CheckIgnoreText(string text) {
        UIMessage errorMessage = new UIMessage();
        string[] ignoreList = { "{", "}", "\\" };

        for( int i = 0; i < ignoreList.Length; i++ ) {
            if( text.Contains(ignoreList[i]) ) {
                errorMessage.type = SendMessageType.Type_ErrorMessage;
                errorMessage.message = MagiStringUtil.GetString(10005028);

                if( ClientDataManager.Instance.SelectSceneMode.Equals(ClientDataManager.SceneMode.Lobby) )
                    LobbySceneScriptManager.Instance.m_ChatEvent.AddUserMessage(errorMessage);
                else if( ClientDataManager.Instance.SelectSceneMode.Equals(ClientDataManager.SceneMode.Dungeon) )
                    DungeonSceneScriptManager.Instance.m_ChatEvent.AddUserMessage(errorMessage);

                return true;
            }
        }

        return false;
    }

    #endregion
}
