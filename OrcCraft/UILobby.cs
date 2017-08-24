using UnityEngine;
using System.Collections;

public class UILobby : MonoBehaviour {
    readonly private string[] tabSpriteNameArray = { "hero_bar", "skill_bar", "weapon_bar", "gold_bar", "gem_bar" };

    public UISprite uiTapSp = null;

    public UILabel cashValueLb = null;
    public UILabel goldValueLb = null;
    public UILabel skillValueLb = null;

    private eUILobbyTab uiLobbyTabType = eUILobbyTab.Type_Hero;

    private GameObject startEffectObj = null;

    public void OpenFrame() {
        if( gameObject.activeSelf == false )
            gameObject.SetActive(true);

        SetTab(uiLobbyTabType);
        UpdateGoodsValue();

        if( SoundManager.Instance != null )
            SoundManager.Instance.PlayBGM(eBGMType.Type_Lobby);

        if( SupportScreenOut.Instance != null )
            SupportScreenOut.Instance.ScreenIn();
    }

    public void Refresh(bool isRefreshTab = true) {
        if( isRefreshTab )
            SetTab(uiLobbyTabType);
        UpdateGoodsValue();
    }

    #region Goods

    private void UpdateGoodsValue() {
        if( ClientManager.Instance != null && ClientManager.Instance.GetStatus != null ) {
            if( cashValueLb != null )
                cashValueLb.text = ClientManager.Instance.GetStatus.cashValue.ToString();
            if( goldValueLb != null )
                goldValueLb.text = ClientManager.Instance.GetStatus.goldValue.ToString();
            if( skillValueLb != null )
                skillValueLb.text = ClientManager.Instance.GetStatus.skillValue.ToString();
        }
    }

    #endregion

    #region Tab

    private void SetTab(eUILobbyTab type) {
        if( uiTapSp != null )
            uiTapSp.spriteName = tabSpriteNameArray[(int)type];

        if( LobbyManager.Instance != null )
            LobbyManager.Instance.SetLobbyTab(type);
    }

    #endregion

    #region Button

    public void OnHeroTab() {
        uiLobbyTabType = eUILobbyTab.Type_Hero;
        SetTab(uiLobbyTabType);
    }

    public void OnSkillTab() {
        uiLobbyTabType = eUILobbyTab.Type_Skill;
        SetTab(uiLobbyTabType);
    }

    public void OnWeaponTab() {
        uiLobbyTabType = eUILobbyTab.Type_Weapon;
        SetTab(uiLobbyTabType);
    }

    public void OnGoldTab() {
        uiLobbyTabType = eUILobbyTab.Type_Gold;
        SetTab(uiLobbyTabType);
    }

    public void OnGemTab() {
        uiLobbyTabType = eUILobbyTab.Type_Gem;
        SetTab(uiLobbyTabType);
    }

    #endregion

    void OnDestroy() {
        uiTapSp = null;

        cashValueLb = null;
        goldValueLb = null;
        skillValueLb = null;

        startEffectObj = null;
    }
}
