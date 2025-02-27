using UnityEngine;

namespace Background
{
    public class OldBackground : MonoBehaviour
    {
        [SerializeField] private Transform playerCenterTransform;
        [SerializeField] private Vector2Int backgroundSize;

        private void Update()
        {
            transform.position = new Vector2(
                playerCenterTransform.position.x - playerCenterTransform.position.x % backgroundSize.x,
                playerCenterTransform.position.y - playerCenterTransform.position.y % backgroundSize.y);
        }
    }
}