using UnityEngine;
namespace Spotlight.Presentation
{
    internal static class PresentationObjects
    {
        internal static void Release(Object value)
        {
            if(value==null)return;
            if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value);
        }
    }
}
