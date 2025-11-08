using System.Web;

namespace PickemsPlanter.APIs
{
	public interface ILoginAPI
	{
		string Login(string returnUrl, string realm);
		Task<string> ValidateLoginAsync(IQueryCollection query);
	}

	public class LoginAPI(HttpClient client) : ILoginAPI
	{
		private readonly HttpClient _client = client;

		public string Login(string returnUrl, string realm)
		{
			var queryParams = new Dictionary<string, string>
			{
				["openid.ns"] = "http://specs.openid.net/auth/2.0",
				["openid.mode"] = "checkid_setup",
				["openid.return_to"] = returnUrl,
				["openid.realm"] = realm,
				["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select",
				["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select"
			};

			string queryString = string.Join("&", queryParams.Select(kv => $"{kv.Key}={HttpUtility.UrlEncode(kv.Value)}"));

			return $"{_client.BaseAddress}/openid/login?{queryString}";
		}

		public async Task<string> ValidateLoginAsync(IQueryCollection query)
		{
			var values = new Dictionary<string, string>();

			foreach (var kv in query)
			{
				values[kv.Key] = kv.Value!;
			}

			values["openid.mode"] = "check_authentication";

			HttpRequestMessage request = new(HttpMethod.Post, "openid/login")
			{
				Content = new FormUrlEncodedContent(values)
			};

			var response = await _client.SendAsync(request);

			return await response.Content.ReadAsStringAsync();
		}
	}
}
