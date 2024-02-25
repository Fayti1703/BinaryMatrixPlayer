using Fayti1703.CommonLib;

namespace BinaryMatrix.Engine.Tests;

public class TestPlayer : Player {

	public TestPlayer(PlayerRole role) {
		this.Role = role;
	}

	public PlayerRole Role { get; }
	public ActionSet currentAction = ActionSet.NONE;
	public OperationError lastOperationError = OperationError.NONE;
	public int InvalidOperationCount { get; set; } = 0;
	public CardList Hand { get; } = new();
	public ActionSet GetAndConsumeAction() => Misc.Exchange(ref this.currentAction, ActionSet.NONE);
	public OperationError GetAndClearOperationError() => Misc.Exchange(ref this.lastOperationError, OperationError.NONE);

	public void ReportOperationError(OperationError error) {
		this.lastOperationError = error;
	}

	public void Dispose() {
		this.Hand.Dispose();
	}
}
