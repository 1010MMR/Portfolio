using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Spine;

public class EnemyStatus {
    public Enemy enemy = null;
    public EnemyTable.TableRow table = null;

    public int floorIndex = 0;

    public int curHp = 0;
    public int maxHp = 0;

    public eProcessType processType = eProcessType.Type_None;
    public eProcessType prevProcessType = eProcessType.Type_None;

    private List<DebuffParam> debuffList = null;

    public EnemyStatus() {
    }

    public EnemyStatus(Enemy enemy, EnemyTable.TableRow table) {
        this.enemy = enemy;
        this.table = table;
    }

    public EnemyStatus(Enemy enemy, EnemyTable.TableRow table, int floorIndex) {
        this.enemy = enemy;
        this.table = table;
        this.floorIndex = floorIndex;

        InitHP();

        debuffList = new List<DebuffParam>();
    }

    #region State

    public void SetState(eProcessType type) {
        prevProcessType = processType;
        processType = type;
    }

    #endregion

    #region HP

    private void InitHP() {
        int enemyLevel = StageManager.Instance.GetWaveTable.waveLevel + 
            (GameManager.Instance.GetGameStatus.repeatGameCount * StageManager.RESTART_WAVE_VALUE);

        this.curHp = table.hp + (table.upgradeHp * enemyLevel);
        this.maxHp = this.curHp;
    }

    public bool SetHP(int addHP) {
        return !(curHp = Mathf.Clamp(curHp + addHP, 0, maxHp)).Equals(0);
    }

    #endregion

    #region Move_Speed

    private const float MOVE_SPEED_FACTOR = 0.005f;

    public float GetMoveSpeed {
        get {
            float moveSpeed = table.moveSpeed * MOVE_SPEED_FACTOR;
            float moveDebuffValue = 0;

            for( int i = 0; i < debuffList.Count; i++ ) {
                if( debuffList[i].debuffType.Equals(eDebuffType.Type_Slow) )
                    moveDebuffValue += debuffList[i].debuffValue;
            }

            return Mathf.Clamp(moveSpeed - (moveSpeed * moveDebuffValue), 0, moveSpeed);
        }
    }

    #endregion

    #region Attack_Range

    public float GetAttackRange {
        get {
            return table.attackRange;
        }
    }

    #endregion

    #region Debuff

    public bool AddDebuff(DebuffParam param) {
        int listCount = debuffList.Count;
        int index = debuffList.FindIndex(delegate(DebuffParam a) {
            return a.debuffType.Equals(param.debuffType) &&
                (Mathf.Abs(a.debuffValue) < Mathf.Abs(param.debuffValue));
        });

        if( index > -1 ) {
            debuffList.RemoveAt(index);
            debuffList.Add(param);
        }
        else if( listCount.Equals(0) )
            debuffList.Add(param);
        else
            return false;

        return listCount.Equals(0);
    }

    public bool RemoveDebuff() {
        if( debuffList.Count.Equals(0) )
            return true;
        else {
            List<DebuffParam> debuffTimerList = new List<DebuffParam>(debuffList);
            for( int i = 0; i < debuffTimerList.Count; i++ ) {
                if( debuffTimerList[i].debuffDuration <= Time.realtimeSinceStartup )
                    debuffTimerList.RemoveAt(i);
                else {
                    if( debuffTimerList[i].debuffType.Equals(eDebuffType.Type_DOT) &&
                        debuffTimerList[i].nextEffectTime <= Time.realtimeSinceStartup ) {
                        debuffTimerList[i].nextEffectTime += debuffTimerList[i].debuffPerTime;

                        if( enemy.CheckDeath == false )
                            enemy.OnDamage(Mathf.RoundToInt(debuffTimerList[i].debuffValue), false, false);
                    }
                }
            }

            debuffList = debuffTimerList;
            return debuffList.Count.Equals(0);
        }
    }

    #endregion

    ~EnemyStatus() {
        table = null;
        debuffList = null;
    }
}

