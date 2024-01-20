using System.Diagnostics;
using System.Text.Json;
using Fayti1703.CommonLib.Enumeration;

namespace BinaryMatrix.Engine;
using static CellName;

public enum OperationError {
	NONE,
	NO_ACTION,
	EMPTY_STACK,
	LANE_BLOCKED,
	DOUBLE_DRAW,
	WRONG_FACING,
	BREAK_ON_EMPTY,
	DOUBLE_FACEUP_BREAK_IN_DEFENSE,
	WRONG_ROLE,
	UNKNOWN_CARD,
	UNKNOWN_LANE
}

public static class GameExecution {
	public static void ExecutePlayerTurn(GameContext context, Player player, ActionSet action, HashSet<Cell> drawnDecks) {
		OperationError error = ExecutePlayerAction(context, player, action, drawnDecks);
		if(error != OperationError.NONE) {
			player.ReportOperationError(error);
			player.InvalidOperationCount++;
			if(player.InvalidOperationCount == 2) {
				SuccessiveInvalidOperationSteps(context, player);
			}
		} else {
			player.InvalidOperationCount = 0;
		}
	}

	private static void SuccessiveInvalidOperationSteps(GameContext context, Player player) {
		if(player.Role == PlayerRole.ATTACKER) {
			/* BINLOG: extension here? */
			player.Hand.MoveAllTo(context.board[XA].cards);
		}

		/* BINLOG: extension here? */
		Cell[] discards = { context.board[X0], context.board[X1], context.board[X2], context.board[X3], context.board[X4], context.board[X5] };
		int currentIndex = 0;
		foreach(Card card in player.Hand) {
			discards[currentIndex].cards.Add(card);
			currentIndex++;
			if(currentIndex == discards.Length)
				currentIndex = 0;
		}
		player.Hand.Clear();
	}

	public static OperationError ExecutePlayerAction(GameContext context, Player player, ActionSet action, HashSet<Cell> drawnDecks) {
		switch(action.type) {
			case ActionType.NONE:
				return OperationError.NO_ACTION;
			case ActionType.DRAW: {
				if(action.lane == ActionSet.LANE_A) {
					if(player.Role == PlayerRole.ATTACKER) return OperationError.WRONG_ROLE;
					bool drawOK = TryDraw(context, context.board[DA], player);
					if(!drawOK) return OperationError.EMPTY_STACK;
				} else {
					Lane lane = context.board.GetLane(action.lane);
					if(player.Role == PlayerRole.ATTACKER) {
						if(lane.defenderStack.cards.Count != 0) return OperationError.LANE_BLOCKED;
						if(!drawnDecks.Add(lane.laneDeck)) return OperationError.DOUBLE_DRAW;
					}

					bool drawOK = TryDraw(context, lane.laneDeck, player);
					if(!drawOK) {
						if(player.Role == PlayerRole.DEFENDER) return OperationError.EMPTY_STACK;
						context.SetVictor(PlayerRole.ATTACKER);
					}
				}
			} break;
			case ActionType.PLAY:
			case ActionType.FACEUP_PLAY: {
				if(action.lane == ActionSet.LANE_A) return OperationError.UNKNOWN_LANE;
				Indexed<Card>? result = ResolveCard(action.card, player);
				if(result == null) return OperationError.UNKNOWN_CARD;

				(int index, Card card) = result.Value;
				Lane lane = context.board.GetLane(action.lane);
				Cell stack = player.Role == PlayerRole.ATTACKER ? lane.attackerStack : lane.defenderStack;
				if(stack.Revealed && action.type == ActionType.PLAY) return OperationError.WRONG_FACING;

				bool stackEmpty = stack.cards.Count == 0;
				if(card.value == Value.BREAK) {
					if(stackEmpty) return OperationError.BREAK_ON_EMPTY;
					if(player.Role == PlayerRole.DEFENDER && stack.Revealed && stack.cards.Any(x => x.value == Value.BREAK))
						return OperationError.DOUBLE_FACEUP_BREAK_IN_DEFENSE;
				}

				ref Card newCard = ref stack.cards.Add(player.Hand.Take(index)!.Value);
				if(action.type == ActionType.FACEUP_PLAY) {
					newCard.revealed = true;
					switch(newCard.value) {
						case Value.BREAK:
						case Value.BOUNCE when stackEmpty && player.Role == PlayerRole.ATTACKER:
							ResolveCombat(context, lane, player);
							break;
					}
				}
			} break;
			case ActionType.DISCARD: {
				Indexed<Card>? result = ResolveCard(action.card, player);
				if(result == null) return OperationError.UNKNOWN_CARD;

				if(action.lane == ActionSet.LANE_A) {
					if(player.Role == PlayerRole.DEFENDER) return OperationError.WRONG_ROLE;
					if(context.board[DA].cards.Count == 0 && context.board[XA].cards.Count == 0)
						return OperationError.EMPTY_STACK;
					context.board[XA].cards.Add(player.Hand.Take(result.Value.index)!.Value).revealed = true;
					bool drawOK = TryDraw(context, context.board[DA], player);
					Debug.Assert(drawOK);
					drawOK = TryDraw(context, context.board[DA], player);
					Debug.Assert(drawOK);
				} else {
					if(player.Role == PlayerRole.ATTACKER) return OperationError.WRONG_ROLE;
					context.board[X0 + action.lane].cards.Add(player.Hand.Take(result.Value.index)!.Value).revealed = true;
				}
			} break;
			case ActionType.COMBAT: {
				if(action.lane == ActionSet.LANE_A) return OperationError.UNKNOWN_LANE;
				if(player.Role == PlayerRole.DEFENDER) return OperationError.WRONG_ROLE;
				Lane lane = context.board.GetLane(action.lane);
				if(lane.attackerStack.cards.Count == 0) return OperationError.EMPTY_STACK;
				ResolveCombat(context, lane, player);
			} break;
		}

		return OperationError.NONE;
	}

