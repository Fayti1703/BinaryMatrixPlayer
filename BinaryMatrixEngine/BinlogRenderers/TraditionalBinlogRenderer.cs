using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Fayti1703.CommonLib.Enumeration;

namespace BinaryMatrix.Engine.BinlogRenderers;

#pragma warning disable CS8524 // 'Unhandled unnamed enum value' -- not relevant, since our named enums are exhaustive for output.



public class TraditionalBinlogRenderer : BinlogRenderer<string[]> {
	public string[] RenderBinlog(TurnLog log) {
		StringArrayBuilder builder = new();
		builder.Append($"`V{log.turnNumber:000}` `n------`");
		builder.BreakEntry();
		foreach(ActionLog actionLog in log.actions) {
			RenderPlayerID(builder.c, actionLog.whoDidThis).Append(' ');
			RenderAction(builder.c, actionLog.resolvedAction);
			if(actionLog.moveResults != null) {
				builder.Append(' ');
				RenderMoveLogs(builder.c, actionLog.moveResults);
			}
			builder.BreakEntry();
			if(actionLog.combatLog != null) {
				RenderCombatLog(builder, actionLog.combatLog.Value);
			}
		}

		return builder.ToArray();
	}

	private static void RenderCombatLog(StringArrayBuilder builder, CombatLog combatLog) {
		builder.Append("`n--` c").Append(combatLog.inLane).Append(" / ");
		RenderCardList(builder.c, combatLog.initialAS).Append("/ ");
		RenderCardList(builder.c, combatLog.initialDS);
		builder.BreakEntry();
		int remainingCardsInDS = combatLog.initialDS.Count;
		foreach(CombatSpecialLog special in combatLog.specials) {
			builder.Append("`n--`")
				.Append(special.role switch {
					PlayerRole.ATTACKER => 'a',
					PlayerRole.DEFENDER => 'd'
				})
				.Append(special.type switch {
					SpecialType.TRAP => '@',
					SpecialType.BOUNCE => '?'
				}).Append(' ');
			RenderMoveLogs(builder.c, special.results);
			builder.BreakEntry();
			/* `d?` or `a@` */
			if(( special.role == PlayerRole.DEFENDER ) == ( special.type == SpecialType.BOUNCE )) {
				remainingCardsInDS -= special.results[0].cards.Count;
			}
		}

		builder.Append($"`n--` {combatLog.attackerPower} {combatLog.defenderPower}");
		if(combatLog.damage < 0) builder.Append('-'); else builder.Append(combatLog.damage);
		builder.Append(" / ");
		foreach((int index, CardMoveLog moveLog) in combatLog.results.WithIndex()) {
			RenderCardList(builder.c, moveLog.cards).Append(' ');
			/* replicate an in-game bug: if the attacker damage doesn't completely drain the DS, the destination (XA) is not rendered */
			if(!( index == 1 && moveLog.dest == CellName.XA && moveLog.cards.Count < remainingCardsInDS ))
				RenderMoveDestination(builder.c, moveLog.dest);
		}

		/* no special victor declaration */
	}

	private static StringBuilder RenderCardList(StringBuilder builder, IEnumerable<CardID> cardIDs) {
		foreach((CardID cardID, bool last) in cardIDs.WithLast()) {
			RenderCardID(builder, cardID);
			if(!last) builder.Append(' ');
		}
		return builder;
	}

	private static StringBuilder RenderMoveLogs(StringBuilder builder, IReadOnlyList<CardMoveLog> moveLogs) {
		foreach(CardMoveLog moveLog in moveLogs) {
			/* done this way replicate in-game spacing for move logs (always appends a ` `, even if end-of-list) */
			builder.Append("/ ");
			RenderCardList(builder, moveLog.cards).Append(' ');
			RenderMoveDestination(builder, moveLog.dest).Append(' ');
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
				return builder.Append("`n--`");
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

	private static StringBuilder RenderPlayerID(StringBuilder builder, PlayerID playerID) {
		return builder.Append(playerID.role switch {
			PlayerRole.ATTACKER => 'a',
			PlayerRole.DEFENDER => 'd'
		}).Append(playerID.index.ToString("x"));
	}

	private static StringBuilder RenderCardID(StringBuilder builder, CardID cardID) {
		return cardID.IsUnknown ?
			builder.Append('X') :
			builder.Append(CardID.AxiomToSymbol(cardID.axiom)).Append(CardID.ValueToSymbol(cardID.value))
		;
	}
}


internal class StringArrayBuilder : IReadOnlyList<string> {
	public readonly StringBuilder c = new();
	private readonly List<string> entries = new();

	#region StringBuilder part
	public StringArrayBuilder Append(string s) {
		this.c.Append(s);
		return this;
	}

	public StringArrayBuilder Append(int v) {
		this.c.Append(v);
		return this;
	}

	public StringArrayBuilder Append(char c) {
		this.c.Append(c);
		return this;
	}

	public StringArrayBuilder BreakEntry() {
		this.entries.Add(this.c.ToString());
		this.c.Clear();
		return this;
	}

	public void Clear() { this.entries.Clear(); }

	public StringArrayBuilder Append(
		[InterpolatedStringHandlerArgument("")]
		ref AppendInterpolation handler
	) {
		return this;
	}

	[InterpolatedStringHandler]
	public struct AppendInterpolation {
		private StringBuilder.AppendInterpolatedStringHandler appender;

		public AppendInterpolation(int literalLength, int formattedCount, StringArrayBuilder parent) =>
			this.appender = new StringBuilder.AppendInterpolatedStringHandler(literalLength, formattedCount, parent.c);

		public void AppendLiteral(string value) => this.appender.AppendLiteral(value);
		public void AppendFormatted<T>(T value) => this.appender.AppendFormatted(value);
		public void AppendFormatted<T>(T value, string? format) => this.appender.AppendFormatted(value, format);
		public void AppendFormatted<T>(T value, int alignment) => this.appender.AppendFormatted(value, alignment);
		public void AppendFormatted<T>(T value, int alignment, string? format) => this.appender.AppendFormatted(value, alignment, format);
		public void AppendFormatted(ReadOnlySpan<char> value, int alignment = 0, string? format = null) => this.appender.AppendFormatted(value, alignment, format);
		public void AppendFormatted(string? value, int alignment = 0, string? format = null) => this.appender.AppendFormatted(value, alignment, format);
		public void AppendFormatted(object? value, int alignment = 0, string? format = null) => this.appender.AppendFormatted(value, alignment, format);
	}

	#endregion

	#region List<string> part
	public string[] ToArray() => this.entries.ToArray();

	public IEnumerator<string> GetEnumerator() => this.entries.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => ( (IEnumerable) this.entries ).GetEnumerator();
	public int Count => this.entries.Count;

	public string this[int index] {
		get => this.entries[index];
		set => this.entries[index] = value;
	}
	#endregion
}
