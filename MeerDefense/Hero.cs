using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HeroStatus {
    private Hero hero = null;
    private Transform heroObj = null;

    private int id = 0;

    private int floorIndex = 0;

    private int level = 0;
    private int skillLevel = 0;

    private int curDamage = 0;

    private float attackSpeed = 0;
    private float attackRange = 0;

    private int moveSpeed = 0;

    private HeroTable.TableRow table = null;
    private List<HeroAddValueTable.TableRow> addValueTableList = null;
    private EffectTable.TableRow effectTable = null;

    private List<BuffParam> buffList = null;

    private Enemy curTargetEnemy = null;

    public HeroStatus() {
    }

    public HeroStatus(Hero hero, int floorIndex, HeroBaseInfo info) {
        this.id = info.id;

        this.hero = hero;
        this.heroObj = hero.transform;

        this.table = info.GetTable();
        this.floorIndex = floorIndex;

        this.level = info.level;
        this.skillLevel = info.skillLevel;

        this.attackSpeed = table.attackSpeed;
        this.attackRange = table.attackRange;
        this.moveSpeed = table.moveSpeed;

        this.buffList = new List<BuffParam>();

        if( HeroAddValueTable.Instance != null &&
            HeroAddValueTable.Instance.FindTable(this.table.archiType, out addValueTableList) ) {
            this.curDamage = table.damage +
                addValueTableList[Mathf.Clamp(info.level, 0, addValueTableList.Count - 1)].addDamageValue;
        }

        if( EffectTable.Instance != null &&
            EffectTable.Instance.FindTable(info.GetTable().attackEffectID, out effectTable) == false )
            SupportDebug.LogError(string.Format("EffectTable not found. {0}", info.GetTable().attackEffectID));

        curTargetEnemy = null;
    }

    #region ID

    public int GetID {
        get {
            return id;
        }
    }

    #endregion

    #region Table

    public HeroTable.TableRow GetTable {
        get {
            return table;
        }
    }

    public EffectTable.TableRow GetEffectTable {
        get {
            return effectTable;
        }
    }

    #endregion

    #region Level

    public int GetLevel {
        get {
            return level;
        }
    }

    public int GetSkillLevel {
        get {
            return skillLevel;
        }
    }

    #endregion

    #region Damage

    public int GetDamage {
        get {
            float addValue = 0;
            for( int i = 0; i < buffList.Count; i++ ) {
                if( buffList[i].buffType.Equals(eBuffType.Type_Attack) )
                    addValue += buffList[i].buffValue;
            }

            return Mathf.RoundToInt(curDamage + (curDamage * addValue));
        }
    }

    #endregion

    #region Floor

    public int GetFloorIndex {
        get {
            return floorIndex;
        }
    }

    #endregion

    #region Attack_Speed

    public float GetAttackSpeed {
        get {
            float addValue = 0;
            for( int i = 0; i < buffList.Count; i++ ) {
                if( buffList[i].buffType.Equals(eBuffType.Type_Attack_Speed) )
                    addValue += buffList[i].buffValue;
            }

            return attackSpeed / (addValue.Equals(0) ? 1.0f : addValue);
        }
    }

    #endregion

    #region Attack_Range

    public float GetAttackRange {
        get {
            float addValue = 0;
            for( int i = 0; i < buffList.Count; i++ ) {
                if( buffList[i].buffType.Equals(eBuffType.Type_Attack_Range) )
                    addValue += buffList[i].buffValue;
            }

            return attackRange + attackRange * addValue;
        }
    }

    #endregion

    #region Move_Speed

    public int GetMoveSpeed {
        get {
            float addValue = 0;
            for( int i = 0; i < buffList.Count; i++ ) {
                if( buffList[i].buffType.Equals(eBuffType.Type_Move_Speed) )
                    addValue += buffList[i].buffValue;
            }

            return Mathf.Clamp(Mathf.RoundToInt(moveSpeed + (moveSpeed * addValue)), 0, moveSpeed);
        }
    }

    #endregion

    #region Target

    public eHeroProcessType GetEnemyNormalType(out Enemy target) {
        target = null;

        if( curTargetEnemy != null ) {
            if( curTargetEnemy.GetStatus.GetHP.Equals(0) == false && 
                curTargetEnemy.GetStatus.GetFloorIndex.Equals(floorIndex) ) {
                target = curTargetEnemy;
                SetRotation(target.transform);

                if( EnemyInRange(curTargetEnemy) )
                    return eHeroProcessType.Type_Attack;
                else
                    return eHeroProcessType.Type_Move;
            }
        }

        List<Enemy> enemyList = null;
        if( GameManager.Instance != null && GameManager.Instance.GetEnemyList(floorIndex, out enemyList) ) {
            float range = 0;
            float minRange = float.MaxValue;

            for( int i = 0; i < enemyList.Count; i++ ) {
                range = Mathf.Abs(heroObj.localPosition.x - enemyList[i].gameObject.transform.localPosition.x);
                if( minRange > range ) {
                    minRange = range;
                    target = enemyList[i];
                }
            }

            if( target != null ) {
                curTargetEnemy = target;
                SetRotation(target.transform);

                if( EnemyInRange(target) )
                    return eHeroProcessType.Type_Attack;
                else
                    return eHeroProcessType.Type_Move;
            }
        }

        return eHeroProcessType.Type_Idle;
    }

    public eHeroProcessType GetEnemyPiercingType(out List<Enemy> targetList) {
        targetList = null;

        List<Enemy> enemyList = null;
        if( GameManager.Instance != null && GameManager.Instance.GetEnemyList(floorIndex, out enemyList) ) {
            float leftRangeX = heroObj.localPosition.x - attackRange * 0.5f;
            float rightRangeX = heroObj.localPosition.x + attackRange * 0.5f;
            float targetPosX = 0;

            float range = 0;
            float minRange = float.MaxValue;

            Enemy target = null;

            List<Enemy> leftEnemyList = new List<Enemy>();
            List<Enemy> rightEnemyList = new List<Enemy>();

            for( int i = 0; i < enemyList.Count; i++ ) {
                range = Mathf.Abs(heroObj.localPosition.x - enemyList[i].gameObject.transform.localPosition.x);
                targetPosX = enemyList[i].gameObject.transform.localPosition.x;

                if( targetPosX >= leftRangeX && targetPosX <= heroObj.localPosition.x )
                    leftEnemyList.Add(enemyList[i]);
                else if( targetPosX <= rightRangeX && targetPosX >= heroObj.localPosition.x )
                    rightEnemyList.Add(enemyList[i]);

                if( minRange > range ) {
                    minRange = range;
                    target = enemyList[i];
                }
            }

            targetList = new List<Enemy>();
            if( leftEnemyList.Count.Equals(0) && rightEnemyList.Count.Equals(0) ) {
                targetList.Add(target);
                SetRotation(target.transform);

                return eHeroProcessType.Type_Move;
            }

            else {
                if( leftEnemyList.Count > rightEnemyList.Count )
                    targetList.AddRange(leftEnemyList);
                else
                    targetList.AddRange(rightEnemyList);

                SetRotation(targetList[targetList.Count - 1].transform);

                return eHeroProcessType.Type_Attack;
            }
        }

        return eHeroProcessType.Type_Idle;
    }

    public eHeroProcessType GetEnemyExplosionType(out Enemy target) {
        target = null;

        List<Enemy> enemyList = null;
        if( GameManager.Instance != null && GameManager.Instance.GetEnemyList(floorIndex, out enemyList) ) {
            float range = 0;
            float maxRange = 0;

            if( curTargetEnemy != null && curTargetEnemy.GetStatus.GetHP.Equals(0) == false && 
                curTargetEnemy.GetStatus.GetFloorIndex.Equals(floorIndex) )
                target = curTargetEnemy;
            else {
                curTargetEnemy = null;

                for( int i = 0; i < enemyList.Count; i++ ) {
                    range = Mathf.Abs(heroObj.localPosition.x - enemyList[i].gameObject.transform.localPosition.x);
                    if( range <= attackRange * 0.5f && maxRange < range ) {
                        maxRange = range;
                        target = enemyList[i];
                    }
                }
            }

            if( target != null ) {
                curTargetEnemy = target;
                SetRotation(target.transform);

                if( EnemyInRange(target) )
                    return eHeroProcessType.Type_Attack;
                else
                    return eHeroProcessType.Type_Move;
            }
        }

        return eHeroProcessType.Type_Idle;
    }

    private void SetRotation(Transform target) {
        heroObj.localRotation = (heroObj.localPosition.x - target.localPosition.x) > 0 ?
            Quaternion.Euler(Vector3.up * -180.0f) : Quaternion.Euler(Vector3.zero);
    }

    public void SetRotation() {
        if( curTargetEnemy != null )
            heroObj.localRotation = (heroObj.localPosition.x - curTargetEnemy.transform.localPosition.x) > 0 ?
                Quaternion.Euler(Vector3.up * -180.0f) : Quaternion.Euler(Vector3.zero);
    }

    public bool EnemyInRange(Enemy target) {
        return Mathf.Abs(heroObj.localPosition.x - target.gameObject.transform.localPosition.x) <= attackRange * 0.5f;
    }

    public int TargetDirection() {
        return heroObj.localRotation.y.Equals(0) ? 1 : -1;
    }

    public void ResetEnemy() {
        curTargetEnemy = null;
    }

    #endregion

    #region Buff

    public bool AddBuff(BuffParam buffParam) {
        int buffListCount = buffList.Count;
        int index = buffList.FindIndex(delegate(BuffParam a) {
            return a.buffType.Equals(buffParam.buffType) &&
                (Mathf.Abs(a.buffValue) < Mathf.Abs(buffParam.buffValue));
        });

        if( index > -1 ) {
            buffList.RemoveAt(index);
            buffList.Add(buffParam);
        }
        else if( buffListCount.Equals(0) )
            buffList.Add(buffParam);
        else
            return false;

        return buffListCount.Equals(0);
    }

    public bool RemoveBuff() {
        if( buffList.Count.Equals(0) )
            return true;
        else {
            List<BuffParam> buffTimerList = new List<BuffParam>(buffList);
            for( int i = 0; i < buffTimerList.Count; i++ ) {
                if( buffTimerList[i].buffTime <= Time.realtimeSinceStartup ) {
                    buffTimerList.RemoveAt(i);
                    if( hero != null )
                        hero.SetBuffText(SkillTable.Instance.GetBuffString(eBuffType.Type_End));
                }
            }

            buffList = buffTimerList;
            return buffList.Count.Equals(0);
        }
    }

    #endregion

    ~HeroStatus() {
        hero = null;
        heroObj = null;
        table = null;
        buffList = null;
        curTargetEnemy = null;
    }
}

