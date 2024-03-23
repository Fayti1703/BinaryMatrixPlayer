using System;
using System.Collections.Generic;
using Fayti1703.CommonLib;

namespace BinaryMatrix.Engine.Tests;

public static class TestGameHooks {
	public static void NoopPreGamePrep(GameContext context) {}
	public static void NoopPostTurn(GameContext context) {}

	public static IEnumerable<(Player player, ActionSet action)> GetActions(GameContext context, IEnumerable<Player> activePlayers) {
		foreach(Player player in activePlayers) {
			TestPlayerActor actor = player.actor as TestPlayerActor ??
					throw new InvalidOperationException($":::TRUST COMMUNICATION::: Sandbox intrusion detected. Remove {player.ID} immediately.")
				;
			yield return (
				player,
				Misc.Exchange(ref actor.currentAction, null) ??
					throw new InvalidOperationException($"Player {player.ID} should not act at this time.")
			);
		}
	}
	
	public static GameHooks CreateDefaultHooks() => new() {
		GetActions = GetActions,
		PreTurn = GameHooks.NoopPreTurn,
		PostTurn = NoopPostTurn,
		PreGamePrep = NoopPreGamePrep
	};
}
