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
            if (!_adjacencyList.ContainsKey(from)) AddNode(from);
            if (!_adjacencyList.ContainsKey(to)) AddNode(to);

            if (!_adjacencyList[from].Contains(to)) _adjacencyList[from].Add(to);
            if (!_adjacencyList[to].Contains(from)) _adjacencyList[to].Add(from);
        }

        public override void RemoveEdge(T from, T to)
        {
            if (_adjacencyList.TryGetValue(from, out var value)) value.Remove(to);
            if (_adjacencyList.TryGetValue(to, out var value1)) value1.Remove(from);

            if (!Equals(from, _centralNode) && _adjacencyList[from].Count == 0) _adjacencyList.Remove(from);
            if (!Equals(to, _centralNode) && _adjacencyList[to].Count == 0) _adjacencyList.Remove(to);
        }

        public override void RemoveNode(T node)
        {
            if (!_adjacencyList.TryGetValue(node, out var value)) return;

            var connectedNodes = new List<T>(value);
            foreach (var connectedNode in connectedNodes)
            {
                RemoveEdge(node, connectedNode);
                if (!Equals(connectedNode, _centralNode) && _adjacencyList[connectedNode].Count == 0)
                    _adjacencyList.Remove(connectedNode);
            }

            _adjacencyList.Remove(node);
        }
    }
}