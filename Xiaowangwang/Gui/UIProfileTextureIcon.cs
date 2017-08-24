using UnityEngine;
using System.Collections;

/// <summary>
/// <para>name : UIProfileTextureIcon</para>
/// <para>describe : NGUI UITexture를 사용하여, 받아온 Texture2D를 적용합니다.</para>
/// <para>tag : leeyonghyeon@corp.netease.com</para>
/// </summary>
[RequireComponent(typeof(UITexture))]
public class UIProfileTextureIcon : MonoBehaviour
{
    private UITexture m_uiTexture = null;
    private int m_containTextureNativeID = 0;

    void Awake()
    {
        if (m_uiTexture == null)
            m_uiTexture = gameObject.GetComponent<UITexture>();
        Release();
    }

    /// <summary>
    /// <para>name : ReturnTexture</para>
    /// <para>describe : ProfileTxManager.ReturnTexture Callback에서 받아온 Texture2D를 적용합니다.</para>
    /// <para>tag : leeyonghyeon@corp.netease.com</para>
    /// </summary>
    public void ReturnTexture(Texture2D texture)
    {
        if(this != null)
        {
            if(m_uiTexture == null)
                m_uiTexture = gameObject.GetComponent<UITexture>();

            if(m_uiTexture != null)
            {
                bool isTextureExists = texture != null;
                m_uiTexture.gameObject.SetActive(isTextureExists);

                if(isTextureExists)
                {
                    if(m_containTextureNativeID.Equals(texture.GetNativeTextureID()) == false)
                    {
                        m_containTextureNativeID = texture.GetNativeTextureID();

                        Texture2D createTex = Instantiate(texture) as Texture2D;
                        if(createTex != null)
                            m_uiTexture.mainTexture = createTex;
                    }
                }
            }
        }

        texture = null;
    }

    public void OnOffColor(bool b)
    {
        if(m_uiTexture == null)
            m_uiTexture = gameObject.GetComponent<UITexture>();

        if(m_uiTexture != null)
            m_uiTexture.color = (b) ? Color.white : new Color(1, 1, 1, 0.5f);
    }

    public void Release()
    {
        Texture2D basicTexture = ProfileTxManager.instance.GetBasicIcon();
        if (basicTexture != null)
        {
            m_uiTexture.mainTexture = Instantiate(basicTexture) as Texture2D;
            m_containTextureNativeID = basicTexture.GetNativeTextureID();
        }

        basicTexture = null;
    }

    void OnDestroy()
    {
        m_uiTexture = null;
    }
}
