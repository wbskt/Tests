namespace Wbskt.Bdd.Tests;

public class MultiServerTests
{
    private CoreServerClient _coreServerClient;

    [SetUp]
    public void Setup()
    {
        _coreServerClient = new CoreServerClient("https://wbskt.com");
    }

    [Test]
    public void Test1()
    {
        Assert.Pass();
    }
}
