using TMPro;
using UnityEngine;

public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;

    private void Awake()
    {
        HidePrompt();
    }

    public void ShowPrompt(string text)
    {
        promptText.text = text;
        promptRoot.SetActive(true);
    }

    public void HidePrompt()
    {
        promptRoot.SetActive(false);
    }
}