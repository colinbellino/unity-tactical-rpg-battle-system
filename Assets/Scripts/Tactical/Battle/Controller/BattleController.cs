using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;
using System.Collections.Generic;
using Tactical.Core.StateMachine;
using Tactical.Core.Controller;
using Tactical.Grid.Model;
using Tactical.Grid.Component;
using Tactical.Actor.Component;
using Tactical.Battle.Model;
using Tactical.Battle.Component;
using Tactical.Battle.BattleState;
using Tactical.UI.Controller;
using Tactical.AI.Controller;

namespace Tactical.Battle.Controller {

	public class BattleController : StateMachine {

		public CameraRig cameraRig;
		public Board board;
		public LevelData levelData;
		public Transform tileSelectionIndicator;
		public Point pos;
		public GameObject heroPrefab;
		public List<Unit> units = new List<Unit>();
		public Turn turn = new Turn();
		public IEnumerator round;
		public Tile currentTile {
			get { return board.GetTile(pos); }
		}
		[Header("- UI -")]
		public ActionMenuPanelController actionMenuPanelController;
		public TurnOrderPanelController turnOrderPanelController;
		public HitIndicatorPanelController hitIndicatorPanelController;
		public UnitDirectionController unitDirectionController;
		public StatPanelController statPanelController;
		public BattleMessageController battleMessageController;
		public ComputerPlayerController cpu;
		public TimeController timeController;
		[Header("- Audio -")]
		public AudioSource audioSource;
		public AudioClip tileSelectionClip;

		private void Start () {
			ChangeState<InitBattleState>();
		}

		private void OnValidate () {
			Assert.IsNotNull(actionMenuPanelController, "Missing component: ActionMenuPanelController");
			Assert.IsNotNull(turnOrderPanelController, "Missing component: TurnOrderPanelController");
			Assert.IsNotNull(hitIndicatorPanelController, "Missing component: HitIndicatorPanelController");
			Assert.IsNotNull(unitDirectionController, "Missing component: UnitDirectionController");
			Assert.IsNotNull(statPanelController, "Missing component: StatPanelController");
			Assert.IsNotNull(battleMessageController, "Missing component: BattleMessageController");
			Assert.IsNotNull(cpu, "Missing component: ComputerPlayerController");
			Assert.IsNotNull(timeController, "Missing component: TimeController");
			Assert.IsNotNull(audioSource, "Missing component: AudioSource");
		}
	}

}
