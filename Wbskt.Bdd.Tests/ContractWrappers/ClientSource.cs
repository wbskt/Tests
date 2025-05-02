using System.Text.Json.Serialization;
using Wbskt.Client;
using Wbskt.Common.Contracts;

namespace Wbskt.Bdd.Tests.ContractWrappers;

public class ClientSource : ClientConnectionRequest, ITestSource
{
    public bool Fail { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string ChannelName { get; set; } = string.Empty;

    [JsonIgnore]
    public WbsktListener? Listener { get; set; }

    [JsonIgnore]
    public List<string> ReceivedPayloads = [];

    [JsonIgnore]
    public CancellationTokenSource CancellationToken = new CancellationTokenSource();
}
