using System.Net;
using System.Net.Http.Json;

namespace Wbskt.Bdd.Tests.Utils;

public class ApiResult<T>
{
    public HttpStatusCode StatusCode { get; set; }
    public bool IsSuccessStatusCode { get; set; }
    public string ErrorMessage {  get; set; } = string.Empty;

    public T? Value { get; set; }
}

public static class ApiHelper
{
    public static async Task<ApiResult<TS>> GetApiResult<TS>(this HttpResponseMessage response)
    {
        var result = new ApiResult<TS>
        {
            StatusCode = response.StatusCode,
            IsSuccessStatusCode = response.IsSuccessStatusCode
        };
        if (response.IsSuccessStatusCode == false)
        {
            result.ErrorMessage = response.ReasonPhrase ?? string.Empty;
            return result;
        }

        try
        {
            if (typeof(TS) == typeof(string))
            {
                result.Value = (TS)(object)await response.Content.ReadAsStringAsync();
            }
            else
            {
                result.Value = await response.Content.ReadFromJsonAsync<TS>() ?? throw new InvalidOperationException();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return result;
    }
}
