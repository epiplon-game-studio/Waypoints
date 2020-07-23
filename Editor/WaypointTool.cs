using System;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Graphs;
using UnityEngine;
using Waypoints;

[EditorTool("Waypoint Tool")]
public class WaypointTool : EditorTool
{
    static Texture2D btnUp, btnDown;
    static GUIContent gearsTex, plusTex, penTex,
        removeTex, clearTex, cancelTex, bulkTex, confirmTex;

    [SerializeField]
    Texture2D m_ToolIcon;
    GUIContent m_IconContent;

    WaypointGraph waypointGraph;
    bool isAvailable;
    NodegraphState nodegraphState = NodegraphState.None;

    private void OnEnable()
    {
        m_IconContent = new GUIContent()
        {
            image = Resources.Load<Texture2D>("waypoint-editor-tool"),
            text = "Waypoint Tool",
            tooltip = "Waypoint Tool"
        };

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

    public override GUIContent toolbarIcon
    {
        get { return m_IconContent; }
    }

    const float X_OFFSET = 35;

    public override bool IsAvailable()
    {
        waypointGraph = Selection.activeGameObject?.GetComponent<WaypointGraph>();
        isAvailable = !(waypointGraph == null);
        return isAvailable;
    }


    public override void OnToolGUI(EditorWindow window)
    {
        var rect = new Rect(10, 10, 30, 30);

        if (isAvailable)
        {
            var waypointSerialized = new SerializedObject(waypointGraph);
            var graphProperty = waypointSerialized.FindProperty("graph");

            EditorGUI.BeginChangeCheck();
            window.BeginWindows();

            if (graphProperty.objectReferenceValue == null)
            {
                if (GUI.Button(new Rect(10, 10, 120, 30), "Create new Graph"))
                {
                    var graphInstance = CreateInstance<Waypoints.Graph>();
                    string path = EditorUtility.SaveFilePanelInProject("Create Waypoint Graph", "New Graph", "asset", "Graph saved.");
                    if (!string.IsNullOrEmpty(path))
                    {
                        AssetDatabase.CreateAsset(graphInstance, path);
                        AssetDatabase.Refresh();
                        graphProperty.objectReferenceValue = graphInstance;
                        waypointSerialized.ApplyModifiedProperties();
                    }
                }
            }
            else
            {
                EditWaypoint(rect, graphProperty);
            }
            window.EndWindows();
            EditorGUI.EndChangeCheck();
        }
        else
        {
            EditorGUI.BeginChangeCheck();
            window.BeginWindows();

            GUI.Box(new Rect(10, 10, 200, 30), "No Waypoint selected.");

            window.EndWindows();
            EditorGUI.EndChangeCheck();
        }
    }

    private void EditWaypoint(Rect rect, SerializedProperty graphProperty)
    {
        if (GUI.Button(rect, plusTex, ButtonStyle(nodegraphState == NodegraphState.Placing)))
        {
            nodegraphState = NodegraphState.Placing;
        }
        rect.x += X_OFFSET;

        if (GUI.Button(rect, penTex, ButtonStyle(nodegraphState == NodegraphState.Editing)))
        {
            nodegraphState = NodegraphState.Editing;
        }
        rect.x += X_OFFSET;

        if (GUI.Button(rect, removeTex, ButtonStyle(nodegraphState == NodegraphState.Removing)))
        {
            nodegraphState = NodegraphState.Removing;
        }
        rect.x += X_OFFSET;

        if (GUI.Button(rect, clearTex))
        {
            if (EditorUtility.DisplayDialog("Clearing all the Waypoint Nodes", "This action will " +
                            "remove all the nodes from the currently selected Waypoint. Are you sure?",
                            "Yes", "No"))
            {
                // TODO: clear all the nodes
            }
        }
        rect.x += X_OFFSET;

        using (new Handles.DrawingScope(Color.green))
        {

        }
    }

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

}
