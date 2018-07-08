using UnityEditor;
using UnityEngine;
using Nodegraph;

namespace Nodegraph.Editor
{
    [UnityEditor.CustomEditor(typeof(Nodegraph))]
    public class NodegraphEditor : UnityEditor.Editor
    {
        GUIStyle clearBtn;
        Texture gearsTex, plusTex, penTex, removeTex, clearTex, cancelTex;

        SerializedProperty graphProperty;
        SerializedProperty movingObstacleTag;
        Nodegraph nodegraph;

        Node currentNode = null;
        int selectedIndex = -1;

        bool showNodes = true;
        float labelSize = 40f;

        private void OnEnable()
        {
            graphProperty = serializedObject.FindProperty("graph");
            movingObstacleTag = serializedObject.FindProperty("movingObstacleTag");
            nodegraph = (Nodegraph)target;

            gearsTex = Resources.Load<Texture>("gears");
            plusTex = Resources.Load<Texture>("plus");
            penTex = Resources.Load<Texture>("pen");
            removeTex = Resources.Load<Texture>("remove");
            clearTex = Resources.Load<Texture>("clear");
            cancelTex = Resources.Load<Texture>("cancel");
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
                Rect rect = EditorGUILayout.GetControlRect(false, 50);
                var offset = rect.width /= 5;
                if (GUI.Button(rect, new GUIContent(gearsTex, "Rebuild Graph")))
                {
                    nodegraph.RebuildNodegraph();
                    SceneView.RepaintAll();
                }

                switch (nodegraph.State)
                {
                    case NodegraphState.None:
                        rect.x += offset;
                        if (GUI.Button(rect, new GUIContent(plusTex, "Place Nodes")))
                        {
                            nodegraph.State = NodegraphState.Placing;
                        }

                        rect.x += offset;
                        if (GUI.Button(rect, new GUIContent(penTex, "Edit Nodes")))
                        {
                            nodegraph.State = NodegraphState.Editing;
                        }

                        rect.x += offset;
                        if (GUI.Button(rect, new GUIContent(removeTex, "Remove Nodes")))
                        {
                            nodegraph.State = NodegraphState.Removing;
                        }

                        rect.x += offset;
                        if (GUI.Button(rect, new GUIContent(clearTex, "Clear Nodes"), clearBtn))
                        {
                            nodegraph.ClearNodes();
                            SceneView.RepaintAll();

                            currentNode = null;
                            selectedIndex = -1;
                        }
                        break;
                    case NodegraphState.Placing:
                    case NodegraphState.Removing:
                    case NodegraphState.Editing:
                        rect = EditorGUILayout.GetControlRect(false, 50);
                        if (GUI.Button(rect, new GUIContent("Cancel Actions", cancelTex)))
                        {
                            nodegraph.State = NodegraphState.None;
                            EditorUtility.SetDirty(graphProperty.objectReferenceValue);

                            currentNode = null;
                            selectedIndex = -1;
                        }
                        break;
                }

                // node list
                EditorGUILayout.Space();
                showNodes = EditorGUILayout.Foldout(showNodes, "Node list");
                if (showNodes)
                {
                    var nodelist = nodegraph.GetNodes();

                    for (int i = 0; i < nodelist.Count; i++)
                    {
                        rect = EditorGUILayout.GetControlRect();

                        Color original = GUI.color;
                        GUI.color = selectedIndex == i ? Color.cyan : Color.white;

                        nodelist[i].Position = EditorGUI.Vector3Field(rect, string.Format("#{0}", i), nodelist[i].Position);
                        GUI.color = original;
                    }

                    SceneView.RepaintAll();
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }

        }

        private void OnSceneGUI()
        {
            if (EditorWindow.mouseOverWindow == null)
                return;

            EditorGUI.BeginChangeCheck();

            switch (nodegraph.State)
            {
                case NodegraphState.Placing:
                    PlacingNodes();
                    break;
                case NodegraphState.Removing:
                    RemovingNodes();
                    break;
                case NodegraphState.Editing:
                    var nodelist = nodegraph.GetNodes();
                    for (int i = 0; i < nodelist.Count; i++)
                    {
                        int controlID = GUIUtility.GetControlID(FocusType.Keyboard);
                        if (currentNode == nodelist[i])
                            currentNode.Position = Handles.PositionHandle(currentNode.Position, Quaternion.identity);
                        else
                            Handles.CubeHandleCap(controlID, nodelist[i].Position, Quaternion.identity, 2f, EventType.Layout);

                        if (HandleUtility.nearestControl == controlID && Event.current.GetTypeForControl(controlID) == EventType.MouseDown)
                        {
                            currentNode = nodelist[i];
                        }
                    }

                    break;
            }

            EditorGUI.EndChangeCheck();
        }

        private void OnDisable()
        {
            nodegraph.State = NodegraphState.None;
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
