using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Tactical.Core;
using Tactical.Core.Model;
using Tactical.Core.Component;
using Tactical.Actor.Component;

namespace Tactical.Core.Editor {

	public static class EnemyParser {

		[MenuItem("Pre Production/Parse Enemies")]
		public static void Parse() {
			CreateDirectories();
			ParseEnemies();
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		private static void CreateDirectories () {
			if (!AssetDatabase.IsValidFolder("Assets/Resources/Enemies")) {
				AssetDatabase.CreateFolder("Assets/Resources", "Enemies");
			}
		}

		private static void ParseEnemies () {
			string readPath = string.Format("{0}/Settings/Enemies.csv", Application.dataPath);
			string[] readText = File.ReadAllLines(readPath);
			for (int i = 1; i < readText.Length; ++i) {
				PartsEnemies(readText[i]);
			}
		}

		private static void PartsEnemies (string line) {
			string[] elements = line.Split(',');
			if (elements[0] == "") {
				Debug.LogError("Invalid line: " + line);
				return;
			}

			// TODO: Make this create a prefab later on.

			// EnemyData enemyData = ScriptableObject.CreateInstance<EnemyData>();
			// enemyData.Load(readText[i]);
			// string fileName = string.Format("{0}{1}.asset", filePath, enemyData.name);
			// AssetDatabase.CreateAsset(enemyData, fileName);
		}

		private static GameObject GetOrCreate (string jobName) {
			string fullPath = string.Format("Assets/Resources/Enemies/{0}.prefab", jobName);
			GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(fullPath);
			if (obj == null) {
				obj = Create(fullPath);
			}
			return obj;
		}

		private static GameObject Create (string fullPath) {
			GameObject instance = new GameObject ("temp");
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, fullPath);
			GameObject.DestroyImmediate(instance);
			return prefab;
		}

	}

}