public class Enemy : MonoBehaviour {
    public SkeletonAnimation skeletonAnim = null;
    public Transform effectDummy = null;
    public Transform damageDummy = null;

    public string[] motionNameArray = null;

    private EnemyStatus enemyStatus = null;

    void Start() {
        InitSpineAnimation();
    }

    void Update() {
        Process();
    }

    #region Init

    public void Init(EnemyTable.TableRow enemyTable, int floorIndex) {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        enemyStatus = new EnemyStatus(this, enemyTable, floorIndex);

        transform.localPosition = GameManager.Instance.GetEnemyStartPos(floorIndex);
        skeletonAnim.gameObject.transform.localPosition = Vector3.forward * floorIndex;

        SetState(eProcessType.Type_Move);
    }

    public void Preload(EnemyTable.TableRow enemyTable) {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        enemyStatus = new EnemyStatus(this, enemyTable);
        if( PoolManager.Instance != null )
            PoolManager.Instance.AddEnemyPool(this);
    }

    public EnemyStatus GetStatus {
        get {
            return enemyStatus;
        }
    }

    #endregion

    #region Process

    private void Process() {
        if( enemyStatus != null ) {
            switch( enemyStatus.processType ) {
                case eProcessType.Type_None:
                    break;

                case eProcessType.Type_Move:
                    MoveProcess();
                    break;

                case eProcessType.Type_Attack:
                    AttackProcess();
                    break;

                case eProcessType.Type_Damage:
                    DamageProcess();
                    break;

                case eProcessType.Type_Death:
                    DeathProcess();
                    break;
            }
        }
    }

    #endregion

    #region Move

    private void MoveProcess() {
        if( transform.localPosition.x <=
            GameManager.Instance.GetEnemyEndXPos(enemyStatus.floorIndex) + enemyStatus.GetAttackRange )
            SetState(eProcessType.Type_Attack);
        else
            transform.position += Vector3.left * Time.deltaTime * enemyStatus.GetMoveSpeed;
    }

    #endregion

    #region Attack

    private const float CREATE_ATTACK_EFFECT_FACTOR = 0.5f;

    private void AttackProcess() {
    }

    private void Attack() {
        if( GameManager.Instance != null ) {
            float randomValue = Random.Range(0.0f, 1.0f);
            if( randomValue >= CREATE_ATTACK_EFFECT_FACTOR )
                AddFragmentEffect();

            GameManager.Instance.AddBaseDamage(-enemyStatus.table.attackDamage);
        }
    }

    #endregion

    #region Damage

    public void OnDamage(int damage, bool isCritical, bool isDamageMotion = true) {
        bool isDeath = !enemyStatus.SetHP(-damage);
        SetState(isDeath ? eProcessType.Type_Death : 
            isDamageMotion ? eProcessType.Type_Damage : enemyStatus.processType);

        if( isDeath ) {
            AddGoldEffect();

            if( GameManager.Instance != null ) {
                GameManager.Instance.GetGameStatus.AddKillCount();

                int goldValue = enemyStatus.table.goldValue;
                int addGoldValue = Mathf.RoundToInt(((float)StageManager.Instance.GetWaveTable.waveLevel / 10.0f) * enemyStatus.table.addGoldValue);

                GameManager.Instance.GetGameStatus.AddGold(goldValue + addGoldValue);
            }

            if( SoundManager.Instance != null )
                SoundManager.Instance.PlayAudioClip(enemyStatus.table.soundPathArray[(int)eEnemySoundType.Type_Death]);
        }

        else {
            if( SoundManager.Instance != null )
                SoundManager.Instance.PlayAudioClip(enemyStatus.table.soundPathArray[(int)eEnemySoundType.Type_Damage]);
        }

        if( isCritical ) {
            AddCriticalEffect();
            if( SoundManager.Instance != null )
                SoundManager.Instance.PlayAudioClip("Sound/InGame/e_critical_hit");
        }

        SetDamageText(damage, isCritical ? 
            eDamageTextType.Type_EnemyDamageCritical : eDamageTextType.Type_EnemyDamage);
    }

