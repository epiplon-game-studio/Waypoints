using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Waypoints
{
    [ExecuteInEditMode]
    public class WaypointGraph : MonoBehaviour
    {
        static List<WaypointGraph> graphs = new List<WaypointGraph>();

        public static WaypointGraph Get(string label)
        {
            return graphs.FirstOrDefault(n => n.m_nodegraphLabel.Equals(label));
        }

        [SerializeField, HideInInspector]
        private Graph _graph;
        public Graph MainGraph
        {
            get
            {
                if (_graph == null)
                    _graph = new Graph();

                return _graph;
            }
        }

        [Header("Node Settings")]
        public string m_nodegraphLabel;
        public float m_nodeMaximumDistance = 3f;
        public float m_nodeSize = 0.25f;
        public QueryTriggerInteraction m_hitTriggers;
        [Tooltip("For removing nodes")]
        public float m_brushRadius = 3f;
        public bool m_autoRebuild = true;

        [Header("Editor")]
        public Color m_nodeColor = Color.cyan;
        public Color m_staticConnection = Color.cyan;
        public Color m_dynamicConnection = Color.yellow;
        public bool m_showLog;

        [Space]
        public int m_bulkNodeDistanceGap = 3;
        public float m_bulkSpawnDistance = 4;

        [Space]
        public float m_nodeUpOffset = 0.3f;
        public bool m_alwaysShowNodes;
        public LayerMask solidLayerMask;
        [HideInInspector] public string movingObstacleTag;

        ConnectionComparer connectionComparer;

        private void Awake()
        {
            graphs.Add(this);
            connectionComparer = new ConnectionComparer() { getNode = GetNode };
        }

        public void PrintLog(string message)
        {
            if (m_showLog)
                Debug.Log(message);
        }

        private void OnDestroy()
        {
            graphs.Remove(this);
        }

        #region Node Operations

        public void AddNode(Vector3 position)
        {
            var newNode = new Node(position + Vector3.up * m_nodeUpOffset);
            MainGraph.AllNodes.Add(newNode);
        }

        public void ClearNodes()
        {
            MainGraph.AllNodes.Clear();
        }

        public void RemoveNode(int i)
        {
            if (MainGraph.AllNodes.Count == 0)
            {
                Debug.LogWarning("No nodes to remove.");
                return;
            }

            MainGraph.AllNodes.RemoveAt(i);
        }

        public Node GetNode(int index)
        {
            return MainGraph.GetNode(index);
        }

        public List<Node> GetNodes()
        {
            return MainGraph.AllNodes;
        }

        public void RebuildNodegraph()
        {
            bool hitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            RaycastHit[] hits = new RaycastHit[8];

            for (int i = 0; i < MainGraph.AllNodes.Count; i++)
            {
                var overlapped = Physics.OverlapSphere(MainGraph.AllNodes[i].Position, m_nodeSize, solidLayerMask, m_hitTriggers);
                if (overlapped.Count() > 0)
                {
                    MainGraph.AllNodes[i].ConnectedNodes = new Connection[0];
                    PrintLog("Found node overlap.");
                    continue;
                }

                var connectedNodes = MainGraph.AllNodes.Where(n => MainGraph.AllNodes[i] != n)
                    .Where(n => Vector3.Distance(MainGraph.AllNodes[i].Position, n.Position) < m_nodeMaximumDistance)
                    .Select(n =>
                    {
                        var connection = new Connection();
                        connection.StartNodeIndex = i;
                        connection.EndNodeIndex = MainGraph.AllNodes.IndexOf(n);
                        connection.Cost = Vector3.Distance(MainGraph.AllNodes[i].Position, n.Position);

                        // tries to find something between the nodes
                        var diff = (n.Position - MainGraph.AllNodes[i].Position);
                        var n_hits = Physics.RaycastNonAlloc(MainGraph.AllNodes[i].Position, diff.normalized,
                            hits, diff.magnitude, solidLayerMask, m_hitTriggers);
                        if (n_hits > 0)
                        {
                            connection.Type = ConnectionType.Dynamic;
                            for (int h = 0; h < n_hits; h++)
                            {
                                if (!hits[h].collider.CompareTag(movingObstacleTag))
                                {
                                    connection.Type = ConnectionType.Null;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            connection.Type = ConnectionType.Static;
                        }


                        return connection;
                    })
                    .Where(c => c.Type != ConnectionType.Null);

                MainGraph.AllNodes[i].ConnectedNodes = connectedNodes.ToArray();
            }

            Physics.queriesHitBackfaces = hitBackfaces;
        }

        /// <summary>
        /// Tries to find the shortest path to the target
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="end">Final position</param>
        /// <returns>Path</returns>
        public List<Connection> QueryPath(Vector3 start, Vector3 end)
        {
            if (MainGraph.AllNodes.Count == 0)
                return new List<Connection>();

            DateTime clockStart = DateTime.Now;
            //==============

            Node startNode = FindClosestNode(start);
            Node endNode = FindClosestNode(end);

            List<Connection> path = new List<Connection>();
            List<Node> visited = new List<Node>();

            // could not find a path 
            if (startNode == null || endNode == null)
                return path;

            // create a temporary connection to the starting node
            var startConnection = new Connection() { EndNodeIndex = MainGraph.AllNodes.IndexOf(startNode) };
            path = Search(startConnection, endNode, ref path, ref visited);

            //=============
            TimeSpan clockEnd = DateTime.Now - clockStart;
            PrintLog("Operation took: " + clockEnd.TotalSeconds);
            PrintLog("Search found " + path.Count + " nodes.");

            return path;
        }


        /// <summary>
        /// Executes the pathfinding recursive search function
        /// </summary>
        /// <param name="node">Current Node</param>
        /// <param name="endNode">Target node to be reached</param>
        /// <param name="path"></param>
        /// <param name="visited"></param>
        /// <returns></returns>
        private List<Connection> Search(Connection connection, Node endNode, ref List<Connection> path, ref List<Node> visited)
        {
            var node = MainGraph.GetNode(connection.EndNodeIndex);
            path.Add(connection);
            visited.Add(node);

            connectionComparer.endNode = endNode;
            Array.Sort(node.ConnectedNodes, connectionComparer);
            foreach (var childConnect in node.ConnectedNodes)
            {
                Node child = MainGraph.GetNode(childConnect.EndNodeIndex);

                // don't let it go to the same path
                if (visited.Contains(child))
                    continue;
                else
                    path = Search(childConnect, endNode, ref path, ref visited);
            }

            var lastNode = MainGraph.GetNode(path[path.Count - 1].EndNodeIndex);
            if (lastNode != endNode)
                path.RemoveAt(path.Count - 1);

            return path;
        }

        public static float Heuristic(Vector3 nodePosition, Node goal)
        {
            return Vector3.Distance(goal.Position, nodePosition);
        }

        private Node FindClosestNode(Vector3 position)
        {
            bool hitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            Node closestNode = null;
            for (int i = 0; i < MainGraph.AllNodes.Count; i++)
            {
                // didn't hit a solid surface
                if (!Physics.Linecast(position, MainGraph.AllNodes[i].Position, solidLayerMask))
                {
                    if (closestNode == null)
                        closestNode = MainGraph.AllNodes[i];
                    else
                    {
                        if (Vector3.Distance(MainGraph.AllNodes[i].Position, position)
                            < Vector3.Distance(closestNode.Position, position))
                            closestNode = MainGraph.AllNodes[i];
                    }
                }
            }

            Physics.queriesHitBackfaces = hitBackfaces;

            return closestNode;
        }

        #endregion

#if UNITY_EDITOR
        [HideInInspector]
        public NodegraphState State = NodegraphState.None;

        private void OnDrawGizmosSelected()
        {
            if (m_alwaysShowNodes)
                return;

            DrawNodegraph();
        }

        private void OnDrawGizmos()
        {
            if (m_alwaysShowNodes)
                DrawNodegraph();
        }

        void DrawNodegraph()
        {
            if (MainGraph)
            {
                if (MainGraph.AllNodes != null)
                {
                    foreach (var n in MainGraph.AllNodes)
                    {
                        Gizmos.color = m_nodeColor;
                        Gizmos.DrawCube(n.Position, Vector3.one * m_nodeSize);
                        foreach (var c in n.ConnectedNodes)
                        {
                            var node = MainGraph.GetNode(c.EndNodeIndex);
                            if (node != null)
                            {
                                switch (c.Type)
                                {
                                    case ConnectionType.Null:
                                        Gizmos.color = Color.red; // this shouldn't happen
                                        break;
                                    case ConnectionType.Static:
                                        Gizmos.color = m_staticConnection;
                                        break;
                                    case ConnectionType.Dynamic:
                                        Gizmos.color = m_dynamicConnection;
                                        break;
                                }

                                Gizmos.DrawLine(n.Position, node.Position);
                            }

                        }
                    }
                }

            }
        }
#endif
    }

    public enum NodegraphState
    {
        None,
        Bulk,
        Placing,
        Editing,
        Removing,
        Clearing
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    static class NodegraphHierarchy
    {
        static Texture icon;
        static GUIStyle iconStyle;

        static NodegraphHierarchy()
        {
            icon = Resources.Load<Texture>("waypoint-icon");
            if (iconStyle == null)
            {
                iconStyle = new GUIStyle();
                iconStyle.alignment = TextAnchor.MiddleRight;
            }

            EditorApplication.hierarchyWindowItemOnGUI += HighlightItems;
        }

        private static void HighlightItems(int instanceID, Rect selectionRect)
        {
            var target = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (target == null)
                return;

            if (target.GetComponent<WaypointGraph>() != null)
            {
                GUI.Label(selectionRect, new GUIContent(icon), iconStyle);
            }
        }
    }
#endif
}
