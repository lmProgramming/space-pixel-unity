using UnityEngine;

public sealed class MapInfo : MonoBehaviour
{
    [field: SerializeField] public Transform mapTransform;
    public static MapInfo Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}