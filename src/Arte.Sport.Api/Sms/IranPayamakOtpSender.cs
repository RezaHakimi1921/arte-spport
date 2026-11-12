using System.Net.Http.Json;

namespace Arte.Sport.Api.Sms;

public sealed class IranPayamakOtpSender(HttpClient http, IConfiguration config) : ISmsSender
{
    public async Task SendOtpAsync(string mobile, string code, CancellationToken ct)
    {
        var key = config["Sms:IranPayamak:ApiKey"];
        var line = config["Sms:IranPayamak:LineNumber"];
        var pattern = config["Sms:IranPayamak:Pattern"];
        var param = config["Sms:IranPayamak:CodeParam"];
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(pattern))
            throw new SmsNotConfiguredException();

        using var message = new HttpRequestMessage(HttpMethod.Post, "ws/v1/sms/pattern");
        message.Headers.TryAddWithoutValidation("Api-Key", key);
        message.Content = JsonContent.Create(new
        {
            code = pattern,
            recipient = mobile,
            attributes = new Dictionary<string, string> { [string.IsNullOrWhiteSpace(param) ? "code" : param] = code },
            line_number = line,
            number_format = "english",
        });
        using var response = await http.SendAsync(message, ct);
        if (!response.IsSuccessStatusCode) throw new SmsNotConfiguredException();
    }
}
