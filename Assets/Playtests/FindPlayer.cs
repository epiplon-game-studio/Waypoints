using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Waypoints;
using System.Linq;

public class FindPlayer : MonoBehaviour
{
    GameObject target;
    List<Connection> Path;
    WaypointGraph debug;
    public float m_repathTime = 1f;
    float lastRepath;
    int pathIndex = 0;
    Vector3 origin;
    bool traveling = false;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player");
        debug = WaypointGraph.Get("debug");
        origin = transform.position;
        Path = new List<Connection>();
    }

    private void Update()
    {
        if (Time.time > lastRepath + m_repathTime && !traveling)
        {
            Path = debug.QueryPath(transform.position, target.transform.position);
            //if (TotalCost(newPath) < TotalCost(Path) || Path.Count == 0)
            //    Path = newPath;

            lastRepath = Time.time;
            pathIndex = 0;
        }

        if (Path.Count > 0 && pathIndex < Path.Count)
        {
            var currentNode = debug.GetNode(Path[pathIndex].EndNodeIndex);
            transform.position = Vector3.MoveTowards(transform.position, currentNode.Position, 5 * Time.deltaTime);
            traveling = true;

            if (Vector3.Distance(transform.position, currentNode.Position) < 0.01)
            {
                pathIndex++;
                traveling = false;
            }
        }
    }

    private float TotalCost(List<Connection> connections)
    {
        return connections.Sum(c => c.Cost);
    }

    private void OnGUI()
    {
        //if (GUI.Button(new Rect(0, 0, 100, 40), "Find"))
        //{
        //    Path = debug.QueryPath(transform.position, Player.transform.position);
        //}
        if (GUI.Button(new Rect(0, 0, 100, 40), "Restart"))
        {
            transform.position = origin;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Node lastNode = null;
        if (Path != null)
        {
            for (int i = 0; i < Path.Count; i++)
            {

                Node node = debug.GetNode(Path[i].EndNodeIndex);
                if (i == 0)
                {
                    Gizmos.DrawLine(transform.position, node.Position);
                }
                else
                {
                    Gizmos.DrawLine(lastNode.Position, node.Position);
                }
                Gizmos.DrawCube(node.Position, Vector3.one * 0.5f);

                lastNode = node;
            }
        }
    }
}
