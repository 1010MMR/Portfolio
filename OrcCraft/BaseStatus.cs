using UnityEngine;
using System.Collections;

public class BaseStatus {
    public int curHp = 0;
    public int maxHp = 0;

    public BaseStatus(int curHp) {
        this.curHp = curHp;
        this.maxHp = this.curHp;
    }

    #region HP

    public bool SetHP(int addHP) {
        return !(curHp = Mathf.Clamp(curHp + addHP, 0, maxHp)).Equals(0);
    }

    #endregion
}