    private void DamageProcess() {
    }

    private void SetDamageText(int damage, eDamageTextType type) {
        UIDamageText damageText = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetDamageTextFromPool(type, out damageText) )
            damageText.Init(damageDummy != null ? damageDummy : transform, damage);
        else {
            LoadAssetbundle.LoadPrefabCB loadDamageTextPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadDamageTextCompleteCB);
            PrefabManager.Instance.LoadPrefab(GameManager.Instance.GetDamagePath(type), System.Guid.NewGuid(), loadDamageTextPrefabCB, damage);
        }
    }

    private void LoadDamageTextCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootArray[(int)GameManager.eRootType.Type_Damage].transform;
            createObj.transform.localScale = gameObj.transform.localScale;

            UIDamageText damageText = createObj.GetComponent<UIDamageText>();
            if( damageText != null )
                damageText.Init(damageDummy != null ? damageDummy : transform, (int)param[0]);
        }
    }

    #endregion

    #region Death

    public bool CheckDeath {
        get {
            return enemyStatus.processType.Equals(eProcessType.Type_Death);
        }
    }

    public void OnDeath() {
        if( CheckDeath == false )
            SetState(eProcessType.Type_Death);
    }

    private void DeathProcess() {
    }

    #endregion

    #region State

    private void SetState(eProcessType type) {
        if( enemyStatus != null ) {
            switch( type ) {
                case eProcessType.Type_Move:
                    SetAnimation(type, motionNameArray[(int)type], true);
                    break;
                case eProcessType.Type_Attack:
                    SetAnimation(type, motionNameArray[(int)type], true);
                    break;
                case eProcessType.Type_Damage:
                    SetAnimation(type, motionNameArray[(int)type], false);
                    break;
                case eProcessType.Type_Death:
                    SetAnimation(type, motionNameArray[(int)type], false);
                    break;
            }

            enemyStatus.SetState(type);
        }
    }

    #endregion

    #region Animation

    private void InitSpineAnimation() {
        if( skeletonAnim != null ) {
            skeletonAnim.state.Start += MotionStartCB;
            skeletonAnim.state.End += MotionEndCB;
            skeletonAnim.state.Complete += MotionCompleteCB;
        }
    }

    public void SetAnimation(eProcessType type, string name, bool isLoop) {
        if( skeletonAnim != null ) {
            skeletonAnim.state.ClearTracks();
            skeletonAnim.state.SetAnimation((int)type, name, isLoop);
        }
    }

    private void MotionStartCB(Spine.AnimationState state, int trackIndex) {
        switch( (eProcessType)trackIndex ) {
            case eProcessType.Type_Attack:
                if( SoundManager.Instance != null )
                    SoundManager.Instance.PlayAudioClip(enemyStatus.table.soundPathArray[(int)eEnemySoundType.Type_Attack]);
                break;
        }
    }

    private void MotionEndCB(Spine.AnimationState state, int trackIndex) {
        if( enemyStatus.prevProcessType.Equals(eProcessType.Type_None) == false &&
            enemyStatus.prevProcessType.Equals(enemyStatus.processType) == false ) {
            switch( enemyStatus.processType ) {
                case eProcessType.Type_Damage:
                    eProcessType prevType = enemyStatus.prevProcessType;
                    enemyStatus.processType = eProcessType.Type_None;

                    SetState(prevType);
                    break;

                case eProcessType.Type_Death:
                    OnDeath();
                    if( PoolManager.Instance != null )
                        PoolManager.Instance.AddEnemyPool(this);
                    break;
            }
        }
    }

    private void MotionCompleteCB(Spine.AnimationState state, int trackIndex, int loopCount) {
        switch( (eProcessType)trackIndex ) {
            case eProcessType.Type_Attack:
                switch( enemyStatus.table.attackType ) {
                    case eEnemyAttackType.Type_Range:
                        SetShot();
                        break;
                    case eEnemyAttackType.Type_Suicide:
                        Attack();
                        if( PoolManager.Instance != null )
                            PoolManager.Instance.AddEnemyPool(this);
                        break;
                    case eEnemyAttackType.Type_Slow:
                        break;
                    default:
                        if( SoundManager.Instance != null )
                            SoundManager.Instance.PlayAudioClip(enemyStatus.table.soundPathArray[(int)eEnemySoundType.Type_Attack]);
                        Attack();
                        break;
                }

                break;
        }
    }

    #endregion

    #region Shot

    private void SetShot() {
        int hashID = Animator.StringToHash(enemyStatus.table.attackEffectPath);
        Vector3 targetPos = GameManager.Instance.GetEnemyEndPos(enemyStatus.floorIndex);

        EnemyShot shot = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEnemyShotFromPool(hashID, out shot) )
            shot.Init(hashID, enemyStatus.table, effectDummy.position, targetPos);
        else {
            LoadAssetbundle.LoadPrefabCB loadPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadShotCompleteCB);
            PrefabManager.Instance.LoadPrefab(enemyStatus.table.attackEffectPath, System.Guid.NewGuid(),
                loadPrefabCB, hashID, enemyStatus.table, effectDummy.position, targetPos);
        }
    }

    private void LoadShotCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.
                rootArray[(int)GameManager.eRootType.Type_Object].transform;
            createObj.transform.position = effectDummy.position;
            createObj.transform.localScale = gameObj.transform.localScale;

            EnemyShot shot = createObj.GetComponent<EnemyShot>();
            if( shot != null )
                shot.Init((int)param[0], (EnemyTable.TableRow)param[1], (Vector3)param[2], (Vector3)param[3]);
        }
    }

    #endregion

    #region Effect

    private void AddGoldEffect() {
        SkeletonEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(eEffectType.Type_DropGold, out effect) )
            effect.Init(damageDummy != null ? damageDummy : transform);
        else {
            LoadAssetbundle.LoadPrefabCB loadEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadEffectCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Gold_Effect", System.Guid.NewGuid(), loadEffectPrefabCB);
        }
    }

    private void AddCriticalEffect() {
        SkeletonEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(eEffectType.Type_Critical, out effect) )
            effect.Init(damageDummy != null ? damageDummy : transform);
        else {
            LoadAssetbundle.LoadPrefabCB loadEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadEffectCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Critical_Effect", System.Guid.NewGuid(), loadEffectPrefabCB);
        }
    }

    private void AddFragmentEffect() {
        SkeletonEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(eEffectType.Type_Fragment, out effect) )
            effect.Init(damageDummy != null ? damageDummy : transform);
        else {
            LoadAssetbundle.LoadPrefabCB loadEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadEffectCompleteCB);
            PrefabManager.Instance.LoadPrefab(GameManager.Instance.GetCastle.fragmentEffectPath, System.Guid.NewGuid(), loadEffectPrefabCB);
        }
    }

    private void LoadEffectCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootArray[(int)GameManager.eRootType.Type_Object].transform;
            createObj.transform.localScale = gameObj.transform.localScale;

            SkeletonEffect effect = createObj.GetComponent<SkeletonEffect>();
            if( effect != null )
                effect.Init(damageDummy != null ? damageDummy : transform);
        }
    }

    #endregion

    #region Debuff

    public void AddDebuff(DebuffParam param) {
        bool addDebuff = enemyStatus.AddDebuff(param);
        if( addDebuff )
            StartCoroutine("DebuffTimer");
    }

    private IEnumerator DebuffTimer() {
        while( true ) {
            if( enemyStatus.RemoveDebuff() )
                yield break;

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    #endregion

    public void SetActive(bool isSwitch) {
        StopAllCoroutines();
        gameObject.SetActive(isSwitch);
    }

    void OnDestroy() {
        skeletonAnim = null;
        effectDummy = null;
        damageDummy = null;
        motionNameArray = null;

        enemyStatus = null;
    }
}
