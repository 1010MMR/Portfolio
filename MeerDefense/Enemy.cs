using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyStatus {
    private Enemy enemy = null;
    private Transform enemyObj = null;
    private int floorIndex = 0;

    private int level = 0;

    private int curHp = 0;
    private int maxHp = 0;

    private int damage = 0;
    private int moveSpeed = 0;

    private int killGold = 0;

    private EnemyTable.TableRow table = null;
    private List<BuffParam> buffList = null;

    public EnemyStatus() {
    }

    public EnemyStatus(Enemy enemy, int level, EnemyTable.TableRow table) {
        this.enemy = enemy;
        this.enemyObj = enemy.transform;
        this.table = table;

        this.floorIndex = table.startFloor;

        this.level = level;

        this.maxHp = table.hp + level * table.hpAddValue;
        this.curHp = maxHp;

        this.damage = table.damage;
        this.moveSpeed = table.moveSpeed;

        this.killGold = table.killGold;

        this.buffList = new List<BuffParam>();
    }

    #region Table

    public int GetTableID {
        get {
            return table != null ? table.id : 0;
        }
    }

    #endregion

    #region Floor

    public int GetFloorIndex {
        get {
            return floorIndex;
        }
    }

    public bool SetFloorIndex(int index) {
        return !GameManager.MAX_FLOOR_INDEX.Equals(floorIndex = index);
    }

    #endregion

    #region Level

    public int GetLevel {
        get {
            return level;
        }
    }

    #endregion

    #region HP

    public int GetHP {
        get {
            return curHp;
        }
    }

    public int GetMaxHP {
        get {
            return maxHp;
        }
    }

    public bool SetHP(int addHP) {
        return !(curHp = Mathf.Clamp(curHp + addHP, 0, maxHp)).Equals(0);
    }

    #endregion

    #region Damage

    public int GetDamage {
        get {
            return damage;
        }
    }

    #endregion

    #region Move

    public int GetMoveSpeed {
        get {
            float moveBuffValue = 0;
            for( int i = 0; i < buffList.Count; i++ ) {
                if( buffList[i].buffType.Equals(eBuffType.Type_Move_Speed) )
                    moveBuffValue += buffList[i].buffValue;
            }

            return Mathf.Clamp(Mathf.RoundToInt(moveSpeed + (moveSpeed * moveBuffValue)), 0, moveSpeed);
        }
    }

    public bool FloorTargetOn() {
        float endPointX = GameManager.Instance.GetEndPoint(floorIndex).x;
        bool isFloorOver = endPointX > 0 ?
            enemyObj.localPosition.x >= endPointX :
            enemyObj.localPosition.x <= endPointX;

        return isFloorOver;
    }

    #endregion

    #region Kill_Gold

    public int GetKillGold {
        get {
            float addValue = 0;
            for( int i = 0; i < buffList.Count; i++ ) {
                if( buffList[i].buffType.Equals(eBuffType.Type_Gain_Gold) )
                    addValue += buffList[i].buffValue;
            }

            return Mathf.RoundToInt(killGold + (killGold * addValue));
        }
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
                    if( enemy != null )
                        enemy.SetBuffText(SkillTable.Instance.GetBuffString(eBuffType.Type_End));
                }
            }

            buffList = buffTimerList;
            return buffList.Count.Equals(0);
        }
    }

    #endregion

    #region Immunity

    public bool CheckImmunity(eDefenseType type) {
        if( table.immunityArray != null ) {
            for( int i = 0; i < table.immunityArray.Length; i++ ) {
                if( table.immunityArray[i].Equals(type) )
                    return true;
            }
        }

        return false;
    }

    #endregion

    ~EnemyStatus() {
        enemy = null;
        enemyObj = null;

        table = null;
        buffList = null;
    }
}

public class Enemy : MonoBehaviour {
    private const float LOW_DISTANCE = 0.05f;
    private const float HP_BAR_POSITION_Y = -70.0f;
    private const float MOVE_SPEED_FACTOR = 30.0f;

    public GameObject spriteGroup = null;

    public SpriteAnimator spriteAnimator = null;
    public Transform effectDummy = null;

    private EnemyStatus status = null;
    private UISlider hpBar = null;

    #region Status

    public void Init(int level, EnemyTable.TableRow table) {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        status = new EnemyStatus(this, level, table);
        MovePosition(status.GetFloorIndex);

        InitHpBar();
    }

    public EnemyStatus GetStatus {
        get {
            return status;
        }
    }

    #endregion

    #region Move

    public void MovePosition(int index) {
        transform.localPosition = GameManager.Instance.GetStartPoint(index);
        spriteGroup.transform.localRotation = (index.Equals(0) || (index % 2).Equals(0)) ?
            Quaternion.Euler(Vector3.zero) : Quaternion.Euler(Vector3.up * -180.0f);

        StartCoroutine("Move", index);
    }

    private IEnumerator Move(int index) {
        if( spriteAnimator != null )
            spriteAnimator.PlayAnimation(eAnimationType.Type_Walk, null);

        while( status.FloorTargetOn() == false ) {
            transform.localPosition += Vector3.right * status.GetMoveSpeed * 
                MOVE_SPEED_FACTOR * MoveDirection(index) * Time.deltaTime;

            yield return null;
        }

        index++;
        if( status.SetFloorIndex(index) )
            MovePosition(index);
        else
            Attack();
    }

