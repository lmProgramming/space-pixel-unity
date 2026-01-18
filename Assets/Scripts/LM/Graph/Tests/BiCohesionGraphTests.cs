using System.Collections.Generic;
using NUnit.Framework;

namespace LM.Graph.Tests.LM.Graph.Tests
{
    [TestFixture]
    public class BiCohesionGraphTests
    {
        [SetUp]
        public void SetUp()
        {
            _graph = new BiCohesionGraph<string>(CentralNode);
        }

        private BiCohesionGraph<string> _graph;
        private const string CentralNode = "Command";

        [Test]
        public void AddNode_AddsNodeToGraph()
        {
            _graph.AddNode("ModuleA");

            Assert.IsTrue(_graph.ContainsNode("ModuleA"));
        }

        [Test]
        public void AddEdge_ConnectsTwoNodes()
        {
            _graph.AddEdge(CentralNode, "ModuleA");

            Assert.IsTrue(_graph.ContainsNode(CentralNode));
            Assert.IsTrue(_graph.ContainsNode("ModuleA"));
            Assert.Contains("ModuleA", _graph.GetConnectedNodes(CentralNode));
            Assert.Contains(CentralNode, _graph.GetConnectedNodes("ModuleA"));
        }

        [Test]
        public void RemoveEdge_DisconnectsTwoNodes()
        {
            _graph.AddEdge(CentralNode, "ModuleA");

            _graph.RemoveEdge(CentralNode, "ModuleA");

            Assert.IsFalse(_graph.GetConnectedNodes(CentralNode).Contains("ModuleA"));
        }

        [Test]
        public void RemoveEdge_RemovesUnreachableNode_WhenDisconnectedFromCentral()
        {
            // Setup: Command -- ModuleA
            _graph.AddEdge(CentralNode, "ModuleA");

            List<string> removedNodes = null;
            _graph.OnNodesRemovedDueToUnreachability += nodes => removedNodes = nodes;

            // Act: Remove the only connection
            _graph.RemoveEdge(CentralNode, "ModuleA");

            // Assert: ModuleA should be removed (no longer reachable from Command)
            Assert.IsFalse(_graph.ContainsNode("ModuleA"));
            Assert.IsTrue(_graph.ContainsNode(CentralNode)); // Central node always stays

            // Event should have fired with ModuleA
            Assert.IsNotNull(removedNodes, "Event should have fired for single disconnected node");
            Assert.Contains("ModuleA", removedNodes);
        }

        [Test]
        public void RemoveEdge_RemovesEntireDisconnectedSubgraph()
        {
            // Setup: Command -- ModuleA -- ModuleB -- EngineC
            _graph.AddEdge(CentralNode, "ModuleA");
            _graph.AddEdge("ModuleA", "ModuleB");
            _graph.AddEdge("ModuleB", "EngineC");

            // Act: Cut between ModuleA and ModuleB (simulating ship cut in half)
            _graph.RemoveEdge("ModuleA", "ModuleB");

            // Assert: ModuleB and EngineC should be removed (unreachable from Command)
            Assert.IsTrue(_graph.ContainsNode(CentralNode));
            Assert.IsTrue(_graph.ContainsNode("ModuleA")); // Still connected to Command
            Assert.IsFalse(_graph.ContainsNode("ModuleB")); // Disconnected subgraph
            Assert.IsFalse(_graph.ContainsNode("EngineC")); // Disconnected subgraph
        }

        [Test]
        public void RemoveEdge_KeepsNodesConnectedToCentralViaDifferentPath()
        {
            // Setup: Command -- ModuleA -- ModuleB
            //                \-- ModuleC --/
            // (ModuleB has two paths to Command)
            _graph.AddEdge(CentralNode, "ModuleA");
            _graph.AddEdge("ModuleA", "ModuleB");
            _graph.AddEdge(CentralNode, "ModuleC");
            _graph.AddEdge("ModuleC", "ModuleB");

            // Act: Remove one path to ModuleB
            _graph.RemoveEdge("ModuleA", "ModuleB");

            // Assert: All nodes should still be present (ModuleB reachable via ModuleC)
            Assert.IsTrue(_graph.ContainsNode(CentralNode));
            Assert.IsTrue(_graph.ContainsNode("ModuleA"));
            Assert.IsTrue(_graph.ContainsNode("ModuleB"));
            Assert.IsTrue(_graph.ContainsNode("ModuleC"));
        }

        [Test]
        public void RemoveEdge_CentralNodeNeverRemoved()
        {
            // Setup: Only central node with no connections
            _graph.AddNode(CentralNode);

            // The central node should always remain
            Assert.IsTrue(_graph.ContainsNode(CentralNode));
        }

