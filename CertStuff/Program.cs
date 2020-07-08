using System;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Net.Http;
using System.Net.Http.Headers;

namespace CertStuff
{
    class Program
    {
        static void Main(string[] args)
        {
            var tenantId = "b183855b-32e9-470a-9b14-726979b79ac1";
            var resource = "https://contoso.sharepoint.com";
            var clientId = "61e18c45-190b-4928-98eb-f2a193dd91a2";
            var accessToken = GetS2SAccessToken("https://login.microsoftonline.com/" + tenantId, resource, clientId).Result;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json;odata=nometadata");

                var rawJson = client.GetStringAsync("https://contoso.sharepoint.com/_api/web?$select=Title").Result;
                Console.WriteLine(rawJson);
                Console.Read();
            }
        }

        public static async Task<string> GetS2SAccessToken(string authority, string resource, string clientId)
        {
            var certPath = Path.Combine(GetCurrentDirectoryFromExecutingAssembly(), "cert.pfx");
            var certfile = File.OpenRead(certPath);
            var certificateBytes = new byte[certfile.Length];
            certfile.Read(certificateBytes, 0, (int)certfile.Length);
            var cert = new X509Certificate2(
                certificateBytes,
                "rencore",
                X509KeyStorageFlags.Exportable |
                X509KeyStorageFlags.MachineKeySet |
                X509KeyStorageFlags.PersistKeySet);

            var certificate = new CertStuff.ClientAssertionCertificate(clientId, cert);
            AuthenticationContext context = new AuthenticationContext(authority);
            AuthenticationResult authenticationResult = await context.AcquireTokenAsync(resource, certificate);
            return authenticationResult.AccessToken;
        }

        public static string GetCurrentDirectoryFromExecutingAssembly()
        {
            var codeBase = typeof(Program).GetTypeInfo().Assembly.CodeBase;

            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            return Path.GetDirectoryName(path);
        }
    }
}
