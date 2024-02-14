using System.Diagnostics;
using System.Text;
using Fayti1703.CommonLib.Enumeration;

namespace BinaryMatrix.Engine.BinlogRenderers;

#pragma warning disable CS8524 // 'Unhandled unnamed enum value' -- not relevant, since our named enums are exhaustive for output.

public class FullStringBinlogRenderer : BinlogRenderer<string> {
	public string RenderBinlog(TurnLog log) {
		StringBuilder builder = new();
		builder.Append($"{log.turnNumber:000} ------:\n");
		foreach(ActionLog actionLog in log.actions) {
			RenderPlayerID(builder, actionLog.whoDidThis).Append(' ');
			RenderAction(builder, actionLog.resolvedAction);
			if(actionLog.moveResults != null) {
				if(actionLog.resolvedAction.type == ActionType.DRAW && actionLog.moveResults.Count == 0) {
					/* attacker victory! */
					builder.Append(" / >>>");
				}
				builder.Append(" / ");
				RenderMoveLogs(builder, actionLog.moveResults);
			}
			builder.Append('\n');
			if(actionLog.combatLog != null) {
				RenderCombatLog(builder, actionLog.combatLog.Value);
			}
		}

		return builder.ToString();
	}

	private static StringBuilder RenderCardList(StringBuilder builder, IEnumerable<CardID> cardIDs) {
		foreach((CardID cardID, bool last) in cardIDs.WithLast()) {
			RenderCardID(builder, cardID);
			if(!last) builder.Append(' ');
		}
		return builder;
	}

	private static StringBuilder RenderCombatLog(StringBuilder builder, CombatLog combatLog) {
		builder.Append("-- c").Append(combatLog.inLane).Append(" / ");
		RenderCardList(builder, combatLog.initialAS).Append(" / ");
		RenderCardList(builder, combatLog.initialDS);
		builder.Append('\n');
		foreach((CombatSpecialLog special, bool last) in combatLog.specials.WithLast()) {
			builder
				.Append("-- ")
				.Append(special.role switch {
					PlayerRole.ATTACKER => 'a',
					PlayerRole.DEFENDER => 'd'
				})
				.Append(special.type switch {
					SpecialType.TRAP => '@',
					SpecialType.BOUNCE => '?'
				}).Append(" / ")
				;
			RenderMoveLogs(builder, special.results);
			if(!last) builder.Append('\n');
		}

		builder.Append($"-- {combatLog.attackerPower} {combatLog.defenderPower} ");
		if(combatLog.damage < 0) builder.Append('-'); else builder.Append(combatLog.damage);
		if(combatLog.results.Count != 0) {
			builder.Append(" / ");
			RenderMoveLogs(builder, combatLog.results);
		}

		if(combatLog.victorDeclared)
			builder.Append(" / >>>");

		return builder;
	}

	private static StringBuilder RenderMoveLogs(StringBuilder builder, IReadOnlyList<CardMoveLog> moveLogs) {
		foreach((CardMoveLog moveLog, bool last) in moveLogs.WithLast()) {
			RenderCardList(builder, moveLog.cards).Append(' ');
			RenderMoveDestination(builder, moveLog.dest);
			if(!last) builder.Append(" / ");
		}

		return builder;
	}

	private static StringBuilder RenderMoveDestination(StringBuilder builder, MoveDestination destination) {
		switch(destination.type) {
			case DestinationType.HAND:
				builder.Append('h');
				return RenderPlayerID(builder, destination.player!.Value);
			case DestinationType.BOARD:
				return builder.Append(destination.cell!.Value.ToString().ToLowerInvariant());
		}

		throw new UnreachableException();
	}

	private static StringBuilder RenderAction(StringBuilder builder, ResolvedActionSet action) {
		switch(action.type) {
			case ActionType.NONE:
				return builder.Append("--");
			case ActionType.DRAW:
				return builder.Append('d').Append(action.lane);
			case ActionType.PLAY:
			case ActionType.FACEUP_PLAY:
			case ActionType.DISCARD:
#pragma warning disable CS8509 // 'Non-exhaustive switch expression' -- this switch expression is exhaustive per control-flow analysis
				builder.Append(action.type switch {
					ActionType.PLAY => 'p',
					ActionType.FACEUP_PLAY => 'u',
					ActionType.DISCARD => 'x'
				});
#pragma warning restore CS8509
				RenderCardID(builder, action.card);
				return builder.Append(action.lane);
			case ActionType.COMBAT:
				return builder.Append('c').Append(action.lane);
		}

		throw new UnreachableException();
	}

	private static StringBuilder RenderCardID(StringBuilder builder, CardID cardID) {
		return cardID.IsUnknown ?
			builder.Append('X') :
			builder.Append(CardID.AxiomToSymbol(cardID.axiom)).Append(CardID.ValueToSymbol(cardID.value))
		;
	}

	private static StringBuilder RenderPlayerID(StringBuilder builder, PlayerID playerID) {
		return builder.Append(playerID.role switch {
			PlayerRole.ATTACKER => 'a',
			PlayerRole.DEFENDER => 'd'
		}).Append(playerID.index.ToString("x"));
	}
}
