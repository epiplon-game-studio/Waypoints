using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Nodegraph
{
    public class BulkControl
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Vector3Int Extension
        {
            get
            {
                return new Vector3Int(Mathf.FloorToInt(Scale.x / 2), Mathf.FloorToInt(Scale.y / 2), Mathf.FloorToInt(Scale.z / 2));
            }
        }

        public BulkControl(Vector3 position)
        {
            Position = position;
            Scale = Vector3.one;
        }
    }
}
