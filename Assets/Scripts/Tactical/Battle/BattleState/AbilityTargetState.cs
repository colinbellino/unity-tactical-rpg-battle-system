using System.Collections.Generic;
using Tactical.Core;
using Tactical.Core.Extensions;
using Tactical.Core.EventArgs;
using Tactical.Grid.Model;
using Tactical.Grid.Component;
using Tactical.Actor.Component;

namespace Tactical.Battle.BattleState {

	public class AbilityTargetState : BattleState {

		private List<Tile> tiles;
		private AbilityRange ar;

		public override void Enter () {
			base.Enter ();
			ar = turn.ability.GetComponent<AbilityRange>();
			SelectTiles ();
			statPanelController.ShowPrimary(turn.actor.gameObject);
			if (ar.directionOriented) {
				RefreshSecondaryStatPanel(pos);
			}
		}

		public override void Exit () {
			base.Exit ();
			board.DeSelectTiles(tiles);
			statPanelController.HidePrimary();
			statPanelController.HideSecondary();
		}

		protected override void OnMove (object sender, InfoEventArgs<Point> e) {
			if (ar.directionOriented) {
				ChangeDirection(e.info);
			} else {
				SelectTile(e.info + pos);
				RefreshSecondaryStatPanel(pos);
			}
		}

		protected override void OnFire (object sender, InfoEventArgs<int> e) {
			if (e.info == 0) {
				if (ar.directionOriented || tiles.Contains(board.GetTile(pos))) {
					owner.ChangeState<ConfirmAbilityTargetState>();
				}
			} else {
				owner.ChangeState<CommandCategorySelectionState>();
			}
		}

		private void ChangeDirection (Point p) {
			Direction dir = p.GetDirection();
			if (turn.actor.dir != dir) {
				board.DeSelectTiles(tiles);
				turn.actor.dir = dir;
				turn.actor.Match();
				SelectTiles ();
			}
		}

		private void SelectTiles () {
			tiles = ar.GetTilesInRange(board);
			board.SelectTiles(tiles);
		}
	}

}