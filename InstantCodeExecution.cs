using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

using Object = UnityEngine.Object;

/// <summary>Editor tool used to execute code in the editor</summary>
public class InstantCodeExecution : EditorWindow
{
	const string INCLUDES = "using System;\nusing System.Collections;\nusing System.Collections.Generic;\nusing System.IO;\nusing System.Linq;\nusing UnityEditor;\nusing UnityEditor.SceneManagement;\nusing UnityEngine;\nusing UnityEngine.SceneManagement;\nusing UnityEngine.UI;\nusing Object = UnityEngine.Object;\nusing Random = UnityEngine.Random;";
	const string ICE_TEMPLATE = "\n\nclass ICE : UnityEngine.Object\n{\n\t[InitializeOnLoadMethod]\n\tpublic static void Execute()\n\t{\n\t\t" + PLACEHOLDER_MARK + "\n\n\t\tAssetDatabase.DeleteAsset(\"" + ICE_PATH + "\");\n\t}\n" + FIND_METHODS + SPAWN_METHODS + PLACEMENT_METHODS;

	const string FIND_METHODS = "\n\tstatic T Find<T>(string name = null, Func<T, bool> customCheck = null) where T : Object\n\t{\n\t\tT[] instances = FindObjectsOfType<T>(true);\n\n\t\tif (string.IsNullOrEmpty(name))\n\t\t{\n\t\t\tif (customCheck != null)\n\t\t\t{\n\t\t\t\tforeach (T instance in instances)\n\t\t\t\t{\n\t\t\t\t\tif (customCheck.Invoke(instance))\n\t\t\t\t\t\treturn instance;\n\t\t\t\t}\n\t\t\t}\n\n\t\t\treturn instances[0];\n\t\t}\n\n\t\tforeach (T instance in instances)\n\t\t{\n\t\t\tbool nameCheck = instance.name == name;\n\n\t\t\tif (nameCheck)\n\t\t\t{\n\t\t\t\tif (customCheck != null && customCheck.Invoke(instance))\n\t\t\t\t\treturn instance;\n\t\t\t\telse\n\t\t\t\t\treturn instance;\n\t\t\t}\n\t\t}\n\n\t\treturn null;\n\t}\n\n\tstatic GameObject Find(string name, Func<GameObject, bool> customCheck = null)\n\t{\n\t\treturn Find<GameObject>(name, customCheck);\n\t}\n\n\tstatic T[] FindAll<T>(string name = null, Func<T, bool> customCheck = null) where T : Object\n\t{\n\t\tT[] instances = FindObjectsOfType<T>(true);\n\t\tList<T> result = new List<T>();\n\n\t\tif (string.IsNullOrEmpty(name))\n\t\t{\n\t\t\tif (customCheck != null)\n\t\t\t{\n\t\t\t\tforeach (T instance in instances)\n\t\t\t\t{\n\t\t\t\t\tif (customCheck.Invoke(instance))\n\t\t\t\t\t\tresult.Add(instance);\n\t\t\t\t}\n\t\t\t}\n\t\t\telse\n\t\t\t\tresult.AddRange(instances);\n\n\t\t\treturn result.ToArray();\n\t\t}\n\n\t\tforeach (T instance in instances)\n\t\t{\n\t\t\tbool nameCheck = instance.name.Contains(name);\n\n\t\t\tif (nameCheck)\n\t\t\t{\n\t\t\t\tif (customCheck != null)\n\t\t\t\t{\n\t\t\t\t\tif (customCheck.Invoke(instance))\n\t\t\t\t\t\tresult.Add(instance);\n\t\t\t\t}\n\t\t\t\telse\n\t\t\t\t\tresult.Add(instance);\n\t\t\t}\n\t\t}\n\n\t\treturn result.ToArray();\n\t}\n\n\tstatic GameObject[] FindAll(string name, Func<GameObject, bool> customCheck = null)\n\t{\n\t\treturn FindAll<GameObject>(name, customCheck);\n\t}\n";
	const string SPAWN_METHODS = "\n\tstatic T[] SpawnMultiple<T>(T model, int count, Vector3[] positions = null, Quaternion[] rotations = null, Transform[] parents = null) where T : Component\n\t{\n\t\tVector3 defaultPos = model.transform.position;\n\t\tQuaternion defaultRot = model.transform.rotation;\n\t\tList<T> result = new List<T>();\n\n\t\tfor (int i = 0; i < count; i++)\n\t\t{\n\t\t\tVector3 position = defaultPos;\n\n\t\t\tif (positions != null)\n\t\t\t{\n\t\t\t\tif (positions.Length > 1)\n\t\t\t\t\tposition = positions[i];\n\t\t\t\telse\n\t\t\t\t\tposition = positions[0];\n\t\t\t}\n\n\t\t\tQuaternion rotation = defaultRot;\n\n\t\t\tif (rotations != null)\n\t\t\t{\n\t\t\t\tif (rotations.Length > 1)\n\t\t\t\t\trotation = rotations[i];\n\t\t\t\telse\n\t\t\t\t\trotation = rotations[0];\n\t\t\t}\n\n\t\t\tTransform parent = null;\n\n\t\t\tif (parents != null)\n\t\t\t{\n\t\t\t\tif (parents.Length > 1)\n\t\t\t\t\tparent = parents[i];\n\t\t\t\telse\n\t\t\t\t\tparent = parents[0];\n\t\t\t}\n\n\t\t\tresult.Add((T)Instantiate(model, position, rotation, parent));\n\t\t}\n\n\t\tif (result.Count == 0)\n\t\t\treturn null;\n\t\telse\n\t\t\treturn result.ToArray();\n\t}\n\n\tstatic T[] SpawnMultipleWithPrefabLink<T>(T model, int count, Vector3[] positions = null, Quaternion[] rotations = null, Transform[] parents = null) where T : Component\n\t{\n\t\tVector3 defaultPos = model.transform.position;\n\t\tQuaternion defaultRot = model.transform.rotation;\n\t\tList<T> result = new List<T>();\n\n\t\tfor (int i = 0; i < count; i++)\n\t\t{\n\t\t\tVector3 position = defaultPos;\n\n\t\t\tif (positions != null)\n\t\t\t{\n\t\t\t\tif (positions.Length > 1)\n\t\t\t\t\tposition = positions[i];\n\t\t\t\telse\n\t\t\t\t\tposition = positions[0];\n\t\t\t}\n\n\t\t\tQuaternion rotation = defaultRot;\n\n\t\t\tif (rotations != null)\n\t\t\t{\n\t\t\t\tif (rotations.Length > 1)\n\t\t\t\t\trotation = rotations[i];\n\t\t\t\telse\n\t\t\t\t\trotation = rotations[0];\n\t\t\t}\n\n\t\t\tTransform parent = null;\n\n\t\t\tif (parents != null)\n\t\t\t{\n\t\t\t\tif (parents.Length > 1)\n\t\t\t\t\tparent = parents[i];\n\t\t\t\telse\n\t\t\t\t\tparent = parents[0];\n\t\t\t}\n\n\t\t\tT obj = (T)PrefabUtility.InstantiatePrefab(model);\n\n\t\t\tobj.transform.SetParent(parent);\n\t\t\tobj.transform.position = position;\n\t\t\tobj.transform.rotation = rotation;\n\n\t\t\tresult.Add(obj);\n\t\t}\n\n\t\tif (result.Count == 0)\n\t\t\treturn null;\n\t\telse\n\t\t\treturn result.ToArray();\n\t}\n";
	const string PLACEMENT_METHODS = "\n\tstatic void PlaceOnLine(Transform[] objects, Vector3 startPos, Vector3 direction, float distance, bool alignRotation = false)\n\t{\n\t\tfloat totalSize = distance * (objects.Length - 1);\n\t\tVector3 endPos = startPos + direction.normalized * totalSize;\n\n\t\tfor (int i = 0; i < objects.Length; i++)\n\t\t{\n\t\t\tobjects[i].position = Vector3.Lerp(startPos, endPos, (float)i / (objects.Length - 1));\n\n\t\t\tif (alignRotation)\n\t\t\t\tobjects[i].LookAt(endPos + direction);\n\t\t}\n\t}\n\n\tstatic void PlaceOnCircle(Transform[] objects, Vector3 startPos, Vector3 circleNormal, float radius, bool alignRotation = false)\n\t{\n\t\tVector3 dir = Vector3.forward * radius;\n\t\tGameObject holder = new GameObject(\"Holder\");\n\t\tholder.transform.position = startPos;\n\n\t\tList<Transform> previousParent = new List<Transform>();\n\n\t\tfor (int i = 0; i < objects.Length; i++)\n\t\t{\n\t\t\tpreviousParent.Add(objects[i].transform.parent);\n\n\t\t\tobjects[i].position = startPos + Quaternion.Euler(0, (float)i * 360 / objects.Length, 0) * dir;\n\t\t\tobjects[i].SetParent(holder.transform);\n\t\t}\n\n\t\tif (!alignRotation)\n\t\t{\n\t\t\tforeach (Transform child in holder.transform)\n\t\t\t\tchild.rotation = Quaternion.identity;\n\t\t}\n\n\t\tholder.transform.up = circleNormal;\n\n\t\tfor (int i = 0; i < objects.Length; i++)\n\t\t\tobjects[i].SetParent(previousParent[i]);\n\n\t\tDestroyImmediate(holder.gameObject);\n\t}\n\n\tstatic void SnapToGrid(Transform[] objects, float size, Vector3 startPos = default(Vector3))\n\t{\n\t\tforeach (Transform obj in objects)\n\t\t{\n\t\t\tVector3 pos = (obj.position - startPos) / size;\n\t\t\tpos = new Vector3(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));\n\t\t\tobj.position = pos * size + startPos;\n\t\t}\n\t}\n\n\tstatic Object GetAsset(string name, Type type)\n\t{\n\t\tstring[] guids = AssetDatabase.FindAssets(\"t:\" + type.Name + \" \" + name);\n\n\t\tif (guids.Length > 0)\n\t\t\treturn AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guids[0]), type);\n\t\treturn null;\n\t}\n}";

