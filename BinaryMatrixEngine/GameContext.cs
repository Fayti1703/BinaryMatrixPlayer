using System.Collections.Immutable;
using JetBrains.Annotations;

namespace BinaryMatrix.Engine;

public struct GameState {
	public readonly int turnCounter;
	public readonly GameBoard board;
	public readonly PlayerRole? victor;
	public readonly IReadOnlyList<TurnLog> binlog;

	public GameState(int turnCounter, GameBoard board, PlayerRole? victor, IReadOnlyList<TurnLog> binlog) {
		this.turnCounter = turnCounter;
		this.board = board;
		this.victor = victor;
		this.binlog = binlog;
	}
}

[PublicAPI]
public sealed class GameContext : IDisposable {
	public int TurnCounter { get; private set; }
	public IReadOnlyList<Player> Attackers { get; }
	public IReadOnlyList<Player> Defenders { get; }
	public readonly GameBoard board;
	public IReadOnlyList<TurnLog> FullBinlog => this.binlog;

	private List<TurnLog> binlog;

	public RNG rng { get; }

	private IEnumerable<Player> Players => this.Attackers.Concat(this.Defenders);
	private readonly GameHooks hooks;

	public PlayerRole? Victor { get; private set; }

	private GameContext(IEnumerable<Player> players, RNG rng, GameHooks? hooks, GameBoard board, List<TurnLog> binlog) {
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
		this.board = board;
		this.binlog = binlog;
	}

	public GameContext(IEnumerable<Player> players, RNG rng, GameHooks hooks)
		: this(players, rng, hooks, new GameBoard(), new List<TurnLog>()) { }

	public GameContext(GameState state, IEnumerable<Player> players, RNG rng, GameHooks hooks)
		: this(players, rng, hooks, state.board.Copy(), new List<TurnLog>(state.binlog)) {
		this.TurnCounter = state.turnCounter;
		this.Victor = state.victor;
	}

	public void Setup() {
		/* Clean any remaining state from possible previous runs */
		this.board.Clear();
		this.binlog.Clear();
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
			this.TurnCounter,
			this.board.Copy(),
			this.Victor,
			this.binlog.ToImmutableList()
		);
	}

	public PlayerRole? Tick() {
		this.hooks.PreTurn(this);
		IEnumerable<Player> activePlayers = this.TurnCounter % 2 == 0 ? this.Defenders : this.Attackers;
		{
			List<ActionLog> actions = new();
			HashSet<Cell> drawnDecks = new();
			foreach((Player player, ActionSet action) in this.hooks.GetActions(this, activePlayers)) {
				GameExecution.ExecutePlayerTurn(this, player, action, drawnDecks, out ActionLog actionLog);
				actions.Add(actionLog);
				if(this.Victor != null) break;
			}
			this.binlog.Add(new TurnLog(this.TurnCounter, actions));
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

	[Obsolete("Access the player's ID property directly")]
	public PlayerID GetPlayerID(Player player) {
		return player.ID;
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
	[Obsolete("Create your own GameHooks instead.")]
	public static readonly GameHooks Default;

	public delegate void PreGamePrepType(GameContext context);
	public delegate void PreTurnType(GameContext context);
	public delegate void PostTurnType(GameContext context);
	public delegate IEnumerable<(Player player, ActionSet action)> GetActionsType(GameContext context, IEnumerable<Player> activePlayers);

	public required PreGamePrepType PreGamePrep;
	public required PreTurnType PreTurn;
	public required PostTurnType PostTurn;
	public required GetActionsType GetActions;

	public static GameHooks MakeAsyncRules(GetActionsType getActions) {
		return new GameHooks {
			PreGamePrep = StandardPreGamePrep,
			PreTurn = NoopPreTurn,
			PostTurn = AsyncPostTurn,
			GetActions = getActions
		};
	}

	static GameHooks() {
		Default = new GameHooks {
			PreGamePrep = StandardPreGamePrep,
			PreTurn = NoopPreTurn,
			PostTurn = AsyncPostTurn,
			GetActions = LegacyGetPlayerActions
		};
	}

	public static void NoopPreTurn(GameContext context) { /* do nothing */ }

	public static void AsyncPostTurn(GameContext context) {
		if(context is { TurnCounter: 109 }) {
			context.SetVictor(PlayerRole.DEFENDER);
		}
	}

	public static void StandardPreGamePrep(GameContext context) {
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

	[Obsolete("Implement your own `GetActions` hook instead. This method may disappear in future.")]
	public static IEnumerable<(Player player, ActionSet action)> LegacyGetPlayerActions(GameContext context, IEnumerable<Player> activePlayers) {
		foreach(Player player in activePlayers) {
			if(player.actor is ActionablePlayerActor actionableActor) {
				yield return (player, actionableActor.GetAndConsumeAction());
			} else {
				throw new Exception("Cannot handle an non-ActionablePlayerActor via LegacyGetPlayerActions!");
			}
		}
	}
}