public enum eHeroProcessType {
    Type_None = -1,

    Type_Idle,
    Type_Move,
    Type_Attack,

    Type_End,
}

public class Hero : MonoBehaviour {
    public SpriteAnimator spriteAnimator = null;
    public GameObject[] weaponEffectDummyArray = null;
    
    private HeroStatus status = null;

    void FixedUpdate() {
        if( status != null )
            status.SetRotation();
    }

    #region Status

    public void Init(HeroBaseInfo info, TowerPlaceToken token) {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        StopAllCoroutines();

        gameObject.transform.localPosition = new Vector3(0, token.obj.transform.localPosition.y);
        status = new HeroStatus(this, token.FloorIndex, info);

        token.AddPlacedHero(this);

        StartCoroutine("Process");
    }

    public void InitModel() {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        StopAllCoroutines();

        gameObject.transform.localPosition = Vector3.zero;
        if( spriteAnimator != null )
            spriteAnimator.PlayAnimation(eAnimationType.Type_Lobby_Idle, null);
    }

    public HeroStatus GetStatus {
        get {
            return status;
        }
    }

    #endregion

    #region Process

    private IEnumerator Process() {
        Enemy targetEnemy = null;
        List<Enemy> enemyList = null;

        switch( status.GetTable.type ) {
            case eAttackType.Type_Normal:
                switch( status.GetEnemyNormalType(out targetEnemy) ) {
                    case eHeroProcessType.Type_Idle:
                        Idle();
                        yield break;

                    case eHeroProcessType.Type_Attack:
                        StartCoroutine(Attack(targetEnemy));
                        yield break;

                    case eHeroProcessType.Type_Move:
                        StartCoroutine("Move", targetEnemy);
                        yield break;
                }

                break;

            case eAttackType.Type_Piercing:
                switch( status.GetEnemyPiercingType(out enemyList) ) {
                    case eHeroProcessType.Type_Idle:
                        Idle();
                        yield break;

                    case eHeroProcessType.Type_Attack:
                        StartCoroutine(Attack(enemyList));
                        yield break;

                    case eHeroProcessType.Type_Move:
                        StartCoroutine("Move", enemyList[enemyList.Count - 1]);
                        yield break;
                }

                break;

            case eAttackType.Type_Explosion:
                switch( status.GetEnemyNormalType(out targetEnemy) ) {
                    case eHeroProcessType.Type_Idle:
                        Idle();
                        yield break;

                    case eHeroProcessType.Type_Attack:
                        StartCoroutine(Attack(targetEnemy.transform));
                        yield break;

                    case eHeroProcessType.Type_Move:
                        StartCoroutine("Move", targetEnemy);
                        yield break;
                }

                break;
        }
    }

