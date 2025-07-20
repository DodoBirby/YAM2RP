namespace YAM2RP;

public interface IYam2rpAction
{
	int Run();
	IYam2rpAction Chain(Func<IYam2rpAction> next);
}
