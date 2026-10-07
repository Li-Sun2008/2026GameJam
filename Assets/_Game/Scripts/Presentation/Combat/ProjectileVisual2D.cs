using UnityEngine;
using Spotlight.Contracts;

namespace Spotlight.Presentation
{
    public class ProjectileVisual2D : MonoBehaviour
    {
        public Transform VisualRoot;
        public SpriteRenderer ElementOverlay;

        private static readonly Color[] ElementColors =
        {
            new Color32(64, 156, 255, 255),  // Ë®
            new Color32(255, 83, 64, 255),   // »ð
            new Color32(190, 146, 62, 255),  // ÍÁ
            new Color32(62, 190, 104, 255),  // Ä¾
            new Color32(89, 224, 184, 255),  // ·ç
            new Color32(177, 100, 255, 255)  // À×
        };

        public void SetDirection(WorldPoint direction)
        {
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
            if (ElementOverlay == null) return;

            int index = FirstKnownElement(appliedElementMask);

            ElementOverlay.enabled = index >= 0;

            if (index >= 0)
                ElementOverlay.color = ElementColors[index];
        }

        public void ResetVisual()
        {
            if (VisualRoot != null)
                VisualRoot.localRotation = Quaternion.identity;

            if (ElementOverlay != null)
            {
                ElementOverlay.enabled = false;
                ElementOverlay.color = Color.white;
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