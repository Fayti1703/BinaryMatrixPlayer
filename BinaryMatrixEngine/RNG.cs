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
