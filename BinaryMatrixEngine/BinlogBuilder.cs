using System.Collections.Immutable;

namespace BinaryMatrix.Engine;

public struct CardMoveLogBuilder {
	private List<CardMoveLog>? results;
	private List<CardID>? currentCards;
	private MoveDestination? currentDestination;

	public void EndCurrentSet() {
		if(this.currentDestination == null) return;
		if(this.currentCards!.Count != 0) {
			this.results ??= new List<CardMoveLog>();
			this.results.Add(new CardMoveLog(
				this.currentCards!,
				this.currentDestination.Value
			));
		}
		this.currentCards = null;
		this.currentDestination = null;
	}

	private void PrepareForInsert(MoveDestination dest) {
		if(this.currentDestination == dest) return;
		this.EndCurrentSet();
		this.currentCards = new List<CardID>();
		this.currentDestination = dest;
	}

	public void Add(CardID card, MoveDestination dest) {
		this.PrepareForInsert(dest);
		this.currentCards!.Add(card);
	}

	public void Add(IEnumerable<CardID> cards, MoveDestination dest) {
		this.PrepareForInsert(dest);
		this.currentCards!.AddRange(cards);
	}

	public IReadOnlyList<CardMoveLog> Finish() {
		this.EndCurrentSet();
		return (IReadOnlyList<CardMoveLog>?) this.results ?? ImmutableList<CardMoveLog>.Empty;
	}

	public IReadOnlyList<CardMoveLog>? FinishOptional() {
		this.EndCurrentSet();
		return this.results;
	}
}

public struct CombatLogBuilder {
	public int inLane;
	public IReadOnlyList<CardID> initialAS;
	public IReadOnlyList<CardID> initialDS;
	private List<CombatSpecialLog>? specials;
	public int attackerPower;
	public int defenderPower;
	public int damage;
	public CardMoveLogBuilder results;
	public bool victorDeclared;

	public void AddSpecialLog(CombatSpecialLog special) {
		this.specials ??= new List<CombatSpecialLog>();
		this.specials.Add(special);
	}

	public CombatLog Finish() {
		return new CombatLog(
			this.inLane,
			this.initialAS,
			this.initialDS,
			(IReadOnlyList<CombatSpecialLog>?) this.specials ?? ImmutableList<CombatSpecialLog>.Empty,
			this.attackerPower,
			this.defenderPower,
			this.damage,
			this.results.Finish(),
			this.victorDeclared
		);
	}

}
