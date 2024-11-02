using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinaryMatrix.Engine.Tests;

internal class TestUtils {
	[MustDisposeResource]
	internal static GameContext CreateScenarioContext(RNG rng) {
		Player attacker = new(PlayerRole.ATTACKER, 0, new TestPlayerActor());
		Player defender = new(PlayerRole.DEFENDER, 0, new TestPlayerActor());
		GameContext game = new(new[] { attacker, defender }, rng, TestGameHooks.CreateDefaultHooks());
		return game;
	}


	[MustDisposeResource]
	internal static GameContext CreateStaticRNGScenarioContext(int[]? rngSequence = null) {
		return CreateScenarioContext(new StaticRNG(rngSequence ?? Array.Empty<int>()));
	}
}
