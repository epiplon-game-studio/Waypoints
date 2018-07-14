using UnityEditor;
using UnityEngine;
using Nodegraph;

namespace Nodegraph.Editor
{
    [UnityEditor.CustomEditor(typeof(Nodegraph))]
    public class NodegraphEditor : UnityEditor.Editor
    {
        GUIStyle clearBtn;
        Texture gearsTex, plusTex, penTex, removeTex, clearTex, cancelTex, bulkTex, confirmTex;

        SerializedProperty graphProperty;
        SerializedProperty movingObstacleTag;
        Nodegraph nodegraph;

        Node currentNode = null;
        int selectedIndex = -1;

        bool showNodes = true;
        float labelSize = 40f;

        BulkControl bulkControl = null;

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
            bulkTex = Resources.Load<Texture>("bulk");
            confirmTex = Resources.Load<Texture>("confirm");
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
                var offset = rect.width /= 6;

                switch (nodegraph.State)
                {
                    case NodegraphState.None:
                        if (GUI.Button(rect, new GUIContent(gearsTex, "Rebuild Graph")))
                        {
                            nodegraph.RebuildNodegraph();
                            SceneView.RepaintAll();
                        }

                        rect.x += offset;
                        if (GUI.Button(rect, new GUIContent(plusTex, "Place Nodes")))
                        {
                            nodegraph.State = NodegraphState.Placing;
                        }

                        rect.x += offset;
                        if (GUI.Button(rect, new GUIContent(bulkTex, "Bulk Node Placement")))
                        {
                            nodegraph.State = NodegraphState.Bulk;
                            var position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * nodegraph.m_bulkSpawnDistance;
                            bulkControl = new BulkControl(position);
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
                    case NodegraphState.Bulk:
                        if (GUI.Button(rect, new GUIContent(confirmTex, "Create Bulk Nodes")))
                        {
                            Vector3Int extension = bulkControl.Extension;

                            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);
                            Handles.color = Color.green;
                            for (int x = -extension.x; x < extension.x; x += nodegraph.m_bulkNodeDistanceGap)
                            {
                                for (int y = -extension.y; y < extension.y; y += nodegraph.m_bulkNodeDistanceGap)
                                {
                                    for (int z = -extension.z; z < extension.z; z += nodegraph.m_bulkNodeDistanceGap)
                                    {
                                        nodegraph.AddNode(new Vector3(x, y, z) + bulkControl.Position);
                                    }
                                }
                            }

                            nodegraph.State = NodegraphState.None;
                            EditorUtility.SetDirty(graphProperty.objectReferenceValue);

                            currentNode = null;
                            selectedIndex = -1;
                        }
                        goto case NodegraphState.Placing;
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
                case NodegraphState.Bulk:
                    BulkNodes();
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

        void BulkNodes()
        {
            EditorGUI.BeginChangeCheck();
            float handleSize = HandleUtility.GetHandleSize(bulkControl.Position);
            bulkControl.Position = Handles.PositionHandle(bulkControl.Position, Quaternion.identity);
            bulkControl.Scale = Handles.ScaleHandle(bulkControl.Scale, bulkControl.Position, Quaternion.identity, handleSize + 1);
            Handles.color = Color.cyan;
            Handles.DrawWireCube(bulkControl.Position, bulkControl.Scale);

            Vector3Int extension = bulkControl.Extension;

            int controlID = GUIUtility.GetControlID(FocusType.Keyboard);
            Handles.color = Color.green;
            for (int x = -extension.x; x < extension.x; x += nodegraph.m_bulkNodeDistanceGap)
            {
                for (int y = -extension.y; y < extension.y; y += nodegraph.m_bulkNodeDistanceGap)
                {
                    for (int z = -extension.z; z < extension.z; z += nodegraph.m_bulkNodeDistanceGap)
                    {
                        Handles.CubeHandleCap(controlID, new Vector3(x, y, z) + bulkControl.Position, Quaternion.identity, 1f, EventType.Repaint);
                    }
                }
            }

            EditorGUI.EndChangeCheck();
            SceneView.RepaintAll();
        }
    }
}
