using System;
using UnityEngine;

namespace Spotlight.Presentation
{
    /// <summary>只缩放 Visual 子节点；时间由主程逐帧传入，组件自身不写 Update。</summary>
    [DisallowMultipleComponent]
    public sealed class ElementVisual2D : MonoBehaviour
    {
        private const float MinAmplitude = 0f;
        private const float MaxAmplitude = 0.08f;

        public Transform VisualRoot;
        public float PulseAmplitude = 0.04f;
        public float PulsePeriodSeconds = 1.2f;

        private Vector3 baseScale = Vector3.one;
        private bool cached;
        private bool warnedSelfRoot;

        private void Awake() { CacheBaseScale(); }
        private void OnEnable() { ResetVisual(); }
        private void OnDisable() { ResetVisual(); }

        /// <summary>按游戏时间求呼吸缩放；相同输入必得相同输出。</summary>
        public void ApplyVisualTime(double gameTime)
        {
            if (VisualRoot == null) return;
            if (!cached) CacheBaseScale();
            if (!cached) return;
            if (PulsePeriodSeconds <= 0f) { ResetVisual(); return; }
            double cycles = gameTime / PulsePeriodSeconds;
            double fraction = cycles - Math.Floor(cycles);
            float amplitude = Mathf.Clamp(PulseAmplitude, MinAmplitude, MaxAmplitude);
            float factor = 1f + amplitude * Mathf.Sin((float)(fraction * (Math.PI * 2.0)));
            VisualRoot.localScale = baseScale * factor;
        }

        /// <summary>恢复缓存的基础缩放；启用、停用、池回收后都回到这里。</summary>
        public void ResetVisual()
        {
            if (VisualRoot == null) return;
            if (!cached) CacheBaseScale();
            if (!cached) return;
            VisualRoot.localScale = baseScale;
        }

        private void CacheBaseScale()
        {
            if (VisualRoot == null) return;
            if (VisualRoot == transform && !warnedSelfRoot)
            {
                warnedSelfRoot = true;
                Debug.LogWarning("ElementVisual2D 的 VisualRoot 指向了根节点：根节点缩放会被 ViewPool 覆盖，请改绑 Visual 子节点。" + name, this);
            }
            baseScale = VisualRoot.localScale;
            cached = true;
        }
    }
}
