using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections;
using System.Collections.Generic;

namespace Tactical.Grid.DevTools {

	using Model;
	using Component;

	public class BoardCreator : MonoBehaviour {

		[SerializeField] private GameObject[] tileViewPrefabs;
		[SerializeField] private GameObject tileSelectionIndicatorPrefab;
		[SerializeField] private int width = 10;
		[SerializeField] private int depth = 10;
		[SerializeField] private int height = 8;
		[SerializeField] private Point pos;
		[SerializeField] private LevelData levelData;
		private Dictionary<Point, Tile> tiles = new Dictionary<Point, Tile>();

		public Transform marker {
			get {
				if (_marker == null) {
					GameObject instance = Instantiate(tileSelectionIndicatorPrefab) as GameObject;
					_marker = instance.transform;
					_marker.parent = transform;
				}
				return _marker;
			}
		}
		private Transform _marker;

		public void Grow () {
			GrowSingle(pos);
		}

		public void Shrink () {
			ShrinkSingle(pos);
		}

		public void GrowArea () {
			Rect r = RandomRect();
			GrowRect(r);
		}

		public void ShrinkArea () {
			Rect r = RandomRect();
			ShrinkRect(r);
		}

		public void UpdateMarker () {
			Tile t = tiles.ContainsKey(pos) ? tiles[pos] : null;
			marker.localPosition = t != null ? t.center + new Vector3(0f, 0.1f, 0f) : new Vector3(pos.x, 0, pos.y);
		}

		public void Clear () {
			for (int i = transform.childCount - 1; i >= 0; --i) {
				DestroyImmediate(transform.GetChild(i).gameObject);
			}
			tiles.Clear();
		}

		public void Save () {
			string filePath = Application.dataPath + "/Resources/Levels";
			if (!Directory.Exists(filePath)) {
				CreateSaveDirectory ();
			}

			LevelData board = ScriptableObject.CreateInstance<LevelData>();
			board.tiles = new List<Vector3>( tiles.Count );
			foreach (Tile t in tiles.Values) {
				board.tiles.Add( new Vector3(t.pos.x, t.height, t.pos.y) );
			}

			string fileName = string.Format("Assets/Resources/Levels/{1}.asset", filePath, name);
			AssetDatabase.CreateAsset(board, fileName);
		}

		public void Load () {
			Clear();

			if (levelData == null) { return; }

			foreach (Vector3 v in levelData.tiles) {
				Tile t = Create();
				t.Load(v);
				tiles.Add(t.pos, t);
			}
		}

		private Rect RandomRect () {
			int x = UnityEngine.Random.Range(0, width - 2);
			int y = UnityEngine.Random.Range(0, height - 2);
			int w = UnityEngine.Random.Range(1, width - x);
			int h = UnityEngine.Random.Range(1, depth - y);
			return new Rect(x, y, w, h);
		}

		private void GrowRect (Rect rect) {
			for (int y = (int)rect.yMin; y < (int)rect.yMax; ++y) {
				for (int x = (int)rect.xMin; x < (int)rect.xMax; ++x) {
					Point p = new Point(x, y);
					GrowSingle(p);
				}
			}
		}

		private void ShrinkRect (Rect rect) {
			for (int y = (int)rect.yMin; y < (int)rect.yMax; ++y) {
				for (int x = (int)rect.xMin; x < (int)rect.xMax; ++x) {
					Point p = new Point(x, y);
					ShrinkSingle(p);
				}
			}
		}

		private Tile Create () {
			var tileViewPrefab = tileViewPrefabs[Random.Range(0, tileViewPrefabs.Length)];
			GameObject instance = Instantiate(tileViewPrefab) as GameObject;
			instance.transform.parent = transform;
			return instance.GetComponent<Tile>();
		}

		private Tile GetOrCreate (Point p) {
			if (tiles.ContainsKey(p)) {
				return tiles[p];
			}

			Tile t = Create();
			t.Load(p, 0);
			tiles.Add(p, t);

			return t;
		}

		private void GrowSingle (Point p) {
			Tile t = GetOrCreate(p);
			if (t.height < height) {
				t.Grow();
			}
		}

		private void ShrinkSingle (Point p) {
			if (!tiles.ContainsKey(p)) {
				return;
			}

			Tile t = tiles[p];
			t.Shrink();

			if (t.height <= 0) {
				tiles.Remove(p);
				DestroyImmediate(t.gameObject);
			}
		}

		private void CreateSaveDirectory () {
			string filePath = Application.dataPath + "/Resources";
			if (!Directory.Exists(filePath)) {
				AssetDatabase.CreateFolder("Assets", "Resources");
			}
			filePath += "/Levels";
			if (!Directory.Exists(filePath)) {
				AssetDatabase.CreateFolder("Assets/Resources", "Levels");
			}
			AssetDatabase.Refresh();
		}
	}

	[CustomEditor(typeof(BoardCreator))]
	public class BoardCreatorInspector : Editor {

		public BoardCreator current {
			get {
				return (BoardCreator) target;
			}
		}

		public override void OnInspectorGUI () {
			DrawDefaultInspector();

			if (GUILayout.Button("Clear")) {
				current.Clear();
			}
			if (GUILayout.Button("Grow")) {
				current.Grow();
			}
			if (GUILayout.Button("Shrink")) {
				current.Shrink();
			}
			if (GUILayout.Button("Grow Area")) {
				current.GrowArea();
			}
			if (GUILayout.Button("Shrink Area")) {
				current.ShrinkArea();
			}
			if (GUILayout.Button("Save")) {
				current.Save();
			}
			if (GUILayout.Button("Load")) {
				current.Load();
			}

			if (GUI.changed) {
				current.UpdateMarker();
			}
		}

	}

}