	const string REMOTE_NAME = "RemoteICEMethods";
	const string REMOTE_CLASS = "\n\nstatic class " + REMOTE_NAME + "\n{\n" + INSERTION_NOTE + "\n}";
	const string METHOD_NAME_MARK = "[Method name]";
	const string METHOD_BODY_MARK = "[Method body]";
	const string METHOD_FORMAT = "[ICE]\nstatic void " + METHOD_NAME_MARK + "()\n\t{\n\t\t" + METHOD_BODY_MARK + "\n\t}\n\n";
	const string INSERTION_NOTE = "\t// method insertion point (do not remove)";

	public const string ICE_PATH = "Assets/ICE.cs";
	public const string PLACEHOLDER_MARK = "[Placeholder]";

	public const string REMOTE_PATH = "Assets/Editor/RemoteICEMethods.cs";

	const string SAVE_KEY = "ICE_Presets";
	const string SELECTED_KEY = "ICE_Selected";

	static string detectedRemotePath;

	int SelectedPreset
	{
		get => Mathf.Clamp(PlayerPrefs.GetInt(SELECTED_KEY, -1), -1, presets.Count - 1);
		set => PlayerPrefs.SetInt(SELECTED_KEY, Mathf.Clamp(value, -1, presets.Count - 1));
	}

