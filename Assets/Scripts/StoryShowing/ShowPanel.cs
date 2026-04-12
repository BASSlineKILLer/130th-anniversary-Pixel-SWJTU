using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class ShowPanel : MonoBehaviour
{
    public TextMeshProUGUI text;
    private List<string> texts = new List<string>();
    private int currentPage = 0;

    public UnityEvent onPanelHidden;

    private bool isVisible = false;

    void Start()
    {
        if (!isVisible) 
        {
            gameObject.SetActive(false);
        }
    }    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            currentPage = Mathf.Max(0, currentPage - 1);
            UpdateText();
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            currentPage = Mathf.Min(texts.Count - 1, currentPage + 1);
            UpdateText();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            Hide();
        }
    }

    public void Show(List<string> storyTexts)
    {
        texts = storyTexts;
        currentPage = 0;
        isVisible = true;
        Debug.Log("ShowPanel.Show called with " + texts.Count + " texts");
        UpdateText();
        gameObject.SetActive(true);
    }

    private void UpdateText()
    {
        if (texts.Count > 0 && currentPage < texts.Count)
        {
            if (text != null)
            {
                text.text = texts[currentPage];
                Debug.Log($"Displaying page {currentPage}: '{text.text}'");
            }
            else
            {
                Debug.Log("text is null");
            }
        }
        else
        {
            Debug.Log("No text to display or invalid page");
        }
    }

    public void Hide()
    {
        isVisible = false;
        gameObject.SetActive(false);
        onPanelHidden.Invoke();
    }
}
