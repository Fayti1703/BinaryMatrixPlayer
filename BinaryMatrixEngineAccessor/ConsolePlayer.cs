using BinaryMatrix.Engine;
using static Fayti1703.CommonLib.Misc;

namespace BinaryMatrix.Accessor;

public class ConsolePlayerActor : ActionablePlayerActor {
	public Player player = null!;
	public ActionSet action;

	public ActionSet GetAndConsumeAction() => Exchange(ref this.action, ActionSet.NONE);

	public void ReportOperationError(OperationError error) {
		Console.WriteLine("Your operation did not succeed. Error code: " + error);
	}

	public void Dispose() {}

}
