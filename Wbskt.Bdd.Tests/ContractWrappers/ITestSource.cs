namespace Wbskt.Bdd.Tests.ContractWrappers;

public interface ITestSource
{
    public bool Fail { get; set; }

    public string Reason { get; set; }
}
