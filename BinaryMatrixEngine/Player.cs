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
	public readonly CardSpecification card;

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

public readonly struct CardSpecification {
	public readonly Axiom? axiom;
	public readonly Value? value;

	public CardSpecification(Value? value, Axiom? axiom) {
		this.axiom = axiom;
		this.value = value;
	}

	public bool Matches(Card card) {
		if(this.value != null) {
			if(this.value != card.value)
				return false;
		}

		if(this.axiom != null) {
			if(this.axiom != card.axiom)
				return false;
		}

		return true;
	}
}

public interface Player : IDisposable {
	public PlayerRole Role { get; }

	public ActionSet GetAndConsumeAction();

	public void ReportOperationError(OperationError error);

	public int InvalidOperationCount { get; set; }
	public CardList Hand { get; }
}