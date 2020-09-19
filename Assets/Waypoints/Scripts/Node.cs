using System;
using System.Collections;
using UnityEngine;

namespace Waypoints
{
    /// <summary>
    /// A node in the graph, with a position and all possible connections
    /// </summary>
    [Serializable]
    public class Node
    {
        public Vector3 Position;
        public Connection[] ConnectedNodes;

        public Node(Vector3 position)
        {
            Position = position;
            ConnectedNodes = new Connection[0];
        }
    }

    /// <summary>
    /// A connection with next node information
    /// </summary>
    [Serializable]
    public struct Connection
    {
        public int StartNodeIndex;      // index of the start node
        public int EndNodeIndex;        // index of the end node
        public float Cost;              // Cost to reach the node (distance)
        public ConnectionType Type;     // see the type description

        public static bool operator ==(Connection c1, Connection c2)
        {
            if (ReferenceEquals(c1, null))
            {
                return ReferenceEquals(c2, null);
            }

            return c1.EndNodeIndex.Equals(c2.EndNodeIndex);
        }

        public static bool operator !=(Connection c1, Connection c2)
        {
            if (ReferenceEquals(c1, null))
            {
                return ReferenceEquals(c2, null);
            }

            return !c1.EndNodeIndex.Equals(c2.EndNodeIndex);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public struct ConnectionComparer : IComparer
    {
        public Node endNode;
        public delegate Node GetNode(int index);
        public GetNode getNode;

        public int Compare(object x, object y)
        {
            var a = (Connection)x;
            var b = (Connection)y;
            var aCost = a.Cost + WaypointGraph.Heuristic(getNode(a.EndNodeIndex).Position, endNode);
            var bCost = b.Cost + WaypointGraph.Heuristic(getNode(b.EndNodeIndex).Position, endNode);
            if (aCost > bCost)
                return 1;
            if (aCost < bCost)
                return -1;

            return 0;
        }
    }

    // Null: no connection - should be removed
    // Static: free to move
    // Dynamic: can be blocked by moving obstacle, like doors or platforms
    public enum ConnectionType { Null, Static, Dynamic }
}