    #endregion

    #region Attack

    private IEnumerator Attack(Enemy targetEnemy) {
        if( spriteAnimator != null ) {
            SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(AttackMotionClear);
            spriteAnimator.PlayAnimation(eAnimationType.Type_Attack, motionClearCB, targetEnemy);
        }

        yield return new WaitForSeconds(status.GetAttackSpeed);

        StartCoroutine("Process");
    }

    private IEnumerator Attack(List<Enemy> targetEnemyList) {
        if( spriteAnimator != null ) {
            SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(AttackMotionClear);
            spriteAnimator.PlayAnimation(eAnimationType.Type_Attack, motionClearCB, targetEnemyList);
        }

        yield return new WaitForSeconds(status.GetAttackSpeed);

        StartCoroutine("Process");
    }

    private IEnumerator Attack(Transform targetTransform) {
        if( spriteAnimator != null ) {
            SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(AttackMotionClear);
            spriteAnimator.PlayAnimation(eAnimationType.Type_Attack, motionClearCB, targetTransform);
        }

        yield return new WaitForSeconds(status.GetAttackSpeed);

        StartCoroutine("Process");
    }

    private void AttackMotionClear(params object[] param) {
        Enemy targetEnemy = null;
        List<Enemy> enemyList = null;

        switch( status.GetTable.type ) {
            case eAttackType.Type_Normal:
                targetEnemy = (Enemy)param[0];
                if( targetEnemy != null )
                    targetEnemy.SetDamage(new DamageParam(-status.GetDamage, status.GetTable.damageTime,
                                status.GetTable.damageNum, eDefenseType.Type_End));

                if( targetEnemy.GetStatus.GetHP.Equals(0) )
                    status.ResetEnemy();

                LoadAttackEffect();

                break;

            case eAttackType.Type_Piercing:
                enemyList = (List<Enemy>)param[0];
                if( enemyList != null ) {
                    for( int i = 0; i < enemyList.Count; i++ ) {
                        enemyList[i].AddDamage(
                            new DamageParam(-status.GetDamage, status.GetTable.damageTime,
                                status.GetTable.damageNum, eDefenseType.Type_End));
                    }
                }

                LoadAttackEffect();

                break;

            case eAttackType.Type_Explosion:
                LoadAttackEffect((Transform)param[0],
                    new SpriteEffect.EffectCompleteCB(ExplosionTargetEffectCompleteCB));

                break;
        }
    }

