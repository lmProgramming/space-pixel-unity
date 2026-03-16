using NUnit.Framework;

namespace LMPro.Graph.Tests
{
    [TestFixture]
    public class GraphTests
    {
        [SetUp]
        public void SetUp()
        {
            _graph = new Graph<string>();
        }

        private Graph<string> _graph;

        [Test]
        public void AddNode_AddsNodeToGraph()
        {
            _graph.AddNode("A");

            Assert.IsTrue(_graph.ContainsNode("A"));
        }

        [Test]
        public void AddNode_DuplicateNode_DoesNotThrow()
        {
            _graph.AddNode("A");

            Assert.DoesNotThrow(() => _graph.AddNode("A"));
            Assert.IsTrue(_graph.ContainsNode("A"));
        }

        [Test]
        public void ContainsNode_ReturnsFalse_ForNonExistentNode()
        {
            Assert.IsFalse(_graph.ContainsNode("NonExistent"));
        }

        [Test]
        public void AddEdge_RequiresBothNodesToExist()
        {
            // AddEdge in base Graph requires nodes to already exist
            _graph.AddNode("A");
            _graph.AddNode("B");

            _graph.AddEdge("A", "B");

            Assert.Contains("B", _graph.GetConnectedNodes("A"));
            Assert.Contains("A", _graph.GetConnectedNodes("B"));
        }

        [Test]
        public void GetConnectedNodes_ReturnsEmptyList_ForNodeWithNoConnections()
        {
            _graph.AddNode("A");

            var connections = _graph.GetConnectedNodes("A");

            Assert.IsNotNull(connections);
            Assert.AreEqual(0, connections.Count);
        }

        [Test]
        public void GetConnectedNodes_ReturnsEmptyList_ForNonExistentNode()
        {
            var connections = _graph.GetConnectedNodes("NonExistent");

            Assert.IsNotNull(connections);
            Assert.AreEqual(0, connections.Count);
        }

        [Test]
        public void RemoveEdge_RemovesConnectionBetweenNodes()
        {
            _graph.AddNode("A");
            _graph.AddNode("B");
            _graph.AddEdge("A", "B");

            _graph.RemoveEdge("A", "B");

            Assert.IsFalse(_graph.GetConnectedNodes("A").Contains("B"));
            Assert.IsFalse(_graph.GetConnectedNodes("B").Contains("A"));
        }

        [Test]
        public void RemoveNode_RemovesNodeFromGraph()
        {
            _graph.AddNode("A");

            _graph.RemoveNode("A");

            Assert.IsFalse(_graph.ContainsNode("A"));
        }

        [Test]
        public void RemoveNode_RemovesAllEdgesToNode()
        {
            _graph.AddNode("A");
            _graph.AddNode("B");
            _graph.AddNode("C");
            _graph.AddEdge("A", "B");
            _graph.AddEdge("B", "C");

            _graph.RemoveNode("B");

            Assert.IsFalse(_graph.GetConnectedNodes("A").Contains("B"));
            Assert.IsFalse(_graph.GetConnectedNodes("C").Contains("B"));
        }

        [Test]
        public void GetAllNodes_ReturnsAllNodes()
        {
            _graph.AddNode("A");
            _graph.AddNode("B");
            _graph.AddNode("C");

            var allNodes = _graph.GetAllNodes();

            Assert.AreEqual(3, allNodes.Count);
            Assert.Contains("A", allNodes);
            Assert.Contains("B", allNodes);
            Assert.Contains("C", allNodes);
        }

        [Test]
        public void GetAllNodes_ReturnsEmptyList_ForEmptyGraph()
        {
            var allNodes = _graph.GetAllNodes();

            Assert.IsNotNull(allNodes);
            Assert.AreEqual(0, allNodes.Count);
        }
    }
}