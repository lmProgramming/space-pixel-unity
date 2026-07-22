using Core.UI;
using UnityEngine;
using Zenject;

namespace UI.Stack
{
    public class GameUiInstaller : MonoInstaller
    {
        [SerializeField] private GameUi gameUi;

        public override void InstallBindings()
        {
            if (!gameUi)
                throw new UnityException("[GameUiInstaller] GameUi is required.");

            Container.Bind<IGameUi>().FromInstance(gameUi).AsSingle();
        }
    }
}