	private static void ResolveTraps(GameContext context, Cell scanDeck, Cell targetDeck, Cell discardPile) {
		if(scanDeck.Revealed) return;
		foreach(ref Card card in scanDeck.cards) {
			if(card.revealed)
				continue;
			card.revealed = true;
			if(card.value != Value.TRAP || targetDeck.cards.Count <= 0)
				continue;
			Card trapped = targetDeck.cards.TakeLast()!.Value;
			trapped.revealed = true;
			discardPile.cards.Add(trapped);
		}

		scanDeck.Revealed = true;
	}

	private static void ResolveCombat(GameContext context, Lane lane, Player player) {
		bool defenseFirst = player.Role == PlayerRole.DEFENDER;
		if(defenseFirst) {
			ResolveTraps(context, lane.defenderStack, lane.attackerStack, lane.discardPile);
			ResolveTraps(context, lane.attackerStack, lane.defenderStack, context.board[XA]);
		} else {
			ResolveTraps(context, lane.attackerStack, lane.defenderStack, context.board[XA]);
			ResolveTraps(context, lane.defenderStack, lane.attackerStack, lane.discardPile);
		}

		bool hasBounces =
			lane.attackerStack.cards.Any(x => x.value == Value.BOUNCE) ||
			lane.defenderStack.cards.Any(x => x.value == Value.BOUNCE)
		;

		int apow; int dpow;

		if(hasBounces) {
			apow = 0; dpow = 0;
		} else {
			apow = CalculateStackPower(lane.attackerStack);
			dpow = CalculateStackPower(lane.defenderStack);
		}

		if(hasBounces) {
			if(defenseFirst) {
				DiscardBounces(context, lane.defenderStack, context.board[XA]);
				DiscardBounces(context, lane.attackerStack, lane.discardPile);
			} else {
				DiscardBounces(context, lane.attackerStack, lane.discardPile);
				DiscardBounces(context, lane.defenderStack, context.board[XA]);
			}
		}

		if(apow == 0 && dpow == 0) {
			/* No combat victor. ("Bounce") */
			lane.attackerStack.cards.MoveAllTo(context.board[XA].cards);
			lane.attackerStack.Revealed = false;
			if(lane.defenderStack.cards.Count == 0)
				lane.defenderStack.Revealed = false;
			return;
		}

		if(dpow > apow) {
			/* Defender victory. */
			lane.attackerStack.cards.MoveAllTo(lane.discardPile.cards);
			lane.attackerStack.Revealed = false;
			if(lane.defenderStack.cards.Count == 0) /* shouldn't be possible, but best to be prudent */
				lane.defenderStack.Revealed = false;
			return;
		}

		Debug.Assert(apow >= dpow);
		bool hasBreak = lane.attackerStack.cards.Any(x => x.value == Value.BREAK) || lane.defenderStack.cards.Any(x => x.value == Value.BREAK);
		lane.attackerStack.cards.MoveAllTo(context.board[XA].cards);
		int damage = hasBreak ? Math.Max(lane.defenderStack.cards.Count, apow) : apow - dpow + 1;
		/* BINLOG: record damage here */

		while(damage > 0) {
			Card? card = lane.defenderStack.cards.TakeLast();
			if(card == null) break;
			context.board[XA].cards.Add(card.Value);
			damage--;
		}

		if(damage > 0) {
			if(player.Role == PlayerRole.ATTACKER) {
				while(damage > 0) {
					if(!TryDraw(context, lane.laneDeck, player)) {
						context.SetVictor(PlayerRole.ATTACKER);
						break;
					}
					damage--;
				}
			} else {
				while(damage > 0) {
					foreach(Player attacker in context.Attackers) {
						if(damage == 0) break;
						if(!TryDraw(context, lane.laneDeck, attacker)) {
							context.SetVictor(PlayerRole.ATTACKER);
							goto endLp;
						}
						damage--;
					}
				}
				endLp:;
			}
		}

		lane.attackerStack.Revealed = false;
		if(lane.defenderStack.cards.Count == 0)
			lane.defenderStack.Revealed = false;

	}

