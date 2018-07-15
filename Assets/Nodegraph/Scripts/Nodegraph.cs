﻿using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nodegraph
{
    [ExecuteInEditMode]
    public class Nodegraph : MonoBehaviour
    {
        static List<Nodegraph> graphs = new List<Nodegraph>();

        public static Nodegraph Get(string label)
        {
            return graphs.FirstOrDefault(n => n.m_nodegraphLabel.Equals(label));
        }

        [SerializeField]
        private Graph graph;

        [Header("Node Settings")]
        public string m_nodegraphLabel;
        public float m_nodeMaximumDistance = 3f;
        public float m_nodeSize = 0.25f;
        [Tooltip("For removing nodes")]
        public float m_brushRadius = 3f;

        [Header("Editor")]
        public Color m_nodeColor = Color.cyan;
        public Color m_staticConnection = Color.cyan;
        public Color m_dynamicConnection = Color.yellow;

        [Space]
        public int m_bulkNodeDistanceGap = 3;
        public float m_bulkSpawnDistance = 4;

        [Space]
        public float m_nodeUpOffset = 0.3f;
        public bool m_alwaysShowNodes;
        public LayerMask solidLayerMask;
        [HideInInspector] public string movingObstacleTag;

        private void Awake()
        {
            graphs.Add(this);
        }

        private void OnDestroy()
        {
            graphs.Remove(this);
        }

        #region Node Operations

        public void AddNode(Vector3 position)
        {
            if (graph == null)
            {
                Debug.LogError("No Graph is set on the " + gameObject.name + " object.");
                return;
            }

            if (graph.AllNodes == null)
                graph.AllNodes = new List<Node>();

            var newNode = new Node(position + Vector3.up * m_nodeUpOffset);
            graph.AllNodes.Add(newNode);
        }

        public void ClearNodes()
        {
            graph.AllNodes.Clear();
        }

        public void RemoveNodes(Vector3 center)
        {
            if (graph == null)
            {
                Debug.LogError("No Graph is set on the " + gameObject.name + " object.");
                return;
            }

            if (graph.AllNodes.Count == 0)
            {
                Debug.LogWarning("No nodes to remove.");
                return;
            }

            graph.AllNodes.RemoveAll(n => Vector3.Distance(n.Position, center) <= m_brushRadius);
        }

        public Node GetNode(int index)
        {
            return graph.GetNode(index);
        }

        public List<Node> GetNodes()
        {
            return graph.AllNodes;
        }

        public void RebuildNodegraph()
        {
            bool hitBackfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;
            RaycastHit[] hits = new RaycastHit[8];

            for (int i = 0; i < graph.AllNodes.Count; i++)
            {
                var overlapped = Physics.OverlapSphere(graph.AllNodes[i].Position, m_nodeSize, solidLayerMask);
                if (overlapped.Count() > 0)
                {
                    graph.AllNodes[i].ConnectedNodes.Clear();
                    Debug.Log("Overlapped");
                    continue;
                }

                var connectedNodes = graph.AllNodes.Where(n => graph.AllNodes[i] != n)
                    .Where(n => Vector3.Distance(graph.AllNodes[i].Position, n.Position) < m_nodeMaximumDistance)
                    .Select(n =>
                    {
                        var connection = new Connection();
                        connection.StartNodeIndex = i;
                        connection.EndNodeIndex = graph.AllNodes.IndexOf(n);
                        connection.Cost = Vector3.Distance(graph.AllNodes[i].Position, n.Position);

                        // tries to find something between the nodes
                        var diff = (n.Position - graph.AllNodes[i].Position);
                        //if (Physics.Linecast(graph.AllNodes[i].Position, n.Position, out hit, solidLayerMask))
                        var n_hits = Physics.RaycastNonAlloc(graph.AllNodes[i].Position, diff.normalized, hits, diff.magnitude, solidLayerMask);
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

                graph.AllNodes[i].ConnectedNodes = connectedNodes.ToList();
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
            if (graph == null || graph.AllNodes.Count == 0)
                return new List<Connection>();

            DateTime clockStart = DateTime.Now;
            //==============

            float startMin = graph.AllNodes.Min(n => Vector3.Distance(start, n.Position));
            Node startNode = graph.AllNodes.Find(n => Vector3.Distance(start, n.Position) <= startMin);

            float endMin = graph.AllNodes.Min(n => Vector3.Distance(end, n.Position));
            Node endNode = graph.AllNodes.Find(n => Vector3.Distance(end, n.Position) <= endMin);

            List<Connection> path = new List<Connection>();
            List<Node> visited = new List<Node>();

            // create a temporary connection to the starting node
            var startConnection = new Connection() { EndNodeIndex = graph.AllNodes.IndexOf(startNode) };
            path = Search(startConnection, endNode, ref path, ref visited);

            //=============
            TimeSpan clockEnd = DateTime.Now - clockStart;
            Debug.Log("Operation took: " + clockEnd.TotalSeconds);
            Debug.Log("Search found " + path.Count + " nodes.");

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
            var node = graph.GetNode(connection.EndNodeIndex);
            path.Add(connection);
            visited.Add(node);

            foreach (var childConnect in node.ConnectedNodes.OrderBy(c => c.Cost + heuristic(c.EndNodeIndex, endNode)))
            {
                Node child = graph.GetNode(childConnect.EndNodeIndex);

                // don't let it go to the same path
                if (visited.Contains(child))
                    continue;
                else
                    path = Search(childConnect, endNode, ref path, ref visited);
            }

            var lastNode = graph.GetNode(path.Last().EndNodeIndex);
            if (lastNode != endNode)
                path.RemoveAt(path.Count - 1);

            return path;
        }

        private float heuristic(int nodeIndex, Node goal)
        {
            return Vector3.Distance(goal.Position, GetNode(nodeIndex).Position);
        }

        //public List<Connection> AstarSearch(Node start, Node goal)
        //{
        //    Queue<Node> frontier = new Queue<Node>();
        //    frontier.Enqueue(start);

        //    while (frontier.Count > 0)
        //    {
        //        var current = frontier.Dequeue();

        //        if (current == goal)
        //            break;

        //        foreach (var next in current.ConnectedNodes)
        //        {
        //            var nextNode = GetNode(next.EndNodeIndex);
        //        }
        //    }


        //    return null;
        //}

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
            if (graph)
            {
                if (graph.AllNodes != null)
                {
                    foreach (var n in graph.AllNodes)
                    {
                        Gizmos.color = m_nodeColor;
                        Gizmos.DrawCube(n.Position, Vector3.one * m_nodeSize);
                        foreach (var c in n.ConnectedNodes)
                        {
                            var node = graph.GetNode(c.EndNodeIndex);
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
        Removing
    }

#if UNITY_EDITOR
    [InitializeOnLoad]
    static class NodegraphHierarchy
    {
        static Texture icon;
        static GUIStyle iconStyle;

        static NodegraphHierarchy()
        {
            icon = Resources.Load<Texture>("nodegraph-icon");
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

            if (target.GetComponent<Nodegraph>() != null)
            {
                GUI.Label(selectionRect, new GUIContent(icon), iconStyle);
            }
        }
    }
#endif
}
