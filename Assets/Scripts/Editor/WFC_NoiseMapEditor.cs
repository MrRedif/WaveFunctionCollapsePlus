using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WFC_NoiseMap))]
public class WFC_NoiseMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        if (GUILayout.Button("Update"))
        {
            var obj = target as WFC_NoiseMap;
            obj.seed = Random.Range(0, int.MaxValue);
            obj.UpdateNoiseMap();
        }



        serializedObject.Update();
        serializedObject.ApplyModifiedProperties();
    }
}
