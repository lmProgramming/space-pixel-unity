using Core.Pixelation;
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

        T WithPixelatedRigidbody<T>() where T : Component, IPixelatedRigidbody;
    }
}