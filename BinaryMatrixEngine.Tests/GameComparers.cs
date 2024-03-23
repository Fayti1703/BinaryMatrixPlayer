using System;
using System.Collections.Generic;
using System.Linq;
using Fayti1703.CommonLib.Enumeration;

namespace BinaryMatrix.Engine.Tests;

public class GameBoardComparer : IEqualityComparer<GameBoard> {
	private IEqualityComparer<Cell> cellComparer;

	public GameBoardComparer(IEqualityComparer<Cell> cellComparer) {
		this.cellComparer = cellComparer;
	}

	public bool Equals(GameBoard? x, GameBoard? y) {
		if(x == null || y == null) return x == null && y == null;
		return Enum.GetValues<CellName>().All(cellName =>
				this.cellComparer.Equals(x[cellName], y[cellName])
		);
	}

	public int GetHashCode(GameBoard obj) {
		return Enum.GetValues<CellName>().Select(cellName =>
			this.cellComparer.GetHashCode(obj[cellName])
		).Aggregate(1, HashCode.Combine);
	}

	public static GameBoardComparer CreateDefault() =>
		new(new CellComparer(new CardListComparer(new StrictCardComparer())))
	;
}

public class CellComparer : IEqualityComparer<Cell> {
	private IEqualityComparer<CardList> cardsComparerer;

	public CellComparer(IEqualityComparer<CardList> cardsComparerer) {
		this.cardsComparerer = cardsComparerer;
	}

	public bool Equals(Cell? x, Cell? y) {
		if(x == null || y == null) return x == null && y == null;
		return
			x.name == y.name &&
			x.Revealed == y.Revealed &&
			this.cardsComparerer.Equals(x.cards, y.cards)
		;
	}

	public int GetHashCode(Cell obj) {
		return HashCode.Combine(obj.cards, (int) obj.name);
	}
}

public class CardListComparer : IEqualityComparer<CardList> {
	private readonly IEqualityComparer<Card> cardComparer;

	public CardListComparer(IEqualityComparer<Card> cardComparer) {
		this.cardComparer = cardComparer;
	}

	public bool Equals(CardList? x, CardList? y) {
		if((x?.Count ?? 0) != (y?.Count ?? 0)) return false;
		if(x == null || y == null) return true;
		return x.WithIndex().All(pair => this.cardComparer.Equals(pair.value, y[pair.index]));
	}

	public int GetHashCode(CardList obj) =>
		obj.Select(this.cardComparer.GetHashCode).Aggregate(1, HashCode.Combine)
	;
}

/* By default, two `Card`s are considered the same `Card` even if their `revealed` state is different.
 * For test-cases, we care about the `revealed` state; so we must override the equality.
 */
public class StrictCardComparer : IEqualityComparer<Card> {
	public bool Equals(Card x, Card y) {
		return x.ID == y.ID && x.revealed == y.revealed;
	}

	public int GetHashCode(Card obj) {
		return HashCode.Combine(obj.ID, obj.revealed);
	}
}

public class CombatLogComparer : IEqualityComparer<CombatLog> {
	private readonly IEqualityComparer<IReadOnlyList<CombatSpecialLog>> specialsComparer;
	private readonly IEqualityComparer<IReadOnlyList<CardMoveLog>> resultsComparer;
	private readonly ElementwiseComparer<CardID> cardIDsComparer;

	public CombatLogComparer(
		IEqualityComparer<IReadOnlyList<CombatSpecialLog>> specialsComparer,
		IEqualityComparer<IReadOnlyList<CardMoveLog>> resultsComparer
	) {
		this.specialsComparer = specialsComparer;
		this.resultsComparer = resultsComparer;
		this.cardIDsComparer = new ElementwiseComparer<CardID>(EqualityComparer<CardID>.Default);
	}
	
