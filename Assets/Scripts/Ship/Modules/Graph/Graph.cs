using System.Collections.Generic;

namespace Ship.Modules.Graph
{
    public class Graph<T>
    {
        private readonly Dictionary<T, List<T>> _adjacencyList = new();
        private readonly T _centralNode;

        public Graph(T centralNode)
        {
            _centralNode = centralNode;
        }

        public void AddNode(T node)
        {
            if (!_adjacencyList.ContainsKey(node)) _adjacencyList[node] = new List<T>();
        }

        public void AddEdge(T from, T to)
        {
            if (!_adjacencyList.ContainsKey(from) || !_adjacencyList.TryGetValue(to, out var value)) return;
            _adjacencyList[from].Add(to);
            value.Add(from);
        }

        public void RemoveEdge(T from, T to)
        {
            if (!_adjacencyList.ContainsKey(from) || !_adjacencyList.TryGetValue(to, out var value)) return;
            _adjacencyList[from].Remove(to);
            value.Remove(from);
            if (Equals(from, _centralNode)) return;
            if (value.Count == 0) _adjacencyList.Remove(from);
        }

        public List<T> GetConnectedNodes(T node)
        {
            return _adjacencyList.TryGetValue(node, out var value) ? value : new List<T>();
        }

        public bool ContainsNode(T node)
        {
            return _adjacencyList.ContainsKey(node);
        }

        public List<T> GetAllNodes()
        {
            return new List<T>(_adjacencyList.Keys);
        }
    }
}