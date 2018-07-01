using UnityEditor;
using UnityEngine;
using Nodegraph;

namespace Nodegraph.Editor
{
    [UnityEditor.CustomEditor(typeof(Nodegraph))]
    public class NodegraphEditor : UnityEditor.Editor
    {
        GUIStyle clearBtn;

        SerializedProperty graphProperty;

        private void OnEnable()
        {
            graphProperty = serializedObject.FindProperty("graph");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (clearBtn == null)
            {
                clearBtn = new GUIStyle(GUI.skin.button);
                clearBtn.normal.textColor = Color.white;
                clearBtn.active.textColor = Color.white;
            }

            if (graphProperty.objectReferenceValue != null)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Current Action: " + Nodegraph.Current.State);

                GUI.contentColor = Color.white;
                Rect rect = EditorGUILayout.GetControlRect();
                if (GUI.Button(rect, "Rebuild Graph"))
                {
                    Nodegraph.Current.RebuildNodegraph();
                    SceneView.RepaintAll();
                }

                switch (Nodegraph.Current.State)
                {
                    case NodegraphState.None:
                        rect = EditorGUILayout.GetControlRect();
                        if (GUI.Button(rect, "Place Nodes"))
                        {
                            Nodegraph.Current.State = NodegraphState.Placing;
                        }

                        rect = EditorGUILayout.GetControlRect();
                        if (GUI.Button(rect, "Remove Nodes"))
                        {
                            Nodegraph.Current.State = NodegraphState.Removing;
                        }

                        rect = EditorGUILayout.GetControlRect();
                        GUI.contentColor = Color.red;
                        if (GUI.Button(rect, "Clear Nodes", clearBtn))
                        {
                            Nodegraph.Current.ClearNodes();
                            SceneView.RepaintAll();
                        }
                        break;
                    case NodegraphState.Placing:
                    case NodegraphState.Removing:
                        rect = EditorGUILayout.GetControlRect();
                        if (GUI.Button(rect, "Cancel Actions"))
                        {
                            Nodegraph.Current.State = NodegraphState.None;
                            EditorUtility.SetDirty(graphProperty.objectReferenceValue);
                        }
                        break;
                }
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

        }

        private void OnDisable()
        {
            Nodegraph.Current.State = NodegraphState.None;
        }

        private void OnSceneGUI()
        {
            if (EditorWindow.mouseOverWindow == null)
                return;

            switch (Nodegraph.Current.State)
            {
                case NodegraphState.Placing:
                    PlacingNodes();
                    break;
                case NodegraphState.Removing:
                    RemovingNodes();
                    break;
            }

        }

        void PlacingNodes()
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            RaycastHit hitInfo;
            Ray click = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
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

        void RemovingNodes()
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);

            RaycastHit hitInfo;
            Ray click = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(click, out hitInfo, 1000, Nodegraph.Current.solidLayerMask))
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(hitInfo.point, Vector3.up, Nodegraph.Current.m_brushRadius);
                Handles.DrawWireDisc(hitInfo.point, Vector3.right, Nodegraph.Current.m_brushRadius);
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
                            Nodegraph.Current.RemoveNodes(hitInfo.point);
                            Event.current.Use();
                        }
                    }
                    break;
            }
        }
    }
}
