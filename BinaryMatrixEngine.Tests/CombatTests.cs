using System;
using System.Collections.Generic;
using static BinaryMatrix.Engine.Axiom;
using static BinaryMatrix.Engine.CellName;
using static BinaryMatrix.Engine.Value;

namespace BinaryMatrix.Engine.Tests;

[TestClass]
public class CombatTests {
	public static readonly CombatLogComparer combatLogComparer = CombatLogComparer.CreateDefault();
	public static readonly IEqualityComparer<GameBoard> gameBoardComparer = GameBoardComparer.CreateDefault();

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
}
