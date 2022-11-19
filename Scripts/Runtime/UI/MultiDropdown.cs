using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MultiDropdown : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI captionText;
    [SerializeField] RectTransform dropdownList;
    public List<Dropdown.OptionData> options;
    [SerializeField] RectTransform optionTemplate;

    public List<int> values = new List<int>();

    List<RectTransform> optionToggles = new List<RectTransform>();

    void OnValidate()
    {
        if(captionText != null)
        {
            UpdateCaptionText();
        }
    }

    private void Awake()
    {
        for(int i = 0; i < options.Count; i++)
        {
            RectTransform newOption = Instantiate(optionTemplate, optionTemplate.parent);

            newOption.GetComponent<Toggle>().SetIsOnWithoutNotify(values.Contains(i));

            newOption.name = optionTemplate.name + " " + i;
            newOption.Find("Item Background").GetComponent<Image>().sprite = options[i].image;
            newOption.Find("Item Label").GetComponent<Text>().text = options[i].text;

            optionToggles.Add(newOption);
        }
        optionTemplate.gameObject.SetActive(false);

        UpdateCaptionText();
    }

    public void AddValue(int value)
    {
        Toggle toggleComponent = optionToggles[value].GetComponent<Toggle>();
        toggleComponent.SetIsOnWithoutNotify(true);
        if(!values.Contains(value))
        {
            values.Add(value);
            UpdateCaptionText();
        }
    }

    void UpdateCaptionText()
    {
        if(values.Count == 0)
        {
            captionText.text = "None";
        }
        else if(values.Count > 1)
        {
            captionText.text = "Mixed";
        }
        else
        {
            int index = values[0];
            if(options.Count > 0 && index >= 0 && index < options.Count)
            {
                captionText.text = options[index].text;
            }
        }
    }

    public void OnOptionToggle(RectTransform toggle)
    {
        int value = optionToggles.IndexOf(toggle);

        if(toggle.GetComponent<Toggle>().isOn)
        {
            if(!values.Contains(value))
            {
                values.Add(value);
            }
        }
        else
        {
            values.Remove(value);
        }

        UpdateCaptionText();
    }

    public void ToggleDropdown()
    {
        dropdownList.gameObject.SetActive(!dropdownList.gameObject.activeSelf);
    }
}
