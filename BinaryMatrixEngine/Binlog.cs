namespace BinaryMatrix.Engine;

public readonly struct ResolvedActionSet {
	public readonly ActionType type;
	public readonly int lane;
	public readonly CardID card;
	public readonly bool explicitNone = false;

	public ResolvedActionSet(ActionType type, bool explicitNone = false) {
		if(type != ActionType.NONE)
			throw new ArgumentException("Invalid overload called for this type.", nameof(type));
		this.type = type;
		this.explicitNone = explicitNone;
		this.lane = -1;
		this.card = CardID.Unknown;
	}

	public ResolvedActionSet(ActionType type, int lane) {
		if(type != ActionType.DRAW && type != ActionType.COMBAT)
			throw new ArgumentException("Invalid overload called for this type.", nameof(type));
		this.type = type;
		this.lane = lane;
		this.card = CardID.Unknown;
	}

	public ResolvedActionSet(ActionType type, CardID card, int lane) {
		if(type != ActionType.PLAY && type != ActionType.FACEUP_PLAY && type != ActionType.DISCARD)
			throw new ArgumentException("Invalid overload called for this type.", nameof(type));
		this.type = type;
		this.lane = lane;
		this.card = card;
	}
}

public readonly struct MoveDestination : IEquatable<MoveDestination> {
	public readonly DestinationType type;
	public readonly CellName? cell;
	public readonly PlayerID? player;

	public MoveDestination(CellName cell) {
		this.type = DestinationType.BOARD;
		this.cell = cell;
	}

	public MoveDestination(PlayerID player) {
		this.type = DestinationType.HAND;
		this.player = player;
	}

	public bool Equals(MoveDestination other) =>
		this.type == other.type && Nullable.Equals(this.cell, other.cell) && Nullable.Equals(this.player, other.player);
	override public bool Equals(object? obj) => obj is MoveDestination other && this.Equals(other);
	override public int GetHashCode() => HashCode.Combine((int) this.type, this.cell, this.player);
	public static bool operator ==(MoveDestination left, MoveDestination right) => left.Equals(right);
	public static bool operator !=(MoveDestination left, MoveDestination right) => !left.Equals(right);

	public static implicit operator MoveDestination(CellName name) => new(name);
	public static implicit operator MoveDestination(PlayerID name) => new(name);
}

public enum DestinationType {
	BOARD,
	HAND
}

public readonly struct CardMoveLog {
	public readonly IReadOnlyList<CardID> cards;
	public readonly MoveDestination dest;

	public CardMoveLog(IReadOnlyList<CardID> cards, MoveDestination destination) {
		this.cards = cards;
		this.dest = destination;
	}

	public static IReadOnlyList<CardMoveLog> SingleMove(CardID card, MoveDestination destination) {
		return new[] { new CardMoveLog(new[] { card }, destination) };
	}
}

public enum SpecialType {
	TRAP,
	BOUNCE
}

public readonly struct CombatSpecialLog {
	public readonly SpecialType type;
	public readonly PlayerRole role;

	public readonly IReadOnlyList<CardMoveLog> results;

	public CombatSpecialLog(SpecialType type, PlayerRole role, IReadOnlyList<CardMoveLog> results) {
		this.type = type;
		this.role = role;
		this.results = results;
	}
}

public readonly struct CombatLog {
	public readonly int inLane;
	public readonly IReadOnlyList<CardID> initialAS;
	public readonly IReadOnlyList<CardID> initialDS;

	public readonly IReadOnlyList<CombatSpecialLog> specials;
	public readonly int attackerPower;
	public readonly int defenderPower;
	public readonly int damage;

	public readonly IReadOnlyList<CardMoveLog> results;
	public readonly bool victorDeclared = false;

	public CombatLog(
		int inLane,
		IReadOnlyList<CardID> initialAS,
		IReadOnlyList<CardID> initialDS,
		IReadOnlyList<CombatSpecialLog> specials,
		int attackerPower,
		int defenderPower,
		int damage,
		IReadOnlyList<CardMoveLog> results,
		bool victorDeclared = false
	) {
		this.inLane = inLane;
		this.initialAS = initialAS;
		this.initialDS = initialDS;
		this.specials = specials;
		this.attackerPower = attackerPower;
		this.defenderPower = defenderPower;
		this.damage = damage;
		this.results = results;
		this.victorDeclared = victorDeclared;
	}
}


public readonly struct ActionLog {
	public readonly PlayerID whoDidThis;
	public readonly ResolvedActionSet resolvedAction;
	public readonly IReadOnlyList<CardMoveLog>? moveResults;
	public readonly CombatLog? combatLog;

	public ActionLog(
		PlayerID whoDidThis,
		ResolvedActionSet resolvedAction,
		IReadOnlyList<CardMoveLog>? moveResults,
		CombatLog? combatLog = null
	) {
		this.whoDidThis = whoDidThis;
		this.resolvedAction = resolvedAction;
		this.moveResults = moveResults;
		this.combatLog = combatLog;
	}
}

public readonly struct TurnLog {
	public readonly int turnNumber;
	public readonly IReadOnlyList<ActionLog> actions;

	public TurnLog(int turnNumber, IReadOnlyList<ActionLog> actions) {
		this.turnNumber = turnNumber;
		this.actions = actions;
	}
}
