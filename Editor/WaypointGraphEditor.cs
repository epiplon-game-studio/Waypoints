using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Waypoints;

namespace Waypoints.Editor
{
    [UnityEditor.CustomEditor(typeof(WaypointGraph))]
    public class WaypointGraphEditor : UnityEditor.Editor
    {
        static Texture2D btnUp, btnDown;
        static GUIContent gearsTex, plusTex, penTex,
            removeTex, clearTex, cancelTex, bulkTex, confirmTex;

        SerializedProperty graphProperty, movingObstacleTag, autoRebuild;
        WaypointGraph graph;

        Node currentNode = null;
        //int selectedIndex = -1;

        List<Node> selectedNodes = new List<Node>();

        bool showNodes = true;
        float labelSize = 40f;

        BulkControl bulkControl = null;

        const float X_OFFSET = 35;
        const float CUBE_SIZE = 2f;

        private void OnEnable()
        {
            graphProperty = serializedObject.FindProperty("graph");
            movingObstacleTag = serializedObject.FindProperty("movingObstacleTag");
            autoRebuild = serializedObject.FindProperty("m_autoRebuild");
            graph = (WaypointGraph)target;

            gearsTex = LoadContent("gears", "Rebuild Graph");
            plusTex = LoadContent("plus", "Place Waypoint Node");
            penTex = LoadContent("pen", "Edit Waypoint Node");
            removeTex = LoadContent("remove", "Remove Waypoint Node");
            clearTex = LoadContent("clear", "Clear Waypoints");
            //cancelTex = Resources.Load<Texture>("cancel");
            //bulkTex = Resources.Load<Texture>("bulk");
            //confirmTex = Resources.Load<Texture>("confirm");
            btnUp = Resources.Load<Texture2D>("button-up");
            btnDown = Resources.Load<Texture2D>("button-down");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            movingObstacleTag.stringValue = EditorGUILayout.TagField("Obstacle Tag", movingObstacleTag.stringValue);

            if (graphProperty.objectReferenceValue != null)
            {
                Color originalColor = GUI.color;
                EditorGUILayout.Space();

                GUI.contentColor = Color.white;
                Rect rect = EditorGUILayout.GetControlRect(false, 50);
                rect = EditorGUILayout.GetControlRect(false, 50);
                if (GUI.Button(rect, new GUIContent("Rebuild Graph")))
                {
                    graph.RebuildNodegraph();
                    SceneView.RepaintAll();
                }

                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private void OnSceneGUI()
        {
            Tools.current = Tool.None;

            Handles.BeginGUI();
            var rect = new Rect(10, 10, 30, 30);
            Toolbar(rect, graphProperty);
            Handles.EndGUI();

            EditorGUI.BeginChangeCheck();

            int id = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(id);
            switch (graph.State)
            {
                case NodegraphState.Placing:
                    PlacingNodes(id);
                    EditorGUI.EndChangeCheck();
                    break;
                case NodegraphState.Bulk:
                    BulkNodes(id);
                    EditorGUI.EndChangeCheck();
                    break;
                case NodegraphState.Removing:
                    RemovingNodes(id);
                    EditorGUI.EndChangeCheck();
                    break;
                case NodegraphState.Editing:
                    EditingNodes();
                    break;
            }

            SceneView.lastActiveSceneView.Repaint();
        }

        private void Toolbar(Rect rect, SerializedProperty graphProperty)
        {
            if (GUI.Button(rect, plusTex, ButtonStyle(graph.State == NodegraphState.Placing)))
            {
                graph.State = NodegraphState.Placing;
            }
            rect.x += X_OFFSET;

            if (GUI.Button(rect, penTex, ButtonStyle(graph.State == NodegraphState.Editing)))
            {
                graph.State = NodegraphState.Editing;
            }
            rect.x += X_OFFSET;

            if (GUI.Button(rect, removeTex, ButtonStyle(graph.State == NodegraphState.Removing)))
            {
                graph.State = NodegraphState.Removing;
            }
            rect.x += X_OFFSET;

            if (GUI.Button(rect, clearTex))
            {
                if (EditorUtility.DisplayDialog("Clearing all the Waypoint Nodes", "This action will " +
                                "remove all the nodes from the currently selected Waypoint. Are you sure?",
                                "Yes", "No"))
                {
                    graph.ClearNodes();
                }
            }
            rect.x += X_OFFSET;
        }

        #region Actions
        void PlacingNodes(int id)
        {
            RaycastHit hitInfo;
            Ray click = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(click, out hitInfo, 1000, graph.solidLayerMask, graph.m_hitTriggers))
            {
                Handles.color = Color.yellow;
                Handles.DrawWireCube(hitInfo.point, Vector3.one / 4f);

                var nodes = graph.GetNodes();
                Handles.color = Color.white;
                for (int ni = 0; ni < nodes.Count; ni++)
                {
                    if (Vector3.Distance(hitInfo.point, nodes[ni].Position) <= graph.m_nodeMaximumDistance)
                        Handles.DrawDottedLine(hitInfo.point, nodes[ni].Position, 2);
                }
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
                            if (Physics.Raycast(click, out hitInfo, 1000, graph.solidLayerMask))
                            {
                                graph.AddNode(hitInfo.point);
                                if (autoRebuild.boolValue)
                                    graph.RebuildNodegraph();
                            }

                            Event.current.Use();
                        }
                    }
                    break;
                case EventType.KeyDown:
                    if (Event.current.keyCode == KeyCode.Space)
                    {
                        Camera sceneCamera = SceneView.lastActiveSceneView.camera;
                        var position = sceneCamera.transform.position + sceneCamera.transform.forward;
                        graph.AddNode(position);
                        Event.current.Use();
                    }
                    break;
            }
        }