	InstantCodeExecution window;

	GUIStyle codeAreaStyle;
	GUIStyle boldCenterStyle;
	GUIStyle boldStyle;

	List<CodePreset> presets;
	List<ICEMethod> remoteMethods;
	Vector2 scroll;
	string presetName;
	string editorCode;
	bool remoteMode;

	[MenuItem("Tools/ICE")]
	static void ShowWindow()
	{
		InstantCodeExecution window = GetWindow<InstantCodeExecution>();
		window.titleContent = new GUIContent("Instant Code Execution");
		window.minSize = new Vector2(500, 400);

		window.position = new Rect(
			window.position.x,
			window.position.y,
			window.minSize.x,
			window.minSize.y
		);

		window.Init(window);
		window.Show();
	}

	void Init(InstantCodeExecution window)
	{
		this.window = window;
		editorCode = "";

		// this tool does not behave well between assembly reloads
		AssemblyReloadEvents.beforeAssemblyReload += () => window.Close();

		if (presets == null)
		{
			presets = new List<CodePreset>();

			if (PlayerPrefs.HasKey(SAVE_KEY))
				presets = JsonUtility.FromJson<PresetList>(PlayerPrefs.GetString(SAVE_KEY)).presets;
		}

		string[] guids = AssetDatabase.FindAssets("t:Script " + REMOTE_NAME);

		if (guids.Length == 0)
		{
			string dirPath = Path.GetFullPath(REMOTE_PATH.Substring(0, REMOTE_PATH.LastIndexOf("/")));

			if (!Directory.Exists(dirPath))
				Directory.CreateDirectory(dirPath);

			File.WriteAllText(Path.GetFullPath(REMOTE_PATH), INCLUDES + REMOTE_CLASS);
			AssetDatabase.Refresh();
			return;
		}
		else
			detectedRemotePath = AssetDatabase.GUIDToAssetPath(guids[0]);

		List<string> lines = new List<string>(File.ReadAllLines(detectedRemotePath));

		if (remoteMethods == null)
		{
			remoteMethods = new List<ICEMethod>();

			foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<ICEAttribute>())
			{
				if (!method.IsStatic || method.ReturnType != typeof(void))
					continue;

				string line = lines.Find(item => item.Contains("static void " + method.Name));
				remoteMethods.Add(new ICEMethod(method, line != null ? lines.IndexOf(line) : -1));
			}
		}

