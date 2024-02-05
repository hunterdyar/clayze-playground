using System;
using TMPro;
using UnityEngine;

namespace UI
{
	public class UIRoomCodeText : MonoBehaviour
	{
		private TMP_Text _text;

		private void Awake()
		{
			_text = GetComponent<TMP_Text>();
			_text.text = "";
		}

		private void Start()
		{
			OnRoomCodeChange(Initializer.RoomCode);
		}

		private void OnEnable()
		{
			Initializer.OnRoomCodeChange += OnRoomCodeChange;
		}

		private void OnDisable()
		{
			Initializer.OnRoomCodeChange -= OnRoomCodeChange;
		}

		private void OnRoomCodeChange(string rc)
		{
			_text.text = rc;
		}
	}
}