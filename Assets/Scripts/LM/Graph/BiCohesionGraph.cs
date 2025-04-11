using System.Collections.Generic;

namespace Ship.Modules.Graph
{
    public class BiCohesionGraph<T> : Graph<T>
    {
        private readonly T _centralNode;

        public BiCohesionGraph(T centralNode)
        {
            _centralNode = centralNode;
        }

        public override void AddEdge(T from, T to)
        {
            if (!AdjacencyList.ContainsKey(from)) AddNode(from);
            if (!AdjacencyList.ContainsKey(to)) AddNode(to);

            if (!AdjacencyList[from].Contains(to)) AdjacencyList[from].Add(to);
            if (!AdjacencyList[to].Contains(from)) AdjacencyList[to].Add(from);
        }

        public override void RemoveEdge(T from, T to)
        {
            if (AdjacencyList.TryGetValue(from, out var value)) value.Remove(to);
            if (AdjacencyList.TryGetValue(to, out var value1)) value1.Remove(from);

            if (!Equals(from, _centralNode) && AdjacencyList[from].Count == 0) AdjacencyList.Remove(from);
            if (!Equals(to, _centralNode) && AdjacencyList[to].Count == 0) AdjacencyList.Remove(to);
        }

        public override void RemoveNode(T node)
        {
            if (!AdjacencyList.TryGetValue(node, out var value)) return;

            var connectedNodes = new List<T>(value);
            foreach (var connectedNode in connectedNodes)
            {
                RemoveEdge(node, connectedNode);
                if (!Equals(connectedNode, _centralNode) && AdjacencyList[connectedNode].Count == 0)
                    AdjacencyList.Remove(connectedNode);
            }

            AdjacencyList.Remove(node);
        }
    }
}