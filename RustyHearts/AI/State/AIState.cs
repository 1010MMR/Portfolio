using UnityEngine;
using System.Collections;

/// <summary>
/// <para>name : AIState</para>
/// <para>describe : Abstract class for AIMelee.</para>
/// </summary>
public abstract class AIState {
    public enum AI_STATE {
        AI_STATE_ATTACK,
        AI_STATE_CHASE,
        AI_STATE_STAY,
        AI_STATE_MAX,
    };

    public abstract AI_STATE GetState();
    public abstract float GetUtility(Character enemyCharacter, AICharacter ai);
    public virtual bool Init(AICharacter ai) {
        return true;
    }
    public abstract void Process(AICharacter ai);
    public virtual void Reset(AICharacter ai) {
    }

    public bool end {
        get {
            return end_;
        }
        set {
            end_ = value;
        }
    }

    private bool end_ = false;
};
