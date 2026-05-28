using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_ActionEntry : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TMP_Text _valueText;

    public void Setup(Sprite icon, string value)
    {
        if (_icon != null)
        {
            _icon.sprite = icon;
            _icon.enabled = icon != null;
        }

        if (_valueText != null)
            _valueText.text = value;
    }

    public void SetValue(string value)
    {
        if (_valueText != null)
            _valueText.text = value;
    }
}
