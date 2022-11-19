using TMPro;
using UnityEngine.EventSystems;
using UnityEngine;

public class ClickableText : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();

        if(eventData.button == PointerEventData.InputButton.Left)
        {
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, null);
            if(linkIndex > -1)
            {
                TMP_LinkInfo linkInfo = text.textInfo.linkInfo[linkIndex];

                Application.OpenURL(linkInfo.GetLinkText());
            }
        }
    }
}
