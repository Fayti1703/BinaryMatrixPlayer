using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BinaryMatrix.Engine;

/* cannot box a ref struct, so Object methods are not callable. */
#pragma warning disable CS0660, CS0661
/**
 * <summary>A reference that can be set to an equivalent of <c>null</c>.</summary>
 * <remarks>Equivalent to <c>Nullable&lt;ref T&gt;</c>.</remarks>
 * <seealso cref="Nullable"/>
 */
public readonly ref struct OptionalRef<T> {
#pragma warning restore CS0660, CS0661
	private readonly ref T value;

	public OptionalRef(ref T value) {
		this.value = ref value;
	}

	/** <summary>The <c>null</c> value.</summary> */
	public static OptionalRef<T> Empty => default;

	/**
	 * <summary>Get the value of this <see cref="OptionalRef{T}" />. This value is always a reference.</summary>
	 * <exception cref="T:System.InvalidOperationException">This <see cref="OptionalRef{T}" /> has no valid value (i.e. <see cref="HasValue"/> is <c>false</c>).</exception>
	 */
	[UnscopedRef]
	public ref T Value {
		get {
			if(Unsafe.IsNullRef(ref this.value))
				throw new InvalidOperationException("No reference here!");
			return ref this.value;
		}
	}

	/** <summary>Indicates whether this <see cref="OptionalRef{T}" /> has a valid value.</summary> */
	public bool HasValue => !Unsafe.IsNullRef(ref this.value);

	public static bool operator ==(OptionalRef<T> a, OptionalRef<T> b) {
		return a.HasValue == b.HasValue && (!a.HasValue || Unsafe.AreSame(ref a.Value, ref b.Value));
	}

	public static bool operator !=(OptionalRef<T> a, OptionalRef<T> b) {
		return !(a == b);
	}
}

public static class OptionalRefExtensions {
	public delegate void RefAction<T>(ref T value);

	public static void Apply<T>(this OptionalRef<T> @ref, RefAction<T> action) {
		if(@ref.HasValue)
			action(ref @ref.Value);
	}
}

public static class EnumerableExtensions {

	public static T? FirstOrNull<T>(this IEnumerable<T> collection) where T : struct {
		using IEnumerator<T> enumerator = collection.GetEnumerator();

		if(enumerator.MoveNext())
			return enumerator.Current;

		return null;
	}

	public static T? FirstOrNull<T>(this IEnumerable<T> collection, Predicate<T> predicate) where T : struct {
		foreach(T candidate in collection) {
			if(predicate(candidate))
				return candidate;
		}

		return null;
	}

}
