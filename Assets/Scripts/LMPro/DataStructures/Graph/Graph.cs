using System.Collections.Generic;

namespace LMPro.DataStructures.Graph
{
    public class Graph<T>
    {
        protected readonly Dictionary<T, List<T>> AdjacencyList = new();

        public void AddNode(T node)
        {
            if (!AdjacencyList.ContainsKey(node)) AdjacencyList[node] = new List<T>();
        }

        public virtual void AddEdge(T from, T to)
        {
            if (!AdjacencyList.ContainsKey(from) || !AdjacencyList.TryGetValue(to, out var value)) return;
            AdjacencyList[from].Add(to);
            value.Add(from);
        }

        public virtual void RemoveEdge(T from, T to)
        {
            if (!AdjacencyList.ContainsKey(from) || !AdjacencyList.TryGetValue(to, out var value)) return;
            AdjacencyList[from].Remove(to);
            value.Remove(from);
        }

        public List<T> GetConnectedNodes(T node)
        {
            return AdjacencyList.TryGetValue(node, out var value) ? value : new List<T>();
        }

        public virtual void RemoveNode(T node)
        {
            if (!AdjacencyList.TryGetValue(node, out _)) return;

            foreach (var kvp in AdjacencyList) kvp.Value.Remove(node);

            AdjacencyList.Remove(node);
        }

        public bool ContainsNode(T node)
        {
            return AdjacencyList.ContainsKey(node);
        }

        public List<T> GetAllNodes()
        {
            return new List<T>(AdjacencyList.Keys);
        }
    }
}