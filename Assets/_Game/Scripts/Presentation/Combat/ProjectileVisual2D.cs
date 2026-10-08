using UnityEngine;
using Spotlight.Contracts;

namespace Spotlight.Presentation
{
    public class ProjectileVisual2D : MonoBehaviour
    {
        public Transform VisualRoot;
        public SpriteRenderer ElementOverlay;

        private Quaternion baseRotation;
        private Color baseColor;
        private bool cachedRotation;
        private bool cachedColor;

        private void Awake() { CacheBaseVisual(); }
        private void OnEnable() { ResetVisual(); }
        private void OnDisable() { ResetVisual(); }

        private void CacheBaseVisual()
        {
            if (!cachedRotation && VisualRoot != null)
            {
                baseRotation = VisualRoot.localRotation;
                cachedRotation = true;
            }
            if (!cachedColor && ElementOverlay != null)
            {
                baseColor = ElementOverlay.color;
                cachedColor = true;
            }
        }

        private static readonly Color[] ElementColors =
        {
            new Color32(64, 156, 255, 255),  // ˮ
            new Color32(255, 83, 64, 255),   // ��
            new Color32(190, 146, 62, 255),  // ��
            new Color32(62, 190, 104, 255),  // ľ
            new Color32(89, 224, 184, 255),  // ��
            new Color32(177, 100, 255, 255)  // ��
        };

        public void SetDirection(WorldPoint direction)
        {
            CacheBaseVisual();
            if (VisualRoot == null) return;

            float lengthSq =
                direction.X * direction.X +
                direction.Y * direction.Y;

            if (lengthSq <= 0.000001f ||
                float.IsNaN(lengthSq) ||
                float.IsInfinity(lengthSq))
                return;

            float angle =
                Mathf.Atan2(direction.Y, direction.X) *
                Mathf.Rad2Deg;

            VisualRoot.localRotation =
                Quaternion.Euler(0f, 0f, angle);
        }

        public void SetElements(int appliedElementMask)
        {
            CacheBaseVisual();
            if (ElementOverlay == null) return;

            int index = FirstKnownElement(appliedElementMask);

            ElementOverlay.enabled = index >= 0;

            if (index >= 0)
                ElementOverlay.color = ElementColors[index];
        }

        public void ResetVisual()
        {
            CacheBaseVisual();
            if (VisualRoot != null)
                VisualRoot.localRotation = baseRotation;

            if (ElementOverlay != null)
            {
                ElementOverlay.enabled = false;
                ElementOverlay.color = baseColor;
            }
        }

        private static int FirstKnownElement(int mask)
        {
            if ((mask & 1) != 0) return 0;
            if ((mask & 2) != 0) return 1;
            if ((mask & 4) != 0) return 2;
            if ((mask & 8) != 0) return 3;
            if ((mask & 16) != 0) return 4;
            if ((mask & 32) != 0) return 5;
            return -1;
        }
    }
}