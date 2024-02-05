using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class UICopyTextToClipboard : MonoBehaviour
	{
		private TMP_Text _text;
		private Button _button;

		private void Awake()
		{
			_text = GetComponentInChildren<TMP_Text>();
			_button = GetComponent<Button>();
		}

		private void Start()
		{
			_button.onClick.AddListener(CopyTextToClipboard);
		}

		public void CopyTextToClipboard()
		{
			GUIUtility.systemCopyBuffer = _text.text;
		}
	}
}