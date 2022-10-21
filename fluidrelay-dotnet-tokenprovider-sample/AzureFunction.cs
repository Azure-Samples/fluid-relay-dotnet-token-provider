using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace fluidrelay_dotnet_tokenprovider_sample
{
    public static class AzureFunction
    {
        // NOTE: retrieve the key from a secure location.
        private static readonly string key = "myTenantKey";

        [FunctionName("AzureFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            var content = await new StreamReader(req.Body).ReadToEndAsync();
            var body = !string.IsNullOrEmpty(content) ? JObject.Parse(content) : null;

            // tenantId, documentId, userId and userName are required parameters
            var tenantId = (req.Query["tenantId"].ToString() ?? body["tenantId"]?.ToString()) as string;
            var documentId = (req.Query["documentId"].ToString() ?? body["documentId"]?.ToString() ?? null) as string;
            var userId = (req.Query["userId"].ToString() ?? body["userId"]?.ToString()) as string;
            var userName = (req.Query["userName"].ToString() ?? body["userName"]?.ToString()) as string;
            var scopes = (req.Query["scopes"].ToString().Split(",") ?? body["scopes"]?.ToString().Split(",") ?? null) as string[];

            if (string.IsNullOrEmpty(tenantId))
            {
                return new BadRequestObjectResult("No tenantId provided in query params");
            }

            if (string.IsNullOrEmpty(key))
            {
                return new NotFoundObjectResult($"No key found for the provided tenantId: ${tenantId}");
            }

            var user = new { name = userName, id = userId };

            // Will generate the token and returned by an ITokenProvider implementation to use with the AzureClient.
            var token = GenerateToken(
                tenantId,
                key,
                scopes ?? new string[] { "doc:read", "doc:write", "summary:write" },
                documentId,
                user
            );

            return new OkObjectResult(token);
        }

        private static string GenerateToken(string tenantId, string key, string[] scopes, string? documentId, dynamic user, int lifetime = 3600, string ver = "1.0")
        {
            var docId = documentId ?? "";
            var now = DateTime.Now;

            var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var header = new JwtHeader(credentials);
            var payload = new JwtPayload
            {
                { "documentId", docId },
                { "scopes", scopes },
                { "tenantId", tenantId },
                { "user", user },
                { "iat", new DateTimeOffset(now).ToUnixTimeSeconds() },
                { "exp", new DateTimeOffset(now.AddSeconds(lifetime)).ToUnixTimeSeconds() },
                { "ver", ver },
                { "jti", Guid.NewGuid() }
            };

            var token = new JwtSecurityToken(header, payload);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
