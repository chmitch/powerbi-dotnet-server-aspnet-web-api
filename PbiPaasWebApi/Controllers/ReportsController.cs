using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.PowerBI.Security;
using Microsoft.Rest;
using PbiPaasWebApi.Models;
using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Cors;
using Microsoft.IdentityModel.Clients.ActiveDirectory;


namespace PbiPaasWebApi.Controllers
{
    [RoutePrefix("api")]
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    public class ReportsController : ApiController
    {
        private string Username;
        private string Password;
        private string AuthorityUrl;
        private string ResourceUrl;
        private string ClientId;
        private string ApiUrl;
        private string GroupId;

        public ReportsController()
        {
            this.Username = ConfigurationManager.AppSettings["pbiUsername"];
            this.Password = ConfigurationManager.AppSettings["pbiPassword"];
            this.AuthorityUrl = ConfigurationManager.AppSettings["authorityUrl"];
            this.ResourceUrl = ConfigurationManager.AppSettings["resourceUrl"];
            this.ClientId = ConfigurationManager.AppSettings["clientId"];
            this.ApiUrl = ConfigurationManager.AppSettings["apiUrl"];
            this.GroupId = ConfigurationManager.AppSettings["groupId"];
        }
        // GET: api/Reports
        [HttpGet]
        public async Task<IHttpActionResult> Get([FromUri]bool includeTokens = false)
        {
            // Create a user password cradentials.
            var credential = new UserPasswordCredential(Username, Password);

            // Authenticate using created credentials
            var authenticationContext = new AuthenticationContext(AuthorityUrl);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(ResourceUrl, ClientId, credential);
            var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");

            using (var client = new PowerBIClient(new Uri(ApiUrl), tokenCredentials))
            {
                // Get a list of reports.
                var reports = await client.Reports.GetReportsInGroupAsync(GroupId);

                var reportsWithTokens = reports.Value
                    .Select(report =>
                    {
                        string accessToken = null;
                        if (includeTokens)
                        {
                            // Generate Embed Token for reports without effective identities.
                            GenerateTokenRequest generateTokenRequestParameters;
                            generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                            var tokenResponse = client.Reports.GenerateTokenInGroupAsync(GroupId, report.Id, generateTokenRequestParameters).Result;
                            accessToken = tokenResponse.Token;
                        }

                        return new ReportWithToken(report, accessToken);
                    }).ToList();
                
                return Ok(reportsWithTokens);
            }
        }

        // GET: api/Reports/386818d4-f37f-485f-b750-08f982b0c146
        [HttpGet]
        public async Task<IHttpActionResult> Get(string id)
        {
            // Create a user password cradentials.
            var credential = new UserPasswordCredential(Username, Password);

            // Authenticate using created credentials
            var authenticationContext = new AuthenticationContext(AuthorityUrl);
            var authenticationResult = await authenticationContext.AcquireTokenAsync(ResourceUrl, ClientId, credential);
            var tokenCredentials = new TokenCredentials(authenticationResult.AccessToken, "Bearer");

            using (var client = new PowerBIClient(new Uri(ApiUrl), tokenCredentials))
            {
                // Get a list of reports.
                var reports = await client.Reports.GetReportsInGroupAsync(GroupId);

                var report = reports.Value.FirstOrDefault(r => r.Id == id);
                
                // Generate Embed Token for reports without effective identities.
                GenerateTokenRequest generateTokenRequestParameters;
                generateTokenRequestParameters = new GenerateTokenRequest(accessLevel: "view");
                var tokenResponse = await client.Reports.GenerateTokenInGroupAsync(GroupId, report.Id, generateTokenRequestParameters);

                var reportWithToken = new ReportWithToken(report, tokenResponse.Token);

                return Ok(reportWithToken);
            }
        }
        /*
        [HttpGet]
        public async Task<IHttpActionResult> SearchByName([FromUri]string query, [FromUri]bool includeTokens = false)
        {
            if(string.IsNullOrWhiteSpace(query))
            {
                return Ok(Enumerable.Empty<ReportWithToken>());
            }

            var credentials = new TokenCredentials(workspaceCollectionAccessKey, "AppKey");
            using (var client = new PowerBIClient(new Uri(apiUrl), credentials))
            {
                var reportsResponse = await client.Reports.GetReportsAsync(this.workspaceCollectionName, this.workspaceId.ToString());
                var reports = reportsResponse.Value.Where(r => r.Name.ToLower().StartsWith(query.ToLower()));

                var reportsWithTokens = reports
                    .Select(report =>
                     {
                         string accessToken = null;
                         if (includeTokens)
                         {
                             var embedToken = PowerBIToken.CreateReportEmbedToken(this.workspaceCollectionName, this.workspaceId.ToString(), report.Id);
                             accessToken = embedToken.Generate(this.workspaceCollectionAccessKey);
                         }

                         return new ReportWithToken(report, accessToken);
                     })
                    .ToList();

                return Ok(reportsWithTokens);
            }
        }
        */
    }
}
