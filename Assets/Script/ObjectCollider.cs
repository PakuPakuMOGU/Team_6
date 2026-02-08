using UnityEngine;
using System.Collections.Generic;

public class ObjectCollider : MonoBehaviour
{
    void Start()
    {
        // ★ 子オブジェクトの MeshFilter だけ取得
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        List<CombineInstance> combineList = new List<CombineInstance>();

        foreach (MeshFilter mf in meshFilters)
        {
            // ★ 親自身の MeshFilter は無視
            if (mf.gameObject == this.gameObject) continue;

            if (mf.sharedMesh == null) continue;

            CombineInstance ci = new CombineInstance();
            ci.mesh = mf.sharedMesh;

            // ★ 子のローカル座標を親のローカル空間に変換
            ci.transform = this.transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;

            combineList.Add(ci);

            // ★ 子は非表示にする
            mf.gameObject.SetActive(false);
        }

        if (combineList.Count == 0)
        {
            Debug.LogWarning("結合できるメッシュがありません");
            return;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.CombineMeshes(combineList.ToArray(), true, true);

        MeshFilter myMF = GetComponent<MeshFilter>();
        if (myMF == null) myMF = gameObject.AddComponent<MeshFilter>();
        myMF.sharedMesh = combinedMesh;

        MeshRenderer myMR = GetComponent<MeshRenderer>();
        if (myMR == null) myMR = gameObject.AddComponent<MeshRenderer>();

        // ★ 最初の子のマテリアルをコピー
        foreach (MeshFilter mf in meshFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                myMR.sharedMaterial = mr.sharedMaterial;
                break;
            }
        }

        combinedMesh.RecalculateBounds();
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateTangents();
    }
}