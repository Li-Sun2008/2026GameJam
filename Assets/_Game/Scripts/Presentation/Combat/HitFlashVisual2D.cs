using UnityEngine;

namespace Spotlight.Presentation
{
    /// <summary>Body-only hit feedback driven by game time supplied by the world renderer.</summary>
    public sealed class HitFlashVisual2D : MonoBehaviour
    {
        public SpriteRenderer BodyRenderer;
        public Color FlashColor = Color.white;
        public float DurationSeconds = .12f;
        private SpriteRenderer cachedBody;
        private Color originalColor;
        private double endTime;
        private bool flashing;

        private void Awake() { CacheBody(); }
        private void OnEnable() { ResetVisual(); }
        private void OnDisable() { ResetVisual(); }

        // Also handles inspector/public-field assignment after Awake without capturing a flash tint.
        private void CacheBody()
        {
            if (cachedBody == BodyRenderer) return;
            if (cachedBody != null && flashing) cachedBody.color = originalColor;
            flashing = false;
            cachedBody = BodyRenderer;
            if (cachedBody != null) originalColor = cachedBody.color;
        }

        public void BindBody(SpriteRenderer body) { ResetVisual(); BodyRenderer = body; CacheBody(); }
        public void PlayHit(double gameTime)
        {
            CacheBody();
            if (cachedBody == null || !ValidTime(gameTime)) return;
            if (float.IsNaN(DurationSeconds) || float.IsInfinity(DurationSeconds) || DurationSeconds <= 0)
            { ResetVisual(); return; }
            endTime = gameTime + DurationSeconds;
            if (!ValidTime(endTime)) { ResetVisual(); return; }
            flashing = true;
            cachedBody.color = FlashColor;
        }
        public void ApplyVisualTime(double gameTime)
        {
            CacheBody();
            if (flashing && ValidTime(gameTime) && gameTime >= endTime) ResetVisual();
        }
        public void ResetVisual()
        {
            CacheBody();
            if (cachedBody != null && flashing) cachedBody.color = originalColor;
            flashing = false;
            endTime = 0;
        }
        private static bool ValidTime(double value) { return value >= 0 && !double.IsNaN(value) && !double.IsInfinity(value); }
    }
}
