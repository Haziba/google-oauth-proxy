using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Web;
using System.Threading;
using Amazon.SimpleSystemsManagement.Model;
using Newtonsoft.Json;
using System.Linq;

namespace GoogleOauthProxy.Login
{
    public class LoginHandler : IRequestResponseHandler<APIGatewayProxyRequest, APIGatewayProxyResponse>
    {
        private readonly ILogger<LoginHandler> _logger;

        public LoginHandler(ILogger<LoginHandler> logger)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }
            _logger = logger;
        }

        public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest input, ILambdaContext context)
        {
            var ssmClient = new Amazon.SimpleSystemsManagement.AmazonSimpleSystemsManagementClient();

            var googleOauthClientId = await ssmClient.GetParameterAsync(new GetParameterRequest { Name = "/GoogleOauthProxy/GoogleClientId" });
            var googleOauthClientSecret = await ssmClient.GetParameterAsync(new GetParameterRequest { Name = "/GoogleOauthProxy/GoogleSecretKey" });
            var googleOauthUrlRegex = await ssmClient.GetParameterAsync(new GetParameterRequest { Name = "/GoogleOauthProxy/UrlRegex" });

            var returnUrl = input.QueryStringParameters["redirectUri"];

            var urlRegex = new Regex(googleOauthUrlRegex.Parameter.Value);

            if (!urlRegex.IsMatch(returnUrl))
                return new APIGatewayProxyResponse
                {
                    StatusCode = 403
                };

            var clientSecrets = new ClientSecrets
            {
                ClientId = googleOauthClientId.Parameter.Value,
                ClientSecret = googleOauthClientSecret.Parameter.Value
            };

            var authorisationFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[] { "email" },
                UserDefinedQueryParams = new[] { new KeyValuePair<string, string>("hd", "myunidays.com") }
            });

            var authorisationApp = new AuthorizationCodeWebApp(authorisationFlow,
                "https://" + input.Headers["Host"] + "/Prod/google-oauth/callback",
                JsonConvert.SerializeObject(new { returnUrl, state = input.QueryStringParameters["state"] }));

            var result = await authorisationApp.AuthorizeAsync(string.Empty, CancellationToken.None);

            var location = result.RedirectUri;

            return new APIGatewayProxyResponse
            {
                StatusCode = 303,
                Headers = new Dictionary<string, string>
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Location"] = location
                }
            };
        }
    }
}
