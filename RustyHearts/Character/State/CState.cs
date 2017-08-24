using UnityEngine;
using System.Collections;

/// <summary>
/// <para>name : CState</para>
/// <para>describe : Override class for Character State.</para>
/// </summary>
public class CState {
    public CState() {
    }

    virtual public bool SetState(Character.State State) {
        return false;
    }
}

public class CStateRespawn : CState {
    override public bool SetState(Character.State State) {
        switch( State ) {
            case Character.State.Stay:
                return true;
            default:
                break;
        }
        return false;
    }
}

public class CStateStay : CState {
    override public bool SetState(Character.State State) {
        switch( State ) {
            case Character.State.Stay:
            case Character.State.Respawn:
                return false;
            default:
                break;
        }
        return true;
    }
}

public class CStateMove : CState {
    override public bool SetState(Character.State State) {
        switch( State ) {
            case Character.State.Stay:
                return true;
            default:
                break;
        }
        return false;
    }
}

public class CStateDamage : CState {
    override public bool SetState(Character.State State) {
        switch( State ) {
            case Character.State.Death:
                return false;
            default:
                break;
        }
        return true;
    }
}

public class CStateAttack : CState {
    override public bool SetState(Character.State State) {
        switch( State ) {
            case Character.State.Attack:
            case Character.State.Death:
            case Character.State.Damage:
            case Character.State.Respawn:
                return true;
            default:
                break;
        }
        return false;
    }
}

public class CStateDeath : CState {
    override public bool SetState(Character.State State) {
        switch( State ) {
            case Character.State.Respawn:
                return true;
            default:
                break;
        }
        return false;
    }
}

public class CStateBackStep : CState {
    override public bool SetState(Character.State State) {
        switch( State ) {
            case Character.State.Death:
                return true;
            default:
                break;
        }
        return false;
    }
}