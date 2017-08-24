using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIItemTooltip : MonoBehaviour
{
    private const float TOOLTIP_CLOSE_DISTANCE = 0.0f;

    public enum BACKGROUND_IMAGE
    {
        NORMAL,
        BOX,
        RIGHT,
    }

    private Transform m_transform = null;

    private UISprite[] m_spriteArray = null;
    private enum SPRITE_TYPE
    {
        TYPE_BACKGROUND,
        TYPE_MANAGE_ICON,

        TYPE_DRESSCODE_01,
        TYPE_DRESSCODE_02,

        TYPE_END,
    }

    private UILabel[] m_labelArray = null;
    private enum LABEL_TYPE
    {
        TYPE_ITEM_NAME,
        TYPE_DESC_TEXT,

        TYPE_MANAGE_MANAGE,
        TYPE_MANAGE_USER_EXP,
        TYPE_MANAGE_AP,
        TYPE_MANAGE_DOG_EXP,

        TYPE_POTION_AP,

        TYPE_CLOTH_DRESSCODE_01,
        TYPE_CLOTH_DRESSCODE_02,

        TYPE_INTERIOR_VALUE,
        TYPE_INTERIOR_SIZE,

        TYPE_RECEIPE_TEXT,

        TYPE_ALL_TEXT,
        TYPE_ALL_AP_VALUE,

        TYPE_END,
    }

    private GameObject[] m_objectArray = null;
    private enum OBJECT_TYPE
    {
        TYPE_MANAGE,
        TYPE_POTION,
        TYPE_CLOTH,
        TYPE_INTERIOR,
        TYPE_RECEIPE,
        TYPE_ALL,

        TYPE_INTERIOR_REWARD_AP,
        TYPE_INTERIOR_REWARD_EXP,
        TYPE_INTERIOR_REWARD_NONE,

        TYPE_END,
    }

    private enum BACKGROUND_TYPE
    {
        TYPE_NORMAL,
        TYPE_SMALL,
        TYPE_LARGE,

        TYPE_END,
    }

    private Transform m_target = null;
    private CDirection m_cDirection = null;
    private Vector3 m_vMovePos;
    private Vector3 m_TransPos;
    
    private int m_bShowTooltip = -1;
    public bool CheckShowTooltip { get { return m_bShowTooltip.Equals(1); } }

    void Awake()
    {
        m_vMovePos = Vector3.zero;
        m_transform = transform;

        #region Label
        string[] labelPathArray = { "Name", "Desc", "Manage_Group/Manage_Value", "Manage_Group/User_Exp_Value", "Manage_Group/Ap_Value", "Manage_Group/Dog_Exp_Value", 
                                    "Potion_Group/Ap_Value", "Cloth_Group/Cloth01/Value", "Cloth_Group/Cloth02/Value", "Interior_Group/Point/Value", "Interior_Group/Size", 
                                    "Receipe_Group/Text", "All_Group/Ticket_Value", "All_Group/Ap_Value" };
        m_labelArray = new UILabel[(int)LABEL_TYPE.TYPE_END];
        for(int i = 0; i < labelPathArray.Length; i++)
            m_labelArray[i] = m_transform.FindChild(labelPathArray[i]).GetComponent<UILabel>();
        #endregion

        #region Sprite
        string[] spritePathArray = { "Background", "Manage_Group/Manage_Icon/Icon", "Cloth_Group/Cloth01/Icon", "Cloth_Group/Cloth02/Icon" };
        m_spriteArray = new UISprite[(int)SPRITE_TYPE.TYPE_END];
        for(int i = 0; i < spritePathArray.Length; i++)
            m_spriteArray[i] = m_transform.FindChild(spritePathArray[i]).GetComponent<UISprite>();
        #endregion

        #region Object
        string[] objectPathArray = { "Manage_Group", "Potion_Group", "Cloth_Group", "Interior_Group", "Receipe_Group", "All_Group", 
                                           "Interior_Group/Reward/Ap", "Interior_Group/Reward/Exp", "Interior_Group/Reward/None" };
        m_objectArray = new GameObject[(int)OBJECT_TYPE.TYPE_END];
        for(int i = 0; i < objectPathArray.Length; i++)
            m_objectArray[i] = m_transform.FindChild(objectPathArray[i]).gameObject;

        m_cDirection = gameObject.GetComponent<CDirection>();
        #endregion
    }

    void Start()
    {
        OnOffTooltip(false, true);
    }

    void Update()
    {
        UpdateTargetPosition();
    }

    public void UpdateDepth(int depth, int sortingOrder)
    {
        UIPanel panel = gameObject.GetComponent<UIPanel>();
        panel.depth = depth;
        panel.sortingOrder = sortingOrder;
    }

    public void UpdateTooltip(uint index, Transform target)
    {
        ReleaseGroup();

        m_transform.position      = target.position;
        m_TransPos                = m_transform.position;
        m_transform.localPosition = m_transform.localPosition + m_vMovePos;

        m_target = target;

        ITEM_TYPE mainType = Util.ParseItemMainType(index);
        ITEM_SUB_TYPE subType = Util.ParseItemSubType(index);

        switch(mainType)
        {
            case ITEM_TYPE.MATERIAL:
                m_objectArray[(int)OBJECT_TYPE.TYPE_MANAGE].SetActive(true);
                SetBackground(BACKGROUND_TYPE.TYPE_LARGE);

                ItemInfo_Material iMaterial;
                if(WorldManager.instance.m_dataManager.m_ItemDataMaterial.GetItemData(index, out iMaterial))
                {
                    #region Label
                    m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].text = iMaterial.itemName;
                    m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = iMaterial.explain;

                    m_labelArray[(int)LABEL_TYPE.TYPE_MANAGE_MANAGE].text = iMaterial.getDesireAmount.ToString();
                    m_labelArray[(int)LABEL_TYPE.TYPE_MANAGE_USER_EXP].text = iMaterial.bonusExp.ToString();
                    m_labelArray[(int)LABEL_TYPE.TYPE_MANAGE_AP].text = string.Format("-{0}", iMaterial.useActivePower.ToString());
                    m_labelArray[(int)LABEL_TYPE.TYPE_MANAGE_DOG_EXP].text = iMaterial.bonusDogExp.ToString();
                    #endregion

                    #region Sprite
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_MANAGE_ICON].spriteName = GetDesireIconName(iMaterial.index);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_MANAGE_ICON].MakePixelPerfect();
                    #endregion
                }
                break;

            case ITEM_TYPE.DIRECTION:
				ItemInfo_Direct iDirect;
				if(WorldManager.instance.m_dataManager.m_itemDataDirect.GetItemData(index, out iDirect))
				{
					if(CheckManageAllSubType(subType))
					{
						m_objectArray[(int)OBJECT_TYPE.TYPE_ALL].SetActive(true);
						SetBackground(BACKGROUND_TYPE.TYPE_NORMAL);

						#region Label
						m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].text = iDirect.itemName;
						m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = iDirect.explain;

						m_labelArray[(int)LABEL_TYPE.TYPE_ALL_AP_VALUE].text = string.Format("-{0}", iDirect.useAp.ToString());
                        m_labelArray[(int)LABEL_TYPE.TYPE_ALL_TEXT].text = iDirect.getAmount.Equals(0) ? Str.instance.Get(410029, "%PERCENT%", Mathf.RoundToInt(iDirect.getPercent * 100.0f).ToString()) :
                                                                                                        Str.instance.Get(410029, "%TIME%", Mathf.RoundToInt(iDirect.getAmount / 60).ToString());
                        #endregion
                    }

					else
					{
						m_objectArray[(int)OBJECT_TYPE.TYPE_POTION].SetActive(true);
						SetBackground(BACKGROUND_TYPE.TYPE_NORMAL);

						#region Label
						m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].text = iDirect.itemName;
						m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = iDirect.explain;

						m_labelArray[(int)LABEL_TYPE.TYPE_POTION_AP].text = iDirect.getAmount.Equals(0) ?
							string.Format("+{0}%", Mathf.RoundToInt(iDirect.getPercent * 100.0f)) : string.Format("+{0}", iDirect.getAmount);
						#endregion
					}
				}
				break;

			case ITEM_TYPE.INTERIOR:
                m_objectArray[(int)OBJECT_TYPE.TYPE_INTERIOR].SetActive(true);
                SetBackground(BACKGROUND_TYPE.TYPE_LARGE);

                ItemInfo_Interior iInterior;
                if(WorldManager.instance.m_dataManager.m_itemDataInterior.GetItemData(index, out iInterior))
                {
                    #region Label
                    m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].text = iInterior.itemName;
                    m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = iInterior.explain;

                    m_labelArray[(int)LABEL_TYPE.TYPE_INTERIOR_VALUE].text = iInterior.funiture_Point.ToString();
                    m_labelArray[(int)LABEL_TYPE.TYPE_INTERIOR_SIZE].text = string.Format("{0}x{1}", iInterior.tileX, iInterior.tileY);
                    #endregion

                    #region Object
                    RoomPlayInfo[] roomPlayInfo = WorldManager.instance.m_dataManager.m_RoomPlayData.GetPlayData(iInterior.index);
                    if (roomPlayInfo.Length.Equals(0))
                    {
                        m_objectArray[(int)OBJECT_TYPE.TYPE_INTERIOR_REWARD_NONE].SetActive(true);
                        m_objectArray[(int)OBJECT_TYPE.TYPE_INTERIOR_REWARD_AP].SetActive(false);
                        m_objectArray[(int)OBJECT_TYPE.TYPE_INTERIOR_REWARD_EXP].SetActive(false);
                    }
                    else
                    {
                        m_objectArray[(int)OBJECT_TYPE.TYPE_INTERIOR_REWARD_NONE].SetActive(false);

                        for (int i = 0; i < roomPlayInfo.Length; i++)
                        {
                            bool isAp = !roomPlayInfo[i].recoveryActive.Equals(0);
                            m_objectArray[(int)OBJECT_TYPE.TYPE_INTERIOR_REWARD_AP].SetActive(isAp);
                            m_objectArray[(int)OBJECT_TYPE.TYPE_INTERIOR_REWARD_EXP].SetActive(!isAp);
                        }
                    }
                    #endregion
                }
                break;

            case ITEM_TYPE.CLOTHES:
                m_objectArray[(int)OBJECT_TYPE.TYPE_CLOTH].SetActive(true);
                SetBackground(BACKGROUND_TYPE.TYPE_NORMAL);

                ItemInfo_Clothes iClothes;
                if(WorldManager.instance.m_dataManager.m_itemDataClothes.GetItemData(index, out iClothes))
                {
                    m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].text = iClothes.itemName;
                    m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = iClothes.explain;

                    DressCodeInfo[] infoArray = WorldManager.instance.m_dataManager.m_itemDataClothes.GetDressCodeInfoArray(iClothes);

                    m_labelArray[(int)LABEL_TYPE.TYPE_CLOTH_DRESSCODE_01].text = infoArray[0].value.ToString();
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DRESSCODE_01].spriteName = infoArray[0].iconName;

                    bool isExists = infoArray.Length.Equals(2);
                    m_labelArray[(int)LABEL_TYPE.TYPE_CLOTH_DRESSCODE_02].gameObject.SetActive(isExists);
                    m_spriteArray[(int)SPRITE_TYPE.TYPE_DRESSCODE_02].gameObject.SetActive(isExists);

                    if (isExists)
                    {
                        m_labelArray[(int)LABEL_TYPE.TYPE_CLOTH_DRESSCODE_02].text = infoArray[1].value.ToString();
                        m_spriteArray[(int)SPRITE_TYPE.TYPE_DRESSCODE_02].spriteName = infoArray[1].iconName;
                    }
                }
                break;

            case ITEM_TYPE.RECIPE:
                m_objectArray[(int)OBJECT_TYPE.TYPE_RECEIPE].SetActive(true);
                SetBackground(BACKGROUND_TYPE.TYPE_NORMAL);

                ItemInfo_Recipe iRecipe;
                if (WorldManager.instance.m_dataManager.m_ItemTableRecipe.GetItemData(index, out iRecipe))
                {
                    m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].text = iRecipe.RecipeItemName;
                    m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = iRecipe.RecipeItemExplain;
                    m_labelArray[(int)LABEL_TYPE.TYPE_RECEIPE_TEXT].text = iRecipe.FarmView;
                }
                break;

            case ITEM_TYPE.MONEY:
            case ITEM_TYPE.OWN:
            case ITEM_TYPE.PETEGG:
            case ITEM_TYPE.PETUPGRADE:
            case ITEM_TYPE.DOGTICKET:
                SetBackground(BACKGROUND_TYPE.TYPE_SMALL);
                
                m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].text = WorldManager.instance.GetItemName(index);
                m_labelArray[(int)LABEL_TYPE.TYPE_DESC_TEXT].text = WorldManager.instance.GetItemExplain(index);
                break;
        }
    }

    private void UpdateTargetPosition()
    {
        if (CheckShowTooltip && m_target != null)
        {
            Vector3 heading = m_target.position - m_TransPos;
            float distance = (Mathf.RoundToInt(heading.sqrMagnitude * 1000000.0f)) / 1000000.0f;

            if (distance > TOOLTIP_CLOSE_DISTANCE)
                OnOffTooltip(false, true);
        }
    }

    public void SetBackground( BACKGROUND_IMAGE eType )
    {
        UISprite p = m_transform.FindChild( "Background" ).gameObject.GetComponent<UISprite>();

        switch( eType )
        {
            case BACKGROUND_IMAGE.NORMAL:   p.spriteName = "Bg_Shop_Tipbox"; break;
            case BACKGROUND_IMAGE.BOX:      p.spriteName = "Bg_Gch_Tipbox01"; break;
            case BACKGROUND_IMAGE.RIGHT:
                {
                    p.spriteName = "Bg_Shop_TipboxRight";
                    m_vMovePos = new Vector3( -110f, 0, 0 );
                }
                break;
        }
    }

    #region Util

    private string GetDesireIconName(uint index)
    {
        ITEM_SUB_TYPE type = Util.ParseItemSubType(index);
        switch(type)
        {
            case ITEM_SUB_TYPE.MATERIAL_HUNGRY: return "Icon_Desire_Hungry_Small";
            case ITEM_SUB_TYPE.MATERIAL_THIRTY: return "Icon_Desire_Thirsty_Small";
            case ITEM_SUB_TYPE.MATERIAL_DUNG: return "Icon_Desire_Defecation_Small";
            case ITEM_SUB_TYPE.MATERIAL_CLEAN: return "Icon_Desire_Clean_Small";
            case ITEM_SUB_TYPE.MATERIAL_HAPPY: return "Icon_Desire_Happy_Small";
            default: return "";
        }
    }

    private bool CheckManageAllSubType(ITEM_SUB_TYPE type)
    {
		if(type.Equals(ITEM_SUB_TYPE.DIRECT_MANAGE_NORMAL) || type.Equals(ITEM_SUB_TYPE.DIRECT_MANAGE_ROOM) || 
		   type.Equals(ITEM_SUB_TYPE.DIRECT_MANAGE_ALL))
            return true;
        else
            return false;
    }

    private void ReleaseGroup()
    {
        for(int i = 0; i < (int)OBJECT_TYPE.TYPE_ALL + 1; i++)
            m_objectArray[i].SetActive(false);
    }

    private void SetBackground(bool isLarge)
    {
        m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].transform.localPosition = isLarge ? new Vector3(-2.0f, 175.0f, 0) : new Vector3(-2.0f, 148.0f, 0);
        m_spriteArray[(int)SPRITE_TYPE.TYPE_BACKGROUND].height = isLarge ? 198 : 170;
    }

    private void SetBackground(BACKGROUND_TYPE type)
    {
        Vector3 pos = Vector3.zero;
        int height = 0;

        switch (type)
        {
            case BACKGROUND_TYPE.TYPE_NORMAL: pos = new Vector3(-2.0f, 148.0f, 0); height = 170; break;
            case BACKGROUND_TYPE.TYPE_SMALL: pos = new Vector3(-2.0f, 108.0f, 0); height = 130; break;
            case BACKGROUND_TYPE.TYPE_LARGE: pos = new Vector3(-2.0f, 175.0f, 0); height = 198; break;
        }

        m_labelArray[(int)LABEL_TYPE.TYPE_ITEM_NAME].transform.localPosition = pos;
        m_spriteArray[(int)SPRITE_TYPE.TYPE_BACKGROUND].height = height;
    }

    #endregion

    public void OnOffTooltip(bool b, bool isInstantly = false)
    {
        if (m_bShowTooltip.Equals((b ? 1 : 0)))
            return;

        m_bShowTooltip = (b ? 1 : 0);
        m_target = (b ? m_target : null);

        iTween.Stop(gameObject);

        if (isInstantly) m_cDirection.ResetToBeginning(b ? 100000111 : 100000112);
        else m_cDirection.SetInit(b ? 100000111 : 100000112, true);
    }
}
