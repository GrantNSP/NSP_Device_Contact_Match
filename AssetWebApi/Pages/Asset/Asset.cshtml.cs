using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace assetWebApi.Pages.Asset
{
    public class AssetModel : PageModel
    {
        public List<assetData> listAsset = new List<assetData>();
        public void OnGet()
        {
            try
            {
                string connString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["DefaultConnection"];

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT [Key Id],[N-Central ID],[CompanyName],[AssetName],[CurrentSyncUser],[LastSyncUser],[ContactID],[ContactName] FROM [Asset].[dbo].[AssetContact]", conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                assetData cAsset = new assetData();
                                cAsset.Keyid = reader.GetInt32(0);
                                cAsset.nCentralId = reader.GetString(1);
                                cAsset.companyName= reader.GetString(2);
                                cAsset.assetName = reader.GetString(3);
                                cAsset.currentSync= reader.GetString(4);
                                cAsset.lastSync= reader.GetString(5);
                                cAsset.contactId = reader.GetString(6);
                                cAsset.contactName = reader.GetString(7);

                                listAsset.Add(cAsset);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }

    public class assetData
    {
        public int Keyid;
        public string? nCentralId;
        public string? companyName;
        public string? assetName;
        public string? currentSync;
        public string? lastSync;
        public string? contactId;
        public string? contactName;
    }
}
