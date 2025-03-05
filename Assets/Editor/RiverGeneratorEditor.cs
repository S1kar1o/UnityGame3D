using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RiverGenerator))]
public class RiverGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        RiverGenerator riverGen = (RiverGenerator)target;

        if (DrawDefaultInspector())
        {
            if (riverGen.autoUpdate)
            {
                riverGen.riverGenerator();

            }

        }




        if (GUILayout.Button("Generate"))
        {
            riverGen.riverGenerator();
        }




    }
}
