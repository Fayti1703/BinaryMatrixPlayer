using System;
using System.Collections.Generic;
using static BinaryMatrix.Engine.Axiom;
using static BinaryMatrix.Engine.CellName;
using static BinaryMatrix.Engine.Value;

namespace BinaryMatrix.Engine.Tests;

[TestClass]
public class CombatTests {
	public static readonly CombatLogComparer combatLogComparer = CombatLogComparer.CreateDefault();
	public static readonly IEqualityComparer<CardList> cardListComparer = new CardListComparer(new StrictCardComparer());
	public static readonly IEqualityComparer<GameBoard> gameBoardComparer = new GameBoardComparer(new CellComparer(cardListComparer));

	private static GameContext CreateScenarioContext() {
		Player attacker = new(PlayerRole.ATTACKER, 0, new TestPlayerActor());
		Player defender = new(PlayerRole.DEFENDER, 0, new TestPlayerActor());
		StaticRNG rng = new(Array.Empty<int>());
		GameContext game = new(new[] { attacker, defender }, rng, TestGameHooks.CreateDefaultHooks());
		return game;
	}

	[TestMethod]
	public void SimpleTrapDefender() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.Add(new Card(EIGHT, CHAOS));
		game.board[D0].cards.AddRange([ new Card(TRAP, CHAOS), new Card(TRAP, CHOICE) ]);

		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);
		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(EIGHT, CHAOS) ],
			initialDS: [ new CardID(TRAP, CHAOS),  new CardID(TRAP, CHOICE) ],
			specials: [ new CombatSpecialLog(SpecialType.TRAP, PlayerRole.DEFENDER,
				CardMoveLog.SingleMove(new CardID(EIGHT, CHAOS), X0)
			) ],
			attackerPower: 0, defenderPower: 0, damage: 0,
			results: Array.Empty<CardMoveLog>(),
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[D0].cards.AddRange([ new Card(TRAP, CHAOS) { revealed = true }, new Card(TRAP, CHOICE) { revealed = true } ]);
		expectedBoard[D0].Revealed = true;
		expectedBoard[X0].cards.Add(new Card(EIGHT, CHAOS) { revealed = true });
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}

	[TestMethod]
	public void WildDefense() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.Add(new Card(EIGHT, CHAOS));
		game.board[D0].cards.AddRange([ new Card(FIVE, CHAOS), new Card(WILD, CHAOS) ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(EIGHT, CHAOS) ],
			initialDS: [ new CardID(FIVE, CHAOS), new CardID(WILD, CHAOS) ],
			specials: [],
			attackerPower: 3, defenderPower: 3, damage: 1,
			results: [
				/* attacker stack discard */
				new CardMoveLog([ new CardID(EIGHT, CHAOS) ], XA),
				/* drawn from one damage */
				new CardMoveLog([ new CardID(WILD, CHAOS) ], XA)
			],
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[D0].cards.Add(new Card(FIVE, CHAOS) { revealed = true });
		expectedBoard[D0].Revealed = true;
		expectedBoard[XA].cards.AddRange([
			new Card(EIGHT, CHAOS) { revealed = true },
			new Card(WILD, CHAOS) { revealed = true }
		]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}

	[TestMethod]
	public void BounceDefense() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.Add(new Card(EIGHT, CHAOS));
		game.board[D0].cards.AddRange([ new Card(BOUNCE, CHAOS) ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(EIGHT, CHAOS) ],
			initialDS: [ new CardID(BOUNCE, CHAOS) ],
			specials: [
				new CombatSpecialLog(SpecialType.BOUNCE, PlayerRole.DEFENDER,
					CardMoveLog.SingleMove(new CardID(BOUNCE, CHAOS), XA)
				)
			],
			attackerPower: 0, defenderPower: 0, damage: 0,
			results: [
				/* attacker stack discard */
				new CardMoveLog([ new CardID(EIGHT, CHAOS) ], XA),
			],
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[XA].cards.AddRange([
			new Card(BOUNCE, CHAOS) { revealed = true },
			new Card(EIGHT, CHAOS) { revealed = true }
		]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}

	[TestMethod]
	public void BreakDefense() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.Add(new Card(TWO, CHAOS));
		game.board[D0].cards.AddRange([ new Card(THREE, CHAOS), new Card(THREE, CHOICE), new Card(BREAK, CHAOS) ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(TWO, CHAOS) ],
			initialDS: [ new CardID(THREE, CHAOS), new CardID(THREE, CHOICE), new CardID(BREAK, CHAOS) ],
			specials: [],
			attackerPower: 1, defenderPower: 0, damage: 3,
			results: [
				new CardMoveLog([ new CardID(TWO, CHAOS) ], XA),
				new CardMoveLog([ new CardID(BREAK, CHAOS), new CardID(THREE, CHOICE), new CardID(THREE, CHAOS) ], XA)
			],
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[XA].cards.AddRange([
			new Card(TWO, CHAOS) { revealed = true },
			new Card(BREAK, CHAOS) { revealed = true },
			new Card(THREE, CHOICE) { revealed = true },
			new Card(THREE, CHAOS) { revealed = true }
		]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}

	[TestMethod]
	public void BreakDefenseCombat() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.Add(new Card(TWO, CHAOS));
		game.board[D0].cards.AddRange([ new Card(THREE, CHAOS), new Card(THREE, CHOICE), new Card(BREAK, CHAOS) { revealed = true } ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Defenders[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(TWO, CHAOS) ],
			initialDS: [ new CardID(THREE, CHAOS), new CardID(THREE, CHOICE), new CardID(BREAK, CHAOS) ],
			specials: [],
			attackerPower: 1, defenderPower: 0, damage: 3,
			results: [
				new CardMoveLog([ new CardID(TWO, CHAOS) ], XA),
				new CardMoveLog([ new CardID(BREAK, CHAOS), new CardID(THREE, CHOICE), new CardID(THREE, CHAOS) ], XA)
			],
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[XA].cards.AddRange([
			new Card(TWO, CHAOS) { revealed = true },
			new Card(BREAK, CHAOS) { revealed = true },
			new Card(THREE, CHOICE) { revealed = true },
			new Card(THREE, CHAOS) { revealed = true }
		]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}

	[TestMethod]
	public void BreakDefenseMistakeCombat() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.Add(new Card(EIGHT, CHAOS));
		game.board[D0].cards.AddRange([ new Card(FOUR, CHAOS), new Card(BREAK, CHAOS) { revealed = true } ]);
		game.board[L0].cards.AddRange([ new Card(TWO, KIN), new Card(FIVE, CHAOS) ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Defenders[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(EIGHT, CHAOS) ],
			initialDS: [ new CardID(FOUR, CHAOS), new CardID(BREAK, CHAOS) ],
			specials: [],
			attackerPower: 3, defenderPower: 2, damage: 3,
			results: [
				new CardMoveLog([ new CardID(EIGHT, CHAOS) ], XA),
				new CardMoveLog([ new CardID(BREAK, CHAOS), new CardID(FOUR,  CHAOS) ], XA),
				new CardMoveLog([ CardID.Unknown ], new PlayerID(PlayerRole.ATTACKER, 0))
			],
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[XA].cards.AddRange([
			new Card(EIGHT, CHAOS) { revealed = true },
			new Card(BREAK, CHAOS) { revealed = true },
			new Card(FOUR, CHAOS) { revealed = true },
		]);
		expectedBoard[L0].cards.AddRange([ new Card(TWO, KIN) ]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
		Assert.AreEqual(game.Attackers[0].Hand, new CardList { new(FIVE, CHAOS) }, cardListComparer);
	}

	[TestMethod]
	public void BreakDefenseBlunderCombat() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.AddRange([ new Card(EIGHT, CHAOS), new Card(EIGHT, CHOICE) ]);
		game.board[D0].cards.AddRange([ new Card(FOUR, CHAOS), new Card(BREAK, CHAOS) { revealed = true } ]);
		game.board[L0].cards.AddRange([ new Card(FIVE, CHAOS) ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Defenders[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(EIGHT, CHAOS), new CardID(EIGHT, CHOICE) ],
			initialDS: [ new CardID(FOUR, CHAOS), new CardID(BREAK, CHAOS) ],
			specials: [],
			attackerPower: 4, defenderPower: 2, damage: 4,
			results: [
				new CardMoveLog([ new CardID(EIGHT, CHAOS), new CardID(EIGHT, CHOICE) ], XA),
				new CardMoveLog([ new CardID(BREAK, CHAOS), new CardID(FOUR, CHAOS) ], XA),
				new CardMoveLog([ CardID.Unknown ], new PlayerID(PlayerRole.ATTACKER, 0))
			],
			victorDeclared: true
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[XA].cards.AddRange([
			new Card(EIGHT, CHAOS) { revealed = true },
			new Card(EIGHT, CHOICE) { revealed = true },
			new Card(BREAK, CHAOS) { revealed = true },
			new Card(FOUR, CHAOS) { revealed = true },
		]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
		Assert.AreEqual(game.Attackers[0].Hand, new CardList { new(FIVE, CHAOS) }, cardListComparer);
		Assert.AreEqual(game.Victor, PlayerRole.ATTACKER);
	}

	[TestMethod]
	public void SimpleVictoryDefense() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.Add(new Card(EIGHT, CHAOS));
		game.board[D0].cards.AddRange([ new Card(TEN, CHAOS), new Card(SIX, CHAOS) ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(EIGHT, CHAOS) ],
			initialDS: [ new CardID(TEN, CHAOS), new CardID(SIX, CHAOS) ],
			specials: [],
			attackerPower: 3, defenderPower: 4, damage: -1,
			results: [
				new CardMoveLog([ new CardID(EIGHT, CHAOS) ], X0),
			],
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[D0].cards.AddRange([
			new Card(TEN, CHAOS) { revealed = true },
			new Card(SIX, CHAOS) { revealed = true }
		]);
		expectedBoard[D0].Revealed = true;
		expectedBoard[X0].cards.Add(new Card(EIGHT, CHAOS) { revealed = true });
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}

	[TestMethod]
	public void SimpleTrapAttacker() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.AddRange([ new Card(TRAP, CHAOS), new Card(EIGHT, CHAOS) ]);
		game.board[D0].cards.Add(new Card(BOUNCE, CHAOS));
		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);
		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(TRAP, CHAOS), new CardID(EIGHT, CHAOS) ],
			initialDS: [ new CardID(BOUNCE, CHAOS) ],
			specials: [ new CombatSpecialLog(SpecialType.TRAP, PlayerRole.ATTACKER,
				CardMoveLog.SingleMove(new CardID(BOUNCE, CHAOS), XA)
			) ],
			attackerPower: 3, defenderPower: 0, damage: 4,
			results: [
				new CardMoveLog([ new CardID(TRAP, CHAOS), new CardID(EIGHT, CHAOS) ], XA)
			],
			victorDeclared: true
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[XA].cards.AddRange([
			new Card(BOUNCE, CHAOS) { revealed = true },
			new Card(TRAP, CHAOS) { revealed = true },
			new Card(EIGHT, CHAOS) { revealed = true }
		]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
		Assert.AreEqual(game.Victor, PlayerRole.ATTACKER);
	}

	[TestMethod]
	public void BounceAttack() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.Add(new Card(BOUNCE, CHAOS));
		game.board[D0].cards.Add(new Card(FOUR, CHAOS));
		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);
		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(BOUNCE, CHAOS) ],
			initialDS: [ new CardID(FOUR, CHAOS) ],
			specials: [ new CombatSpecialLog(SpecialType.BOUNCE, PlayerRole.ATTACKER,
				CardMoveLog.SingleMove(new CardID(BOUNCE, CHAOS), X0)
			) ],
			attackerPower: 0, defenderPower: 0, damage: 0,
			results: [],
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[D0].cards.AddRange([
			new Card(FOUR, CHAOS) { revealed = true }
		]);
		expectedBoard[D0].Revealed = true;
		expectedBoard[X0].cards.Add(new Card(BOUNCE, CHAOS) { revealed = true });
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}

	[TestMethod]
	public void BreakAttack() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.AddRange([ new Card(TWO, CHAOS), new Card(BREAK, CHAOS) ]);
		game.board[D0].cards.AddRange([ new Card(THREE, CHAOS), new Card(THREE, CHOICE), new Card(THREE, KIN) ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(TWO, CHAOS), new CardID(BREAK, CHAOS) ],
			initialDS: [ new CardID(THREE, CHAOS), new CardID(THREE, CHOICE), new CardID(THREE, KIN) ],
			specials: [],
			attackerPower: 1, defenderPower: 0, damage: 3,
			results: [
				new CardMoveLog([ new CardID(TWO, CHAOS), new CardID(BREAK, CHAOS) ], XA),
				new CardMoveLog([ new CardID(THREE, KIN), new CardID(THREE, CHOICE), new CardID(THREE, CHAOS) ], XA)
			],
			victorDeclared: false
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[XA].cards.AddRange([
			new Card(TWO, CHAOS) { revealed = true },
			new Card(BREAK, CHAOS) { revealed = true },
			new Card(THREE, KIN) { revealed = true },
			new Card(THREE, CHOICE) { revealed = true },
			new Card(THREE, CHAOS) { revealed = true }
		]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}

	[TestMethod]
	public void SimpleVictoryAttacker() {
		GameContext game = CreateScenarioContext();
		game.board[A0].cards.AddRange([ new Card(TEN, CHAOS), new Card(SIX, CHAOS) ]);
		game.board[D0].cards.AddRange([ new Card(EIGHT, CHAOS) ]);
		GameExecution.ResolveCombat(game, game.board[0], game.Attackers[0], out CombatLog log);

		Assert.AreEqual(new CombatLog(
			inLane: 0,
			initialAS: [ new CardID(TEN, CHAOS), new CardID(SIX, CHAOS) ],
			initialDS: [ new CardID(EIGHT, CHAOS) ],
			specials: [],
			attackerPower: 4, defenderPower: 3, damage: 2,
			results: [
				new CardMoveLog([ new CardID(TEN, CHAOS), new CardID(SIX, CHAOS) ], XA),
				new CardMoveLog([ new CardID(EIGHT, CHAOS) ], XA)
			],
			victorDeclared: true
		), log, combatLogComparer);

		GameBoard expectedBoard = new();
		expectedBoard[XA].cards.AddRange([
			new Card(TEN, CHAOS) { revealed = true },
			new Card(SIX, CHAOS) { revealed = true },
			new Card(EIGHT, CHAOS) { revealed = true }
		]);
		Assert.AreEqual(expectedBoard, game.board, gameBoardComparer);
	}
}