    private void ExplosionTargetEffectCompleteCB(Vector3 targetPosition) {
        float attackRange = status.GetTable.attackRange * 0.25f;

        float leftPosX = targetPosition.x - attackRange;
        float rightPosX = targetPosition.x + attackRange;

        List<Enemy> enemyList = null;
        if( GameManager.Instance != null && GameManager.Instance.GetEnemyList(status.GetFloorIndex, out enemyList) ) {
            for( int i = 0; i < enemyList.Count; i++ ) {
                if( enemyList[i].gameObject.transform.localPosition.x >= leftPosX &&
                    enemyList[i].gameObject.transform.localPosition.x <= rightPosX )
                    enemyList[i].AddDamage(
                        new DamageParam(-status.GetDamage, status.GetTable.damageTime,
                            status.GetTable.damageNum, eDefenseType.Type_End));
            }
        }
    }

    #endregion

    #region Move

    private IEnumerator Move(Enemy targetEnemy) {
        if( spriteAnimator != null )
            spriteAnimator.PlayAnimation(eAnimationType.Type_Walk, null);

        while( targetEnemy.GetStatus.GetFloorIndex.Equals(status.GetFloorIndex) &&
            status.EnemyInRange(targetEnemy) == false ) {
                transform.localPosition += Vector3.right * status.GetMoveSpeed * status.TargetDirection() * Time.deltaTime;

            yield return null;
        }

        StartCoroutine("Process");
    }

    #endregion

    #region Idle

    private void Idle() {
        if( spriteAnimator != null ) {
            SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(IdleMotionClear);
            spriteAnimator.PlayAnimation(eAnimationType.Type_Idle, motionClearCB);
        }
    }

    private void IdleMotionClear(params object[] param) {
        StartCoroutine("Process");
    }

    #endregion

    #region Buff

    public void AddBuff(BuffParam buffParam) {
        bool addBuff = status.AddBuff(buffParam);
        if( addBuff ) {
            SetBuffText(buffParam);
            StartCoroutine("BuffTimer");
        }
    }

