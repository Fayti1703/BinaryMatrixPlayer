using System.Collections.Immutable;
using Fayti1703.CommonLib.Enumeration;
using JetBrains.Annotations;

namespace BinaryMatrix.Engine;

public enum GameType {
	ASYNC
}

public struct GameState {
	public readonly GameType gameType;
	public readonly int turnCounter;
	public readonly GameBoard board;
	public readonly PlayerRole? victor;

	public GameState(GameType type, int turnCounter, GameBoard board, PlayerRole? victor) {
		this.gameType = type;
		this.turnCounter = turnCounter;
		this.board = board;
		this.victor = victor;
	}
}

[PublicAPI]
public sealed class GameContext : IDisposable {
	public readonly GameType gameType;
	public int TurnCounter { get; private set; }
	public IReadOnlyList<Player> Attackers { get; }
	public IReadOnlyList<Player> Defenders { get; }
	public readonly GameBoard board;

	public RNG rng { get; }

	private IEnumerable<Player> Players => this.Attackers.Concat(this.Defenders);
	private readonly GameHooks hooks;

	public PlayerRole? Victor { get; private set; }

	private GameContext(IEnumerable<Player> players, RNG rng, GameHooks? hooks) {
		/* fallbacks */
		this.Attackers = ImmutableList<Player>.Empty;
		this.Defenders = ImmutableList<Player>.Empty;
		foreach(IGrouping<PlayerRole,Player> group in players.GroupBy(x => x.Role)) {
			switch(group.Key) {
				case PlayerRole.ATTACKER:
					this.Attackers = group.ToList();
					break;
				case PlayerRole.DEFENDER:
					this.Defenders = group.ToList();
					break;
			}
		}
		this.rng = rng;
		this.hooks = hooks ?? GameHooks.Default;
	}

	public GameContext(GameType gameType, IEnumerable<Player> players, RNG rng, GameHooks? hooks = null) : this(players, rng, hooks) {
		this.gameType = gameType;
		this.board = new GameBoard();
	}

	public GameContext(GameState state, IEnumerable<Player> players, RNG rng, GameHooks? hooks = null) : this(players, rng, hooks) {
		this.gameType = state.gameType;
		this.TurnCounter = state.turnCounter;
		this.Victor = state.victor;
		this.board = state.board.Copy();
	}

	public void Setup() {
		/* Clean any remaining state from possible previous runs */
		this.board.Clear();
		foreach(Player player in this.Players)
			player.InvalidOperationCount = 0;
		this.Victor = null;

		this.hooks.PreGamePrep(this);
		this.board[CellName.L3].cards.Last().Apply((ref Card x) => x.revealed = true);
		this.board[CellName.L4].cards.Last().Apply((ref Card x) => x.revealed = true);
		this.board[CellName.L5].cards.Last().Apply((ref Card x) => x.revealed = true);
	}

	public GameState SaveState() {
		return new GameState(
			this.gameType,
			this.TurnCounter,
			this.board.Copy(),
			this.Victor
		);
	}

	public PlayerRole? Tick() {
		this.hooks.PreTurn(this);
		IEnumerable<Player> activePlayers = this.TurnCounter % 2 == 0 ? this.Defenders : this.Attackers;
		{
			HashSet<Cell> drawnDecks = new();
			foreach(
				(Player player, ActionSet action) in
				activePlayers.Select(x => (player: x, action: x.GetAndConsumeAction()))
			) {
				GameExecution.ExecutePlayerTurn(this, player, action, drawnDecks);
				if(this.Victor != null) break;
			}
		}
		if(this.Victor != null) return this.Victor;
		this.hooks.PostTurn(this);
		if(this.Victor != null) return this.Victor;
		this.TurnCounter++;
		return null;
	}

	public void SetVictor(PlayerRole role) {
		if(this.Victor != null)
			throw new InvalidOperationException("A victor has already been declared.");
		this.Victor = role;
	}


	public void Dispose() {
		this.board.Dispose();
	}

	public PlayerID GetPlayerID(Player player) {
		IReadOnlyList<Player> containingList = player.Role == PlayerRole.ATTACKER ? this.Attackers : this.Defenders;
		/* ``IReadOnlyList`1`` doesn't have an `IndexOf`, so... */
		return new PlayerID(player.Role, containingList.WithIndex().Single(x => x.value == player).index);
	}
}

public interface RNG {
	/** <summary>Return a new random value, in the range <c>[0;rangeEnd[</c>.</summary> */
	int Next(int upperBound);
}

public class RandomRNG : RNG {
	private readonly Random random;

	public RandomRNG(Random random) {
		this.random = random;
	}
	public int Next(int upperBound) => this.random.Next(upperBound);
}

[PublicAPI]
public struct GameHooks {
	public static readonly GameHooks Default;

	public delegate void PreGamePrepType(GameContext context);
	public delegate void PreTurnType(GameContext context);
	public delegate void PostTurnType(GameContext context);

	public PreGamePrepType PreGamePrep;
	public PreTurnType PreTurn;
	public PostTurnType PostTurn;

	static GameHooks() {
		Default = new GameHooks {
			PreGamePrep = DefaultPreGamePrep,
			PreTurn = DefaultPreTurn,
			PostTurn = DefaultPostTurn
		};
	}

	private static void DefaultPreTurn(GameContext context) { /* do nothing */ }

	private static void DefaultPostTurn(GameContext context) {
		if(context is { gameType: GameType.ASYNC, TurnCounter: 109 }) {
			context.SetVictor(PlayerRole.DEFENDER);
		}
	}

	private static void DefaultPreGamePrep(GameContext context) {
		using CardList cards = GameExecution.FisherYatesShuffle(context.rng, Card.allCards);
		int j = 0;
		for(int i = 0; i < 13; i++) {
			context.board[CellName.L0].cards.Add(cards[j++]);
			context.board[CellName.L1].cards.Add(cards[j++]);
			context.board[CellName.L2].cards.Add(cards[j++]);
			context.board[CellName.L3].cards.Add(cards[j++]);
			context.board[CellName.L4].cards.Add(cards[j++]);
			context.board[CellName.L5].cards.Add(cards[j++]);
		}
	}
}
