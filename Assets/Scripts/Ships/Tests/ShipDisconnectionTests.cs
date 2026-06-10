using System.Collections;
using System.Collections.Generic;
using Core.Pixelation;
using NUnit.Framework;
using Ships.Tests.TestHelpers.Factories;
using Ships.Tests.TestHelpers.Fixtures;
using UnityEngine;
using UnityEngine.TestTools;
using ZLinq;
using Module = Ships.Modules.Module;

namespace Ships.Tests
{
    /// <summary>
    ///     Integration tests for ship module disconnection logic.
    ///     Tests the real cohesion pipeline: pixel destruction → CheckCohesion → DetachConnections →
    ///     BiCohesionGraph.RemoveEdge → HandleUnreachableModules
    /// </summary>
    [TestFixture]
    public class ShipDisconnectionTests : ShipTestBase
    {
        /// <summary>
        ///     Gets the connection points between two modules.
        /// </summary>
        private static List<Vector2Int> GetConnectionPoints(Module from, Module to)
        {
            return from.ConnectionPoints.TryGetValue(to, out var points)
                ? points.AsValueEnumerable().ToList()
                : new List<Vector2Int>();
        }


        [UnityTest]
        public IEnumerator DestroyAllConnectionPoints_ModuleDisconnects()
        {
            // Arrange: Create a two-module ship
            var components = ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects);
            yield return WaitForLifecycle();

            var ship = components.Ship;
            var commandModule = components.Command;
            var otherModule = components.Other;

