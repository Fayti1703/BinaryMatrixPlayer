namespace BinaryMatrix.Engine;

public enum PlayerRole {
	ATTACKER,
	DEFENDER
}

public enum ActionType {
	NONE,
	DRAW,
	PLAY,
	FACEUP_PLAY,
	COMBAT,
	DISCARD
}

public struct ActionSet {
	public readonly ActionType type;
	public readonly int lane;
	public readonly CardSpecification? card;

	/* "a" Pseudo-Lane */
	public const int LANE_A = 6;
	public static readonly ActionSet NONE = new(ActionType.NONE);

	public ActionSet(ActionType type) {
		if(type != ActionType.NONE)
			throw new ArgumentException("Invalid overload called for this type.", nameof(type));
		this.type = type;
		this.lane = -1;
		this.card = default;
	}

	public ActionSet(ActionType type, int lane) {
		if(type != ActionType.DRAW && type != ActionType.COMBAT)
			throw new ArgumentException("Invalid overload called for this type.", nameof(type));
		this.type = type;
		this.lane = lane;
		this.card = default;
	}

	public ActionSet(ActionType type, CardSpecification card, int lane) {
		if(type != ActionType.PLAY && type != ActionType.FACEUP_PLAY && type != ActionType.DISCARD)
			throw new ArgumentException("Invalid overload called for this type.", nameof(type));
		this.type = type;
		this.lane = lane;
		this.card = card;
	}
}

public interface CardSpecification {
	public int? ResolveForPlayer(Player player);
}

public interface Player : IDisposable {
	public PlayerRole Role { get; }

	public ActionSet GetAndConsumeAction();

	public void ReportOperationError(OperationError error);

	public int InvalidOperationCount { get; set; }
	public CardList Hand { get; }
}

public readonly struct PlayerID {
	public readonly PlayerRole role;
	public readonly int index;

	public PlayerID(PlayerRole role, int index) {
		this.role = role;
		this.index = index;
	}

	override public string ToString() {
		return (this.role == PlayerRole.ATTACKER ? 'a' : 'd') + this.index.ToString();
	}
}
