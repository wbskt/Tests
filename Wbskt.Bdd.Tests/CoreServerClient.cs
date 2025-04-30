using System.Net.Http.Headers;
using System.Net.Http.Json;
using Wbskt.Common.Contracts;

namespace Wbskt.Bdd.Tests;

public class CoreServerClient
{
    private readonly HttpClient _client = new();
    private const string Login = "api/users/login";
    private const string Register = "api/users/register";
    private const string Channels = "api/channels";
    private const string Dispatch = "api/channels/{0}/dispatch";

    public CoreServerClient(string baseAddress = "https://wbskt.com")
    {
        _client.BaseAddress = new Uri(baseAddress);
    }

    public void SetUserToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<ApiResult<string>> RegisterUser(UserRegistrationRequest request)
    {
        var result = await _client.PostAsync(Register, JsonContent.Create(request));
        return await result.GetApiResult<string>();
    }

    public async Task<ApiResult<string>> GetToken(UserLoginRequest request)
    {
        var result = await _client.PostAsync(Login, JsonContent.Create(request));
        return await result.GetApiResult<string>();
    }

    public async Task<ApiResult<ChannelDetails>> CreateChannel(ChannelRequest request)
    {
        var result = await _client.PostAsync(Channels, JsonContent.Create(request));
        return await result.GetApiResult<ChannelDetails>();
    }

    public async Task<ApiResult<ChannelDetails[]>> GetAllChannels()
    {
        var result = await _client.GetAsync(Channels);
        return await result.GetApiResult<ChannelDetails[]>();
    }

    public async Task<ApiResult<string>> DispatchMessage(Guid publisherId, ClientPayload payload)
    {
        var result = await _client.PostAsync(string.Format(Dispatch, publisherId), JsonContent.Create(payload));
        return await result.GetApiResult<string>();
    }
}
