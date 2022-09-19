using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MultiDropdown : MonoBehaviour
{
    [SerializeField] Text captionText;
    [SerializeField] RectTransform dropdownList;
    public List<Dropdown.OptionData> options;
    [SerializeField] RectTransform optionTemplate;

    public List<int> values = new List<int>();

    List<RectTransform> optionToggles = new List<RectTransform>();

    private void Awake()
    {
        for (int i = 0; i < options.Count; i++)
        {
            RectTransform newOption = Instantiate(optionTemplate, optionTemplate.parent);

            newOption.GetComponent<Toggle>().isOn = values.Contains(i);

            newOption.name = optionTemplate.name + " " + i;
            newOption.Find("Item Background").GetComponent<Image>().sprite = options[i].image;
            newOption.Find("Item Label").GetComponent<Text>().text = options[i].text;

            optionToggles.Add(newOption);
        }
        optionTemplate.gameObject.SetActive(false);

        UpdateCaptionText();
    }

    void OnValidate()
    {
        if (captionText != null)
        {
            UpdateCaptionText();
        }
    }

    public void AddValue(int value)
    {
        Toggle toggleComponent = optionToggles[value].GetComponent<Toggle>();
        toggleComponent.isOn = true;
    }

    void UpdateCaptionText()
    {
        if (values.Count == 0)
        {
            captionText.text = "None";
        }
        else if (values.Count > 1)
        {
            captionText.text = "Mixed";
        }
        else
        {
            captionText.text = options[values[0]].text;
        }
    }

    public void OnOptionToggle(RectTransform toggle)
    {
        if (optionToggles.Contains(toggle))
        {
            int value = optionToggles.IndexOf(toggle);

            if (toggle.GetComponent<Toggle>().isOn)
            {
                if (!values.Contains(value))
                {
                    values.Add(value);
                }
            }
            else
            {
                values.Remove(value);
            }
        }

        UpdateCaptionText();
    }

    public void ToggleDropdown()
    {
        dropdownList.gameObject.SetActive(!dropdownList.gameObject.activeSelf);
    }
}
