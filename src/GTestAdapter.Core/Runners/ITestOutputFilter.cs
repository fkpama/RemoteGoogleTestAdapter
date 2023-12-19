namespace GoogleTestAdapter.Remote.Runners
{
    public interface ITestOutputFilter
    {
        string Transform(string line);
    }
}