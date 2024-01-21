using Fayti1703.CommonLib.Enumeration;
using JetBrains.Annotations;

namespace BinaryMatrix.Engine;
using static CellName;

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public enum CellName {
	A0, A1, A2, A3, A4, A5, DA,
	D0, D1, D2, D3, D4, D5, XA,
	L0, L1, L2, L3, L4, L5,
	X0, X1, X2, X3, X4, X5
}

public sealed class Cell : IDisposable {
	public readonly CardList cards;
	public readonly CellName name;

	private bool _revealed = false;

	internal Cell(CellName name) {
		this.name = name;
		this.cards = new CardList(name switch {
			>= L0 and <= L5 => 13,
			_ => 0
		});
	}

	public bool Revealed {
		get => this.name switch {
			>= X0 and <= X5 => true,
			XA => true,
			>= A0 and <= A5 or >= D0 and <= D5 => this._revealed,
			_ => false
		};
		set {
			if(this.name is not (>= A0 and <= A5 or >= D0 and <= D5))
				throw new InvalidOperationException($"The cell {this.name} does not have a dynamic revealed state.");

			this._revealed = value;
		}
	}

	public CellName? AssociatedDiscard => this.name switch {
		L0 => X0,
		L1 => X1,
		L2 => X2,
		L3 => X3,
		L4 => X4,
		L5 => X5,
		DA => XA,
		_ => null
	};

	public void Dispose() {
		this.cards.Dispose();
	}
}

/* Don't try to use `default(Lane)`. */
public struct Lane {
	public readonly Cell attackerStack;
	public readonly Cell defenderStack;
	public readonly Cell laneDeck;
	public readonly Cell discardPile;

	internal Lane(
		Cell attackerStack,
		Cell defenderStack,
		Cell laneDeck,
		Cell discardPile
	) {
		this.attackerStack = attackerStack;
		this.defenderStack = defenderStack;
		this.laneDeck = laneDeck;
		this.discardPile = discardPile;
	}
}

public sealed class GameBoard : IDisposable {
	private readonly IReadOnlyList<Cell> cells;

	public GameBoard() {
		this.cells = Enum.GetValues<CellName>().Select(x => new Cell(x)).ToList();
	}

	public Cell GetCell(CellName name) {
		return this.cells[(int) name];
	}

	public Lane GetLane(int laneNo) {
		if(laneNo < 0 || laneNo > 5)
			throw new IndexOutOfRangeException("Lane index out of bounds, should be [0;5]");
		CellName a = A0 + laneNo;
		CellName d = D0 + laneNo;
		CellName l = L0 + laneNo;
		CellName x = X0 + laneNo;

		return new Lane(this[a], this[d], this[l], this[x]);
	}

	public Cell this[CellName name] => this.GetCell(name);
	public Lane this[int laneNo] => this.GetLane(laneNo);

	public GameBoard Copy() {
		GameBoard newBoard = new();

		foreach((int index, Cell cell) in this.cells.WithIndex()) {
			newBoard.cells[index].cards.AddAll(cell.cards);
		}

		return newBoard;
	}

	public void Dispose() {
		foreach(Cell cell in this.cells) {
			cell.Dispose();
		}
	}

	public void Clear() {
		foreach(Cell cell in this.cells) {
			cell.cards.Clear();
		}
	}
}
