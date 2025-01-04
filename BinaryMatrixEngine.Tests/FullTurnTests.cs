using System;
using System.Collections.Immutable;
using static BinaryMatrix.Engine.Axiom;
using static BinaryMatrix.Engine.CellName;
using static BinaryMatrix.Engine.Value;

namespace BinaryMatrix.Engine.Tests;

[TestClass]
public class FullTurnTests {
	[TestMethod]
	public void MultipleComplexAttackers() {
		StaticRNG rng = new(Array.Empty<int>());
		TestPlayerActor a0a = new();
		TestPlayerActor a1a = new();
		TestPlayerActor a2a = new();
		TestPlayerActor d0a = new();
		Player a0 = new(PlayerRole.ATTACKER, 0, a0a);
		Player a1 = new(PlayerRole.ATTACKER, 1, a1a);
		Player a2 = new(PlayerRole.ATTACKER, 2, a2a);
		Player d0 = new(PlayerRole.DEFENDER, 0, d0a);
		using GameContext game = new(new[] { a0, a1, a2, d0 }, rng, TestGameHooks.CreateDefaultHooks());

		game.board[L1].cards.AddRange(new[] { new Card(BREAK, DATA), new Card(SEVEN, CHOICE), new Card(WILD, VOID), new Card(THREE, KIN),  new Card(THREE, CHOICE) });
		game.board[L2].cards.AddRange(new[] { new Card(SEVEN, FORM), new Card(FIVE, KIN), new Card(TWO, KIN), new Card(TRAP, VOID), new Card(TWO, CHAOS) });
		game.board[L3].cards.AddRange(new[] { new Card(TWO, VOID), new Card(FIVE, KIN), new Card(TRAP, FORM) { revealed = true } });

		game.board[A1].cards.AddRange(new[] { new Card(EIGHT, CHOICE), new Card(TRAP, CHAOS) });
		game.board[A2].cards.AddRange(new[] { new Card(EIGHT, KIN), new Card(EIGHT, FORM) });

		game.board[D1].cards.AddRange(new[] { new Card(FOUR, DATA) });
		game.board[D2].cards.AddRange(new[] { new Card(FOUR, VOID) });

		a1.Hand.Add(new Card(BREAK, CHOICE));
		a2.Hand.Add(new Card(BREAK, KIN));
		game.TurnCounter = 35;
		a0a.currentAction = new ActionSet(ActionType.DRAW, 3);
		a1a.currentAction = new ActionSet(ActionType.FACEUP_PLAY, new NativeCardSpecification(BREAK, null), 2);
		a2a.currentAction = new ActionSet(ActionType.FACEUP_PLAY, new NativeCardSpecification(BREAK, null), 1);

		PlayerRole? victor = game.Tick();
		Assert.AreEqual(null, victor);
		Assert.AreEqual(36, game.TurnCounter);
		Assert.AreEqual(new CardList(new[] { new Card(BREAK, DATA), new Card(SEVEN, CHOICE) }), game.board[L1].cards, Comparers.CardList);
		Assert.AreEqual(new CardList(new[] { new Card(SEVEN, FORM), new Card(FIVE, KIN) }), game.board[L2].cards, Comparers.CardList);
		Assert.AreEqual(new CardList(new[] { new Card(TWO, VOID), new Card(FIVE, KIN) { revealed = true } }), game.board[L3].cards, Comparers.CardList);

		Assert.AreEqual(new CardList(), game.board[A1].cards, Comparers.CardList);
		Assert.AreEqual(new CardList(), game.board[A2].cards, Comparers.CardList);
		Assert.AreEqual(new CardList(), game.board[D1].cards, Comparers.CardList);
		Assert.AreEqual(new CardList(), game.board[D2].cards, Comparers.CardList);

		Assert.AreEqual(new CardList(new[] {
			/* -- first combat -- */
			/* attacker stack: */
			new Card(EIGHT, KIN) { revealed = true },
			new Card(EIGHT, FORM) { revealed = true },
			new Card(BREAK, CHOICE) { revealed = true },
			/* defender stack: */
			new Card(FOUR, VOID) { revealed = true },

			/* -- second combat -- */
			/* trapped: */
			new Card(FOUR, DATA) { revealed = true },
			/* attacker stack: */
			new Card(EIGHT, CHOICE) { revealed = true },
			new Card(TRAP, CHAOS) { revealed = true },
			new Card(BREAK, KIN) { revealed = true },
		}), game.board[XA].cards, Comparers.CardList);

		Assert.AreEqual(new CardList(new[] { new Card(TRAP, FORM) } ), a0.Hand, Comparers.CardList);
		Assert.AreEqual(new CardList(new[] { new Card(TWO, CHAOS), new Card(TRAP, VOID), new Card(TWO, KIN) } ), a1.Hand, Comparers.CardList);
		Assert.AreEqual(new CardList(new[] { new Card(THREE, CHOICE), new Card(THREE, KIN), new Card(WILD, VOID)  } ), a2.Hand, Comparers.CardList);

		Assert.AreEqual(new[] {
			new TurnLog(35, new[] {
				new ActionLog(
					new PlayerID(PlayerRole.ATTACKER, 0),
					new ResolvedActionSet(ActionType.DRAW, 3),
					CardMoveLog.SingleMove(new CardID(TRAP, FORM), new PlayerID(PlayerRole.ATTACKER, 0))
				),
				new ActionLog(
					new PlayerID(PlayerRole.ATTACKER, 1),
					new ResolvedActionSet(ActionType.FACEUP_PLAY, new CardID(BREAK, CHOICE), 2),
					CardMoveLog.SingleMove(new CardID(BREAK, CHOICE), A2),
					new CombatLog(2,
						new[] { new CardID(EIGHT, KIN), new CardID(EIGHT, FORM), new CardID(BREAK, CHOICE) },
						new[] { new CardID(FOUR, VOID) },
						ImmutableList<CombatSpecialLog>.Empty,
						4, 2, 4,
						new[] {
							new CardMoveLog(new[] { new CardID(EIGHT, KIN), new CardID(EIGHT, FORM), new CardID(BREAK, CHOICE) }, XA),
							new CardMoveLog(new[] { new CardID(FOUR, VOID) }, XA),
							new CardMoveLog(new[] { CardID.Unknown, CardID.Unknown, CardID.Unknown }, new PlayerID(PlayerRole.ATTACKER, 1))
						}
					)
				),
				new ActionLog(
					new PlayerID(PlayerRole.ATTACKER, 2),
					new ResolvedActionSet(ActionType.FACEUP_PLAY, new CardID(BREAK, KIN), 1),
					CardMoveLog.SingleMove(new CardID(BREAK, KIN), A1),
					new CombatLog(1,
						new[] { new CardID(EIGHT, CHOICE), new CardID(TRAP, CHAOS), new CardID(BREAK, KIN) },
						new[] { new CardID(FOUR, DATA) },
						new[] { new CombatSpecialLog(SpecialType.TRAP, PlayerRole.ATTACKER, CardMoveLog.SingleMove(new CardID(FOUR, DATA), XA)) },
						3, 0, 3,
						new[] {
							new CardMoveLog(new[] { new CardID(EIGHT, CHOICE), new CardID(TRAP, CHAOS), new CardID(BREAK, KIN) }, XA),
							new CardMoveLog(new[] { CardID.Unknown, CardID.Unknown, CardID.Unknown }, new PlayerID(PlayerRole.ATTACKER, 2))
						}
					)
				)
			})
		},  game.FullBinlog, Comparers.TurnLogs);
	}
}