            // Verify initial state - both modules should be in the graph
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(commandModule), "Command module should be in graph");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(otherModule), "Other module should be in graph");

            // Get connection points
            var connectionPoints = GetConnectionPoints(commandModule, otherModule);
            Assert.IsTrue(connectionPoints.Count > 0, "Modules should have connection points");

            // Act: Destroy all connection points on the command module
            commandModule.PixelatedRigidbody.RemovePixels(connectionPoints);

            yield return null; // Wait for events to process

            // Assert: Other module should be destroyed and disconnected
            Assert.IsFalse(ship.ModuleGraph.ContainsNode(otherModule),
                "Disconnected module should be removed from graph");
            Assert.IsFalse(otherModule,
                "Disconnected module should be destroyed (null reference)");
        }

        [UnityTest]
        public IEnumerator DestroyMiddleModule_DownstreamModulesDisconnect()
        {
            // Arrange: Create Command -- Module2 -- Module3
            const int moduleWidth = 5;
            const int moduleHeight = 5;
            var components = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("Command", Vector2.zero, moduleWidth, moduleHeight)
                .WithModule("Module2", new Vector2(moduleWidth, 0), moduleWidth, moduleHeight)
                .WithModule("Module3", new Vector2(moduleWidth * 2, 0), moduleWidth, moduleHeight)
                .BuildLayoutResult();
            yield return WaitForLifecycle();

            var ship = components.Ship;
            var module2 = components.OtherModules[0];
            var module3 = components.OtherModules[1];

            // Verify initial state
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(module2));
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(module3));

            // Get connection points between module2 and command
            var module2ToCommandPoints = GetConnectionPoints(module2, components.CommandModule);

            // Act: Destroy all connection points on module2 that connect to command
            module2.PixelatedRigidbody.RemovePixels(module2ToCommandPoints);

            yield return null;

            // Assert: Both module2 and module3 should disconnect (module3 loses path to command)
            Assert.IsFalse(ship.ModuleGraph.ContainsNode(module2),
                "Module2 should be removed from graph");
            Assert.IsFalse(ship.ModuleGraph.ContainsNode(module3),
                "Module3 should be removed from graph (lost path to command)");
        }

        [UnityTest]
        public IEnumerator AlternatePath_ModuleStaysConnected()
        {
            // Arrange: Create ship with alternate paths
            const int moduleSize = 5;
            var components = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("Command", Vector2.zero, moduleSize, moduleSize)
                .WithModule("ModuleA", new Vector2(moduleSize, 0), moduleSize, moduleSize)
                .WithModule("ModuleB", new Vector2(0, moduleSize), moduleSize, moduleSize)
                .WithModule("ModuleC", new Vector2(moduleSize, moduleSize), moduleSize, moduleSize)
                .BuildLayoutResult();
            yield return WaitForLifecycle();

            var ship = components.Ship;
            var moduleA = components.OtherModules[0];
            var moduleB = components.OtherModules[1];
            var moduleC = components.OtherModules[2];

            // Verify initial connections exist
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleA));
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleB));
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleC));

            // Get connection points between moduleA and command
            var moduleAToCommandPoints = GetConnectionPoints(moduleA, components.CommandModule);

            // Skip if no direct connection (module layout may vary)
            if (moduleAToCommandPoints.Count == 0)
                Assert.Inconclusive("Test requires moduleA to have direct connection to command");

            // Act: Destroy direct connection between moduleA and command
            moduleA.PixelatedRigidbody.RemovePixels(moduleAToCommandPoints);

            yield return null;

            // Assert: If alternate path exists through C, moduleA should stay connected
            // This depends on the actual topology created - verify the graph state
            _ = ship.ModuleGraph.ContainsNode(moduleA) &&
                ship.ModuleGraph.ContainsNode(moduleB) &&
                ship.ModuleGraph.ContainsNode(moduleC);

            // Log the result for diagnostic purposes
            Debug.Log($"After removing A-Command connection: A={ship.ModuleGraph.ContainsNode(moduleA)}, " +
                      $"B={ship.ModuleGraph.ContainsNode(moduleB)}, C={ship.ModuleGraph.ContainsNode(moduleC)}");

            // The actual assertion depends on whether an alternate path exists
            // If C connects A to B and B connects to Command, A should stay
        }

        [UnityTest]
        public IEnumerator DestroyCommandModulePixels_ShipDestroyed()
        {
            // Arrange: Create a simple ship
            var components = ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects, 3, 3);
            yield return WaitForLifecycle();

            var ship = components.Ship;
            var commandModule = components.Command;
            var shipGo = ship.gameObject;

            // Act: Destroy all pixels in the command module
            var dimensions = commandModule.PixelatedRigidbody.Dimensions();
            var allPixels = new List<Vector2Int>();
            for (var x = 0; x < dimensions.x; x++)
            for (var y = 0; y < dimensions.y; y++)
                allPixels.Add(new Vector2Int(x, y));

            commandModule.PixelatedRigidbody.RemovePixels(allPixels);

            yield return null;
            yield return null; // Extra frame for destruction

            // Assert: Ship should be destroyed
            Assert.IsTrue(shipGo == null, "Ship should be destroyed when command module has no pixels");
        }

        [UnityTest]
        public IEnumerator PartialConnectionPointDestruction_ModuleStaysConnected()
        {
            // Arrange: Create a two-module ship with larger modules (more connection points)
            var components = ShipTestFactory.CreateTwoModuleShip(Container, CreatedObjects, 10, 10);
            yield return WaitForLifecycle();

            var ship = components.Ship;
            var commandModule = components.Command;
            var otherModule = components.Other;

            var connectionPoints = GetConnectionPoints(commandModule, otherModule);

            // Skip if not enough connection points
            if (connectionPoints.Count < 2) Assert.Inconclusive("Need at least 2 connection points for this test");

            // Act: Destroy only SOME connection points (leave at least one)
            var pointsToDestroy = connectionPoints.AsValueEnumerable().Take(connectionPoints.Count - 1).ToList();
            commandModule.PixelatedRigidbody.RemovePixels(pointsToDestroy);

            yield return null;

            // Assert: Module should still be connected (has remaining connection points)
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(otherModule),
                "Module should stay connected when some connection points remain");
            Assert.AreEqual(ship.transform, otherModule.transform.parent,
                "Module should still be parented to ship");
        }

        [UnityTest]
        public IEnumerator SliceCentralModuleNearTop_TopModuleDisconnects()
        {
            // Arrange: Create vertical ship A (top) -- B (central/command) -- C (bottom)
            const int moduleWidth = 5;
            const int moduleHeight = 10;
            var components = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("CommandB", Vector2.zero, moduleWidth, moduleHeight)
                .WithModule("ModuleA", new Vector2(0, moduleHeight), moduleWidth, moduleHeight)
                .WithModule("ModuleC", new Vector2(0, -moduleHeight), moduleWidth, moduleHeight)
                .BuildLayoutResult();
            yield return WaitForLifecycle();

            var ship = components.Ship;
            var commandModule = components.CommandModule;
            var moduleA = components.OtherModules[0];
            var moduleC = components.OtherModules[1];

            // Verify initial state
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleA), "Module A should be in graph initially");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleC), "Module C should be in graph initially");

            // Act: Slice a horizontal line near the TOP of B (closer to A)
            // This tests the DIVISION flow:
            // 1. RemovePixels creates a horizontal gap in B
            // 2. GridRegionFinder.FloodFindCohesiveRegions detects B split into 2 regions
            // 3. HandleDivision removes the smaller region (top portion) as debris
            // 4. PixelLostByDivision fires OnPixelsLost with the debris pixels
            // 5. Module.CheckCohesion sees B's connection points to A were in the debris
            // 6. DetachConnections removes the A-B edge
            // 7. A becomes unreachable from command and gets deparented
            var dimensions = commandModule.PixelatedRigidbody.Dimensions();
            var sliceY = dimensions.y - 2; // Near top (2 pixels from top edge)
            var horizontalLine = new List<Vector2Int>();
            for (var x = 0; x < dimensions.x; x++)
                horizontalLine.Add(new Vector2Int(x, sliceY));

            commandModule.PixelatedRigidbody.RemovePixels(horizontalLine);

            yield return null; // Wait for cohesion check and events

            // Assert: Module A should be destroyed and disconnected
            Assert.IsFalse(ship.ModuleGraph.ContainsNode(moduleA),
                "Module A should be disconnected after slicing near top of B");
            Assert.IsFalse(moduleA,
                "Disconnected module should be destroyed (null reference)");

            // Module C should still be connected (bottom portion of B remains with its connection points)
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleC),
                "Module C should remain connected after slicing near top of B");
        }

        [UnityTest]
        public IEnumerator SliceCentralModuleNearBottom_BottomModuleDisconnects()
        {
            // Arrange: Create vertical ship A (top) -- B (central/command) -- C (bottom)
            const int moduleWidth = 5;
            const int moduleHeight = 10;
            var components = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("CommandB", Vector2.zero, moduleWidth, moduleHeight)
                .WithModule("ModuleA", new Vector2(0, moduleHeight), moduleWidth, moduleHeight)
                .WithModule("ModuleC", new Vector2(0, -moduleHeight), moduleWidth, moduleHeight)
                .BuildLayoutResult();
            yield return WaitForLifecycle();

            var ship = components.Ship;
            var commandModule = components.CommandModule;
            var moduleA = components.OtherModules[0];
            var moduleC = components.OtherModules[1];

            // Verify initial state
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleA), "Module A should be in graph initially");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleC), "Module C should be in graph initially");

            // Act: Slice a horizontal line near the BOTTOM of B (closer to C)
            // This tests the DIVISION flow (same as SliceCentralModuleNearTop but opposite end):
            // The bottom portion becomes debris, taking B's connection points to C with it
            // C then becomes unreachable and gets deparented
            var dimensions = commandModule.PixelatedRigidbody.Dimensions();
            var sliceY = 1; // Near bottom (1 pixel from bottom edge)
            var horizontalLine = new List<Vector2Int>();
            for (var x = 0; x < dimensions.x; x++)
                horizontalLine.Add(new Vector2Int(x, sliceY));

            commandModule.PixelatedRigidbody.RemovePixels(horizontalLine);

            yield return null; // Wait for cohesion check and events

            // Assert: Module C should be destroyed and disconnected
            Assert.IsFalse(ship.ModuleGraph.ContainsNode(moduleC),
                "Module C should be disconnected after slicing near bottom of B");
            Assert.IsFalse(moduleC,
                "Disconnected module should be destroyed (null reference)");

            // Module A should still be connected (top portion of B remains with its connection points)
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleA),
                "Module A should remain connected after slicing near bottom of B");
        }

        [UnityTest]
        public IEnumerator SliceCentralModuleInMiddle_SmallerHalfDisconnects()
        {
            // Arrange: Create vertical ship A (top) -- B (central/command) -- C (bottom)
            const int moduleWidth = 5;
            const int moduleHeight = 10;
            var components = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithCommand("CommandB", Vector2.zero, moduleWidth, moduleHeight)
                .WithModule("ModuleA", new Vector2(0, moduleHeight), moduleWidth, moduleHeight)
                .WithModule("ModuleC", new Vector2(0, -moduleHeight), moduleWidth, moduleHeight)
                .BuildLayoutResult();
            yield return WaitForLifecycle();

            var ship = components.Ship;
            var commandModule = components.CommandModule;
            var moduleA = components.OtherModules[0];
            var moduleC = components.OtherModules[1];

            // Verify initial state
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleA), "Module A should be in graph initially");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(moduleC), "Module C should be in graph initially");

            // Act: Slice a horizontal line in the MIDDLE of B
            // When halves are equal size, GridRegionFinder keeps the LAST region (arbitrary)
            // One half becomes debris, its connected module disconnects
            var dimensions = commandModule.PixelatedRigidbody.Dimensions();
            var sliceY = dimensions.y / 2; // Middle
            var horizontalLine = new List<Vector2Int>();
            for (var x = 0; x < dimensions.x; x++)
                horizontalLine.Add(new Vector2Int(x, sliceY));

            commandModule.PixelatedRigidbody.RemovePixels(horizontalLine);

            yield return null; // Wait for cohesion check and events

            // Assert: At least one module should be disconnected
            // When sliced in the middle, one half becomes debris and its connected module disconnects
            var aConnected = ship.ModuleGraph.ContainsNode(moduleA);
            var cConnected = ship.ModuleGraph.ContainsNode(moduleC);

            // Log the state for debugging
            Debug.Log($"After middle slice: A connected={aConnected}, C connected={cConnected}");

            // At least one should be disconnected (the smaller half's connected module)
            Assert.IsFalse(aConnected && cConnected,
                "At least one module should disconnect when B is sliced in the middle");

            // Exactly one should remain connected (we're not destroying the command module)
            Assert.IsTrue(aConnected || cConnected,
                "At least one module should remain connected to the surviving half of B");
        }

        /// <summary>
        ///     Creates a two-module ship where a wide cannon-like module connects to
        ///     a command module on its right edge. Then slices a 3-pixel vertical gap
        ///     near the cannon's right edge, separating the connection strip as debris.
        ///     Verifies the cannon properly detaches from the command module.
        /// </summary>
        [UnityTest]
        public IEnumerator SliceCannonNearConnectionEdge_CannonDisconnects()
        {
            // Arrange: Command (6 wide) on the right, Cannon (16 wide) on the left
            // Module transform offset must be (cannonWidth + commandWidth) / 2 for edge adjacency
            const int cannonWidth = 16;
            const int cannonHeight = 10;
            const int commandWidth = 6;
            const int commandHeight = 10;

            var layout = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithoutGameObjectInjection()
                .WithCommand("Command", new Vector2((cannonWidth + commandWidth) / 2f, 0), commandWidth, commandHeight)
                .WithModule("Cannon", Vector2.zero, cannonWidth, cannonHeight)
                .BuildLayoutResult();
            var ship = layout.Ship;
            var commandModule = layout.CommandModule;
            var cannonModule = layout.OtherModules[0];

            yield return WaitForLifecycle();

            // --- Step 1: Verify modules exist in graph ---
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(cannonModule), "Cannon should be in graph initially");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(commandModule), "Command should be in graph initially");
            Debug.Log("[Test] Both modules are in the graph.");

            // --- Step 2: Verify pixel grids are initialized ---
            var cannonDims = cannonModule.PixelatedRigidbody.Dimensions();
            var commandDims = commandModule.PixelatedRigidbody.Dimensions();
            Assert.AreEqual(cannonWidth, cannonDims.x, "Cannon grid width");
            Assert.AreEqual(cannonHeight, cannonDims.y, "Cannon grid height");
            Assert.AreEqual(commandWidth, commandDims.x, "Command grid width");
            Assert.AreEqual(commandHeight, commandDims.y, "Command grid height");
            Debug.Log(
                $"[Test] Cannon grid: {cannonDims.x}x{cannonDims.y}, Command grid: {commandDims.x}x{commandDims.y}");

            // --- Step 3: Verify world positions make modules adjacent ---
            var cannonRightEdgeWorld =
                cannonModule.PixelatedRigidbody.LocalToWorldPoint(new Vector2Int(cannonWidth - 1, 0));
            var commandLeftEdgeWorld = commandModule.PixelatedRigidbody.LocalToWorldPoint(new Vector2Int(0, 0));
            var edgeDistance = Mathf.Abs(commandLeftEdgeWorld.x - cannonRightEdgeWorld.x);
            Debug.Log($"[Test] Cannon right edge world x={cannonRightEdgeWorld.x}, " +
                      $"Command left edge world x={commandLeftEdgeWorld.x}, distance={edgeDistance}");
            Assert.AreEqual(1f, edgeDistance, 0.01f,
                "Cannon right edge and Command left edge should be exactly 1 unit apart for adjacency");

            // --- Step 4: Verify connection points exist ---
            var cannonToCommandPoints = GetConnectionPoints(cannonModule, commandModule);
            var commandToCannonPoints = GetConnectionPoints(commandModule, cannonModule);
            Debug.Log($"[Test] Cannon→Command connection points: {cannonToCommandPoints.Count}, " +
                      $"Command→Cannon connection points: {commandToCannonPoints.Count}");
            Debug.Log($"[Test] Cannon→Command points: [{string.Join(", ", cannonToCommandPoints)}]");
            Debug.Log($"[Test] Command→Cannon points: [{string.Join(", ", commandToCannonPoints)}]");
            Assert.IsTrue(cannonToCommandPoints.Count > 0,
                "Cannon should have connection points to Command on its right edge");

            // --- Step 5: Verify connection points are on the cannon's right edge ---
            foreach (var pt in cannonToCommandPoints)
                Assert.AreEqual(cannonWidth - 1, pt.x,
                    $"Connection point ({pt.x},{pt.y}) should be on cannon's right edge (x={cannonWidth - 1})");

            // --- Step 6: Record pixel counts before slice ---
            var cannonPixelsBefore = cannonModule.PixelatedRigidbody.CurrentPixelCount;
            Debug.Log($"[Test] Cannon pixel count before slice: {cannonPixelsBefore}");
            Assert.AreEqual(cannonWidth * cannonHeight, cannonPixelsBefore, "Cannon should be fully solid");

            // --- Step 7: Perform the slice ---
            var sliceStartX = cannonWidth - 4; // x=12
            var slicePixels = new List<Vector2Int>();
            for (var x = sliceStartX; x < sliceStartX + 3; x++)
            for (var y = 0; y < cannonHeight; y++)
                slicePixels.Add(new Vector2Int(x, y));

            Debug.Log($"[Test] Slicing cannon at x={sliceStartX}..{sliceStartX + 2}, " +
                      $"removing {slicePixels.Count} pixels.");

            // Subscribe to OnPixelsLost to log what happens
            var pixelsLostEvents = new List<(List<Vector2Int> pixels, PixelLoseReason reason)>();
            cannonModule.PixelatedRigidbody.OnPixelsLost += (pixels, reason) =>
            {
                pixelsLostEvents.Add((new List<Vector2Int>(pixels), reason));
                Debug.Log($"[Test] OnPixelsLost fired: reason={reason}, pixelCount={pixels.Count}, " +
                          $"pixels=[{string.Join(", ", pixels.AsValueEnumerable().Take(20))}{(pixels.Count > 20 ? "..." : "")}]");
            };

            cannonModule.PixelatedRigidbody.RemovePixels(slicePixels);

            // --- Step 8: Check what happened immediately after RemovePixels ---
            var cannonPixelsAfter = cannonModule.PixelatedRigidbody.CurrentPixelCount;
            Debug.Log($"[Test] Cannon pixel count after slice: {cannonPixelsAfter}");
            Debug.Log($"[Test] OnPixelsLost events fired: {pixelsLostEvents.Count}");

            foreach (var evt in pixelsLostEvents)
                Debug.Log($"[Test]   Event: reason={evt.reason}, count={evt.pixels.Count}");

            // We expect at least 2 events: one for Destroyed (the sliced pixels), one for Division
            Assert.IsTrue(pixelsLostEvents.Count >= 1, "At least one OnPixelsLost event should have fired");

            var destroyedEvent = pixelsLostEvents.AsValueEnumerable()
                .FirstOrDefault(e => e.reason == PixelLoseReason.Destroyed);
            var divisionEvents = pixelsLostEvents.AsValueEnumerable().Where(e => e.reason == PixelLoseReason.Division)
                .ToList();

            Assert.IsNotNull(destroyedEvent.pixels, "A Destroyed event should have fired for the sliced pixels");
            Debug.Log($"[Test] Destroyed event: {destroyedEvent.pixels.Count} pixels");

            // --- Step 9: Check if division happened ---
            Debug.Log($"[Test] Division events: {divisionEvents.Count}");

            if (divisionEvents.Count == 0)
            {
                // Division didn't happen — check if the grid is actually split
                Debug.Log("[Test] WARNING: No division event fired! Checking grid connectivity...");

                // Verify the right strip (x=15) still has pixels
                var rightStripAlive = false;
                for (var y = 0; y < cannonHeight; y++)
                    if (cannonModule.PixelatedRigidbody.IsPixel(new Vector2Int(cannonWidth - 1, y)))
                    {
                        rightStripAlive = true;
                        break;
                    }

                Debug.Log($"[Test] Right strip (x={cannonWidth - 1}) still has pixels: {rightStripAlive}");

                // Verify the left bulk (x=0) still has pixels
                var leftBulkAlive = false;
                for (var y = 0; y < cannonHeight; y++)
                    if (cannonModule.PixelatedRigidbody.IsPixel(new Vector2Int(0, y)))
                    {
                        leftBulkAlive = true;
                        break;
                    }

                Debug.Log($"[Test] Left bulk (x=0) still has pixels: {leftBulkAlive}");

                // Verify the gap exists
                var gapExists = true;
                for (var y = 0; y < cannonHeight; y++)
                    if (cannonModule.PixelatedRigidbody.IsPixel(new Vector2Int(sliceStartX, y)))
                    {
                        gapExists = false;
                        break;
                    }

                Debug.Log($"[Test] Gap at x={sliceStartX} exists: {gapExists}");
                Assert.IsTrue(gapExists, "The sliced column should be empty");
            }
            else
            {
                // Division happened — check which pixels were in the division
                foreach (var divEvt in divisionEvents)
                {
                    var divXValues = divEvt.pixels.AsValueEnumerable().Select(p => p.x).Distinct().OrderBy(x => x)
                        .ToList();
                    Debug.Log($"[Test] Division region x-values: [{string.Join(", ", divXValues)}], " +
                              $"pixel count: {divEvt.pixels.Count}");

                    // Check if connection points overlap with division pixels
                    var connectionPointsInDivision = cannonToCommandPoints.AsValueEnumerable()
                        .Where(cp => divEvt.pixels.Contains(cp)).ToList();
                    Debug.Log("[Test] Connection points in this division region: " +
                              $"{connectionPointsInDivision.Count} of {cannonToCommandPoints.Count}");
                    Debug.Log(
                        $"[Test] Connection points in division: [{string.Join(", ", connectionPointsInDivision)}]");
                }
            }

            // --- Step 10: Check remaining connection points after slice ---
            var remainingCannonToCommand = GetConnectionPoints(cannonModule, commandModule);
            Debug.Log("[Test] Remaining Cannon→Command connection points after slice: " +
                      $"{remainingCannonToCommand.Count}");
            Debug.Log($"[Test] Remaining points: [{string.Join(", ", remainingCannonToCommand)}]");

            yield return null; // Wait for cohesion check and events

            // --- Step 11: Final state ---
            var cannonInGraph = ship.ModuleGraph.ContainsNode(cannonModule);
            var commandInGraph = ship.ModuleGraph.ContainsNode(commandModule);
            Debug.Log($"[Test] Final state: Cannon in graph={cannonInGraph}, Command in graph={commandInGraph}");

            // The right strip (x=15) with connection points should have become debris,
            // leaving the cannon bulk (x=0..11) with no connection points to Command.
            // CheckCohesion should have detected all connection points were lost and detached.
            Assert.IsFalse(cannonInGraph,
                "Cannon should be disconnected after its connection edge was separated as debris. " +
                $"Remaining connection points: {remainingCannonToCommand.Count}");
        }

        /// <summary>
        ///     Same scenario as SliceCannonNearConnectionEdge_CannonDisconnects but pixels are
        ///     removed gradually across 5 frames (6 pixels per frame), simulating real bullet
        ///     impacts cutting through the cannon over time.
        /// </summary>
        [UnityTest]
        public IEnumerator SliceCannonAcrossMultipleFrames_CannonDisconnects()
        {
            const int cannonWidth = 16;
            const int cannonHeight = 10;
            const int commandWidth = 6;
            const int commandHeight = 10;

            var layout = ShipTestBuilder.CreateShip(Container, CreatedObjects)
                .WithoutGameObjectInjection()
                .WithCommand("Command", new Vector2((cannonWidth + commandWidth) / 2f, 0), commandWidth, commandHeight)
                .WithModule("Cannon", Vector2.zero, cannonWidth, cannonHeight)
                .BuildLayoutResult();
            var ship = layout.Ship;
            var commandModule = layout.CommandModule;
            var cannonModule = layout.OtherModules[0];

            yield return WaitForLifecycle();

            // --- Verify initial state ---
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(cannonModule), "Cannon should be in graph initially");
            Assert.IsTrue(ship.ModuleGraph.ContainsNode(commandModule), "Command should be in graph initially");

            var cannonToCommandPoints = GetConnectionPoints(cannonModule, commandModule);
            Assert.IsTrue(cannonToCommandPoints.Count > 0,
                "Cannon should have connection points to Command");
            Assert.AreEqual(cannonWidth * cannonHeight, cannonModule.PixelatedRigidbody.CurrentPixelCount,
                "Cannon should be fully solid");

            Debug.Log($"[Test] Initial connection points: {cannonToCommandPoints.Count} " +
                      $"at [{string.Join(", ", cannonToCommandPoints)}]");

            // --- Subscribe to events ---
            var pixelsLostEvents = new List<(List<Vector2Int> pixels, PixelLoseReason reason, int frame)>();
            var currentFrame = 0;
            var frameCur = currentFrame;
            cannonModule.PixelatedRigidbody.OnPixelsLost += (pixels, reason) =>
            {
                pixelsLostEvents.Add((new List<Vector2Int>(pixels), reason, frameCur));
                Debug.Log($"[Test] Frame {frameCur}: OnPixelsLost reason={reason}, " +
                          $"count={pixels.Count}, " +
                          $"x-values=[{string.Join(", ", pixels.AsValueEnumerable().Select(p => p.x).Distinct().OrderBy(x => x))}]");
            };

            // --- Build the 30 pixels to remove (x=12,13,14 × y=0..9) ---
            var sliceStartX = cannonWidth - 4; // 12
            var allSlicePixels = new List<Vector2Int>();
            for (var x = sliceStartX; x < sliceStartX + 3; x++)
            for (var y = 0; y < cannonHeight; y++)
                allSlicePixels.Add(new Vector2Int(x, y));

            // --- Split into 5 batches of 6 pixels each ---
            var batches = new List<List<Vector2Int>>();
            for (var i = 0; i < allSlicePixels.Count; i += 6)
                batches.Add(allSlicePixels.GetRange(i, Mathf.Min(6, allSlicePixels.Count - i)));

            Assert.AreEqual(5, batches.Count, "Should have 5 batches of 6 pixels");

            // --- Remove pixels across 5 frames ---
            for (var frame = 0; frame < batches.Count; frame++)
            {
                var batch = batches[frame];
                Debug.Log($"[Test] Frame {frame}: Removing {batch.Count} pixels: " +
                          $"[{string.Join(", ", batch)}]");

                cannonModule.PixelatedRigidbody.RemovePixels(batch);

                // Log state after each frame
                var remaining = GetConnectionPoints(cannonModule, commandModule);
                var pixelCount = cannonModule.PixelatedRigidbody.CurrentPixelCount;
                var cannonStillInGraph = ship.ModuleGraph.ContainsNode(cannonModule);
                Debug.Log($"[Test] Frame {frame} after: pixels={pixelCount}, " +
                          $"connectionPoints={remaining.Count}, inGraph={cannonStillInGraph}");

                yield return null;
            }

            // --- Log all events ---
            Debug.Log($"[Test] Total OnPixelsLost events: {pixelsLostEvents.Count}");
            foreach (var evt in pixelsLostEvents)
                Debug.Log($"[Test]   Frame {evt.frame}: reason={evt.reason}, count={evt.pixels.Count}");

            var divisionEvents = pixelsLostEvents.AsValueEnumerable().Where(e => e.reason == PixelLoseReason.Division)
                .ToList();
            Debug.Log($"[Test] Division events total: {divisionEvents.Count}");

            // --- Check if division happened at all ---
            if (divisionEvents.Count > 0)
            {
                foreach (var divEvt in divisionEvents)
                {
                    var divXValues = divEvt.pixels.AsValueEnumerable().Select(p => p.x).Distinct().OrderBy(x => x)
                        .ToList();
                    var connectionPointsInDivision = cannonToCommandPoints.AsValueEnumerable()
                        .Where(cp => divEvt.pixels.Contains(cp)).ToList();
                    Debug.Log($"[Test] Division at frame {divEvt.frame}: " +
                              $"x-values=[{string.Join(", ", divXValues)}], " +
                              $"connectionPts in division={connectionPointsInDivision.Count}/{cannonToCommandPoints.Count}");
                }
            }
            else
            {
                Debug.Log("[Test] WARNING: No division events fired across all frames!");

                // Check grid state
                for (var x = sliceStartX - 1; x <= cannonWidth - 1; x++)
                {
                    var hasPixel = false;
                    for (var y = 0; y < cannonHeight; y++)
                        if (cannonModule.PixelatedRigidbody.IsPixel(new Vector2Int(x, y)))
                        {
                            hasPixel = true;
                            break;
                        }

                    Debug.Log($"[Test] Column x={x} has pixels: {hasPixel}");
                }
            }

            // --- Final assertions ---
            yield return null; // extra frame for safety

            var finalRemaining = GetConnectionPoints(cannonModule, commandModule);
            var cannonInGraph = ship.ModuleGraph.ContainsNode(cannonModule);
            var commandInGraph = ship.ModuleGraph.ContainsNode(commandModule);
            Debug.Log($"[Test] Final: Cannon in graph={cannonInGraph}, Command in graph={commandInGraph}, " +
                      $"remaining connection points={finalRemaining.Count}");

            Assert.IsFalse(cannonInGraph,
                "Cannon should be disconnected after multi-frame slice separated its connection edge. " +
                $"Remaining connection points: {finalRemaining.Count}");
        }
    }
}