		if (SelectedPreset != -1)
			LoadPreset(SelectedPreset);
	}

	void GenerateIfNeeded()
	{
		if (codeAreaStyle == null)
			codeAreaStyle = new GUIStyle(GUI.skin.textArea) { wordWrap = true, stretchHeight = true };

		if (boldCenterStyle == null)
		{
			boldCenterStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold
			};
		}

		if (boldStyle == null)
			boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
	}

	void OnGUI()
	{
		GenerateIfNeeded();

		EditorGUILayout.LabelField("Instant Code Execution", boldCenterStyle);
		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.Space();
			GUI.color = remoteMode ? Color.white : Color.cyan;

			if (GUILayout.Button("Code console"))
				remoteMode = false;

			EditorGUILayout.Space();
			GUI.color = remoteMode ? Color.cyan : Color.white;

			if (GUILayout.Button("Remote code"))
				remoteMode = true;

			EditorGUILayout.Space();
			GUI.color = Color.white;
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

		if (remoteMode)
			DisplayRemotes();
		else
			DisplayConsole();

		EditorGUILayout.Space();
	}

	void DisplayRemotes()
	{
		scroll = EditorGUILayout.BeginScrollView(scroll, GUI.skin.box);
		{
			if (remoteMethods.Count == 0)
				EditorGUILayout.LabelField("No remote code found", boldCenterStyle, GUILayout.ExpandHeight(true));
			else
			{
				foreach (ICEMethod method in remoteMethods)
				{
					EditorGUILayout.BeginVertical(GUI.skin.box);
					{
						EditorGUILayout.BeginHorizontal();
						{
							EditorGUILayout.LabelField(method.methodName, boldStyle);
							EditorGUILayout.Space();

							if (method.isDefault && GUILayout.Button("Open file"))
								method.Open();

							GUI.color = Color.cyan;
							EditorGUILayout.Space();

							if (GUILayout.Button("Execute"))
								method.Execute();

							GUI.color = Color.white;
						}
						EditorGUILayout.EndHorizontal();

						EditorGUILayout.Space();

						for (int i = 0; i < method.argsInfos.Length; i++)
						{
							Type argType = method.argsInfos[i].ParameterType;

							if (argType.IsSubclassOf(typeof(Object)))
							{
								method.args[i] = EditorGUILayout.ObjectField(
									method.argsInfos[i].Name,
									(Object)method.args[i],
									argType,
									true
								);
							}
							else
								method.args[i] = DisplayArgument(argType, method.argsInfos[i].Name, method.args[i]);
						}
					}
					EditorGUILayout.EndVertical();
				}
			}
			EditorGUILayout.EndScrollView();
		}
	}

	T DisplayArgument<T>(Type argType, string label, T value)
	{
		if (argType.IsEnum)
		{
			return DirtyCast<T>(EditorGUILayout.Popup(
				label,
				value != null ? DirtyCast<int>(value) : 0,
				Enum.GetNames(argType)
			));
		}

		return argType switch
		{
			Type t when t == typeof(bool) => DirtyCast<T>(EditorGUILayout.Toggle(label, DirtyCast<bool>(value))),
			Type t when t == typeof(int) => DirtyCast<T>(EditorGUILayout.IntField(label, DirtyCast<int>(value))),
			Type t when t == typeof(float) => DirtyCast<T>(EditorGUILayout.FloatField(label, DirtyCast<float>(value))),
			Type t when t == typeof(string) => DirtyCast<T>(EditorGUILayout.TextField(label, DirtyCast<string>(value))),
			Type t when t == typeof(Color) => DirtyCast<T>(EditorGUILayout.ColorField(label, DirtyCast<Color>(value))),
			Type t when t == typeof(Vector2) =>
				DirtyCast<T>(EditorGUILayout.Vector2Field(label, DirtyCast<Vector2>(value))),
			Type t when t == typeof(Vector3) =>
				DirtyCast<T>(EditorGUILayout.Vector3Field(label, DirtyCast<Vector3>(value))),
			_ => DisplayNoSupport<T>(argType)
		};
	}

	T DirtyCast<T>(object value) => value != null ? (T)(object)value : default;

	T DisplayNoSupport<T>(Type argType)
	{
		EditorGUILayout.LabelField("This tool doesn't support type [" + argType + "]");
		return default;
	}

	void DisplayConsole()
	{
		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.LabelField("Preset name :", GUILayout.Width(80));
			presetName = EditorGUILayout.TextField(presetName);

			if (GUILayout.Button("API", GUILayout.Width(50)))
				ICE_API.ShowWindow();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		{
			for (int i = 0; i < 5; i++)
				PresetButton(i);
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		{
			for (int i = 5; i < 10; i++)
				PresetButton(i);
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.Space();

			if (presets.Count > 0 && GUILayout.Button("Delete preset"))
				DeletePreset();

			EditorGUILayout.Space();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
		EditorGUILayout.Space();

		scroll = EditorGUILayout.BeginScrollView(scroll);
		{
			editorCode = EditorGUILayout.TextArea(editorCode, codeAreaStyle).Replace("\t", "    ");

			if (SelectedPreset != -1)
			{
				presets[SelectedPreset].name = presetName;
				presets[SelectedPreset].code = editorCode;
				SavePresets();
			}
		}
		EditorGUILayout.EndScrollView();

		EditorGUILayout.Space();

		EditorGUILayout.BeginHorizontal();
		{
			EditorGUILayout.Space();

			if (GUILayout.Button("Execute"))
				Execute();

			EditorGUILayout.Space();

			if (GUILayout.Button("Save script"))
				Save();

			EditorGUILayout.Space();
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space();
	}

	void PresetButton(int index)
	{
		GUILayoutOption option = GUILayout.Width(window.position.width / 5 - 3.5f);

		if (index < presets.Count)
		{
			if (index == SelectedPreset)
				GUI.color = Color.cyan;

			if (GUILayout.Button(presets[index].name, option))
				LoadPreset(index);

			if (index == SelectedPreset)
				GUI.color = Color.white;
		}
		else if (index == presets.Count)
		{
			if (GUILayout.Button("+", option))
				CreatePreset();
		}
	}

	void LoadPreset(int index)
	{
		SelectedPreset = index;

		presetName = presets[SelectedPreset].name;
		editorCode = string.IsNullOrWhiteSpace(presets[SelectedPreset].code) ? "" : presets[SelectedPreset].code;

		Deselect();
	}

	void CreatePreset()
	{
		presets.Add(new CodePreset(presetName));
		LoadPreset(presets.Count - 1);

		SavePresets();
		Deselect();
	}

	void DeletePreset()
	{
		presets.RemoveAt(SelectedPreset);
		SavePresets();

		SelectedPreset = Mathf.Clamp(SelectedPreset, -1, presets.Count);

		if (SelectedPreset != -1)
			LoadPreset(SelectedPreset);

		Deselect();
	}

	void SavePresets()
	{
		PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(new PresetList(presets)));
	}

	void Execute()
	{
		File.WriteAllText(Path.GetFullPath(ICE_PATH), INCLUDES + ICE_TEMPLATE.Replace(PLACEHOLDER_MARK, editorCode));
		AssetDatabase.ImportAsset(ICE_PATH);

		Deselect();
	}

	void Save()
	{
		string scriptContent = AssetDatabase.LoadAssetAtPath<TextAsset>(REMOTE_PATH).text;
		int insertionIndex = scriptContent.IndexOf(INSERTION_NOTE);

		// cleanup preset name
		string presetName = presets[SelectedPreset].name;
		string methodName = "";
		bool needUpper = false;

		foreach (char c in presetName)
		{
			if (c == ' ')
				needUpper = true;
			else
			{
				if (needUpper)
				{
					methodName += char.ToUpper(c);
					needUpper = false;
				}
				else
					methodName += c;
			}
		}

		// same name method removal
		int existingIndex = scriptContent.IndexOf("static void " + methodName) - ("[ICE]\n".Length + 1);

		if (existingIndex > -1)
		{
			insertionIndex = existingIndex;
			int openCount = 0;
			bool canClose = false;

			for (int i = existingIndex; i < scriptContent.Length; i++)
			{
				char c = scriptContent[i];

				if (c == '{')
				{
					canClose = true;
					openCount++;
				}

				if (c == '}')
					openCount--;

				if (canClose && openCount == 0)
				{
					scriptContent = scriptContent.Remove(existingIndex, i - existingIndex + 3);
					break;
				}
			}
		}

		// file writing
		string newMethod = METHOD_FORMAT.Replace(METHOD_NAME_MARK, methodName).Replace(METHOD_BODY_MARK, editorCode);
		File.WriteAllText(Path.GetFullPath(REMOTE_PATH), scriptContent.Insert(insertionIndex, newMethod));

		AssetDatabase.Refresh();
	}

	void Deselect() => GUI.FocusControl(null);

	[Serializable]
	public class PresetList
	{
		public List<CodePreset> presets;

		public PresetList(List<CodePreset> presets) { this.presets = presets; }
	}

	[Serializable]
	public class CodePreset
	{
		public string name;
		public string code;

		public CodePreset(string name)
		{
			this.name = name;
			code = "";
		}
	}

	[Serializable]
	public class ICEMethod
	{
		public readonly string methodName;
		public readonly bool isDefault;
		public readonly ParameterInfo[] argsInfos;

		public object[] args;

		private Action<object[]> execute;
		private int lineIndex;

		public ICEMethod(MethodInfo info, int lineIndex)
		{
			if (!info.IsStatic)
			{
				Debug.LogError(
					"Target method is not static. Make the target method static to make it invokable by this tool."
				);
				return;
			}

			methodName = info.Name;
			isDefault = lineIndex != -1;
			argsInfos = info.GetParameters();

			args = new object[argsInfos.Length];

			for (int i = 0; i < args.Length; i++)
			{
				args[i] = GetDefaultValue(
					argsInfos[i].ParameterType,
					argsInfos[i].HasDefaultValue,
					argsInfos[i].DefaultValue
				);
			}

			execute = args => info.Invoke(null, args);
			this.lineIndex = lineIndex + 1;
		}

		object GetDefaultValue(Type type, bool hasDefault, object defaultValue)
		{
			return type switch
			{
				Type t when t == typeof(bool) => hasDefault ? defaultValue : default(bool),
				Type t when t == typeof(char) => hasDefault ? defaultValue : default(char),
				Type t when t == typeof(int) => hasDefault ? defaultValue : default(int),
				Type t when t == typeof(float) => hasDefault ? defaultValue : default(float),
				Type t when t == typeof(string) => hasDefault ? defaultValue : default(string),
				Type t when t == typeof(Color) => hasDefault ? defaultValue : default(Color),
				Type t when t == typeof(AnimationCurve) =>
					hasDefault ? defaultValue : default(AnimationCurve),
				Type t when t == typeof(Vector2) =>
					hasDefault ? defaultValue : default(Vector2),
				Type t when t == typeof(Vector3) =>
					hasDefault ? defaultValue : default(Vector3),
				_ => null
			};
		}

		public void Open()
		{
			AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(detectedRemotePath), lineIndex);
		}

		public void Execute() => execute?.Invoke(args);
	}
}

