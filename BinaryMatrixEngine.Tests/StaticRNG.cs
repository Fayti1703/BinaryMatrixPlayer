using System;
using System.Collections.Generic;

namespace BinaryMatrix.Engine.Tests;

/**
 * <summary>A very simple 'RNG implementation' that simply outputs the provided values, in sequence.</summary>
 */
public class StaticRNG : RNG {
	private readonly int[] values;
	private int index;

	public StaticRNG(int[] values) {
		this.values = values;
		this.index = 0;
	}

	public int Next(int upperBound) {
		int value = this.values[this.index];
		if(value >= upperBound)
			throw new InvalidOperationException($"The currently provided value is out of range for the request ({value} >= {upperBound}). Occurred at index #{this.index}");
		this.index++;
		return value;
	}
}
