using System.Text.Json;
using Wbskt.Bdd.Tests.ContractWrappers;
using Wbskt.Bdd.Tests.Utils;

namespace Wbskt.Bdd.Tests;

public class ClientTests
{
    private CoreServerClient _commonClient;
    private TestSource _testSource;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var json = await File.ReadAllTextAsync($"{nameof(ClientTests)}.json");
        json = PlaceholderReplacer.ReplacePlaceholders(json);
        _testSource = JsonSerializer.Deserialize<TestSource>(json) ?? new TestSource();
        _commonClient = new CoreServerClient();

        await Task.WhenAll(_testSource.Users.Select(user => Task.Run(async () =>
        {
            var result = await _commonClient.RegisterUser(user).ConfigureAwait(false);
            Assert.That(result.IsSuccessStatusCode, Is.True);
            result = await _commonClient.GetToken(user);
            Assert.That(result.IsSuccessStatusCode, Is.True);
            user.Token = result.Value ?? string.Empty;
            user.Client.SetUserToken(user.Token);
        })));

        await Task.WhenAll(_testSource.Users.Select(user => Task.Run(async () =>
        {
            foreach (var channel in user.Channels)
            {
                var result = await user.Client.CreateChannel(channel);
                Assert.That(result.IsSuccessStatusCode, Is.True);
                Assert.That(result.Value?.ChannelName, Is.EqualTo(channel.ChannelName));
                Assert.That(result.Value.ChannelSecret, Is.EqualTo(channel.ChannelSecret));
                channel.ChannelSubscriberId = result.Value.ChannelSubscriberId;
                channel.ChannelPublisherId = result.Value.ChannelPublisherId;
            }
        })));
    }

    [Test]
    public async Task ClientConnectionTest()
    {
        var clients = _testSource.Users.SelectMany(u => u.Channels).SelectMany(c => c.GetClientsForChannel()).ToArray();

        var ct = new CancellationTokenSource();

        Task.WaitAll(clients.Select(listener => listener.StartListeningAsync(ct.Token)).ToArray(), 1 * 60 * 1000);

        var count = 0;
        foreach (var listener in clients)
        {
            count++;
            Assert.That(listener.IsConnected, Is.True, message: $"count is {count}");
        }

        Task.WaitAll(clients.Select(listener => listener.StopListeningAsync()).ToArray(), 10 * 1000);
    }
}
