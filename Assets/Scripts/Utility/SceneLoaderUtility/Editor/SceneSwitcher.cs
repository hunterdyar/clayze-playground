using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace UnityToolbarExtender.Examples
{
	static class ToolbarStyles
	{
		public static readonly GUIStyle commandButtonStyle;

		static ToolbarStyles()
		{
			commandButtonStyle = new GUIStyle("Command")
			{
				fontSize = 16,
				alignment = TextAnchor.MiddleCenter,
				imagePosition = ImagePosition.ImageAbove,
				fontStyle = FontStyle.Bold
			};
		}
	}

	[InitializeOnLoad]
	public class SceneSwitchLeftButton
	{
		static SceneSwitchLeftButton()
		{
			ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
		}

		static void OnToolbarGUI()
		{
			GUILayout.FlexibleSpace();

			if(GUILayout.Button(new GUIContent("C", "Start Connect Scene"), ToolbarStyles.commandButtonStyle))
			{
				SceneHelper.StartScene("Connect");
			}
		}
	}

	static class SceneHelper
	{
		static string sceneToOpen;
		private static SceneSetup[] _sceneSetup;

		static SceneHelper()
		{
			EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
		}
		public static void StartScene(string sceneName)
		{
			if(EditorApplication.isPlaying)
			{
				EditorApplication.isPlaying = false;
				//we listen to the statechange with an event.
			}
			else
			{
				Debug.Log("Entering Connect Scene");
				//if we are entering playmode, save the loaded scenes.
				_sceneSetup = EditorSceneManager.GetSceneManagerSetup();
				sceneToOpen = sceneName;
				EditorApplication.update += WaitUntilCanEnterPlayMode;//enter playmode once we have finished compiling, saving, etc.
			}
		}

		private static void EditorApplicationOnplayModeStateChanged(PlayModeStateChange stateChange)
		{
			if (stateChange == PlayModeStateChange.EnteredEditMode)
			{
				Debug.Log(stateChange);
				//EditorApplication.playModeStateChanged -= EditorApplicationOnplayModeStateChanged;

				//todo: This is getting set to null when we reload the domain on entering playmode...
				if (_sceneSetup != null)
				{
					EditorSceneManager.RestoreSceneManagerSetup(_sceneSetup);
					_sceneSetup = null;
				}
			}
		}
		
		static void WaitUntilCanEnterPlayMode()
		{
			if (sceneToOpen == null ||
			    EditorApplication.isPlaying || EditorApplication.isPaused ||
			    EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
			{
				return;
			}
			

			EditorApplication.update -= WaitUntilCanEnterPlayMode;

			if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				// need to get scene via search because the path to the scene
				// file contains the package version so it'll change over time
				string[] guids = AssetDatabase.FindAssets("t:scene " + sceneToOpen, null);
				if (guids.Length == 0)
				{
					Debug.LogWarning("Couldn't find scene file");
				}
				else
				{
					string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
					EditorSceneManager.OpenScene(scenePath);
					EditorApplication.isPlaying = true;
				}
			}
			sceneToOpen = null;
		}
	}
}
