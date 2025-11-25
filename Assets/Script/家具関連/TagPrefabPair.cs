
using System;
using UnityEngine;

[Serializable]
public class TagPrefabPair
{
    [Tooltip("ボタンに付けたタグ名（例：F_Chair / F_Table など）")]
    public string tag;

    [Tooltip("このタグに対応する家具のPrefab")]
    public GameObject prefab;
}
