using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TowerStatus {
    private Tower tower = null;
    private Transform towerObj = null;
    private int floorIndex = 0;

    private int level = 0;
    private int enchantLevel = 0;

    private int curDamage = 0;
    private float attackSpeed = 0;
    private float attackRange = 0;

    private TowerTable.TableRow table = null;
    private List<TowerAddValueTable.TableRow> addValueTableList = null;
    private EffectTable.TableRow effectTable = null;

    private List<BuffParam> buffList = null;

    private Enemy curTargetEnemy = null;

    public TowerStatus() {
    }

    public TowerStatus(Tower tower, int floorIndex, TowerBaseInfo info, bool isMaxLevelSet = false) {
        this.tower = tower;
        this.towerObj = tower.transform;
        this.table = info.GetTable();
        this.floorIndex = floorIndex;

        this.enchantLevel = info.enchantLevel;

        this.attackSpeed = table.attackSpeed;
        this.attackRange = table.attackRange;

        this.buffList = new List<BuffParam>();

        if( TowerAddValueTable.Instance != null &&
            TowerAddValueTable.Instance.FindTable(table.id, out addValueTableList) ) {
            this.level = isMaxLevelSet ? addValueTableList.Count - 1 : 0;

            this.curDamage = table.damage + addValueTableList[Mathf.Clamp(this.level, 0, addValueTableList.Count - 1)].addDamageValue;
            this.curDamage += Mathf.RoundToInt(this.curDamage * this.enchantLevel * TowerAddValueTable.ENCHANT_ADD_FACTOR);
        }

        if( EffectTable.Instance != null )
            EffectTable.Instance.FindTable(info.GetTable().attackEffectID, out effectTable);

        towerObj.localRotation = (floorIndex.Equals(0) || (floorIndex % 2).Equals(0)) ?
            Quaternion.Euler(Vector3.up * -180.0f) : Quaternion.Euler(Vector3.zero);

        this.curTargetEnemy = null;
    }

    public TowerStatus(Tower tower, int floorIndex, TowerPlacedInfo info) {
        TowerBaseInfo towerBaseInfo = null;
        if( ClientManager.Instance != null && ClientManager.Instance.FindTowerBaseInfo(info.tableID, out towerBaseInfo) ) {
            this.tower = tower;
            this.towerObj = tower.transform;
            this.table = towerBaseInfo.GetTable();
            this.floorIndex = floorIndex;

            this.level = info.level;
            this.enchantLevel = towerBaseInfo.enchantLevel;

            this.attackSpeed = table.attackSpeed;
            this.attackRange = table.attackRange;

            this.buffList = new List<BuffParam>();

            if( TowerAddValueTable.Instance != null &&
                TowerAddValueTable.Instance.FindTable(table.id, out addValueTableList) ) {
                this.curDamage = table.damage + addValueTableList[Mathf.Clamp(this.level, 0, addValueTableList.Count - 1)].addDamageValue;
                this.curDamage += Mathf.RoundToInt(this.curDamage * this.enchantLevel * TowerAddValueTable.ENCHANT_ADD_FACTOR);
            }

            if( EffectTable.Instance != null )
                EffectTable.Instance.FindTable(towerBaseInfo.GetTable().attackEffectID, out effectTable);

            towerObj.localRotation = (floorIndex.Equals(0) || (floorIndex % 2).Equals(0)) ?
                Quaternion.Euler(Vector3.up * -180.0f) : Quaternion.Euler(Vector3.zero);

            this.curTargetEnemy = null;
        }
    }

    #region Table

    public TowerTable.TableRow GetTable {
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

    #region Floor

    public int GetFloorIndex {
        get {
            return floorIndex;
        }
    }

    #endregion

    #region Level

    public int GetEnchantLevel {
        get {
            return enchantLevel;
        }
    }

    public int GetLevel {
        get {
            return level;
        }
    }

    #endregion

    #region Upgrade

    public bool CheckMaxUpgrade() {
        if( addValueTableList != null )
            return level.Equals(addValueTableList.Count - 1);
        else
            return true;
    }

    public void Upgrade() {
        level = Mathf.Clamp(level + 1, 0, addValueTableList.Count - 1);
        if( addValueTableList != null )
            curDamage = table.damage + addValueTableList[level].addDamageValue;
        else
            curDamage = table.damage;

        curDamage += Mathf.RoundToInt(curDamage * enchantLevel * TowerAddValueTable.ENCHANT_ADD_FACTOR);
    }

    public int GetUpgradePrice {
        get {
            return addValueTableList != null ?
                addValueTableList[Mathf.Clamp(level, 0, addValueTableList.Count - 1)].ugradeGoldValue : 0;
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

    #region Target

    public Enemy GetCurrentTargetEnemy {
        get {
            return curTargetEnemy;
        }
    }

    public bool GetEnemyNormalType(out Enemy target) {
        target = null;

        if( curTargetEnemy != null && EnemyInRange(curTargetEnemy) ) {
            target = curTargetEnemy;
            SetRotation(target.transform);

            return true;
        }

        else {
            List<Enemy> enemyList = null;
            if( GameManager.Instance != null && GameManager.Instance.GetEnemyList(floorIndex, out enemyList) ) {
                float range = 0;
                float minRange = float.MaxValue;

                for( int i = 0; i < enemyList.Count; i++ ) {
                    range = Mathf.Abs(towerObj.localPosition.x - enemyList[i].gameObject.transform.localPosition.x);
                    if( range <= attackRange * 0.5f && minRange > range ) {
                        minRange = range;
                        target = enemyList[i];
                    }
                }

                if( target != null ) {
                    curTargetEnemy = target;
                    SetRotation(target.transform);

                    return true;
                }
            }
        }

        return false;
    }

    public bool GetEnemyPiercingType(out List<Enemy> targetList) {
        targetList = null;

        List<Enemy> enemyList = null;
        if( GameManager.Instance != null && GameManager.Instance.GetEnemyList(floorIndex, out enemyList) ) {
            float leftRangeX = towerObj.localPosition.x - attackRange * 0.5f;
            float rightRangeX = towerObj.localPosition.x + attackRange * 0.5f;
            float targetPosX = 0;

            List<Enemy> leftEnemyList = new List<Enemy>();
            List<Enemy> rightEnemyList = new List<Enemy>();

            for( int i = 0; i < enemyList.Count; i++ ) {
                targetPosX = enemyList[i].gameObject.transform.localPosition.x;
                if( targetPosX >= leftRangeX && targetPosX <= towerObj.localPosition.x )
                    leftEnemyList.Add(enemyList[i]);
                else if( targetPosX <= rightRangeX && targetPosX >= towerObj.localPosition.x )
                    rightEnemyList.Add(enemyList[i]);
            }

            if( leftEnemyList.Count.Equals(0) && rightEnemyList.Count.Equals(0) )
                return false;
            else {
                targetList = new List<Enemy>();
                if( leftEnemyList.Count > rightEnemyList.Count )
                    targetList.AddRange(leftEnemyList);
                else
                    targetList.AddRange(rightEnemyList);

                SetRotation(targetList[targetList.Count - 1].transform);

                return true;
            }
        }

        return false;
    }

    public bool GetEnemyExplosionType(out Enemy target) {
        target = null;

        List<Enemy> enemyList = null;
        if( GameManager.Instance != null && GameManager.Instance.GetEnemyList(floorIndex, out enemyList) ) {
            float range = 0;
            float maxRange = 0;

            for( int i = 0; i < enemyList.Count; i++ ) {
                range = Mathf.Abs(towerObj.localPosition.x - enemyList[i].gameObject.transform.localPosition.x);
                if( range <= attackRange * 0.5f && maxRange < range ) {
                    maxRange = range;
                    target = enemyList[i];
                }
            }

            if( target != null )
                SetRotation(target.transform);
        }

        return target != null;
    }

    public bool GetEnemyAreaType(out List<Enemy> targetList) {
        targetList = null;

        List<Enemy> enemyList = null;
        if( GameManager.Instance != null && GameManager.Instance.GetEnemyList(floorIndex, out enemyList) ) {
            float leftRangeX = towerObj.localPosition.x - attackRange * 0.5f;
            float rightRangeX = towerObj.localPosition.x + attackRange * 0.5f;
            float targetPosX = 0;

            targetList = new List<Enemy>();
            for( int i = 0; i < enemyList.Count; i++ ) {
                targetPosX = enemyList[i].gameObject.transform.localPosition.x;
                if( targetPosX >= leftRangeX && targetPosX <= rightRangeX )
                    targetList.Add(enemyList[i]);
            }

            return !targetList.Count.Equals(0);
        }

        return false;
    }

    private void SetRotation(Transform target) {
        towerObj.localRotation = (towerObj.localPosition.x - target.localPosition.x) > 0 ? 
            Quaternion.Euler(Vector3.up * -180.0f) : Quaternion.Euler(Vector3.zero);
    }

    public void SetRotation() {
        if( curTargetEnemy != null )
            towerObj.localRotation = (towerObj.localPosition.x - curTargetEnemy.transform.localPosition.x) > 0 ?
                Quaternion.Euler(Vector3.up * -180.0f) : Quaternion.Euler(Vector3.zero);
    }

    private bool EnemyInRange(Enemy target) {
        return target.GetStatus.GetFloorIndex.Equals(floorIndex) && 
            Mathf.Abs(towerObj.localPosition.x - target.gameObject.transform.localPosition.x) <= attackRange * 0.5f;
    }

    public void ResetEnemy() {
        curTargetEnemy = null;
    }

    #endregion

    #region Sell_Price

    public int GetSellPrice {
        get {
            return Mathf.RoundToInt((table.buyGold *
                TowerManager.TOWER_SELL_DEPRECIATION_FACTOR));
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
                    if( tower != null )
                        tower.SetBuffText(SkillTable.Instance.GetBuffString(eBuffType.Type_End));
                }
            }

            buffList = buffTimerList;
            return buffList.Count.Equals(0);
        }
    }

    #endregion

    ~TowerStatus() {
        tower = null;
        towerObj = null;
        table = null;
        buffList = null;
        addValueTableList = null;
        curTargetEnemy = null;
    }
}

public class Tower : MonoBehaviour {
    public SpriteAnimator spriteAnimator = null;
    public string[] spriteAtlasPathArray = null;
    public GameObject[] effectDummyArray = null;

    private TowerStatus status = null;

    void FixedUpdate() {
        if( status != null )
            status.SetRotation();
    }

    #region Status

    public void Init(TowerPlaceToken token, TowerBaseInfo info) {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        StopAllCoroutines();

        gameObject.transform.localPosition = token.obj.transform.localPosition;
        status = new TowerStatus(this, token.FloorIndex, info);

        SetAtlas();

        if( TowerManager.Instance != null )
            TowerManager.Instance.AddTowerList(this);

        StartCoroutine("AttackMode");
    }

    public void Init(TowerPlaceToken token, TowerPlacedInfo info) {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        StopAllCoroutines();

        gameObject.transform.localPosition = token.obj.transform.localPosition;
        status = new TowerStatus(this, token.FloorIndex, info);

        SetAtlas();

        if( TowerManager.Instance != null )
            TowerManager.Instance.AddTowerList(this);

        StartCoroutine("AttackMode");
    }

    public void InitModel(TowerBaseInfo info, bool isPlay = true) {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        StopAllCoroutines();

        gameObject.transform.localPosition = Vector3.zero;
        status = new TowerStatus(this, 0, info, true);

        SetAtlas();
        if( isPlay && spriteAnimator != null )
            spriteAnimator.PlayAnimation(eAnimationType.Type_Lobby_Idle, null);
    }

    public void Upgrade(int usePrice) {
        if( status.CheckMaxUpgrade() == false ) {
            status.Upgrade();

            UpdateAtlas();

            if( GameManager.Instance != null )
                GameManager.Instance.AddGameGold(-usePrice);
        }
    }

    public TowerStatus GetStatus {
        get {
            return status;
        }
    }

    #endregion

    #region Load_Sprite_Atlas

    private void SetAtlas() {
        LoadAssetbundle.LoadUIAtlasCB loadAtlasCB = null;

        int[] changeLevelArray = { 0, 3, 6, 10 };
        for( int i = changeLevelArray.Length - 1; i >= 0; --i ) {
            if( status.GetLevel >= changeLevelArray[i] ) {
                loadAtlasCB = new LoadAssetbundle.LoadUIAtlasCB(LoadAtlasCompleteCB);
                LoadAssetManager.Instance.LoadUIAtlas(spriteAtlasPathArray[i], loadAtlasCB);

                break;
            }
        }
    }

    private void UpdateAtlas() {
        LoadAssetbundle.LoadUIAtlasCB loadAtlasCB = null;

        int[] changeLevelArray = { 0, 3, 6, 10 };
        for( int i = 0; i < changeLevelArray.Length; i++ ) {
            if( status.GetLevel.Equals(changeLevelArray[i]) ) {
                loadAtlasCB = new LoadAssetbundle.LoadUIAtlasCB(LoadAtlasCompleteCB);
                LoadAssetManager.Instance.LoadUIAtlas(spriteAtlasPathArray[i], loadAtlasCB);

                break;
            }
        }
    }

    private void LoadAtlasCompleteCB(UIAtlas atlas, params object[] param) {
        if( atlas != null && spriteAnimator != null )
            spriteAnimator.GetAnimationSprite.atlas = atlas;
    }

    #endregion

    #region Attack

    private IEnumerator AttackMode() {
        while( true ) {
            switch( status.GetTable.attackType ) {
                case eAttackType.Type_Normal: {
                        Enemy targetEnemy = null;
                        if( status.GetEnemyNormalType(out targetEnemy) ) {
                            if( spriteAnimator != null ) {
                                SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(AttackMotionCB);
                                spriteAnimator.PlayAnimation(eAnimationType.Type_Attack, motionClearCB, targetEnemy);
                            }

                            yield return new WaitForSeconds(status.GetAttackSpeed);
                        }
                    }

                    break;

                case eAttackType.Type_Piercing: {
                        List<Enemy> enemyList = null;
                        if( status.GetEnemyPiercingType(out enemyList) ) {
                            if( spriteAnimator != null ) {
                                SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(AttackMotionCB);
                                spriteAnimator.PlayAnimation(eAnimationType.Type_Attack, motionClearCB, enemyList);
                            }

                            yield return new WaitForSeconds(status.GetAttackSpeed);
                        }
                    }

                    break;

                case eAttackType.Type_Explosion: {
                        Enemy targetEnemy = null;
                        if( status.GetEnemyExplosionType(out targetEnemy) ) {
                            if( spriteAnimator != null ) {
                                SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(AttackMotionCB);
                                spriteAnimator.PlayAnimation(eAnimationType.Type_Attack, motionClearCB, targetEnemy.transform);
                            }

                            yield return new WaitForSeconds(status.GetAttackSpeed);
                        }
                    }

                    break;

                case eAttackType.Type_Area: {
                        List<Enemy> enemyList = null;
                        if( status.GetEnemyAreaType(out enemyList) ) {
                            if( spriteAnimator != null ) {
                                SpriteAnimator.MotionClearCB motionClearCB = new SpriteAnimator.MotionClearCB(AttackMotionCB);
                                spriteAnimator.PlayAnimation(eAnimationType.Type_Attack, motionClearCB, enemyList);
                            }

                            yield return new WaitForSeconds(status.GetAttackSpeed);
                        }
                    }

                    break;

                case eAttackType.Type_Slow: {
                        if( spriteAnimator != null )
                            spriteAnimator.PlayAnimation(eAnimationType.Type_Idle, null);

                        List<Enemy> enemyList = null;
                        if( status.GetEnemyAreaType(out enemyList) ) {
                            for( int i = 0; i < enemyList.Count; i++ ) {
                                enemyList[i].AddBuff(new BuffParam(status.GetTable.towerType, 
                                    -(status.GetDamage * 0.01f), status.GetTable.damageTime + Time.realtimeSinceStartup, eBuffType.Type_Move_Speed));
                            }

                            yield return new WaitForSeconds(status.GetAttackSpeed);
                        }
                    }

                    break;

                case eAttackType.Type_Generator: {
                        if( spriteAnimator != null )
                            spriteAnimator.PlayAnimation(eAnimationType.Type_Idle, null);

                        SetBuffText(SupportString.GetString(6200,
                            "#@Value@#", string.Format("{0:n0}", status.GetDamage)));
                        if( GameManager.Instance != null )
                            GameManager.Instance.AddGameGold(status.GetDamage);

                        yield return new WaitForSeconds(status.GetAttackSpeed);
                    }

                    break;
            }

            yield return null;
        }
    }

    private void AttackMotionCB(params object[] param) {
        switch( status.GetTable.attackType ) {
            case eAttackType.Type_Normal: {
                    Enemy targetEnemy = (Enemy)param[0];
                    if( targetEnemy != null )
                        targetEnemy.AddDamage(
                            new DamageParam(-status.GetDamage, status.GetTable.damageTime,
                                status.GetTable.damageNum, status.GetTable.towerType));

                    LoadAttackEffect();

                    if( targetEnemy.GetStatus.GetHP.Equals(0) )
                        status.ResetEnemy();
                }

                break;

            case eAttackType.Type_Piercing: {
                    List<Enemy> enemyList = (List<Enemy>)param[0];
                    if( enemyList != null ) {
                        for( int i = 0; i < enemyList.Count; i++ ) {
                            enemyList[i].AddDamage(
                                new DamageParam(-status.GetDamage, status.GetTable.damageTime,
                                    status.GetTable.damageNum, status.GetTable.towerType));
                        }
                    }

                    LoadAttackEffect();
                }

                break;

            case eAttackType.Type_Explosion: {
                    LoadAttackEffect((Transform)param[0], 
                        new SpriteEffect.EffectCompleteCB(ExplosionTargetEffectCompleteCB));
                }

                break;

            case eAttackType.Type_Area: {
                    List<Enemy> enemyList = (List<Enemy>)param[0];
                    if( enemyList != null ) {
                        for( int i = 0; i < enemyList.Count; i++ ) {
                            enemyList[i].AddDamage(
                                new DamageParam(-status.GetDamage, status.GetTable.damageTime,
                                    status.GetTable.damageNum, status.GetTable.towerType));
                        }
                    }
                }

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
                            status.GetTable.damageNum, status.GetTable.towerType));
            }
        }
    }

    #endregion

    #region Sell

    public void Sell() {
        StopAllCoroutines();

        SpriteEffect effect = null;
        if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool((int)EffectTable.eEffectType.Type_Tower_Sell, out effect) )
            effect.Init(transform);
        else {
            EffectTable.TableRow effectTable = null;
            if( EffectTable.Instance.FindTable((int)EffectTable.eEffectType.Type_Tower_Sell, out effectTable) ) {
                LoadAssetbundle.LoadPrefabCB loadPrefabCB = new LoadAssetbundle.LoadPrefabCB(LoadDestroyEffectCompleteCB);
                PrefabManager.Instance.LoadPrefab(effectTable.effectPath, System.Guid.NewGuid(), loadPrefabCB, effectTable);
            }
        }

        if( TowerManager.Instance != null )
            TowerManager.Instance.RemoveTowerList(this);

        if( PoolManager.Instance != null )
            PoolManager.Instance.AddTowerPool(this);
        if( GameManager.Instance != null )
            GameManager.Instance.AddGameGold(status.GetSellPrice);
    }

    private void LoadDestroyEffectCompleteCB(GameObject gameObj, System.Guid uid, params object[] param) {
        if( gameObj != null ) {
            GameObject createObj = Instantiate(gameObj) as GameObject;

            createObj.transform.parent = GameManager.Instance.rootPanelArray[(int)GameManager.eRootPanelType.Type_Effect].transform;
            createObj.transform.localScale = gameObj.transform.localScale;

            SpriteEffect effect = createObj.GetComponent<SpriteEffect>();
            if( effect != null )
                effect.Init(transform, (EffectTable.TableRow)param[0]);
        }
    }

    #endregion

    #region LoadEffect

    private void LoadAttackEffect() {
        if( status.GetEffectTable != null ) {
            SpriteEffect effect = null;
            for( int i = 0; i < effectDummyArray.Length; i++ ) {
                if( effectDummyArray[i].activeInHierarchy ) {
                    if( PoolManager.Instance != null && PoolManager.Instance.GetEffectFromPool(status.GetEffectTable.id, out effect) )
                        SetAttackEffect(effect, effectDummyArray[i].transform);
                    else {
                        LoadAssetbundle.LoadPrefabCB loadEffectCB = new LoadAssetbundle.LoadPrefabCB(LoadAttackEffectCompleteCB);
                        PrefabManager.Instance.LoadPrefab(status.GetEffectTable.effectPath, System.Guid.NewGuid(),
                            loadEffectCB, effectDummyArray[i].transform, status.GetEffectTable);
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

    public void SetActive(bool isSwitch) {
        gameObject.SetActive(isSwitch);
        if( isSwitch == false )
            StopAllCoroutines();
    }

    public void Release() {
        StopAllCoroutines();
    }

    void OnDestroy() {
        spriteAnimator = null;
        spriteAtlasPathArray = null;

        status = null;
    }
}
