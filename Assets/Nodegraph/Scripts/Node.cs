using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nodegraph
{
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

    [Serializable]
    public struct Connection
    {
        public int Index;   // Node index
        public float Cost;  // Cost to reach the node (distance)

        public static bool operator ==(Connection c1, Connection c2)
        {
            if (ReferenceEquals(c1, null))
            {
                return ReferenceEquals(c2, null);
            }

            return c1.Index.Equals(c2.Index);
        }

        public static bool operator !=(Connection c1, Connection c2)
        {
            if (ReferenceEquals(c1, null))
            {
                return ReferenceEquals(c2, null);
            }

            return !c1.Index.Equals(c2.Index);
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
}
