using System.Collections.Generic;
using UnityEngine;

namespace Bygd
{
    public static class RouteGraph
    {
        private const float ConnectionRadius = 50f;

        public static List<Vector3> FindRoute(Vector3 from, Vector3 to)
        {
            // Collect all nodes: stations + waypoints
            var nodes = new List<Vector3>();
            var nodeNames = new List<string>();

            foreach (var kvp in BygdPlugin.Stations)
            {
                nodes.Add(kvp.Value);
                nodeNames.Add("@" + kvp.Key);
            }

            foreach (var kvp in BygdPlugin.Waypoints)
            {
                nodes.Add(kvp.Value);
                nodeNames.Add("#" + kvp.Key);
            }

            // Add start and end points
            int startIdx = nodes.Count;
            nodes.Add(from);
            nodeNames.Add("__start__");

            int endIdx = nodes.Count;
            nodes.Add(to);
            nodeNames.Add("__end__");

            int n = nodes.Count;

            // Build graph: connect nodes within radius
            var adj = new List<List<(int idx, float dist)>>();
            for (int i = 0; i < n; i++)
                adj.Add(new List<(int, float)>());

            for (int i = 0; i < n; i++)
            {
                for (int j = i + 1; j < n; j++)
                {
                    float edgeDist = Vector3.Distance(nodes[i], nodes[j]);
                    if (edgeDist <= ConnectionRadius)
                    {
                        adj[i].Add((j, edgeDist));
                        adj[j].Add((i, edgeDist));
                    }
                }
            }

            // Dijkstra
            float[] dist = new float[n];
            int[] prev = new int[n];
            bool[] visited = new bool[n];

            for (int i = 0; i < n; i++)
            {
                dist[i] = float.MaxValue;
                prev[i] = -1;
            }
            dist[startIdx] = 0;

            for (int step = 0; step < n; step++)
            {
                int u = -1;
                float minDist = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (!visited[i] && dist[i] < minDist)
                    {
                        minDist = dist[i];
                        u = i;
                    }
                }

                if (u == -1 || u == endIdx) break;
                visited[u] = true;

                foreach (var (v, w) in adj[u])
                {
                    float newDist = dist[u] + w;
                    if (newDist < dist[v])
                    {
                        dist[v] = newDist;
                        prev[v] = u;
                    }
                }
            }

            // Route not found
            if (prev[endIdx] == -1)
                return null;

            // Reconstruct path
            var path = new List<Vector3>();
            int cur = endIdx;
            while (cur != -1)
            {
                path.Add(nodes[cur]);
                cur = prev[cur];
            }
            path.Reverse();

            // Remove start point (Lox is already there)
            if (path.Count > 1)
                path.RemoveAt(0);

            return path;
        }
    }
}
