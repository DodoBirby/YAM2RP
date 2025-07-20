
namespace YAM2RP;

public class ErrorAction(string error) : IYam2rpAction
{
	Lazy<List<string>> errors = new(() => [error]);
	readonly string error = error;

	public IYam2rpAction Chain(Func<IYam2rpAction> next)
	{
		var result = next();
		if (result is ErrorAction errorAction)
		{
			errors.Value.Add(errorAction.error);
			return this;
		}
		return result;
	}

	public int Run()
	{
		Console.Error.WriteLine("Failed to parse arguments as a valid YAM2RP command, Parsing errors:");
		foreach (var error in errors.Value)
		{
			if (error != "")
			{
				Console.Error.WriteLine(error);
			}
		}
		return 1;
	}
}
