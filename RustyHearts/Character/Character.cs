using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Magi;

[AddComponentMenu("Magi/Character/Character")]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Model))]
[RequireComponent(typeof(CharacterCustomization))]

/// <summary>
/// <para>name : CBuffDebuff</para>
/// <para>describe : character buff/debuff state class.</para>
/// </summary>
public class CBuffDebuff {
    public ESkillType type;
    public float time;
    public float endTime;
    public float value;

    public GameObject effect;
    public int stringID;

    public Character me;
    public Character attacker;

    int timer = 0;
    bool isOnBuffDebuff = false;
    public bool CheckOnBuffDebuff {
        get {
            return isOnBuffDebuff;
        }
    }

    public CBuffDebuff() {
        type = ESkillType.None;

        time = 0;
        endTime = 0;
        value = 0;

        effect = null;
        stringID = 0;

        me = null;
        attacker = null;

        timer = 0;
        isOnBuffDebuff = false;
    }

    /// <summary>
    /// <para>name : StartBuffTime</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Add buff/debuff timer.</para>
    /// </summary>
    public void StartBuffTime() {
        PatchManager.Instance.onTimer += Timer;
        isOnBuffDebuff = true;
    }

    /// <summary>
    /// <para>name : StopBuffTime</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Release buff/debuff timer.</para>
    /// </summary>
    public void StopBuffTime() {
        PatchManager.Instance.onTimer -= Timer;
        isOnBuffDebuff = false;
    }

    /// <summary>
    /// <para>name : Timer</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Buff/debuff timer.</para>
    /// </summary>
    private void Timer() {
        if( me == null || me.CheckCharacterAlive == false ) {
            StopBuffTime();
            return;
        }

        me.AddBuffDebuffHP(value, me);
        me.AddEffectPanel(type, stringID, 0, (int)Mathf.Abs(value));

        timer++;
        if( timer >= time )
            StopBuffTime();
    }
}

/// <summary>
/// <para>name : Character</para>
/// <para>describe : In Game, Character Object class.</para>
/// </summary>
public class Character : MonoBehaviour {
    public enum State {
        None = -1,

        Respawn,
        Stay,
        Move,
        Attack,
        Damage,
        Death,
        MidairDamage,
        DownDamage,
        JumpTargetAttack,
        Skill,
        DistanceMaintenance,
        Growth,
        Summon,

        BuffDebuff_Fear,
        BuffDebuff_Freeze,
        BuffDebuff_Stun,
        BuffDebuff_Sleep,
        BuffDebuff_Stone,

        SelectStay,

        Add_Pool,

        Max,
    };

    public enum SubState {
        Init,
        Process01,
        Process02,
        Process03,
        Process04,
        End,
    }

    public enum LobbyState {
        Type_None = -1,

        Type_Summon,
        Type_SelectedStay,
        Type_Stay,

        Type_End,
    }

    public enum AgentState {
        MOVE = -1,
        STOP = 0,
    };

    public float respawnWaitTime_;

    [HideInInspector]
    public Transform shadowTransform_;

    private float jumpSpeed_;
    private CharacterController characterController_;
    private int tableID_;
    private float weight_ = 100.0f;
    private System.Guid uid_;
    private string name_;
    private int countAttackComboID_ = 0;
    private float nextAttackTime_ = 0;

    private State state_ = State.Stay;
    private int attackIndex_ = 0;

    private bool movementTargetEnable_;

    private bool isMovementWait = false;
    private float movementWaitTime = 0.35f;

    private Character characterTarget_;
    private Vector3 movementTarget_;
    private Vector3 movementStart_;

    private Character enemyPresent_ = null;
    private int sessionID_ = 0;
    private bool attackIncapacity_ = false;
    private NavMeshAgent agent_;
    private Model model_;

    private Animator animator_ = null;
    private AnimatorStateInfo currentBaseState_;
    private CharacterTableManager.CTable characterTable_;
    private CharacterComboTable combo_;
    private CharacterComboTable.CTable selectedCombo_;
    private SkillTableManager.CTable selectedComboSkill_;
    private CharacterAttackTable attackTable_;
    private CharacterPartsTable partsTable_;

    private List<SkillTableManager.CTable> skillTable_;

    private bool attackHoming_ = false;
    private int attackStateNameHash_ = 0;
    private string attackStateName_ = "";

    private float stateTime_ = 0;
    private int currentStateNameHash_ = 0;
    private AnimatorStateInfo oldBaseState_;
    private float hp_ = 100;
    private CharacterJump characterJump_ = null;
    private bool midair_ = false;
    private float radius_ = 0;
    private float agentRadius_ = 0;

    private int level_ = 1;
    private int maxHP_ = 0;
    private int attack_ = 0;
    private int defense_ = 0;
    private int magicAttack_ = 0;
    private int magicDefense_ = 0;
    private float critical_ = 0;
    private float avoidChance_ = 0;
    private float attackSpeed_ = 2.0f;
    private float moveSpeed_ = 0;
    private float actionInterval = 0.5f;

    [HideInInspector]
    public CharacterType attributeType = CharacterType.NONE;
    [HideInInspector]
    public CharacterTeam teamType = CharacterTeam.Type_None;

    private float scale_ = 1.0f;

    private float m_fDamagePanelTime = 0f;
    private int m_nDamagePanelCount = 0;

    private float m_fEffectPanelTime = 0f;
    private int m_nEffectPanelCount = 0;

    private bool attackCatchUpEnemy_ = false;
    private float attackCatchUpMoveSpeed_ = 0;

    private bool attackPullEnemy_ = true;
    private float attackPullEnemyRange_ = 0;
    private float attackPullMoveSpeed_ = 0;

    private int m_nLobby_Index = 0;
    private int attackSkillID_ = 0;

    private bool isInitCharacter = true;
    private bool isInitSkill = false;
    private float currentY_ = 0;
    private float prefabScale_ = 1;

    private SubState subState_;
    private Vector3 centerPosition_ = Vector3.zero;

    private CharacterEffectText m_Effect_Panel;

    private List<Material> materials_ = new List<Material>();
    private Dictionary<int, Material> dMaterialList_ = new Dictionary<int, Material>();

    [HideInInspector]
    public GameObject m_Elit_Effect;
    [HideInInspector]
    public GameObject m_DamageText_Target;
    [HideInInspector]
    public CharacterDamageImageText m_DamageText_Prefab;
    [HideInInspector]
    public GameObject m_HpBar_Target;
    [HideInInspector]
    public CharacterHpBar m_HpBar_Prefab;
    [HideInInspector]
    public GameObject m_Info_Target;
    [HideInInspector]
    public GameObject m_Effect_Target;
    [HideInInspector]
    public Transform m_Parant_Panel;
    [HideInInspector]
    public CharacterHpBar m_HpBar_Panel;
    [HideInInspector]
    public Camera m_WorldCamera;
    [HideInInspector]
    public Camera m_GuiCamera;
    [HideInInspector]
    public LobbyState lobbyState = LobbyState.Type_None;

    public CharacterTableManager.CTable STable {
        get {
            if( characterTable_ != null )
                return characterTable_;
            else {
                if( CharacterTableManager.Instance.FindTable(tableID_, out characterTable_) )
                    return characterTable_;
                else
                    return null;
            }
        }
    }

    public bool IsLoadComplate {
        get {
            return (combo_ != null && attackTable_ != null);
        }
    }

    public int Lobby_Index {
        get {
            return m_nLobby_Index;
        }
        set {
            m_nLobby_Index = value;
        }
    }

    public float Scale {
        get {
            return scale_;
        }
    }

    public string AttackStateName {
        get {
            return attackStateName_;
        }
    }

    public int DropGold {
        get;
        set;
    }

    public int ChargeCount {
        get;
        set;
    }

    public Vector3 centerPosition {
        get {
            return centerPosition_;
        }
    }

    public bool CheckCharacterDeath {
        get {
            if( state_.Equals(State.Add_Pool) ||
                state_.Equals(State.Death) )
                return true;
            if( hp_ < 1.0f )
                return true;

            return false;
        }
    }

    public bool CheckCharacterAlive {
        get {
            if( state_.Equals(State.Death) )
                return false;
            if( hp_ < 1.0f )
                return false;

            return true;
        }
    }

    public Character EnemyPresent {
        get {
            if( enemyPresent_ != null ) {
                if( enemyPresent_.CheckCharacterAlive && enemyPresent_.IsActive() )
                    return enemyPresent_;

                enemyPresent_ = null;
            }

            return enemyPresent_;
        }
        set {
            enemyPresent_ = value;
            if( enemyPresent_ != null ) {
                if( CharacterManager.Instance.CheckTeamType(teamType, enemyPresent_.teamType) )
                    enemyPresent_ = null;

                List<Character> friends;
                if( CharacterManager.Instance.FindCharacterTeamInRange(out friends, teamType, transform.position, 2.0f) == true ) {
                    for( int i = 0; i < friends.Count; i++ ) {
                        if( friends[i].enemyPresent_ == null )
                            friends[i].enemyPresent_ = enemyPresent_;
                    }
                }
            }
        }
    }

    public State state {
        get {
            return state_;
        }
    }
    public State nextState {
        get {
            return nextState_;
        }
    }
    public System.Guid UID {
        get {
            return uid_;
        }
        set {
            uid_ = value;
        }
    }
    public string Name {
        get {
            return name_;
        }
        set {
            name_ = value;
        }
    }
    public int tableID {
        get {
            return tableID_;
        }
        set {
            tableID_ = value;
        }
    }

    public NavMeshAgent Agent {
        get {
            if( agent_ == null )
                agent_ = gameObject.GetComponent<NavMeshAgent>();

            return agent_;
        }
    }

    public float jumpSpeed {
        get {
            return jumpSpeed_;
        }
        set {
            jumpSpeed_ = value;
        }
    }

    public CharacterController characterController {
        get {
            return characterController_;
        }
    }

    public int level {
        get {
            return level_;
        }
        set {
            level_ = value;
            RefreshStatus();

            hp_ = maxHP;

            if( m_HpBar_Panel != null ) {
                m_HpBar_Panel.SetPlayerHP_Bar((int)hp_, maxHP);
            }
        }
    }

    public int maxHP {
        get {
            float hp = maxHP_;

            hp += hp * equipItemHpRatio_;
            hp += equipItemHp_;

            return (int)hp;
        }
    }

    public int attack {
        get {
            float attack = attack_;

            attack += attack * equipItemPhysicalAttackRatio_;
            attack += equipItemPhysicalAttack_;

            float attackValue = attack * GetBuffValue(ESkillType.Buff_AddPhysicalAttack);
            attackValue -= attack * GetDebuffValue(ESkillType.Debuff_MinusPhysicalAttack);

            return (int)Mathf.Max(attack + attackValue, 0);
        }
    }

    public int defense {
        get {
            float defense = defense_;

            defense += defense * equipItemPhysicalDefenseRatio_;
            defense += equipItemPhysicalDefense_;

            float defenseValue = defense * GetBuffValue(ESkillType.Buff_AddPhysicalDefense);
            defenseValue -= defense * GetDebuffValue(ESkillType.Debuff_MinusPhysicalDefense);

            return (int)Mathf.Max(defense + defenseValue, 0);
        }
    }

    public int magicAttack {
        get {
            float attack = magicAttack_;

            attack += attack * equipItemMagicAttackRatio_;
            attack += equipItemMagicAttack_;

            float attackValue = attack * GetBuffValue(ESkillType.Buff_AddMagicAttack);
            attackValue -= attack * GetDebuffValue(ESkillType.Debuff_MinusMagicAttack);

            return (int)Mathf.Max(attack + attackValue, 0);
        }
    }

    public int magicDefense {
        get {
            float defense = magicDefense_;

            defense += defense * equipItemMagicDefenseRatio_;
            defense += equipItemMagicDefense_;

            float defenseValue = defense * GetBuffValue(ESkillType.Buff_AddMagicDefense);
            defenseValue -= defense * GetDebuffValue(ESkillType.Debuff_MinusMagicDefense);

            return (int)Mathf.Max(defense + defenseValue, 0);
        }
    }

    public float critical {
        get {
            float temp = critical_ + equipItemCriticalRatio_;
            if( CharacterManager.Instance.CheckTeamType(teamType) )
                temp += GameManager.Instance.AddCriticalValue;

            float temp2 = temp * GetBuffValue(ESkillType.Buff_AddCritical);

            return Mathf.Max(temp + temp2, 0);
        }
    }

    public float avoidChance {
        get {
            float temp = avoidChance_ + equipItemAvoidChance_;
            float temp2 = avoidChance_ * GetBuffValue(ESkillType.Buff_AddAvoidChance);
            return Mathf.Max(temp + temp2, 0);
        }
    }

    public float attackSpeed {
        get {
            float ms = attackSpeed_ + equipItemAttackSpeed_;
            float ms2 = (ms * buffAttackSpeed_);
            ms2 -= (ms * debuffAttackSpeed_);
            return Mathf.Max(ms + ms2, 0);
        }
    }

    public float moveSpeed {
        get {
            float ms = moveSpeed_ + equipItemMoveSpeedRatio_;
            float ms2 = (ms * buffMoveSpeed_);
            ms2 -= (ms * debuffMoveSpeed_);
            return Mathf.Max(ms + ms2, 0);
        }
    }

    public bool superArmor {
        get {
            bool isSuperArmor = buff_.Exists(delegate(CBuffDebuff a) {
                return a.type.Equals(ESkillType.Buff_SuperArmor);
            });

            return isEquipItemSuperArmor_ ? isEquipItemSuperArmor_ : isSuperArmor;
        }
    }

    public CharacterComboTable comboTable {
        get {
            return combo_;
        }
    }

    #region BasicCallback

    void Awake() {
        animator_ = null;
        attackIndex_ = 0;

        Init();
    }

    void Update() {
        if( WeaponTableManager.Instance == null )
            return;
        if( ItemTableManager.Instance == null )
            return;
        if( CharacterManager.Instance == null )
            return;
        if( ValueTableManager.Instance == null )
            return;

        if( WeaponTableManager.Instance.IsLoadComplate == false )
            return;
        if( ItemTableManager.Instance.IsLoadComplate == false )
            return;
        if( ValueTableManager.Instance.IsLoadComplate == false )
            return;
        if( SkillTableManager.Instance.IsLoadComplate == false )
            return;

        InitCharacterState();

        Process();

        if( m_nDamagePanelCount > 0 ) {
            m_fDamagePanelTime += Time.deltaTime;
            if( m_fDamagePanelTime >= 5.0f ) {
                m_fDamagePanelTime = 0.0f;
                m_nDamagePanelCount = 0;
            }
        }

        if( m_nEffectPanelCount > 0 ) {
            m_fEffectPanelTime += Time.deltaTime;
            if( m_fEffectPanelTime >= 5.0f ) {
                m_fEffectPanelTime = 0.0f;
                m_nEffectPanelCount = 0;
            }
        }
    }

    void LateUpdate() {
        if( shadowTransform_ != null ) {
            Vector3 shadowPos = transform.position;
            shadowPos.y = currectLandingY_ + 0.01f;
            shadowTransform_.transform.position = shadowPos;
        }
    }

    #endregion

    #region Active

    /// <summary>
    /// <para>name : SetActive</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Set character object active by (bool)switch.</para>
    /// </summary>
    public void SetActive(bool active) {
        if( gameObject.activeSelf.Equals(active) == false )
            gameObject.SetActive(active);

        if( m_HpBar_Panel != null ) {
            m_HpBar_Panel.SetActive(active);
        }

        for( int i = 0; i < buff_.Count; i++ ) {
            if( buff_[i].effect == null )
                continue;
            buff_[i].effect.SetActive(active);
        }

        for( int i = 0; i < debuff_.Count; i++ ) {
            if( debuff_[i].effect == null )
                continue;
            debuff_[i].effect.SetActive(active);
        }
    }

    /// <summary>
    /// <para>name : SetPartyActive</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Set character team objects active by (bool)switch.</para>
    /// </summary>
    public void SetPartyActive(bool active) {
        if( CheckCharacterDeath )
            return;

        if( active ) {
            if( CharacterManager.Instance.ActivePlayer != null )
                Warp(CharacterManager.Instance.ActivePlayer.transform.position);

            if( gameObject.activeSelf.Equals(active) == false )
                gameObject.SetActive(active);
        }

        else {
            if( CharacterManager.Instance.respawnEffect_ != null )
                Instantiate(CharacterManager.Instance.respawnEffect_, transform.position, CharacterManager.Instance.respawnEffect_.transform.rotation);

            if( gameObject.activeSelf.Equals(active) == false )
                gameObject.SetActive(active);
        }

        if( m_HpBar_Panel != null )
            m_HpBar_Panel.SetActive(false);

        for( int i = 0; i < buff_.Count; i++ ) {
            if( buff_[i].effect == null )
                continue;
            buff_[i].effect.SetActive(active);
        }

        for( int i = 0; i < debuff_.Count; i++ ) {
            if( debuff_[i].effect == null )
                continue;
            debuff_[i].effect.SetActive(active);
        }
    }

    /// <summary>
    /// <para>name : IsActive</para>
    /// <para>parameter : </para>
    /// <para>return : bool</para>
    /// <para>describe : Check character object's Active State.</para>
    /// </summary>
    public bool IsActive() {
        return gameObject.activeSelf;
    }

    #endregion

    /// <summary>
    /// <para>name : SetMinionRespawn</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Minion Resapwn.</para>
    /// </summary>
    public void SetMinionRespawn() {
        SetActive(true);

        if( characterController.enabled == false )
            characterController.enabled = true;

        SetStateReset();

        if( CharacterManager.Instance.respawnEffect_ != null )
            Instantiate(CharacterManager.Instance.respawnEffect_, transform.position, CharacterManager.Instance.respawnEffect_.transform.rotation);

        if( m_HpBar_Panel != null )
            m_HpBar_Panel.SetActive(true);
        else
            AddPlayerHPBar();

        InitSkill();
    }

    /// <summary>
    /// <para>name : SetEnemyRespawn</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Enemy object respawn.</para>
    /// </summary>
    public void SetEnemyRespawn() {
        gameObject.SetActive(true);

        if( characterController.enabled == false )
            characterController.enabled = true;
        if( m_Elit_Effect != null && m_Elit_Effect.activeSelf == false )
            m_Elit_Effect.SetActive(true);

        enemyPresent_ = CharacterManager.Instance.ActivePlayer;
        transform.LookAt(enemyPresent_.transform.position);

        SetStateReset();

        bool isBoss = false;
        if( CharacterManager.Instance.Boss != null )
            isBoss = CharacterManager.Instance.Boss.Equals(this);

        if( isBoss == false ) {
            if( ClientDataManager.Instance.SelectGameMode.Equals(ClientDataManager.GameMode.BossAttackMode) ) {
                if( isBoss = characterTable_.isBoss ) {
                    CharacterManager.Instance.Boss = this;
                    SetStateRespawn(isBoss);
                }
                else {
                    if( CharacterManager.Instance.respawnEffect_ != null )
                        Instantiate(CharacterManager.Instance.respawnEffect_, transform.position, CharacterManager.Instance.respawnEffect_.transform.rotation);
                }
            }

            else {
                if( CharacterManager.Instance.respawnEffect_ != null )
                    Instantiate(CharacterManager.Instance.respawnEffect_, transform.position, CharacterManager.Instance.respawnEffect_.transform.rotation);
            }
        }
        else {
            SetStateRespawn(isBoss);
        }

        if( m_HpBar_Panel != null )
            m_HpBar_Panel.SetActive(true);
        else
            AddPlayerHPBar();

        InitSkill();
    }

    #region Init

    /// <summary>
    /// <para>name : Init</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Initialize basic character component. </para>
    /// </summary>
    private void Init() {
        if( agent_ == null )
            agent_ = GetComponent<NavMeshAgent>();
        if( characterController_ == null )
            characterController_ = GetComponent<CharacterController>();

        agent_.enabled = false;

        agentRadius_ = agent_.radius;
        radius_ = agentRadius_ * scale_;

        animator_ = GetComponent<Animator>();
        characterJump_ = new CharacterJump(this);

        if( model_ == null ) {
            model_ = GetComponent<Model>();
            Model.delegateSetCurrentMotionCB cb = new Model.delegateSetCurrentMotionCB(SetCurrentMotionCB);
            if( cb != null ) {
                model_.SetCurrentMotionCB(cb);
            }
        }

        movementTarget_ = Vector2.zero;
        movementStart_ = Vector2.zero;

        m_DamageText_Target = gameObject;
        m_HpBar_Target = gameObject;
        m_Info_Target = gameObject;
        m_Effect_Target = gameObject;
    }

    /// <summary>
    /// <para>name : InitCharacterState</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Initialize character state. </para>
    /// </summary>
    public void InitCharacterState() {
        if( isInitCharacter ) {
            if( uid_ == System.Guid.Empty ) {
                uid_ = System.Guid.NewGuid();
            }
            CharacterManager.Instance.Add(uid_, this);

            prefabScale_ = transform.localScale.x;
            if( tableID_ == 0 ) {
                tableID_ = 1;
            }
            if( CharacterTableManager.Instance.FindTable(tableID_, out characterTable_) == false ) {
                MagiDebug.LogError(string.Format("{0}, character table not found.", tableID_));
            }

            SetWeapon();

            shadowTransform_ = transform.FindChild("shadow");

            if( CharacterManager.Instance.mode_ == CharacterManager.EMode.Lobby ) {
                switch( lobbyState ) {
                    case LobbyState.Type_SelectedStay:
                        SetStateSelectStay();
                        break;

                    case LobbyState.Type_Summon:
                        SetStateSummon();
                        break;

                    case LobbyState.Type_Stay:
                        SetStateLobbyStay();
                        break;

                    default:
                        SetStateLobbyStay();
                        break;
                }

                if( characterTable_.partsTable != "" ) {
                    PartsTableManager.Instance.LoadPartsTable(characterTable_.partsTable, new PartsTableManager.LoadComplateCB(CharacterPartsLoadTableComplateCB));
                }

                isInitCharacter = false;

                return;
            }

            if( ComboTableManager.Instance.FindComboTable(characterTable_.comboTableID, out combo_) == true ) {
                SelectCombo();

                foreach( var comboTable in combo_.combotable.Values ) {
                    if( comboTable.attackSpeed > 0.0000001f )
                        continue;
                    float endTime = 0;
                    int comboCount = comboTable.combos.Length;
                    for( int i = 0; i < comboTable.combos.Length; ++i ) {
                        var c = comboTable.combos[i];

                        ModelTemplate.SStateInfo stateInfo;
                        if( model_.modelTemplate_.GetStateInfo(0, c.stateNameHash, out stateInfo) == false ) {
                        }

                        if( i < comboCount - 1 ) {
                            CharacterComboTable.SCombo nc = comboTable.combos[i + 1];

                            List<ModelTemplate.SCondition> listCondition = new List<ModelTemplate.SCondition>();
                            listCondition.Add(new ModelTemplate.SCondition("State", (int)Character.State.Attack));
                            listCondition.AddRange(nc.conditions);

                            MecanimTransition mt = model_.modelTemplate_.FindTransition(0, c.stateNameHash, listCondition.ToArray());
                            if( mt != null ) {
                                endTime += mt.endTime;
                            }
                            else {
                                endTime += stateInfo.endTime;
                            }
                        }

                        else {
                            endTime += stateInfo.endTime;
                        }
                    }
                    comboTable.attackSpeed = (1.0f / endTime);
                }
            }
            else {
                MagiDebug.LogError(string.Format("ComboTable file not found. {0}", characterTable_.comboTableID));
            }

            if( AttackTableManager.Instance.FindAttackTable(characterTable_.attackTableID, out attackTable_) == false ) {
                MagiDebug.LogError(string.Format("AttackTable file not found. {0}", characterTable_.attackTableID));
            }

            if( characterTable_.partsTable != "" ) {
                PartsTableManager.Instance.LoadPartsTable(characterTable_.partsTable, new PartsTableManager.LoadComplateCB(CharacterPartsLoadTableComplateCB));
            }

            if( teamType.Equals(CharacterTeam.Type_Enemy) ) {
                scale_ = Random.Range(characterTable_.enemyScale[0], characterTable_.enemyScale[1]) * prefabScale_;
                model_.scale = scale_;
            }

            else {
                scale_ = characterTable_.scale * prefabScale_;
                model_.scale = scale_;

                agent_.angularSpeed = 5000.0f;
            }

            centerPosition_ = Vector3.Scale(GetComponent<CharacterController>().center, transform.localScale);

            RefreshStatus();

            hp_ = maxHP;

            if( StageManager.Instance.stageTable.category.Equals(StageTableManager.Category.EXPEDITION) ) {
                hp_ *= (teamInfo_.nBattleHP * 0.01f);
            }

            InitShoot();

            if( m_HpBar_Panel == null ) {
                AddPlayerHPBar();
            }

            if( CharacterManager.Instance.mode_ == CharacterManager.EMode.Game ) {
                SkinnedMeshRenderer tempSkinnedMeshRenderer = null;
                SkinnedMeshRenderer[] tempSkinnedMeshRendererArray = GetComponentsInChildren<SkinnedMeshRenderer>();
                for( int i = 0; i < tempSkinnedMeshRendererArray.Length; i++ ) {
                    tempSkinnedMeshRenderer = tempSkinnedMeshRendererArray[i];
                    if( tempSkinnedMeshRenderer != null ) {
                        for( int count_ = 0; count_ < tempSkinnedMeshRenderer.materials.Length; ++count_ ) {
                            if( tempSkinnedMeshRenderer.materials[count_].shader.name.Contains("Cull") )
                                tempSkinnedMeshRenderer.materials[count_].shader = CharacterManager.Instance.basicCullOffOneLight;
                            else if( tempSkinnedMeshRenderer.materials[count_].shader.name.Contains("Glow") )
                                continue;
                            else if( tempSkinnedMeshRenderer.materials[count_].shader.name.Contains("Additive") )
                                continue;
                            else
                                tempSkinnedMeshRenderer.materials[count_].shader = CharacterManager.Instance.basicOneLight;
                        }
                    }
                }
            }

            GetMaterial();

            StageTableManager.Category stageCategory = StageTableManager.Category.NORMAL;
            if( StageManager.Instance != null && StageManager.Instance.stageTable != null )
                stageCategory = StageManager.Instance.stageTable.category;

            switch( stageCategory ) {
                case StageTableManager.Category.TUTORIAL:
                case StageTableManager.Category.NORMAL:
                case StageTableManager.Category.TIME_ATTACK:
                case StageTableManager.Category.INFINITE:
                case StageTableManager.Category.BOSS_ATTACK:
                    if( CharacterManager.Instance.ActivePlayer.Equals(this) == false )
                        SetActive(false);

                    break;
            }

            isInitCharacter = false;
        }
    }

