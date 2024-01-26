namespace BinaryMatrix.Engine;

public interface BinlogRenderer<out T> {
	public T RenderBinlog(TurnLog log);
}
