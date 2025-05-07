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
    }
}
