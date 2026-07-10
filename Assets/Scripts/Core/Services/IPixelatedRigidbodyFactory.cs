using Core.Pixelation;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Services
{
    public interface IPixelatedRigidbodyFactory
    {
        IPixelatedRigidbodyShellBuilder CreatePixelatedRigidbodyShell(
            Transform parent,
            string newName,
            Vector3 localPosition,
            Quaternion localRotation,
            RigidbodyType2D bodyType);
    }

    public interface IPixelatedRigidbodyShellBuilder
    {
        GameObject GameObject { get; }

        [CanBeNull]
        IPixelatedRigidbody PixelatedRigidbody { get; }

        IPixelatedRigidbodyShellBuilder AsDisabledGameObject();

        IPixelatedRigidbodyShellBuilder WithPixelatedRigidbody<T>() where T : Component, IPixelatedRigidbody;
    }
}