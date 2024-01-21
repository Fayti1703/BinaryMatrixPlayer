using System.Diagnostics;
using Fayti1703.CommonLib.Enumeration;

namespace BinaryMatrix.Engine;

public static class NativeBinCmdParser {
	public static ActionSet? ParseActionSet(string input) {
		switch(input[0]) {
			case 'd':
			case 'c': {
				int? lane = ParseLane(input[1..]);
				if(lane == null) return null;
				return new ActionSet(input[0] switch {
					'd' => ActionType.DRAW,
					'c' => ActionType.COMBAT,
					_ => throw new UnreachableException()
				}, lane.Value);
			}
			case 'p':
			case 'u':
			case 'x': {
				NativeCardSpecification? card = ParseCard(input[1..], out int nextIndex);
				if(card == null) return null;
				int? lane = ParseLane(input[(1 + nextIndex)..]);
				if(lane == null) return null;
				return new ActionSet(input[0] switch {
					'p' => ActionType.PLAY,
					'u' => ActionType.FACEUP_PLAY,
					'x' => ActionType.DISCARD,
					_ => throw new UnreachableException()
				}, card.Value, lane.Value);
			}
		}

		return null;
	}

	private static int? ParseLane(string input) {
		if(!int.TryParse(input, out int value)) {
			if(input == "a")
				return ActionSet.LANE_A;
			return null;
		}
		if(value < 0 || value > 5) return null;
		return value;
	}

	private static NativeCardSpecification? ParseCard(string input, out int nextIndex) {
		Value? value = CardID.ValueFromSymbol(input[0]);
		if(value == null) {
			nextIndex = 0;
			return null;
		}

		Axiom? axiom = CardID.AxiomFromSymbol(input[1]);
		nextIndex = axiom == null ? 1 : 2;
		return new NativeCardSpecification(value, axiom);
	}
}

public readonly struct NativeCardSpecification : CardSpecification {
	public readonly Axiom? axiom;
	public readonly Value? value;

	public NativeCardSpecification(Value? value, Axiom? axiom) {
		this.axiom = axiom;
		this.value = value;
	}

	public int? ResolveForPlayer(Player player) {
		NativeCardSpecification self = this;
		return player.Hand.WithIndex().FirstOrNull(x => self.Matches(x.value))?.index;
	}

	public bool Matches(Card card) {
		if(this.value != null) {
			if(this.value != card.Value)
				return false;
		}

		if(this.axiom != null) {
			if(this.axiom != card.Axiom)
				return false;
		}

		return true;
	}
}
