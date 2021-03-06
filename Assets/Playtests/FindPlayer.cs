﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Waypoints;
using System.Linq;

public class FindPlayer : MonoBehaviour
{
    GameObject target;
    List<Connection> Path;
    public WaypointGraph debug;
    public float m_repathTime = 1f;
    float lastRepath;
    int pathIndex = 0;
    Vector3 origin;

    private void Start()
    {
        target = GameObject.FindGameObjectWithTag("Player");
        origin = transform.position;
        Path = debug.QueryPath(transform.position, target.transform.position);
    }

    private void Update()
    {
        if (Path.Count > 0 && pathIndex < Path.Count)
        {
            var currentNode = debug.GetNode(Path[pathIndex].EndNodeIndex);
            transform.position = Vector3.MoveTowards(transform.position, currentNode.Position, 5 * Time.deltaTime);

            if (Vector3.Distance(transform.position, currentNode.Position) < 0.01)
            {
                pathIndex++;
            }
        }
    }

    private float TotalCost(List<Connection> connections)
    {
        return connections.Sum(c => c.Cost);
    }

    private void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 100, 40), "Restart"))
        {
            transform.position = origin;
            Path = debug.QueryPath(transform.position, target.transform.position);
            pathIndex = 0;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.grey;
        Node lastNode = new Node(transform.position); 
        if (Path != null)
        {
            for (int i = 0; i < Path.Count; i++)
            {
                Node node = debug.GetNode(Path[i].EndNodeIndex);
                Gizmos.DrawLine(lastNode.Position, node.Position);
                Gizmos.DrawCube(node.Position, Vector3.one * 0.5f);

                lastNode = node;
            }
        }
    }
}
