using System;
using System.Runtime.CompilerServices;

namespace BinaryMatrix.Engine.Tests;

[TestClass]
public class CardListTest {
	[AssemblyInitialize]
	public static void TurnOnFastFail() { CardList.FAIL_FAST_INVALID = true; }

	[TestMethod]
	public void EmptyListTest() {
		CardList list = new();
		Assert.AreEqual(0, list.Count);
		Assert.That.InvariantsArePreserved(list);
		Assert.IsNull(list.TakeFirst());
		Assert.IsNull(list.TakeLast());
		Assert.IsNull(list.Take(0));
		Assert.IsNull(list.Take(1));
	}

	[TestMethod]
	public void AddToEmptyListTest() {
		CardList list = new();
		ref Card inserted = ref list.Add(new Card(Value.BREAK, Axiom.KIN));
		Assert.AreEqual(1, list.Count);
		Assert.That.AreSame(ref inserted, ref list[0]);
		Assert.AreEqual(new CardID(Value.BREAK, Axiom.KIN), list[0].ID);
	}
}

[TestClass]
public class CardListConstructionTest {
	[TestMethod]
	public void InitialCapacityStartsEmpty() {
		CardList list = new(5);
		Assert.AreEqual(0, list.Count);
		Assert.That.InvariantsArePreserved(list);
	}

	[TestMethod]
	public void InitialCapacityCanBeAddedTo() {
		CardList list = new(5);
		ref Card inserted = ref list.Add(new Card(Value.BREAK, Axiom.KIN));
		Assert.AreEqual(1, list.Count);
		Assert.That.AreSame(ref inserted, ref list[0]);
		Assert.AreEqual(new CardID(Value.BREAK, Axiom.KIN), list[0].ID);
	}
}


public static class CardListAsserts {
	public static void InvariantsArePreserved(this Assert _0, CardList list) {
		int listCount = list.Count;
		for(int i = 0; i < listCount; i++) {
			ref Card _ = ref list[0];
		}
		Assert.ThrowsException<IndexOutOfRangeException>(() => list[-1], "-1 access must always throw IndexOutOfRangeException");
		Assert.ThrowsException<IndexOutOfRangeException>(() => list[listCount], "1 past the end access must always throw IndexOutOfRangeException");
		if(listCount == 0) {
			Assert.That.IsEmpty(list.First(), "An empty list must have no First element");
			Assert.That.IsEmpty(list.Last(), "An empty list must have no Last element");
		} else {
			ref Card expectedFirst = ref list[0];
			ref Card expectedLast = ref list[^1];
			Assert.That.PointsTo(ref expectedFirst, list.First(), "A non-empty list's First element must point to element 0");
			Assert.That.PointsTo(ref expectedLast, list.Last(), "A non-empty list's Last element must point to element ^1");
		}
		Assert.AreEqual(listCount, list.Count, "None of the invariant checks affect the list count.");
	}
}

public static class OptionalRefAsserts {
	public static void IsEmpty<T>(this Assert _, OptionalRef<T> value, string? message = null) {
		if(value.HasValue)
			throw new AssertFailedException(message ?? "OptionalRef is not empty!");
	}

	public static void IsNotEmpty<T>(this Assert _, OptionalRef<T> value, string? message = null) {
		if(!value.HasValue)
			throw new AssertFailedException(message ?? "OptionalRef is empty!");
	}

	public static void PointsTo<T>(this Assert _, ref T expected, OptionalRef<T> value, string? message = null) {
		if(!value.HasValue)
			throw new AssertFailedException("OptionalRef is empty!");
		if(!Unsafe.AreSame(ref expected, ref value.Value)) {
			unsafe {
				nint expectedAddr = (nint) Unsafe.AsPointer(ref expected);
				nint actualAddr = (nint) Unsafe.AsPointer(ref value.Value);
				throw new AssertFailedException(message ?? $"References do not point to the same location! ({expectedAddr} <=> {actualAddr})");
			}
		}
	}

	public static void AreSame<T>(this Assert _, ref T expected, ref T actual, string? message = null) {
		if(!Unsafe.AreSame(ref expected, ref actual)) {
			unsafe {
				nint expectedAddr = (nint) Unsafe.AsPointer(ref expected);
				nint actualAddr = (nint) Unsafe.AsPointer(ref actual);
				throw new AssertFailedException(message ?? $"References do not point to the same location! ({expectedAddr} <=> {actualAddr})");
			}
		}
	}
}
