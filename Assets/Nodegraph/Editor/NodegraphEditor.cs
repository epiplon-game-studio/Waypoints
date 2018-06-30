using UnityEditor;
using UnityEngine;
using Nodegraph;

namespace Nodegraph.Editor
{
    [UnityEditor.CustomEditor(typeof(Nodegraph))]
    public class NodegraphEditor : UnityEditor.Editor
    {
        Ray click;
        GUIStyle clearBtn;
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (clearBtn == null)
            {
                clearBtn = new GUIStyle(GUI.skin.button);
                clearBtn.normal.textColor = Color.white;
                clearBtn.active.textColor = Color.white;
            }

            EditorGUILayout.Space();

            GUI.contentColor = Color.white;
            Rect rect = EditorGUILayout.GetControlRect();
            if (GUI.Button(rect, "Rebuild Graph"))
            {
                Nodegraph.Current.RebuildNodegraph();
                SceneView.RepaintAll();
            }
            
            rect = EditorGUILayout.GetControlRect();
            string btnLabel = Nodegraph.Current.isPlacingNode ? "Disable Node Placing" : "Enable Node Placing";
            if (GUI.Button(rect, btnLabel))
            {
                Nodegraph.Current.isPlacingNode = !Nodegraph.Current.isPlacingNode;
            }

            rect = EditorGUILayout.GetControlRect();
            GUI.contentColor = Color.red;
            if (GUI.Button(rect, "Clear Nodes", clearBtn))
            {
                Nodegraph.Current.ClearNodes();
                SceneView.RepaintAll();
            }
        }

        private void OnDisable()
        {
            Nodegraph.Current.isPlacingNode = false;
        }

        private void OnSceneGUI()
        {
            if (EditorWindow.mouseOverWindow == null)
                return;

            if (Nodegraph.Current.isPlacingNode)
            {
                int controlID = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(controlID);

                RaycastHit hitInfo;
                click = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                if (Physics.Raycast(click, out hitInfo, 1000, Nodegraph.Current.solidLayerMask))
                {
                    Handles.color = Color.yellow;
                    Handles.DrawWireCube(hitInfo.point, Vector3.one / 4f);
                    SceneView.RepaintAll();
                }

                switch (Event.current.type)
                {
                    case EventType.MouseDown:
                        // placing mode and mouse on Scene tab
                        if (EditorWindow.mouseOverWindow.titleContent.text == "Scene")
                        {
                            // grab mouse down
                            if (Event.current.button == 0)
                            {
                                if (Physics.Raycast(click, out hitInfo, 1000, Nodegraph.Current.solidLayerMask))
                                {
                                    Nodegraph.Current.AddNode(hitInfo.point);
                                }

                                Event.current.Use();
                            }
                        }
                        break;
                }
            }
        }
    }
}