public class ICE_API : EditorWindow
{
	readonly string[] METHODS = new string[]{
		"T Find<T>(string name = null, Func<T, bool> customCheck = null) where T : Object",
		"GameObject Find(string name, Func<GameObject, bool> customCheck = null)",
		"T[ ] FindAll<T>(string name = null, Func<T, bool> customCheck = null) where T : Object",
		"GameObject[ ] FindAll(string name, Func<GameObject, bool> customCheck = null)",
		"T[ ] SpawnMultiple<T>(T model, int count, Vector3[ ] positions = null, Quaternion[ ] rotations = null, Transform[ ] parents = null) where T : Component",
		"T[ ] SpawnMultipleWithPrefabLink<T>(T model, int count, Vector3[ ] positions = null, Quaternion[ ] rotations = null, Transform[ ] parents = null) where T : Component",
		"void PlaceOnLine<T>(T[ ] objects, Vector3 startPos, Vector3 direction, float distance, bool alignRotation = false) where T : MonoBehaviour",
		"void PlaceOnCircle<T>(T[ ] objects, Vector3 startPos, Vector3 circleNormal, float radius, bool alignRotation = false) where T : MonoBehaviour",
		"void SnapToGrid(Transform[] objects, float size, Vector3 startPos = Vector3.zero)",
		"Object GetAsset(string name, Type type)"
	};
	readonly string[] DESCRIPTIONS = new string[]
	{
		"Finds the first object of provided type. Can be overridden with name filter and custom instance check.",
		"Finds a GameObject with the provided name. Can be overridden with custom instance check.",
		"Finds all objects of provided type. Can be overridden with name filter and custom instance check.",
		"Finds all GameObjects with a name containing the name filter. Can be overridden with custom instance check.",
		"Spawns multiple objects at provided positions, rotations and parent them to provided Transforms. If only one position and/or rotation and/or parent is provided, it will be applied to all spawned objects.",
		"Spawns multiple prefabs at provided positions, rotations and parent them to provided Transforms while keeping prefab link. If only one position and/or rotation and/or parent is provided, it will be applied to all spawned objects.",
		"Places objects on a line that starts at \"startPos\" and grows in \"direction\" at \"distance\" with the option to align the object's rotation.",
		"Places objects on a circle that starts at \"startPos\" and with normal \"circleNormal\" with a radius of \"radius\" with the option to align the object's rotation.",
		"Snaps objects to a grid with cells that are the size of \"size\", with the option of offsetting the grid by \"startPos\".",
		"Finds an object with the provided name and type in the Asset folder"
	};

