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

public readonly struct CardID : IEquatable<CardID> {
	public readonly Axiom axiom;
	public readonly Value value;

	public CardID(Axiom axiom, Value value) {
		if(axiom < Axiom.DATA || axiom > Axiom.CHOICE)
			throw new ArgumentException("Invalid suit provided.", nameof(axiom));
		if(value < Value.TWO || value > Value.TRAP)
			throw new ArgumentException("Invalid value provided.", nameof(value));

		this.axiom = axiom;
		this.value = value;
	}

	public static CardID Unknown => default;

	public bool IsUnknown => this.axiom == 0 || this.value == 0;

	static CardID() {
		allIDs = Enum.GetValues<Axiom>().SelectMany(_ => Enum.GetValues<Value>(), (axiom, value) => new CardID(axiom, value)).ToImmutableList();
	}

	public static readonly IReadOnlyList<CardID> allIDs;

	override public string ToString() {
		return this.IsUnknown ? "X" :
			new string(new [] {
				ValueToSymbol(this.value),
				AxiomToSymbol(this.axiom)
			})
		;
	}

	public bool Equals(CardID other) => this.axiom == other.axiom && this.value == other.value;
	override public bool Equals(object? obj) => obj is CardID other && this.Equals(other);
	override public int GetHashCode() => HashCode.Combine((int) this.axiom, (int) this.value);
	public static bool operator ==(CardID left, CardID right) => left.Equals(right);
	public static bool operator !=(CardID left, CardID right) => !left.Equals(right);

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
}

[DebuggerDisplay("{DebugDisplay()}")]
public struct Card : IEquatable<Card> {
	public readonly CardID ID;
	public bool revealed = false;

	public readonly Axiom Axiom => this.ID.axiom;
	public readonly Value Value => this.ID.value;

	public Card(CardID id) {
		this.ID = id;
	}

	public Card(Axiom axiom, Value value) {
		this.ID = new CardID(axiom, value);
	}

	/* A particular card cannot ever have an unknown ID. */
	public bool IsInvalid => this.ID.IsUnknown;

	public bool Equals(Card other) => this.ID == other.ID;
	override public bool Equals(object? obj) => obj is Card other && this.Equals(other);
	override public int GetHashCode() => this.ID.GetHashCode();

	public static bool operator ==(Card left, Card right) => left.Equals(right);
	public static bool operator !=(Card left, Card right) => !left.Equals(right);

	static Card() {
		allCards = CardID.allIDs.Select(x => new Card(x)).ToImmutableList();
	}

	public static readonly IReadOnlyList<Card> allCards;

	override public string ToString() {
		return this.ID + (this.revealed ? "u" : "");
	}

	public string DebugDisplay() {
		return this.ToString();
	}
}
