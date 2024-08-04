using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace assetWebApi.Pages.Filter

{
    public class FilterModel : PageModel
    {
        public List<assetData> filterAssetMatch = new List<assetData>();
        public void OnGet()
        {
            try
            {
                string connString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["DefaultConnection"];

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT [Key Id],[N-Central ID],[CompanyName],[AssetName] FROM [Asset].[dbo].[AssetContact] WHERE [Filtered] = 1", conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                assetData cAsset = new assetData();
                                cAsset.Keyid = reader.GetInt32(0);
                                cAsset.nCentralId = reader.GetString(1);
                                cAsset.companyName = reader.GetString(2);
                                cAsset.assetName = reader.GetString(3);

                                filterAssetMatch.Add(cAsset);
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
    }
}
