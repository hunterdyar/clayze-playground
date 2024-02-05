using System;
using TMPro;
using UnityEngine;

public class UIJoinCodeInputFieldPaste : MonoBehaviour
{
	private TMP_InputField _input;
	private void Awake()
	{
		_input = GetComponent<TMP_InputField>();
	}

	private void OnEnable()
	{
		_input.onSelect.AddListener(OnSelect);
	}

	private void OnDisable()
	{
		_input.onSelect.RemoveListener(OnSelect);
	}

	private void OnSelect(string text)
	{
		string clipboard = GUIUtility.systemCopyBuffer;
        if (clipboard.Length == 6)
        {
            _input.text = clipboard;
        }
	}
}