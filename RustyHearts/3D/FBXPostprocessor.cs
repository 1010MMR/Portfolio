using UnityEditor;
using UnityEngine;

class FBXPostprocessor : AssetPostprocessor
{
    void DisableRenderObject(Transform ob)
    {
        foreach (Transform o in ob.transform)
        {
            if (o.name.ToLower().Contains("boxbone")||
                o.name.ToLower().Contains("cb_")||
                o.name.ToLower().Contains("edge_"))
            {
                MeshFilter mf = o.GetComponent<MeshFilter>();
                if (mf != null)
                {
                    mf.renderer.enabled = false;
                }
            }
            DisableRenderObject(o);
        }
    }

    void ExportRootMotion(GameObject go)
    {
    }

    void MakeDummyList(GameObject go)
    {
        foreach (Transform o in go.transform)
        {
            if (o.name.ToLower().Contains("dummy_"))
            {
            }
            MakeDummyList(go);
        }
    }

    // This method is called immediately after importing an FBX.
    void OnPostprocessModel(GameObject go)
    {
        DisableRenderObject(go.transform);
        ExportRootMotion(go);
    }
}
