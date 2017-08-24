using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Magi;

using MagiTable;

public enum CharacterType {
    NONE = -1,

    POWER,
    SPEED,
    MAGIC,

    END,
}

public enum CharacterTypeAdvance {
    Type_None = -1,

    Type_Under,
    Type_Normal,
    Type_Upper,

    Type_End,
}

public enum CharacterTeam {
    Type_None = -1,

    Type_Player,
    Type_MatchEnemy,
    Type_Enemy,
    Type_Minion,

    Type_End,
}

/// <summary>
/// <para>name : CharacterManager</para>
/// <para>describe : Manage character object.</para>
/// </summary>
public class CharacterManager : MonoBehaviour {
    private const float SWAP_BUFF_TIME = 1.0f;

    public enum EMode {
        Lobby,
        Game,
    };

    private static volatile CharacterManager instance_ = null;
    public static CharacterManager Instance {
        get {
            return instance_;
        }
    }

    public delegate void CharacterLoadComplateCB(Character character);
    public delegate bool RandomPositionCB(out Vector3 position);
    public delegate void Auto_UseSkillActivePlayerCB(int skillTableID);
    public delegate void Auto_ChangeActivePlayerCB(System.Guid uid);

    public EMode mode_;

    public Camera m_WorldCamera;
    public Camera m_GuiCamera;

    [HideInInspector]
    public GameObject attachPointLightPrefab_;
    [HideInInspector]
    public CharacterDamageImageText m_DamageText_Prefab;
    [HideInInspector]
    public CharacterHpBar m_Char_HpBar_Prefab;
    [HideInInspector]
    public CharacterHpBar m_Mon_HpBar_Prefab;
    [HideInInspector]
    public CharacterEffectText uiEffectText;
    [HideInInspector]
    public CharacterSkillText uiSkillText;
    [HideInInspector]
    public GameObject changePlayerEffect_;
    [HideInInspector]
    public GameObject hitNormalEffect_;
    [HideInInspector]
    public GameObject hitCriticalEffect_;
    [HideInInspector]
    public GameObject respawnEffect_;
    [HideInInspector]
    public GameObject elitCharacterEffect_;

    public Shader basicOneLight = null;
    public Shader basicCullOffOneLight = null;
    public Texture2D bossRimTexture_;

    public Color hitColor;

    private GameObject rootInstance_ = null;

    private  CState[] state_;
    private  CharacterType[] typeArray = { CharacterType.POWER, CharacterType.SPEED, CharacterType.MAGIC };

    private  System.Guid activePlayerUID_;
    private System.Guid activeEnemyUID_;
    private System.Guid bossUID_;

    private List<System.Guid> battle_PlayerUID_ = new List<System.Guid>();
    private List<System.Guid> battle_EnemyUID_ = new List<System.Guid>();

    private Character activePlayer_;
    private Character activeEnemy_;

    private List<Character> battle_PlayerList_ = new List<Character>();
    private List<Character> battle_EnemyList_ = new List<Character>();
    private List<Character> battle_MinionList_ = new List<Character>();

    private Character boss_;

    private Dictionary<System.Guid, Character> hashCharacter_ = new Dictionary<System.Guid, Character>();
    private List<ESkillProperty> eSwapCharacterSkillPropertyList = new List<ESkillProperty>();

    Auto_UseSkillActivePlayerCB auto_UseSkillActivePlayerCB_;

    private float characterPointFactor_ = 1;

    private string[] objectPath = { "object/AttachPointLight", "ui/ui_dungeon/ui_dungeon_etc/Character_Damage_Image_Text", 
                                      "ui/ui_dungeon/ui_dungeon_etc/CharacterHpBar", "ui/ui_dungeon/ui_dungeon_etc/MonsterHpBar", 
                                      "ui/ui_dungeon/ui_dungeon_etc/UIEffectText", "ui/ui_dungeon/ui_dungeon_etc/UISkillText", "effect/effect_ui_etc/CH_change_01", 
                                      "effect/damge/Damage_Basic_01", "effect/damge/DamageBloodEffect01", "effect/buff/ice_buff_01", "effect/buff/enemy_ground" };
    private enum ObjectIndex {
        Type_None = -1,

        Type_AttachPointLight,
        Type_DamageText,
        Type_Char_Hp_Bar,
        Type_Mon_Hp_Bar,
        Type_UI_Effect_Text,
        Type_UI_Skill_Text,
        Type_Change_Player_Effect,
        Type_Hit_Normal_Effect,
        Type_Hit_Critical_Effect,
        Type_Respawn_Effect,
        Type_Elit_Character_Effect,

        Type_End,
    }

    #region Value

    public System.Guid ActivePlayerUID {
        set {
            activePlayerUID_ = value;
        }
        get {
            return activePlayerUID_;
        }
    }

    public System.Guid ActiveEnemyUID {
        set {
            activeEnemyUID_ = value;
        }
        get {
            return activeEnemyUID_;
        }
    }

    public System.Guid bossUID {
        set {
            bossUID_ = value;
        }
        get {
            return bossUID_;
        }
    }

    public List<System.Guid> Battle_PlayerUID {
        get {
            return battle_PlayerUID_;
        }
        set {
            battle_PlayerUID_ = value;
        }
    }

    public List<System.Guid> Battle_EnemyUID {
        get {
            return battle_EnemyUID_;
        }
        set {
            battle_EnemyUID_ = value;
        }
    }

    public Character ActivePlayer {
        get {
            return activePlayer_;
        }
        set {
            activePlayer_ = value;
        }
    }

    public Character ActiveEnemy {
        get {
            return activeEnemy_;
        }
        set {
            activeEnemy_ = value;
            if( GameManager.Instance != null )
                GameManager.Instance.GetEnemyAttributeType = activeEnemy_.attributeType;
        }
    }

    public List<Character> Battle_Player {
        get {
            return battle_PlayerList_;
        }
    }

    public List<Character> Battle_Enemy {
        get {
            return battle_EnemyList_;
        }
    }

    public Character Boss {
        get {
            return boss_;
        }
        set {
            boss_ = value;
        }
    }