        [Test]
        public void RemoveEdge_LargeDisconnectedSubgraph_AllRemoved()
        {
            // Setup: Command -- A -- B -- C -- D -- E
            _graph.AddEdge(CentralNode, "A");
            _graph.AddEdge("A", "B");
            _graph.AddEdge("B", "C");
            _graph.AddEdge("C", "D");
            _graph.AddEdge("D", "E");

            // Act: Cut at the root
            _graph.RemoveEdge(CentralNode, "A");

            // Assert: Only Command remains
            Assert.IsTrue(_graph.ContainsNode(CentralNode));
            Assert.IsFalse(_graph.ContainsNode("A"));
            Assert.IsFalse(_graph.ContainsNode("B"));
            Assert.IsFalse(_graph.ContainsNode("C"));
            Assert.IsFalse(_graph.ContainsNode("D"));
            Assert.IsFalse(_graph.ContainsNode("E"));
        }

        [Test]
        public void RemoveEdge_BranchingSubgraph_AllBranchesRemoved()
        {
            // Setup:       B
            //             /
            // Command -- A -- C
            //             \
            //              D -- E
            _graph.AddEdge(CentralNode, "A");
            _graph.AddEdge("A", "B");
            _graph.AddEdge("A", "C");
            _graph.AddEdge("A", "D");
            _graph.AddEdge("D", "E");

            // Act: Cut between Command and A
            _graph.RemoveEdge(CentralNode, "A");

            // Assert: Only Command remains, entire tree removed
            Assert.IsTrue(_graph.ContainsNode(CentralNode));
            Assert.IsFalse(_graph.ContainsNode("A"));
            Assert.IsFalse(_graph.ContainsNode("B"));
            Assert.IsFalse(_graph.ContainsNode("C"));
            Assert.IsFalse(_graph.ContainsNode("D"));
            Assert.IsFalse(_graph.ContainsNode("E"));
        }

        [Test]
        public void RemoveEdge_FiresEventWithUnreachableNodes()
        {
            // Setup
            _graph.AddEdge(CentralNode, "ModuleA");
            _graph.AddEdge("ModuleA", "ModuleB");
            _graph.AddEdge("ModuleB", "EngineC");

            List<string> removedNodes = null;
            _graph.OnNodesRemovedDueToUnreachability += nodes => removedNodes = nodes;

            // Act
            _graph.RemoveEdge("ModuleA", "ModuleB");

            // Assert
            Assert.IsNotNull(removedNodes);
            Assert.AreEqual(2, removedNodes.Count);
            Assert.Contains("ModuleB", removedNodes);
            Assert.Contains("EngineC", removedNodes);
        }

        [Test]
        public void RemoveEdge_DoesNotFireEvent_WhenNoNodesRemoved()
        {
            // Setup: Command -- A -- B with alternate path
            _graph.AddEdge(CentralNode, "A");
            _graph.AddEdge(CentralNode, "B");
            _graph.AddEdge("A", "B");

            var eventFired = false;
            _graph.OnNodesRemovedDueToUnreachability += _ => eventFired = true;

            // Act: Remove edge but B is still reachable via Command
            _graph.RemoveEdge("A", "B");

            // Assert
            Assert.IsFalse(eventFired);
        }

        [Test]
        public void RemoveEdge_EventContainsAllDisconnectedNodes()
        {
            // Setup: Complex graph
            // Command -- A -- B
            //                 |
            //                 C -- D
            _graph.AddEdge(CentralNode, "A");
            _graph.AddEdge("A", "B");
            _graph.AddEdge("B", "C");
            _graph.AddEdge("C", "D");

            List<string> removedNodes = null;
            _graph.OnNodesRemovedDueToUnreachability += nodes => removedNodes = nodes;

            // Act: Cut at A-B
            _graph.RemoveEdge("A", "B");

            // Assert: B, C, D should all be in the event
            Assert.IsNotNull(removedNodes);
            Assert.AreEqual(3, removedNodes.Count);
            Assert.Contains("B", removedNodes);
            Assert.Contains("C", removedNodes);
            Assert.Contains("D", removedNodes);
            Assert.IsFalse(removedNodes.Contains("A")); // A is still connected
        }

        [Test]
        public void RemoveEdge_NonExistentEdge_DoesNotThrow()
        {
            _graph.AddNode(CentralNode);
            _graph.AddNode("A");

            Assert.DoesNotThrow(() => _graph.RemoveEdge(CentralNode, "A"));
        }

        [Test]
        public void RemoveEdge_NonExistentNodes_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _graph.RemoveEdge("NonExistent1", "NonExistent2"));
        }

        [Test]
        public void GetAllNodes_ReturnsAllNodesInGraph()
        {
            _graph.AddEdge(CentralNode, "A");
            _graph.AddEdge("A", "B");

            var allNodes = _graph.GetAllNodes();

            Assert.AreEqual(3, allNodes.Count);
            Assert.Contains(CentralNode, allNodes);
            Assert.Contains("A", allNodes);
            Assert.Contains("B", allNodes);
        }

        [Test]
        public void RemoveNode_RemovesNodeAndItsEdges()
        {
            _graph.AddEdge(CentralNode, "A");
            _graph.AddEdge("A", "B");

            _graph.RemoveNode("A");

            Assert.IsFalse(_graph.ContainsNode("A"));
            Assert.IsFalse(_graph.GetConnectedNodes(CentralNode).Contains("A"));
            // B should also be removed as it's no longer reachable
            Assert.IsFalse(_graph.ContainsNode("B"));
        }
    }
}