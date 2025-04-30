using Wbskt.Client;

namespace Wbskt.Bdd.Tests.ContractWrappers;

public class ClientSource : ClientConnectionRequest, ITestSource
{
    public bool Fail { get; set; }

    public string Reason { get; set; } = string.Empty;
}
