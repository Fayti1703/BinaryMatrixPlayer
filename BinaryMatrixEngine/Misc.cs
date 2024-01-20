using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace BinaryMatrix.Engine;

/* cannot box a ref struct, so Object methods are not callable. */
#pragma warning disable CS0660, CS0661
public readonly ref struct OptionalRef<T> {
#pragma warning restore CS0660, CS0661
	private readonly ref T value;

	public OptionalRef(ref T value) {
		this.value = ref value;
	}

	public static OptionalRef<T> Empty => default;

	[UnscopedRef]
	public ref T Value {
		get {
			if(Unsafe.IsNullRef(ref this.value))
				throw new InvalidOperationException("No reference here!");
			return ref this.value;
		}
	}

	public bool HasValue => !Unsafe.IsNullRef(ref this.value);

	public static bool operator ==(OptionalRef<T> a, OptionalRef<T> b) {
		return a.HasValue == b.HasValue && (!a.HasValue || Unsafe.AreSame(ref a.Value, ref b.Value));
	}

	public static bool operator !=(OptionalRef<T> a, OptionalRef<T> b) {
		return !(a == b);
	}
}

public static class Misc {

	public delegate void RefAction<T>(ref T value);

	public static void Apply<T>(this OptionalRef<T> @ref, RefAction<T> action) {
		if(@ref.HasValue)
			action(ref @ref.Value);
	}

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
