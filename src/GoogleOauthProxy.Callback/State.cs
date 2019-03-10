using Newtonsoft.Json;

namespace GoogleOauthProxy.Callback
{
	class State
	{
		[JsonProperty("returnUrl")]
		public string ReturnUrl { get; set; }

		[JsonProperty("state")]
		public string OauthState { get; set; }
	}
}
