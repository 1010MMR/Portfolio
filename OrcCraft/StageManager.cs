using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StageManager : MonoBehaviour {
    public const int MAX_WAVE_VALUE = 50;
    public const int RESTART_WAVE_VALUE = 30;

    public const int ADD_REPEAT_LEVEL = 10;

    private static StageManager instance = null;
    public static StageManager Instance {
        get {
            return instance;
        }
    }

    private WaveTable.TableRow waveTable = null;
    public WaveTable.TableRow GetWaveTable {
        get {
            return waveTable;
        }
    }

    void Awake() {
        instance = this;
    }

    #region Respone_Enemy

    public void StartEnemyRespawn() {
        StartCoroutine("EnemyRespawn", GameManager.Instance.GetGameStatus.waveID);
    }

    private IEnumerator EnemyRespawn(int level) {
        if( GameManager.Instance != null ) {
            waveTable = null;
            if( WaveTable.Instance != null && WaveTable.Instance.FindTable(level, out waveTable) ) {
                BossTable.TableRow bossTable = null;
                if( BossTable.Instance != null && BossTable.Instance.FindTable(waveTable.bossID, out bossTable) )
                    GameManager.Instance.MakeBoss(bossTable);

                float spawnTime = waveTable.waveTime + Time.realtimeSinceStartup;
                while( spawnTime >= Time.realtimeSinceStartup ) {
                    if( GameManager.Instance.GetGameStatus.isGameStart == false )
                        yield break;

                    EnemyTable.TableRow enemyTable = null;
                    if( EnemyTable.Instance != null &&
                        EnemyTable.Instance.FindTable(waveTable.GetRandomSelectMonsterID(), out enemyTable) )
                        GameManager.Instance.MakeEnemy(enemyTable,
                            Random.Range(0, GameManager.MAX_ENEMY_LANE_COUNT));

                    yield return new WaitForSeconds(waveTable.createTime);
                }
            }

            else {
                GameManager.Instance.GetGameStatus.GameOver();
                yield break;
            }

            GameManager.Instance.GetGameStatus.AddGem(waveTable.rewardValue);
            StartCoroutine("EnemyRespawn", GameManager.Instance.GetGameStatus.AddWave());
        }
    }

    #endregion

    public void Release() {
        StopAllCoroutines();
    }

    void OnDestroy() {
        instance = null;

        waveTable = null;
    }
}
