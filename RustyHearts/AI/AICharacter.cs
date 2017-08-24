using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// <para>name : AICharacter</para>
/// <para>describe : Add character object if this character is AI or enemy.</para>
/// </summary>
[AddComponentMenu("Magi/AI/AICharacter")]
[System.Serializable]
[RequireComponent(typeof(Character))]
[RequireComponent(typeof(PlayerController))]
public class AICharacter : MonoBehaviour {
    public bool backStep_ = false;
    public bool sideStep_ = false;

    private float investigationRange_ = 10.0f;
    private float chaseRange_ = 20.0f;

    private AIState bestState_ = null;
    private AIState[] states_ = new AIState[(int)AIState.AI_STATE.AI_STATE_MAX];
    private Character parent_ = null;
    private AIMelee melee_ = null;

    private float backStepStartTime_ = 0;
    private float sideStepStartTime_ = 0;

    void Awake() {
        parent_ = GetComponent<Character>();
        melee_ = new AIMelee();
        melee_.Init(parent_);

        int stateMax = (int)AIState.AI_STATE.AI_STATE_MAX;
        for( int i = 0; i < stateMax; ++i ) {
            states_[i] = melee_.GetState((AIState.AI_STATE)i);
        }
    }

    void OnDestroy() {
        bestState_ = null;
        states_ = null;
        parent_ = null;
        melee_ = null;
    }

    void FixedUpdate() {
        if( DungeonSceneScriptManager.Instance.m_DungeonPlayEvent.StartDelayTime == false )
            return;
        if( GameManager.Instance != null && GameManager.Instance.GamePause )
            return;

        Process();
    }

    public Character parent {
        get {
            return parent_;
        }
    }

    public float InvestigationRange {
        get {
            return investigationRange_;
        }
    }

    public float ChaseRange {
        get {
            return chaseRange_;
        }
    }

    public float BackStepStartTime {
        get {
            return backStepStartTime_;
        }
        set {
            backStepStartTime_ = value;
        }
    }

    public float SideStepStartTime {
        get {
            return sideStepStartTime_;
        }
        set {
            sideStepStartTime_ = value;
        }
    }

    /// <summary>
    /// <para>name : Process</para>
    /// <para>parameter : </para>
    /// <para>return : void</para>
    /// <para>describe : Select best AIState end processing.</para>
    /// </summary>
    private void Process() {
        bestState_ = SelectState();
        if( bestState_ != null ) {
            bestState_.Process(this);
        }
    }

    /// <summary>
    /// <para>name : SelectState</para>
    /// <para>parameter : </para>
    /// <para>return : AIState</para>
    /// <para>describe : Select best AIState.</para>
    /// </summary>
    private AIState SelectState() {
        Character parent = GetComponent<Character>();
        if( parent == null ) {
            return bestState_;
        }

        if( parent.state.Equals(Character.State.Respawn) )
            return bestState_;

        Character enemy = parent.EnemyPresent;
        if( enemy == null ) {
            List<Character> enemyList;
            if( parent.FindEnemy(out enemyList, InvestigationRange) )
                parent.EnemyPresent = enemyList[0];
        }

        if( bestState_ != null && bestState_.end == false )
            return bestState_;

        AIState best = null;
        float bestUtility = 0.0f;

        for( int i = 0; i < (int)AIState.AI_STATE.AI_STATE_MAX; ++i ) {
            if( states_[i] == null )
                continue;

            float utility = states_[i].GetUtility(enemy, this);
            if( utility > bestUtility ) {
                best = states_[i];
                bestUtility = utility;
            }
        }

        if( best != null ) {
            if( best.Init(this) )
                best.end = false;
        }

        return best;
    }
}
