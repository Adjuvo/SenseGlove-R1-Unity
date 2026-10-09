using System.Collections.Generic;
using UnityEngine;

public class R1_MessageLog : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text textMesh;
    [SerializeField] private int maxMessages = 25;

    private readonly Queue<string> messages = new();

    public string MessageText
    {
        get { return textMesh != null ? textMesh.text : ""; }
        set { if (textMesh != null) { textMesh.text = value; } }
    }

    public void AddMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        if (message[message.Length - 1] == '\n')
            messages.Enqueue(message);
        else
            messages.Enqueue(message + "\n"); //should avoid me modifying the original parameter

        while (messages.Count > maxMessages)
        {
            messages.Dequeue();
        }

        RefreshText();
    }

    public void ClearMessages()
    {
        messages.Clear();
        RefreshText();
    }

    private void RefreshText()
    {
        MessageText = string.Join("", messages);
    }
}
