using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nodegraph
{
    /// <summary>
    /// A node in the graph, with a position and all possible connections
    /// </summary>
    [Serializable]
    public class Node
    {
        public Vector3 Position;
        public List<Connection> ConnectedNodes;

        public Node(Vector3 position)
        {
            Position = position;
            ConnectedNodes = new List<Connection>();
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

    // Null: no connection - should be removed
    // Static: free to move
    // Dynamic: can be blocked by moving obstacle, like doors or platforms
    public enum ConnectionType { Null, Static, Dynamic }
}