    private IEnumerator BuffTimer() {
        while( true ) {
            if( status.RemoveBuff() )
                yield break;

            yield return new WaitForSeconds(Time.deltaTime);
        }
    }

    #endregion

    #region Buff_Text

    public void SetBuffText(string text) {
        UIDamageText buffText = null;
        if (PoolManager.Instance != null && PoolManager.Instance.GetEffectTextFromPool(out buffText))
            buffText.Init(transform, text);
        else {
            LoadAssetbundle.LoadPrefabCB loadBuffTextPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadBuffTextCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Text_Effect", System.Guid.NewGuid(), loadBuffTextPrefabCB, text);
        }
    }

    public void SetBuffText(BuffParam param) {
        string buffEffectText = SkillTable.Instance.GetBuffString(param);

        UIDamageText buffText = null;
        if (PoolManager.Instance != null && PoolManager.Instance.GetEffectTextFromPool(out buffText))
            buffText.Init(transform, buffEffectText);
        else {
            LoadAssetbundle.LoadPrefabCB loadBuffTextPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadBuffTextCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Text_Effect", System.Guid.NewGuid(), loadBuffTextPrefabCB, buffEffectText);
        }
    }

    private void LoadBuffTextCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootPanelArray[(int)GameManager.eRootPanelType.Type_Effect].transform;
            createObj.transform.localScale = gameObj.transform.localScale;

            UIDamageText buffText = createObj.GetComponent<UIDamageText>();
            if( buffText != null )
                buffText.Init(transform, (string)param[0]);
        }
    }

    #endregion

    #region LoadEffect

    private void LoadAttackEffect() {
        if( status.GetEffectTable != null ) {
            SpriteEffect effect = null;
            for( int i = 0; i < weaponEffectDummyArray.Length; i++ ) {
                if( weaponEffectDummyArray[i].activeInHierarchy ) {
                    if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(status.GetEffectTable.id, out effect) )
                        SetAttackEffect(effect, weaponEffectDummyArray[i].transform);
                    else {
                        LoadAssetbundle.LoadPrefabCB loadEffectCB = new LoadAssetbundle.LoadPrefabCB(LoadAttackEffectCompleteCB);
                        PrefabManager.Instance.LoadPrefab(status.GetEffectTable.effectPath, System.Guid.NewGuid(),
                            loadEffectCB, weaponEffectDummyArray[i].transform, status.GetEffectTable);
                    }
                }
            }
        }
    }

    private void LoadAttackEffect(Transform target, SpriteEffect.EffectCompleteCB effectCompleteCB) {
        if( status.GetEffectTable != null ) {
            SpriteEffect effect = null;
            if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(status.GetEffectTable.id, out effect) )
                effect.Init(target, effectCompleteCB);
            else {
                LoadAssetbundle.LoadPrefabCB loadEffectCB = new LoadAssetbundle.LoadPrefabCB(LoadAttackEffectCompleteCB);
                PrefabManager.Instance.LoadPrefab(status.GetEffectTable.effectPath, System.Guid.NewGuid(),
                    loadEffectCB, target, status.GetEffectTable, effectCompleteCB);
            }
        }
    }

    private void SetAttackEffect(SpriteEffect effect, Transform parent) {
        effect.gameObject.transform.localRotation = gameObject.transform.localRotation;
        effect.Init(parent);
    }

    private void LoadAttackEffectCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootPanelArray[(int)GameManager.eRootPanelType.Type_Effect].transform;

            createObj.transform.localScale = gameObj.transform.localScale;
            createObj.transform.localPosition = Vector3.zero;

            SpriteEffect effect = createObj.GetComponent<SpriteEffect>();
            if( effect != null ) {
                if( param.Length.Equals(2) )
                    effect.Init((Transform)param[0], (EffectTable.TableRow)param[1]);
                else if( param.Length.Equals(3) )
                    effect.Init((Transform)param[0], (EffectTable.TableRow)param[1], (SpriteEffect.EffectCompleteCB)param[2]);
            }
        }
    }

    #endregion

    public void SetActive(bool isSwitch) {
        gameObject.SetActive(isSwitch);
        if( isSwitch == false ) {
            StopAllCoroutines();
            spriteAnimator.Init();
        }
    }

    public void Release() {
        StopAllCoroutines();
    }

    void OnDestroy() {
        spriteAnimator = null;
        status = null;
    }
}
