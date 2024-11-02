using System;
using System.Collections.Generic;
using System.Linq;
using Fayti1703.CommonLib.Enumeration;
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

	internal static void SetBoardLanes(GameBoard target, IEnumerable<IEnumerable<string>> laneCards) {
		for(int i = 0; i < 6; i++) {
			target[CellName.L0 + i].cards.Clear();
		}

		foreach((int index, IEnumerable<string> cards) in laneCards.WithIndex()) {
			new CardList(cards.Select((str, i) => {
				if(str.Length != 2) throw new ArgumentException($"Invalid CardID at {index}:{i}", nameof(laneCards));
				Value value = CardID.ValueFromSymbol(str[0]) ?? throw new ArgumentException($"Invalid Value in CardID at {index}:{i}", nameof(laneCards));
				Axiom axiom = CardID.AxiomFromSymbol(str[1]) ?? throw new ArgumentException($"Invalid Axiom in CardID at {index}:{i}", nameof(laneCards));
				return new Card(value, axiom);
			})).MoveAllTo(target[CellName.L0 + index].cards);
		}
	}
}
