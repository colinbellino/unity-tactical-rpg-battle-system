using UnityEngine;
using UnityEngine.InputNew;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Reflection;

[CustomEditor(typeof(ActionMap))]
public class ActionMapEditor : Editor
{
	static class Styles
	{
		public static GUIContent iconToolbarPlus =	EditorGUIUtility.IconContent("Toolbar Plus", "Add to list");
		public static GUIContent iconToolbarMinus =	EditorGUIUtility.IconContent("Toolbar Minus", "Remove from list");
		public static GUIContent iconToolbarPlusMore =	EditorGUIUtility.IconContent("Toolbar Plus More", "Choose to add to list");
		public static Dictionary<InputControlType, string[]> controlTypeSubLabels;

		static Styles()
		{
			controlTypeSubLabels = new Dictionary<InputControlType, string[]>();
			controlTypeSubLabels[InputControlType.Vector2] = new string[] { "X", "Y" };
			controlTypeSubLabels[InputControlType.Vector3] = new string[] { "X", "Y", "Z" };
			controlTypeSubLabels[InputControlType.Vector4] = new string[] { "X", "Y", "Z", "W" };
			controlTypeSubLabels[InputControlType.Quaternion] = new string[] { "X", "Y", "Z", "W" };

		}
	}
	
	ActionMap m_ActionMapEditCopy;
	
	int m_SelectedScheme = 0;
	int m_SelectedDeviceIndex = 0;
	[System.NonSerialized]
	InputAction m_SelectedAction = null;
	List<string> m_PropertyNames = new List<string>();
	HashSet<string> m_PropertyBlacklist  = new HashSet<string>();
	Dictionary<string, string> m_PropertyErrors = new Dictionary<string, string>();
	InputControlDescriptor m_SelectedSource = null;
	ButtonAxisSource m_SelectedButtonAxisSource = null;
	bool m_Modified = false;
	
	int selectedScheme
	{
		get { return m_SelectedScheme; }
		set
		{
			if (m_SelectedScheme == value)
				return;
			m_SelectedScheme = value;
		}
	}
	
	InputAction selectedAction
	{
		get { return m_SelectedAction; }
		set
		{
			if (m_SelectedAction == value)
				return;
			m_SelectedAction = value;
		}
	}
	
	void OnEnable()
	{
		Revert();
		RefreshPropertyNames();
		CalculateBlackList();
	}
	
	public virtual void OnDisable ()
	{
		// When destroying the editor check if we have any unapplied modifications and ask about applying them.
		if (m_Modified)
		{
			string dialogText = "Unapplied changes to ActionMap '" + serializedObject.targetObject.name + "'.";
			if (EditorUtility.DisplayDialog ("Unapplied changes", dialogText, "Apply", "Revert"))
				Apply();
		}
	}
	
	void Apply()
	{
		EditorGUIUtility.keyboardControl = 0;

		m_ActionMapEditCopy.name = target.name;
		SerializedObject temp = new SerializedObject(m_ActionMapEditCopy);
		temp.Update();
		SerializedProperty prop = temp.GetIterator();
		while (prop.Next(true))
			serializedObject.CopyFromSerializedProperty(prop);

		// Make sure references in control schemes to action map itself are stored correctly.
		for (int i = 0; i < m_ActionMapEditCopy.controlSchemes.Count; i++)
			serializedObject.FindProperty("m_ControlSchemes")
				.GetArrayElementAtIndex(i)
				.FindPropertyRelative("m_ActionMap").objectReferenceValue = target;
		
		serializedObject.ApplyModifiedProperties();

		var existingAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(target));

		// Add action sub-assets.
		ActionMap actionMap = (ActionMap)target;
		for (int i = 0; i < m_ActionMapEditCopy.actions.Count; i++)
		{
			InputAction action = m_ActionMapEditCopy.actions[i];
			action.actionMap = actionMap;
			action.actionIndex = i;
			if (existingAssets.Contains(action))
				continue;
			AssetDatabase.AddObjectToAsset(action, target);
		}

