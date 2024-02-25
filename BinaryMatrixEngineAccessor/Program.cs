using BinaryMatrix.Engine;

namespace BinaryMatrix.Accessor;

public static class Program {
	public static void Main() {

		Player attacker = new(new PlayerID(PlayerRole.ATTACKER, 0), new ConsolePlayerActor());
		Player defender = new(new PlayerID(PlayerRole.DEFENDER, 0), new ConsolePlayerActor());

		GameContext context = new(new[] { attacker, defender }, new RandomRNG(new Random(1024)), GameHooks.Default);

		context.Setup();

		int lastTurnCounter = -1;
		while(context.Victor == null) {
			bool isDef = context.TurnCounter % 2 == 0;
			if(context.TurnCounter != lastTurnCounter) {
				CommandExecution.PrintPrompt(context, isDef ? defender : attacker);
				lastTurnCounter = context.TurnCounter;
			}
			string? cmd;
			do {
				WritePrompt(context);
				Console.Out.Flush();
				cmd = Console.ReadLine();
			} while(cmd == "");
			if(cmd == null) break;

			CommandExecution.RunCommand(context, isDef ? defender : attacker, cmd);
		}

		Console.WriteLine("{0} has won!", context.Victor);

	}

	public static void WritePrompt(GameContext context) {
		bool isDef = context.TurnCounter % 2 == 0;
		Console.Write("{0:D3} {1:D3}> ", context.TurnCounter, isDef ? 303 : 320);
	}
}
