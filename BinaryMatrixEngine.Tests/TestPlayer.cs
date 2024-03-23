using Fayti1703.CommonLib;

namespace BinaryMatrix.Engine.Tests;

public class TestPlayerActor : PlayerActor {
	public ActionSet currentAction = ActionSet.NONE;
	public OperationError lastOperationError = OperationError.NONE;

	public void ReportOperationError(OperationError error) {
		this.lastOperationError = error;
	}

	public void Dispose() { }
}
