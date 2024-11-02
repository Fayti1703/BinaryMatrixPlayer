using System.Collections.Generic;
using static BinaryMatrix.Engine.Axiom;
using static BinaryMatrix.Engine.CellName;
using static BinaryMatrix.Engine.Value;
namespace BinaryMatrix.Engine.Tests;

[TestClass]
public class ActionTests {
	[TestMethod]
	public void SimpleTest() {
		using GameContext context = TestUtils.CreateScenarioContext(new SFC32RNG(0xbeef5eed, 0xbeef5eed, 0xbeef5eed, 78));
		TestUtils.SetBoardLanes(context.board, [
			["@&","4&","3%","*+","7#","4!","2&","a^","?^",">#","2^","8#"],
			["3+","5^","?&",">&","*!","6!","*&","6^","6#","2%","8^",">!","@!"],
			["8!","6%","9+","2!","3!","*%","7^","5+","7&","9#","a&","a!","9%"],
			[">+","3&","6&","5%","@%","?#","8&","*#","a#","5!","?+","6+",">^"],
			["@+","4#","a%","5&","8+","7%","3#","a+",">%","*^","4%","4^","7!"],
			["@#","@^","9!","7+","9&","5#","2#","2+","4+","?!","9^","3^","8%"]
		]);
		context.board[DA].cards.Add(new Card(BOUNCE, KIN));
		context.TurnCounter = 1;


		HashSet<CellName> drawnDecks = [];
		OperationError error = GameExecution.ExecutePlayerAction(
			context,
			context.Attackers[0],
			new ActionSet(ActionType.DRAW, ActionSet.LANE_A),
			drawnDecks,
			out ActionLog log
		);
		Assert.AreEqual(0, drawnDecks.Count);
		Assert.AreEqual(OperationError.NONE, error);
		Assert.AreEqual([ new Card(BOUNCE, KIN) ], context.Attackers[0].Hand, Comparers.CardList);
		Assert.AreEqual(new ActionLog(
			whoDidThis: new PlayerID(PlayerRole.ATTACKER, 0),
			resolvedAction: new ResolvedActionSet(ActionType.DRAW, ActionSet.LANE_A),
			moveResults: CardMoveLog.SingleMove(new CardID(BOUNCE, KIN), new PlayerID(PlayerRole.ATTACKER, 0))
		), log, Comparers.ActionLog);

		GameBoard expectedBoard = new();
		TestUtils.SetBoardLanes(expectedBoard, [
			["@&","4&","3%","*+","7#","4!","2&","a^","?^",">#","2^","8#"],
			["3+","5^","?&",">&","*!","6!","*&","6^","6#","2%","8^",">!","@!"],
			["8!","6%","9+","2!","3!","*%","7^","5+","7&","9#","a&","a!","9%"],
			[">+","3&","6&","5%","@%","?#","8&","*#","a#","5!","?+","6+",">^"],
			["@+","4#","a%","5&","8+","7%","3#","a+",">%","*^","4%","4^","7!"],
			["@#","@^","9!","7+","9&","5#","2#","2+","4+","?!","9^","3^","8%"]
		]);
		Assert.AreEqual(expectedBoard, context.board, Comparers.GameBoard);
	}
}
