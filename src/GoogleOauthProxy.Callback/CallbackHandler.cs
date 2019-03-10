using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Kralizek.Lambda;
using Microsoft.Extensions.Logging;
using Amazon.SimpleSystemsManagement.Model;
using Newtonsoft.Json;

namespace GoogleOauthProxy.Callback
{
    public class CallbackHandler : IRequestResponseHandler<APIGatewayProxyRequest, APIGatewayProxyResponse>
    {
        private readonly ILogger<CallbackHandler> _logger;

        public CallbackHandler(ILogger<CallbackHandler> logger)
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

            var clientSecrets = new ClientSecrets
            {
                ClientId = googleOauthClientId.Parameter.Value,
                ClientSecret = googleOauthClientSecret.Parameter.Value
            };

            var state = JsonConvert.DeserializeObject<State>(input.QueryStringParameters["state"]);

            var authorisationFlow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer { ClientSecrets = clientSecrets });

            return new APIGatewayProxyResponse
            {
                StatusCode = 303,
                Headers = new Dictionary<string, string>
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Location"] = $"{state.ReturnUrl}?code={input.QueryStringParameters["code"]}&state={state.OauthState}"
                }
            };
        }
    }
}