		m_Modified = false;
		// Reimporting is needed in order for the sub-assets to show up.
		AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(target));
		
		UpdateActionMapScript();
	}
	
	void Revert ()
	{
		EditorGUIUtility.keyboardControl = 0;
		
		ActionMap original = (ActionMap)serializedObject.targetObject;
		m_ActionMapEditCopy = Instantiate<ActionMap>(original);
		m_ActionMapEditCopy.name = original.name;
		
		m_Modified = false;
	}
	
	void RefreshPropertyNames()
	{
		// Calculate property names.
		m_PropertyNames.Clear();
		for (int i = 0; i < m_ActionMapEditCopy.actions.Count; i++)
			m_PropertyNames.Add(GetCamelCaseString(m_ActionMapEditCopy.actions[i].name, false));
		
		// Calculate duplicates.
		HashSet<string> duplicates = new HashSet<string>(m_PropertyNames.GroupBy(x => x).Where(group => group.Count() > 1).Select(group => group.Key));
		
		// Calculate errors.
		m_PropertyErrors.Clear();
		for (int i = 0; i < m_PropertyNames.Count; i++)
		{
			string name = m_PropertyNames[i];
			if (m_PropertyBlacklist.Contains(name))
				m_PropertyErrors[name] = "Invalid action name: "+name+".";
			else if (duplicates.Contains(name))
				m_PropertyErrors[name] = "Duplicate action name: "+name+".";
		}
	}
	
	void CalculateBlackList()
	{
		m_PropertyBlacklist = new HashSet<string>(typeof(ActionMapInput).GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Select(e => e.Name));
	}
	
	public override void OnInspectorGUI()
	{
		EditorGUI.BeginChangeCheck();

		DrawControlSchemeSelection();

		if (m_ActionMapEditCopy.controlSchemes.Count > 0)
		{
			EditorGUILayout.Space();
			DrawControlSchemeGUI();

			EditorGUILayout.Space();
			DrawActionList();
			
			if (selectedAction != null)
			{
				EditorGUILayout.Space();
				DrawActionGUI();
			}
			
			EditorGUILayout.Space();
		}

		if (EditorGUI.EndChangeCheck())
		{
			EditorUtility.SetDirty(m_ActionMapEditCopy);
			m_Modified = true;
		}

		ApplyRevertGUI();
	}

	void DrawControlSchemeSelection()
	{
		if (selectedScheme >= m_ActionMapEditCopy.controlSchemes.Count)
			selectedScheme = m_ActionMapEditCopy.controlSchemes.Count - 1;
		
		// Show schemes
		EditorGUILayout.LabelField("Control Schemes");

		EditorGUIUtility.GetControlID(FocusType.Passive);
		EditorGUILayout.BeginVertical("Box");
		for (int i = 0; i < m_ActionMapEditCopy.controlSchemes.Count; i++)
		{
			Rect rect = EditorGUILayout.GetControlRect();
			
			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
			{
				EditorGUIUtility.keyboardControl = 0;
				selectedScheme = i;
				Event.current.Use();
			}
			
			if (selectedScheme == i)
				GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
			
			EditorGUI.LabelField(rect, m_ActionMapEditCopy.controlSchemes[i].name);
		}
		EditorGUILayout.EndVertical();
		
		// Control scheme remove and add buttons
		EditorGUILayout.BeginHorizontal();
		{
			GUILayout.Space(15 * EditorGUI.indentLevel);

			if (GUILayout.Button(Styles.iconToolbarMinus, GUIStyle.none))
				RemoveControlScheme();
			
			if (GUILayout.Button(Styles.iconToolbarPlus, GUIStyle.none))
				AddControlScheme();

			GUILayout.FlexibleSpace();
		}
		EditorGUILayout.EndHorizontal();
	}

	void AddControlScheme()
	{
		var controlScheme = new ControlScheme("New Control Scheme", m_ActionMapEditCopy);

		for (int i = 0; i < m_ActionMapEditCopy.actions.Count; i++)
			controlScheme.bindings.Add(new ControlBinding());

		m_ActionMapEditCopy.controlSchemes.Add(controlScheme);

		selectedScheme = m_ActionMapEditCopy.controlSchemes.Count - 1;
	}

	void RemoveControlScheme()
	{
		m_ActionMapEditCopy.controlSchemes.RemoveAt(selectedScheme);
		if (selectedScheme >= m_ActionMapEditCopy.controlSchemes.Count)
			selectedScheme = m_ActionMapEditCopy.controlSchemes.Count - 1;
	}

	void DrawControlSchemeGUI()
	{
		ControlScheme scheme = m_ActionMapEditCopy.controlSchemes[selectedScheme];

		EditorGUI.BeginChangeCheck();
		string schemeName = EditorGUILayout.TextField("Control Scheme Name", scheme.name);
		if (EditorGUI.EndChangeCheck())
			scheme.name = schemeName;

		string[] deviceNames = InputDeviceUtility.GetDeviceNames();

		for (int i = 0; i < scheme.serializableDeviceTypes.Count; i++)
		{
			Rect rect = EditorGUILayout.GetControlRect();

			if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
			{
				m_SelectedDeviceIndex = i;
				Repaint();
			}
			if (m_SelectedDeviceIndex == i)
				GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);

			EditorGUI.BeginChangeCheck();
			int deviceIndex = EditorGUI.Popup(
				rect,
				"Device Type",
				InputDeviceUtility.GetDeviceIndex(scheme.serializableDeviceTypes[i]),
				deviceNames);
			if (EditorGUI.EndChangeCheck())
				scheme.serializableDeviceTypes[i].value = InputDeviceUtility.GetDeviceType(deviceIndex);
		}

		// Device remove and add buttons
		EditorGUILayout.BeginHorizontal();
		{
			GUILayout.Space(15 * EditorGUI.indentLevel);

			if (GUILayout.Button(Styles.iconToolbarMinus, GUIStyle.none))
				RemoveDevice();
			
			if (GUILayout.Button(Styles.iconToolbarPlus, GUIStyle.none))
				AddDevice();
			
			GUILayout.FlexibleSpace();
		}
		EditorGUILayout.EndHorizontal();

		// Pad this area with spacing so all control schemes use same heights,
		// and the actions table below doesn't move when switching control scheme.
		int maxDevices = 0;
		for (int i = 0; i < m_ActionMapEditCopy.controlSchemes.Count; i++)
			maxDevices = Mathf.Max(maxDevices, m_ActionMapEditCopy.controlSchemes[i].serializableDeviceTypes.Count);
		int extraLines = maxDevices - scheme.serializableDeviceTypes.Count;
		EditorGUILayout.GetControlRect(true, extraLines * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing));
	}

	void AddDevice()
	{
		ControlScheme scheme = m_ActionMapEditCopy.controlSchemes[selectedScheme];
		scheme.serializableDeviceTypes.Add(new SerializableType(null));
	}

	void RemoveDevice()
	{
		ControlScheme scheme = m_ActionMapEditCopy.controlSchemes[selectedScheme];
		if (m_SelectedDeviceIndex >= 0 && m_SelectedDeviceIndex < scheme.serializableDeviceTypes.Count)
		{
			scheme.serializableDeviceTypes.RemoveAt(m_SelectedDeviceIndex);
		}
	}

	void DrawActionList()
	{
		// Show actions
		EditorGUILayout.LabelField("Actions", m_ActionMapEditCopy.controlSchemes[selectedScheme].name + " Bindings");
		EditorGUILayout.BeginVertical("Box");
		{
			foreach (var action in m_ActionMapEditCopy.actions)
			{
				DrawActionRow(action, selectedScheme);
			}
			if (m_ActionMapEditCopy.actions.Count == 0)
				EditorGUILayout.GetControlRect();
		}
		EditorGUILayout.EndVertical();
		
		// Action remove and add buttons
		EditorGUILayout.BeginHorizontal();
		{
			GUILayout.Space(15 * EditorGUI.indentLevel);

			if (GUILayout.Button(Styles.iconToolbarMinus, GUIStyle.none))
				RemoveAction();
			
			if (GUILayout.Button(Styles.iconToolbarPlus, GUIStyle.none))
				AddAction();
			
			GUILayout.FlexibleSpace();
		}
		EditorGUILayout.EndHorizontal();
	}

	void AddAction()
	{
		var action = ScriptableObject.CreateInstance<InputAction>();
		action.name = "New Control";
		m_ActionMapEditCopy.actions.Add(action);
		for (int i = 0; i < m_ActionMapEditCopy.controlSchemes.Count; i++)
			m_ActionMapEditCopy.controlSchemes[i].bindings.Add(new ControlBinding());
		
		selectedAction = m_ActionMapEditCopy.actions[m_ActionMapEditCopy.actions.Count - 1];
		
		RefreshPropertyNames();
	}

	void RemoveAction()
	{
		int actionIndex = m_ActionMapEditCopy.actions.IndexOf(selectedAction);
		m_ActionMapEditCopy.actions.RemoveAt(actionIndex);
		for (int i = 0; i < m_ActionMapEditCopy.controlSchemes.Count; i++)
			m_ActionMapEditCopy.controlSchemes[i].bindings.RemoveAt(actionIndex);
		ScriptableObject.DestroyImmediate(selectedAction, true);
		
		if (m_ActionMapEditCopy.actions.Count == 0)
			selectedAction = null;
		else
			selectedAction = m_ActionMapEditCopy.actions[Mathf.Min(actionIndex, m_ActionMapEditCopy.actions.Count - 1)];
		
		RefreshPropertyNames();
	}
	
	void ApplyRevertGUI()
	{
		bool valid = true;
		if (m_PropertyErrors.Count > 0)
		{
			valid = false;
			EditorGUILayout.HelpBox(string.Join("\n", m_PropertyErrors.Values.ToArray()), MessageType.Error);
		}
		
		EditorGUI.BeginDisabledGroup(!m_Modified);
		
		GUILayout.BeginHorizontal();
		{
			GUILayout.FlexibleSpace();
			
			if (GUILayout.Button("Revert"))
				Revert();
			
			EditorGUI.BeginDisabledGroup(!valid);
			if (GUILayout.Button("Apply"))
				Apply();
			EditorGUI.EndDisabledGroup();
		}
		GUILayout.EndHorizontal();
		
		EditorGUI.EndDisabledGroup();
	}
	
	void DrawActionRow(InputAction action, int selectedScheme)
	{
		int actionIndex = m_ActionMapEditCopy.actions.IndexOf(action);
		
		int sourceCount = 0;
		for (int i = 0; i < m_ActionMapEditCopy.controlSchemes.Count; i++)
		{
			ControlBinding schemeBinding = m_ActionMapEditCopy.controlSchemes[i].bindings[actionIndex];
			int count = schemeBinding.sources.Count + schemeBinding.buttonAxisSources.Count;
			sourceCount = Mathf.Max(sourceCount, count);
		}
		int lines = Mathf.Max(1, sourceCount);
		
		float height = EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines - 1) + 8;
		Rect totalRect = GUILayoutUtility.GetRect(1, height);
		
		Rect baseRect = totalRect;
		baseRect.yMin += 4;
		baseRect.yMax -= 4;
		
		if (selectedAction == action)
			GUI.DrawTexture(totalRect, EditorGUIUtility.whiteTexture);
		
		// Show control fields
		
		Rect rect = baseRect;
		rect.height = EditorGUIUtility.singleLineHeight;
		rect.width = EditorGUIUtility.labelWidth - 4;
		
		EditorGUI.LabelField(rect, action.controlData.name);
		
		// Show binding fields

		ControlBinding binding = m_ActionMapEditCopy.controlSchemes[selectedScheme].bindings[actionIndex];
		if (binding != null)
		{
			rect = baseRect;
			rect.height = EditorGUIUtility.singleLineHeight;
			rect.xMin += EditorGUIUtility.labelWidth;
			
			if (binding.primaryIsButtonAxis)
			{
				DrawButtonAxisSources(ref rect, binding);
				DrawSources(ref rect, binding);
			}
			else
			{
				DrawSources(ref rect, binding);
				DrawButtonAxisSources(ref rect, binding);
			}
		}
		
		if (Event.current.type == EventType.MouseDown && totalRect.Contains(Event.current.mousePosition))
		{
			EditorGUIUtility.keyboardControl = 0;
			selectedAction = action;
			Event.current.Use();
		}
	}
	
	void DrawSources(ref Rect rect, ControlBinding binding)
	{
		for (int i = 0; i < binding.sources.Count; i++)
		{
			DrawSourceSummary(rect, binding.sources[i]);
			rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
	}
	
	void DrawButtonAxisSources(ref Rect rect, ControlBinding binding)
	{
		for (int i = 0; i < binding.buttonAxisSources.Count; i++)
		{
			DrawButtonAxisSourceSummary(rect, binding.buttonAxisSources[i]);
			rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		}
	}
	
	void DrawSourceSummary(Rect rect, InputControlDescriptor source)
	{
		EditorGUI.LabelField(rect, GetSourceString(source));
	}
	
	void DrawButtonAxisSourceSummary(Rect rect, ButtonAxisSource source)
	{
		if ((System.Type)(source.negative.deviceType) == (System.Type)(source.positive.deviceType))
			EditorGUI.LabelField(rect,
				string.Format("{0} {1} & {2}",
					InputDeviceUtility.GetDeviceName(source.negative),
					InputDeviceUtility.GetDeviceControlName(source.negative),
					InputDeviceUtility.GetDeviceControlName(source.positive)
				)
			);
		else
			EditorGUI.LabelField(rect, string.Format("{0} & {1}", GetSourceString(source.negative), GetSourceString(source.positive)));
	}
	
	string GetSourceString(InputControlDescriptor source)
	{
		return string.Format("{0} {1}", InputDeviceUtility.GetDeviceName(source), InputDeviceUtility.GetDeviceControlName(source));
	}
	
	void UpdateActionMapScript () {
		ActionMap original = (ActionMap)serializedObject.targetObject;
		string className = GetCamelCaseString(original.name, true);
		StringBuilder str = new StringBuilder();
		
		str.AppendFormat(@"using UnityEngine;
using UnityEngine.InputNew;

// GENERATED FILE - DO NOT EDIT MANUALLY
public class {0} : ActionMapInput {{
	public {0} (ActionMap actionMap) : base (actionMap) {{ }}
	
", className);
		
		for (int i = 0; i < m_ActionMapEditCopy.actions.Count; i++)
		{
			InputControlType controlType = m_ActionMapEditCopy.actions[i].controlData.controlType;
			string typeStr = string.Empty;
			switch(controlType)
			{
			case InputControlType.Button:
				typeStr = "ButtonInputControl";
				break;
			case InputControlType.AbsoluteAxis:
			case InputControlType.RelativeAxis:
				typeStr = "AxisInputControl";
				break;
			case InputControlType.Vector2:
				typeStr = "Vector2InputControl";
				break;
			case InputControlType.Vector3:
				typeStr = "Vector3InputControl";
				break;
			}

			str.AppendFormat("	public {2} @{0} {{ get {{ return ({2})this[{1}]; }} }}\n", GetCamelCaseString(m_ActionMapEditCopy.actions[i].name, false), i, typeStr);
		}
		
		str.AppendLine(@"}");
		
		string path = AssetDatabase.GetAssetPath(original);
		path = path.Substring(0, path.Length - Path.GetExtension(path).Length) + ".cs";
		File.WriteAllText(path, str.ToString());
		AssetDatabase.ImportAsset(path);

		original.SetMapTypeName(className+", "+"Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
	}
	
	string GetCamelCaseString(string input, bool capitalFirstLetter)
	{
		string output = string.Empty;
		bool capitalize = capitalFirstLetter;
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			if (c == ' ')
			{
				capitalize = true;
				continue;
			}
			if (char.IsLetter(c))
			{
				if (capitalize)
					output += char.ToUpper(c);
				else if (output.Length == 0)
					output += char.ToLower(c);
				else
					output += c;
				capitalize = false;
				continue;
			}
			if (char.IsDigit(c))
			{
				if (output.Length > 0)
				{
					output += c;
					capitalize = false;
				}
				continue;
			}
			if (c == '_')
			{
				output += c;
				capitalize = true;
				continue;
			}
		}
		return output;
	}
	
	void DrawActionGUI()
	{
		EditorGUI.BeginChangeCheck();
		string name = EditorGUILayout.TextField("Name", selectedAction.controlData.name);
		if (EditorGUI.EndChangeCheck())
		{
			InputControlData data = selectedAction.controlData;
			data.name = name;
			selectedAction.controlData = data;
			selectedAction.name = name;
			RefreshPropertyNames();
		}
		
		EditorGUI.BeginChangeCheck();
		var type = (InputControlType)EditorGUILayout.EnumPopup("Type", selectedAction.controlData.controlType);
		if (EditorGUI.EndChangeCheck())
		{
			InputControlData data = selectedAction.controlData;
			data.controlType = type;
			selectedAction.controlData = data;
		}
		
		EditorGUILayout.Space();

		if (Styles.controlTypeSubLabels.ContainsKey(selectedAction.controlData.controlType))
		{
			DrawCompositeControl(selectedAction);
		}
		else
		{
			if (selectedScheme >= 0 && selectedScheme < m_ActionMapEditCopy.controlSchemes.Count)
			{
				int actionIndex = m_ActionMapEditCopy.actions.IndexOf(selectedAction);
				DrawBinding(m_ActionMapEditCopy.controlSchemes[selectedScheme].bindings[actionIndex]);
			}
		}
	}

	void DrawCompositeControl(InputAction action)
	{
		string[] subLabels = Styles.controlTypeSubLabels[action.controlData.controlType];
		if (action.controlData.componentControlIndices == null ||
			action.controlData.componentControlIndices.Length != subLabels.Length)
		{
			var data = action.controlData;
			data.componentControlIndices = new int[subLabels.Length];
			action.controlData = data;
		}
		for (int i = 0; i < subLabels.Length; i++)
		{
			DrawCompositeSource(string.Format("Source ({0})", subLabels[i]), action, i);
		}
	}

	void DrawCompositeSource(string label, InputAction action, int index)
	{
		EditorGUI.BeginChangeCheck();
		string[] actionStrings = m_ActionMapEditCopy.actions.Select(e => e.name).ToArray();
		int controlIndex = EditorGUILayout.Popup(label, action.controlData.componentControlIndices[index], actionStrings);
		if (EditorGUI.EndChangeCheck())
		{
			action.controlData.componentControlIndices[index] = controlIndex;
		}
	}
	
	void DrawBinding(ControlBinding binding)
	{
		if (binding.primaryIsButtonAxis)
		{
			DrawButtonAxisSources(binding);
			DrawSources(binding);
		}
		else
		{
			DrawSources(binding);
			DrawButtonAxisSources(binding);
		}
		
		// Remove and add buttons
		EditorGUILayout.BeginHorizontal();
		GUILayout.Space(15 * EditorGUI.indentLevel);
		if (GUILayout.Button(Styles.iconToolbarMinus, GUIStyle.none))
		{
			if (m_SelectedSource != null)
				binding.sources.Remove(m_SelectedSource);
			if (m_SelectedButtonAxisSource != null)
				binding.buttonAxisSources.Remove(m_SelectedButtonAxisSource);
		}
		Rect r = GUILayoutUtility.GetRect(Styles.iconToolbarPlusMore, GUIStyle.none);
		if (GUI.Button(r, Styles.iconToolbarPlusMore, GUIStyle.none))
		{
			ShowAddOptions(r, binding);
		}
		GUILayout.FlexibleSpace();
		EditorGUILayout.EndHorizontal();
	}
	
	void ShowAddOptions(Rect rect, ControlBinding binding)
	{
		GenericMenu menu = new GenericMenu();
		menu.AddItem(new GUIContent("Regular Source"), false, AddSource, binding);
		menu.AddItem(new GUIContent("Button Axis Source"), false, AddButtonAxisSource, binding);
		menu.DropDown(rect);
	}
	
	void AddSource(object data)
	{
		ControlBinding binding = (ControlBinding)data;
		var source = new InputControlDescriptor();
		binding.sources.Add(source);
		
		m_SelectedButtonAxisSource = null;
		m_SelectedSource = source;
	}
	
	void AddButtonAxisSource(object data)
	{
		ControlBinding binding = (ControlBinding)data;
		var source = new ButtonAxisSource(new InputControlDescriptor(), new InputControlDescriptor());
		binding.buttonAxisSources.Add(source);
		
		m_SelectedSource = null;
		m_SelectedButtonAxisSource = source;
	}
	
	void DrawSources(ControlBinding binding)
	{
		for (int i = 0; i < binding.sources.Count; i++)
		{
			DrawSourceSummary(binding.sources[i]);
		}
	}
	
	void DrawButtonAxisSources(ControlBinding binding)
	{
		for (int i = 0; i < binding.buttonAxisSources.Count; i++)
		{
			DrawButtonAxisSourceSummary(binding.buttonAxisSources[i]);
		}
	}
	
	void DrawSourceSummary(InputControlDescriptor source)
	{
		Rect rect = EditorGUILayout.GetControlRect();
		
		if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
		{
			m_SelectedButtonAxisSource = null;
			m_SelectedSource = source;
			Repaint();
		}
		if (m_SelectedSource == source)
			GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
		
		DrawSourceSummary(rect, "Source", source);
		
		EditorGUILayout.Space();
	}
	
	void DrawButtonAxisSourceSummary(ButtonAxisSource source)
	{
		Rect rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing);
		
		if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
		{
			m_SelectedSource = null;
			m_SelectedButtonAxisSource = source;
			Repaint();
		}
		if (m_SelectedButtonAxisSource == source)
			GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
		
		rect.height = EditorGUIUtility.singleLineHeight;
		DrawSourceSummary(rect, "Source (negative)", source.negative);
		rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
		DrawSourceSummary(rect, "Source (positive)", source.positive);
		
		EditorGUILayout.Space();
	}
	
	void DrawSourceSummary(Rect rect, string label, InputControlDescriptor source)
	{
		if (m_SelectedSource == source)
			GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
		
		rect = EditorGUI.PrefixLabel(rect, new GUIContent(label));
		rect.width = (rect.width - 4) * 0.5f;
		
		int indentLevel = EditorGUI.indentLevel;
		EditorGUI.indentLevel = 0;

		List<System.Type> types = m_ActionMapEditCopy.controlSchemes[selectedScheme].deviceTypes.ToList();
		string[] deviceNames = types.Select(e => e == null ? string.Empty : e.Name).ToArray();
		EditorGUI.BeginChangeCheck();
		int deviceIndex = EditorGUI.Popup(rect, types.IndexOf(source.deviceType), deviceNames);
		if (EditorGUI.EndChangeCheck())
			source.deviceType = types[deviceIndex];
		
		rect.x += rect.width + 4;
		
		string[] controlNames = InputDeviceUtility.GetDeviceControlNames(source.deviceType);
		EditorGUI.BeginChangeCheck();
		int controlIndex = EditorGUI.Popup(rect, source.controlIndex, controlNames);
		if (EditorGUI.EndChangeCheck())
			source.controlIndex = controlIndex;
		
		EditorGUI.indentLevel = indentLevel;
	}
}
