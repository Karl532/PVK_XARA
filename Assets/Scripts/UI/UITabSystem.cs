using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UITabSystem : MonoBehaviour
{
    private List<UITabButton> tabButtons = new List<UITabButton>();
    private List<GameObject> tabContents = new List<GameObject>();
    private int currentTabIndex = 0;

    public void AddTab(UITabButton button, GameObject content)
    {
        int index = tabButtons.Count;
        tabButtons.Add(button);
        tabContents.Add(content);

        button.GetButton().onClick.AddListener(() => SelectTab(index));

        // Hide content by default
        content.SetActive(false);
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabButtons.Count) return;

        currentTabIndex = index;

        // Update all tabs
        for (int i = 0; i < tabButtons.Count; i++)
        {
            tabButtons[i].SetActive(i == index);
            tabContents[i].SetActive(i == index);
        }
    }

    public void Initialize()
    {
        if (tabButtons.Count > 0)
        {
            SelectTab(0);
        }
    }
}