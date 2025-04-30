using Wbskt.Common.Contracts;

namespace Wbskt.Bdd.Tests.ContractWrappers;

public class ChannelSource : ChannelRequest, ITestSource
{
    public bool Fail { get; set; }

    public string Reason { get; set; } = string.Empty;
}
