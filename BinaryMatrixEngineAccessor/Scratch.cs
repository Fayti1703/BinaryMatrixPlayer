namespace BinaryMatrix.Accessor;

public class Scratch {
	public static void SMain() {
		Console.WriteLine(typeof(Foo).GetInterface("IFrobbable"));
		Console.WriteLine(typeof(Foo).GetInterface("IThing"));
	}
}

public class Foo : IThing {

}

public interface IThing : IFrobbable {

}

public interface IFrobbable {

}