	GUIStyle boldCenterStyle;
	GUIStyle boldWrappedStyle;
	GUIStyle wrappedStyle;

	bool[] toggled;
	Vector2 scroll;

	public static void ShowWindow()
	{
		ICE_API window = GetWindow<ICE_API>();
		window.titleContent = new GUIContent("ICE API");
		window.minSize = new Vector2(400, 300);
		window.Show();
	}

	void GenerateIfNeeded()
	{
		if (boldCenterStyle == null)
		{
			boldCenterStyle = new GUIStyle(GUI.skin.label)
			{
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold
			};
		}

		if (boldWrappedStyle == null)
		{
			boldWrappedStyle = new GUIStyle(EditorStyles.foldout)
			{
				fontStyle = FontStyle.Bold,
				wordWrap = true
			};
		}

		if (wrappedStyle == null)
			wrappedStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };

		if (toggled == null)
			toggled = new bool[METHODS.Length];
	}

	void OnGUI()
	{
		GenerateIfNeeded();

		EditorGUILayout.LabelField("ICE API", boldCenterStyle);
		EditorGUILayout.Space();

		scroll = EditorGUILayout.BeginScrollView(scroll);
		{
			for (int i = 0; i < METHODS.Length; i++)
				DisplayMethod(i);
		}
		EditorGUILayout.EndScrollView();
	}

	void DisplayMethod(int index)
	{
		toggled[index] = EditorGUILayout.BeginFoldoutHeaderGroup(toggled[index], METHODS[index], boldWrappedStyle);
		{
			EditorGUILayout.Space();

			if (toggled[index])
				EditorGUILayout.LabelField(DESCRIPTIONS[index], wrappedStyle);
		}
		EditorGUILayout.EndFoldoutHeaderGroup();

		EditorGUILayout.Space();
		EditorGUILayout.Space();
	}
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class ICEAttribute : Attribute
{
	public ICEAttribute() { }
}