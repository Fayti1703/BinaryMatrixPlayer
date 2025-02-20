using System;
using System.Collections.Generic;
using System.Linq;
using Fayti1703.CommonLib.Enumeration;

namespace BinaryMatrix.Engine.Tests;

[TestClass]
public class ContextTests {
	[TestMethod]
	public void BasicSetupTest() {
		using GameContext context = new([], new SFC32RNG(0xbeef5eed, 0xbeef5eed, 0xbeef5eed), new GameHooks {
			PreGamePrep = GameHooks.StandardPreGamePrep,
			GetActions = (_, _) => throw new InvalidOperationException("This method should not be called"),
			PreTurn = _ => throw new InvalidOperationException("This method should not be called"),
			PostTurn = _ => throw new InvalidOperationException("This method should not be called")
		});

		context.Setup();
		GameBoard expectedBoard = new();
		string[][] expectedCards = [
			["3+","3%","3&","*+",">^","4!","2&","@+","?+","3^","2^","8#","?&"],
			["6%","9+","@#","a%","@%","6#","9#","6^","7+","2%","8!","@^","@!"],
			["7^","a!",">&","2!",">#","*%","7!","5+","7&","a+","a^","a#","9%"],
			[">+","7%","?^","5&","6!","?#","8^","4&","4+","5#","?%","6+",">!"],
			["@&","5!","6&","5^","8+","a&","3#","*&",">%","*!","4%","4^","7#"],
			["8%","5%","9!","4#","9&","*^","2#","2+","*#","?!","9^","3!","8&"]
		];
		foreach((int index, string[] cards) in expectedCards.WithIndex()) {
			new CardList(cards.Select(x => new CardID(
				CardID.ValueFromSymbol(x[0])!.Value,
				CardID.AxiomFromSymbol(x[1])!.Value
			)).Select(x => new Card(x))).MoveAllTo(expectedBoard[CellName.L0 + index].cards);
		}
		expectedBoard[CellName.L3].cards.Last().Value.revealed = true;
		expectedBoard[CellName.L4].cards.Last().Value.revealed = true;
		expectedBoard[CellName.L5].cards.Last().Value.revealed = true;
		Assert.AreEqual(expectedBoard, context.board, GameBoardComparer.CreateDefault());
	}

	[TestMethod]
	public void DoubleSetupTest() {
		using GameContext context = new([], new SFC32RNG(0xbeef5eed, 0xbeef5eed, 0xbeef5eed), new GameHooks {
			PreGamePrep = GameHooks.StandardPreGamePrep,
			GetActions = (_, _) => throw new InvalidOperationException("This method should not be called"),
			PreTurn = _ => throw new InvalidOperationException("This method should not be called"),
			PostTurn = _ => throw new InvalidOperationException("This method should not be called")
		});

		context.Setup();
		context.Setup();
		GameBoard expectedBoard = new();
		string[][] expectedCards = [
			["9&","3!","a^","4!","3%","?+","6^","a%","9^","*!","5%","6#","8+"],
			[">^","*+","a+","7+","@&","a!","3#","2^","2%","8^","2+","*%","@^"],
			["?!","3^","*&","2#","8%","6&","?#","4&","7&","7#","9!","@!","4+"],
			[">%","2!","5&","a#","5+","4#","a&","*#","8&","*^","@+","4^","6%"],
			["?^","3+","5^","4%",">!","6+","9#","7^","?%","9%","7%","8!",">#"],
			[">&","5#",">+","5!","@%","6!","?&","3&","7!","8#","@#","9+","2&"]
		];
		foreach((int index, string[] cards) in expectedCards.WithIndex()) {
			new CardList(cards.Select(x => new CardID(
				CardID.ValueFromSymbol(x[0])!.Value,
				CardID.AxiomFromSymbol(x[1])!.Value
			)).Select(x => new Card(x))).MoveAllTo(expectedBoard[CellName.L0 + index].cards);
		}
		expectedBoard[CellName.L3].cards.Last().Value.revealed = true;
		expectedBoard[CellName.L4].cards.Last().Value.revealed = true;
		expectedBoard[CellName.L5].cards.Last().Value.revealed = true;
		Assert.AreEqual(expectedBoard, context.board, GameBoardComparer.CreateDefault());
	}

	[TestMethod]
	public void SetupClearsPlayerHandsTest() {
		using GameContext context = new(
			[ new Player(PlayerRole.ATTACKER, 0, new TestPlayerActor()), new Player(PlayerRole.DEFENDER, 0, new TestPlayerActor()) ],
			new SFC32RNG(0xbeef5eed, 0xbeef5eed, 0xbeef5eed),
			new GameHooks {
				PreGamePrep = GameHooks.StandardPreGamePrep,
				GetActions = (_, _) => throw new InvalidOperationException("This method should not be called"),
				PreTurn = _ => throw new InvalidOperationException("This method should not be called"),
				PostTurn = _ => throw new InvalidOperationException("This method should not be called")
			}
		);
		context.Attackers[0].Hand.AddRange([ new Card(Value.SIX, Axiom.CHAOS) ]);
		context.Defenders[0].Hand.AddRange([ new Card(Value.EIGHT, Axiom.DATA) ]);

		context.Setup();
		CardListComparer handComparer = new(EqualityComparer<Card>.Default);
		Assert.AreEqual(context.Attackers[0].Hand, new CardList(), handComparer);
		Assert.AreEqual(context.Defenders[0].Hand, new CardList(), handComparer);
	}
}
