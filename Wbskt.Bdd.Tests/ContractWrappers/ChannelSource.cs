using Wbskt.Common.Contracts;

namespace Wbskt.Bdd.Tests.ContractWrappers;

public class ChannelSource : ChannelDetails, ITestSource
{
    public bool Fail { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string[] Payloads { get; set; } = [];
}
