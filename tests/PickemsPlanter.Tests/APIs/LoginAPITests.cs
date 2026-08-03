using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System.Net;
using System.Web;
using Xunit;

namespace PickemsPlanter.APIs;

public class LoginAPITests
{
	private static (LoginAPI api, FakeHttpMessageHandler handler) MakeApi(Func<HttpRequestMessage, HttpResponseMessage> respond)
	{
		var handler = new FakeHttpMessageHandler(respond);
		var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://steamcommunity.com") };

		return (new LoginAPI(httpClient), handler);
	}

	[Fact]
	public void Login_BuildsAnOpenIdCheckidSetupUrl_CarryingTheReturnUrlAndRealm()
	{
		var (api, _) = MakeApi(_ => new HttpResponseMessage(HttpStatusCode.OK));

		string url = api.Login(returnUrl: "https://pickemsplanter.example/Profile/SteamCallback", realm: "https://pickemsplanter.example");

		// HttpClient.BaseAddress.ToString() normalizes to a trailing slash, so this string-concats
		// to a double slash before "openid" — harmless (Steam's endpoint tolerates it) but real,
		// pre-existing behavior, not a typo in this assertion.
		Assert.StartsWith("https://steamcommunity.com//openid/login?", url);
		Assert.Contains("openid.mode=checkid_setup", url);
		Assert.Contains($"openid.return_to={HttpUtility.UrlEncode("https://pickemsplanter.example/Profile/SteamCallback")}", url);
		Assert.Contains($"openid.realm={HttpUtility.UrlEncode("https://pickemsplanter.example")}", url);
	}

	[Fact]
	public async Task ValidateLoginAsync_OverridesTheModeToCheckAuthentication_AndForwardsEveryOtherParam()
	{
		var (api, handler) = MakeApi(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("is_valid:true") });

		var query = new QueryCollection(new Dictionary<string, StringValues>
		{
			["openid.mode"] = "id_res",
			["openid.claimed_id"] = "https://steamcommunity.com/openid/id/76500000000000001",
			["openid.sig"] = "some-signature"
		});

		await api.ValidateLoginAsync(query);

		var form = HttpUtility.ParseQueryString(handler.LastRequestBody!);
		Assert.Equal("check_authentication", form["openid.mode"]);
		Assert.Equal("https://steamcommunity.com/openid/id/76500000000000001", form["openid.claimed_id"]);
		Assert.Equal("some-signature", form["openid.sig"]);
		Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
	}

	[Fact]
	public async Task ValidateLoginAsync_ReturnsTheRawResponseBody()
	{
		var (api, _) = MakeApi(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("ns:http://specs.openid.net/auth/2.0\nis_valid:true") });

		string result = await api.ValidateLoginAsync(new QueryCollection());

		Assert.Equal("ns:http://specs.openid.net/auth/2.0\nis_valid:true", result);
	}
}
