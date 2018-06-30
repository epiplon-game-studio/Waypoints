using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nodegraph.Editor
{
    public class NodegraphWindow : EditorWindow
    {
        [MenuItem("Window/Nodegraph")]
        static void Init()
        {
            NodegraphWindow window = (NodegraphWindow)GetWindow(typeof(NodegraphWindow));
            window.titleContent = new GUIContent("Nodegraph");
            window.Show();
        }

        private void OnGUI()
        {
            Rect rect = EditorGUILayout.GetControlRect();
            rect.y += EditorGUIUtility.singleLineHeight;
            if (GUI.Button(rect, "Rebuild Graph"))
            {
                Debug.Log("Graph is rebuilding.");
            }

            rect.y += EditorGUIUtility.singleLineHeight;
            string btnLabel = Nodegraph.Current.isPlacingNode ? "Disable Node Placing" : "Enable Node Placing";

            if (GUI.Button(rect, btnLabel))
            {
                Nodegraph.Current.isPlacingNode = !Nodegraph.Current.isPlacingNode;
            }
   
        }
    }
}
