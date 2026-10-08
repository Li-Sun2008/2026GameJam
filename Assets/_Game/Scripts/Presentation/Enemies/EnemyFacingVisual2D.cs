using UnityEngine;
using Spotlight.Contracts;

namespace Spotlight.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyFacingVisual2D : MonoBehaviour
    {
        public SpriteRenderer BodyRenderer;
        public bool DefaultFacingRight = true;
        private bool baseFlipX;
        private bool cached;

        private void Awake() { CacheBaseVisual(); }
        private void OnEnable() { ResetVisual(); }
        private void OnDisable() { ResetVisual(); }

        private void CacheBaseVisual()
        {
            if (cached || BodyRenderer == null) return;
            baseFlipX = BodyRenderer.flipX;
            cached = true;
        }

        public void SetDirection(WorldPoint direction)
        {
            CacheBaseVisual();
            if (BodyRenderer == null || float.IsNaN(direction.X) || float.IsInfinity(direction.X) ||
                float.IsNaN(direction.Y) || float.IsInfinity(direction.Y)) return;
            if (direction.X > 0.0001f) BodyRenderer.flipX = !DefaultFacingRight;
            else if (direction.X < -0.0001f) BodyRenderer.flipX = DefaultFacingRight;
        }

        public void ResetVisual()
        {
            CacheBaseVisual();
            if (BodyRenderer != null) BodyRenderer.flipX = baseFlipX;
        }
    }
}