    /// <summary>
    /// <para>name : InitShoot</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Initialize character shoot table. </para>
    /// </summary>
    void InitShoot() {
        if( attackTable_ == null )
            return;

        foreach( var atable in attackTable_.all.Values ) {
            if( atable.methods != null ) {
                for( int i = 0; i < atable.methods.Count; i++ ) {
                    switch( atable.methods[i].GetCategory() ) {
                        case CharacterAttackTable.MethodCategory.Shoot: {
                                ModelTemplate.SStateInfo stateInfo;
                                if( model_.modelTemplate_.GetStateInfo(0, atable.nameHash, out stateInfo) == false ) {
                                    MagiDebug.LogError(string.Format("model_.modelTemplate_.GetStateInfo==false, {0}, {1}", characterTable_.id, atable.name));
                                    break;
                                }
                                float endTime = stateInfo.endTime;
                                if( endTime < 0.02f )
                                    endTime = 0.02f;

                                Model.SUserEventKey userKey = new Model.SUserEventKey();
                                var methodShoot = atable.methods[i] as CharacterAttackTable.SMethodShoot;
                                userKey.functionName = "OnShootEvent";
                                userKey.normalizedTime = methodShoot.frame / (endTime * 30.0f);
                                userKey.nameHash = atable.nameHash;
                                userKey.param = new object[2];
                                userKey.param[0] = methodShoot.frame;
                                userKey.param[1] = methodShoot.shootID;

                                model_.SetUserEventKey(atable.nameHash, userKey);

                                if( ShootManager.Instance != null ) {
                                    ShootManager.Instance.AddPreload(methodShoot.shootID);
                                }
                            }
                            break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// <para>name : InitShoot</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Initialize character skill list. </para>
    /// </summary>
    public void InitSkill() {
        if( isInitSkill )
            return;

        skillTable_ = new List<SkillTableManager.CTable>();
        SkillTableManager.CTable skillTable;
        SkillPowerUpTableManager.STable levelTable;
        CSkillCoolTime skillCool_;

        bool isAddUseCoolTime = true;
        if( StageManager.Instance.stageTable != null )
            isAddUseCoolTime = !StageManager.Instance.stageTable.category.Equals(StageTableManager.Category.PVP_TEST);

        for( int i = 0; i < STable.skillID.Length; ++i ) {
            if( SkillTableManager.Instance != null && SkillTableManager.Instance.FindSkillTable(STable.skillID[i], out skillTable) ) {
                if( SkillTableManager.Instance.CheckSkillExist(STable.grade, i) == false )
                    continue;

                if( teamInfo_.skillLevelGroup != null && teamInfo_.skillLevelGroup.Length > i ) {
                    if( SkillPowerUpTableManager.Instance != null &&
                        SkillPowerUpTableManager.Instance.FindTable(teamInfo_.skillLevelGroup[i], out levelTable) ) {
                        skillTable = new SkillTableManager.CTable(levelTable.nPlusLevel, skillTable);
                    }
                }

                if( teamType.Equals(CharacterTeam.Type_Player) == false ) {
                    if( isAddUseCoolTime ) {
                        if( skillTable.enemyUseCoolTime.Equals(0) == false ) {
                            skillCool_ = new CSkillCoolTime();

                            skillCool_.fullTime = Random.Range(skillTable.useCoolTime, skillTable.enemyUseCoolTime);
                            skillCool_.start = false;

                            skillCoolTime_.Add(skillTable.id, skillCool_);
                        }
                    }
                }

                if( skillTable.skillPropertyList.Exists(delegate(ESkillProperty a) {
                    return a.type.Equals(ESkillType.BloodSucking);
                }) ) {
                    if( ShootManager.Instance != null )
                        ShootManager.Instance.AddPreload(1001);
                }

                skillTable_.Add(skillTable);
            }
        }

        for( int i = 0; i < skillTable_.Count; i++ ) {
            StartSkillCoolTime(skillTable_[i].id);
        }

        isInitSkill = true;
    }

    /// <summary>
    /// <para>name : InitPlayerTeamInfo</para>
    /// <para>parameter : ClientDataManager.Mon_Info</para>
    /// <para>return : void</para>
    /// <para>describe : Initialize character team Info.</para>
    /// </summary>
    public void InitPlayerTeamInfo(ClientDataManager.Mon_Info teamInfo) {
        level_ = teamInfo.nLevel;

        if( isInitCharacter == true ) {
            teamInfo_ = teamInfo;
            return;
        }

        RefreshStatus();

        SetParts();
        SetWeapon();
    }

    public void ResetState() {
        if( state_.Equals(State.Death) )
            return;
        if( model_ == null )
            return;

        model_.ResetNextMotion();

        foreach( CSkillCoolTime coolTime in skillCoolTime_.Values ) {
            coolTime.start = true;
        }

        nextAttackTime_ = Time.time;
        selectedCombo_ = null;
        SelectCombo();

        model_.speed = 1;
    }

    #endregion

    /// <summary>
    /// <para>name : MoveStop</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Character Movement stop.</para>
    /// </summary>
    public void MoveStop() {
        if( state_ == State.Move ) {
            SetStateStay();
        }
    }

    /// <summary>
    /// <para>name : RefreshStatus</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Refresh Character base status.</para>
    /// </summary>
    public void RefreshStatus() {
        if( characterTable_ == null )
            return;

        switch( teamType ) {
            case CharacterTeam.Type_Enemy:
                maxHP_ = (int)(Content.Monster_GetMaxHP(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);
                attack_ = (int)(Content.Monster_GetPhysicalAttack(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);
                defense_ = (int)(Content.Monster_GetPhysicalDefense(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);
                magicAttack_ = (int)(Content.Monster_GetMagicAttack(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);
                magicDefense_ = (int)(Content.Monster_GetMagicDefense(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);

                attackSpeed_ = characterTable_.attackSpeed;
                moveSpeed_ = characterTable_.moveSpeed;
                avoidChance_ = characterTable_.avoid;
                critical_ = characterTable_.critical;
                attributeType = characterTable_.type;

                EquipItemStateInit();

                break;

            default:
                maxHP_ = (int)(Content.GetMaxHP(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);
                attack_ = (int)(Content.GetPhysicalAttack(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);
                defense_ = (int)(Content.GetPhysicalDefense(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);
                magicAttack_ = (int)(Content.GetMagicAttack(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);
                magicDefense_ = (int)(Content.GetMagicDefense(level_ + teamInfo_.nPowerUpAddLevel, characterTable_, 0, 0) ^ ClientDataManager.Instance.Encryption);

                attackSpeed_ = characterTable_.attackSpeed;
                moveSpeed_ = characterTable_.moveSpeed;
                avoidChance_ = characterTable_.avoid;
                critical_ = characterTable_.critical;
                attributeType = characterTable_.type;

                EquipItem_RefreshStatus();
                AddBattleBuff();

                break;
        }
    }

    /// <summary>
    /// <para>name : AddHP</para>
    /// <para>parameter : float, Character, bool</para>
    /// <para>return : int</para>
    /// <para>describe : Add/Minus character hp.</para>
    /// </summary>
    public int AddHP(float hp, Character attacker, bool isDeathCheck = true) {
        if( state == State.Death ) {
            hp_ = 0;
            return 0;
        }

        StageTableManager.Category stageCategory = StageTableManager.Category.NORMAL;
        if( StageManager.Instance.stageTable != null )
            stageCategory = StageManager.Instance.stageTable.category;

        int damage = (int)-hp;
        switch( stageCategory ) {
            case StageTableManager.Category.TUTORIAL:
            case StageTableManager.Category.NORMAL:
            case StageTableManager.Category.EXPEDITION:
            case StageTableManager.Category.INFINITE:
            case StageTableManager.Category.BOSS_ATTACK:
                if( teamType.Equals(CharacterTeam.Type_Player) && 
                    CharacterManager.Instance.ActivePlayer.Equals(this) == false && 
                    teamType.Equals(attacker.teamType) == false )
                    break;

                hp_ = Mathf.Clamp(hp_ + hp, 0, maxHP);

                break;

            case StageTableManager.Category.PVP33:
            case StageTableManager.Category.PVP_TEST:
                hp_ = Mathf.Clamp(hp_ + hp, 0, maxHP);

                break;

            case StageTableManager.Category.TIME_ATTACK:
                if( !teamType.Equals(CharacterTeam.Type_Player) )
                    hp_ = Mathf.Clamp(hp_ + hp, 0, maxHP);

                break;
        }

        if( m_HpBar_Panel == null )
            AddPlayerHPBar();

        if( m_HpBar_Panel != null )
            m_HpBar_Panel.SetPlayerHP_Bar((int)hp_, maxHP);

        if( isDeathCheck && hp_ < 1.0f )
            Death();

        return damage;
    }

    int hashStayStayState = Animator.StringToHash("Base Layer.Select Stay");
    int hashGrowthState = Animator.StringToHash("Base Layer.Growth");
    int hashSummonState = Animator.StringToHash("Base Layer.Respawn");
    int hashRespawnState = Animator.StringToHash("Base Layer.Appear");
    int hashStayState = Animator.StringToHash("Base Layer.Stay");
    int hashRunState = Animator.StringToHash("Base Layer.Run");
    int hashChainBreakState = Animator.StringToHash("Base Layer.Chainbreak");
    int hashDamageState = Animator.StringToHash("Damage Layer.Damage");
    int hashBigDamageState = Animator.StringToHash("Damage Layer.BigDamage");
    int hashDeathBeginState = Animator.StringToHash("Base Layer.DeathBegin");
    int hashDeathState = Animator.StringToHash("Base Layer.Death");
    int hashEvasionState = Animator.StringToHash("Base Layer.Evasion");
    int hashStuneState = Animator.StringToHash("Masmerized Layer.Stun");
    int hashSleepState = Animator.StringToHash("Masmerized Layer.Sleep");

    /// <summary>
    /// <para>name : GetStateMotionHash</para>
    /// <para>parameter : State, int</para>
    /// <para>return : int</para>
    /// <para>describe : Get State motion hash code.</para>
    /// </summary>
    int GetStateMotionHash(State state, int inState) {
        switch( state ) {
            case State.None:
                return 0;
            case State.Respawn:
                return hashRespawnState;
            case State.Stay:
                return hashStayState;
            case State.Move:
                return hashRunState;
            case State.Damage:
                return hashDamageState;
            case State.Death:
                return hashDeathState;
        }
        return 0;
    }

    /// <summary>
    /// <para>name : InstantlyMove</para>
    /// <para>parameter : Vector3</para>
    /// <para>return : void</para>
    /// <para>describe : Move character object instantly.</para>
    /// </summary>
    public void InstantlyMove(Vector3 target) {
        switch( state_ ) {
            case State.Death:
            case State.Damage:
            case State.MidairDamage:
            case State.DownDamage:
            case State.BuffDebuff_Fear:
            case State.BuffDebuff_Freeze:
            case State.BuffDebuff_Stun:
            case State.BuffDebuff_Sleep:
            case State.BuffDebuff_Stone:
                return;
        }

        characterTarget_ = null;
        enemyPresent_ = null;

        movementTarget_ = target;
        SetMovementTargetEnable(true);

        isMovementWait = true;

        SetStateMove();
    }

    /// <summary>
    /// <para>name : SetMovementTarget</para>
    /// <para>parameter : Vector3, Character, bool, State</para>
    /// <para>return : void</para>
    /// <para>describe : Move to target by target (Vector3)position.</para>
    /// </summary>
    public void SetMovementTarget(Vector3 target, Character characterTarget, bool findEnemy, State nextState) {
        characterTarget_ = characterTarget;
        movementTarget_ = target;
        SetMovementTargetEnable(true);

        if( nextState != State.None ) {
            SetNextState(currentBaseState_.nameHash, nextState, 0, "");
        }
    }

    /// <summary>
    /// <para>name : ResetMovementTarget</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Reset move target to Vector3.zero(0,0,0).</para>
    /// </summary>
    public void ResetMovementTarget() {
        nextState_ = State.None;
        if( movementTargetEnable_ == true ) {
            SetNextState(currentBaseState_.nameHash, State.Move, 0, "");
        }
    }

    public Vector3 MovementTarget {
        get {
            return movementTarget_;
        }
        set {
            movementTarget_ = value;
        }
    }

    public bool MovementTargetEnable {
        get {
            return movementTargetEnable_;
        }
    }

    /// <summary>
    /// <para>name : SetMovementTargetEnable</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Set character move if (bool)switch is true.</para>
    /// </summary>
    public void SetMovementTargetEnable(bool isSwitch) {
        if( gameObject.activeSelf == false )
            return;

        if( isMovementWait )
            StartCoroutine("MovementWaitProcess", isSwitch);
        else
            movementTargetEnable_ = isSwitch;
    }

    /// <summary>
    /// <para>name : MovementWaitProcess</para>
    /// <para>parameter : bool</para>
    /// <para>return : yield</para>
    /// <para>describe : Set character move, wait for (float)movementWaitTime.</para>
    /// </summary>
    IEnumerator MovementWaitProcess(bool value) {
        if( value == false ) {
            float timer = movementWaitTime + Time.realtimeSinceStartup;
            while( timer > Time.realtimeSinceStartup ) {
                yield return null;
            }
        }

        isMovementWait = false;
        movementTargetEnable_ = value;
    }

    public bool CheckEndMoveTarget {
        get {
            return movementTargetEnable_ == false && characterTarget_ == null;
        }
    }

    public float AttackRange {
        get {
            if( selectedCombo_ == null )
                return GetAttackRange(1.0f);
            else
                return GetAttackRange(selectedCombo_.attackRange);
        }
    }

    /// <summary>
    /// <para>name : GetAttackRange</para>
    /// <para>parameter : float</para>
    /// <para>return : float</para>
    /// <para>describe : Get Attack range by (float)range, add character (float)scale.  </para>
    /// </summary>
    public float GetAttackRange(float range) {
        return (range + characterController.radius * 0.5f) * scale_;
    }

    /// <summary>
    /// <para>name : AgentStop</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Stop character NavMeshAgent.</para>
    /// </summary>
    void AgentStop(bool stopUpdates = false) {
        if( gameObject.activeSelf == false )
            return;

        if( agent_ != null ) {
            if( agent_.enabled == false )
                return;

            if( stopUpdates == false ) {
                agent_.Resume();
            }
            agent_.Stop(stopUpdates);
        }
    }

    /// <summary>
    /// <para>name : AgentStart</para>
    /// <para>parameter : Vector3</para>
    /// <para>return : void</para>
    /// <para>describe : Start character NavMeshAgent to (Vector3)destination.</para>
    /// </summary>
    void AgentStart(Vector3 destination) {
        if( agent_ != null ) {
            agent_.Resume();
            agent_.SetDestination(destination);
        }
    }

    public float keepDistance_ = 2.0f;
    public float keepDistance {
        get {
            return keepDistance_ * scale_;
        }
    }

    /// <summary>
    /// <para>name : StartMoveTarget</para>
    /// <para>parameter : </para>
    /// <para>return : bool</para>
    /// <para>describe : Character move start to target(enemy/target). </para>
    /// </summary>
    bool StartMoveTarget() {
        bool result = false;

        if( movementTargetEnable_ == true ) {
            if( characterTarget_ != null && characterTarget_.CheckCharacterAlive ) {
                if( (characterTarget_.transform.position - transform.position).magnitude > keepDistance ) {
                    AgentStart(characterTarget_.transform.position);

                    movementStart_ = transform.position;

                    result = true;
                }
            }
            else {
                if( (movementTarget_ - transform.position).magnitude >= 0.05f ) {
                    AgentStart(movementTarget_);

                    movementStart_ = transform.position;

                    result = true;
                }
            }
            return result;
        }

        if( StageManager.Instance != null && StageManager.Instance.stageTable != null ) {
            switch( StageManager.Instance.stageTable.category ) {
                case StageTableManager.Category.TUTORIAL:
                case StageTableManager.Category.NORMAL:
                case StageTableManager.Category.INFINITE:
                    if( ClientDataManager.Instance.Auto_Play == false && CharacterManager.Instance.ActivePlayer != null ) {
                        if( CharacterManager.Instance.ActivePlayer.Equals(this) )
                            return false;
                    }

                    break;
            }
        }

        if( enemyPresent_ != null ) {
            if( enemyPresent_.CheckCharacterAlive == false )
                return false;

            if( NextAttackOn() ) {
                if( (enemyPresent_.transform.position - transform.position).magnitude < keepDistance ) {
                    result = false;
                }

                else {
                    AgentStart(enemyPresent_.transform.position);
                    movementStart_ = transform.position;

                    result = true;
                }
            }

            else {
                result = false;
            }
        }

        return result;
    }

    /// <summary>
    /// <para>name : EndMoveTarget</para>
    /// <para>parameter : </para>
    /// <para>return : bool</para>
    /// <para>describe : Stop move target.</para>
    /// </summary>
    bool EndMoveTarget() {
        bool result = false;

        if( movementTargetEnable_ ) {
            if( characterTarget_ != null ) {
                switch( characterTarget_.state ) {
                    case Character.State.Respawn:
                    case Character.State.Death: {
                            AgentStop();
                            characterTarget_ = null;
                            SetMovementTargetEnable(false);
                            result = true;
                        }
                        break;
                    default: {
                            if( characterTarget_.IsActive() == false ) {
                                if( CharacterManager.Instance.CheckTeamType(CharacterTeam.Type_Enemy) == false && 
                                    CharacterManager.Instance.ActivePlayer != null )
                                    characterTarget_ = CharacterManager.Instance.ActivePlayer;
                            }
                        }
                        break;
                }
            }
            else {
                Vector3 dist = MovementTarget - transform.position;
                dist.y = 0;

                if( dist.magnitude < 0.05f && agent_.remainingDistance < 0.05f ) {
                    AgentStop();
                    movementTarget_ = transform.position;

                    SetMovementTargetEnable(false);
                    characterTarget_ = null;
                    result = true;
                }
            }
            return result;
        }

        if( enemyPresent_ != null ) {
            switch( enemyPresent_.state ) {
                case Character.State.Respawn:
                case Character.State.Death: {
                        AgentStop();
                        enemyPresent_ = null;
                        result = true;
                    }
                    break;
                default: {
                        if( enemyPresent_.IsActive() == false ) {
                            if( CharacterManager.Instance.CheckTeamType(CharacterTeam.Type_Enemy) == false && 
                                CharacterManager.Instance.ActivePlayer != null ) {
                                enemyPresent_ = CharacterManager.Instance.ActivePlayer;
                            }
                        }
                    }
                    break;
            }
        }

        if( movementTargetEnable_ == false && characterTarget_ == null && enemyPresent_ == null ) {
            result = true;
        }

        return result;
    }

    /// <summary>
    /// <para>name : NextAttack</para>
    /// <para>parameter : </para>
    /// <para>return : int</para>
    /// <para>describe : Return next combo index.</para>
    /// </summary>
    public int NextAttack() {
        if( selectedCombo_ == null )
            return 0;
        if( attackIndex_ + 1 < selectedCombo_.combos.Length ) {
            return selectedCombo_.combos[attackIndex_ + 1].stateNameHash;
        }
        return 0;
    }

    int nextAttackIndex_ = 0;

    /// <summary>
    /// <para>name : Attack</para>
    /// <para>parameter : </para>
    /// <para>return : bool</para>
    /// <para>describe : Select character attack state.</para>
    /// </summary>
    public bool Attack() {
        AgentStop();

        switch( state_ ) {
            case State.Attack: {
                    if( teamType.Equals(CharacterTeam.Type_Player) )
                        if( nextState_ == State.Attack )
                            return false;

                    if( attackIndex_ < selectedCombo_.combos.Length && selectedCombo_.combos[attackIndex_].stateNameHash != currentBaseState_.nameHash )
                        return false;

                    if( attackIndex_ >= selectedCombo_.combos.Length )
                        break;

                    CharacterComboTable.SCombo oldCombo = selectedCombo_.combos[attackIndex_];
                    bool currentSkillCombo = selectedCombo_.skillID != 0;
                    int index = 0;
                    if( currentSkillCombo == false && useSkillCombo_.Count != 0 ) {
                        SelectCombo();
                        break;
                    }
                    else {
                        index = attackIndex_ + 1;
                        if( index >= selectedCombo_.combos.Length ) {
                            index = 0;
                            SelectCombo();
                        }
                    }

                    var combo = selectedCombo_.combos[index];

                    if( SetNextState(oldCombo.stateNameHash, State.Attack, combo.stateNameHash, combo.stateName, combo.conditions) == true ) {
                        nextAttackIndex_ = index;
                    }
                }
                return true;
            case State.MidairDamage:
                return false;
            default: {
                }
                break;
        };

        return SetState(State.Attack, 0);
    }

    /// <summary>
    /// <para>name : AttackOnDifferentState</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Select character attack state on different state.</para>
    /// </summary>
    void AttackOnDifferentState() {
        if( BuffDebuffState_.Equals(State.None) == false ) {
            state_ = BuffDebuffState_;
            SetState(state_);

            return;
        }

        const float dot = 0.3f;
        Character enemy = null;

        nextAttackTime_ = Time.time;

        if( CharacterManager.Instance.CheckTeamType(teamType) ) {
            if( NextAttackOn() && FindRangeAndAngle(out enemy, AttackRange, dot) ) {
                EnemyPresent = enemy;

                if( ClientDataManager.Instance.Auto_Play )
                    Attack();
                else
                    SetStateStay();
            }

            else {
                if( StartMoveTarget() )
                    SetState(State.Move);
                else
                    SetStateStay();
            }
        }

        else {
            if( NextAttackOn() && enemyPresent_ != null )
                Attack();
            else {
                if( StartMoveTarget() )
                    SetState(State.Move);
                else
                    SetStateStay();
            }
        }
    }

    /// <summary>
    /// <para>name : AttackTarget</para>
    /// <para>parameter : Character</para>
    /// <para>return : void</para>
    /// <para>describe : Select enemy target.</para>
    /// </summary>
    public void AttackTarget(Character target) {
        if( target == null )
            return;
        if( target.teamType.Equals(teamType) )
            return;

        characterTarget_ = target;
    }

    /// <summary>
    /// <para>name : AttackNotNextCombo</para>
    /// <para>parameter : </para>
    /// <para>return : bool</para>
    /// <para>describe : Select attcak state if combo index is out of range.</para>
    /// </summary>
    public bool AttackNotNextCombo() {
        AgentStop();
        switch( state_ ) {
            case State.Attack: {
                    if( nextState_ == State.Attack )
                        return false;

                    if( attackIndex_ < selectedCombo_.combos.Length && selectedCombo_.combos[attackIndex_].stateNameHash != currentBaseState_.nameHash ) {
                        return false;
                    }

                    int index = attackIndex_ + 1;
                    if( index >= selectedCombo_.combos.Length ) {
                        index = 0;
                        return false;
                    }

                    var combo = selectedCombo_.combos[index];

                    if( SetNextState(selectedCombo_.combos[attackIndex_].stateNameHash, State.Attack, combo.stateNameHash, combo.stateName, combo.conditions) == true ) {
                        nextAttackIndex_ = index;
                    }
                    return true;
                }
            case State.MidairDamage: {
                    return false;
                }
        };
        return SetState(State.Attack, 0);
    }

    /// <summary>
    /// <para>name : IsBackGround</para>
    /// <para>parameter : Vector3</para>
    /// <para>return : bool</para>
    /// <para>describe : Check if character object on ground.</para>
    /// </summary>
    bool IsBackGround(Vector3 pos) {
        NavMeshHit nmHit;
        return Agent.Raycast(pos, out nmHit) == false;
    }

    public struct SMecanimState {
        public SMecanimState(string name, int value) {
            this.name = name;
            this.value = value;
        }
        public string name;
        public int value;
    }

    /// <summary>
    /// <para>name : SetStateReset</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Reset character state.</para>
    /// </summary>
    public void SetStateReset() {
        state_ = State.Stay;
    }

    /// <summary>
    /// <para>name : SetStateAddPool</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Set character state on pool.</para>
    /// </summary>
    public void SetStateAddPool() {
        state_ = State.Add_Pool;
    }

    /// <summary>
    /// <para>name : SetStateNone</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Release character state.</para>
    /// </summary>
    public void SetStateNone() {
        state_ = State.None;
    }

    /// <summary>
    /// <para>name : SetStateChainBreak</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Set character state "chainbreak".</para>
    /// </summary>
    public void SetStateChainBreak() {
        if( animator_ == null )
            return;
        if( state_ == State.Death )
            return;

        agent_.enabled = true;
        nextState_ = State.None;

        model_.speed = 1;

        AgentStop();
        EndMoveTarget();

        state_ = State.Stay;
        if( BuffDebuffState_ != State.None ) {
            state_ = BuffDebuffState_;
            SetState(state_);

            return;
        }

        model_.Play(hashChainBreakState, "Base Layer.Chainbreak", new ModelTemplate.SCondition("State", (int)State.Stay));
    }

    /// <summary>
    /// <para>name : SetStateStay</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Set character state "stay".</para>
    /// </summary>
    public void SetStateStay() {
        if( animator_ == null )
            return;
        if( state_ == State.Death )
            return;

        agent_.enabled = true;
        nextState_ = State.None;

        model_.speed = 1;

        AgentStop();
        EndMoveTarget();

        state_ = State.Stay;
        if( BuffDebuffState_ != State.None ) {
            state_ = BuffDebuffState_;
            SetState(state_);

            return;
        }

        if( state_ != (State)animator_.GetInteger("State") ) {
            model_.Play(hashStayState, "Base Layer.Stay", new ModelTemplate.SCondition("State", (int)State.Stay));
        }
    }

    /// <summary>
    /// <para>name : SetStateMove</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Set character state "move".</para>
    /// </summary>
    public void SetStateMove() {
        if( animator_ == null )
            return;
        if( state_ == State.Death )
            return;

        agent_.enabled = true;
        nextState_ = State.None;

        model_.speed = 1;

        AgentStop();
        EndMoveTarget();

        state_ = State.Move;

        MidairEnable(false);
        attackIndex_ = 0;

        if( state_ != (State)animator_.GetInteger("State") )
            model_.SetCurrentMotion(hashRunState, "Base Layer.Run", new ModelTemplate.SCondition("State", (int)state_));

        currentStateNameHash_ = hashRunState;
    }

    /// <summary>
    /// <para>name : SetStateSelectStay</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Set character state "select stay".</para>
    /// </summary>
    public void SetStateSelectStay() {
        if( animator_ == null )
            return;
        if( state_ == State.Death )
            return;

        agent_.enabled = false;
        nextState_ = State.None;

        model_.speed = 1;

        AgentStop();
        EndMoveTarget();

        model_.Play(hashStayStayState, "Base Layer.Select Stay", new ModelTemplate.SCondition("State", (int)State.SelectStay));
    }

    /// <summary>
    /// <para>name : SetStateLobbyStay</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Set character state "lobby stay".</para>
    /// </summary>
    public void SetStateLobbyStay() {
        if( animator_ == null )
            return;
        if( state_ == State.Death )
            return;

        agent_.enabled = false;
        nextState_ = State.None;

        model_.speed = 1;

        AgentStop();
        EndMoveTarget();

        model_.Play(hashStayState, "Base Layer.Stay", new ModelTemplate.SCondition("State", (int)State.Stay));
    }

    /// <summary>
    /// <para>name : SetStateLobbyStay</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Set character state "growth".</para>
    /// </summary>
    public void SetStateGrowth() {
        if( animator_ == null )
            return;
        if( state_ == State.Death )
            return;

        agent_.enabled = false;
        nextState_ = State.None;

        model_.speed = 1;

        AgentStop();
        EndMoveTarget();

        model_.Play(hashGrowthState, "Base Layer.Growth", new ModelTemplate.SCondition("State", (int)State.Growth));
    }

    /// <summary>
    /// <para>name : SetStateSummon</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Set character state "summon".</para>
    /// </summary>
    public void SetStateSummon() {
        if( animator_ == null )
            return;
        if( state_ == State.Death )
            return;

        agent_.enabled = false;
        nextState_ = State.None;

        model_.speed = 1;

        AgentStop();
        EndMoveTarget();

        model_.Play(hashSummonState, "Base Layer.Respawn", new ModelTemplate.SCondition("State", (int)State.Summon));
    }

    /// <summary>
    /// <para>name : SetStateRespawn</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Set character state if it is boss "respawn", or "appear".</para>
    /// </summary>
    public void SetStateRespawn(bool isBoss = false) {
        if( animator_ == null )
            return;
        if( state_ == State.Death )
            return;

        SetActive(true);

        agent_.enabled = false;
        nextState_ = State.None;

        state_ = State.Respawn;

        model_.speed = 1;

        AgentStop();
        EndMoveTarget();

        if( isBoss ) {
            if( ClientDataManager.Instance.SelectGameMode.Equals(ClientDataManager.GameMode.BossAttackMode) )
                GameManager.Instance.SetBossMatch(this);
            else
                GameManager.Instance.SetBossMatch();

            model_.Play(hashSummonState, "Base Layer.Respawn", new ModelTemplate.SCondition("State", (int)State.Summon));
        }

        else {
            model_.Play(hashRespawnState, "Base Layer.Appear", new ModelTemplate.SCondition("State", (int)State.Respawn));
        }
    }

    /// <summary>
    /// <para>name : GetState</para>
    /// <para>parameter : </para>
    /// <para>return : State</para>
    /// <para>describe : Get current state.</para>
    /// </summary>
    public State GetState() {
        return state_;
    }

    /// <summary>
    /// <para>name : GetState</para>
    /// <para>parameter : State</para>
    /// <para>return : bool</para>
    /// <para>describe : Check state if it equals (State)parameter.</para>
    /// </summary>
    public bool GetState(State currentState_) {
        return currentState_.Equals(state_);
    }

    ulong attackID_ = 0;

    /// <summary>
    /// <para>name : SetState</para>
    /// <para>parameter : State, int[]</para>
    /// <para>return : bool</para>
    /// <para>describe : Set character state to (State)parameter.</para>
    /// </summary>
    public bool SetState(State state, params int[] state2) {
        if( state_ == State.Death )
            return false;

        switch( state_ ) {
            case State.MidairDamage: {
                    switch( state ) {
                        case State.Move:
                            return false;
                    }
                }
                break;

            case State.BuffDebuff_Fear:
            case State.BuffDebuff_Freeze:
            case State.BuffDebuff_Stun:
            case State.BuffDebuff_Sleep:
            case State.BuffDebuff_Stone: {
                    switch( state ) {
                        case State.Death:
                        case State.Damage:
                        case State.MidairDamage:
                        case State.DownDamage:
                        case State.BuffDebuff_Fear:
                        case State.BuffDebuff_Freeze:
                        case State.BuffDebuff_Stun:
                        case State.BuffDebuff_Sleep:
                        case State.BuffDebuff_Stone:
                            break;
                        default:
                            return false;
                    }
                }
                break;
        }

        model_.speed = 1;
        agent_.radius = agentRadius_;
        characterController_.radius = agentRadius_;
        nextState_ = State.None;
        stateTime_ = Time.time;

        switch( state ) {
            case State.SelectStay: {
                    state_ = state;
                    MidairEnable(false);
                    attackIndex_ = 0;
                    if( state_ != (State)animator_.GetInteger("State") ) {
                        model_.Play(hashStayState, "Base Layer.Select Stay", new ModelTemplate.SCondition("State", (int)state_));
                    }
                    currentStateNameHash_ = hashStayState;
                }
                break;
            case State.Respawn: {
                    state_ = state;
                    MidairEnable(false);
                    attackIndex_ = 0;
                    animator_.Play(hashRespawnState);
                    currentStateNameHash_ = hashRespawnState;
                }
                break;
            case State.Stay: {
                    state_ = state;
                    MidairEnable(false);
                    attackIndex_ = 0;
                    if( state_ != (State)animator_.GetInteger("State") ) {
                        model_.Play(hashStayState, "Base Layer.Stay", new ModelTemplate.SCondition("State", (int)state_));
                    }
                    currentStateNameHash_ = hashStayState;
                }
                break;
            case State.Move: {
                    state_ = state;
                    MidairEnable(false);
                    attackIndex_ = 0;
                    if( state_ != (State)animator_.GetInteger("State") ) {
                        model_.SetCurrentMotion(hashRunState, "Base Layer.Run", new ModelTemplate.SCondition("State", (int)state_));
                    }
                    currentStateNameHash_ = hashRunState;
                }
                break;
            case State.Attack: {
                    switch( state ) {
                        case State.Respawn:
                        case State.Damage:
                        case State.DownDamage:
                        case State.MidairDamage: {
                                return false;
                            }
                    }

                    if( attackIndex_ == 0 ) {
                        if( selectedCombo_.skillID != 0 ) {
                            CSkillCoolTime skillCoolTime;
                            if( skillCoolTime_.TryGetValue(selectedCombo_.skillID, out skillCoolTime) ) {
                                if( skillCoolTime.start ) {
                                    SelectCombo();
                                    return false;
                                }
                            }

                            int index = skillTable_.FindIndex(delegate(SkillTableManager.CTable a) {
                                return a.id.Equals(selectedCombo_.skillID);
                            });

                            if( index > -1 )
                                UseDirectSkill(skillTable_[index]);

                            StartSkillCoolTime(selectedCombo_.skillID);
                        }

                        UpdateNextAttackTime();
                    }

                    attackID_ = DamageParam.GenerateAttackID();
                    state_ = state;
                    MidairEnable(false);
                    if( state2.Length != 1 ) {
                        MagiDebug.LogError("State.Attack - state2.Length != 1");
                        return false;
                    }
                    attackIndex_ = state2[0];

                    if( attackIndex_ >= selectedCombo_.combos.Length ) {
                        MagiDebug.LogError("attackIndex_ >= selectedCombo_.combos.Length");
                    }

                    selectedComboSkill_ = null;

                    int skillIndex = skillTable_.FindIndex(delegate(SkillTableManager.CTable a) {
                        return a.id.Equals(selectedCombo_.skillID);
                    });

                    if( skillIndex > -1 )
                        selectedComboSkill_ = skillTable_[skillIndex];

                    List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                    conditions.Add(new ModelTemplate.SCondition("State", (int)state_));
                    conditions.AddRange(selectedCombo_.combos[attackIndex_].conditions);

                    model_.Play(selectedCombo_.combos[attackIndex_].stateNameHash, selectedCombo_.combos[attackIndex_].stateName, conditions.ToArray());

                    attackStateNameHash_ = selectedCombo_.combos[attackIndex_].stateNameHash;
                    attackStateName_ = selectedCombo_.combos[attackIndex_].stateName;

                    currentStateNameHash_ = attackStateNameHash_;
                    attackHoming_ = selectedCombo_.homing != 0;

                    agent_.radius = agentRadius_;// *1.1f;
                    characterController_.radius = agentRadius_;// *1.1f;

                    attackCatchUpEnemy_ = false;
                    attackCatchUpMoveSpeed_ = 0;
                    attackPullEnemy_ = false;
                    attackPullEnemyRange_ = 0;
                    attackPullMoveSpeed_ = 0;
                    attackSkillID_ = selectedCombo_.skillID;

                    if( enemyPresent_ != null ) {
                        Vector3 target = enemyPresent_.transform.position;
                        target.y = transform.position.y;
                        transform.LookAt(target);
                    }

                    if( selectedCombo_.skillID == 0 ) {
                        model_.speed = attackSpeed / (selectedCombo_.attackSpeed * selectedCombo_.combos.Length);
                    }
                }
                break;
            case State.Damage: {
                    state_ = state;
                    attackIndex_ = 0;

                    bool isBigDamage = false;
                    if( state2.Length.Equals(0) == false ) {
                        switch( (DamageEffectType)state2[0] ) {
                            case DamageEffectType.None:
                                return false;

                            case DamageEffectType.Critical:
                            case DamageEffectType.BigDamage:
                                isBigDamage = true;
                                break;
                        }
                    }

                    MidairEnable(false);

                    if( state_ != (State)animator_.GetInteger("State") ) {
                        if( isBigDamage ) {
                            model_.Play(hashBigDamageState, "Damage Layer.BigDamage");
                            currentStateNameHash_ = hashBigDamageState;
                        }

                        else {
                            model_.Play(hashDamageState, "Damage Layer.Damage");
                            currentStateNameHash_ = hashDamageState;
                        }
                    }
                }
                break;
            case State.Death: {
                    for( int i = 0; i < buff_.Count; i++ ) {
                        if( buff_[i].effect == null )
                            continue;

                        Destroy(buff_[i].effect);
                    }

                    for( int i = 0; i < debuff_.Count; i++ ) {
                        if( debuff_[i].effect == null )
                            continue;

                        Destroy(debuff_[i].effect);
                    }

                    state_ = state;
                    MidairEnable(false);
                    attackIndex_ = 0;

                    if( state_ != (State)animator_.GetInteger("State") ) {
                        SubState subState = (SubState)state2[0];
                        switch( subState ) {
                            case SubState.Init:
                                model_.Play(hashDeathBeginState, "Base Layer.DeathBegin");
                                currentStateNameHash_ = hashDeathBeginState;
                                subState_ = subState;

                                break;

                            case SubState.Process01:
                                model_.Play(hashDeathState, "Base Layer.Death", new ModelTemplate.SCondition("State", 99));

                                agent_.enabled = false;
                                characterController_.enabled = false;

                                currentStateNameHash_ = hashDeathBeginState;
                                subState_ = SubState.Process02;

                                break;
                        }
                    }
                }
                break;
            case State.MidairDamage: {
                    state_ = state;
                    List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                    conditions.Add(new ModelTemplate.SCondition("State", (int)-99));
                    conditions.Add(new ModelTemplate.SCondition("MidairDamage", (int)eMidairDamageState.Ready));
                    model_.Play(hashMidairDamageReadyState, "Damage Layer.MidairDamageReady", conditions.ToArray());

                    currentStateNameHash_ = hashMidairDamageReadyState;
                    midairDamageState_ = eMidairDamageState.Ready; //점프 준비중.
                    startY_ = transform.position.y - currectLandingY_;
                }
                break;
            case State.DownDamage: {
                    state_ = state;
                }
                break;
            case State.JumpTargetAttack: {
                    state_ = state;
                    MidairEnable(false);
                }
                break;
            case State.DistanceMaintenance: {
                    state_ = state;
                    MidairEnable(false);
                    attackIndex_ = 0;
                    if( state_ != (State)animator_.GetInteger("State") ) {
                        model_.SetCurrentMotion(hashRunState, "Base Layer.Run", new ModelTemplate.SCondition("State", (int)state_));
                    }
                    currentStateNameHash_ = hashRunState;
                }
                break;
            case State.BuffDebuff_Fear:
                break;

            case State.BuffDebuff_Freeze:
            case State.BuffDebuff_Stone: {
                    state_ = state;
                    agent_.walkableMask = (int)AgentState.STOP;

                    MidairEnable(false);
                    SetAnimatorActive(false);
                }

                break;

            case State.BuffDebuff_Stun: {
                    state_ = state;
                    MidairEnable(false);

                    if( State.Stay != (State)animator_.GetInteger("State") ) {
                        List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                        conditions.Add(new ModelTemplate.SCondition("State", (int)-99));
                        model_.Play(hashStuneState, "Masmerized Layer.Stun", conditions.ToArray());
                    }

                    currentStateNameHash_ = hashStuneState;
                }

                break;

            case State.BuffDebuff_Sleep: {
                    state_ = state;
                    MidairEnable(false);
                    if( State.Stay != (State)animator_.GetInteger("State") ) {
                        List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                        conditions.Add(new ModelTemplate.SCondition("State", (int)-99));
                        model_.Play(hashSleepState, "Masmerized Layer.Sleep", conditions.ToArray());
                    }

                    currentStateNameHash_ = hashSleepState;
                }

                break;
        }

        return true;
    }

    State nextState_ = State.None;

    /// <summary>
    /// <para>name : SetNextState</para>
    /// <para>parameter : int, State, int ,string, ModelTemplate.SCondition[]</para>
    /// <para>return : bool</para>
    /// <para>describe : Set next motion to (State)parameter.</para>
    /// </summary>
    public bool SetNextState(int hashCurrentMotionNameHash, State state, int motionNextNameHash, string motionNextName, params ModelTemplate.SCondition[] conditions) {
        switch( state ) {
            case State.None: {
                    nextState_ = state;
                    if( CharacterManager.Instance.ActivePlayer != null && 
                        CharacterManager.Instance.ActivePlayer.Equals(this) )
                        MagiDebug.Log("NonState");
                }
                break;
            case State.Stay: {
                    switch( nextState_ ) {
                        case State.Attack:
                        case State.Respawn:
                        case State.Damage:
                        case State.Death:
                        case State.MidairDamage:
                            return false;
                    }
                    if( model_.SetNextMotion(hashCurrentMotionNameHash, hashStayState, motionNextName, new ModelTemplate.SCondition("State", (int)state)) == false )
                        return false;
                    nextState_ = state;
                }
                break;
            case State.Move: {
                    switch( nextState_ ) {
                        case State.Attack:
                        case State.Respawn:
                        case State.Damage:
                        case State.Death:
                        case State.MidairDamage:
                            return false;
                    }
                    if( model_.SetNextMotion(hashCurrentMotionNameHash, hashRunState, motionNextName, new ModelTemplate.SCondition("State", (int)state)) == false )
                        return false;
                    nextState_ = state;
                }
                break;
            case State.Attack: {
                    switch( nextState_ ) {
                        case State.MidairDamage:
                            return false;
                    }

                    List<ModelTemplate.SCondition> listCondition = new List<ModelTemplate.SCondition>();
                    listCondition.Add(new ModelTemplate.SCondition("State", (int)State.Attack));
                    listCondition.AddRange(conditions);
                    if( model_.SetNextMotion(hashCurrentMotionNameHash, motionNextNameHash, motionNextName, listCondition.ToArray()) == false ) {
                        return false;
                    }
                    nextState_ = state;
                }
                break;
            case State.MidairDamage: {
                }
                break;
        }
        return true;
    }

    /// <summary>
    /// <para>name : SetAnimatorActive</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Set Animator speed.</para>
    /// </summary>
    void SetAnimatorActive(bool switch_) {
        switch( switch_ ) {
            case true:
                animator_.speed = 1;
                break;

            case false:
                animator_.speed = 0;
                break;
        }
    }

    public enum DamageEffectType {
        None,
        Normal,
        Critical,
        BigDamage,
    }

    /// <summary>
    /// <para>name : Heal</para>
    /// <para>parameter : DamageParam</para>
    /// <para>return : bool</para>
    /// <para>describe : Add heal effect on this character.</para>
    /// </summary>
    public bool Heal(DamageParam damageParam) {
        Character ownerCharacter;
        CharacterManager.Instance.FindCharacter(damageParam.ownerID, out ownerCharacter);

        AddDamagePanel(0, (int)damageParam.damage, false, CharacterTypeAdvance.Type_Normal);
        AddHP(-damageParam.damage, ownerCharacter);

        return true;
    }

    /// <summary>
    /// <para>name : SetPause</para>
    /// <para>parameter : float</para>
    /// <para>return : void</para>
    /// <para>describe : Set pause model Animator.</para>
    /// </summary>
    public void SetPause(float duration) {
        model_.SetPause(duration);
    }

    /// <summary>
    /// <para>name : InstantlyDamage</para>
    /// <para>parameter : Character, (ESkillProperty)List</para>
    /// <para>return : void</para>
    /// <para>describe : Add damge value instantly.</para>
    /// </summary>
    public void InstantlyDamage(Character ownerCharacter, List<ESkillProperty> eSkillPropertyList) {
        if( CharacterManager.Instance.CheckTeamType(teamType, ownerCharacter.teamType) )
            return;

        switch( state_ ) {
            case State.Respawn:
            case State.Growth:
            case State.Summon:
            case State.SelectStay:
            case State.Death:
                return;
        }

        Vector3 at = Vector3.zero;
        nextState_ = State.None;

        float damage = 0;
        int damagePanel = 0;

        SetHitColor(true, CharacterManager.Instance.hitColor, 0.15f);

        DamageEffectType result = DamageEffectType.BigDamage;
        bool isCritical = ownerCharacter.critical > Random.Range(0, 1.0f);
        if( isCritical )
            result = DamageEffectType.Critical;

        CharacterTypeAdvance typeAdvance = CharacterTypeAdvance.Type_Normal;

        float ownerPhysicalDamage = ownerCharacter.attack * (GameManager.Instance != null ? GameManager.Instance.GetDamageFactor : 1.0f);
        float ownerMagicDamage = ownerCharacter.magicAttack * (GameManager.Instance != null ? GameManager.Instance.GetDamageFactor : 1.0f);

        if( hp_ != -99 ) {
            for( int i = 0; i < eSkillPropertyList.Count; i++ ) {
                switch( eSkillPropertyList[i].type ) {
                    case ESkillType.Attack:
                        damage = SetDamage(eSkillPropertyList[i].baseStatus.Equals(ESkillBaseStatus.Magic) ? ownerMagicDamage : ownerPhysicalDamage, isCritical,
                            eSkillPropertyList[i], ownerCharacter, ownerCharacter.equipItemAttakEffectRatio_, out typeAdvance, false);

                        if( eSkillPropertyList[i].GetDuration(ownerCharacter).Equals(0) ) {
                            if( eSkillPropertyList[i].effectPath != "" ) {
                                LoadAssetbundle.LoadPrefabComplateCB loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadDamageEffectCompleteCB);
                                PrefabManager.Instance.LoadPrefab("", eSkillPropertyList[i].effectPath.ToLower(), System.Guid.Empty, loadComplateCB);
                            }

                            damagePanel = AddHP((int)-damage, ownerCharacter, false);
                            AddDamagePanel(0, damagePanel, isCritical, typeAdvance);
                        }

                        break;

                    case ESkillType.BloodSucking:
                        damage = SetDamage(eSkillPropertyList[i].baseStatus.Equals(ESkillBaseStatus.Magic) ? ownerMagicDamage : ownerPhysicalDamage, isCritical,
                           eSkillPropertyList[i], ownerCharacter, ownerCharacter.equipItemBloodSuckingRatio_, out typeAdvance, false);

                        damagePanel = AddHP((int)-damage, ownerCharacter, false);
                        AddDamagePanel(0, damagePanel, isCritical, typeAdvance);

                        DamageParam bloodSuckingDamageParam = new DamageParam();
                        bloodSuckingDamageParam.teamType = teamType;
                        bloodSuckingDamageParam.damage = damage * 0.2f;
                        bloodSuckingDamageParam.bloodSucking = true;

                        ShootManager.Instance.AddShoot(1001, this.gameObject, ownerCharacter.gameObject, bloodSuckingDamageParam);

                        break;
                }
            }

            if( ownerCharacter != null )
                AddDebuff(eSkillPropertyList, ownerCharacter);
        }

        switch( StageManager.Instance.stageTable.category ) {
            case StageTableManager.Category.TUTORIAL:
            case StageTableManager.Category.NORMAL:
            case StageTableManager.Category.INFINITE:
            case StageTableManager.Category.TIME_ATTACK:
                if( CharacterManager.Instance.ActivePlayer != null && CharacterManager.Instance.ActivePlayer.Equals(ownerCharacter) )
                    DungeonSceneScriptManager.Instance.OnCombo();
                if( characterTable_.isBoss )
                    DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.BossGaugeTween();

                break;

            case StageTableManager.Category.BOSS_ATTACK:
                if( CharacterManager.Instance.ActivePlayer != null && CharacterManager.Instance.ActivePlayer.Equals(ownerCharacter) )
                    DungeonSceneScriptManager.Instance.OnCombo();
                if( characterTable_.isBoss )
                    DungeonSceneScriptManager.Instance.m_BossAttackEvent.TweenPlay(this);

                break;
        }

        if( superArmor ) {
            if( hp_ < 1.0f )
                Death();
        }

        if( hp_ < 1.0f )
            Death();
        else
            SetState(Character.State.Damage, (int)result);
    }

    /// <summary>
    /// <para>name : Damage</para>
    /// <para>parameter : DamageParam</para>
    /// <para>return : DamageEffectType</para>
    /// <para>describe : Add damge value on this character.</para>
    /// </summary>
    public DamageEffectType Damage(DamageParam damageParam) {
        switch( state_ ) {
            case State.Respawn:
            case State.Growth:
            case State.Summon:
            case State.SelectStay:
            case State.Death:
                return DamageEffectType.None;
        }

        Vector3 at = Vector3.zero;
        nextState_ = State.None;

        float avoidChanceFactor = Random.Range(0.0f, 1.0f);
        if( avoidChanceFactor <= avoidChance ) {
            SetHitColor(false, Color.black, 0);
            AddTextPanel(MagiStringUtil.GetString(44));

            return DamageEffectType.None;
        }

        Character ownerCharacter;
        if( CharacterManager.Instance.FindCharacter(damageParam.ownerID, out ownerCharacter) == false )
            return DamageEffectType.None;

        if( EnemyPresent == null || EnemyPresent.CheckCharacterAlive == false )
            EnemyPresent = ownerCharacter;

        SetHitColor(true, CharacterManager.Instance.hitColor, 0.15f);

        bool firstHit = ExistDamageAttackID(damageParam.attackID) == false;

        DamageEffectType result = DamageEffectType.Normal;
        if( damageParam.critical == true )
            result = DamageEffectType.Critical;

        CharacterTypeAdvance typeAdvance = CharacterTypeAdvance.Type_Normal;

        if( hp_ != -99 ) {
            float damage = 0;
            int damagePanel = 0;

            if( damageParam.skillTable != null ) {
                for( int i = 0; i < damageParam.skillTable.skillPropertyList.Count; i++ ) {
                    switch( damageParam.skillTable.skillPropertyList[i].type ) {
                        case ESkillType.Attack:
                            damage = SetDamage(damageParam, damageParam.skillTable.skillPropertyList[i], ownerCharacter, ownerCharacter.equipItemAttakEffectRatio_, out typeAdvance, false);

                            if( damageParam.skillTable.skillPropertyList[i].GetDuration(ownerCharacter).Equals(0) ) {
                                if( damageParam.skillTable.skillPropertyList[i].effectPath != "" ) {
                                    LoadAssetbundle.LoadPrefabComplateCB loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadDamageEffectCompleteCB);
                                    PrefabManager.Instance.LoadPrefab("", damageParam.skillTable.skillPropertyList[i].effectPath.ToLower(), System.Guid.Empty, loadComplateCB);
                                }

                                damagePanel = AddHP((int)-damage, ownerCharacter, false);
                                AddDamagePanel(0, damagePanel, damageParam.critical, typeAdvance);
                            }

                            break;

                        case ESkillType.BloodSucking:
                            damage = SetDamage(damageParam, damageParam.skillTable.skillPropertyList[i], ownerCharacter, ownerCharacter.equipItemBloodSuckingRatio_, out typeAdvance, false);

                            damagePanel = AddHP((int)-damage, ownerCharacter, false);
                            AddDamagePanel(0, damagePanel, damageParam.critical, typeAdvance);

                            DamageParam bloodSuckingDamageParam = new DamageParam();
                            bloodSuckingDamageParam.teamType = teamType;
                            bloodSuckingDamageParam.damage = damage * 0.2f;
                            bloodSuckingDamageParam.bloodSucking = true;

                            ShootManager.Instance.AddShoot(1001, this.gameObject, ownerCharacter.gameObject, bloodSuckingDamageParam);

                            break;
                    }
                }

                if( firstHit ) {
                    if( ownerCharacter != null )
                        AddDebuff(damageParam.skillTable.skillPropertyList, ownerCharacter);
                }
            }

            else {
                damage = SetDamage(damageParam, ownerCharacter, out typeAdvance);

                damagePanel = AddHP((int)-damage, ownerCharacter, false);
                AddDamagePanel(0, damagePanel, damageParam.critical, typeAdvance);
            }
        }

        bool isBattle = false;
        switch( StageManager.Instance.stageTable.category ) {
            case StageTableManager.Category.TUTORIAL:
            case StageTableManager.Category.NORMAL:
            case StageTableManager.Category.INFINITE:
            case StageTableManager.Category.TIME_ATTACK:
                if( CharacterManager.Instance.ActivePlayer != null && CharacterManager.Instance.ActivePlayer.Equals(ownerCharacter) )
                    DungeonSceneScriptManager.Instance.OnCombo();
                if( characterTable_.isBoss )
                    DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.BossGaugeTween();

                break;

            case StageTableManager.Category.BOSS_ATTACK:
                if( CharacterManager.Instance.ActivePlayer != null && CharacterManager.Instance.ActivePlayer.Equals(ownerCharacter) )
                    DungeonSceneScriptManager.Instance.OnCombo();
                if( characterTable_.isBoss )
                    DungeonSceneScriptManager.Instance.m_BossAttackEvent.TweenPlay(this);

                break;

            case StageTableManager.Category.EXPEDITION:
            case StageTableManager.Category.PVP33:
                isBattle = true;
                break;
        }

        if( superArmor ) {
            if( hp_ < 1.0f )
                Death();

            return result;
        }

        int attackerWeight = ownerCharacter.STable.attackWeight + damageParam.addWeight + (damageParam.critical ? 2 : 0);
        if( STable.weight <= attackerWeight ) {
            MidairEnable(false);

            at = damageParam.attackerPosition - transform.position;
            at.y = 0;
            at.Normalize();
            transform.forward = at;

            if( state_ == State.MidairDamage && midairDamageState_ < eMidairDamageState.Landing ) {
                jumpingDist_ = damageParam.midairDist * 100.0f / weight_;
                jumpingAngle_ = damageParam.midairAngle;
                SetState(State.MidairDamage);
            }

            else {
                if( damageParam.rising && STable.weight < attackerWeight ) {
                    jumpingDist_ = damageParam.risingDist * 100.0f / weight_;
                    jumpingAngle_ = damageParam.risingAngle;
                    SetState(State.MidairDamage);
                }

                else {
                    if( hp_ < 1.0f ) {
                        if( damageParam.push && STable.weight < attackerWeight )
                            DamagePush(damageParam);

                        Death();
                    }

                    else {
                        if( STable.weight < attackerWeight ) {
                            if( damageParam.push )
                                DamagePush(damageParam);

                            if( damageParam.bigDamage )
                                result = DamageEffectType.BigDamage;
                        }

                        if( result.Equals(DamageEffectType.Normal) && isBattle )
                            return result;

                        SetState(Character.State.Damage, (int)result);
                    }
                }
            }
        }

        else {
            if( hp_ < 1.0f ) {
                Death();
            }
        }

        return result;
    }

    /// <summary>
    /// <para>name : SetDamage</para>
    /// <para>parameter : float, bool, ESkillProperty, Character, float, out CharacterTypeAdvance, bool</para>
    /// <para>return : float</para>
    /// <para>describe : Set damage value, and return final damage value, out advance type.</para>
    /// </summary>
    float SetDamage(float baseDamage, bool isCritical, ESkillProperty skillProperty, Character ownerCharacter, float addValue, out CharacterTypeAdvance typeAdvance, bool isAddFactor = true) {
        float damage = isAddFactor ? baseDamage + baseDamage * (skillProperty.GetValue + addValue) : baseDamage * (skillProperty.GetValue + addValue);
        float defenseValue = skillProperty.baseStatus.Equals(ESkillBaseStatus.Magic) ? (float)magicDefense : (float)defense;
        bool isAdvantageTypeDamage = false;

        damage = Content.GetDamage(damage, defenseValue);

        if( ownerCharacter != null ) {
            if( CharacterManager.Instance.GetAdvantageCharacterType(ownerCharacter.attributeType).Equals(attributeType) ) {
                damage *= (GameManager.Instance.attributeAddDamageRatio_ + ownerCharacter.equipItemAttributeDamageRatio_);
                isAdvantageTypeDamage = true;
            }
        }

        if( isCritical )
            damage *= 2;

        damage = Mathf.Max(damage, 1.0f);
        typeAdvance = isAdvantageTypeDamage ? CharacterTypeAdvance.Type_Upper :
            ownerCharacter.attributeType.Equals(attributeType) ? CharacterTypeAdvance.Type_Normal : CharacterTypeAdvance.Type_Under;

        SetCumulativeDamage(skillProperty.baseStatus, (int)damage);

        return damage;
    }

    /// <summary>
    /// <para>name : SetDamage</para>
    /// <para>parameter : DamageParam, ESkillProperty, Character, float, out CharacterTypeAdvance, bool</para>
    /// <para>return : float</para>
    /// <para>describe : Set damage value, and return final damage value, out advance type.</para>
    /// </summary>
    float SetDamage(DamageParam damageParam, ESkillProperty skillProperty, Character ownerCharacter, float addValue, out CharacterTypeAdvance typeAdvance, bool isAddFactor = true) {
        float damage = 0;
        int defenseValue = 0;
        bool isAdvantageTypeDamage = false;

        switch( skillProperty.baseStatus ) {
            case ESkillBaseStatus.Physical:
            case ESkillBaseStatus.None:
                if( isAddFactor )
                    damage = damageParam.damage + damageParam.damage * (skillProperty.GetValue + addValue);
                else
                    damage = damageParam.damage * (skillProperty.GetValue + addValue);

                defenseValue = defense;
                break;

            case ESkillBaseStatus.Magic:
                if( isAddFactor )
                    damage = damageParam.magicDamage + damageParam.magicDamage * (skillProperty.GetValue + addValue);
                else
                    damage = damageParam.magicDamage * (skillProperty.GetValue + addValue);

                defenseValue = magicDefense;
                break;
        }

        damage = Content.GetDamage(damage, defenseValue);

        if( ownerCharacter != null ) {
            if( CharacterManager.Instance.GetAdvantageCharacterType(ownerCharacter.attributeType).Equals(attributeType) ) {
                damage *= (GameManager.Instance.attributeAddDamageRatio_ + ownerCharacter.equipItemAttributeDamageRatio_);
                isAdvantageTypeDamage = true;
            }
        }

        if( damageParam.critical )
            damage *= 2;

        damage = Mathf.Max(damage, 1.0f);
        typeAdvance = isAdvantageTypeDamage ? CharacterTypeAdvance.Type_Upper : 
            ownerCharacter.attributeType.Equals(attributeType) ? CharacterTypeAdvance.Type_Normal : CharacterTypeAdvance.Type_Under;

        SetCumulativeDamage(skillProperty.baseStatus, (int)damage);

        return damage;
    }

    /// <summary>
    /// <para>name : SetDamage</para>
    /// <para>parameter : DamageParam, Character, out CharacterTypeAdvance</para>
    /// <para>return : float</para>
    /// <para>describe : Set damage value, and return final damage value, out advance type.</para>
    /// </summary>
    float SetDamage(DamageParam damageParam, Character ownerCharacter, out CharacterTypeAdvance typeAdvance) {
        float damage = 0;
        bool isAdvantageTypeDamage = false;

        damage = damageParam.damage;
        damage = Content.GetDamage(damage, (float)defense);

        if( ownerCharacter != null ) {
            if( CharacterManager.Instance.GetAdvantageCharacterType(ownerCharacter.attributeType).Equals(attributeType) ) {
                damage *= (GameManager.Instance.attributeAddDamageRatio_ + ownerCharacter.equipItemAttributeDamageRatio_);
                isAdvantageTypeDamage = true;
            }
        }

        if( damageParam.critical == true )
            damage *= 2;

        damage = Mathf.Max(damage, 1.0f);
        typeAdvance = isAdvantageTypeDamage ? CharacterTypeAdvance.Type_Upper :
            ownerCharacter.attributeType.Equals(attributeType) ? CharacterTypeAdvance.Type_Normal : CharacterTypeAdvance.Type_Under;

        SetCumulativeDamage(ESkillBaseStatus.None, (int)damage);

        return damage;
    }

    /// <summary>
    /// <para>name : SetCumulativeDamage</para>
    /// <para>parameter : ESkillBaseStatus, int</para>
    /// <para>return : void</para>
    /// <para>describe : Set cumulative damage value on DungeonTestManager.</para>
    /// </summary>
    private void SetCumulativeDamage(ESkillBaseStatus type, int damage) {
        if( DungeonTestManager.Instance == null )
            return;
        if( CharacterManager.Instance.Battle_Player == null )
            return;

        bool isMyTeam = false;
        for( int i = 0; i < CharacterManager.Instance.Battle_Player.Count; i++ ) {
            if( CharacterManager.Instance.Battle_Player[i].Equals(this) ) {
                isMyTeam = true;
                break;
            }
        }

        switch( type ) {
            case ESkillBaseStatus.None:
            case ESkillBaseStatus.Physical:
                if( isMyTeam )
                    DungeonTestManager.Instance.PhysicalDamagePoint += (int)damage;
                else
                    DungeonTestManager.Instance.PhysicalAttackPoint += (int)damage;
                break;

            case ESkillBaseStatus.Magic:
                if( isMyTeam )
                    DungeonTestManager.Instance.MagicDamagePoint += (int)damage;
                else
                    DungeonTestManager.Instance.MagicAttackPoint += (int)damage;
                break;
        }

        if( isMyTeam )
            DungeonTestManager.Instance.DamagePoint += (int)damage;
        else
            DungeonTestManager.Instance.AttackPoint += (int)damage;
    }

    bool hitColorEnable_ = false;
    float hitColorEndTime_ = 0;

    /// <summary>
    /// <para>name : SetHitColor</para>
    /// <para>parameter : bool, Color, float</para>
    /// <para>return : void</para>
    /// <para>describe : Change character object color by (Color)hitColor, during (float)duration time.</para>
    /// </summary>
    void SetHitColor(bool enable, Color hitColor, float duration) {
        hitColorEnable_ = enable;
        if( enable == true ) {
            hitColorEndTime_ = Time.time + duration;
            for( int i = 0; i < materials_.Count; i++ ) {
                materials_[i].SetColor("_HitColor", hitColor);
            }
        }
        else {
            for( int i = 0; i < materials_.Count; i++ ) {
                materials_[i].SetColor("_HitColor", Color.black);
            }
        }
    }

    /// <summary>
    /// <para>name : SetCharacterPointFactor</para>
    /// <para>parameter : float</para>
    /// <para>return : void</para>
    /// <para>describe : Set material point factor to (float)parameter.</para>
    /// </summary>
    public void SetCharacterPointFactor(float factor) {
        for( int i = 0; i < materials_.Count; i++ ) {
            materials_[i].SetFloat("_CharacterPointFactor", factor);
        }
    }

    public float CurrectLandingY {
        get {
            return currectLandingY_;
        }
    }

    float currectLandingY_ = 0;

    /// <summary>
    /// <para>name : Process</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing state.</para>
    /// </summary>
    void Process() {
        if( characterTarget_ != null ) {
            movementTarget_ = characterTarget_.transform.position;
        }

        currentBaseState_ = animator_.GetCurrentAnimatorStateInfo(0);
        model_.Process();

        switch( state_ ) {
            case State.None:
                break;

            case State.SelectStay:
                if( GameManager.Instance != null && GameManager.Instance.GamePause )
                    return;

                SelectStayProcess();
                break;
            case State.Respawn:
                RespawnProcess();
                break;
            case State.Stay:
                if( GameManager.Instance != null && GameManager.Instance.GamePause )
                    return;

                StayProcess();
                break;
            case State.Move:
                if( GameManager.Instance != null && GameManager.Instance.GamePause ) {
                    SetStateStay();
                    return;
                }

                MoveProcess();
                break;
            case State.Attack:
                if( GameManager.Instance != null && GameManager.Instance.GamePause ) {
                    SetStateStay();
                    return;
                }

                AttackProcess();
                break;
            case State.Damage:
                if( GameManager.Instance != null && GameManager.Instance.GamePause )
                    return;

                DamageProcess();
                break;
            case State.Death:
                DeathProcess();
                break;
            case State.MidairDamage:
                if( GameManager.Instance != null && GameManager.Instance.GamePause )
                    return;

                MidairDamageProcess();
                break;
            case State.JumpTargetAttack:
                JumpTargetAttackProcess();
                break;
        }

        DamagePushProcess();
        BuffProcess();
        DebuffProcess();

        if( hitColorEnable_ == true && Time.time > hitColorEndTime_ ) {
            SetHitColor(false, Color.black, 0);
        }

        foreach( var coolTime in skillCoolTime_ ) {
            if( coolTime.Value.start == false )
                continue;
            if( Time.time - coolTime.Value.startTime > coolTime.Value.fullTime ) {
                skillCoolTime_.Remove(coolTime.Key);
                break;
            }
        }

        switch( state_ ) {
            case State.MidairDamage: {
                    NavMeshHit nmHit;
                    if( Agent.Raycast(transform.position, out nmHit) == false ) {
                        currectLandingY_ = nmHit.position.y;
                    }
                }
                break;
            default: {
                    currectLandingY_ = transform.position.y;
                }
                break;
        }

        for( int i = 0; i < damageParam_.Count; ++i ) {
            var damageParam = damageParam_[i];

            if( damageParam.damage < 0 ) {
                Heal(damageParam);
            }
            else {
                switch( Damage(damageParam) ) {
                    case DamageEffectType.Normal:
                            DamageEffect(damageParam, false);
                        break;

                    case DamageEffectType.BigDamage:
                    case DamageEffectType.Critical:
                            DamageEffect(damageParam, true);
                        break;
                }
            }
        }
        damageParam_.Clear();

        if( movementTargetEnable_ == true && characterTarget_ != null ) {
            if( characterTarget_.state == State.Death ) {
                SetMovementTargetEnable(false);
            }
        }
        
        if( enemyPresent_ != null ) {
            switch( enemyPresent_.state ) {
                case State.Respawn:
                case State.Death: {
                        enemyPresent_ = null;
                    }
                    break;
                default: {
                        if( enemyPresent_.IsActive() == false ) {
                            if( CharacterManager.Instance.CheckTeamType(CharacterTeam.Type_Enemy) == false && 
                                CharacterManager.Instance.ActivePlayer != null ) {
                                enemyPresent_ = CharacterManager.Instance.ActivePlayer;
                            }
                        }
                    }
                    break;
            }
        }

        oldBaseState_ = currentBaseState_;
    }

    [SerializeField]
    private float damageBloodAmount_ = 3; //amount of blood when taking damage (relative to damage taken (relative to HP remaining))
    [SerializeField]
    private float maxBloodIndication_ = 0.5f; //max amount of blood when not taking damage (relative to HP lost)

    /// <summary>
    /// <para>name : AngularProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing Transform angular.</para>
    /// </summary>
    void AngularProcess() {
        if( enemyPresent_ != null && movementTargetEnable_ == false )
            return;

        Vector3 destV = Vector3.zero;
        destV.x = movementTarget_.x - transform.position.x;
        destV.z = movementTarget_.z - transform.position.z;
        if( destV.sqrMagnitude > 0.1f ) {
            destV.Normalize();
            transform.forward = Vector3.Slerp(transform.forward, destV, Time.deltaTime * agent_.angularSpeed / 50.0f);
        }
    }

    /// <summary>
    /// <para>name : SelectStayProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing select stay.</para>
    /// </summary>
    void SelectStayProcess() {
    }

    /// <summary>
    /// <para>name : RespawnProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing respawn state.</para>
    /// </summary>
    void RespawnProcess() {
        if( hashRespawnState == currentBaseState_.nameHash && animator_.IsInTransition(0) ) {
            SetStateStay();
        }

        else if( hashSummonState == currentBaseState_.nameHash && animator_.IsInTransition(0) ) {
            SetStateStay();
        }
    }

    /// <summary>
    /// <para>name : StayProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing stay state.</para>
    /// </summary>
    void StayProcess() {
        if( StartMoveTarget() == true ) {
            SetState(State.Move);
        }
    }

    /// <summary>
    /// <para>name : MoveProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing move state.</para>
    /// </summary>
    void MoveProcess() {
        if( Time.time - stateTime_ > actionInterval && hashRunState != currentBaseState_.nameHash ) {
            SetStateStay();
            return;
        }

        StartMoveTarget();
        if( EndMoveTarget() == true ) {
            SetStateStay();
        }

        if( agent_ != null ) {
            model_.speed = moveSpeed;
            agent_.speed = model_.speed * 10.0f;
        }
    }

    public bool CheckAttackMotionComplate { // 공격 모션 대기 시간이 종료되면
        get {
            if( attackStateNameHash_.Equals(currentBaseState_.nameHash) )
                return animator_.IsInTransition(0);

            return true;
        }
    }

    /// <summary>
    /// <para>name : AttackStop</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Stop attack state.</para>
    /// </summary>
    public void AttackStop() {
        nextState_ = State.None;
        attackIndex_ = 0;

        model_.ResetNextMotion();
    }

    /// <summary>
    /// <para>name : AttackProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing attack state.</para>
    /// </summary>
    void AttackProcess() {
        if( attackStateNameHash_ != currentBaseState_.nameHash ) {
            if( nextState_.Equals(State.Attack) ) {
                if( currentBaseState_.nameHash.Equals(hashStayState) ) {
                    model_.ResetNextMotion();

                    int comboIndex = attackIndex_ + 1;

                    if( selectedCombo_.combos.Length <= comboIndex )
                        comboIndex = 0;

                    SetState(State.Attack, comboIndex);
                }
            }

            else {
                if( Time.time - stateTime_ >= actionInterval ) {
                    nextAttackTime_ = Time.time;

                    if( StartMoveTarget() == true ) {
                        SetState(State.Move);
                    }
                    else {
                        SetStateStay();
                    }
                }
            }
        }

        else {
            if( animator_.IsInTransition(0) == true ) {
                int index = attackIndex_ + 1;
                if( index >= selectedCombo_.combos.Length ) {
                    SelectCombo();
                }
            }
        }

        if( movementTargetEnable_ == true && characterTarget_ == null ) {
            SetNextState(currentBaseState_.nameHash, State.Move, 0, "");
            if( (transform.position - movementTarget_).magnitude < 1.0f ) {
                SetMovementTargetEnable(false);
            }
        }

        if( attackCatchUpEnemy_ ) {
            if( enemyPresent_ != null ) {
                Vector3 target = enemyPresent_.transform.position;
                if( (transform.position - target).magnitude > scale_ ) {
                    Vector3 targetV = (target - transform.position).normalized * attackCatchUpMoveSpeed_ * Time.deltaTime;
                    transform.position += targetV;
                }
            }
        }

        if( attackPullEnemy_ ) {
            List<Character> enemyList;
            if( FindEnemy(out enemyList, attackPullEnemyRange_) == true ) {
                for( int i = 0; i < enemyList.Count; i++ ) {
                    if( (transform.position - enemyList[i].transform.position).magnitude > scale_ ) {
                        Vector3 v = (transform.position - enemyList[i].transform.position).normalized * attackPullMoveSpeed_ * Time.deltaTime;
                        enemyList[i].transform.position += v;
                    }
                }
            }
        }
    }

    /// <summary>
    /// <para>name : DamageProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing damage state.</para>
    /// </summary>
    void DamageProcess() {
        if( (hashDamageState.Equals(currentBaseState_.nameHash) || hashBigDamageState.Equals(currentBaseState_.nameHash)) 
            && animator_.IsInTransition(0) ) {
            AttackOnDifferentState();
        }

        else {
            if( Time.time - stateTime_ > actionInterval ) {
                AttackOnDifferentState();
            }
        }
    }

    float deathEndTime_ = 0.5f;

    /// <summary>
    /// <para>name : DeathProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing death state.</para>
    /// </summary>
    void DeathProcess() {
        switch( subState_ ) {
            case SubState.Init:
                ModelTemplate.SStateInfo stateInfo;
                if( model_.modelTemplate_.GetStateInfo(0, hashDeathBeginState, out stateInfo) )
                    deathEndTime_ = stateInfo.endTime;

                subState_ = SubState.Process01;

                break;

            case SubState.Process01:
                if( Time.time - stateTime_ > deathEndTime_ ) {
                    model_.Play(hashDeathState, "Base Layer.Death", new ModelTemplate.SCondition("State", 99));

                    agent_.enabled = false;
                    characterController_.enabled = false;

                    subState_ = SubState.Process02;
                }

                break;

            case SubState.Process02:
                if( Time.time - stateTime_ > deathEndTime_ ) {
                    CharacterPoolManager.Instance.AddCharacterPool(this);
                    
                    subState_ = SubState.End;
                }

                break;

            case SubState.End:
                break;
        }
    }

    float startY_;
    float jumpingDist_ = 0;
    float jumpingAngle_ = 0;

    public bool AttackMidairDamageTest() {
        switch( state_ ) {
            case State.Attack: {
                    jumpingDist_ = 5.0f;
                    jumpingAngle_ = 70.0f;
                    return SetState(State.MidairDamage);
                }
            case State.MidairDamage: {
                    jumpingDist_ = 1.0f;
                    jumpingAngle_ = 70.0f;
                    return SetState(State.MidairDamage);
                }
            default: {
                }
                break;
        };
        Attack();
        return true;
    }

    public bool MidAirCheck {
        get {
            if( state_.Equals(State.MidairDamage) )
                return true;
            else
                return false;
        }
    }

    /// <summary>
    /// <para>name : MidairEnable</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Set mid air state (bool)parameter.</para>
    /// </summary>
    public void MidairEnable(bool enable) {
        midair_ = enable;
        AgentStop(enable);
    }

    enum eMidairDamageState {
        Ready = 0,
        Up = 1,
        Down = 2,
        Landing = 3,
        End = 4,
    };

    float midairDamageStartTime_ = 0;
    eMidairDamageState midairDamageState_ = eMidairDamageState.Ready;
    int hashMidairDamageReadyState = Animator.StringToHash("Damage Layer.MidairDamageReady");
    int hashMidairDamageState = Animator.StringToHash("Damage Layer.MidairDamage");
    int hashMidairDamageLanding = Animator.StringToHash("Damage Layer.MidairDamageLanding");

    float startLandingTime_ = 0;

    /// <summary>
    /// <para>name : MidairDamageProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing damage on air state.</para>
    /// </summary>
    void MidairDamageProcess() {
        switch( midairDamageState_ ) {
            case eMidairDamageState.Ready: {
                    if( hashMidairDamageReadyState == currentBaseState_.nameHash ) {
                        List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                        conditions.Add(new ModelTemplate.SCondition("State", (int)state_));
                        conditions.Add(new ModelTemplate.SCondition("MidairDamage", (int)eMidairDamageState.Up));
                        model_.Play(hashMidairDamageState, "Damage Layer.MidairDamage", conditions.ToArray());
                    }

                    midairDamageStartTime_ = Time.time;
                    characterJump_.JumpInit(jumpingDist_, jumpingAngle_, transform.forward, startY_, currectLandingY_);
                    midairDamageState_ = eMidairDamageState.Up;

                    characterJump_.JumpUpMove(false, false);
                    currentY_ = characterJump_.currentY_;
                    MidairEnable(true);
                }
                break;
            case eMidairDamageState.Up: {
                    if( hashMidairDamageReadyState == currentBaseState_.nameHash ) {
                        List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                        conditions.Add(new ModelTemplate.SCondition("State", (int)state_));
                        conditions.Add(new ModelTemplate.SCondition("MidairDamage", (int)eMidairDamageState.Up));
                        model_.Play(hashMidairDamageState, "Damage Layer.MidairDamage", conditions.ToArray());
                    }

                    if( characterJump_.JumpUpMove(false, false) == true ) {
                        midairDamageState_ = eMidairDamageState.Down;
                    }
                    currentY_ = characterJump_.currentY_;

                    if( hashMidairDamageState != currentBaseState_.nameHash )
                        break;

                    if( characterController_.isGrounded ) {
                        MidairEnable(false);
                        midairDamageState_ = eMidairDamageState.Landing;

                        List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                        conditions.Add(new ModelTemplate.SCondition("State", (int)state_));
                        conditions.Add(new ModelTemplate.SCondition("MidairDamage", (int)eMidairDamageState.Landing));
                        model_.Play(hashMidairDamageLanding, "Damage Layer.MidairDamageLanding", conditions.ToArray());
                    }
                }
                break;
            case eMidairDamageState.Down: {
                    if( hashMidairDamageState != currentBaseState_.nameHash ) {
                        List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                        conditions.Add(new ModelTemplate.SCondition("State", (int)state_));
                        conditions.Add(new ModelTemplate.SCondition("MidairDamage", (int)eMidairDamageState.Up));
                        model_.Play(hashMidairDamageState, "Damage Layer.MidairDamage", conditions.ToArray());
                        break;
                    }

                    if( characterJump_.JumpDownMove(false, false) == true ) {
                        MidairEnable(false);
                        midairDamageState_ = eMidairDamageState.Landing;
                        List<ModelTemplate.SCondition> conditions = new List<ModelTemplate.SCondition>();
                        conditions.Add(new ModelTemplate.SCondition("State", (int)state_));
                        conditions.Add(new ModelTemplate.SCondition("MidairDamage", (int)eMidairDamageState.Landing));
                        model_.Play(hashMidairDamageLanding, "Damage Layer.MidairDamageLanding", conditions.ToArray());
                        startLandingTime_ = Time.time;
                    }
                    currentY_ = characterJump_.currentY_;
                }
                break;
            case eMidairDamageState.Landing: {
                    if( Time.time - startLandingTime_ > 1.0f || hashMidairDamageLanding == currentBaseState_.nameHash && animator_.IsInTransition(0) ) {
                        midairDamageState_ = eMidairDamageState.End;
                    }
                }
                break;
            case eMidairDamageState.End: {
                    if( hp_ < 1.0f )
                        Death(SubState.Process01);
                    else
                        SetStateStay();
                }
                break;
        };
    }

    /// <summary>
    /// <para>name : JumpTargetAttackProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing jump on target state.</para>
    /// </summary>
    void JumpTargetAttackProcess() {
    }

    /// <summary>
    /// <para>name : GetCountAttackComboID</para>
    /// <para>parameter : </para>
    /// <para>return : int</para>
    /// <para>describe : Return attack combo id.</para>
    /// </summary>
    public int GetCountAttackComboID() {
        return countAttackComboID_;
    }

    /// <summary>
    /// <para>name : MoveEnemyPresent</para>
    /// <para>parameter : State</para>
    /// <para>return : void</para>
    /// <para>describe : Move to enemy target position.</para>
    /// </summary>
    public void MoveEnemyPresent(State nextState) {
        if( enemyPresent_ == null )
            return;

        SetMovementTarget(enemyPresent_.transform.position, null, false, nextState);
    }

    public bool AttackIncapacity {
        get {
            return attackIncapacity_;
        }
        set {
            attackIncapacity_ = value;
        }
    }

    /// <summary>
    /// <para>name : GetNextAttackTime</para>
    /// <para>parameter : </para>
    /// <para>return : float</para>
    /// <para>describe : Return release next attack (float)time.</para>
    /// </summary>
    public float GetNextAttackTime() {
        return nextAttackTime_;
    }

    /// <summary>
    /// <para>name : UpdateNextAttackTime</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Update next attack (float)time by combo table value.</para>
    /// </summary>
    public void UpdateNextAttackTime() {
        nextAttackTime_ = Time.time + (selectedCombo_.attackSpeed * selectedCombo_.combos.Length * GameManager.Instance.GetGameSpeed);
    }

    public bool CheckComboAttack(Character enemyPresent) {
        return true;
    }

    /// <summary>
    /// <para>name : NextAttackOn</para>
    /// <para>parameter : </para>
    /// <para>return : bool</para>
    /// <para>describe : Check next attack time if this value is over than current time.</para>
    /// </summary>
    public bool NextAttackOn() {
        return Time.time >= nextAttackTime_;
    }

    // Select Weapon
    List<GameObject> weaponList_ = new List<GameObject>();

    /// <summary>
    /// <para>name : WeaponLoadPrefabComplateCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load prefab and equip model object.</para>
    /// </summary>
    void WeaponLoadPrefabComplateCB(string tableName, GameObject prefab, System.Guid uid, string name, params object[] param) {
        if( param.Length < 1 )
            return;
        string dummyName = param[0] as string;
        if( dummyName == null )
            return;
        model_.EquipItem(dummyName, prefab);
    }

    Dictionary<int, List<GameObject>> damageEffect_ = new Dictionary<int, List<GameObject>>();

    /// <summary>
    /// <para>name : DamageEffectLoadPrefabComplateCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Add damage effect prefab on object.</para>
    /// </summary>
    void DamageEffectLoadPrefabComplateCB(string tableName, GameObject prefab, System.Guid uid, string name, params object[] param) {
        if( prefab == null )
            return;

        int weaponIndex = (int)param[0];

        if( damageEffect_.ContainsKey(weaponIndex) == false ) {
            List<GameObject> weaponGO = new List<GameObject>();

            weaponGO.Add(prefab);
            damageEffect_.Add(weaponIndex, weaponGO);
        }
        else {
            damageEffect_[weaponIndex].Add(prefab);
        }
    }

    /// <summary>
    /// <para>name : SetParts</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Get parts table value and load prefabs.</para>
    /// </summary>
    void SetParts() {
        List<string> partPrefab = new List<string>();
        CharacterCustomization customization = gameObject.GetComponent<CharacterCustomization>();
        if( customization != null ) {
            if( characterTable_.partsID == null ) {
                MagiDebug.LogError(string.Format("characterTable_ partsID({0}) not found.", characterTable_.id));
                return;
            }

            CharacterPartsTable.STable spartstable;
            for( int i = 0; i < characterTable_.partsID.Length; ++i ) {
                if( partsTable_.TryGetValue(characterTable_.partsID[i], out spartstable) == false )
                    continue;
                partPrefab.Add(spartstable.prefab);
            }

            customization.SetParts(partPrefab.ToArray());
        }

        model_.scale = prefabScale_;
    }

    /// <summary>
    /// <para>name : CharacterPartsLoadTableComplateCB</para>
    /// <para>parameter : CharacterPartsTable</para>
    /// <para>return : void</para>
    /// <para>describe : Load character parts table.</para>
    /// </summary>
    void CharacterPartsLoadTableComplateCB(CharacterPartsTable parts) {
        partsTable_ = parts;
        SetParts();
    }

    /// <summary>
    /// <para>name : SetWeapon</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Add weapon on model object.</para>
    /// </summary>
    void SetWeapon() {
        if( characterTable_.weaponItemID != null ) {
            EquipItem(characterTable_.weaponItemID);
        }
    }

    /// <summary>
    /// <para>name : SelectCombo</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Select combo table.</para>
    /// </summary>
    void SelectCombo() {
        if( selectedCombo_ != null && selectedCombo_.skillID != 0 ) {
            CSkillCoolTime sct;
            if( skillCoolTime_.TryGetValue(selectedCombo_.skillID, out sct) == true ) {
                if( sct.start == false ) {
                    //                    MagiDebug.LogError("SelectCombo error.");
                    return;
                }
            }
        }

        attackIndex_ = 0;

        if( testComboID_ != 0 ) {
            combo_.GetCombo(testComboID_, out selectedCombo_);
            return;
        }

        if( useSkillCombo_.Count != 0 ) {
            SetCombo(useSkillCombo_[0]);
            useSkillCombo_.Remove(useSkillCombo_[0]);
        }
        else {
            if( combo_ == null )
                return;

            combo_.SelectCombo(out selectedCombo_);
        }
    }

    /// <summary>
    /// <para>name : SetCombo</para>
    /// <para>parameter : CharacterComboTable.CTable</para>
    /// <para>return : void</para>
    /// <para>describe : Set select combo table by skill id.</para>
    /// </summary>
    void SetCombo(CharacterComboTable.CTable ctable) {
        if( selectedCombo_ != null && selectedCombo_.skillID != 0 ) {
            CSkillCoolTime sct;
            if( skillCoolTime_.TryGetValue(selectedCombo_.skillID, out sct) == true ) {
                if( sct.start == false ) {
                    return;
                }
            }
        }
        selectedCombo_ = ctable;
    }

    /// <summary>
    /// <para>name : EquipItem</para>
    /// <para>parameter : int[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load prefab for equip item.</para>
    /// </summary>
    public void EquipItem(int[] itemUID) {
        LoadAssetbundle.LoadPrefabComplateCB loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(WeaponLoadPrefabComplateCB);

        for( int i = 0; i < itemUID.Length; i++ ) {
            WeaponTableManager.Instance.AddFromTable(itemUID[i], i, loadComplateCB);
        }
    }

    /// <summary>
    /// <para>name : HitDataLoadPrefabComplateCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load hit object from model data.</para>
    /// </summary>
    void HitDataLoadPrefabComplateCB(string tableName, GameObject prefab, System.Guid uid, string name, params object[] param) {
        GameObject igo = Instantiate(prefab) as GameObject;
        var hitData = igo.GetComponent<HitData>() as HitData;
        if( hitData == null )
            return;

        if( param.Length != 2 )
            return;

        MagiMecanimEvent mEvent = param[0] as MagiMecanimEvent;
        DamageParam damageParam = param[1] as DamageParam;
        hitData.SetParam(transform, mEvent.HitData, damageParam);
    }

    /// <summary>
    /// <para>name : MakeDamageParam</para>
    /// <para>parameter : out DamageParam, SkillTableManager.CTable</para>
    /// <para>return : void</para>
    /// <para>describe : Make DamageParam class object from skill table values.</para>
    /// </summary>
    public void MakeDamageParam(out DamageParam damageParam, SkillTableManager.CTable skillTable) {
        damageParam = new DamageParam();
        damageParam.teamType = teamType;
        damageParam.hitID = DamageParam.GenerateHitID();
        damageParam.ownerID = uid_;
        damageParam.damage = attack * (GameManager.Instance != null ? GameManager.Instance.GetDamageFactor : 1.0f);
        damageParam.magicDamage = magicAttack * (GameManager.Instance != null ? GameManager.Instance.GetDamageFactor : 1.0f);
        damageParam.attackerDirection = this.transform.forward;
        damageParam.attackerPosition = this.transform.position;
        damageParam.effectY = this.agent_.height / 2.0f + this.transform.position.y;
        damageParam.damageDist = 0;
        damageParam.damageWeight = 1;
        damageParam.push = false;
        damageParam.rising = false;
        damageParam.bigDamage = false;
        damageParam.midairDist = 3;
        damageParam.midairAngle = 80;
        damageParam.skillTable = skillTable;
        damageParam.attackID = attackID_;
        damageParam.critical = critical > Random.Range(0, 1.0f);
    }

    /// <summary>
    /// <para>name : MakeDamageParam</para>
    /// <para>parameter : out DamageParam, int</para>
    /// <para>return : void</para>
    /// <para>describe : Make DamageParam class object from default value.</para>
    /// </summary>
    void MakeDamageParam(out DamageParam damageParam, int stateHash) {
        damageParam = new DamageParam();
        damageParam.teamType = teamType;
        damageParam.hitID = 0;
        damageParam.ownerID = uid_;
        damageParam.damage = attack * (GameManager.Instance != null ? GameManager.Instance.GetDamageFactor : 1.0f);
        damageParam.magicDamage = magicAttack * (GameManager.Instance != null ? GameManager.Instance.GetDamageFactor : 1.0f);
        damageParam.attackerDirection = this.transform.forward;
        damageParam.attackerPosition = this.transform.position;
        damageParam.effectY = this.agent_.height / 2.0f + this.transform.position.y;
        damageParam.damageDist = 0;
        damageParam.damageWeight = 1;
        damageParam.push = false;
        damageParam.rising = false;
        damageParam.bigDamage = false;
        damageParam.midairDist = 3;
        damageParam.midairAngle = 80;
        damageParam.skillTable = selectedComboSkill_;
        damageParam.attackID = attackID_;
        if( attackTable_ != null ) {
            CharacterAttackTable.STable atable;
            if( attackTable_.FindTable(stateHash, out atable) ) {
                damageParam.addWeight = atable.addWeight;

                if( atable.methods != null ) {
                    for( int i = 0; i < atable.methods.Count; i++ ) {
                        switch( atable.methods[i].GetCategory() ) {
                            case CharacterAttackTable.MethodCategory.EnemyPush: {
                                    damageParam.push = true;
                                    var methodEnemyPush = atable.methods[i] as CharacterAttackTable.SMethodEnemyPush;
                                    damageParam.damageDist = methodEnemyPush.damageDist;
                                }
                                break;
                            case CharacterAttackTable.MethodCategory.EnemyRising: {
                                    damageParam.rising = true;
                                    var methodEnemyRising = atable.methods[i] as CharacterAttackTable.SMethodEnemyRising;
                                    damageParam.risingDist = methodEnemyRising.risingDist;
                                    damageParam.risingAngle = methodEnemyRising.risingAngle;
                                }
                                break;
                            case CharacterAttackTable.MethodCategory.HitPause: {
                                    var methodHitPause = atable.methods[i] as CharacterAttackTable.SHitPause;
                                    damageParam.pause = true;
                                    damageParam.pauseDuration = methodHitPause.duration;
                                }
                                break;
                            case CharacterAttackTable.MethodCategory.BigDamage: {
                                    damageParam.bigDamage = true;
                                }
                                break;
                        }
                    }
                }
            }
        }

        damageParam.critical = critical > Random.Range(0, 1.0f);
    }

    /// <summary>
    /// <para>name : OnMecanimEventHitData</para>
    /// <para>parameter : MagiMecanimEvent</para>
    /// <para>return : void</para>
    /// <para>describe : Make hit object data.</para>
    /// </summary>
    void OnMecanimEventHitData(MagiMecanimEvent mEvent) {
        DamageParam damageParam;
        MakeDamageParam(out damageParam, mEvent.stateHash_);

        object[] param = new object[] { mEvent, damageParam };
        HitDataManager.Instance.ActiveHitData(mEvent.parent_, mEvent.HitData.paramType, param);
    }

    /// <summary>
    /// <para>name : OnShootEvent</para>
    /// <para>parameter : Model.SUserEventKey</para>
    /// <para>return : void</para>
    /// <para>describe : Make shoot event if model event key is "shoot".</para>
    /// </summary>
    void OnShootEvent(Model.SUserEventKey userEventKey) {
        DamageParam damageParam;
        MakeDamageParam(out damageParam, userEventKey.nameHash);

        int shootTableID = (int)userEventKey.param[1];

        GameObject enemyGameObject = null;
        if( enemyPresent_ != null ) {
            enemyGameObject = enemyPresent_.gameObject;
        }
        else {
            List<Character> enemyList;
            if( FindEnemy(out enemyList, AttackRange) == true ) {
                enemyPresent_ = enemyList[0];
                enemyGameObject = enemyPresent_.gameObject;
            }
        }
        ShootManager.Instance.AddShoot(shootTableID, this.gameObject, enemyGameObject, damageParam);
    }

    bool damagePush_ = false;
    float damagePushMoveFactor_ = 0;
    float damagePushDist_ = 0;
    float damagePushCurrentDist_ = 0;
    Vector3 damagePushAt_;

    /// <summary>
    /// <para>name : DamagePush</para>
    /// <para>parameter : DamageParam</para>
    /// <para>return : void</para>
    /// <para>describe : Pushing enemy.</para>
    /// </summary>
    void DamagePush(DamageParam damageParam) {
        damagePush_ = true;

        damagePushMoveFactor_ = damageParam.damageDist;
        damagePushDist_ = damageParam.damageDist * 100.0f / weight_;
        damagePushAt_ = damageParam.attackerDirection;

        damagePushCurrentDist_ = 0;
    }

    /// <summary>
    /// <para>name : DamagePushProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing enemy push state.</para>
    /// </summary>
    void DamagePushProcess() {
        if( damagePush_ == false )
            return;

        Vector3 dm = damagePushAt_ * damagePushMoveFactor_ * 5.0f * Time.deltaTime;
        float moveDist = dm.magnitude;

        damagePushCurrentDist_ += moveDist;
        if( damagePushCurrentDist_ >= damagePushDist_ ) {
            damagePush_ = false;
        }

        AgentStop();

        if( agent_.hasPath )
            agent_.Move(dm);

        MidairEnable(false);
    }

    HashSet<ulong> damagedHitID_ = new HashSet<ulong>();

    /// <summary>
    /// <para>name : CheckHitID</para>
    /// <para>parameter : ulong</para>
    /// <para>return : bool</para>
    /// <para>describe : Check hit id if this value contains hash list.</para>
    /// </summary>
    bool CheckHitID(ulong hitID) {
        if( damagedHitID_.Contains(hitID) == true ) {
            return false;
        }
        damagedHitID_.Add(hitID);
        return true;
    }

    List<ulong> damageAttackID_ = new List<ulong>();

    /// <summary>
    /// <para>name : ExistDamageAttackID</para>
    /// <para>parameter : ulong</para>
    /// <para>return : bool</para>
    /// <para>describe : Check attack id if this value contains hash list.</para>
    /// </summary>
    bool ExistDamageAttackID(ulong attackID) {
        if( damageAttackID_.Contains(attackID) == true ) {
            return true;
        }

        damageAttackID_.Add(attackID);
        return false;
    }

    List<DamageParam> damageParam_ = new List<DamageParam>();

    /// <summary>
    /// <para>name : OnDamage</para>
    /// <para>parameter : DamageParam</para>
    /// <para>return : void</para>
    /// <para>describe : Add on damage.</para>
    /// </summary>
    public void OnDamage(DamageParam damageParam) {
        if( damageParam == null )
            return;
        if( CharacterManager.Instance.CheckTeamType(teamType, damageParam.teamType) )
            return;

        if( damageParam.bloodSucking ) {
            AddHP(damageParam.damage, this);
            AddEffectPanel(ESkillType.Heal, 39, 0, (int)damageParam.damage);

            if( DungeonTestManager.Instance != null && CharacterManager.Instance.ActivePlayer.Equals(this) )
                DungeonTestManager.Instance.HealPoint += (int)damageParam.damage;
            return;
        }

        if( CheckHitID(damageParam.hitID) == false )
            return;

        damageParam_.Add(damageParam);
    }

    /// <summary>
    /// <para>name : DamageEffect</para>
    /// <para>parameter : DamageParam, bool</para>
    /// <para>return : void</para>
    /// <para>describe : Create damage effect.</para>
    /// </summary>
    void DamageEffect(DamageParam damageParam, bool critical) {
        if( CharacterManager.Instance.hitNormalEffect_ != null ) {
            Vector3 at = (transform.position - damageParam.attackerPosition).normalized;
            Quaternion r = Quaternion.LookRotation(-at);
            Vector3 p = new Vector3(transform.position.x, damageParam.effectY, transform.position.z) - (agentRadius_ * at);

            GameObject igo = null;
            if( EffectPoolManager.Instance.GetEffectFromPool(CharacterManager.Instance.hitNormalEffect_.name, p, r, out igo) == false )
                igo = Instantiate(CharacterManager.Instance.hitNormalEffect_, p, r) as GameObject;

            float rotate = Random.Range(-30, 30);
            igo.transform.Rotate(0, rotate, 0);
        }

        if( critical == true ) {
            if( CharacterManager.Instance.hitCriticalEffect_ != null ) {
                Quaternion r = Quaternion.identity;
                Vector3 p = transform.position + characterController_.center + new Vector3(0, 1, 0);

                GameObject igo = null;
                if( EffectPoolManager.Instance.GetEffectFromPool(CharacterManager.Instance.hitCriticalEffect_.name, p, r, out igo) == false )
                    igo = Instantiate(CharacterManager.Instance.hitCriticalEffect_, p, r) as GameObject;
            }
        }
    }

    /// <summary>
    /// <para>name : SetCurrentMotionCB</para>
    /// <para>parameter : int, ModelTemplate.SCondition[]</para>
    /// <para>return : void</para>
    /// <para>describe : Set character motion by model conditions.</para>
    /// </summary>
    public void SetCurrentMotionCB(int hashStateName, params ModelTemplate.SCondition[] conditions) {
        State state = State.None;
        int attackIndex = 0;
        int damageIndex = 0;

        bool enable = false;
        for( int i = 0; i < conditions.Length; i++ ) {
            switch( conditions[i].name ) {
                case "State":
                    state = (State)conditions[i].value;
                    enable = true;
                    continue;

                case "Attack":
                    attackIndex = (int)conditions[i].value;
                    continue;

                case "Damage":
                    damageIndex = (int)conditions[i].value;
                    continue;
            }
        }

        if( enable == false ) {
            nextState_ = State.None;
            return;
        }

        nextState_ = State.None;
        switch( state ) {
            case State.Stay: {
                    SetState(State.Stay);
                }
                break;
            case State.Attack: {
                    int comboIndex = 0;
                    for( int i = 0; i < selectedCombo_.combos.Length; ++i ) {
                        for( int n = 0; n < selectedCombo_.combos[i].conditions.Length; ++n ) {
                            if( selectedCombo_.combos[i].conditions[n].name == "Attack" && selectedCombo_.combos[i].conditions[n].value == attackIndex ) {
                                comboIndex = i;
                                break;
                            }
                        }
                    }

                    SetState(State.Attack, comboIndex);
                }
                break;
            case State.Move: {
                    SelectCombo();
                    SetState(State.Move);
                }
                break;
            case State.Damage: {
                    SetState(State.Damage, damageIndex);
                }
                break;
            case State.Death:
                break;
        }
    }

    public CharacterComboTable.CTable SelectedCombo {
        get {
            return selectedCombo_;
        }
    }

    /// <summary>
    /// <para>name : SelectComboInAttackRange</para>
    /// <para>parameter : Character</para>
    /// <para>return : bool</para>
    /// <para>describe : Select combo by attack range.</para>
    /// </summary>
    public bool SelectComboInAttackRange(Character enemy) {
        if( combo_ == null )
            return false;
        for( int i = 0; i < 10; ++i ) {
            int[] nextCombo = new int[1];
            nextCombo[0] = -1;

            CharacterComboTable.CTable combo;
            combo_.SelectCombo(out combo);

            float length = (enemy.transform.position - transform.position).magnitude - ((enemy.radius + radius));
            if( length >= GetAttackRange(combo.attackRange) ) { // combo.attackRange * scale_ ) {
                continue;
            }

            selectedCombo_ = combo;
            return true;
        }
        return false;
    }

    public AnimatorStateInfo CurrentBaseState {
        get {
            return currentBaseState_;
        }
    }

    public int HP {
        get {
            return (int)hp_;
        }
        set {
            hp_ = value;
        }
    }

    /// <summary>
    /// <para>name : AddDamagePanel</para>
    /// <para>parameter : int, int, bool, CharacterTypeAdvance</para>
    /// <para>return : void</para>
    /// <para>describe : Add damage panel.</para>
    /// </summary>
    void AddDamagePanel(int nDamageType, int nDamageValue, bool bCritical, CharacterTypeAdvance typeAdvance) {
        if( m_DamageText_Prefab == null )
            return;
        if( StageManager.Instance.stageTable.category.Equals(StageTableManager.Category.TIME_ATTACK) &&
            teamType.Equals(CharacterTeam.Type_Player) )
            return;

        if( teamType.Equals(CharacterTeam.Type_Player) ) {
            switch( StageManager.Instance.stageTable.category ) {
                case StageTableManager.Category.TUTORIAL:
                case StageTableManager.Category.NORMAL:
                case StageTableManager.Category.INFINITE:
                case StageTableManager.Category.EXPEDITION:
                case StageTableManager.Category.BOSS_ATTACK:
                    if( CharacterManager.Instance.ActivePlayer != null &&
                        CharacterManager.Instance.ActivePlayer.Equals(this) == false )
                        return;

                    break;

                case StageTableManager.Category.TIME_ATTACK:
                    return;

                default:
                    break;
            }
        }

        CharacterDamageImageText damagePanel = Instantiate(m_DamageText_Prefab) as CharacterDamageImageText;
        damagePanel.gameObject.transform.parent = m_Parant_Panel;

        damagePanel.DamageTextInit(nDamageValue, m_DamageText_Target, characterController_.height * scale_, m_WorldCamera, m_GuiCamera, typeAdvance, bCritical);

        m_nDamagePanelCount++;
    }

    int testComboID_ = 0;
    public void TestCombo(int id) {
        testComboID_ = id;
        combo_.GetCombo(id, out selectedCombo_);
        attackIndex_ = 0;
        SetStateStay();
    }

    public bool TestComboAttack() {
        if( testComboID_ == 0 )
            return false;
        if( selectedCombo_ == null ) {
            SetStateStay();
            return false;
        }
        AgentStop();
        switch( state_ ) {
            case State.Attack: {
                    if( nextState_ == State.Attack )
                        return false;

                    if( attackIndex_ < selectedCombo_.combos.Length && selectedCombo_.combos[attackIndex_].stateNameHash != currentBaseState_.nameHash ) {
                        return false;
                    }

                    CharacterComboTable.SCombo oldCombo = selectedCombo_.combos[attackIndex_];
                    bool currentSkillCombo = selectedCombo_.skillID != 0;
                    int index = 0;
                    if( currentSkillCombo == false && useSkillCombo_.Count != 0 ) {
                        SelectCombo();
                        break;
                    }
                    else {
                        index = attackIndex_ + 1;
                        if( index >= selectedCombo_.combos.Length ) {
                            index = 0;
                            SelectCombo();
                        }
                    }

                    var combo = selectedCombo_.combos[index];
                    if( SetNextState(oldCombo.stateNameHash, State.Attack, combo.stateNameHash, combo.stateName, combo.conditions) == true ) {
                        nextAttackIndex_ = index;
                    }
                    return true;
                }

            case State.MidairDamage: {
                    attackIndex_ = 0;
                    return false;
                }
        };
        return SetState(State.Attack, 0);
    }

    /// <summary>
    /// <para>name : AddPlayerHPBar</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Add hp bar on DungeonPlayEvent UI.</para>
    /// </summary>
    public void AddPlayerHPBar() {
        if( characterTable_ != null && characterTable_.isBoss )
            return;
        if( m_HpBar_Prefab == null )
            return;

        m_HpBar_Panel = Instantiate(m_HpBar_Prefab) as CharacterHpBar;

        Vector3 size = m_HpBar_Panel.transform.localScale;
        m_HpBar_Panel.Init(m_HpBar_Target, m_WorldCamera, m_GuiCamera);
        m_HpBar_Panel.SpawnAt(m_HpBar_Target, size, m_Parant_Panel, characterController_.height * scale_);
        m_HpBar_Panel.FollowTarget = true;
        m_HpBar_Panel.Following();
        m_HpBar_Panel.SetPlayerHP_Bar((int)hp_, maxHP);
        m_HpBar_Panel.SetPlayerAttribute(characterTable_.type);

        if( m_HpBar_Panel != null )
            m_HpBar_Panel.SetActive(this.gameObject.activeSelf);
    }

    /// <summary>
    /// <para>name : RefreshHpBarActivePlayer</para>
    /// <para>parameter : bool</para>
    /// <para>return : void</para>
    /// <para>describe : Set Active hp bar.</para>
    /// </summary>
    public void RefreshHpBarActivePlayer(bool active) {
        if( m_HpBar_Panel != null )
            m_HpBar_Panel.SetActive(true);
    }

    public bool MidairDamageTest() {
        switch( state_ ) {
            case State.MidairDamage: {
                    jumpingDist_ = 15f;
                    jumpingAngle_ = 70.0f;
                }
                break;
            default: {
                    jumpingDist_ = 50.0f;
                    jumpingAngle_ = 80.0f;
                }
                break;
        };
        SetState(State.MidairDamage);
        return true;
    }

    /// <summary>
    /// <para>name : Warp</para>
    /// <para>parameter : Transform</para>
    /// <para>return : void</para>
    /// <para>describe : Warp this object to (Transform)parameter position.</para>
    /// </summary>
    public void Warp(Transform trans) {
        if( agent_ == null )
            agent_ = gameObject.GetComponent<NavMeshAgent>();

        agent_.enabled = false;

        transform.position = trans.position;
        transform.rotation = trans.rotation;

        agent_.enabled = true;
    }

    /// <summary>
    /// <para>name : Warp</para>
    /// <para>parameter : Vector3</para>
    /// <para>return : void</para>
    /// <para>describe : Warp this object to (Vector3)parameter.</para>
    /// </summary>
    public void Warp(Vector3 position) {
        if( agent_ == null )
            agent_ = gameObject.GetComponent<NavMeshAgent>();

        agent_.enabled = false;

        transform.position = position;

        agent_.enabled = true;
    }

    /// <summary>
    /// <para>name : Warp</para>
    /// <para>parameter : RespawnPoint</para>
    /// <para>return : void</para>
    /// <para>describe : Warp this object to (RespawnPoint)parameter position.</para>
    /// </summary>
    public void Warp(RespawnPoint point) {
        if( agent_ == null )
            agent_ = gameObject.GetComponent<NavMeshAgent>();

        agent_.enabled = false;

        transform.position = point.transform.position;
        transform.eulerAngles = point.transform.eulerAngles;

        agent_.enabled = true;
    }

    #region skill

    public List<SkillTableManager.CTable> skillTable {
        get {
            return skillTable_;
        }
    }

    /// <summary>
    /// <para>name : GetComboSkill</para>
    /// <para>parameter : int, out CharacterComboTable.CTable</para>
    /// <para>return : bool</para>
    /// <para>describe : Get combo skill table from select combo.</para>
    /// </summary>
    public bool GetComboSkill(int skillID, out CharacterComboTable.CTable skillCombo) {
        return combo_.GetComboSkill(skillID, out skillCombo);
    }

    public class CSkillCoolTime {
        public bool isFirst_ = true;
        public bool start;
        public float startTime;
        public float fullTime;
    }

    Dictionary<int, CSkillCoolTime> skillCoolTime_ = new Dictionary<int, CSkillCoolTime>();
    List<CharacterComboTable.CTable> useSkillCombo_ = new List<CharacterComboTable.CTable>();

    /// <summary>
    /// <para>name : UseSkill</para>
    /// <para>parameter : int</para>
    /// <para>return : bool</para>
    /// <para>describe : Use skill by (int)parameter.</para>
    /// </summary>
    public bool UseSkill(int skillID) {
        if( selectedCombo_ != null ) {
            if( selectedCombo_.skillID == skillID )
                return false;
        }

        CharacterComboTable.CTable skillCombo;
        if( combo_.GetComboSkill(skillID, out skillCombo) == false )
            return false;
        if( skillCombo == null )
            return false;

        List<Character> enemyList;
        if( FindEnemy(out enemyList, 20.0f) == false )
            return false;

        if( skillCoolTime_.ContainsKey(skillCombo.skillID) == true )
            return false;

        for( int i = 0; i < useSkillCombo_.Count; ++i ) {
            if( useSkillCombo_[i].skillID == skillID )
                return false;
        }

        AddSkillCoolTime(skillID);

        if( selectedCombo_.skillID != 0 ) {
            useSkillCombo_.Add(skillCombo);
        }
        else {
            if( state_ == State.Attack ) {
                useSkillCombo_.Add(skillCombo);
            }
            else {
                bool now = false;
                var enemyPresent = EnemyPresent;
                if( enemyPresent == null ) {
                    now = false;
                    enemyPresent = enemyList[0];
                }

                float length = (enemyPresent.transform.position - transform.position).magnitude - ((enemyPresent.radius + radius));
                now = length < GetAttackRange(skillCombo.attackRange);  // skillCombo.attackRange * scale_;

                if( MovementTargetEnable == false ) {
                    SetMovementTarget(enemyPresent.transform.position, enemyPresent, true, State.Move);
                }
                if( now ) {
                    model_.ResetNextMotion();
                    nextState_ = State.None;
                    attackIndex_ = 0;
                    SetCombo(skillCombo);
                    Attack();
                    // StartSkillCoolTime(skillID);
                }
                else {
                    model_.ResetNextMotion();
                    nextState_ = State.None;
                    attackIndex_ = 0;
                    SetCombo(skillCombo);
                }
            }
        }

        if( teamType.Equals(CharacterTeam.Type_Player) ) {
            SkillTableManager.CTable skillTable = skillTable_.Find(delegate(SkillTableManager.CTable table) {
                return table.id.Equals(skillID);
            });

            AddSkillNamePanel(skillTable.nNameString);
        }

        return true;
    }

    /// <summary>
    /// <para>name : InstantlySkill</para>
    /// <para>parameter : int</para>
    /// <para>return : bool</para>
    /// <para>describe : Use skill by (int)parameter instantly.</para>
    /// </summary>
    public bool InstantlySkill(int skillID) {
        switch( state_ ) {
            case State.Death:
            case State.Damage:
            case State.MidairDamage:
            case State.DownDamage:
            case State.BuffDebuff_Fear:
            case State.BuffDebuff_Freeze:
            case State.BuffDebuff_Stun:
            case State.BuffDebuff_Sleep:
            case State.BuffDebuff_Stone:
                return false;
        }

        if( selectedCombo_ != null ) {
            if( selectedCombo_.skillID == skillID )
                return false;
        }

        CharacterComboTable.CTable skillCombo;
        if( combo_.GetComboSkill(skillID, out skillCombo) == false )
            return false;
        if( skillCombo == null )
            return false;

        List<Character> enemyList;
        if( FindEnemy(out enemyList, 20.0f) )
            if( enemyList[0] != null )
                EnemyPresent = enemyList[0];

        if( skillCoolTime_.ContainsKey(skillCombo.skillID) == true )
            return false;

        for( int i = 0; i < useSkillCombo_.Count; ++i ) {
            if( useSkillCombo_[i].skillID == skillID )
                return false;
        }

        for( int i = 0; i < skillTable_.Count; i++ ) {
            if( skillTable_[i].id.Equals(skillID) == false )
                AddSkillGlobalCoolTime(skillTable_[i].id, 1.0f);
            else {
                AddSkillCoolTime(skillTable_[i].id);
                AddSkillNamePanel(skillTable_[i].nNameString);
            }
        }

        nextAttackTime_ = Time.time;

        model_.ResetNextMotion();

        state_ = State.None;
        nextState_ = State.None;

        attackIndex_ = 0;
        SetCombo(skillCombo);

        Attack();

        return true;
    }

    public enum SkillUseState {
        NONE,
        STANDBY,
        ACTION,
    }

    /// <summary>
    /// <para>name : UseSkill</para>
    /// <para>parameter : int, out SkillUseState</para>
    /// <para>return : bool</para>
    /// <para>describe : Use skill by (int)parameter and get SkillUseState</para>
    /// </summary>
    public bool UseSkill(int skillID, out SkillUseState skillUseState_) {
        skillUseState_ = SkillUseState.NONE;

        if( selectedCombo_ != null ) {
            if( selectedCombo_.skillID == skillID )
                return false;
        }

        CharacterComboTable.CTable skillCombo;
        if( combo_.GetComboSkill(skillID, out skillCombo) == false )
            return false;
        if( skillCombo == null )
            return false;

        List<Character> enemyList;
        if( FindEnemy(out enemyList, 20.0f) == false )
            return false;

        if( skillCoolTime_.ContainsKey(skillCombo.skillID) == true )
            return false;

        for( int i = 0; i < useSkillCombo_.Count; ++i ) {
            if( useSkillCombo_[i].skillID == skillID )
                return false;
        }

        AddSkillCoolTime(skillID);

        if( selectedCombo_.skillID != 0 ) {
            useSkillCombo_.Add(skillCombo);
            skillUseState_ = SkillUseState.STANDBY;
        }

        else {
            if( state_ == State.Attack ) {
                useSkillCombo_.Add(skillCombo);
                skillUseState_ = SkillUseState.STANDBY;
            }

            else {
                bool now = false;
                var enemyPresent = EnemyPresent;
                if( enemyPresent == null ) {
                    now = false;
                    enemyPresent = enemyList[0];
                }

                if( ClientDataManager.Instance.Auto_Play == false && 
                    BattleItemManager.Instance.GetBattleItemState(BattleItemType.Type_Auto_Command, ClientDataManager.Instance.SelectGameMode) == false ) {
                    model_.ResetNextMotion();
                    nextState_ = State.None;
                    attackIndex_ = 0;
                    SetCombo(skillCombo);
                    Attack();
                    skillUseState_ = SkillUseState.ACTION;

                    return true;
                }

                float length = (enemyPresent.transform.position - transform.position).magnitude - ((enemyPresent.radius + radius));
                now = length < GetAttackRange(skillCombo.attackRange); // skillCombo.attackRange * scale_;

                if( MovementTargetEnable == false ) {
                    SetMovementTarget(enemyPresent.transform.position, enemyPresent, true, State.Move);
                }

                if( now ) {
                    model_.ResetNextMotion();
                    nextState_ = State.None;
                    attackIndex_ = 0;
                    SetCombo(skillCombo);
                    Attack();
                    // StartSkillCoolTime(skillID);
                    skillUseState_ = SkillUseState.ACTION;
                }

                else {
                    model_.ResetNextMotion();
                    nextState_ = State.None;
                    attackIndex_ = 0;
                    SetCombo(skillCombo);
                    skillUseState_ = SkillUseState.STANDBY;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// <para>name : AddSkillCoolTime</para>
    /// <para>parameter : int</para>
    /// <para>return : void</para>
    /// <para>describe : Add skill cooltime if skill used.</para>
    /// </summary>
    void AddSkillCoolTime(int skillID) {
        SkillTableManager.CTable comboSkill;
        if( SkillTableManager.Instance.FindSkillTable(skillID, out comboSkill) == false ) {
            MagiDebug.LogError(string.Format("skill not found. {0}", skillID));
            return;
        }

        CSkillCoolTime skillCoolTime;
        if( skillCoolTime_.TryGetValue(skillID, out skillCoolTime) == true ) {
            skillCoolTime.startTime = Time.time;
            skillCoolTime.fullTime = comboSkill.coolTime;

            return;
        }

        skillCoolTime = new CSkillCoolTime();
        skillCoolTime.fullTime = comboSkill.coolTime;

        skillCoolTime.start = false;
        skillCoolTime_.Add(skillID, skillCoolTime);
    }

    /// <summary>
    /// <para>name : AddSkillGlobalCoolTime</para>
    /// <para>parameter : int, float</para>
    /// <para>return : void</para>
    /// <para>describe : Add global skill cooltime value (float)parameter, if skill not used.</para>
    /// </summary>
    void AddSkillGlobalCoolTime(int skillID, float addGlobalCoolTime) {
        if( skillCoolTime_.ContainsKey(skillID) )
            return;

        CSkillCoolTime skillCoolTime = new CSkillCoolTime();
        skillCoolTime.startTime = Time.time;
        skillCoolTime.fullTime = addGlobalCoolTime;

        skillCoolTime.start = true;
        skillCoolTime_.Add(skillID, skillCoolTime);
    }

    /// <summary>
    /// <para>name : StartSkillCoolTime</para>
    /// <para>parameter : int</para>
    /// <para>return : void</para>
    /// <para>describe : Start skill cooltime if skill used.</para>
    /// </summary>
    void StartSkillCoolTime(int skillID) {
        if( skillCoolTime_.ContainsKey(skillID) == false )
            return;

        CSkillCoolTime skillCoolTime;
        if( skillCoolTime_.TryGetValue(skillID, out skillCoolTime) ) {
            skillCoolTime.start = true;
            skillCoolTime.startTime = Time.time;
        }

        GameManager.Instance.SetUseSkillCoolTime(skillID);
    }

    /// <summary>
    /// <para>name : GetSkillCoolNormalizedTime</para>
    /// <para>parameter : out float, int</para>
    /// <para>return : bool</para>
    /// <para>describe : Get skill cooltime normalized value.</para>
    /// </summary>
    public bool GetSkillCoolNormalizedTime(out float normalizedTime, int skillID) {
        normalizedTime = 0;
        CSkillCoolTime SCoolTime;
        if( skillCoolTime_.TryGetValue(skillID, out SCoolTime) == false )
            return false;

        if( SCoolTime.start == false )
            return true;

        normalizedTime = (Time.time - SCoolTime.startTime) / SCoolTime.fullTime;
        normalizedTime = Mathf.Clamp(normalizedTime, 0, 1.0f);
        return true;
    }

    /// <summary>
    /// <para>name : GetSkillCoolRemainTime</para>
    /// <para>parameter : out int, int</para>
    /// <para>return : bool</para>
    /// <para>describe : Get skill cooltime remain value.</para>
    /// </summary>
    public bool GetSkillCoolRemainTime(out int nTime, int skillID) {
        nTime = 0;
        CSkillCoolTime SCoolTime;
        if( skillCoolTime_.TryGetValue(skillID, out SCoolTime) == false )
            return false;

        if( SCoolTime.start == false )
            return true;

        nTime = (int)(SCoolTime.fullTime - (Time.time - SCoolTime.startTime - 1.0f));
        return true;
    }

    /// <summary>
    /// <para>name : GetAvailable_BestSkill</para>
    /// <para>parameter : out SkillTableManager.CTable</para>
    /// <para>return : bool</para>
    /// <para>describe : Get best skill if it available.</para>
    /// </summary>
    public bool GetAvailable_BestSkill(out SkillTableManager.CTable skillTable) {
        ESkillType bestSkillType = ESkillType.None;
        ESkillType currentSkilltype = ESkillType.None;

        SkillTableManager.CTable bestSkillTable = null;

        skillTable = null;

        switch( state_ ) {
            case State.Respawn:
            case State.Damage:
            case State.MidairDamage:
                return false;
        }

        if( skillTable_ == null )
            return false;
        if( selectedCombo_ == null )
            return false;
        if( lobbyState.Equals(LobbyState.Type_None) == false )   //로비 캐릭터일때는 Return
            return false;

        if( useSkillCombo_.Count != 0 )
            return false;

        for( int i = 0; i < skillTable_.Count; i++ ) {
            if( skillTable_[i] == null )
                continue;
            if( selectedCombo_ != null ) {
                if( skillTable_[i].id == selectedCombo_.skillID )
                    continue;
            }

            bool find = false;
            for( int count_ = 0; count_ < useSkillCombo_.Count; ++count_ ) {
                if( useSkillCombo_[count_].skillID.Equals(skillTable_[count_].id) ) {
                    find = true;
                    break;
                }
            }

            if( find )
                continue;

            if( skillCoolTime_.ContainsKey(skillTable_[i].id) == true )
                continue;

            currentSkilltype = GetBestSkillType(skillTable_[i]);
            if( currentSkilltype <= bestSkillType ) {
                continue;
            }

            else {
                bestSkillTable = skillTable_[i];
                bestSkillType = currentSkilltype;
            }
        }

        if( bestSkillTable != null ) {
            skillTable = bestSkillTable;
            return true;
        }

        return false;
    }

    /// <summary>
    /// <para>name : GetBestSkillType</para>
    /// <para>parameter : SkillTableManager.CTable</para>
    /// <para>return : ESkillType</para>
    /// <para>describe : Get best skill type in skill table.</para>
    /// </summary>
    ESkillType GetBestSkillType(SkillTableManager.CTable skillTable) {
        ESkillType bestType = ESkillType.None;
        ESkillProperty currentSkillProperty;

        List<Character> enemy;

        for( int i = 0; i < skillTable.skillPropertyList.Count; i++ ) {
            currentSkillProperty = skillTable.skillPropertyList[i];

            if( currentSkillProperty.type <= bestType )
                continue;

            switch( currentSkillProperty.type ) {
                case ESkillType.Attack:
                case ESkillType.BloodSucking:
                    if( FindEnemy(out enemy, 10.0f) )
                        bestType = currentSkillProperty.type;

                    break;

                case ESkillType.Heal:
                case ESkillType.HealMaxRatio:
                    int invokeHP = Mathf.RoundToInt(maxHP * 0.6f); // maxHP - (int)(maxHP * (skillTable.skillPropertyList[i].GetValue * 0.8f));
                    if( HP < invokeHP )
                        bestType = currentSkillProperty.type;

                    break;

                case ESkillType.Debuff_Stun:
                case ESkillType.Debuff_MinusPhysicalDefense:
                case ESkillType.Debuff_MinusPhysicalAttack:
                case ESkillType.Debuff_MinusMagicDefense:
                case ESkillType.Debuff_MinusMagicAttack:
                case ESkillType.Debuff_MinusAttackSpeed:
                case ESkillType.Debuff_MinusMoveSpeed:
                    if( FindEnemy(out enemy, 10.0f) )
                        bestType = currentSkillProperty.type;

                    break;

                case ESkillType.Buff_AddPhysicalDefense:
                case ESkillType.Buff_AddPhysicalAttack:
                case ESkillType.Buff_AddMagicDefense:
                case ESkillType.Buff_AddMagicAttack:
                case ESkillType.Buff_AddCritical:
                case ESkillType.Buff_AddAvoidChance:
                case ESkillType.Buff_AddAttackSpeed:
                case ESkillType.Buff_AddMoveSpeed:
                    bestType = currentSkillProperty.type;

                    break;
            }
        }

        return bestType;
    }

    /// <summary>
    /// <para>name : UseDirectSkill</para>
    /// <para>parameter : SkillTableManager.CTable</para>
    /// <para>return : void</para>
    /// <para>describe : Use skill table values instantly.</para>
    /// </summary>
    void UseDirectSkill(SkillTableManager.CTable skillTable) {
        AddBuff(skillTable.skillPropertyList);
    }

    #endregion

    #region EquipItem

    int equipItemHp_;
    int equipItemPhysicalAttack_;
    int equipItemPhysicalDefense_;
    int equipItemMagicAttack_;
    int equipItemMagicDefense_;

    float equipItemHpRatio_;
    float equipItemPhysicalAttackRatio_;
    float equipItemPhysicalDefenseRatio_;
    float equipItemMagicAttackRatio_;
    float equipItemMagicDefenseRatio_;

    float equipItemMoveSpeedRatio_;
    float equipItemCriticalRatio_;
    float equipItemAvoidChance_;
    float equipItemAttackSpeed_;

    bool isEquipItemSuperArmor_;

    [HideInInspector]
    public float equipItemAttakEffectRatio_;
    [HideInInspector]
    public float equipItemBloodSuckingRatio_;
    [HideInInspector]
    public float equipItemHealRatio_;
    [HideInInspector]
    public float equipItemBuffRatio_;
    [HideInInspector]
    public float equipItemDebuffRatio_;
    [HideInInspector]
    public float equipItemAttributeDamageRatio_;

    /// <summary>
    /// <para>name : EquipItemStateInit</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Initialize equip item status value.</para>
    /// </summary>
    void EquipItemStateInit() {
        equipItemHp_ = equipItemPhysicalAttack_ = equipItemPhysicalDefense_ = equipItemMagicAttack_ = equipItemMagicDefense_ = 0;
        equipItemHpRatio_ = equipItemPhysicalAttackRatio_ = equipItemPhysicalDefenseRatio_ = equipItemMagicAttackRatio_ = equipItemMagicDefenseRatio_ = 0.0f;

        equipItemMoveSpeedRatio_ = equipItemAttakEffectRatio_ = equipItemBloodSuckingRatio_ = equipItemHealRatio_ = equipItemBuffRatio_ = 0.0f;
        equipItemDebuffRatio_ = equipItemAttributeDamageRatio_ = equipItemCriticalRatio_ = equipItemAvoidChance_ = equipItemAttackSpeed_ = 0.0f;

        isEquipItemSuperArmor_ = false;
    }

    /// <summary>
    /// <para>name : EquipItem_RefreshStatus</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Refresh equip item status value.</para>
    /// </summary>
    void EquipItem_RefreshStatus() {
        EquipItemStateInit();

        List<ClientDataManager.Gear_Info> gearList = new List<ClientDataManager.Gear_Info>();
        ClientDataManager.Gear_Info gearInfo = null;

        if( teamInfo_.equipGearList == null )
            return;

        for( int i = 0; i < teamInfo_.equipGearList.Count; i++ ) {
            if( ClientDataManager.Instance.FindGearInfo(teamInfo_.equipGearList[i], out gearInfo, teamType) )
                gearList.Add(gearInfo);
        }

        for( int i = 0; i < gearList.Count; i++ ) {
            if( gearList[i].baseOptionList != null ) {
                for( int count = 0; count < gearList[i].baseOptionList.Count; count++ ) {
                    switch( gearList[i].baseOptionList[count].option ) {
                        case ItemOptionType.Type_Hp_Value:
                            equipItemHp_ += (int)(gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans));
                            break;
                        case ItemOptionType.Type_Physical_Attack_Value:
                            equipItemPhysicalAttack_ += (int)(gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans));
                            break;
                        case ItemOptionType.Type_Magic_Attack_Value:
                            equipItemMagicAttack_ += (int)(gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans));
                            break;
                        case ItemOptionType.Type_Physical_Defence_Value:
                            equipItemPhysicalDefense_ += (int)(gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans));
                            break;
                        case ItemOptionType.Type_Magic_Defence_Value:
                            equipItemMagicDefense_ += (int)(gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans));
                            break;
                        case ItemOptionType.Type_Hp_Ratio:
                            equipItemHpRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Physical_Attack_Ratio:
                            equipItemPhysicalAttackRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Magic_Attack_Ratio:
                            equipItemMagicAttackRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Physical_Defence_Ratio:
                            equipItemPhysicalDefenseRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Magic_Defence_Ratio:
                            equipItemMagicDefenseRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Move_Speed_Ratio:
                            equipItemMoveSpeedRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Attack_Effect_Ratio:
                            equipItemAttakEffectRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_BloodSucking_Ratio:
                            equipItemBloodSuckingRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Heal_Ratio:
                            equipItemHealRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Buff_Ratio:
                            equipItemBuffRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Debuff_Ratio:
                            equipItemDebuffRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Attribute_Damage_Ratio:
                            equipItemAttributeDamageRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Critical_Ratio:
                            equipItemCriticalRatio_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_Avoid_Ratio:
                            equipItemAvoidChance_ += gearList[i].baseOptionList[count].value * 
                                ItemUpgradeTableManager.Instance.GetAddValue(gearList[i].baseOptionList[count].option, gearList[i].trans);
                            break;
                        case ItemOptionType.Type_SuperArmor:
                            isEquipItemSuperArmor_ = true;
                            break;
                    }
                }
            }
        }

        List<ItemSetEffectTableManager.STable> setEffectTableGroup = new List<ItemSetEffectTableManager.STable>();
        if( ItemSetEffectTableManager.Instance.GetSetEffect(gearList, out setEffectTableGroup) ) {
            for( int i = 0; i < setEffectTableGroup.Count; i++ ) {
                switch( setEffectTableGroup[i].setOption.option ) {
                    case ItemOptionType.Type_Hp_Value:
                        equipItemHp_ += (int)setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Physical_Attack_Value:
                        equipItemPhysicalAttack_ += (int)setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Magic_Attack_Value:
                        equipItemMagicAttack_ += (int)setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Physical_Defence_Value:
                        equipItemPhysicalDefense_ += (int)setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Magic_Defence_Value:
                        equipItemMagicDefense_ += (int)setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Hp_Ratio:
                        equipItemHpRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Physical_Attack_Ratio:
                        equipItemPhysicalAttackRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Magic_Attack_Ratio:
                        equipItemMagicAttackRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Physical_Defence_Ratio:
                        equipItemPhysicalDefenseRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Magic_Defence_Ratio:
                        equipItemMagicDefenseRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Move_Speed_Ratio:
                        equipItemMoveSpeedRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Attack_Effect_Ratio:
                        equipItemAttakEffectRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_BloodSucking_Ratio:
                        equipItemBloodSuckingRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Heal_Ratio:
                        equipItemHealRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Buff_Ratio:
                        equipItemBuffRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Debuff_Ratio:
                        equipItemDebuffRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Attribute_Damage_Ratio:
                        equipItemAttributeDamageRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Critical_Ratio:
                        equipItemCriticalRatio_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_Avoid_Ratio:
                        equipItemAvoidChance_ += setEffectTableGroup[i].setOption.value;
                        break;
                    case ItemOptionType.Type_SuperArmor:
                        isEquipItemSuperArmor_ = true;
                        break;
                }
            }
        }
    }

    #endregion

    #region Add_Battle_Buff

    private const float addBuffValue = 0.05f;

    /// <summary>
    /// <para>name : AddBattleBuff</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Add battle item buff values.</para>
    /// </summary>
    private void AddBattleBuff() {
        if( CharacterManager.Instance.CheckTeamType(teamType) ) {
            if( BattleItemManager.Instance.GetBattleItemState(BattleItemType.Type_Add_Damage, ClientDataManager.Instance.SelectGameMode) ) {
                attack_ = Mathf.RoundToInt(attack_ + attack_ * addBuffValue);
                magicAttack_ = Mathf.RoundToInt(magicAttack_ + magicAttack_ * addBuffValue);
                critical_ = critical_ + addBuffValue;
            }

            if( BattleItemManager.Instance.GetBattleItemState(BattleItemType.Type_Add_Defense, ClientDataManager.Instance.SelectGameMode) ) {
                defense_ = Mathf.RoundToInt(defense_ + defense_ * addBuffValue);
                magicDefense_ = Mathf.RoundToInt(magicDefense_ + magicDefense_ * addBuffValue);
                avoidChance_ = avoidChance_ + addBuffValue;
            }
        }
    }

    #endregion

    #region BuffDebuff

    List<CBuffDebuff> buff_ = new List<CBuffDebuff>();
    List<CBuffDebuff> debuff_ = new List<CBuffDebuff>();
    State BuffDebuffState_ = State.None;

    float buffMoveSpeed_ = 0;
    float debuffMoveSpeed_ = 0;
    float buffAttackSpeed_ = 0;
    float debuffAttackSpeed_ = 0;

    /// <summary>
    /// <para>name : AddBuffDebuffHP</para>
    /// <para>parameter : float, Character</para>
    /// <para>return : void</para>
    /// <para>describe : Add/Minus hp value. </para>
    /// </summary>
    public void AddBuffDebuffHP(float hp, Character attacker) {
        if( state.Equals(State.Death) )
            return;

        hp_ = Mathf.Clamp(hp_ + hp, 0, maxHP);

        if( m_HpBar_Panel == null )
            AddPlayerHPBar();

        if( m_HpBar_Panel != null )
            m_HpBar_Panel.SetPlayerHP_Bar((int)hp_, maxHP);

        if( hp_ < 1.0f )
            Death();
    }

    /// <summary>
    /// <para>name : AddBuff</para>
    /// <para>parameter : (ESkillProperty)List</para>
    /// <para>return : void</para>
    /// <para>describe : Add Buff list.</para>
    /// </summary>
    public void AddBuff(List<ESkillProperty> eSkillPropertyList) {
        if( eSkillPropertyList == null )
            return;

        List<Character> listTeam;
        for( int i = 0; i < eSkillPropertyList.Count; i++ ) {
            if( eSkillPropertyList[i].isTeamEffect ) {
                if( CharacterManager.Instance.FindCharacterInTeam(out listTeam, teamType) == false )
                    continue;

                for( int teamCount = 0; teamCount < listTeam.Count; teamCount++ ) {
                    listTeam[teamCount].SetBuff(eSkillPropertyList[i], this, i);
                }
            }

            else {
                SetBuff(eSkillPropertyList[i], this, i);
            }
        }
    }

    /// <summary>
    /// <para>name : SetBuff</para>
    /// <para>parameter : ESkillProperty, Character, int</para>
    /// <para>return : void</para>
    /// <para>describe : Set buff values by (Character)parameter.</para>
    /// </summary>
    public void SetBuff(ESkillProperty skillProperty, Character ownerCharacter, int count) {
        CBuffDebuff buffDebuff = new CBuffDebuff();
        LoadAssetbundle.LoadPrefabComplateCB loadComplateCB;

        switch( skillProperty.type ) {
            case ESkillType.Heal:
                float healValue = 0;

                if( skillProperty.baseStatus.Equals(ESkillBaseStatus.Physical) )
                    healValue = ownerCharacter.attack * (skillProperty.GetValue + ownerCharacter.equipItemHealRatio_);
                else if( skillProperty.baseStatus.Equals(ESkillBaseStatus.Magic) )
                    healValue = ownerCharacter.magicAttack * (skillProperty.GetValue + ownerCharacter.equipItemHealRatio_);

                if( skillProperty.GetDuration(ownerCharacter).Equals(0) ) {
                    AddHP((int)(healValue), ownerCharacter);
                    AddEffectPanel(skillProperty.type, skillProperty.stringID, count, (int)(healValue));
                }
                else {
                    buffDebuff = new CBuffDebuff();

                    buffDebuff.type = skillProperty.type;
                    buffDebuff.time = skillProperty.GetDuration(ownerCharacter);
                    buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                    buffDebuff.value = healValue / skillProperty.GetDuration(ownerCharacter);
                    buffDebuff.me = this;
                    buffDebuff.attacker = null;
                    buffDebuff.stringID = skillProperty.stringID;

                    if( skillProperty.effectPath != "" ) {
                        loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadTimeEffectCompleteCB);
                        PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, true, buffDebuff);
                    }

                    else {
                        AddBuffList(buffDebuff);
                        buffDebuff.StartBuffTime();
                    }
                }

                if (DungeonTestManager.Instance != null && CharacterManager.Instance.ActivePlayer.Equals(this) == true)
                    DungeonTestManager.Instance.HealPoint += (int)healValue;

                break;

            case ESkillType.HealMaxRatio:
                float healMaxRatioValue = maxHP_ * skillProperty.GetValue;

                if( skillProperty.GetDuration(ownerCharacter).Equals(0) ) {
                    AddHP((int)(healMaxRatioValue), ownerCharacter);
                    AddEffectPanel(skillProperty.type, skillProperty.stringID, count, (int)(healMaxRatioValue));
                }
                else {
                    buffDebuff = new CBuffDebuff();

                    buffDebuff.type = skillProperty.type;
                    buffDebuff.time = skillProperty.GetDuration(ownerCharacter);
                    buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                    buffDebuff.value = healMaxRatioValue;
                    buffDebuff.me = this;
                    buffDebuff.attacker = null;
                    buffDebuff.stringID = skillProperty.stringID;

                    if( skillProperty.effectPath != "" ) {
                        loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadTimeEffectCompleteCB);
                        PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, true, buffDebuff);
                    }

                    else {
                        AddBuffList(buffDebuff);
                        buffDebuff.StartBuffTime();
                    }
                }

                break;

            case ESkillType.Buff_AddAttackSpeed:
                buffAttackSpeed_ += skillProperty.GetValue;

                buffDebuff = new CBuffDebuff();

                buffDebuff.type = skillProperty.type;
                buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                buffDebuff.value = skillProperty.GetValue;
                buffDebuff.attacker = null;
                buffDebuff.stringID = skillProperty.stringID;

                if( skillProperty.effectPath != "" ) {
                    loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadEffectCompleteCB);
                    PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, true, buffDebuff);
                }

                else {
                    AddBuffList(buffDebuff);
                }

                AddEffectPanel(skillProperty.type, skillProperty.stringID, count);
                break;

            case ESkillType.Buff_AddMoveSpeed:
                buffMoveSpeed_ += skillProperty.GetValue;

                buffDebuff = new CBuffDebuff();

                buffDebuff.type = skillProperty.type;
                buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                buffDebuff.value = skillProperty.GetValue;
                buffDebuff.attacker = null;
                buffDebuff.stringID = skillProperty.stringID;

                if( skillProperty.effectPath != "" ) {
                    loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadEffectCompleteCB);
                    PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, true, buffDebuff);
                }

                else {
                    AddBuffList(buffDebuff);
                }

                AddEffectPanel(skillProperty.type, skillProperty.stringID, count);
                break;

            case ESkillType.Buff_AddPhysicalDefense:
            case ESkillType.Buff_AddPhysicalAttack:
            case ESkillType.Buff_AddMagicDefense:
            case ESkillType.Buff_AddMagicAttack:
            case ESkillType.Buff_AddCritical:
            case ESkillType.Buff_AddAvoidChance:
            case ESkillType.Buff_SuperArmor:
                buffDebuff = new CBuffDebuff();

                buffDebuff.type = skillProperty.type;
                buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                buffDebuff.value = skillProperty.GetValue + ownerCharacter.equipItemBuffRatio_;
                buffDebuff.attacker = null;
                buffDebuff.stringID = skillProperty.stringID;

                if( skillProperty.effectPath != "" ) {
                    loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadEffectCompleteCB);
                    PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, true, buffDebuff);
                }

                else {
                    AddBuffList(buffDebuff);
                }

                AddEffectPanel(skillProperty.type, skillProperty.stringID, count);
                break;

            case ESkillType.Buff_RewindSkillTime:
                foreach( CSkillCoolTime coolTime in skillCoolTime_.Values ) {
                    if( coolTime.start )
                        coolTime.startTime -= coolTime.fullTime * skillProperty.GetDuration(ownerCharacter); 
                }

                // skillCoolTime_.Clear();
                break;
        }

        RefreshStatus();
    }

    /// <summary>
    /// <para>name : AddBuffList</para>
    /// <para>parameter : CBuffDebuff</para>
    /// <para>return : void</para>
    /// <para>describe : Add buff list if this object contains buff list.</para>
    /// </summary>
    void AddBuffList(CBuffDebuff buff) {
        buff_.Exists(delegate(CBuffDebuff a) {
            if( a.type.Equals(buff.type) ) {
                a.endTime = 0;
                if( a.CheckOnBuffDebuff )
                    a.StopBuffTime();

                return true;
            }

            else {
                return false;
            }
        });

        buff_.Add(buff);
    }

    /// <summary>
    /// <para>name : AddDebuff</para>
    /// <para>parameter : (ESkillProperty)List, Character</para>
    /// <para>return : void</para>
    /// <para>describe : Add debuff list.</para>
    /// </summary>
    public void AddDebuff(List<ESkillProperty> eSkillPropertyList, Character attacker) {
        if( eSkillPropertyList == null )
            return;

        for( int i = 0; i < eSkillPropertyList.Count; i++ ) {
            SetDeBuff(eSkillPropertyList[i], attacker, i);
        }
    }

    /// <summary>
    /// <para>name : SetDeBuff</para>
    /// <para>parameter : ESkillProperty, Character, int</para>
    /// <para>return : void</para>
    /// <para>describe : Set debuff values by (Character)parameter.</para>
    /// </summary>
    public void SetDeBuff(ESkillProperty skillProperty, Character ownerCharacter, int count) {
        CBuffDebuff buffDebuff = new CBuffDebuff();
        LoadAssetbundle.LoadPrefabComplateCB loadComplateCB;

        switch( skillProperty.type ) {
            case ESkillType.Attack:
                if( skillProperty.GetDuration(ownerCharacter).Equals(0) )
                    return;

                float damage = 0;
                int defenseValue = 0;

                if( skillProperty.baseStatus.Equals(ESkillBaseStatus.Physical) ) {
                    damage = ownerCharacter.attack * (skillProperty.GetValue + ownerCharacter.equipItemDebuffRatio_);
                    defenseValue = defense;
                }

                else if( skillProperty.baseStatus.Equals(ESkillBaseStatus.Magic) ) {
                    damage = ownerCharacter.magicAttack * (skillProperty.GetValue + ownerCharacter.equipItemDebuffRatio_);
                    defenseValue = magicDefense;
                }
                
                damage = damage - ((defenseValue / 2) / 2);

                if( ownerCharacter != null ) {
                    if( CharacterManager.Instance.GetAdvantageCharacterType(ownerCharacter.attributeType).Equals(attributeType) )
                        damage *= (GameManager.Instance.attributeAddDamageRatio_ + ownerCharacter.equipItemAttributeDamageRatio_);
                }

                damage = Mathf.Max(damage, 1.0f);

                buffDebuff = new CBuffDebuff();

                buffDebuff.type = skillProperty.type;
                buffDebuff.time = skillProperty.GetDuration(ownerCharacter);
                buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                buffDebuff.value = -(damage / skillProperty.GetDuration(ownerCharacter));
                buffDebuff.me = this;
                buffDebuff.attacker = ownerCharacter;
                buffDebuff.stringID = skillProperty.stringID;

                if( skillProperty.effectPath != "" ) {
                    loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadTimeEffectCompleteCB);
                    PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, false, buffDebuff);
                }

                else {
                    AddDebuffList(buffDebuff);
                    buffDebuff.StartBuffTime();
                }

                break;

            case ESkillType.Debuff_Stun:
                float probability = 1.0f;
                if( characterTable_.isBoss )
                    return;

                int levelDiff = level - ownerCharacter.level;
                if( levelDiff < 0 )
                    probability += levelDiff * 0.05f;

                if( probability < Random.Range(0, 1.0f) )
                    return;

                buffDebuff = new CBuffDebuff();

                buffDebuff.type = skillProperty.type;
                buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                buffDebuff.value = skillProperty.GetValue;
                buffDebuff.attacker = ownerCharacter;
                buffDebuff.stringID = skillProperty.stringID;

                if( skillProperty.effectPath != "" ) {
                    loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadEffectCompleteCB);
                    PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, false, buffDebuff);
                }

                else {
                    AddDebuffList(buffDebuff);
                }

                BuffDebuffState_ = State.BuffDebuff_Stun;
                SetState(State.BuffDebuff_Stun);

                AddEffectPanel(skillProperty.type, skillProperty.stringID, count);
                break;

            case ESkillType.Debuff_MinusAttackSpeed:
                debuffAttackSpeed_ += skillProperty.GetValue + ownerCharacter.equipItemDebuffRatio_;

                buffDebuff = new CBuffDebuff();

                buffDebuff.type = skillProperty.type;
                buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                buffDebuff.value = skillProperty.GetValue + ownerCharacter.equipItemDebuffRatio_;
                buffDebuff.attacker = ownerCharacter;
                buffDebuff.stringID = skillProperty.stringID;

                if( skillProperty.effectPath != "" ) {
                    loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadEffectCompleteCB);
                    PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, false, buffDebuff);
                }

                else {
                    AddDebuffList(buffDebuff);
                }

                AddEffectPanel(skillProperty.type, skillProperty.stringID, count);
                break;

            case ESkillType.Debuff_MinusMoveSpeed:
                debuffMoveSpeed_ += skillProperty.GetValue + ownerCharacter.equipItemDebuffRatio_;

                buffDebuff = new CBuffDebuff();

                buffDebuff.type = skillProperty.type;
                buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                buffDebuff.value = skillProperty.GetValue + ownerCharacter.equipItemDebuffRatio_;
                buffDebuff.attacker = ownerCharacter;
                buffDebuff.stringID = skillProperty.stringID;

                if( skillProperty.effectPath != "" ) {
                    loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadEffectCompleteCB);
                    PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, false, buffDebuff);
                }

                else {
                    AddDebuffList(buffDebuff);
                }

                AddEffectPanel(skillProperty.type, skillProperty.stringID, count);
                break;

            case ESkillType.Debuff_MinusPhysicalDefense:
            case ESkillType.Debuff_MinusPhysicalAttack:
            case ESkillType.Debuff_MinusMagicDefense:
            case ESkillType.Debuff_MinusMagicAttack:
                buffDebuff = new CBuffDebuff();

                buffDebuff.type = skillProperty.type;
                buffDebuff.endTime = (float)skillProperty.GetDuration(ownerCharacter) + Time.time;
                buffDebuff.value = skillProperty.GetValue + ownerCharacter.equipItemDebuffRatio_;
                buffDebuff.attacker = ownerCharacter;
                buffDebuff.stringID = skillProperty.stringID;

                if( skillProperty.effectPath != "" ) {
                    loadComplateCB = new LoadAssetbundle.LoadPrefabComplateCB(LoadEffectCompleteCB);
                    PrefabManager.Instance.LoadPrefab("", skillProperty.effectPath.ToLower(), System.Guid.Empty, loadComplateCB, false, buffDebuff);
                }

                else {
                    AddDebuffList(buffDebuff);
                }

                AddEffectPanel(skillProperty.type, skillProperty.stringID, count);
                break;
        }

        RefreshStatus();
    }

    /// <summary>
    /// <para>name : AddDebuffList</para>
    /// <para>parameter : CBuffDebuff</para>
    /// <para>return : void</para>
    /// <para>describe : Add debuff list if this object contains debuff list.</para>
    /// </summary>
    void AddDebuffList(CBuffDebuff buff) {
        debuff_.Exists(delegate(CBuffDebuff a) {
            if( a.type.Equals(buff.type) ) {
                a.endTime = 0;
                if( a.CheckOnBuffDebuff )
                    a.StopBuffTime();

                return true;
            }

            else {
                return false;
            }
        });

        debuff_.Add(buff);
    }

    List<CBuffDebuff> removeBuffDebuff_ = new List<CBuffDebuff>();

    /// <summary>
    /// <para>name : BuffProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing buff list objects.</para>
    /// </summary>
    void BuffProcess() {
        if( GameManager.Instance != null && GameManager.Instance.GamePause )
            return;
        if( buff_.Count == 0 )
            return;

        bool isRemoveBuff = false;
        buff_.ForEach(delegate(CBuffDebuff a) {
            if( Time.time > a.endTime ) {
                if( a.effect != null )
                    Destroy(a.effect);

                a.effect = null;
                buff_.Remove(a);

                switch( a.type ) {
                    case ESkillType.Buff_AddAttackSpeed:
                        buffAttackSpeed_ -= a.value;
                        break;

                    case ESkillType.Buff_AddMoveSpeed:
                        buffMoveSpeed_ -= a.value;
                        break;
                }

                isRemoveBuff = true;
            }

            else {
                if( a.effect != null ) {
                    a.effect.transform.position = transform.position;
                    a.effect.transform.rotation = transform.rotation;
                }
            }
        });

        if( isRemoveBuff )
            RefreshStatus();
    }

    /// <summary>
    /// <para>name : ClearDebuff</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Clear all buff list objects.</para>
    /// </summary>
    public void ClearDebuff() {
        for( int i = 0; i < debuff_.Count; i++ ) {
            debuff_[i].endTime = 0;
        }
    }

    /// <summary>
    /// <para>name : DebuffProcess</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Processing debuff list objects.</para>
    /// </summary>
    void DebuffProcess() {
        if( GameManager.Instance != null && GameManager.Instance.GamePause )
            return;
        if( debuff_.Count == 0 )
            return;

        bool isExistMesmerizeState = false;
        bool isRemoveDebuff = false;
        debuff_.ForEach(delegate(CBuffDebuff a) {
            if( Time.time > a.endTime ) {
                if( a.effect != null )
                    Destroy(a.effect);

                a.effect = null;
                debuff_.Remove(a);

                switch( a.type ) {
                    case ESkillType.Debuff_Stun:
                        isExistMesmerizeState = debuff_.Exists(delegate(CBuffDebuff b) {
                            return b.type.Equals(ESkillType.Debuff_Stun);
                        });

                        if( isExistMesmerizeState == false ) {
                            BuffDebuffState_ = State.None;
                            SetStateStay();
                        }

                        break;

                    case ESkillType.Debuff_MinusAttackSpeed:
                        debuffAttackSpeed_ -= a.value;
                        break;

                    case ESkillType.Debuff_MinusMoveSpeed:
                        debuffMoveSpeed_ -= a.value;
                        break;
                }

                isRemoveDebuff = true;
            }

            else {
                if( a.effect != null ) {
                    a.effect.transform.position = transform.position;
                    a.effect.transform.rotation = transform.rotation;
                }
            }
        });

        if( isRemoveDebuff )
            RefreshStatus();
    }

    /// <summary>
    /// <para>name : GetBuffValue</para>
    /// <para>parameter : ESkillType</para>
    /// <para>return : float</para>
    /// <para>describe : Return value if buff list contains same (ESkillType)parameter.</para>
    /// </summary>
    float GetBuffValue(ESkillType skillType) {
        float value = 0;
        for( int i = 0; i < buff_.Count; i++ ) {
            if( buff_[i].type == skillType ) {
                value += buff_[i].value;
            }
        }

        return value;
    }

    /// <summary>
    /// <para>name : GetDebuffValue</para>
    /// <para>parameter : ESkillType</para>
    /// <para>return : float</para>
    /// <para>describe : Return value if debuff list contains same (ESkillType)parameter.</para>
    /// </summary>
    float GetDebuffValue(ESkillType skillType) {
        float value = 0;
        for( int i = 0; i < debuff_.Count; i++ ) {
            if( debuff_[i].type == skillType ) {
                value += debuff_[i].value;
            }
        }

        return value;
    }

    /// <summary>
    /// <para>name : LoadDamageEffectCompleteCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load effect object and Instantiate it.</para>
    /// </summary>
    void LoadDamageEffectCompleteCB(string tableName, GameObject o, System.Guid uid, string name, params object[] param) {
        GameObject effect = Instantiate(o, transform.position, transform.rotation) as GameObject;
        effect.transform.localScale = Vector3.one * characterController_.height * prefabScale_;

        /*
        effect.transform.localScale = Vector3.Scale(effect.transform.localScale, transform.localScale);
        if( characterTable_.boss.Equals(0) == false ) {
            foreach( Transform tf in effect.transform ) {
                tf.localScale = Vector3.Scale(tf.localScale, transform.localScale);
            }
        }
         */
    }

    /// <summary>
    /// <para>name : LoadEffectCompleteCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load effect object and Instantiate it.</para>
    /// </summary>
    void LoadEffectCompleteCB(string tableName, GameObject o, System.Guid uid, string name, params object[] param) {
        GameObject effect = Instantiate(o, transform.position, transform.rotation) as GameObject;
        effect.transform.localScale = Vector3.one * characterController_.height * prefabScale_;

        if( param.Length.Equals(2) == false )
            MagiDebug.LogError("LoadEffect Params error.");

        CBuffDebuff buffDebuff = (CBuffDebuff)param[1];
        buffDebuff.effect = effect;

        if( IsActive() == false )
            buffDebuff.effect.SetActive(false);

        if( (bool)param[0] )
            AddBuffList(buffDebuff);
        else
            AddDebuffList(buffDebuff);
    }

    /// <summary>
    /// <para>name : LoadTimeEffectCompleteCB</para>
    /// <para>parameter : string, GameObject, Guid, string, object[]</para>
    /// <para>return : void</para>
    /// <para>describe : Load effect object and Instantiate it.</para>
    /// </summary>
    void LoadTimeEffectCompleteCB(string tableName, GameObject o, System.Guid uid, string name, params object[] param) {
        GameObject effect = Instantiate(o, transform.position, transform.rotation) as GameObject;
        effect.transform.localScale = Vector3.one * characterController_.height * prefabScale_;

        if( param.Length.Equals(2) == false )
            MagiDebug.LogError("LoadEffect Params error.");

        CBuffDebuff buffDebuff = (CBuffDebuff)param[1];
        buffDebuff.effect = effect;

        if( IsActive() == false )
            buffDebuff.effect.SetActive(false);

        if( (bool)param[0] )
            AddBuffList(buffDebuff);
        else
            AddDebuffList(buffDebuff);

        buffDebuff.StartBuffTime();
    }

    #endregion

    #region Material

    /// <summary>
    /// <para>name : GetMaterial</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Get object materials.</para>
    /// </summary>
    void GetMaterial() {
        Material copyMaterial_;
        MeshRenderer[] meshRender = GetComponentsInChildren<MeshRenderer>();
        for( int i = 0; i < meshRender.Length; i++ ) {
            if( meshRender[i].name.Equals("shadow") )
                continue;

            MeshRenderer mr = meshRender[i];
            for( int count_ = 0; count_ < mr.materials.Length; ++count_ ) {
                materials_.Add(mr.materials[count_]);

                copyMaterial_ = new Material(mr.materials[count_]);
                dMaterialList_.Add(mr.materials[count_].GetHashCode(), copyMaterial_);
            }
        }

        SkinnedMeshRenderer[] skinnedMeshRenderer = GetComponentsInChildren<SkinnedMeshRenderer>();
        for( int i = 0; i < skinnedMeshRenderer.Length; i++ ) {
            SkinnedMeshRenderer smr = skinnedMeshRenderer[i];
            for( int count_ = 0; count_ < smr.materials.Length; ++count_ ) {
                materials_.Add(smr.materials[count_]);

                copyMaterial_ = new Material(smr.materials[count_]);
                dMaterialList_.Add(smr.materials[count_].GetHashCode(), copyMaterial_);
            }
        }

        ToonAlphaTestGlow toonAlphaTestGlow = GetComponent<ToonAlphaTestGlow>();
        if( toonAlphaTestGlow != null ) {
            toonAlphaTestGlow.InitGlowMaterial();
        }
    }

    /// <summary>
    /// <para>name : ChangeMaterial</para>
    /// <para>parameter : Material, bool</para>
    /// <para>return : void</para>
    /// <para>describe : Change materials to (Material)parameter.</para>
    /// </summary>
    void ChangeMaterial(Material material_, bool switch_) { // 바꿀 마테리얼, isChange_가 true면 material_대로 변경, false면 원래대로
        switch( switch_ ) {
            case true: { // 바꿈
                    MeshRenderer[] meshRender = GetComponentsInChildren<MeshRenderer>();
                    for( int i = 0; i < meshRender.Length; i++ ) {
                        if( meshRender[i].name.Equals("shadow") )
                            continue;

                        MeshRenderer mr = meshRender[i];
                        for( int count_ = 0; count_ < mr.materials.Length; ++count_ ) {
                            mr.materials[count_].CopyPropertiesFromMaterial(material_);
                        }
                    }

                    SkinnedMeshRenderer[] skinnedMeshRenderer = GetComponentsInChildren<SkinnedMeshRenderer>();
                    for( int i = 0; i < skinnedMeshRenderer.Length; i++ ) {
                        SkinnedMeshRenderer smr = skinnedMeshRenderer[i];
                        for( int count_ = 0; count_ < smr.materials.Length; ++count_ ) {
                            smr.materials[count_].CopyPropertiesFromMaterial(material_);
                        }
                    }
                }

                break;

            case false: { // 원래대로
                    MeshRenderer[] meshRender = GetComponentsInChildren<MeshRenderer>();
                    for( int i = 0; i < meshRender.Length; i++ ) {
                        if( meshRender[i].name.Equals("shadow") )
                            continue;

                        MeshRenderer mr = meshRender[i];
                        for( int count_ = 0; count_ < mr.materials.Length; ++count_ ) {
                            Material cMaterial_;
                            if( dMaterialList_.TryGetValue(mr.materials[count_].GetHashCode(), out cMaterial_) )
                                mr.materials[count_].CopyPropertiesFromMaterial(cMaterial_);
                        }
                    }

                    SkinnedMeshRenderer[] skinnedMeshRenderer = GetComponentsInChildren<SkinnedMeshRenderer>();
                    for( int i = 0; i < skinnedMeshRenderer.Length; i++ ) {
                        SkinnedMeshRenderer smr = skinnedMeshRenderer[i];
                        for( int count_ = 0; count_ < smr.materials.Length; ++count_ ) {
                            Material cMaterial_;
                            if( dMaterialList_.TryGetValue(smr.materials[count_].GetHashCode(), out cMaterial_) )
                                smr.materials[count_].CopyPropertiesFromMaterial(cMaterial_);
                        }
                    }
                }

                break;
        }
    }

    #endregion

    #region Layer

    /// <summary>
    /// <para>name : WaitChangeLayer</para>
    /// <para>parameter : int</para>
    /// <para>return : yield</para>
    /// <para>describe : Change transform layer to layer (int)index.</para>
    /// </summary>
    public IEnumerator WaitChangeLayer(int layerIndex) {
        while( isInitCharacter ) {
            yield return null;
        }

        Transform[] tran = GetComponentsInChildren<Transform>();
        foreach( Transform tranForm in tran ) {
            tranForm.gameObject.layer = layerIndex;
        }
    }

    #endregion

    /// <summary>
    /// <para>name : Death</para>
    /// <para>parameter : SubState</para>
    /// <para>return : void</para>
    /// <para>describe : Character state to death state.</para>
    /// </summary>
    public void Death(SubState subState = SubState.Init) {
        hp_ = 0;

        if (DungeonTestManager.Instance != null && CharacterManager.Instance.CheckTeamType(CharacterTeam.Type_Player) == false )
            DungeonTestManager.Instance.KillCount++;

        if( m_HpBar_Panel != null ) {
            m_HpBar_Panel.SetPlayerHP_Bar(0, maxHP_);
            m_HpBar_Panel.DestroyMe();
        }

        if( characterController != null )
            characterController.enabled = false;
        if( m_Elit_Effect != null )
            m_Elit_Effect.SetActive(false);

        if( CharacterManager.Instance.CheckTeamType(teamType) ) {
            if( CharacterManager.Instance.ActiveEnemy != null &&
                CharacterManager.Instance.ActiveEnemy.EnemyPresent != null &&
                CharacterManager.Instance.ActiveEnemy.EnemyPresent.Equals(this) )
                CharacterManager.Instance.ActiveEnemy.EnemyPresent = null;
        }

        else {
            if( CharacterManager.Instance.ActivePlayer != null &&
                CharacterManager.Instance.ActivePlayer.EnemyPresent != null &&
                CharacterManager.Instance.ActivePlayer.EnemyPresent.Equals(this) )
                CharacterManager.Instance.ActivePlayer.EnemyPresent = null;

            if( GameManager.Instance.GetState().Equals(GameManager.State.Fight) ) {
                switch( StageManager.Instance.stageTable.category ) {
                    case StageTableManager.Category.TUTORIAL:
                    case StageTableManager.Category.NORMAL:
                    case StageTableManager.Category.INFINITE:
                    case StageTableManager.Category.BOSS_ATTACK:
                        DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.AddPowerUpCount(ChargeCount);
                        break;

                    case StageTableManager.Category.TIME_ATTACK:
                        DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.AddPowerUpCount(ChargeCount);
                        DungeonSceneScriptManager.Instance.m_TimeAttackEvent.CurrentDropGold += DropGold;
                        break;
                }
            }
        }

        SetState(Character.State.Death, (int)subState);
    }

    ClientDataManager.Mon_Info teamInfo_;
    public ClientDataManager.Mon_Info GetCharacterInfo {
        get {
            return teamInfo_;
        }
    }

    public float radius {
        get {
            return radius_;
        }
    }

    #region FindEnemy

    /// <summary>
    /// <para>name : FindEnemy</para>
    /// <para>parameter : out (Character)List, float</para>
    /// <para>return : bool</para>
    /// <para>describe : Find enemy team nearby character.</para>
    /// </summary>
    public bool FindEnemy(out List<Character> enemyList, float range) {
        enemyList = null;
        if( CharacterManager.Instance == null )
            return false;
        return CharacterManager.Instance.FindCharacterInRange(out enemyList, teamType, this, range);
    }

    /// <summary>
    /// <para>name : FindRangeAndAngle</para>
    /// <para>parameter : out Character, float, float</para>
    /// <para>return : bool</para>
    /// <para>describe : Find enemy object nearby character.</para>
    /// </summary>
    public bool FindRangeAndAngle(out Character resultEnemy, float range, float dot) {
        resultEnemy = null;

        Vector2 charPosition = new Vector2(transform.position.x, transform.position.z);
        Vector2 moveTarget = new Vector2(MovementTarget.x, MovementTarget.z);

        if( EnemyPresent != null &&
            CharacterManager.Instance.CheckTeamType(teamType, EnemyPresent.teamType) )
            EnemyPresent = null;

        if( EnemyPresent != null && EnemyPresent.CheckCharacterAlive ) {
            var enemyPresent = EnemyPresent;
            Vector2 enemyChar = new Vector2(enemyPresent.transform.position.x, enemyPresent.transform.position.z);
            Vector2 atTargetChar = (enemyChar - charPosition).normalized;
            Vector2 atMoveTarget = (moveTarget - charPosition).normalized;
            float dot2 = Vector2.Dot(atTargetChar, atMoveTarget);
            if( dot2 < dot ) {
                if( CharacterManager.Instance.GetAttackRangeDistance(this, enemyPresent) < range ) {
                    resultEnemy = enemyPresent;

                    return true;
                }
            }
        }

        var enemyList = new List<Character>();
        if( FindEnemy(out enemyList, range) == false ) {
            return false;
        }

        float minLength = 10000.0f;
        Character minLengthEnemy = null;
        for( int i = 0; i < enemyList.Count; i++ ) {
            switch( enemyList[i].state ) {
                case Character.State.Respawn:
                case Character.State.Death:
                    continue;
            }

            if( MovementTargetEnable == true ) {
                Vector2 targetChar = new Vector2(enemyList[i].transform.position.x, enemyList[i].transform.position.z);

                Vector2 atTargetChar = (targetChar - charPosition).normalized;
                Vector2 atMoveTarget = (moveTarget - charPosition).normalized;
                float dot2 = Vector2.Dot(atTargetChar, atMoveTarget);
                if( dot2 < dot ) {
                    continue;
                }
            }

            float length = CharacterManager.Instance.GetAttackRangeDistance(this, enemyList[i]);

            if( length >= range )
                continue;
            if( length < minLength ) {
                minLength = length;
                minLengthEnemy = enemyList[i];
            }
        }

        if( minLengthEnemy == null )
            return false;

        resultEnemy = minLengthEnemy;

        return true;
    }

    #endregion

    #region Panel

    /// <summary>
    /// <para>name : AddSkillNamePanel</para>
    /// <para>parameter : int</para>
    /// <para>return : void</para>
    /// <para>describe : Add skill name panel on DungeonPlay UI.</para>
    /// </summary>
    public void AddSkillNamePanel(int skillStringID) {
        if( IsActive() == false )
            return;
        if( CharacterManager.Instance.uiSkillText == null )
            return;

        string skillName = MagiStringUtil.GetString(skillStringID);
        DungeonSceneScriptManager.Instance.OnSkillName(skillName, teamInfo_.nMonID);
    }

    /// <summary>
    /// <para>name : AddTextPanel</para>
    /// <para>parameter : string</para>
    /// <para>return : void</para>
    /// <para>describe : Add text panel on DungeonPlay UI. - avoid effect.</para>
    /// </summary>
    public void AddTextPanel(string text) {
        if( IsActive() == false )
            return;
        if( CharacterManager.Instance.uiEffectText == null )
            return;

        m_Effect_Panel = Instantiate(CharacterManager.Instance.uiEffectText) as CharacterEffectText;

        m_Effect_Panel.Init(text, m_Effect_Target, m_WorldCamera, m_GuiCamera);
        m_Effect_Panel.FollowTarget_X = true;

        Vector3 size = m_Effect_Panel.transform.localScale;
        m_Effect_Panel.SpawnAt(m_Effect_Target, size, m_Parant_Panel, characterController_.height * scale_);

        m_Effect_Panel.SetFirstFollowing();

        TweenPosition twPos = m_Effect_Panel.TweenPosition;
        twPos.tweenGroup = sessionID_;
        twPos.duration = 1.0f;
        twPos.from = m_Effect_Panel.transform.localPosition;
        twPos.to = twPos.from;
        twPos.to.y += 100.0f;
        twPos.onFinished.Add(new EventDelegate(m_Effect_Panel, "DestroyMe"));

        TweenScale twScale = m_Effect_Panel.TweenScale;
        twScale.tweenGroup = sessionID_;
        twScale.from = new Vector3(0, 0, 1);
        twScale.to = new Vector3(1, 1, 1);
        twScale.duration = 0.1f;

        m_nEffectPanelCount++;
    }

    /// <summary>
    /// <para>name : AddEffectPanel</para>
    /// <para>parameter : ESkillType, int, int int</para>
    /// <para>return : void</para>
    /// <para>describe : Add text panel on DungeonPlay UI. - skill effect.</para>
    /// </summary>
    public void AddEffectPanel(ESkillType type, int nEffectID, int count, int nEffectValue = 0) {
        if( IsActive() == false )
            return;

        if( CharacterManager.Instance.uiEffectText == null )
            return;
        if( nEffectID.Equals(0) )
            return;

        string effectName = MagiStringUtil.GetReplaceString(nEffectID, "#@Value@#", string.Format("{0:n0}", nEffectValue));
        float upperPos = count * 45.0f;

        m_Effect_Panel = Instantiate(CharacterManager.Instance.uiEffectText) as CharacterEffectText;

        m_Effect_Panel.Init(effectName, m_Effect_Target, m_WorldCamera, m_GuiCamera);
        m_Effect_Panel.FollowTarget_X = true;

        Vector3 size = m_Effect_Panel.transform.localScale;
        m_Effect_Panel.SpawnAt(m_Effect_Target, size, m_Parant_Panel, characterController_.height * scale_);

        m_Effect_Panel.SetFirstFollowing();

        TweenPosition twPos = m_Effect_Panel.TweenPosition;
        twPos.tweenGroup = sessionID_;
        twPos.duration = 1.0f;
        twPos.from = m_Effect_Panel.transform.localPosition;
        twPos.from.y -= 50.0f + upperPos;
        twPos.to = twPos.from;
        twPos.to.y += 100.0f + upperPos;
        twPos.onFinished.Add(new EventDelegate(m_Effect_Panel, "DestroyMe"));

        TweenScale twScale = m_Effect_Panel.TweenScale;
        twScale.tweenGroup = sessionID_;
        twScale.from = new Vector3(0, 0, 1);
        twScale.to = new Vector3(1, 1, 1);
        twScale.duration = 0.1f;

        m_nEffectPanelCount++;
    }

    #endregion

    void OnDestroy() {
        shadowTransform_ = null;
        characterController_ = null;
        characterTarget_ = null;
        enemyPresent_ = null;
        agent_ = null;
        model_ = null;
        animator_ = null;
        characterTable_ = null;
        combo_ = null;
        selectedCombo_ = null;
        selectedComboSkill_ = null;
        attackTable_ = null;
        partsTable_ = null;
        skillTable_ = null;
        characterJump_ = null;

        m_Elit_Effect = null;
        m_DamageText_Target = null;
        m_DamageText_Prefab = null;
        m_HpBar_Target = null;
        m_HpBar_Prefab = null;
        m_Info_Target = null;
        m_Effect_Target = null;
        m_Parant_Panel = null;
        m_HpBar_Panel = null;
        m_Effect_Panel = null;
        m_WorldCamera = null;
        m_GuiCamera = null;
        materials_ = null;
        dMaterialList_ = null;
        weaponList_ = null;
        damageEffect_ = null;
        damagedHitID_ = null;
        damageParam_ = null;
        skillCoolTime_ = null;
        useSkillCombo_ = null;
        buff_ = null;
        debuff_ = null;
        removeBuffDebuff_ = null;
    }
}
