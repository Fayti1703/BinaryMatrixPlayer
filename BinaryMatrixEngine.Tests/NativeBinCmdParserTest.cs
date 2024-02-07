using System;
using System.Collections.Generic;
using static BinaryMatrix.Engine.ActionType;
using static BinaryMatrix.Engine.Axiom;
using static BinaryMatrix.Engine.NativeBinCmdParser;
using static BinaryMatrix.Engine.Value;
using NCardSpec = BinaryMatrix.Engine.NativeCardSpecification;

namespace BinaryMatrix.Engine.Tests;

[TestClass]
public class NativeBinCmdParserTest {
	internal static readonly Lazy<IEqualityComparer<ActionSet?>> _eq = new(() => new NullEqualEqualityComparer<ActionSet>(new TestActionSetEqualityComparer(new TestCardSpecificationComparer())));
	internal static IEqualityComparer<ActionSet?> Eq => _eq.Value;

	internal const int LANE_A = ActionSet.LANE_A;

	[TestMethod]
	public void Pass() =>
        Assert.AreEqual(null, ParseActionSet("--"), Eq);

	[TestMethod]
	public void DrawFromLane() =>
		Assert.AreEqual(new ActionSet(DRAW, lane: 0), ParseActionSet("d0"), Eq);

	[TestMethod]
	public void DrawAttackerDeck() =>
		Assert.AreEqual(new ActionSet(DRAW, lane: LANE_A), ParseActionSet("da"), Eq);

	[TestMethod]
	public void DoCombat() =>
		Assert.AreEqual(new ActionSet(COMBAT, 0), ParseActionSet("c0"), Eq);

	[TestMethod]
	public void PlayCardValue() =>
		Assert.AreEqual(new ActionSet(PLAY, new NCardSpec(EIGHT, null), 0), ParseActionSet("p80"), Eq);

	[TestMethod]
	public void FaceupPlayCardValue() =>
		Assert.AreEqual(new ActionSet(FACEUP_PLAY, new NCardSpec(EIGHT, null), 0), ParseActionSet("u80"), Eq);

	[TestMethod]
	public void PlayFullCard() =>
		Assert.AreEqual(new ActionSet(PLAY, new NCardSpec(EIGHT, FORM), 0), ParseActionSet("p8&0"), Eq);

	[TestMethod]
	public void FaceupPlayFullCard() =>
		Assert.AreEqual(new ActionSet(FACEUP_PLAY, new NCardSpec(EIGHT, FORM), 0), ParseActionSet("u8&0"), Eq);

	[TestMethod]
	public void PlayInvalidCard() =>
		Assert.AreEqual(null, ParseActionSet("pX"));

	[TestMethod]
	public void DiscardCardValueNoDest() =>
		Assert.AreEqual(null, ParseActionSet("x8"));

	[TestMethod]
	public void DiscardCardValueAttackerDiscard() =>
		Assert.AreEqual(new ActionSet(DISCARD, new NCardSpec(EIGHT, null), LANE_A), ParseActionSet("x8a"), Eq);

	[TestMethod]
	public void DiscardFullCardNoDest() =>
		Assert.AreEqual(null, ParseActionSet("x8&"), Eq);

	[TestMethod]
	public void DiscardFullCardAttackerDiscard() =>
		Assert.AreEqual(new ActionSet(DISCARD, new NCardSpec(EIGHT, FORM), LANE_A), ParseActionSet("x8&a"), Eq);

	[TestMethod]
	public void DiscardCardValueLaneDiscard() =>
		Assert.AreEqual(new ActionSet(DISCARD, new NCardSpec(EIGHT, null), 0), ParseActionSet("x80"), Eq);

	[TestMethod]
	public void DiscardFullCardLaneDiscard() =>
		Assert.AreEqual(new ActionSet(DISCARD, new NCardSpec(EIGHT, FORM), 0), ParseActionSet("x8&0"), Eq);

	[TestMethod]
	public void Invalid() =>
		Assert.AreEqual(null, ParseActionSet("XX"), Eq);

}

#region Equality Comparators
internal class NullEqualEqualityComparer<T> : IEqualityComparer<T?> where T : struct {
	private readonly IEqualityComparer<T> inner;

	public NullEqualEqualityComparer(IEqualityComparer<T> inner) {
		this.inner = inner;
	}

	public bool Equals(T? x, T? y) {
		if(x == null) return y == null;
		if(y == null) return false;
		return this.inner.Equals(x.Value, y.Value);
	}

	public int GetHashCode(T? obj) => obj == null ? 0 : this.inner.GetHashCode(obj.Value);
}

internal class TestActionSetEqualityComparer : IEqualityComparer<ActionSet> {
	private readonly IEqualityComparer<NCardSpec> specComparer;

	public TestActionSetEqualityComparer(IEqualityComparer<NCardSpec> specComparer) {
		this.specComparer = specComparer;
	}

	public bool Equals(ActionSet x, ActionSet y) {
		if(x.type != y.type) return false;
		return x.type switch {
			NONE => true,
			DRAW => x.lane == y.lane,
			PLAY or FACEUP_PLAY or DISCARD =>
				x.lane == y.lane && this.specComparer.Equals((NCardSpec) x.card!, (NCardSpec) y.card!),
			COMBAT => x.lane == y.lane,
			_ => throw new ArgumentException("Illegal action sets detected!")
		};
	}

	public int GetHashCode(ActionSet obj) =>
		HashCode.Combine(obj.type, obj.lane, obj.card != null ? this.specComparer.GetHashCode((NCardSpec) obj.card) : 0);
}

internal class TestCardSpecificationComparer : IEqualityComparer<NCardSpec> {
	public bool Equals(NCardSpec x, NCardSpec y) => x.axiom == y.axiom && x.value == y.value;
	public int GetHashCode(NCardSpec obj) => HashCode.Combine(obj.axiom, obj.value);
}
#endregion
