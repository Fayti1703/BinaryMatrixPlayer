using System.Buffers;
using System.Collections;
using static Fayti1703.CommonLib.Misc;

namespace BinaryMatrix.Engine;

/* TODO?: Do an optimization pass over this?
 * We could take advantage of the unique nature of literally *every single list* here.
 * Combat Stacks: Filled one at a time, then rapidly emptied. (Defender stack might have some leftovers.)
 * Lane Decks/Attacker Deck: Drained one at a time, then rapidly refilled.
 * Discard Piles: Filled slowly, then rapidly emptied.
 *
 * (honestly optimizing this doesn't matter except for rapidly simulating games, so this is more of an academic thing)
 */
/* Note on ordering: For optimization purposes,  */
public sealed class CardList : IEnumerable<Card>, IDisposable {

	private const bool FAIL_FAST_INVALID = true;

	private Card[] cards = Array.Empty<Card>();
	public int Count { get; private set; } = 0;

	/* TODO?: Profile array pool usage to get a sense of some better parameters here. */
	private static readonly ArrayPool<Card> listPool = ArrayPool<Card>.Create(8, 32);

	public CardList() {}

	public CardList(int initialCapacity) {
		this.cards = listPool.Rent(initialCapacity);
	}

	public CardList(IEnumerable<Card> cards) {
		if(cards is CardList list) {
			this.cards = listPool.Rent(list.Count);
			this.AddAll(list);
			return;
		}

		Card[] theCards = cards.ToArray();
		this.cards = listPool.Rent(theCards.Length);
		Array.Copy(theCards, this.cards, theCards.Length);
		this.Count = theCards.Length;
		if(FAIL_FAST_INVALID) {
			foreach(ref Card card in this) {
				if(card.IsInvalid)
					throw new ArgumentException("The provided enumerable contains an invalid card!", nameof(cards));
			}
		}
	}

	public CardList(CardList cards) {
		this.cards = listPool.Rent(cards.Count);
		this.AddAll(cards);
	}

	public void Dispose() {
		if(this.cards.Length > 0)
			listPool.Return(this.cards);
	}

	public ref Card this[int index] {
		get {
			if(index < 0 || index >= this.Count)
				throw new IndexOutOfRangeException();
			return ref this.cards[index];
		}
	}

	public ref Card Add(Card card) {
		if(FAIL_FAST_INVALID && card.IsInvalid)
			throw new ArgumentException("Invalid card inserted!", nameof(card));
		this.EnsureFreeCapacity(1);
		this.cards[this.Count] = card;
		this.Count++;
		return ref this.cards[this.Count - 1];
	}

	public void AddAll(CardList cards) {
		this.EnsureFreeCapacity(cards.Count);

		Span<Card> sourceRange = cards.cards.AsSpan(..cards.Count);

		if(FAIL_FAST_INVALID) {
			/* TODO: Can this even happen? */
			foreach(ref Card card in sourceRange) {
				if(card.IsInvalid)
					throw new ArgumentException("The provided list contains an invalid card!", nameof(cards));
			}
		}

		sourceRange.CopyTo(this.cards.AsSpan(this.Count..));
		this.Count += cards.Count;
	}

	public void Clear() {
		listPool.Return(Exchange(ref this.cards, Array.Empty<Card>()));
		this.Count = 0;
	}

	public OptionalRef<Card> First() {
		return this.Count == 0 ? OptionalRef<Card>.Empty : new OptionalRef<Card>(ref this[0]);
	}

	public OptionalRef<Card> Last() {
		return this.Count == 0 ? OptionalRef<Card>.Empty : new OptionalRef<Card>(ref this[^1]);
	}

	private void EnsureFreeCapacity(int requested) {
		int requiredLength = requested + this.Count;

		if(requiredLength < this.cards.Length)
			return;

		int newLength;
		do {
			newLength = this.cards.Length * 2;
			if(newLength == 0) newLength = 1;
		} while(requiredLength > newLength);

		Card[] aNewArray = listPool.Rent(newLength);
		this.cards.CopyTo(aNewArray, 0);
		Card[] theOldArray = Exchange(ref this.cards, aNewArray); /* !! REF INVALIDATION HERE !! */
		listPool.Return(theOldArray);
	}

	public CardListEnumerator GetEnumerator() => new(this);
	IEnumerator<Card> IEnumerable<Card>.GetEnumerator() => this.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();


	public void MoveAllTo(CardList cards) {
		if(cards.Count == 0) {
			/* target list is empty -> we can simply donate our array to it */
			cards.cards = this.cards;
			cards.Count = this.Count;
			this.cards = Array.Empty<Card>();
			this.Count = 0;
		} else {
			/* otherwise -> slow track, add all of our stuff, then clear ourselves */
			cards.AddAll(this);
			this.Clear();
		}
	}

	public Card? TakeFirst() {
		if(this.Count <= 0)
			return null;

		Card ret = this.cards[0];
		this.Count--;
		if(this.Count != 0)
			Array.Copy(this.cards, 1, this.cards, 0, this.Count);
		return ret;
	}

	public Card? TakeLast() {
		if(this.Count <= 0)
			return null;
		Card ret = this.cards[this.Count - 1];
		this.Count--;
		return ret;
	}

	public Card? Take(int index) {
		Card ret = this.cards[index];
		this.Count--;
		if(index < this.Count)
			Array.Copy(this.cards, index + 1, this.cards, index, this.Count - index);
		return ret;
	}

	public class CardListEnumerator : IEnumerator<Card> {
		private readonly CardList list;
		private int current = -1;

		public CardListEnumerator(CardList list) {
			this.list = list;
		}

		public bool MoveNext() {
			if(this.current >= this.list.Count)
				return false;
			this.current++;
			return this.current < this.list.Count;
		}

		public void Reset() {
			this.current = -1;
		}

		public ref Card Current => ref this.list[this.current];
		Card IEnumerator<Card>.Current => this.Current;
		object IEnumerator.Current => this.Current;

		public void Dispose() { /* nothing to dispose */ }
	}
}
