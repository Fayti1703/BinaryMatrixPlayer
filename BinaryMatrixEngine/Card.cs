using System.Collections.Immutable;
using System.Diagnostics;
#pragma warning disable CS8524 // The switch expression does not handle some values of its input type (it is not exhaustive) involving an unnamed enum value.

namespace BinaryMatrix.Engine;

/**
 * <summary>The "axiom" or "suit" of a card.</summary>
 * <remarks>This has no effect on gameplay.</remarks>
 */
public enum Axiom {
	DATA = 1,
	KIN,
	FORM,

	VOID,
	CHAOS,
	CHOICE
}

public enum Value {
#region Numeric Values
	TWO = 2,
	THREE = 3,
	FOUR = 4,
	FIVE = 5,
	SIX = 6,
	SEVEN = 7,
	EIGHT = 8,
	NINE = 9,
	TEN = 10,
#endregion

	WILD,
	BOUNCE,
	BREAK,
	TRAP
}

[DebuggerDisplay("{DebugDisplay()}")]
public struct Card : IEquatable<Card> {
	public readonly Axiom axiom;
	public readonly Value value;
	public bool revealed = false;

	public Card(Axiom axiom, Value value) {
		if(axiom < Axiom.DATA || axiom > Axiom.CHOICE)
			throw new ArgumentException("Invalid suit provided.", nameof(axiom));
		if(value < Value.TWO || value > Value.TRAP)
			throw new ArgumentException("Invalid value provided.", nameof(axiom));
		this.axiom = axiom;
		this.value = value;
	}

	public bool IsInvalid => this.value == 0 || this.axiom == 0;

	public bool Equals(Card other) => this.axiom == other.axiom && this.value == other.value;
	override public bool Equals(object? obj) => obj is Card other && this.Equals(other);
	override public int GetHashCode() => HashCode.Combine((int) this.axiom, (int) this.value);

	public static bool operator ==(Card left, Card right) => left.Equals(right);
	public static bool operator !=(Card left, Card right) => !left.Equals(right);

	static Card() {
		allCards = Enum.GetValues<Axiom>().SelectMany(_ => Enum.GetValues<Value>(), (axiom, value) => new Card(axiom, value)).ToImmutableList();
	}

	public static readonly IReadOnlyList<Card> allCards;

	public static Axiom? AxiomFromSymbol(char c) {
		return c switch {
			'+' => Axiom.DATA,
			'%' => Axiom.KIN,
			'&' => Axiom.FORM,
			'!' => Axiom.CHAOS,
			'^' => Axiom.VOID,
			'#' => Axiom.CHOICE,
			_ => null
		};
	}

	public static char AxiomToSymbol(Axiom axiom) {
		return axiom switch {
			Axiom.DATA => '+',
			Axiom.KIN => '%',
			Axiom.FORM => '&',
			Axiom.VOID => '^',
			Axiom.CHAOS => '!',
			Axiom.CHOICE => '#',
		};
	}

	public static Value? ValueFromSymbol(char c) {
		return c switch {
			'2' => Value.TWO,
			'3' => Value.THREE,
			'4' => Value.FOUR,
			'5' => Value.FIVE,
			'6' => Value.SIX,
			'7' => Value.SEVEN,
			'8' => Value.EIGHT,
			'9' => Value.NINE,
			'a' => Value.TEN,
			'*' => Value.WILD,
			'?' => Value.BOUNCE,
			'>' => Value.BREAK,
			'@' => Value.TRAP,
			_ => null
		};
	}

	public static char ValueToSymbol(Value value) {
		return value switch {
			Value.TWO => '2',
			Value.THREE => '3',
			Value.FOUR => '4',
			Value.FIVE => '5',
			Value.SIX => '6',
			Value.SEVEN => '7',
			Value.EIGHT => '8',
			Value.NINE => '9',
			Value.TEN => 'a',
			Value.WILD => '*',
			Value.BOUNCE => '?',
			Value.BREAK => '>',
			Value.TRAP => '@'
		};
	}

	override public string ToString() {
		if(this.value == 0 || this.axiom == 0)
			return "(( DEFAULT CARD ))";
		if(this.IsInvalid)
			return "(( INVALID CARD ))";

		return new string(new [] { ValueToSymbol(this.value), AxiomToSymbol(this.axiom) });
	}

	public string DebugDisplay() {
		return this.ToString();
	}
}
