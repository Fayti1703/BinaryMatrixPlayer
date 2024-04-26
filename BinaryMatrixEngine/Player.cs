namespace BinaryMatrix.Engine;

public enum PlayerRole {
	ATTACKER,
	DEFENDER
}

public class PlayerData : IDisposable {
	public readonly PlayerID id;
	public int invalidOperationCount;
	public readonly CardList hand;

	public PlayerData(PlayerID id, CardList? hand = null) {
		this.id = id;
		this.hand = hand ?? new CardList();
	}

	public void Dispose() {
		this.hand.Dispose();
	}

	public PlayerData Copy() {
		return new PlayerData(this.id, new CardList(this.hand)) {
			invalidOperationCount = this.invalidOperationCount
		};
	}
}

public sealed class Player : IDisposable {
	public Player(
		PlayerRole role,
		int index,
		PlayerActor actor,
		CardList? hand = null
	) : this(new PlayerID(role, index), actor, hand) {}

	public Player(
		PlayerID id,
		PlayerActor actor,
		CardList? hand = null
	) : this(new PlayerData(id, hand), actor) { }

	public Player(PlayerData data, PlayerActor actor) {
		this.data = data;
		this.actor = actor;
	}

	public readonly PlayerData data;

	public PlayerID ID => this.data.id;
	public PlayerRole Role => this.ID.role;
	public int InvalidOperationCount {
		get => this.data.invalidOperationCount;
		set => this.data.invalidOperationCount = value;
	}
	public CardList Hand => this.data.hand;

	public readonly PlayerActor actor;

	public void Dispose() {
		this.data.Dispose();
		this.actor.Dispose();
	}
}

public interface PlayerActor : IDisposable {
	public void ReportOperationError(OperationError error);
}

[Obsolete("Implement your own `GetActions` hook instead of relying on this interface.")]
public interface ActionablePlayerActor : PlayerActor {
	public ActionSet GetAndConsumeAction();
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
