using UnityEngine;

namespace Spotlight.Presentation
{
    /// <summary>Enemy-only presentation widget. Gameplay remains the owner of HP.</summary>
    public sealed class EnemyHealthBar2D : MonoBehaviour
    {
        public Transform BarRoot;
        public Transform Fill;

        private Vector3 fillInitialPosition;
        private Vector3 fillInitialScale;
        private bool initialized;

        private void Awake()
        {
            CacheInitialGeometry();
        }

        private void OnEnable()
        {
            ResetVisual();
        }

        private void OnDisable()
        {
            ResetVisual();
        }

        public void ApplyHealth(float currentHp, float maxHp)
        {
            CacheInitialGeometry();
            if (BarRoot == null || Fill == null) return;

            if (maxHp <= 0f)
            {
                BarRoot.gameObject.SetActive(false);
                return;
            }

            BarRoot.gameObject.SetActive(true);
            float ratio = Mathf.Clamp01(currentHp / maxHp);
            Fill.localScale = new Vector3(fillInitialScale.x * ratio, fillInitialScale.y, fillInitialScale.z);
            // Keep the fill's left edge fixed while changing only its displayed width.
            Fill.localPosition = fillInitialPosition + Vector3.left * (fillInitialScale.x * (1f - ratio) * 0.5f);
        }

        public void ResetVisual()
        {
            CacheInitialGeometry();
            if (BarRoot != null) BarRoot.gameObject.SetActive(true);
            if (Fill == null) return;
            Fill.localPosition = fillInitialPosition;
            Fill.localScale = fillInitialScale;
        }

        private void CacheInitialGeometry()
        {
            if (initialized || Fill == null) return;
            fillInitialPosition = Fill.localPosition;
            fillInitialScale = Fill.localScale;
            initialized = true;
        }
    }
}
