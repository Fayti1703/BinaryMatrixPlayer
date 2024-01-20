using BinaryMatrix.Engine;
using static Fayti1703.CommonLib.Misc;

namespace BinaryMatrix.Accessor;

public class ConsolePlayer : Player {
	public ActionSet action;

	public ConsolePlayer(PlayerRole role) {
		this.Role = role;
	}

	public PlayerRole Role { get; }

	public ActionSet GetAndConsumeAction() => Exchange(ref this.action, ActionSet.NONE);

	public void ReportOperationError(OperationError error) {
		Console.WriteLine("Your operation did not succeed. Error code: " + error);
	}

	public int InvalidOperationCount { get; set; }

	public CardList Hand { get; } = new();

	public void Dispose() {
		this.Hand.Dispose();
	}

}
