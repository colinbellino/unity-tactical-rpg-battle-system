using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Tactical.Core;
using Tactical.Actor.Component;

namespace Tactical.Battle.Controller {

	public class TurnOrderController : MonoBehaviour {

		const int turnActivation = 100;
		const int turnCost = 50;
		const int moveCost = 30;
		const int actionCost = 20;

		public const string RoundBeganNotification = "TurnOrderController.roundBegan";
		public const string TurnCheckNotification = "TurnOrderController.turnCheck";
		public const string TurnCompletedNotification = "TurnOrderController.turnCompleted";
		public const string RoundEndedNotification = "TurnOrderController.roundEnded";

		public IEnumerator Round () {
			BattleController bc = GetComponent<BattleController>();
			while (true) {
				this.PostNotification(RoundBeganNotification);

				// Increase the CTR of each units with its SPD
				var units = new List<Unit>( bc.units );
				for (int i = 0; i < units.Count; ++i) {
					Stats s = units[i].GetComponent<Stats>();
					s[StatType.CTR] += s[StatType.SPD];
				}

				// Sort the units by CTR
				units.Sort( (a, b) => GetCounter(a).CompareTo(GetCounter(b)) );

				// Loop on units
				for (int i = units.Count - 1; i >= 0; --i) {
					if (CanTakeTurn(units[i])) {
						Stats s = units[i].GetComponent<Stats>();
						Debug.Log(string.Format("{0}: {1}", units[i].name, s[StatType.CTR]));
						bc.turn.Change(units[i]);
						yield return units[i];

						// Calculate the cost of this turn.
						int cost = turnCost;
						if (bc.turn.hasUnitMoved) {
							cost += moveCost;
						}
						if (bc.turn.hasUnitActed) {
							cost += actionCost;
						}

						// And substract it to the CTR.

						s.SetValue(StatType.CTR, s[StatType.CTR] - cost, false);

						units[i].PostNotification(TurnCompletedNotification);
					}
				}

				this.PostNotification(RoundEndedNotification);
			}
		}

		private bool CanTakeTurn (Unit target) {
			var exc = new BaseException( GetCounter(target) >= turnActivation );
			target.PostNotification(TurnCheckNotification, exc);
			return exc.toggle;
		}

		private int GetCounter (Unit target) {
			return target.GetComponent<Stats>()[StatType.CTR];
		}
	}

}