	public static CombatLogComparer CreateDefault() {
		IEqualityComparer<IReadOnlyList<CardMoveLog>> cardMoveLogComparer = new ElementwiseComparer<CardMoveLog>(new CardMoveLogComparer());
		return new CombatLogComparer(
			new ElementwiseComparer<CombatSpecialLog>(new CombatSpecialLogComparer(cardMoveLogComparer)),
			cardMoveLogComparer
		);
	}

	public bool Equals(CombatLog x, CombatLog y) {
		if(x.inLane != y.inLane) return false;
		if(!this.cardIDsComparer.Equals(x.initialAS, y.initialAS)) return false;
		if(!this.cardIDsComparer.Equals(x.initialDS, y.initialDS)) return false;
		if(!this.specialsComparer.Equals(x.specials, y.specials)) return false;
		if(x.attackerPower != y.attackerPower) return false;
		if(x.defenderPower != y.defenderPower) return false;
		if(x.damage != y.damage) return false;
		if(!this.resultsComparer.Equals(x.results, y.results)) return false;
		if(x.victorDeclared != y.victorDeclared) return false;
		return true;
	}

	public int GetHashCode(CombatLog obj) {
		HashCode hashCode = new();
		hashCode.Add(obj.inLane);
		hashCode.Add(this.cardIDsComparer.GetHashCode(obj.initialAS));
		hashCode.Add(this.cardIDsComparer.GetHashCode(obj.initialDS));
		hashCode.Add(this.specialsComparer.GetHashCode(obj.specials));
		hashCode.Add(obj.attackerPower);
		hashCode.Add(obj.defenderPower);
		hashCode.Add(obj.damage);
		hashCode.Add(this.resultsComparer.GetHashCode(obj.results));
		hashCode.Add(obj.victorDeclared);
		return hashCode.ToHashCode();
	}
}

public class CombatSpecialLogComparer : IEqualityComparer<CombatSpecialLog> {
	private readonly IEqualityComparer<IReadOnlyList<CardMoveLog>> resultsComparer;

	public CombatSpecialLogComparer(IEqualityComparer<IReadOnlyList<CardMoveLog>> resultsComparer) {
		this.resultsComparer = resultsComparer;
	}

	public bool Equals(CombatSpecialLog x, CombatSpecialLog y) =>
		x.type == y.type && x.role == y.role && this.resultsComparer.Equals(x.results, y.results);

	public int GetHashCode(CombatSpecialLog obj) {
		return HashCode.Combine((int) obj.type, (int) obj.role, this.resultsComparer.GetHashCode(obj.results));
	}
}

public class CardMoveLogComparer : IEqualityComparer<CardMoveLog> {
	private readonly ElementwiseComparer<CardID> cardIDsComparer = new(EqualityComparer<CardID>.Default);

	public bool Equals(CardMoveLog x, CardMoveLog y) => this.cardIDsComparer.Equals(x.cards, y.cards) && x.dest.Equals(y.dest);

	public int GetHashCode(CardMoveLog obj) {
		return HashCode.Combine(this.cardIDsComparer.GetHashCode(obj.cards), obj.dest);
	}
}

public class ElementwiseComparer<T> : IEqualityComparer<IReadOnlyList<T>> {
	private readonly IEqualityComparer<T> elementComparer;

	public ElementwiseComparer(IEqualityComparer<T> elementComparer) {
		this.elementComparer = elementComparer;
	}

	public bool Equals(IReadOnlyList<T>? x, IReadOnlyList<T>? y) {
		if((x?.Count ?? 0) != (y?.Count ?? 0)) return false;
		if(x == null || y == null) return true;
		foreach((int index, T value) in x.WithIndex()) {
			if(!this.elementComparer.Equals(value, y[index]))
				return false;
		}
		return true;
	}

	public int GetHashCode(IReadOnlyList<T> obj) {
		return obj.Select(this.elementComparer.GetHashCode!).Aggregate(1, HashCode.Combine);
	}
}
