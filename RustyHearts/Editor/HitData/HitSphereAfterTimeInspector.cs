using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Magi;

[CustomEditor(typeof(HitSphereAfterTime))]
public class HitSphereAfterTimeInspector : Editor
{
    GameObject debugHitSphere_;
    HitSphereAfterTime hitSphereAfterTime_;

    void OnEnable()
    {
        hitSphereAfterTime_ = target as HitSphereAfterTime;

        debugHitSphere_ = Resources.LoadAssetAtPath("Assets/Magi/Magi3D/Editor/Prefabs/DebugHitSphere.prefab", typeof(GameObject)) as GameObject;
    }

    void OnDisable()
    {
    }

    private void EditCollider()
    {
        GameObject igo = Instantiate(debugHitSphere_) as GameObject;
        DebugMagiMecanimHit debugMagiMecanimHit = igo.GetComponent<DebugMagiMecanimHit>();
        if (debugMagiMecanimHit != null)
        {
            debugMagiMecanimHit.SetMagiMecanimEvent(hitSphereAfterTime_.mecanimEvent, hitSphereAfterTime_.transform);
        }
    }

    private void SetMagiMecanimEvent(Collider newCollider)
    {
        Transform transform = newCollider.gameObject.transform;

        Vector3 modelScale = new Vector3(1.0f / hitSphereAfterTime_.transform.localScale.x, 1.0f / hitSphereAfterTime_.transform.localScale.y, 1.0f / hitSphereAfterTime_.transform.localScale.z);
        Vector3 localScale = Vector3.Scale(transform.localScale, modelScale);

        if (newCollider.GetType() == typeof(SphereCollider))
        {
            MagiMecanimEvent mecanimEvent = new MagiMecanimEvent();

            SphereCollider sphereCollider = newCollider as SphereCollider;
            mecanimEvent.HitData.paramType = MagiMecanimEventHit.EMagiHitType.Sphere;
            mecanimEvent.HitData.sphereParam = new MagiMecanimEventHit.MagiSphere();
            mecanimEvent.HitData.sphereParam.euler = transform.rotation.eulerAngles;
            mecanimEvent.HitData.sphereParam.center = hitSphereAfterTime_.transform.InverseTransformPoint(transform.position + sphereCollider.center);

            float scale = Mathf.Max(Mathf.Max(localScale.x, localScale.y), localScale.z);
            mecanimEvent.HitData.sphereParam.radius = scale * sphereCollider.radius;

            mecanimEvent.HitData.hitCount = hitSphereAfterTime_.mecanimEvent.HitData.hitCount;
            mecanimEvent.HitData.hitInterval = hitSphereAfterTime_.mecanimEvent.HitData.hitInterval;

            hitSphereAfterTime_.mecanimEvent = mecanimEvent;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Collider newCollider = null;

        EditorGUILayout.BeginHorizontal();
        newCollider = EditorGUILayout.ObjectField(newCollider, typeof(Collider), true) as Collider;

        if (newCollider != null)
        {
            SetMagiMecanimEvent(newCollider);
        }

        if (GUILayout.Button("Edit Collider") == true)
        {
            EditCollider();
        }
        EditorGUILayout.EndHorizontal();
    }
}
