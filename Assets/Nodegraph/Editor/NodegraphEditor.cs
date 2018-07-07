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
        SerializedProperty movingObstacleTag;
        Nodegraph nodegraph;

        private void OnEnable()
        {
            graphProperty = serializedObject.FindProperty("graph");
            movingObstacleTag = serializedObject.FindProperty("movingObstacleTag");
            nodegraph = (Nodegraph)target;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            movingObstacleTag.stringValue = EditorGUILayout.TagField("Obstacle Tag", movingObstacleTag.stringValue);

            if (clearBtn == null)
            {
                clearBtn = new GUIStyle(GUI.skin.button);
                clearBtn.normal.textColor = Color.white;
                clearBtn.active.textColor = Color.white;
            }

            if (graphProperty.objectReferenceValue != null)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Current Action: " + nodegraph.State);

                GUI.contentColor = Color.white;
                Rect rect = EditorGUILayout.GetControlRect();
                if (GUI.Button(rect, "Rebuild Graph"))
                {
                    nodegraph.RebuildNodegraph();
                    SceneView.RepaintAll();
                }

                switch (nodegraph.State)
                {
                    case NodegraphState.None:
                        rect = EditorGUILayout.GetControlRect();
                        if (GUI.Button(rect, "Place Nodes"))
                        {
                            nodegraph.State = NodegraphState.Placing;
                        }

                        rect = EditorGUILayout.GetControlRect();
                        if (GUI.Button(rect, "Remove Nodes"))
                        {
                            nodegraph.State = NodegraphState.Removing;
                        }

                        rect = EditorGUILayout.GetControlRect();
                        GUI.contentColor = Color.red;
                        if (GUI.Button(rect, "Clear Nodes", clearBtn))
                        {
                            nodegraph.ClearNodes();
                            SceneView.RepaintAll();
                        }
                        break;
                    case NodegraphState.Placing:
                    case NodegraphState.Removing:
                        rect = EditorGUILayout.GetControlRect();
                        if (GUI.Button(rect, "Cancel Actions"))
                        {
                            nodegraph.State = NodegraphState.None;
                            EditorUtility.SetDirty(graphProperty.objectReferenceValue);
                        }
                        break;
                }
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

        }

        private void OnDisable()
        {
            nodegraph.State = NodegraphState.None;
        }

        private void OnSceneGUI()
        {
            if (EditorWindow.mouseOverWindow == null)
                return;

            switch (nodegraph.State)
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
            if (Physics.Raycast(click, out hitInfo, 1000, nodegraph.solidLayerMask))
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
                            if (Physics.Raycast(click, out hitInfo, 1000, nodegraph.solidLayerMask))
                            {
                                nodegraph.AddNode(hitInfo.point);
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
            if (Physics.Raycast(click, out hitInfo, 1000, nodegraph.solidLayerMask))
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(hitInfo.point, Vector3.up, nodegraph.m_brushRadius);
                Handles.DrawWireDisc(hitInfo.point, Vector3.right, nodegraph.m_brushRadius);
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
                            nodegraph.RemoveNodes(hitInfo.point);
                            Event.current.Use();
                        }
                    }
                    break;
            }
        }
    }
}
