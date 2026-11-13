using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SunSwitch.UI
{
    [AddComponentMenu("")]
    public sealed class MenuRibbon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
    {
        public GameObject marker;
        public Text label;
        public Outline focusOutline;
        bool hovered, selected, pressed;
        float emphasis;
        void OnDisable()
        {
            hovered = selected = pressed = false; emphasis = 0;
            transform.localScale = Vector3.one;
            if (marker != null) marker.SetActive(false);
            if (focusOutline != null) focusOutline.enabled = false;
            if (label != null) label.rectTransform.anchoredPosition = Vector2.zero;
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; }
        public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; }
        public void OnSelect(BaseEventData e) { selected = true; }
        public void OnDeselect(BaseEventData e) { selected = false; pressed = false; }
        public void OnPointerDown(PointerEventData e) { pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
        void Update()
        {
            bool active = GetComponent<Button>().IsInteractable() && (hovered || selected);
            emphasis = Mathf.MoveTowards(emphasis, active ? 1 : 0, Time.unscaledDeltaTime / 0.15f);
            marker.SetActive(active);
            focusOutline.enabled = active;
            label.color = GetComponent<Button>().IsInteractable()
                ? StorybookGraphic.Hex("FFF5DE") : StorybookGraphic.Hex("B7AD99");
            label.rectTransform.anchoredPosition = new Vector2(0, pressed ? -3 : 0);
            transform.localScale = Vector3.one * (1 + emphasis * 0.015f);
        }
    }
}
