using System.Collections.Immutable;
using System.Diagnostics;
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
	public static void ExecutePlayerTurn(GameContext context, Player player, ActionSet action, HashSet<Cell> drawnDecks, out ActionLog log) {
		OperationError error = ExecutePlayerAction(context, player, action, drawnDecks, out log);
		if(error != OperationError.NONE) {
			player.actor.ReportOperationError(error);
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

	public static OperationError ExecutePlayerAction(GameContext context, Player player, ActionSet action, HashSet<Cell> drawnDecks, out ActionLog log) {
		log = new ActionLog(player.ID, new ResolvedActionSet(ActionType.NONE), null);
		switch(action.type) {
			case ActionType.NONE:
				log = new ActionLog(log.whoDidThis, new ResolvedActionSet(ActionType.NONE, explicitNone: true), null);
				return OperationError.NO_ACTION;
			case ActionType.DRAW: {
				if(action.lane == ActionSet.LANE_A) {
					if(player.Role == PlayerRole.DEFENDER) return OperationError.WRONG_ROLE;
					bool drawOK = TryDraw(context, context.board[DA], player, out CardID drawnCard);
					if(!drawOK) return OperationError.EMPTY_STACK;
					log = new ActionLog(
						log.whoDidThis,
						new ResolvedActionSet(ActionType.DRAW, ActionSet.LANE_A),
						CardMoveLog.SingleMove(drawnCard, log.whoDidThis)
					);
				} else {
					Lane lane = context.board.GetLane(action.lane);
					if(player.Role == PlayerRole.ATTACKER) {
						if(lane.defenderStack.cards.Count != 0) return OperationError.LANE_BLOCKED;
						if(!drawnDecks.Add(lane.laneDeck)) return OperationError.DOUBLE_DRAW;
					}

					bool drawOK = TryDraw(context, lane.laneDeck, player, out CardID drawnCard);
					if(!drawOK) {
						if(player.Role == PlayerRole.DEFENDER) return OperationError.EMPTY_STACK;
						context.SetVictor(PlayerRole.ATTACKER);
						log = new ActionLog(
							log.whoDidThis,
							new ResolvedActionSet(ActionType.DRAW, action.lane),
							ImmutableList<CardMoveLog>.Empty
						);
					}

					log = new ActionLog(
						log.whoDidThis,
						new ResolvedActionSet(ActionType.DRAW, action.lane),
						CardMoveLog.SingleMove(drawnCard, log.whoDidThis)
					);
				}
			} break;
			case ActionType.PLAY:
			case ActionType.FACEUP_PLAY: {
				if(action.lane == ActionSet.LANE_A) return OperationError.UNKNOWN_LANE;
				Indexed<Card>? result = ResolveCard(action.card!, player);
				if(result == null) return OperationError.UNKNOWN_CARD;

				(int index, Card card) = result.Value;
				Lane lane = context.board.GetLane(action.lane);
				Cell stack = player.Role == PlayerRole.ATTACKER ? lane.attackerStack : lane.defenderStack;
				if(stack.Revealed && action.type == ActionType.PLAY) return OperationError.WRONG_FACING;

				bool stackEmpty = stack.cards.Count == 0;
				if(card.Value == Value.BREAK) {
					if(stackEmpty) return OperationError.BREAK_ON_EMPTY;
					if(player.Role == PlayerRole.DEFENDER && stack.Revealed && stack.cards.Any(x => x.Value == Value.BREAK))
						return OperationError.DOUBLE_FACEUP_BREAK_IN_DEFENSE;
				}

				ref Card newCard = ref stack.cards.Add(player.Hand.Take(index)!.Value);

				CardID logCard = action.type == ActionType.FACEUP_PLAY ? newCard.ID : CardID.Unknown;
				ResolvedActionSet resolvedAction = new(action.type, logCard, action.lane);
				IReadOnlyList<CardMoveLog> moveLog = CardMoveLog.SingleMove(logCard, stack.name);

				CombatLog? optCombatLog = default;
				if(action.type == ActionType.FACEUP_PLAY) {
					newCard.revealed = true;
					switch(newCard.Value) {
						case Value.BREAK:
						case Value.BOUNCE when stackEmpty && player.Role == PlayerRole.ATTACKER:
							ResolveCombat(context, lane, player, out CombatLog combatLog);
							optCombatLog = combatLog;
							break;
					}
				}

				log = new ActionLog(
					log.whoDidThis,
					resolvedAction,
					moveLog,
					optCombatLog
				);
			} break;
			case ActionType.DISCARD: {
				Indexed<Card>? result = ResolveCard(action.card!, player);
				if(result == null) return OperationError.UNKNOWN_CARD;

				if(action.lane == ActionSet.LANE_A) {
					if(player.Role == PlayerRole.DEFENDER) return OperationError.WRONG_ROLE;
					if(context.board[DA].cards.Count == 0 && context.board[XA].cards.Count == 0)
						return OperationError.EMPTY_STACK;
					ref Card discarded = ref context.board[XA].cards.Add(player.Hand.Take(result.Value.index)!.Value);
					discarded.revealed = true;
					bool drawOK = TryDraw(context, context.board[DA], player, out CardID firstCard);
					Debug.Assert(drawOK);
					drawOK = TryDraw(context, context.board[DA], player, out CardID secondCard);
					Debug.Assert(drawOK);
					log = new ActionLog(
						log.whoDidThis,
						new ResolvedActionSet(ActionType.DISCARD, discarded.ID, ActionSet.LANE_A),
						new[] {
							new CardMoveLog(new[] { discarded.ID }, XA),
							new CardMoveLog(new[] { firstCard, secondCard }, log.whoDidThis)
						}
					);
				} else {
					if(player.Role == PlayerRole.ATTACKER) return OperationError.WRONG_ROLE;
					ref Card discarded = ref context.board[X0 + action.lane].cards.Add(player.Hand.Take(result.Value.index)!.Value);
					discarded.revealed = true;
					log = new ActionLog(
						log.whoDidThis,
						new ResolvedActionSet(ActionType.DISCARD, discarded.ID, action.lane),
						CardMoveLog.SingleMove(discarded.ID, X0 + action.lane)
					);
				}
			} break;
			case ActionType.COMBAT: {
				if(action.lane == ActionSet.LANE_A) return OperationError.UNKNOWN_LANE;
				if(player.Role == PlayerRole.DEFENDER) return OperationError.WRONG_ROLE;
				Lane lane = context.board.GetLane(action.lane);
				if(lane.attackerStack.cards.Count == 0) return OperationError.EMPTY_STACK;
				ResolveCombat(context, lane, player, out CombatLog combatLog);
				log = new ActionLog(
					log.whoDidThis,
					new ResolvedActionSet(ActionType.COMBAT, action.lane),
					null,
					combatLog
				);
			} break;
		}

		return OperationError.NONE;
	}

	private static void ResolveTraps(GameContext context, Cell scanDeck, Cell targetDeck, Cell discardPile, out IReadOnlyList<CardMoveLog>? results) {
		CardMoveLogBuilder log = default;
		results = null;
		if(scanDeck.Revealed) return;
		foreach(ref Card card in scanDeck.cards) {
			if(card.revealed)
				continue;
			card.revealed = true;
			if(card.Value != Value.TRAP || targetDeck.cards.Count <= 0)
				continue;
			Card trapped = targetDeck.cards.TakeLast()!.Value;
			trapped.revealed = true;
			log.Add(trapped.ID, discardPile.name);
			discardPile.cards.Add(trapped);
		}

		scanDeck.Revealed = true;
		results = log.FinishOptional();
	}

	private static void ResolveCombat(GameContext context, Lane lane, Player player, out CombatLog combatLog) {
		CombatLogBuilder log = new() {
			inLane = lane.laneNo,
			initialAS = lane.attackerStack.cards.Select(x => x.ID).ToImmutableList(),
			initialDS = lane.defenderStack.cards.Select(x => x.ID).ToImmutableList()
		};
		bool defenseFirst = player.Role == PlayerRole.DEFENDER;
		if(defenseFirst) {
			ResolveTraps(context, lane.defenderStack, lane.attackerStack, lane.discardPile, out IReadOnlyList<CardMoveLog>? defTraps);
			if(defTraps != null) log.AddSpecialLog(new CombatSpecialLog(SpecialType.TRAP, PlayerRole.DEFENDER, defTraps));
			ResolveTraps(context, lane.attackerStack, lane.defenderStack, context.board[XA], out IReadOnlyList<CardMoveLog>? atkTraps);
			if(atkTraps != null) log.AddSpecialLog(new CombatSpecialLog(SpecialType.TRAP, PlayerRole.ATTACKER, atkTraps));
		} else {
			ResolveTraps(context, lane.attackerStack, lane.defenderStack, context.board[XA], out IReadOnlyList<CardMoveLog>? atkTraps);
			if(atkTraps != null) log.AddSpecialLog(new CombatSpecialLog(SpecialType.TRAP, PlayerRole.ATTACKER, atkTraps));
			ResolveTraps(context, lane.defenderStack, lane.attackerStack, lane.discardPile, out IReadOnlyList<CardMoveLog>? defTraps);
			if(defTraps != null) log.AddSpecialLog(new CombatSpecialLog(SpecialType.TRAP, PlayerRole.DEFENDER, defTraps));
		}

		bool hasBounces =
			lane.attackerStack.cards.Any(x => x.Value == Value.BOUNCE) ||
			lane.defenderStack.cards.Any(x => x.Value == Value.BOUNCE)
		;

		int apow; int dpow;

		if(hasBounces) {
			apow = 0; dpow = 0;
		} else {
			apow = CalculateStackPower(lane.attackerStack);
			dpow = CalculateStackPower(lane.defenderStack);
		}

		log.attackerPower = apow;
		log.defenderPower = dpow;

		if(hasBounces) {
			if(defenseFirst) {
				DiscardBounces(context, lane.defenderStack, context.board[XA], out IReadOnlyList<CardMoveLog>? defBounces);
				if(defBounces != null) log.AddSpecialLog(new CombatSpecialLog(SpecialType.BOUNCE, PlayerRole.DEFENDER, defBounces));
				DiscardBounces(context, lane.attackerStack, lane.discardPile, out IReadOnlyList<CardMoveLog>? atkBounces);
				if(atkBounces != null) log.AddSpecialLog(new CombatSpecialLog(SpecialType.BOUNCE, PlayerRole.DEFENDER, atkBounces));
			} else {
				DiscardBounces(context, lane.attackerStack, lane.discardPile, out IReadOnlyList<CardMoveLog>? atkBounces);
				if(atkBounces != null) log.AddSpecialLog(new CombatSpecialLog(SpecialType.BOUNCE, PlayerRole.DEFENDER, atkBounces));
				DiscardBounces(context, lane.defenderStack, context.board[XA], out IReadOnlyList<CardMoveLog>? defBounces);
				if(defBounces != null) log.AddSpecialLog(new CombatSpecialLog(SpecialType.BOUNCE, PlayerRole.DEFENDER, defBounces));
			}
		}

		if(apow == 0 && dpow == 0) {
			/* No combat victor. ("Bounce") */
			log.results.Add(lane.attackerStack.cards.Select(x => x.ID), XA);
			lane.attackerStack.cards.MoveAllTo(context.board[XA].cards);
			lane.attackerStack.Revealed = false;
			if(lane.defenderStack.cards.Count == 0)
				lane.defenderStack.Revealed = false;
			combatLog = log.Finish();
			return;
		}

		if(dpow > apow) {
			/* Defender victory. */
			log.damage = -1;
			log.results.Add(lane.attackerStack.cards.Select(x => x.ID), lane.discardPile.name);
			lane.attackerStack.cards.MoveAllTo(lane.discardPile.cards);
			lane.attackerStack.Revealed = false;
			if(lane.defenderStack.cards.Count == 0) /* shouldn't be possible, but best to be prudent */
				lane.defenderStack.Revealed = false;
			combatLog = log.Finish();
			return;
		}

		Debug.Assert(apow >= dpow);
		bool hasBreak = lane.attackerStack.cards.Any(x => x.Value == Value.BREAK) || lane.defenderStack.cards.Any(x => x.Value == Value.BREAK);
		log.results.Add(lane.attackerStack.cards.Select(x => x.ID), XA);
		log.results.EndCurrentSet();
		lane.attackerStack.cards.MoveAllTo(context.board[XA].cards);
		int damage = hasBreak ? Math.Max(lane.defenderStack.cards.Count, apow) : apow - dpow + 1;
		log.damage = damage;

		while(damage > 0) {
			Card? card = lane.defenderStack.cards.TakeLast();
			if(card == null) break;
			log.results.Add(card.Value.ID, XA);
			context.board[XA].cards.Add(card.Value);
			damage--;
		}

		if(damage > 0) {
			if(player.Role == PlayerRole.ATTACKER) {
				PlayerID attackerID = player.ID;
				while(damage > 0) {
					if(!TryDraw(context, lane.laneDeck, player, out CardID logCard)) {
						context.SetVictor(PlayerRole.ATTACKER);
						log.victorDeclared = true;
						break;
					}
					log.results.Add(logCard, attackerID);
					damage--;
				}
			} else {
				while(damage > 0) {
					foreach((int index, Player attacker) in context.Attackers.WithIndex()) {
						if(damage == 0) break;
						if(!TryDraw(context, lane.laneDeck, attacker, out CardID logCard)) {
							context.SetVictor(PlayerRole.ATTACKER);
							log.victorDeclared = true;
							goto endLp;
						}
						log.results.Add(logCard, new PlayerID(PlayerRole.ATTACKER, index));
						damage--;
					}
				}
				endLp:;
			}
		}

		lane.attackerStack.Revealed = false;
		if(lane.defenderStack.cards.Count == 0)
			lane.defenderStack.Revealed = false;

		combatLog = log.Finish();
	}

	private static void DiscardBounces(GameContext context, Cell scanDeck, Cell discardDeck, out IReadOnlyList<CardMoveLog>? results) {
		CardMoveLogBuilder log = default;
		for(int i = 0; i < scanDeck.cards.Count; i++) {
			if(scanDeck.cards[i].Value != Value.BOUNCE)
				continue;
			Card bounce = scanDeck.cards.Take(i)!.Value;
			discardDeck.cards.Add(bounce);
			log.Add(bounce.ID, discardDeck.name);
			i--;
		}
		results = log.FinishOptional();
	}

	private static int CalculateStackPower(Cell stack) {
		int cardSum = 0;
		int wildCount = 0;
		foreach(Card card in stack.cards) {
			switch(card.Value) {
				case Value.WILD:
					wildCount++;
					break;
				case >= Value.TWO and <= Value.TEN:
					cardSum += (int) card.Value;
					break;
			}
		}

		double power = cardSum == 0 ? 0.0 : Math.Log2(cardSum);
		if(wildCount > 0) power = Math.Floor(power);
		else if(power % 1 != 0.0) power = 0;
		return (int) power + wildCount;
	}

	private static Indexed<Card>? ResolveCard(CardSpecification cardSpec, Player player) {
		int? index = cardSpec.ResolveForPlayer(player);
		if(index == null) return null;
		/* TODO?: Catch some possible errors here, point the finger at CardSpecification? */
		return new Indexed<Card>(index.Value, player.Hand[index.Value]);
	}

	private static bool TryDraw(GameContext context, Cell stack, Player drawingPlayer, out CardID logCard) {
		Debug.Assert(stack.name is >= L0 and <= L5 or DA);
		if(stack.cards.Count == 0) {
			Cell discard = context.board[stack.AssociatedDiscard!.Value];
			logCard = default;
			if(discard.cards.Count == 0) return false;
			using CardList cards = FisherYatesShuffle(context.rng, discard.cards);
			discard.cards.Clear();
			cards.MoveAllTo(stack.cards);
			if(stack.name is >= L3 and <= L5) stack.cards.Last().Apply((ref Card x) => x.revealed = true);
		}

		Card drawn = stack.cards.TakeLast()!.Value;
		logCard = drawn.revealed ? drawn.ID : CardID.Unknown;
		drawn.revealed = false;
		drawingPlayer.Hand.Add(drawn);
		if(stack.name is >= L3 and <= L5) stack.cards.Last().Apply((ref Card x) => x.revealed = true);
		return true;
	}

	public static CardList FisherYatesShuffle(RNG rng, IEnumerable<Card> cards) {
		CardList cardList = new(cards);

		for(int i = cardList.Count - 1; i > 0; i--) {
			int j = rng.Next(i);
			(cardList[i], cardList[j]) = (cardList[j], cardList[i]);
		}

		return cardList;
	}
}