        void RemovingNodes(int id)
        {
            RaycastHit hitInfo;
            Ray click = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Physics.Raycast(click, out hitInfo, 1000, graph.solidLayerMask))
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(hitInfo.point, Vector3.up, graph.m_brushRadius);
                Handles.DrawWireDisc(hitInfo.point, Vector3.right, graph.m_brushRadius);

                var nodes = graph.GetNodes();
                Handles.color = Color.red;
                for (int ni = 0; ni < nodes.Count; ni++)
                {
                    if (Vector3.Distance(hitInfo.point, nodes[ni].Position) <= graph.m_brushRadius)
                        Handles.CubeHandleCap(0, nodes[ni].Position, Quaternion.identity, graph.m_nodeSize, EventType.Repaint);
                }
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
                            graph.RemoveNodes(hitInfo.point);
                            if(autoRebuild.boolValue)
                                graph.RebuildNodegraph();

                            Event.current.Use();
                        }
                    }
                    break;
            }
        }

        void BulkNodes(int id)
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
            for (int x = -extension.x; x < extension.x; x += graph.m_bulkNodeDistanceGap)
            {
                for (int y = -extension.y; y < extension.y; y += graph.m_bulkNodeDistanceGap)
                {
                    for (int z = -extension.z; z < extension.z; z += graph.m_bulkNodeDistanceGap)
                    {
                        Handles.CubeHandleCap(controlID, new Vector3(x, y, z) + bulkControl.Position, Quaternion.identity, 1f, EventType.Repaint);
                    }
                }
            }

            EditorGUI.EndChangeCheck();
        }

        void EditingNodes()
        {
            var nodelist = graph.GetNodes();
            for (int i = 0; i < nodelist.Count; i++)
            {
                // select handle control
                int id = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddDefaultControl(id);

                Handles.CubeHandleCap(id, nodelist[i].Position, Quaternion.identity, CUBE_SIZE, EventType.Layout);

                if (selectedNodes.Count > 0)
                {
                    Vector3 median = Vector3.zero;
                    for (int p = 0; p < selectedNodes.Count; p++)
                        median += selectedNodes[p].Position;

                    median /= selectedNodes.Count;

                    EditorGUI.BeginChangeCheck();
                    Vector3 newPosition = Handles.PositionHandle(median, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int p = 0; p < selectedNodes.Count; p++)
                        {
                            Vector3 offset = newPosition - median;
                            selectedNodes[p].Position += offset;
                        }
                    }
                }


                if (HandleUtility.nearestControl == id
                    && Event.current.GetTypeForControl(id) == EventType.MouseDown
                    && Event.current.button == 0) // left mouse button
                {

                    if (!Event.current.shift)
                        selectedNodes.Clear();

                    selectedNodes.Add(nodelist[i]);
                }
            }
        }

        #endregion
        private void OnDisable()
        {
            graph.State = NodegraphState.None;
            Tools.current = Tool.Move;
        }

        #region Helpers 
        GUIStyle ButtonStyle(bool active)
        {
            var style = EditorStyles.miniButton;
            style.fixedHeight = style.fixedWidth = 30;
            style.normal.background = active ? btnDown : btnUp;

            return style;
        }

        GUIContent LoadContent(string textureName, string tooltip)
        {
            return new GUIContent(Resources.Load<Texture>(textureName), tooltip);
        }
        #endregion
    }
}
