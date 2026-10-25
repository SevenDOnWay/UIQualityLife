using UnityEngine;
using UnityEngine.EventSystems;

namespace UIQualityLife.Runtime.Demo.Studio
{
    /// <summary>
    /// Attach to any component spawned on the Canvas to enable direct click-to-select in the Runtime Inspector.
    /// </summary>
    public class UIStudioSelectable : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            if (UIDemoStudioHub.IsInspectMode)
            {
                UIStudioSelection.Select(gameObject);
                eventData.Use();
            }
        }
    }
}
