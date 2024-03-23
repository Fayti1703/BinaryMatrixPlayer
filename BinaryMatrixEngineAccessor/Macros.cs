using BinaryMatrix.Engine;

namespace BinaryMatrix.Accessor;

public static class Macros {
	public static readonly ActionSet[] Macro1 = new[] {
		"d1",
        "d0",
        "d2",
        "d4",
        "d2",
        "pa1",
        "x91",
        "p61",
        "d0",
        "c1",
        "d2",
        "p91",
        "d0",
        "p71",
        "d3",
        "c1",
        "d2",
        "d3",
        "d0",
        "x5a",
        "d0",
	}.Select(x => NativeBinCmdParser.ParseActionSet(x)!.Value).ToArray();

	public static void RunMacro(GameContext context, ActionSet[] actions) {
		foreach(ActionSet action in actions) {
			Player player = context.TurnCounter % 2 == 0 ? context.Defenders[0] : context.Attackers[0];
			((ConsolePlayerActor) player.actor).action = action;
			context.Tick();
		}
	}
}
