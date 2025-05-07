using System.Net;
using System.Text.Json;
using Wbskt.Bdd.Tests.ContractWrappers;
using Wbskt.Bdd.Tests.Utils;

namespace Wbskt.Bdd.Tests;

public class ChannelTests
{
    private CoreServerClient _commonClient;
    private TestSource _testSource;

    [OneTimeSetUp]
    public async Task Setup()
    {
        var json = await File.ReadAllTextAsync($"{nameof(ChannelTests)}.json");
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
    }

    [Test, Order((int)Order.ChannelCreationTest)]
    public async Task ChannelCreationTest()
    {
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

    [Test, Order((int)Order.ChannelCreationFailTest)]
    public async Task ChannelCreationFailTest()
    {
        var firstUser = _testSource.Users.First();
        var firstChannel = firstUser.Channels.First();
        var result = await firstUser.Client.CreateChannel(firstChannel);
        Assert.That(result.IsSuccessStatusCode, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test, Order((int)Order.ChannelGetAllTest)]
    public async Task ChannelGetAllTest()
    {
        await Task.WhenAll(_testSource.Users.Select(user => Task.Run(async () =>
        {
            var result = await user.Client.GetAllChannels();
            Assert.That(result.IsSuccessStatusCode, Is.True);
            Assert.That(result.Value?.Length, Is.EqualTo(user.Channels.Length));
        })));
    }

    private enum Order
    {
        ChannelCreationTest = 1,
        ChannelCreationFailTest,
        ChannelGetAllTest
    }
}
