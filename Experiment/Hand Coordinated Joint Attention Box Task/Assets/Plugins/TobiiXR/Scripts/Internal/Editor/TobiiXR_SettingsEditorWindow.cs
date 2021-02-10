// Copyright © 2018 – Property of Tobii AB (publ) - All Rights Reserved
namespace Tobii.XR
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEditor;
    using UnityEditorInternal;
    using UnityEngine;

    public class TobiiXR_SettingsEditorWindow : EditorWindow 
	{
		private static readonly string TobiiXR_SettingsAssetPath = PathCombine("Internal", "Resources", typeof(TobiiXR_Settings).Name + ".asset");
		private TobiiXR_Settings _settings;

		private readonly TypeDropDownData _standaloneDropDownData = new TypeDropDownData(BuildTargetGroup.Standalone);
		private readonly TypeDropDownData _androidDropDownData = new TypeDropDownData(BuildTargetGroup.Android);

		public class TypeDropDownData
		{
			public string TypeString { get { return TypeStrings[SelectedType]; } }
			public readonly string[] TypeStrings;
            public int SelectedType;
            private readonly BuildTargetGroup _targetGroup;

			public TypeDropDownData(BuildTargetGroup targetGroup)
			{
				var types = EditorUtils.EyetrackingProviderTypes(targetGroup).ToArray();
				TypeStrings = new string[types.Length];

				for(int i = 0; i < types.Length; i++)
				{
					TypeStrings[i] = types[i].FullName;
				}

                _targetGroup = targetGroup;
            }

			public void SetSelectedType(string type)
			{
				SelectedType = Array.IndexOf(TypeStrings, type);
			}

			public void ShowDropDown(TobiiXR_Settings settings, ref string eyeTrackingProviderTypeString)
			{
				EditorGUI.BeginChangeCheck();
				var selected = EditorGUILayout.Popup(_targetGroup.ToString(), SelectedType, TypeStrings);
			
				if (EditorGUI.EndChangeCheck())
				{
					Undo.RecordObject(settings, _targetGroup.ToString()+" Provider changed");
					SelectedType = selected;
					eyeTrackingProviderTypeString = TypeString;
					SetDirty(settings);
					EditorUtils.UpdateCompilerFlags(settings);
				}
			}
		}

		[InitializeOnLoadMethod]
		public static void OnProjectLoadedInEditor()
		{
		    EditorApplication.delayCall += () =>
		    {
		        var config = LoadOrCreateDefaultConfiguration();
		        EditorUtils.UpdateCompilerFlags(config);
            };
        }

        [MenuItem("Window/Tobii/Tobii Settings")]
		public static void ShowWindow() 
		{
			GetWindow<TobiiXR_SettingsEditorWindow>("Tobii Settings").Show();
		}

        private void OnEnable() 
		{
			_settings = LoadOrCreateDefaultConfiguration();

			_standaloneDropDownData.SetSelectedType(_settings.EyeTrackingProviderTypeStandAlone);
			_androidDropDownData.SetSelectedType(_settings.EyeTrackingProviderTypeAndroid);

		    EditorUtils.UpdateCompilerFlags(_settings);
		}


        private void OnGUI() 
		{
			EditorGUILayout.LabelField("Information", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Change settings used to initialize Tobii XR.");
			
			EditorGUILayout.Space();
		    EditorGUILayout.Separator();
		    EditorGUILayout.LabelField("Focused Object Settings", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(Application.isPlaying);

            var defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 250;
            EditorGUI.BeginChangeCheck();
		    var layer = EditorGUILayout.MaskField(new GUIContent("LayerMask", "What layers should be considered for finding gaze focusable objects."), InternalEditorUtility.LayerMaskToConcatenatedLayersMask(_settings.LayerMask), InternalEditorUtility.layers);

		    if (EditorGUI.EndChangeCheck())
		    {
		        Undo.RecordObject(_settings, "Layers changed");
		        _settings.LayerMask = InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(layer);
		        SetDirty(_settings);
		    }

            EditorGUI.BeginChangeCheck();
			var expectedNumberOfObjects = EditorGUILayout.IntField(new GUIContent("Expected Number of Objects", "How many gaze focusable objects do we expect to be in direct line of sight simultaneously."), _settings.ExpectedNumberOfObjects);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(_settings, "ExpectedNumberOfObjects changed");
				_settings.ExpectedNumberOfObjects = expectedNumberOfObjects;
				SetDirty(_settings);
			}

			EditorGUI.BeginChangeCheck();
			var howLongToKeepCandidatesInSeconds = EditorGUILayout.FloatField(new GUIContent("How Long To Keep Candidates In Seconds", "How long do we keep gaze focusable objects in memory."), _settings.HowLongToKeepCandidatesInSeconds);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(_settings, "HowLongToKeepCandidatesInSeconds changed");
				_settings.HowLongToKeepCandidatesInSeconds = howLongToKeepCandidatesInSeconds;
				SetDirty(_settings);
			}

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Eyetracking Data Provider", EditorStyles.boldLabel);

			_standaloneDropDownData.ShowDropDown(_settings, ref _settings.EyeTrackingProviderTypeStandAlone);
			_androidDropDownData.ShowDropDown(_settings, ref _settings.EyeTrackingProviderTypeAndroid);

		    EditorGUIUtility.labelWidth = defaultLabelWidth;

            EditorGUI.EndDisabledGroup();
		}

		private static void SetDirty(UnityEngine.Object obj)
		{
			AssetDatabase.Refresh();
    		EditorUtility.SetDirty(obj);
    		AssetDatabase.SaveAssets();
		}

		private static TobiiXR_Settings LoadOrCreateDefaultConfiguration()
		{
			bool resourceExists;
			var settings = TobiiXR_Settings.CreateDefaultSettings(out resourceExists);

			if(!resourceExists)
			{
				var sdkPath = Path.GetDirectoryName(FindPathToClass(typeof(TobiiXR)));
				var filePath =  PathCombine(sdkPath, TobiiXR_SettingsAssetPath);
				var assetPath = filePath.Replace(Application.dataPath, "Assets");

				if(File.Exists(filePath))
				{
					AssetDatabase.Refresh();
					settings = AssetDatabase.LoadAssetAtPath<TobiiXR_Settings>(assetPath);
					return settings;
				}
				
				AssetDatabase.CreateAsset(settings, assetPath);
				AssetDatabase.SaveAssets();
			}

			return settings;
		}

		private static string PathCombine(params string[] paths)
		{
			return paths.Aggregate((acc, p) => Path.Combine(acc, p));
		}

		private static string FindPathToClass(Type type)
		{
			var filename = type.Name + ".cs";
			return FindAFileRecursively(Application.dataPath, filename);
		}

		private static string FindAFileRecursively(string startDir, string filename)
		{
			foreach(var file in Directory.GetFiles(startDir))
			{
				if(Path.GetFileName(file).Equals(filename)) return file;
			}

			foreach(var dir in Directory.GetDirectories(startDir))
			{
				var file = FindAFileRecursively(dir, filename);
				if(file != null) return file;
			}

			return null;
		}
    }
}