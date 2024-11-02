using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace BinaryMatrix.Engine.Tests;

internal class TestUtils {
	[MustDisposeResource]
	internal static GameContext CreateScenarioContext(int[]? rngSequence = null) {
		Player attacker = new(PlayerRole.ATTACKER, 0, new TestPlayerActor());
		Player defender = new(PlayerRole.DEFENDER, 0, new TestPlayerActor());
		StaticRNG rng = new(rngSequence ?? Array.Empty<int>());
		GameContext game = new(new[] { attacker, defender }, rng, TestGameHooks.CreateDefaultHooks());
		return game;
	}
}
