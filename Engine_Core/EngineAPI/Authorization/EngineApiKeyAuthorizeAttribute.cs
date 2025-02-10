using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Engine_API.Authorization;

public class EngineApiKeyAuthorizeAttribute : Attribute, IAuthorizationFilter
{

    private const string ApiKeyHeaderName = "engine-Key";
    private readonly string _apiKey;        
    public EngineApiKeyAuthorizeAttribute(string apiKey)
    {
            _apiKey = apiKey;   
    }
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        var extractedApiKey = request.Headers["Ocp-Apim-Subscription-Key"].FirstOrDefault()
                              ?? request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(extractedApiKey) || extractedApiKey != _apiKey)
        {
            context.Result = new UnauthorizedResult();
            return;
        }
    }

}