	private static void DiscardBounces(GameContext context, Cell scanDeck, Cell discardDeck) {
		for(int i = 0; i < scanDeck.cards.Count; i++) {
			if(scanDeck.cards[i].value != Value.BOUNCE)
				continue;
			discardDeck.cards.Add(scanDeck.cards.Take(i)!.Value);
			i--;
		}
	}

	private static int CalculateStackPower(Cell stack) {
		int cardSum = 0;
		int wildCount = 0;
		foreach(Card card in stack.cards) {
			switch(card.value) {
				case Value.WILD:
					wildCount++;
					break;
				case >= Value.TWO and <= Value.TEN:
					cardSum += (int) card.value;
					break;
			}
		}

		double power = cardSum == 0 ? 0.0 : Math.Log2(cardSum);
		if(wildCount > 0) power = Math.Floor(power);
		else if(power % 1 != 0.0) power = 0;
		return (int) power + wildCount;
	}

	private static Indexed<Card>? ResolveCard(CardSpecification cardSpec, Player player) {
		return player.Hand.WithIndex().FirstOrNull(x => cardSpec.Matches(x.value));
	}

	private static bool TryDraw(GameContext context, Cell stack, Player drawingPlayer) {
		Debug.Assert(stack.name is >= L0 and <= L5 or DA);
		if(stack.cards.Count == 0) {
			Cell discard = context.board[stack.AssociatedDiscard!.Value];
			if(discard.cards.Count == 0) return false;
			using CardList cards = FisherYatesShuffle(context.rng, discard.cards);
			discard.cards.Clear();
			cards.MoveAllTo(stack.cards);
			if(stack.name is >= L3 and <= L5) stack.cards.Last().Apply((ref Card x) => x.revealed = true);
		}

		Card drawn = stack.cards.TakeLast()!.Value;
		drawn.revealed = false;
		drawingPlayer.Hand.Add(drawn);
		if(stack.name is >= L3 and <= L5) stack.cards.Last().Apply((ref Card x) => x.revealed = true);
		return true;
	}

	public static CardList FisherYatesShuffle(Random rng, IEnumerable<Card> cards) {
		CardList cardList = new(cards);

		for(int i = cardList.Count - 1; i > 0; i--) {
			int j = rng.Next(i);
			(cardList[i], cardList[j]) = (cardList[j], cardList[i]);
		}

		return cardList;
	}
}
