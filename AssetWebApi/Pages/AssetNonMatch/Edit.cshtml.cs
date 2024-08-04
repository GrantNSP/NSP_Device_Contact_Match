using Microsoft.AspNetCore.Mvc.RazorPages;
using assetWebApi.Pages.Asset;
using System.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text;

namespace assetWebApi.Pages.AssetNonMatch
{
    public class EditModel : PageModel
    {
        public assetData assetInput = new assetData();
        public string errorMessage = "";
        public string successMessage = "";
        public string name = "";
        public dynamic? contactData;

        [Display(Name = "User Role")]
        public int SelectedUserRoleId { get; set; }
        public IEnumerable<SelectListItem> contacts { get; set; }
        public void OnGet()
        {
            string Keyid = Request.Query["Keyid"];
            string companyId = "";

            // Get all data
            try
            {
                string connString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["DefaultConnection"];

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT [N-Central ID],[AssetName],[CurrentSyncUser],[LastSyncUser],[ContactID],[ContactName],[Company ID] FROM [Asset].[dbo].[AssetContact] WHERE [Key id] = " + Keyid, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                assetInput.nCentralId = reader.GetString(0);
                                assetInput.assetName = reader.GetString(1);
                                assetInput.currentSync = reader.GetString(2);
                                assetInput.lastSync = reader.GetString(3);
                                assetInput.contactId = reader.GetString(4);
                                assetInput.contactName = reader.GetString(5);
                                companyId= reader.GetString(6);
                            }
                        }
                    }
                    conn.Close();
                }

                // Get data pertaining name and id of contact
                getData(companyId).Wait();
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message + " KeyID:" + Keyid;
                companyId = "";
            }

            // put contacts in an inumerable list
            contacts = GeContacts();
        }

        private async Task getData(string companyId)
        {
            string userName = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["AutotaskUserNameConnection"];
            string secret = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["AutotaskSecretConnection"];
            string apiIntegrationCode = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["AutotaskIntegrationConnection"];

            string firstName;
            string lastName;

            string contactUrl = "https://webservices6.autotask.net/ATServicesRest/V1.0/Contacts/query?search={\"IncludeFields\": [\"id\",\"firstName\", \"lastName\"], \"filter\":[{\"op\":\"eq\", \"field\":\"companyID\",\"value\":\"" + companyId + "\"},{\"op\":\"eq\", \"field\":\"isActive\",\"value\":\"1\"}]}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("UserName", userName);
                client.DefaultRequestHeaders.Add("Secret", secret);
                client.DefaultRequestHeaders.Add("ApiIntegrationCode", apiIntegrationCode);

                try
                {
                    var response = await client.GetAsync(contactUrl);

                    var content = await response.Content.ReadAsStringAsync();

                    contactData = JObject.Parse(content);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        private IEnumerable<SelectListItem> GeContacts()
        {
            var roles = new List<SelectListItem>();

            for (int i = 0; i < contactData.items.Count; i++)
            {
                roles.Add(new SelectListItem { Value = contactData.items[i].id, Text = contactData.items[i].firstName + " " + contactData.items[i].lastName });
            }

            return roles;
        }

        private async Task sendData(string contactId, string nCentralAssetId)
        {
            // get name from AutotaskAPI
            string userName = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["AutotaskUserNameConnection"];
            string secret = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["AutotaskSecretConnection"];
            string apiIntegrationCode = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["AutotaskIntegrationConnection"];

            string firstName;
            string lastName;

            string contactUrl = "https://webservices6.autotask.net/ATServicesRest/V1.0/Contacts/query?search={\"IncludeFields\": [\"firstName\", \"lastName\"], \"filter\":[{\"op\":\"eq\", \"field\":\"id\",\"value\":\"" + contactId + "\"}]}";

            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("UserName", userName);
                client.DefaultRequestHeaders.Add("Secret", secret);
                client.DefaultRequestHeaders.Add("ApiIntegrationCode", apiIntegrationCode);

                try
                {
                    var response = await client.GetAsync(contactUrl);

                    var content = await response.Content.ReadAsStringAsync();

                    dynamic data = JObject.Parse(content);

                    if (data.items.Count == 1)
                    {
                        name = data.items[0].firstName + " " + data.items[0].lastName;
                    }
                    else
                    {
                        errorMessage = "Error contact don't exist";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                try
                {
                    string assetIdUrl = "https://webservices6.autotask.net/ATServicesRest/V1.0/ConfigurationItems/query?search={ \"filter\":[ {\"op\":\"eq\", \"field\":\"N-central Device ID\",\"udf\":true,\"value\":\"" + nCentralAssetId + "\"}]}";

                    var response2 = await client.GetAsync(assetIdUrl);

                    var content2 = await response2.Content.ReadAsStringAsync();

                    dynamic assetData = JObject.Parse(content2);

                    if(assetData.items.Count > 0)
                    {
                        int ATID = assetData.items[0].id;

                        updateAsset ua = new updateAsset()
                        {
                            id = ATID,
                            contactId = contactId
                        };

                        string stringJson = JsonConvert.SerializeObject(ua, Formatting.Indented);

                        var stringContent = new StringContent(stringJson, Encoding.UTF8, "application/json");

                        Console.WriteLine(stringJson);

                        string assetUrl = "https://webservices6.autotask.net/ATServicesRest/V1.0/ConfigurationItems";

                        response2 = await client.PatchAsync(assetUrl, stringContent);

                        content2 = await response2.Content.ReadAsStringAsync();

                        Console.WriteLine(content2);
                    } 
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public void OnPost()
        {
            string Keyid = Request.Query["Keyid"];

            assetInput.contactId = Request.Form["COID"];

            assetInput.nCentralId = Request.Form["NCID"];

            sendData(assetInput.contactId, assetInput.nCentralId).Wait();

            try
            {
                string connString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["DefaultConnection"];

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [ContactID]='" + assetInput.contactId + "',[ContactName]='" + name + "' WHERE [Key id]=" + Keyid, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return;
            }

            Response.Redirect("/", false);
        }
    }
    public class contacts
    {
        public int contactId;
        public string contactName;
    }

    public class updateAsset
    {
        public int id;
        public string? contactId;
    }
}
