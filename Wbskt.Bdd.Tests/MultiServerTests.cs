using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text.Json;
using Wbskt.Bdd.Tests.ContractWrappers;
using Wbskt.Common.Contracts;

namespace Wbskt.Bdd.Tests;

public class MultiServerTests
{
    private CoreServerClient _commonClient;
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
                var result = await user.Client.CreateChannel(channel);
                Assert.That(result.IsSuccessStatusCode, Is.True);
            })));
        }
        await Task.WhenAll(tasks);
        tasks.Clear();

        // Negative
        foreach (var user in _testSource.Users.Where(u => !u.Fail))
        {
            tasks.AddRange(user.Channels.Where(c => c.Fail).Select(channel => Task.Run(async () =>
            {
                var result = await user.Client.CreateChannel(channel);
                Assert.That(result.IsSuccessStatusCode, Is.False);
            })));
        }
        await Task.WhenAll(tasks);
    }

    // [OneTimeTearDown]
    public void Cleanup()
    {
        using var connection = new SqlConnection("#############################################");
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "DELETE FROM dbo.Channels";
        command.ExecuteNonQuery();

        using var command2 = connection.CreateCommand();
        command.CommandType = CommandType.Text;
        command.CommandText = "DELETE FROM dbo.Users";
        command.ExecuteNonQuery();
    }
}
