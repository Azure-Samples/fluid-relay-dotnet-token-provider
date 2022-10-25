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

namespace dotnet_tokenprovider_functionsapp
{
    public static class AzureFunction
    {
        // NOTE: retrieve the key from a secure location.
        private static readonly string key = "myTenantKey";

        [FunctionName("AzureFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req, ILogger log)
        {
            string content = await new StreamReader(req.Body).ReadToEndAsync();
            JObject body = !string.IsNullOrEmpty(content) ? JObject.Parse(content) : null;

            string tenantId = (req.Query["tenantId"].ToString() ?? body["tenantId"]?.ToString()) as string;
            string documentId = (req.Query["documentId"].ToString() ?? body["documentId"]?.ToString() ?? null) as string;
            string userId = (req.Query["userId"].ToString() ?? body["userId"]?.ToString()) as string;
            string userName = (req.Query["userName"].ToString() ?? body["userName"]?.ToString()) as string;
            string[] scopes = (req.Query["scopes"].ToString().Split(",") ?? body["scopes"]?.ToString().Split(",") ?? null) as string[];

            if (string.IsNullOrEmpty(tenantId))
            {
                return new BadRequestObjectResult("No tenantId provided in query params");
            }

            if (string.IsNullOrEmpty(key))
            {
                return new NotFoundObjectResult($"No key found for the provided tenantId: ${tenantId}");
            }

            //  If a user is not specified, the token will not be associated with a user, and a randomly generated mock user will be used instead
            var user = (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(userId)) ? 
                new { name = Guid.NewGuid().ToString(), id = Guid.NewGuid().ToString() } :
                new { name = userName, id = userId };

            // Will generate the token and returned by an ITokenProvider implementation to use with the AzureClient.
            string token = GenerateToken(
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
            string docId = documentId ?? "";
            DateTime now = DateTime.Now;

            SigningCredentials credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            JwtHeader header = new JwtHeader(credentials);
            JwtPayload payload = new JwtPayload
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

            JwtSecurityToken token = new JwtSecurityToken(header, payload);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
