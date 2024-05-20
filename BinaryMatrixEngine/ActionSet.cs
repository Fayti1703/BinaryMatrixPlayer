namespace BinaryMatrix.Engine;

public enum ActionType {
	NONE,
	DRAW,
	PLAY,
	FACEUP_PLAY,
	COMBAT,
	DISCARD
}

public interface CardSpecification {
	public int? ResolveForPlayer(Player player);
}

public readonly struct ActionSet {
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
		this.lane = ValidateLane(lane, allowPseudo: type == ActionType.DRAW);
		this.card = default;
	}

	public ActionSet(ActionType type, CardSpecification card, int lane) {
		if(type != ActionType.PLAY && type != ActionType.FACEUP_PLAY && type != ActionType.DISCARD)
			throw new ArgumentException("Invalid overload called for this type.", nameof(type));
		this.type = type;
		this.lane = ValidateLane(lane, allowPseudo: type == ActionType.DISCARD);
		this.card = card;
	}

	private static int ValidateLane(int lane, bool allowPseudo = false) {
		return lane switch {
			< 0 or > LANE_A => throw new ArgumentOutOfRangeException(nameof(lane), lane, "Lane must be within the range [0; " + LANE_A + "]."),
			LANE_A when !allowPseudo => throw new ArgumentOutOfRangeException(nameof(lane), lane, "Lane must name a real lane, not the `a` pseudolane, for this action."),
			_ => lane
		};
	}
}
