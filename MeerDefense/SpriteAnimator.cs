using UnityEngine;
using System.Collections;

public enum eAnimationType {
    Type_None = -1,

    Type_Walk,
    Type_Death,
    Type_Respawn,
    Type_Effect,
    Type_Attack,
    Type_Idle,
    Type_Lobby_Idle,

    Type_End,
}

[System.Serializable]
public struct SpriteInfo {
    public string spriteName;
    public string soundPath;

    public bool isActive;
}

[System.Serializable]
public class SpriteAnimationInfo {
    public eAnimationType animType = eAnimationType.Type_None;

    public float animSpeed = 1.0f;
    public bool isLoop = false;

    public SpriteInfo[] spriteInfoArray = null;

    ~SpriteAnimationInfo() {
        spriteInfoArray = null;
    }
}

public class SpriteAnimator : MonoBehaviour {
    public delegate void MotionClearCB(params object[] param);

    public SpriteAnimationInfo[] spriteAnimInfoArray = null;

    private UISprite animSp = null;
    public UISprite GetAnimationSprite {
        get {
            return animSp;
        }
    }

    private eAnimationType selectType = eAnimationType.Type_None;

    void Awake() {
        animSp = gameObject.GetComponent<UISprite>();
    }

    public void Init() {
        selectType = eAnimationType.Type_None;
    }

    public void PlayAnimation(eAnimationType type, MotionClearCB motionClearCB, params object[] param) {
        SpriteAnimationInfo animInfo = null;

        if( GetAnimationInfo(type, out animInfo) ) {
            if( selectType.Equals(type) && animInfo.isLoop )
                return;

            StopAllCoroutines();

            selectType = type;
            if( animSp.isActiveAndEnabled )
                StartCoroutine(Play(animInfo, motionClearCB, param));
        }
    }

    private IEnumerator Play(SpriteAnimationInfo animInfo, MotionClearCB motionClearCB, params object[] param) {
        int index = 0;
        while( animInfo.spriteInfoArray.Length.Equals(index) == false ) {
            if( animSp != null ) {
                animSp.spriteName = animInfo.spriteInfoArray[index].spriteName;
                animSp.MakePixelPerfect();

                if( GameManager.Instance != null &&
                    GameManager.Instance.GetGameStatus.isGameStart && SoundManager.Instance != null )
                    SoundManager.Instance.PlayAudioClip(animInfo.spriteInfoArray[index].soundPath);
            }

            if( animInfo.spriteInfoArray[index].isActive && 
                motionClearCB != null )
                motionClearCB(param);

            yield return new WaitForSeconds(animInfo.animSpeed);

            index++;
            if( index.Equals(animInfo.spriteInfoArray.Length) && animInfo.isLoop )
                index = 0;
        }
    }

    private bool GetAnimationInfo(eAnimationType type, out SpriteAnimationInfo animInfo) {
        animInfo = null;
        for( int i = 0; i < spriteAnimInfoArray.Length; i++ ) {
            if( spriteAnimInfoArray[i].animType.Equals(type) ) {
                animInfo = spriteAnimInfoArray[i];
                break;
            }
        }

        return animInfo != null;
    }

    public float GetAnimTime(eAnimationType type) {
        SpriteAnimationInfo animInfo = null;
        if( GetAnimationInfo(type, out animInfo) ) {
            float time = (float)animInfo.spriteInfoArray.Length * (animInfo.animSpeed);

            return time;
        }

        return 0;
    }

    void OnDestroy() {
        spriteAnimInfoArray = null;
        animSp = null;
    }
}
