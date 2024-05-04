namespace BinaryMatrix.Engine;

public interface RNG {
	/** <summary>Return a new random value, in the range <c>[0;upperBound[</c>.</summary> */
	int Next(int upperBound);
}

public class RandomRNG : RNG {
	private readonly Random random;

	public RandomRNG(Random random) {
		this.random = random;
	}
	public int Next(int upperBound) => this.random.Next(upperBound);
}


public class SFC32RNG : RNG {
	private uint a, b, c, d;

	public SFC32RNG(uint a, uint b, uint c, uint d = 1) {
		this.a = a;
		this.b = b;
		this.c = c;
		this.d = d;
	}

	public int Next(int upperBound) {
		return (int) ((double) this.RawNext() / uint.MaxValue * upperBound);
	}

	public uint RawNext() {
		uint t = this.a + this.b + this.d++;
		this.a = this.b ^ this.b >>> 9;
		this.b = this.c + (this.c << 3);
		this.c = this.c << 21 | this.c >>> 11;
		this.c += t;
		return t;
	}

	public uint[] GetState() {
		return [ this.a, this.b, this.c, this.d ];
	}
}
