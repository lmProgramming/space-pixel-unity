using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LM.Graph
{
    public class BiCohesionGraph<T> : Graph<T>
    {
        private readonly T _centralNode;

        public BiCohesionGraph(T centralNode)
        {
            _centralNode = centralNode;
        }

        public event Action<List<T>> OnNodesRemovedDueToUnreachability;

        public override void AddEdge(T from, T to)
        {
            if (!AdjacencyList.ContainsKey(from)) AddNode(from);
            if (!AdjacencyList.ContainsKey(to)) AddNode(to);

            if (!AdjacencyList[from].Contains(to)) AdjacencyList[from].Add(to);
            if (!AdjacencyList[to].Contains(from)) AdjacencyList[to].Add(from);
        }

        public override void RemoveEdge(T from, T to)
        {
            Debug.Log($"[BiCohesionGraph] RemoveEdge called: {from} <-> {to}");

            if (AdjacencyList.TryGetValue(from, out var value)) value.Remove(to);
            if (AdjacencyList.TryGetValue(to, out var value1)) value1.Remove(from);

            RemoveUnreachableNodes();
        }

        private HashSet<T> GetReachableNodes()
        {
            var reachable = new HashSet<T>();
            var queue = new Queue<T>();

            if (!AdjacencyList.ContainsKey(_centralNode))
                return reachable;

            queue.Enqueue(_centralNode);
            reachable.Add(_centralNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (!AdjacencyList.TryGetValue(current, out var neighbors))
                    continue;

                foreach (var neighbor in neighbors.Where(neighbor => !reachable.Contains(neighbor)))
                {
                    reachable.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }

            return reachable;
        }

        private void RemoveUnreachableNodes()
        {
            var reachableNodes = GetReachableNodes();
            var allNodes = new List<T>(AdjacencyList.Keys);

            Debug.Log($"[BiCohesionGraph] Reachability check - Central: {_centralNode}, " +
                      $"Reachable: [{string.Join(", ", reachableNodes)}], " +
                      $"All nodes: [{string.Join(", ", allNodes)}]");

            var unreachableNodes = allNodes.Where(node => !reachableNodes.Contains(node) && !Equals(node, _centralNode))
                .ToList();

            if (unreachableNodes.Count > 0)
            {
                Debug.Log($"[BiCohesionGraph] Removing {unreachableNodes.Count} unreachable node(s): [{string.Join(", ", unreachableNodes)}]");
            }

            foreach (var node in unreachableNodes) RemoveNodeWithoutReachabilityCheck(node);

            if (unreachableNodes.Count > 0) OnNodesRemovedDueToUnreachability?.Invoke(unreachableNodes);
        }

        private void RemoveNodeWithoutReachabilityCheck(T node)
        {
            if (!AdjacencyList.TryGetValue(node, out var value)) return;

            var connectedNodes = new List<T>(value);
            foreach (var connectedNode in connectedNodes)
            {
                if (AdjacencyList.TryGetValue(node, out var nodeEdges))
                    nodeEdges.Remove(connectedNode);
                if (AdjacencyList.TryGetValue(connectedNode, out var connectedEdges))
                    connectedEdges.Remove(node);
            }

            AdjacencyList.Remove(node);
        }

        public override void RemoveNode(T node)
        {
            if (!AdjacencyList.TryGetValue(node, out var value)) return;

            var connectedNodes = new List<T>(value);
            foreach (var connectedNode in connectedNodes)
            {
                RemoveEdge(node, connectedNode);
                if (!Equals(connectedNode, _centralNode) && AdjacencyList.ContainsKey(connectedNode) &&
                    AdjacencyList[connectedNode].Count == 0)
                    AdjacencyList.Remove(connectedNode);
            }

            AdjacencyList.Remove(node);
        }
    }
}