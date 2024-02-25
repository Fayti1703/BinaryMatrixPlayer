using System;
using System.Collections.Generic;
using System.Linq;
using Fayti1703.CommonLib.Enumeration;

namespace BinaryMatrix.Engine.Tests;

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
