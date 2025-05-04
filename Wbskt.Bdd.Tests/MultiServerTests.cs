using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Wbskt.Bdd.Tests.ContractWrappers;
using Wbskt.Client;
using Wbskt.Client.Configurations;
using Wbskt.Common.Contracts;

namespace Wbskt.Bdd.Tests;

public class MultiServerTests
{
    private CoreServerClient _commonClient;
    private ConcurrentBag<Common.Contracts.ChannelDetails> _channelDetails = [];
    private TestSource _testSource;

    [OneTimeSetUp]
    public void Setup()
    {
        var json = File.ReadAllText("TestSource.json");
        json = PlaceholderReplacer.ReplacePlaceholders(json);
        _testSource = JsonSerializer.Deserialize<TestSource>(json) ?? new TestSource();
        foreach (var client in _testSource.Clients)
        {
            if (client.ClientUniqueId == new Guid())
            {
                client.ClientUniqueId = Guid.NewGuid();
            }
        }
        _commonClient = new CoreServerClient();
    }

    [Test, Order(1)]
    public async Task UserCreationTests()
    {
        // Positive
        await Task.WhenAll(_testSource.Users.Where(u => !u.Fail).Select(user => Task.Run(async () =>
        {
            var result = await _commonClient.RegisterUser(user).ConfigureAwait(false);
            Assert.That(result.IsSuccessStatusCode, Is.True);
            result = await _commonClient.GetToken(user);
            Assert.That(result.IsSuccessStatusCode, Is.True);
            user.Token = result.Value;
            user.Client.SetUserToken(user.Token);
        })));

        // Negative
        await Task.WhenAll(_testSource.Users.Where(u => u.Fail).Select(user => Task.Run(async () =>
        {
            var result = await _commonClient.RegisterUser(user).ConfigureAwait(false);
            Assert.That(result.IsSuccessStatusCode, Is.False);
        })));

        // Negative
        var result = await _commonClient.RegisterUser(_testSource.Users.First());
        Assert.That(result.IsSuccessStatusCode, Is.False);

        // Negative
        result = await _commonClient.GetToken(new UserLoginRequest { EmailId = _testSource.Users.First().EmailId, Password = "wrongpassword"});
        Assert.That(result.IsSuccessStatusCode, Is.False);
    }

    [Test, Order(2)]
    public async Task ChannelCreationTests()
    {
        var tasks = new List<Task>();

        // Positive
        foreach (var user in _testSource.Users.Where(u => !u.Fail))
        {
            tasks.AddRange(user.Channels.Where(c => !c.Fail).Select(channel => Task.Run(async () =>
            {
                var result = await user.Client.CreateChannel(channel).ConfigureAwait(false);
                Assert.That(result.IsSuccessStatusCode, Is.True);
                Assert.That(result.Value.ChannelName, Is.EqualTo(channel.ChannelName));
                Assert.That(result.Value.ChannelSecret, Is.EqualTo(channel.ChannelSecret));
                channel.ChannelSubscriberId = result.Value.ChannelSubscriberId;
                channel.ChannelPublisherId = result.Value.ChannelPublisherId;
            })));
        }
        await Task.WhenAll(tasks);
        tasks.Clear();

        // Negative
        foreach (var user in _testSource.Users.Where(u => !u.Fail))
        {
            tasks.AddRange(user.Channels.Where(c => c.Fail).Select(channel => Task.Run(async () =>
            {
                var result = await user.Client.CreateChannel(channel).ConfigureAwait(false);
                Assert.That(result.IsSuccessStatusCode, Is.False);
            })));
        }
        await Task.WhenAll(tasks);
        tasks.Clear();

        // Positive
        await Task.WhenAll(_testSource.Users.Where(u => !u.Fail).Select(user => Task.Run(async () =>
        {
            var result = await user.Client.GetAllChannels().ConfigureAwait(false);
            Assert.That(result.IsSuccessStatusCode, Is.True);
            Assert.That(result.Value, Has.Length.EqualTo(user.Channels.Count(c => !c.Fail)));
            foreach (var channel in result.Value)
            {
                _channelDetails.Add(channel);
            }
        })));
        await Task.WhenAll(tasks);
    }

    [Test, Order(3)]
    public async Task ClientConnectionTest()
    {
        var channelDict = _channelDetails.ToDictionary(cd => cd.ChannelName, cd => cd);
        foreach (var clientSource in _testSource.Clients)
        {
            clientSource.ChannelSubscriberId = channelDict.TryGetValue(clientSource.ChannelName, out var cd) ? cd.ChannelSubscriberId : Guid.NewGuid();
            var wbsktConfigurationCustom = new WbsktConfigurationCustom
            {
                ChannelDetails = new Client.Configurations.ChannelDetails()
                {
                    Secret = clientSource.ChannelSecret,
                    SubscriberId = clientSource.ChannelSubscriberId
                },
                ClientDetails = new ClientDetails
                {
                    Name = clientSource.ClientName,
                    UniqueId = clientSource.ClientUniqueId
                },
                WbsktServerAddress = "wbskt.com"
            };

            var payload = _testSource.Users
                .Where(u => !u.Fail)
                .SelectMany(u => u.Channels)
                .Where(c => !c.Fail)
                .First(c => c.ChannelSubscriberId == clientSource.ChannelSubscriberId).Payloads
                .ToList();

            clientSource.Listener = new WbsktListener(wbsktConfigurationCustom, new Mock<ILogger<WbsktListener>>().Object);
            clientSource.Listener!.ReceivedPayloadEvent += clientPayload =>
            {
                clientSource.ReceivedPayloads.Add(clientPayload.Data);
                if (payload.Count == clientSource.ReceivedPayloads.Count)
                {
                    clientSource.CancellationToken.Cancel();
                }
            };
        }

        var listeningTask = Task.WhenAll(_testSource.Clients.Select(clientConn => Task.Run(async () =>
        {
            clientConn.CancellationToken.CancelAfter(20 * 1000);
            await clientConn.Listener!.StartListeningAsync(clientConn.CancellationToken.Token).ConfigureAwait(false);
        }))).ConfigureAwait(false);

        await Task.Delay(5 * 1000);
        foreach (var user in _testSource.Users.Where(u => !u.Fail))
        {
            foreach (var channel in user.Channels.Where(c => !c.Fail))
            {
                foreach (var payload in channel.Payloads)
                {
                    await user.Client.DispatchMessage(channel.ChannelPublisherId, new ClientPayload() { Data = payload });
                }
            }
        }

        await listeningTask;

        foreach (var clientSource in _testSource.Clients.Where(c => !c.Fail))
        {
            var payload = _testSource.Users
                .Where(u => !u.Fail)
                .SelectMany(u => u.Channels)
                .Where(c => !c.Fail)
                .First(c => c.ChannelSubscriberId == clientSource.ChannelSubscriberId).Payloads
                .ToArray();
            Assert.That(clientSource.ReceivedPayloads.ToArray(), Is.EqualTo(payload));
        }
    }
}
