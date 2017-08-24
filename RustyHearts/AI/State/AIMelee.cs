using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// <para>name : AIMelee</para>
/// <para>describe : Set characters AI State.</para>
/// </summary>
public class AIMelee {
    Character parent_ = null;
    protected AIState[] states_;

    /// <summary>
    /// <para>name : Init</para>
    /// <para>parameter : Character</para>
    /// <para>return : void</para>
    /// <para>describe : Initialize parent AIState.</para>
    /// </summary>
    public void Init(Character parent) {
        parent_ = parent;
        states_ = new AIState[(int)AIState.AI_STATE.AI_STATE_MAX];
        states_[(int)AIState.AI_STATE.AI_STATE_ATTACK] = new AttackAIState();
        states_[(int)AIState.AI_STATE.AI_STATE_CHASE] = new ChaseAIState();
        states_[(int)AIState.AI_STATE.AI_STATE_STAY] = new StayAIState();
    }

    /// <summary>
    /// <para>name : IsEndState</para>
    /// <para>parameter : Character.State</para>
    /// <para>return : bool</para>
    /// <para>describe : Check character's state of the immovable character.</para>
    /// </summary>
    private static bool IsEndState(Character.State state) {
        switch( state ) {
            case Character.State.Damage:
            case Character.State.Death:
            case Character.State.MidairDamage:
                return true;
        }
        return false;
    }

    /// <summary>
    /// <para>name : GetState</para>
    /// <para>parameter : AIState.AI_STATE</para>
    /// <para>return : AIState</para>
    /// <para>describe : Get current AIMelee State.</para>
    /// </summary>
    public AIState GetState(AIState.AI_STATE state) {
        return states_[(int)state];
    }

    /// <summary>
    /// <para>name : AttackAIState</para>
    /// <para>describe : AIState - Set character attack enemy State.</para>
    /// </summary>
    public class AttackAIState : AIState {
        public override AI_STATE GetState() {
            return AI_STATE.AI_STATE_ATTACK;
        }

        public override bool Init(AICharacter ai) {
            if( AIMelee.IsEndState(ai.parent.state) )
                return false;

            if( ai.parent.EnemyPresent == null )
                return false;

            SkillTableManager.CTable skillTable;
            if( ai.parent.GetAvailable_BestSkill(out skillTable) == true ) {
                ai.parent.UseSkill(skillTable.id);
            }
            return ai.parent.Attack();
        }

        public override float GetUtility(Character enemyCharacter, AICharacter ai) {
            if( AIMelee.IsEndState(ai.parent.state) )
                return 0.0f;

            if( ai.parent.AttackIncapacity )
                return 0.0f;

            if( enemyCharacter == null )
                return 0.0f;

            switch( enemyCharacter.state ) {
                case Character.State.Death:
                    return 0.0f;
            }

            if( ai.parent.NextAttackOn() == false )
                return 0.0f;

            if( ai.parent.SelectComboInAttackRange(enemyCharacter) == false ) {
                return 0.0f;
            }

            if( ai.parent.CheckComboAttack(enemyCharacter) ) {
                return 2.0f;
            }

            return 0.0f;
        }

        public override void Process(AICharacter ai) {
            var enemy = ai.parent.EnemyPresent;
            if( enemy == null ) {
                end = true;
                return;
            }
            switch( ai.parent.state ) {
                case Character.State.Attack: {
                        ai.parent.AttackNotNextCombo();
                    }
                    break;
                default:
                    end = true;
                    break;
            }
        }
    }

    /// <summary>
    /// <para>name : AttackAIState</para>
    /// <para>describe : AIState - Set character chase enemy State.</para>
    /// </summary>
    public class ChaseAIState : AIState {
        double checkTime_ = 0;
        Vector3 lastChasePos_;

        public override AI_STATE GetState() {
            return AI_STATE.AI_STATE_CHASE;
        }

        public override bool Init(AICharacter ai) {
            checkTime_ = Time.time;
            lastChasePos_ = ai.parent.transform.position;
            return true;
        }

        public override float GetUtility(Character enemyCharacter, AICharacter ai) {
            if( AIMelee.IsEndState(ai.parent.state) )
                return 0.0f;

            if( enemyCharacter == null )
                return 0.0f;

            if( MagiUtil.IsInRange(enemyCharacter.transform.position - ai.parent.transform.position, ai.parent.keepDistance + enemyCharacter.radius) ) {
                return 0.0f;
            }
            if( MagiUtil.IsInRange(enemyCharacter.transform.position - ai.parent.transform.position, ai.ChaseRange) ) {
                return 0.6f;
            }
            return 0.0f;
        }

        public override void Process(AICharacter ai) {
            if( ai.parent.GetCountAttackComboID() != 0 ) {
                end = true;
                return;
            }

            if( AIMelee.IsEndState(ai.parent.state) ) {
                end = true;
                return;
            }

            Character enemyPresent = ai.parent.EnemyPresent;
            if( enemyPresent == null ) {
                end = true;
                return;
            }

            if( Time.time - checkTime_ > 2.0f ) {
                checkTime_ = Time.time;
                float fLength = (lastChasePos_ - ai.parent.transform.position).magnitude;
                if( fLength < 1.0f ) {
                    end = true;
                    return;
                }
                lastChasePos_ = ai.parent.transform.position;
            }

            if( MagiUtil.IsInRange(enemyPresent.transform.position - ai.parent.transform.position, ai.parent.keepDistance + enemyPresent.radius) ) {
                end = true;
                return;
            }

            if( MagiUtil.IsInRange(enemyPresent.transform.position - ai.parent.transform.position, ai.parent.AttackRange) ) {
                end = true;
                return;
            }

            if( MagiUtil.IsInRange(enemyPresent.transform.position - ai.parent.transform.position, ai.ChaseRange) == false ) {
                end = true;
                return;
            }
        }
    }

    /// <summary>
    /// <para>name : AttackAIState</para>
    /// <para>describe : AIState - Set character stay State. (release)</para>
    /// </summary>
    public class StayAIState : AIState {
        public override AI_STATE GetState() {
            return AI_STATE.AI_STATE_STAY;
        }

        public override bool Init(AICharacter ai) {
            ai.parent.SetStateStay();
            return true;
        }

        public override float GetUtility(Character enemyCharacter, AICharacter ai) {
            if( AIMelee.IsEndState(ai.parent.state) )
                return 0.0f;

            switch( ai.parent.state ) {
                case Character.State.Attack:
                    return 0.0f;
            }

            if( enemyCharacter == null )
                return 0.1f;

            if( ai.parent.NextAttackOn() == true ) {
                return 0.0f;
            }

            if( MagiUtil.IsInRange(enemyCharacter.transform.position - ai.parent.transform.position, ai.parent.keepDistance + enemyCharacter.radius) == false ) {
                return 0.0f;
            }

            return 0.1f;
        }

        public override void Process(AICharacter ai) {
            if( ai.parent.state != Character.State.Stay ) {
                end = true;
                return;
            }

            if( ai.parent.EnemyPresent != null && ai.parent.NextAttackOn() == true ) {
                end = true;
                return;
            }
        }
    }

    ~AIMelee() {
        parent_ = null;
        states_ = null;
    }
}