    #endregion

    void Awake() {
        MagiDebug.Log("CharacterManager.Awake");

        instance_ = this;

        if( state_ == null ) {
            state_ = new CState[(int)Character.State.Max];
            state_[(int)Character.State.Respawn] = new CStateRespawn();
            state_[(int)Character.State.Stay] = new CStateStay();
            state_[(int)Character.State.Move] = new CStateMove();
            state_[(int)Character.State.Attack] = new CStateAttack();
            state_[(int)Character.State.Damage] = new CStateDamage();
            state_[(int)Character.State.Death] = new CStateDeath();
        }
    }

    IEnumerator Start() {
        while( PatchManager.Instance == null ) {
            yield return null;
        }

        LoadAssetbundle.LoadPrefabComplateCB loadComplateCB = null;
        for( int i = 0; i < (int)ObjectIndex.Type_End; i++ ) {
            loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadObjectComplateCB);
            PrefabManager.Instance.LoadPrefab("", objectPath[i].ToLower(), System.Guid.Empty, loadComplateCB, i);
        }

        InitSwapCharacterBuff();
    }

    /// <summary>
    /// <para>name : GetRootInstance</para>
    /// <para>parameter : </para>
    /// <para>return : GameObject</para>
    /// <para>describe : Return character prefabs parent object.</para>
    /// </summary>
    public GameObject GetRootInstance() {
        if( rootInstance_ == null ) {
            rootInstance_ = GameObject.Find("_InstanceCharacter");
            if( rootInstance_ == null )
                rootInstance_ = new GameObject("_InstanceCharacter");
        }
        return rootInstance_;
    }

    #region Set_Character

    /// <summary>
    /// <para>name : Add</para>
    /// <para>parameter : Guid, Character</para>
    /// <para>return : void</para>
    /// <para>describe : Add hash character group.</para>
    /// </summary>
    public void Add(System.Guid uid, Character character) {
        switch( mode_ ) {
            case EMode.Lobby:
                return;
        }

        if( CheckTeamType(character.teamType) ) {
            character.m_DamageText_Prefab = m_DamageText_Prefab;
            character.m_HpBar_Prefab = m_Char_HpBar_Prefab;
        }
        else {
            character.m_DamageText_Prefab = m_DamageText_Prefab;
            character.m_HpBar_Prefab = m_Mon_HpBar_Prefab;
        }

        if( ClientDataManager.Instance.SelectSceneMode.Equals(ClientDataManager.SceneMode.Dungeon) &&
            DungeonSceneScriptManager.Instance.m_DungeonPlayEvent != null )
            character.m_Parant_Panel = DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.damageTextGroup.transform;

        character.m_WorldCamera = m_WorldCamera;
        character.m_GuiCamera = m_GuiCamera;

        if( hashCharacter_.ContainsKey(uid) == false )
            hashCharacter_.Add(uid, character);
    }

    /// <summary>
    /// <para>name : Remove</para>
    /// <para>parameter : Guid</para>
    /// <para>return : void</para>
    /// <para>describe : Remove hash character group.</para>
    /// </summary>
    public void Remove(System.Guid uid) {
        if( hashCharacter_.ContainsKey(uid) )
            hashCharacter_.Remove(uid);
    }

    /// <summary>
    /// <para>name : AddFromTable</para>
    /// <para>parameter : int, Guid, string, CharacterTeam, object[]</para>
    /// <para>return : bool</para>
    /// <para>describe : Load character object by CharacterTable id.</para>
    /// </summary>
    public bool AddFromTable(int tableID, System.Guid uid, string name, CharacterTeam team, params object[] param) {
        CharacterTableManager.CTable table;
        if( CharacterTableManager.Instance.FindTable(tableID, out table) == false ) {
            MagiDebug.LogError(string.Format("character not found = {0}", tableID));
            return false;
        }

        object[] param2 = new object[param.Length + 1];
        param2[0] = team;
        for( int i = 0; i < param.Length; ++i ) {
            param2[i + 1] = param[i];
        }

        LoadAssetbundle.LoadPrefabComplateCB loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadPrefabComplateCB);
        PrefabManager.Instance.LoadPrefab(tableID.ToString(), table.prefab.ToLower(), uid, loadComplateCB, param2);
        return true;
    }

    /// <summary>
    /// <para>name : LoadFromTable</para>
    /// <para>parameter : int, Guid, string, LoadAssetbundle.LoadPrefabComplateCB, object[]</para>
    /// <para>return : bool</para>
    /// <para>describe : Load character prefab by CharacterTable id.</para>
    /// </summary>
    public bool LoadFromTable(int tableID, System.Guid uid, string name, LoadAssetbundle.LoadPrefabComplateCB loadComplateCB, params object[] param) {
        CharacterTableManager.CTable table;
        if( CharacterTableManager.Instance.FindTable(tableID, out table) == false ) {
            MagiDebug.LogError(string.Format("character not found = {0}", tableID));
            return false;
        }

        PrefabManager.Instance.LoadPrefab(tableID.ToString(), table.prefab.ToLower(), uid, loadComplateCB, param);
        return true;
    }

    /// <summary>
    /// <para>name : LoadPrefabComplateCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load prefab and Instantiate object.</para>
    /// </summary>
    void LoadPrefabComplateCB(string tableName, GameObject go, System.Guid uid, string name, params object[] param) {
        if( go == null )
            return;

        switch( mode_ ) {
            case EMode.Lobby: {
                }
                break;
            case EMode.Game: {
                    RandomPositionCB randomPositionCB = null;
                    CharacterLoadComplateCB characterLoadComplateCB = null;
                    Vector3 position = Vector3.zero;

                    CharacterTeam team = (CharacterTeam)param[0];
                    switch( team ) {
                        case CharacterTeam.Type_Enemy: {
                                if( param.Length >= 2 ) {
                                    randomPositionCB = param[2] as RandomPositionCB;
                                    if( randomPositionCB(out position) == false ) {
                                        MagiDebug.LogError("RandomPositionCB Error");
                                        return;
                                    }
                                    characterLoadComplateCB = param[1] as CharacterLoadComplateCB;
                                }

                                if( randomPositionCB == null ) {
                                    position = CharacterManager.Instance.ActivePlayer.transform.position;
                                }
                            }
                            break;
                        default: {
                                if( hashCharacter_.ContainsKey(uid) == true ) {
                                    MagiDebug.LogError("LoadPrefabComplateCB - Same key already - " + uid.ToString());
                                    return;
                                }

                                if( param.Length >= 2 ) {
                                    if( param[1] is CharacterLoadComplateCB ) {
                                        characterLoadComplateCB = param[1] as CharacterLoadComplateCB;
                                    }
                                }
                            }
                            break;
                    }

                    GameObject igo = Instantiate(go, position, Random.rotationUniform) as GameObject;
                    var rootInstance = GetRootInstance();
                    igo.transform.parent = rootInstance.transform;

                    Character character = igo.GetComponent<Character>();
                    if( character == null ) {
                        MagiDebug.LogError(string.Format("character == null, tableName = {0}", tableName));
                        return;
                    }

                    character.UID = uid;
                    character.Name = name;
                    character.tableID = System.Convert.ToInt32(tableName);
                    character.teamType = (CharacterTeam)param[0];

                    StageTableManager.Category stageCategory = StageTableManager.Category.NORMAL;
                    if( StageManager.Instance != null && StageManager.Instance.stageTable != null ) {
                        stageCategory = StageManager.Instance.stageTable.category;
                    }

                    switch( stageCategory ) {
                        case StageTableManager.Category.TUTORIAL:
                        case StageTableManager.Category.NORMAL:
                        case StageTableManager.Category.TIME_ATTACK:
                        case StageTableManager.Category.INFINITE: 
                        case StageTableManager.Category.BOSS_ATTACK: {
                                switch( team ) {
                                    case CharacterTeam.Type_Player:
                                        battle_PlayerList_.Add(character);

                                        NavMeshAgent agent_ = igo.GetComponent<NavMeshAgent>();
                                        agent_.avoidancePriority = 1;

                                        if( uid.Equals(activePlayerUID_) )
                                            activePlayer_ = character;

                                        break;

                                    case CharacterTeam.Type_Enemy:
                                        if( bossUID_ != System.Guid.Empty && uid.Equals(bossUID_) )
                                            boss_ = character;
                                        if( StageManager.Instance != null && StageManager.Instance.stageTable != null )
                                            character.level = StageManager.Instance.stageTable.level;

                                        break;
                                }
                            }
                            break;

                        case StageTableManager.Category.PVP33:
                            switch( team ) {
                                case CharacterTeam.Type_Player:
                                    for( int i = 0; i < battle_PlayerUID_.Count; i++ ) {
                                        if( uid.Equals(activePlayerUID_) )
                                            activePlayer_ = character;

                                        if( uid.Equals(battle_PlayerUID_[i]) ) {
                                            battle_PlayerList_.Add(character);
                                            break;
                                        }
                                    }

                                    break;

                                default:
                                    for( int i = 0; i < battle_EnemyUID_.Count; i++ ) {
                                        if( uid.Equals(battle_EnemyUID_[i]) ) {
                                            battle_EnemyList_.Add(character);
                                            break;
                                        }
                                    }

                                    break;
                            }

                            break;

                        case StageTableManager.Category.PVP_TEST:
                            switch( team ) {
                                case CharacterTeam.Type_Player:
                                    for( int i = 0; i < battle_PlayerUID_.Count; i++ ) {
                                        if( uid.Equals(battle_PlayerUID_[i]) ) {
                                            battle_PlayerList_.Add(character);
                                            break;
                                        }
                                    }

                                    break;

                                default:
                                    for( int i = 0; i < battle_EnemyUID_.Count; i++ ) {
                                        if( uid.Equals(battle_EnemyUID_[i]) ) {
                                            battle_EnemyList_.Add(character);
                                            break;
                                        }
                                    }

                                    break;
                            }

                            break;

                        case StageTableManager.Category.EXPEDITION:
                            switch( team ) {
                                case CharacterTeam.Type_Player:
                                    for( int i = 0; i < battle_PlayerUID_.Count; i++ ) {
                                        if( uid.Equals(activePlayerUID_) )
                                            activePlayer_ = character;

                                        if( uid.Equals(battle_PlayerUID_[i]) ) {
                                            battle_PlayerList_.Add(character);
                                            break;
                                        }
                                    }

                                    break;

                                default:
                                    for( int i = 0; i < battle_EnemyUID_.Count; i++ ) {
                                        if( uid.Equals(activeEnemyUID_) )
                                            ActiveEnemy = character;

                                        if( uid.Equals(battle_EnemyUID_[i]) ) {
                                            battle_EnemyList_.Add(character);
                                            break;
                                        }
                                    }

                                    break;
                            }

                            break;
                    }

                    int nPlayerLevel = 0;
                    if( ClientDataManager.Instance.FindPlayTeam_Info_Level(uid, team, out nPlayerLevel) )
                        character.level = nPlayerLevel;

                    var playerController = character.GetComponent<PlayerController>();
                    var aiCharacter = character.GetComponent<AICharacter>();

                    switch( character.teamType ) {
                        case CharacterTeam.Type_Player:
                            playerController.enabled = true;
                            aiCharacter.enabled = false;
                            break;

                        case CharacterTeam.Type_MatchEnemy:
                            playerController.enabled = true;
                            aiCharacter.enabled = false;
                            break;

                        default:
                            playerController.enabled = false;
                            aiCharacter.enabled = true;
                            break;
                    }

                    if( characterLoadComplateCB != null ) {
                        characterLoadComplateCB(character);
                    }
                }
                break;
        }
    }

    #endregion

    #region Find_Character

    /// <summary>
    /// <para>name : GetAttackRangeDistance</para>
    /// <para>parameter : Character, Character</para>
    /// <para>return : float</para>
    /// <para>describe : Return center object to target object magnitude value.</para>
    /// </summary>
    public float GetAttackRangeDistance(Vector3 center, Character t) {
        return (center - t.transform.position).magnitude - t.radius;
    }

    /// <summary>
    /// <para>name : GetAttackRangeDistance</para>
    /// <para>parameter : Character, Character</para>
    /// <para>return : float</para>
    /// <para>describe : Return center object to target object magnitude value.</para>
    /// </summary>
    public float GetAttackRangeDistance(Character center, Character t) {
        return (center.transform.position - t.transform.position).magnitude - ((center.radius + t.radius));
    }

    /// <summary>
    /// <para>name : FindCharacterInRange</para>
    /// <para>parameter : out (Character)List, CharacterTeam, Vector3, float</para>
    /// <para>return : bool</para>
    /// <para>describe : Find character team objects by (float)parameter.</para>
    /// </summary>
    public bool FindCharacterInRange(out List<Character> charList, CharacterTeam exclusionTeam, Character center, float range) {
        charList = null;
        if( hashCharacter_ == null )
            return false;

        charList = new List<Character>();
        foreach( Character c in hashCharacter_.Values ) {
            if( c == null )
                continue;
            if( CheckTeamType(exclusionTeam, c.teamType) )
                continue;

            switch( c.state ) {
                case Character.State.Respawn:
                case Character.State.Death:
                    continue;
                default:
                    if( c.IsActive() == false )
                        continue;
                    if( c.CheckCharacterAlive == false )
                        continue;
                    break;
            }

            if( GetAttackRangeDistance(center, c) < range )
                charList.Add(c);

            charList.Sort((x, y) => GetAttackRangeDistance(center, x).CompareTo(GetAttackRangeDistance(center, y)));
        }

        return charList.Count != 0;
    }

    /// <summary>
    /// <para>name : FindCharacterInRange</para>
    /// <para>parameter : out (Character)List, CharacterTeam, Vector3, float</para>
    /// <para>return : bool</para>
    /// <para>describe : Find character team objects by (float)parameter.</para>
    /// </summary>
    public bool FindCharacterInRange(out List<Character> charList, CharacterTeam exclusionTeam, Vector3 center, float range) {
        charList = null;
        if( hashCharacter_ == null )
            return false;

        charList = new List<Character>();
        foreach( Character c in hashCharacter_.Values ) {
            if( c == null )
                continue;

            if( exclusionTeam.Equals(c.teamType) )
                continue;

            switch( c.state ) {
                case Character.State.Respawn:
                case Character.State.Death: {
                        continue;
                    }
                default: {
                        if( c.IsActive() == false )
                            continue;
                        if( c.CheckCharacterAlive == false )
                            continue;
                    }
                    break;
            }

            if( GetAttackRangeDistance(center, c) < range ) {
                charList.Add(c);
            }

            charList.Sort((x, y) => GetAttackRangeDistance(center, x).CompareTo(GetAttackRangeDistance(center, y)));
        }

        return charList.Count != 0;
    }

    /// <summary>
    /// <para>name : FindCharacterInTeam</para>
    /// <para>parameter : out (Character)List, CharacterTeam</para>
    /// <para>return : bool</para>
    /// <para>describe : Find character team objects by (CharacterTeam)parameter.</para>
    /// </summary>
    public bool FindCharacterInTeam(out List<Character> charList, CharacterTeam team) {
        charList = new List<Character>();

        foreach( Character c in hashCharacter_.Values ) {
            if( CheckTeamType(team, c.teamType) == false )
                continue;

            switch( c.state ) {
                case Character.State.Respawn:
                case Character.State.Death:
                    continue;
            }

            charList.Add(c);
        }

        return charList.Count != 0;
    }

    /// <summary>
    /// <para>name : FindCharacterTeamInRange</para>
    /// <para>parameter : out (Character)List, CharacterTeam, Vector3, float</para>
    /// <para>return : bool</para>
    /// <para>describe : Find character team objects by (float)parameter.</para>
    /// </summary>
    public bool FindCharacterTeamInRange(out List<Character> charList, CharacterTeam teamType, Vector3 center, float range) {
        charList = null;
        if( hashCharacter_ == null )
            return false;

        charList = new List<Character>();
        foreach( Character c in hashCharacter_.Values ) {
            if( teamType != c.teamType )
                continue;

            switch( c.state ) {
                case Character.State.Respawn:
                case Character.State.Death:
                    continue;
                default: {
                        if( c.IsActive() == false )
                            continue;
                    }
                    break;
            }

            if( c == null )
                continue;

            if( (c.transform.position - center).magnitude < range ) {
                charList.Add(c);
            }
            charList.Sort((x, y) => (x.transform.position - center).magnitude.CompareTo((y.transform.position - center).magnitude));
        }
        return charList.Count != 0;
    }

    /// <summary>
    /// <para>name : FindCharacter</para>
    /// <para>parameter : Guid, out Character</para>
    /// <para>return : bool</para>
    /// <para>describe : Find character object by (Guid)parameter.</para>
    /// </summary>
    public bool FindCharacter(System.Guid uid, out Character resultCharacter) {
        return hashCharacter_.TryGetValue(uid, out resultCharacter);
    }

    /// <summary>
    /// <para>name : FindHashPlayer</para>
    /// <para>parameter : Guid, out Character</para>
    /// <para>return : bool</para>
    /// <para>describe : Find player object by (Guid)parameter.</para>
    /// </summary>
    public bool FindHashPlayer(System.Guid uid, out Character result) {
        Character resultCharacter = null;
        bool isExist = battle_PlayerList_.Exists(delegate(Character a) {
            if( a.UID.Equals(uid) ) {
                resultCharacter = a;
                return true;
            }

            else
                return false;
        });

        result = resultCharacter;
        return isExist;
    }

    /// <summary>
    /// <para>name : FindHashEnemy</para>
    /// <para>parameter : Guid, out Character</para>
    /// <para>return : bool</para>
    /// <para>describe : Find enemy object by (Guid)parameter.</para>
    /// </summary>
    public bool FindHashEnemy(System.Guid uid, out Character result) {
        Character resultCharacter = null;
        bool isExist = battle_EnemyList_.Exists(delegate(Character a) {
            if( a.UID.Equals(uid) ) {
                resultCharacter = a;
                return true;
            }

            else
                return false;
        });

        result = resultCharacter;
        return isExist;
    }

    #endregion

    #region Change_Character

    /// <summary>
    /// <para>name : InitSwapCharacterBuff</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Initialize swap buff.</para>
    /// </summary>
    private void InitSwapCharacterBuff() {
        ESkillProperty[] eSkillPropertyArray = { new ESkillProperty(ESkillType.Buff_AddPhysicalDefense, ESkillBaseStatus.None, false, 1.5f, 0, 0, 0, SWAP_BUFF_TIME, 0, 0, ""),
                                                   new ESkillProperty(ESkillType.Buff_AddMagicDefense, ESkillBaseStatus.None, false, 1.5f, 0, 0, 0, SWAP_BUFF_TIME, 0, 0, ""), 
                                                   new ESkillProperty(ESkillType.Buff_SuperArmor, ESkillBaseStatus.None, false, 0, 0, 0, 0, SWAP_BUFF_TIME, 0, 0, "") };
        eSwapCharacterSkillPropertyList = eSkillPropertyArray.ToList();
    }

    /// <summary>
    /// <para>name : ChangeActivePlayer</para>
    /// <para>parameter : Guid</para>
    /// <para>return : bool</para>
    /// <para>describe : Change main player character on player team.</para>
    /// </summary>
    public bool ChangeActivePlayer(System.Guid uid) {
        if( activePlayer_ == null )
            return false;

        Character selectCharacter = battle_PlayerList_.Find(delegate(Character a) {
            return a.UID.Equals(uid);
        });

        if( selectCharacter == null || selectCharacter.CheckCharacterAlive == false )
            return false;

        Vector3 position = activePlayer_.transform.position;

        for( int i = 0; i < battle_PlayerList_.Count; i++ ) {
            if( battle_PlayerList_[i].Equals(selectCharacter) ) {
                activePlayer_ = battle_PlayerList_[i];
                activePlayerUID_ = selectCharacter.UID;

                activePlayer_.SetActive(true);
                activePlayer_.SetStateStay();

                if( eSwapCharacterSkillPropertyList != null )
                    activePlayer_.AddBuff(eSwapCharacterSkillPropertyList);

                PlayerTeamWrap(position);
            }

            else {
                battle_PlayerList_[i].ResetState();
                battle_PlayerList_[i].SetActive(false);
            }
        }

        if( changePlayerEffect_ != null ) {
            Instantiate(changePlayerEffect_, activePlayer_.transform.position, Quaternion.identity);
        }

        return true;
    }

    /// <summary>
    /// <para>name : ChangeActiveEnemy</para>
    /// <para>parameter : Guid</para>
    /// <para>return : bool</para>
    /// <para>describe : Change main enemy character on enemy team.</para>
    /// </summary>
    public bool ChangeActiveEnemy(System.Guid uid) {
        if( activeEnemy_ == null )
            return false;

        Character selectCharacter = battle_EnemyList_.Find(delegate(Character a) {
            return a.UID.Equals(uid);
        });

        if( selectCharacter == null || selectCharacter.CheckCharacterAlive == false )
            return false;
        
        Vector3 position = activeEnemy_.transform.position;

        for( int i = 0; i < battle_EnemyList_.Count; i++ ) {
            if( battle_EnemyList_[i].Equals(selectCharacter) ) {
                ActiveEnemy = battle_EnemyList_[i];
                activeEnemyUID_ = selectCharacter.UID;

                activeEnemy_.SetActive(true);
                activeEnemy_.SetStateStay();

                activeEnemy_.Warp(position);

                if( eSwapCharacterSkillPropertyList != null )
                    activeEnemy_.AddBuff(eSwapCharacterSkillPropertyList);

                GameManager.Instance.GetEnemyAttributeType = activeEnemy_.attributeType;
            }

            else {
                battle_EnemyList_[i].ResetState();
                battle_EnemyList_[i].SetActive(false);
            }
        }

        if( changePlayerEffect_ != null ) {
            Instantiate(changePlayerEffect_, position, Quaternion.identity);
        }

        return true;
    }

    /// <summary>
    /// <para>name : ChangeControlPlayer</para>
    /// <para>parameter : Guid</para>
    /// <para>return : bool</para>
    /// <para>describe : Change control character on player team.</para>
    /// </summary>
    public bool ChangeControlPlayer(System.Guid uid) {
        if( activePlayer_ == null )
            return false;

        Character selectCharacter = battle_PlayerList_.Find(delegate(Character a) {
            return a.UID.Equals(uid);
        });

        if( selectCharacter == null || selectCharacter.CheckCharacterAlive == false )
            return false;

        activePlayer_.RefreshHpBarActivePlayer(false);
        selectCharacter.RefreshHpBarActivePlayer(true);

        activePlayer_ = selectCharacter;
        activePlayerUID_ = selectCharacter.UID;

        return true;
    }

    /// <summary>
    /// <para>name : GetChangeAvailablePlayer</para>
    /// <para>parameter : out Guid</para>
    /// <para>return : bool</para>
    /// <para>describe : Change player team member.</para>
    /// </summary>
    public bool GetChangeAvailablePlayer(out System.Guid uid) {
        uid = System.Guid.Empty;

        for( int i = 0; i < battle_PlayerList_.Count; i++ ) {
            if( battle_PlayerList_[i] == null )
                continue;
            if( activePlayer_.Equals(battle_PlayerList_[i]) )
                continue;
            if( battle_PlayerList_[i].CheckCharacterAlive == false )
                continue;
            if( ClientDataManager.Instance.Auto_Play &&
                BattleItemManager.Instance.GetBattleItemState(BattleItemType.Type_Auto_Command, ClientDataManager.Instance.SelectGameMode) &&
                GameManager.Instance.GetEnemyAttributeType.Equals(battle_PlayerList_[i].attributeType) ) {
                uid = battle_PlayerList_[i].UID;
                break;
            }

            uid = battle_PlayerList_[i].UID;
            break;
        }

        if( uid.Equals(System.Guid.Empty) )
            return false;

        return true;
    }

    /// <summary>
    /// <para>name : GetChangeAvailableEnemy</para>
    /// <para>parameter : out Guid</para>
    /// <para>return : bool</para>
    /// <para>describe : Change enemy team member.</para>
    /// </summary>
    public bool GetChangeAvailableEnemy(out System.Guid uid) {
        uid = System.Guid.Empty;

        for( int i = 0; i < battle_EnemyList_.Count; i++ ) {
            if( battle_EnemyList_[i] == null )
                continue;
            if( activeEnemy_.Equals(battle_EnemyList_[i]) )
                continue;
            if( battle_EnemyList_[i].CheckCharacterAlive == false )
                continue;

            uid = battle_EnemyList_[i].UID;
            break;
        }

        if( uid.Equals(System.Guid.Empty) )
            return false;

        return true;
    }

    #endregion

    #region Move_Character

    /// <summary>
    /// <para>name : PlayerMoveTarget</para>
    /// <para>parameter : Vector3</para>
    /// <para>return : void</para>
    /// <para>describe : Warp player to (Vector3)parameter.</para>
    /// </summary>
    public bool PlayerMoveTarget(Vector3 position) {
        switch( CharacterManager.Instance.ActivePlayer.state ) {
            case Character.State.Stay:
            case Character.State.Move:
                break;
            default:
                return false;
        }

        PlayerController playerController = activePlayer_.GetComponent<PlayerController>();
        playerController.MoveTarget(position);

        return true;
    }

    /// <summary>
    /// <para>name : PlayerTeamWrap</para>
    /// <para>parameter : Vector3</para>
    /// <para>return : void</para>
    /// <para>describe : Warp player team to (Vector3)parameter.</para>
    /// </summary>
    public void PlayerTeamWrap(Vector3 position) {
        for( int i = 0; i < battle_PlayerList_.Count; i++ ) {
            battle_PlayerList_[i].Warp(position);
            battle_PlayerList_[i].SetMovementTargetEnable(false);
        }
    }

    #endregion

    #region Auto

    public void SetAuto_UseSkillActivePlayerCB(Auto_UseSkillActivePlayerCB auto_UseSkillActivePlayerCB) {
        auto_UseSkillActivePlayerCB_ = auto_UseSkillActivePlayerCB;
    }

    public void Auto_UseSkillActivePlayer(int skillTableID) {
        if( auto_UseSkillActivePlayerCB_ != null ) {
            auto_UseSkillActivePlayerCB_(skillTableID);
        }
    }

    Auto_ChangeActivePlayerCB auto_ChangeActivePlayerCB_;
    public void SetAuto_ChangeActivePlayerCB(Auto_ChangeActivePlayerCB auto_ChangeActivePlayerCB) {
        auto_ChangeActivePlayerCB_ = auto_ChangeActivePlayerCB;
    }

    public void Auto_ChangeActivePlayer(System.Guid uid) {
        if( auto_ChangeActivePlayerCB_ != null ) {
            auto_ChangeActivePlayerCB_(uid);
        }
    }

    #endregion

    #region Util

    /// <summary>
    /// <para>name : GetCharacterAttributeSpriteName</para>
    /// <para>parameter : CharacterType, out string, out string</para>
    /// <para>return : void</para>
    /// <para>describe : Get UI sprite name from (CharacterType)parameter.</para>
    /// </summary>
    public void GetCharacterAttributeSpriteName(CharacterType type_, out string bgName_, out string lbName_) { // 속성 배경 -> 속성 배경과 레벨 라벨의 배경
        bgName_ = "";
        lbName_ = "";

        switch( type_ ) {
            case CharacterType.POWER:
                bgName_ = "Character_TypePower_Bg";
                lbName_ = "Type_Power_LvBg";
                break;

            case CharacterType.MAGIC:
                bgName_ = "Character_TypeMagic_Bg";
                lbName_ = "Type_Magic_LvBg";
                break;

            case CharacterType.SPEED:
                bgName_ = "Character_TypeSpeed_Bg";
                lbName_ = "Type_Speed_LvBg";
                break;
        }
    }

    /// <summary>
    /// <para>name : GetCharacterAttributeSpriteName</para>
    /// <para>parameter : CharacterType</para>
    /// <para>return : string</para>
    /// <para>describe : Return UI sprite name from (CharacterType)parameter.</para>
    /// </summary>
    public string GetCharacterAttributeSpriteName(CharacterType type_) {
        switch( type_ ) {
            case CharacterType.POWER:
                return "Type_Power_icon";

            case CharacterType.MAGIC:
                return "Type_Magic_icon";

            case CharacterType.SPEED:
                return "Type_Speed_icon";
        }

        return "";
    }

    /// <summary>
    /// <para>name : GetCharacterAttributeSpriteName</para>
    /// <para>parameter : CharacterType</para>
    /// <para>return : int</para>
    /// <para>describe : Return UI string id from (CharacterType)parameter.</para>
    /// </summary>
    public int GetCharacterAttributeStringID(CharacterType type_) {
        switch( type_ ) {
            case CharacterType.POWER:
                return 1119001;

            case CharacterType.MAGIC:
                return 1119003;

            case CharacterType.SPEED:
                return 1119002;
        }

        return 0;
    }

    /// <summary>
    /// <para>name : GetAdvantageCharacterType</para>
    /// <para>parameter : CharacterType</para>
    /// <para>return : CharacterType</para>
    /// <para>describe : Return advantage type by (CharacterType)parameter.</para>
    /// </summary>
    public CharacterType GetAdvantageCharacterType(CharacterType myType) {
        int typeSite = (int)myType + 1;
        if( typeSite.Equals(typeArray.Length) )
            typeSite = 0;

        return typeArray[typeSite];
    }

    /// <summary>
    /// <para>name : GetDisAdvantageCharacterType</para>
    /// <para>parameter : CharacterType</para>
    /// <para>return : CharacterType</para>
    /// <para>describe : Return disadvantage type by (CharacterType)parameter.</para>
    /// </summary>
    public CharacterType GetDisAdvantageCharacterType(CharacterType myType) {
        int typeSite = (int)myType + 2;
        if( typeSite >= typeArray.Length )
            typeSite -= typeArray.Length;

        return typeArray[typeSite];
    }

    /// <summary>
    /// <para>name : EnemyDeathAll</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Destroy all enemy object.</para>
    /// </summary>
    public void EnemyDeathAll() {
        for( int i = 0; i < battle_EnemyList_.Count; i++ ) {
            battle_EnemyList_[i].Death();
        }
    }

    /// <summary>
    /// <para>name : CheckStageClear</para>
    /// <para>parameter : </para>
    /// <para>return : bool</para>
    /// <para>describe : Check if enemy all death.</para>
    /// </summary>
    public bool CheckStageClear() {
        for( int i = 0; i < battle_EnemyList_.Count; i++ ) {
            if( battle_EnemyList_[i] != null ) {
                if( battle_EnemyList_[i].CheckCharacterAlive )
                    return false;
            }
        }

        return true;
    }

    public float characterPointFactor {
        set {
            characterPointFactor_ = value;
            foreach( var character in hashCharacter_.Values ) {
                character.SetCharacterPointFactor(value);
            }
        }
        get {
            return characterPointFactor_;
        }
    }

    /// <summary>
    /// <para>name : GetAvailableCharacterList</para>
    /// <para>parameter : CharacterTeam, ref (Character)List</para>
    /// <para>return : bool</para>
    /// <para>describe : Get character list if object available.</para>
    /// </summary>
    public bool GetAvailableCharacterList(CharacterTeam team, ref List<Character> list) {
        list.Clear();

        if( CheckTeamType(team) ) {
            for( int i = 0; i < battle_PlayerList_.Count; i++ ) {
                if( battle_PlayerList_[i].IsActive() )
                    list.Add(battle_PlayerList_[i]);
            }
        }

        else {
            for( int i = 0; i < battle_EnemyList_.Count; i++ ) {
                if( battle_EnemyList_[i].IsActive() )
                    list.Add(battle_EnemyList_[i]);
            }
        }

        return !list.Count.Equals(0);
    }

    /// <summary>
    /// <para>name : CheckTeamType</para>
    /// <para>parameter : CharacterTeam</para>
    /// <para>return : bool</para>
    /// <para>describe : Check team type if equals (CharacterTeam)parameter.</para>
    /// </summary>
    public bool CheckTeamType(CharacterTeam type) { // 아군일 경우 true, 적일 경우 false
        switch( type ) {
            case CharacterTeam.Type_Player:
            case CharacterTeam.Type_Minion:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// <para>name : CheckTeamType</para>
    /// <para>parameter : CharacterTeam, CharacterTeam</para>
    /// <para>return : bool</para>
    /// <para>describe : Check team type if equals (CharacterTeam)parameter.</para>
    /// </summary>
    public bool CheckTeamType(CharacterTeam myType, CharacterTeam otherType) {
        switch( myType ) {
            case CharacterTeam.Type_Player:
            case CharacterTeam.Type_Minion:
                return CheckTeamType(otherType);

            default:
                return !CheckTeamType(otherType);
        }
    }

    #endregion

    #region ADD_MINION

    /// <summary>
    /// <para>name : AddMinion</para>
    /// <para>parameter : Character, ESkillProperty</para>
    /// <para>return : void</para>
    /// <para>describe : Add minion character from (ESkillProperty)parameter.</para>
    /// </summary>
    public void AddMinion(Character masterCharacter, ESkillProperty property) {
        CharacterTableManager.CTable characterTable = null;
        if( CharacterTableManager.Instance.FindTable(property.useValue, out characterTable) ) {
            int minionLevel = Mathf.RoundToInt(masterCharacter.level * (property.baseStatus.Equals(ESkillBaseStatus.Level) ? property.addValue : 1));

            for( int i = 0; i < property.useAddValue; i++ ) {
                LoadAssetbundle.LoadPrefabComplateCB loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadMinionCompleteCB);
                CharacterManager.Instance.LoadFromTable(characterTable.id, System.Guid.NewGuid(), "", loadComplateCB, masterCharacter, minionLevel);
            }
        }
    }

    /// <summary>
    /// <para>name : LoadMinionCompleteCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load minion character prefab.</para>
    /// </summary>
    void LoadMinionCompleteCB(string tableName, GameObject go, System.Guid uid, string name, params object[] param) {
        if( go == null )
            return;

        GameObject igo = Instantiate(go) as GameObject;
        var rootInstance = CharacterManager.Instance.GetRootInstance();
        igo.transform.parent = rootInstance.transform;

        Character masterCharacter = (Character)param[0];
        Character minion = igo.GetComponent<Character>();

        minion.UID = uid;
        minion.Name = name;
        minion.tableID = System.Convert.ToInt32(tableName);
        minion.teamType = CharacterTeam.Type_Minion;
        minion.level = (int)param[1];

        PlayerController playerController = minion.GetComponent<PlayerController>();
        AICharacter aiCharacter = minion.GetComponent<AICharacter>();

        playerController.enabled = true;
        aiCharacter.enabled = false;

        minion.Warp(GetSidePos(masterCharacter.transform.position));

        if( battle_MinionList_ != null && battle_MinionList_.Contains(minion) == false )
            battle_MinionList_.Add(minion);

        minion.InitCharacterState();
        minion.SetMinionRespawn();
    }

    /// <summary>
    /// <para>name : DestroyMinion</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Destroy all minion object.</para>
    /// </summary>
    public void DestroyMinion() {
        if( battle_MinionList_ == null )
            return;

        for( int i = 0; i < battle_MinionList_.Count; i++ ) {
            if( battle_MinionList_[i] != null )
                battle_MinionList_[i].Death();
        }
    }

    /// <summary>
    /// <para>name : GetSidePos</para>
    /// <para>parameter : Vector3</para>
    /// <para>return : Vector3</para>
    /// <para>describe : Return random position - around (Vector3)parametor.</para>
    /// </summary>
    Vector3 GetSidePos(Vector3 origin) {
        Vector3 position = Vector3.zero;
        Vector3 sidePosition = Vector3.zero;
        Vector2 addPosition = Vector3.zero;

        position = origin;
        for( int i = 0; i < 20; ++i ) {
            sidePosition = position;
            addPosition = Random.insideUnitCircle;

            sidePosition.x = addPosition.x;
            sidePosition.z = addPosition.y;

            NavMeshHit nmHit;
            if( NavMesh.Raycast(origin, sidePosition, out nmHit, -1) )
                return position;
        }

        return position;
    }

    #endregion

    #region METAMORPHOSIS

    /// <summary>
    /// <para>name : InitMetamorphosis</para>
    /// <para>parameter : Character, ESkillProperty</para>
    /// <para>return : void</para>
    /// <para>describe : Initialize metamorphosis character from (ESkillProperty)parameter.</para>
    /// </summary>
    public void InitMetamorphosis(Character currentCharacter, ESkillProperty property) {
        CharacterTableManager.CTable characterTable = null;

        if( CharacterTableManager.Instance.FindTable(property.useValue, out characterTable) ) {
            int metaLevel = Mathf.RoundToInt(currentCharacter.level * (property.baseStatus.Equals(ESkillBaseStatus.Level) ? property.addValue : 1));

            LoadAssetbundle.LoadPrefabComplateCB loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadMetaCompleteCB);
            CharacterManager.Instance.LoadFromTable(characterTable.id, System.Guid.NewGuid(), "", loadComplateCB, currentCharacter, metaLevel);
        }
    }

    /// <summary>
    /// <para>name : LoadMetaCompleteCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load metamorphosis character prefab.</para>
    /// </summary>
    void LoadMetaCompleteCB(string tableName, GameObject go, System.Guid uid, string name, params object[] param) {
        if( go == null )
            return;

        GameObject igo = Instantiate(go) as GameObject;
        var rootInstance = CharacterManager.Instance.GetRootInstance();
        igo.transform.parent = rootInstance.transform;

        Character currentCharacter = (Character)param[0];
        Character metaCharacter = igo.GetComponent<Character>();

        metaCharacter.UID = uid;
        metaCharacter.Name = name;
        metaCharacter.tableID = System.Convert.ToInt32(tableName);
        metaCharacter.teamType = currentCharacter.teamType;
        metaCharacter.level = (int)param[1];

        PlayerController playerController = metaCharacter.GetComponent<PlayerController>();
        AICharacter aiCharacter = metaCharacter.GetComponent<AICharacter>();

        playerController.enabled = true;
        aiCharacter.enabled = false;

        if( battle_PlayerList_ != null && battle_PlayerList_.Contains(metaCharacter) == false )
            battle_PlayerList_.Add(metaCharacter);

        metaCharacter.InitCharacterState();
        metaCharacter.InitSkill();
        metaCharacter.Warp(currentCharacter.transform);

        ActivePlayer = metaCharacter;
        ActivePlayerUID = uid;

        currentCharacter.ResetState();
        currentCharacter.SetActive(false);

        metaCharacter.SetActive(true);
        metaCharacter.SetStateRespawn();

        if( DungeonSceneScriptManager.Instance.m_DungeonPlayEvent != null )
            DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.RefreshCharacterInfo();
    }

    #endregion

    #region LoadObject

    /// <summary>
    /// <para>name : LoadObjectComplateCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load character UI prefab.</para>
    /// </summary>
    void LoadObjectComplateCB(string tableName, GameObject go, System.Guid uid, string name, params object[] param) {
        if( go == null )
            return;

        switch( (ObjectIndex)param[0] ) {
            case ObjectIndex.Type_AttachPointLight:
                attachPointLightPrefab_ = go;
                break;
            case ObjectIndex.Type_DamageText:
                m_DamageText_Prefab = go.GetComponent<CharacterDamageImageText>();
                break;
            case ObjectIndex.Type_Char_Hp_Bar:
                m_Char_HpBar_Prefab = go.GetComponent<CharacterHpBar>();
                break;
            case ObjectIndex.Type_Mon_Hp_Bar:
                m_Mon_HpBar_Prefab = go.GetComponent<CharacterHpBar>();
                break;
            case ObjectIndex.Type_UI_Effect_Text:
                uiEffectText = go.GetComponent<CharacterEffectText>();
                break;
            case ObjectIndex.Type_UI_Skill_Text:
                uiSkillText = go.GetComponent<CharacterSkillText>();
                break;
            case ObjectIndex.Type_Change_Player_Effect:
                changePlayerEffect_ = go;
                break;
            case ObjectIndex.Type_Hit_Normal_Effect:
                hitNormalEffect_ = go;
                break;
            case ObjectIndex.Type_Hit_Critical_Effect:
                hitCriticalEffect_ = go;
                break;
            case ObjectIndex.Type_Respawn_Effect:
                respawnEffect_ = go;
                break;
            case ObjectIndex.Type_Elit_Character_Effect:
                elitCharacterEffect_ = go;
                break;
        }
    }

    #endregion

    void OnDestroy() {   
        instance_ = null;
        attachPointLightPrefab_= null;
        m_DamageText_Prefab= null;
        m_Char_HpBar_Prefab= null;
        m_Mon_HpBar_Prefab= null;
        uiEffectText= null;
        m_WorldCamera= null;
        m_GuiCamera= null;
        changePlayerEffect_= null;
        hitNormalEffect_= null;
        hitCriticalEffect_= null;
        respawnEffect_= null;
        elitCharacterEffect_= null;
        basicOneLight= null;
        basicCullOffOneLight= null;
        bossRimTexture_= null;
        state_= null;
        typeArray= null;
        battle_PlayerUID_= null;
        battle_EnemyUID_= null;
        rootInstance_= null;
        activePlayer_= null;
        activeEnemy_= null;
        battle_PlayerList_= null;
        battle_EnemyList_= null;
        boss_= null;
        hashCharacter_= null;
        auto_UseSkillActivePlayerCB_= null;
        auto_ChangeActivePlayerCB_= null;
        objectPath = null;
    }
}
