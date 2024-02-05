using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RuntimeConsole : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    private static RuntimeConsole Instance;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("Multiple Runtime Console exist.");
        }

        _text.text = "";
    }

    public static void Log(object message, bool alsoDebug = true)
    {
        if (Instance == null)
        {
            Debug.Log(message);
            return;
        }
        
        if (alsoDebug)
        {
            Debug.Log(message);
        }
        
        string m = message.ToString();
        Instance.AddLine(m);
    }
    public void AddLine(string line)
    {
        _text.text = _text.text + "\n" + line;
    }
}
