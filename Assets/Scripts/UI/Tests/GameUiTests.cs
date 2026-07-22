using System;
using System.Collections.Generic;
using Core.UI;
using NUnit.Framework;
using UI.Stack;
using UnityEngine;
using Zenject;
using ZLinq;
using Object = UnityEngine.Object;

namespace UI.Tests
{
    public class GameUiTests
    {
        private List<GameObject> _created;
        private GameUi _gameUi;
        private GameObject _host;

        [SetUp]
        public void SetUp()
        {
            _created = new List<GameObject>();
            _host = new GameObject("GameUiHost");
            _created.Add(_host);
            var container = new DiContainer();

            _host.SetActive(false);
            _gameUi = _host.AddComponent<GameUi>();
            container.Bind<IGameUi>().FromInstance(_gameUi);
            container.Inject(_gameUi);
            _gameUi.SetRootParentsForTesting(_gameUi.transform);
            _host.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _created.AsValueEnumerable().Where(go => go))
                Object.DestroyImmediate(go);
        }

        [Test]
        public void SetRoot_IncreasesDepthAndIsUnkillable()
        {
            var root = CreateMarker("Root");
            _gameUi.SetRoot(root);

            Assert.AreEqual(1, _gameUi.Depth);
            Assert.AreSame(root, _gameUi.RootForTesting());
            Assert.IsFalse(_gameUi.TryPop());
            Assert.AreEqual(1, _gameUi.Depth);
            Assert.Throws<InvalidOperationException>(() => _gameUi.Pop());
        }

        [Test]
        public void SetRoot_Twice_Throws()
        {
            _gameUi.SetRoot(CreateMarker("Root"));
            Assert.Throws<InvalidOperationException>(() => _gameUi.SetRoot(CreateMarker("Other")));
        }

        [Test]
        public void PushExisting_ThenTryPop_RemovesOverlayButKeepsRoot()
        {
            var root = CreateMarker("Root");
            _gameUi.SetRoot(root);

            var overlay = CreateMarker("Overlay");
            _gameUi.PushExistingForTesting(overlay);

            Assert.AreEqual(2, _gameUi.Depth);
            Assert.AreSame(overlay, _gameUi.PeekForTesting());
            Assert.IsTrue(_gameUi.TryPop());
            Assert.AreEqual(1, _gameUi.Depth);
            Assert.AreSame(root, _gameUi.PeekForTesting());
        }

        [Test]
        public void Escape_OnRoot_WithPop_DoesNotChangeDepth()
        {
            _gameUi.SetRoot(CreateMarker("Root"));
            _gameUi.HandleEscapeForTesting();
            Assert.AreEqual(1, _gameUi.Depth);
        }

        [Test]
        public void Escape_OnOverlayWithoutPanel_Pops()
        {
            _gameUi.SetRoot(CreateMarker("Root"));
            _gameUi.PushExistingForTesting(CreateMarker("Overlay"));

            _gameUi.HandleEscapeForTesting();
            Assert.AreEqual(1, _gameUi.Depth);
        }

        [Test]
        public void Notify_DoesNotChangeDepth()
        {
            _gameUi.SetRoot(CreateMarker("Root"));
            var depth = _gameUi.Depth;
            // Notify requires DiContainer + prefab; depth must stay unchanged when host missing throws
            Assert.Throws<ZenjectException>(() => _gameUi.Notify("hello"));
            Assert.AreEqual(depth, _gameUi.Depth);
        }

        private StackMarker CreateMarker(string name)
        {
            var go = new GameObject(name);
            _created.Add(go);
            return go.AddComponent<StackMarker>();
        }

        private sealed class StackMarker : MonoBehaviour
        {
        }
    }
}