using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

public class ButtonWithText : MonoBehaviour
{
    private Button _button;
    private TMP_Text _text;
    private bool _initComplete;

    public Button Button => _button;
    public TMP_Text TMP_Text => _text;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        _button = GetComponent<Button>();
        _text = GetComponentInChildren<TMP_Text>();
    }

    public ButtonClickedEvent onClick => _button.onClick;

    public string Text
    {
        set
        {
            _text.text = value;
        }
    }

    public bool Interactable
    {
        set
        {
            _button.interactable = value;
        }
    }
}
