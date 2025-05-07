using Wbskt.Bdd.Tests.Utils;
using Wbskt.Common.Contracts;

namespace Wbskt.Bdd.Tests.ContractWrappers;

public class UserSource : UserRegistrationRequest, ITestSource
{
    public bool Fail { get; set; }

    public int StatusCode { get; set; } = 200;

    public string Reason { get; set; } = string.Empty;

    public ChannelSource[] Channels { get; set; } = [];

    public string Token { get; set; } = string.Empty;

    public CoreServerClient Client { get; set; } = new CoreServerClient();
}
