using BinaryMatrix.Engine;
using JetBrains.Annotations;

namespace BinaryMatrix.Accessor;

public class CommandExecution {
	/* FIXME?: This doesn't support multiple players per side yet */
	public static void RunCommand(GameContext game, Player activePlayer, string cmd) {
		if(cmd.Length == 0) return;
		switch(cmd[0]) {
			case '/':
				RunMetaCommand(game, activePlayer, cmd[1..].TrimStart());
				/* meta command */
				break;
			default:
				ActionSet? action = NativeBinCmdParser.ParseActionSet(cmd);
				if(action == null) {
					Console.WriteLine("Could not parse your input.");
					return;
				}
				((ConsolePlayerActor) activePlayer.actor).action = action.Value;
				goto case '-';
			case '-':
				game.Tick();
				break;
		}
	}

	private static void RunMetaCommand(GameContext game, Player activePlayer, string cmd) {
		switch(cmd) {
			#if false
			case "full-state":
				DumpFullState(game);
				break;
			#endif
			case "board":
				PrintBoard(game.board);
				break;
			case "hand":
				PrintHand(activePlayer);
				break;
			case "prompt":
				PrintPrompt(game, activePlayer);
				break;
			case "macro1":
				Macros.RunMacro(game, Macros.Macro1);
				break;
			case "break":
				throw new Exception("Break signalled!");
		}
	}

	public static void PrintPrompt(GameContext game, Player activePlayer) {
		PrintHeader(game);
		PrintHand(activePlayer);

		PrintBoard(game.board);
	}

	private static void PrintHand(Player activePlayer) {
		Console.Write("h{0}0: ", activePlayer.Role == PlayerRole.ATTACKER ? "a" : "d");
		WriteCards(activePlayer.Hand);
		Console.Write("\n");
	}

	private static void PrintHeader(GameContext game) {

	}

	private static void PrintRow(GameBoard board, CellName firstColumn, string fallbackText, ConsoleColor fallbackColor) {
		for(int i = 0; i < 6; i++) {
			OptionalRef<Card> card = board[firstColumn + i].cards.Last();
			if(card.HasValue) {
				if(card.Value.revealed) {
					WriteCard(card.Value);
				} else {
					Console.Write("X ");
				}
			} else {
				WriteWithColor(fallbackColor, fallbackText);
			}
			Console.Write(" ");
		}
		Console.Write("\n");
	}

	private static void PrintBoard(GameBoard board) {
		PrintRow(board, CellName.A0, "  ", default);
		PrintRow(board, CellName.D0, "  ", default);
		PrintRow(board, CellName.L0, "--", ConsoleColor.DarkRed);
		PrintRow(board, CellName.X0, "  ", default);
	}

	#if false
	private static void DumpFullState(GameContext game) {
		throw new NotImplementedException();
	}
	#endif

	public static void PrintStack(GameContext context, CellName name) {
		ConsoleColor colorBackup = Console.ForegroundColor;
		Console.ForegroundColor = ConsoleColor.White;
		Console.Write("{0}: ", name);
		WriteCards(context.board[name].cards);
		Console.Write("\n");
		Console.ForegroundColor = colorBackup;
	}

	public static void WriteCards(CardList cards) {
		foreach(Card card in cards) {
			WriteCard(card);
			Console.Write(" ");
		}
	}

	public static void WriteCard(Card card) {
		WriteWithColor(
			card.Axiom switch {
				Axiom.DATA => ConsoleColor.Gray,
				Axiom.KIN => ConsoleColor.Cyan,
				Axiom.FORM => ConsoleColor.Green,
				Axiom.VOID => ConsoleColor.Yellow,
				Axiom.CHAOS => ConsoleColor.Red,
				Axiom.CHOICE => ConsoleColor.DarkYellow,
				_ => throw new Exception()
			},
			"{0}", card
		);
	}

	[StringFormatMethod(nameof(format))]
	public static void WriteWithColor(ConsoleColor color, string format, params object[] args) {
		var backup = Console.ForegroundColor;
		Console.ForegroundColor = color;
		Console.Write(format, args);
		Console.ForegroundColor = backup;
	}
}
