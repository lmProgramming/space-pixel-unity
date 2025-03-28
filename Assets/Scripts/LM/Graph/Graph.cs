using System.Collections.Generic;

namespace Ship.Modules.Graph
{
    public class Graph<T>
    {
        protected readonly Dictionary<T, List<T>> _adjacencyList = new();

        public void AddNode(T node)
        {
            if (!_adjacencyList.ContainsKey(node)) _adjacencyList[node] = new List<T>();
        }

        public virtual void AddEdge(T from, T to)
        {
            if (!_adjacencyList.ContainsKey(from) || !_adjacencyList.TryGetValue(to, out var value)) return;
            _adjacencyList[from].Add(to);
            value.Add(from);
        }

        public virtual void RemoveEdge(T from, T to)
        {
            if (!_adjacencyList.ContainsKey(from) || !_adjacencyList.TryGetValue(to, out var value)) return;
            _adjacencyList[from].Remove(to);
            value.Remove(from);
        }

        public List<T> GetConnectedNodes(T node)
        {
            return _adjacencyList.TryGetValue(node, out var value) ? value : new List<T>();
        }

        public virtual void RemoveNode(T node)
        {
            if (!_adjacencyList.ContainsKey(node)) return;
            _adjacencyList.Remove(node);

            foreach (var value in _adjacencyList) RemoveEdge(value.Key, node);
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