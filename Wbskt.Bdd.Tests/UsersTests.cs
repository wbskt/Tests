using System.Net;
using System.Text.Json;
using Wbskt.Bdd.Tests.ContractWrappers;
using Wbskt.Bdd.Tests.Utils;
using Wbskt.Common.Contracts;

namespace Wbskt.Bdd.Tests;

public class UsersTests
{
    private CoreServerClient _commonClient;
    private TestSource _testSource;

    [OneTimeSetUp]
    public void Setup()
    {
        var json = File.ReadAllText($"{nameof(UsersTests)}.json");
        json = PlaceholderReplacer.ReplacePlaceholders(json);
        _testSource = JsonSerializer.Deserialize<TestSource>(json) ?? new TestSource();
        _commonClient = new CoreServerClient();
    }

    [Test, Order((int)Order.UserCreationTest)]
    public async Task UserCreationTest()
    {
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

    [Test, Order((int)Order.UserCreationFailsTest)]
    public async Task UserCreationFailsTest()
    {
        var result = await _commonClient.RegisterUser(_testSource.Users.First());
        Assert.That(result.IsSuccessStatusCode, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test, Order((int)Order.UserGetTokenTest)]
    public async Task UserGetTokenTest()
    {
        await Task.WhenAll(_testSource.Users.Select(user => Task.Run(async () =>
        {
            var result = await _commonClient.GetToken(user);
            Assert.That(result.IsSuccessStatusCode, Is.True);
            user.Token = result.Value ?? string.Empty;
            user.Client.SetUserToken(user.Token);
        })));
    }

    [Test, Order((int)Order.UserGetTokenFailTest)]
    public async Task UserGetTokenFailTest()
    {
        var result = await _commonClient.GetToken(new UserLoginRequest { EmailId = _testSource.Users.First().EmailId, Password = "wrong-password"});
        Assert.That(result.IsSuccessStatusCode, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test, Order((int)Order.UserTokenUsageTest)]
    public async Task UserTokenUsageTest()
    {
        await Task.WhenAll(_testSource.Users.Select(user => Task.Run(async () =>
        {
            var result = await user.Client.GetAllChannels();
            Assert.That(result.IsSuccessStatusCode, Is.True);
        })));
    }

    [Test, Order((int)Order.UserTokenUsageFailTest)]
    public async Task UserTokenUsageFailTest()
    {
        var user = _testSource.Users.First();
        user.Client.SetUserToken(user.Token + "bad-signature");
        var result = await user.Client.GetAllChannels();
        Assert.That(result.IsSuccessStatusCode, Is.False);
        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        user.Client.SetUserToken(user.Token);
        result = await user.Client.GetAllChannels();
        Assert.That(result.IsSuccessStatusCode, Is.True);
    }

    private enum Order
    {
        UserCreationTest = 1,
        UserCreationFailsTest,
        UserGetTokenTest,
        UserGetTokenFailTest,
        UserTokenUsageTest,
        UserTokenUsageFailTest
    }
}
