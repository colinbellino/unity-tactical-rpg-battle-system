using UnityEngine;
using System;
using Tactical.Grid.Model;
using Tactical.Core.Enums;
using Tactical.Core.EventArgs;

namespace Tactical.Core.Controller {

	public class BattleInputController : MonoBehaviour {

		public static event EventHandler<InfoEventArgs<Point>> MoveEvent;
		public static event EventHandler<InfoEventArgs<BattleInputs>> ActionEvent;
		public GameControls controls;

		private Repeater horizontal;
		private Repeater vertical;

		private void Start () {
			controls = new GameControls();
			controls.Battle.Enable();

			// Create the repeaters for the axis.
			horizontal = new Repeater();
			vertical = new Repeater();
		}

		private void Update () {
			HandleMove();
			HandleAction();
		}

		private void HandleMove () {
			var move = controls.Battle.Move.ReadValue<Vector2>();
			int x = horizontal.Update(move.x);
			int y = vertical.Update(move.y);

			// Handle movement inputs.
			if (x != 0 || y != 0) {
				MoveEvent?.Invoke(this, new InfoEventArgs<Point>(new Point(x, y)));
	  	}
		}

		private void HandleAction () {
			// Handle action inputs.
			if (controls.Battle.Confirm.WasReleasedThisFrame()) {
				ActionEvent?.Invoke(this, new InfoEventArgs<BattleInputs>(BattleInputs.Confirm));
			}
			if (controls.Battle.Cancel.WasReleasedThisFrame()) {
				ActionEvent?.Invoke(this, new InfoEventArgs<BattleInputs>(BattleInputs.Cancel));
			}
			if (controls.Battle.RotateCamera.WasPerformedThisFrame()) {
				var value = controls.Battle.RotateCamera.ReadValue<float>();
				if (value < 0) {
					ActionEvent?.Invoke(this, new InfoEventArgs<BattleInputs>(BattleInputs.RotateCameraLeft));
				} else {
					ActionEvent?.Invoke(this, new InfoEventArgs<BattleInputs>(BattleInputs.RotateCameraRight));
				}
			}
		}

	}

	class Repeater {

		private const float threshold = 0.3f;
		private const float rate = 0.15f;
		private float next;
		private bool hold;

		public int Update (float rawValue) {
			int retValue = 0;
			int value = Mathf.RoundToInt(rawValue);

			if (value != 0) {
				if (Time.time > next) {
					retValue = value;
					next = Time.time + (hold ? rate : threshold);
					hold = true;
				}
			} else {
				hold = false;
				next = 0;
			}

			return retValue;
		}
	}

}