    private int MoveDirection(int index) {
        return (index.Equals(0) || (index % 2).Equals(0)) ? 1 : -1;
    }

    #endregion

    #region Death

    private void Death() {
        if( GameManager.Instance != null && 
            GameManager.Instance.CheckEnemyExist(this) ) {
            GameManager.Instance.RemoveEnemyList(this);

            StopAllCoroutines();
            if( spriteAnimator != null ) {
                SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(DeathMotionClear);
                spriteAnimator.PlayAnimation(eAnimationType.Type_Death, motionClearCB);
            }
        }
    }

    private void DeathMotionClear(params object[] param) {
        if( PoolManager.Instance != null )
            PoolManager.Instance.AddEnemyPool(this);

        GameManager.Instance.AddGameGold(status.GetKillGold + 
            ClientManager.Instance.GetUserStatus.GetAddGainGold());
    }

    #endregion

    #region Attack

    private void Attack() {
        StopAllCoroutines();

        if( GameManager.Instance != null && 
            GameManager.Instance.GetGameStatus != null )
            GameManager.Instance.GetGameStatus.GameOver();
    }

    #endregion

    #region Damage

    public void AddDamage(DamageParam param) {
        if( isActiveAndEnabled )
            StartCoroutine("Damage", param);
    }

    private IEnumerator Damage(DamageParam param) {
        for( int i = 0; i < param.num; i++ ) {
            if( status.CheckImmunity(param.type) == false )
                SetDamage(param);
            else {
                SetBuffText(SupportString.GetString(1026));
                SetDamageEffect(param);
            }

            yield return new WaitForSeconds(param.time);
        }
    }

    public void SetDamage(DamageParam param) {
        if( status.SetHP(param.damage) == false )
            Death();

        SetDamageEffect(param);
        SetDamageText(param);

        SetHpBar();
    }

    #endregion

    #region Damage_Effect

    private void SetDamageEffect(DamageParam param) {
        SpriteEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(param.GetDamageEffectID, out effect) )
            effect.Init(effectDummy);
        else {
            EffectTable.TableRow effectTable = null;
            if( EffectTable.Instance.FindTable(param.GetDamageEffectID, out effectTable) ) {
                LoadAssetbundle.LoadPrefabCB loadDamageEffectPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadDamageEffectCompleteCB);
                PrefabManager.Instance.LoadPrefab(effectTable.effectPath, System.Guid.NewGuid(), loadDamageEffectPrefabCB, effectTable);
            }
        }
    }

    private void LoadDamageEffectCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootPanelArray[(int)GameManager.eRootPanelType.Type_Effect].transform;
            createObj.transform.localScale = gameObj.transform.localScale;

            SpriteEffect effect = createObj.GetComponent<SpriteEffect>();
            if( effect != null )
                effect.Init(effectDummy, (EffectTable.TableRow)param[0]);
        }
    }

    #endregion

    #region Damage_Text

    private void SetDamageText(DamageParam param) {
        UIDamageText damageText = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetDamageTextFromPool(out damageText) )
            damageText.Init(effectDummy, param);
        else {
            LoadAssetbundle.LoadPrefabCB loadDamageTextPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadDamageTextCompleteCB);
            PrefabManager.Instance.LoadPrefab("Effect/Text_Damage", System.Guid.NewGuid(), loadDamageTextPrefabCB, param);
        }
    }

    private void LoadDamageTextCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootPanelArray[(int)GameManager.eRootPanelType.Type_Effect].transform;
            createObj.transform.localScale = gameObj.transform.localScale;

            UIDamageText damageText = createObj.GetComponent<UIDamageText>();
            DamageParam damageParam = (DamageParam)param[0];
            if( damageText != null )
                damageText.Init(effectDummy, damageParam);
        }
    }

    #endregion

    #region HP_Bar

    private void InitHpBar() {
        if( hpBar != null )
            SetHpBar();
        else {
            LoadAssetbundle.LoadPrefabCB loadPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadHpBarCompleteCB);
            PrefabManager.Instance.LoadPrefab("UI/UI_Object/Enemy_Hp_Bar", System.Guid.NewGuid(), loadPrefabCB);
        }
    }

    private void SetHpBar() {
        float hpRatio = (float)status.GetHP / (float)status.GetMaxHP;
        if( hpBar != null )
            hpBar.value = Mathf.Clamp(hpRatio, 0, 1.0f);
    }

    private void LoadHpBarCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = gameObject.transform;
            createObj.transform.localScale = gameObj.transform.localScale;
            createObj.transform.localPosition = Vector3.up * HP_BAR_POSITION_Y;

            hpBar = createObj.GetComponent<UISlider>();
            SetHpBar();
        }
    }

    #endregion

    #region Buff

    public void AddBuff(BuffParam buffParam) {
        if( status.CheckImmunity(buffParam.defenseType) == false ) {
            bool addBuff = status.AddBuff(buffParam);
            if( addBuff ) {
                SetBuffText(buffParam);
                StartCoroutine("BuffTimer");
            }
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
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectTextFromPool(out buffText) )
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

    public void SetActive(bool isSwitch) {
        gameObject.SetActive(isSwitch);
        if( isSwitch == false ) {
            StopAllCoroutines();
            spriteAnimator.Init();
        }
    }

    void OnDestroy() {
        spriteGroup = null;
        spriteAnimator = null;
        effectDummy = null;

        status = null;
        hpBar = null;
    }
}