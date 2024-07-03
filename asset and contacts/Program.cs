using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data.SqlClient;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace assetsAndContacts
{
    class Program
    {
        private static readonly string logFolder = "Logs Match";
        static async Task Main(string[] args)
        {
            Program pg = new Program();
            string[] data = pg.getConfig();
            string dataSource = data[0];
            string databaseName = data[1];
            string userId = data[2];
            string password = data[3];
            string connStringWarehouse = "Data Source = " + dataSource + ";Initial Catalog = " + databaseName + ";User ID = " + userId + ";Password = " + password + ";MultipleActiveResultSets=true";

            dataSource = data[4];
            databaseName = data[5];
            userId = data[6];
            password = data[7];
            string connStringAsset = "Data Source = " + dataSource + ";Initial Catalog = " + databaseName + ";User ID = " + userId + ";Password = " + password + ";MultipleActiveResultSets=true";

            string userName = data[8];
            string secret = data[9];
            string apiIntegrationCode = data[10];

            string nCentralAssetId;
            string internalAssetId;
            string AssetName;
            string lastLoginUser;
            string lastSyncUser = "";

            string nCentralCompanyId;
            string autotaskCompanyId;
            string companyName;
            int result = 1000;

            string possibleMatches = "";
            string firstName;
            string lastName;
            string autotaskContactId;

            using (SqlConnection connWarehouse = new SqlConnection(connStringWarehouse))
            {
                connWarehouse.Open();
                SqlCommand cmd = new SqlCommand("SELECT c1.[customerid],c1.[customername],c1.[psacustomername] FROM [ods_smarti_ds1].[dbo].[customer] c1 LEFT JOIN [ods_smarti_ds1].[dbo].customer c2 ON c1.parentid = c2.customerid WHERE c1.deleted = 0 AND c1.psacustomername != '' and c1.parentid != 257 and c1.parentid != 392 and c1.[parentid] != 1 and c2.parentid != 392 and c1.customerid != 254 AND c1.customerid !=341 AND c1.customerid != 257 AND c1.parentid != 229 ORDER BY c1.customerName ASC", connWarehouse);

                using (SqlDataReader reader1 = cmd.ExecuteReader())
                {
                    while (reader1.Read())
                    {
                        nCentralCompanyId = reader1.GetInt32(0).ToString();
                        companyName = reader1.GetString(1);
                        autotaskCompanyId = reader1.GetString(2);

                        Console.WriteLine("Company: " + nCentralCompanyId + " | " + companyName + " | " + autotaskCompanyId);

                        SqlCommand cmd2 = new SqlCommand("SELECT dd.[N-central DeviceID],dd.DeviceID,dd.DeviceName,fccllu.[username],dd.CustomerID,d.customerid,c.customername FROM [warehouse].[dbo].[fact_cim_custom last loggedin user] fccllu LEFT JOIN [warehouse].[dbo].[dim_Device] dd ON dd.DeviceID = fccllu.deviceid LEFT JOIN ods_smarti_ds1.dbo.device d ON d.deviceid = dd.[N-central DeviceID] LEFT JOIN ods_smarti_ds1.dbo.customer c ON c.customerid = d.customerid WHERE (dd.DeviceClassID = 99 OR dd.DeviceClassID = 107) AND c.customername NOT LIKE '%unmanaged%' AND dd.[N-central CustomerID] = " + nCentralCompanyId, connWarehouse);

                        using (SqlDataReader reader2 = cmd2.ExecuteReader())
                        {
                            while (reader2.Read())
                            {
                                nCentralAssetId = reader2.GetInt32(0).ToString();
                                internalAssetId = reader2.GetInt32(1).ToString();
                                AssetName = pg.quotationDeletion(reader2.GetString(2));
                                lastLoginUser = pg.quotationDeletion(reader2.GetString(3));

                                Console.WriteLine("Asset: " + nCentralAssetId + " | " + AssetName + " | " + lastLoginUser);

                                using (SqlConnection connAsset = new SqlConnection(connStringAsset))
                                {
                                    connAsset.Open();
                                    SqlCommand cmd3 = new SqlCommand("SELECT COUNT(*) FROM [Asset].[dbo].[AssetContact] WHERE [N-Central ID] = " + nCentralAssetId, connAsset);

                                    using (SqlDataReader reader3 = cmd3.ExecuteReader())
                                    {
                                        while (reader3.Read())
                                        {
                                            result = reader3.GetInt32(0);
                                        }
                                    }

                                    Console.WriteLine("Result: " + result);

                                    if(result == 0) // if not available make a new entry where last login user = last sync user
                                    {
                                        cmd3 = new SqlCommand("INSERT INTO [Asset].dbo.[AssetContact] ([N-Central ID],[Company ID],[CompanyName],[AssetName],[LastUser],[LastSyncUser],[contactId],[contactName]) VALUES (" + nCentralAssetId + ",'" + autotaskCompanyId + "','"+companyName+"','" + AssetName + "','" + lastLoginUser + "','" + lastLoginUser + "',0,'No Match')", connAsset);
                                        cmd3.ExecuteNonQuery();
                                    }
                                    else // Check if last sync user = last login user
                                    {
                                        cmd3 = new SqlCommand("SELECT [LastSyncUser] FROM [Asset].[dbo].[AssetContact] WHERE [N-Central ID] = " + nCentralAssetId, connAsset);

                                        using (SqlDataReader reader4 = cmd3.ExecuteReader())
                                        {
                                            while (reader4.Read())
                                            {
                                                lastSyncUser = reader4.GetString(0);
                                            }
                                        }

                                        if (!lastSyncUser.Equals(lastLoginUser)) // Update Data in SQL
                                        {
                                            cmd3 = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [LastSyncUser] = '" + lastLoginUser + "' WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                            cmd3.ExecuteNonQuery();
                                        }
                                    }

                                    if (result == 0 || !lastSyncUser.Equals(lastLoginUser)) // if data was not available hence result = 0 or current last login user is not the same as last sync user, GET Company ID
                                    {
                                        // try match with available contacts
                                        string contactApiUrlCompany = "https://webservices6.autotask.net/ATServicesRest/V1.0/Contacts/query?search={\"filter\":[{\"op\":\"eq\", \"field\":\"companyID\",\"value\":\"" + autotaskCompanyId + "\"},{\"op\":\"contains\", \"field\":\"emailAddress\",\"value\":\"" + lastLoginUser + "\"},{\"op\":\"eq\", \"field\":\"isActive\",\"value\":\"1\"}]}";
                                        using (var client = new HttpClient())
                                        {
                                            client.DefaultRequestHeaders.Add("UserName", userName);
                                            client.DefaultRequestHeaders.Add("Secret", secret);
                                            client.DefaultRequestHeaders.Add("ApiIntegrationCode", apiIntegrationCode);

                                            try
                                            {
                                                var reponse = await client.GetAsync(contactApiUrlCompany);

                                                var content = await reponse.Content.ReadAsStringAsync();

                                                dynamic contactData = JObject.Parse(content);

                                                if (contactData.items.Count < 1)
                                                {
                                                    possibleMatches = "No Matches";
                                                    Console.WriteLine(possibleMatches);
                                                }
                                                else if (contactData.items.Count > 1)
                                                {
                                                    Console.WriteLine("More than 1");
                                                    for (int i = 0; i < contactData.items.Count; i++)
                                                    {
                                                        firstName = contactData.items[i].firstName;
                                                        firstName = pg.quotationDeletion(firstName);
                                                        lastName = contactData.items[i].lastName;
                                                        lastName = pg.quotationDeletion(lastName);
                                                        Console.WriteLine("First NameMM: " + firstName);
                                                        Console.WriteLine("Last NameMM: " + lastName);

                                                        autotaskContactId = contactData.items[i].id;

                                                        if (i + 1 < contactData.items.Count)
                                                        {
                                                            if (i != 0)
                                                            {
                                                                possibleMatches = possibleMatches + " " + firstName + " " + lastName + " " + autotaskContactId + " or";
                                                            }
                                                            else
                                                            {
                                                                possibleMatches = firstName + " " + lastName + " " + autotaskContactId + " or";
                                                            }
                                                        }
                                                        else
                                                        {
                                                            possibleMatches = possibleMatches + " " + firstName + " " + lastName + " " + autotaskContactId;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Match");
                                                    firstName = contactData.items[0].firstName;
                                                    Console.WriteLine("FirstName: " + firstName);
                                                    firstName = pg.quotationDeletion(firstName);

                                                    lastName = contactData.items[0].lastName;
                                                    Console.WriteLine("LastName: " + lastName);
                                                    lastName = pg.quotationDeletion(lastName);

                                                    autotaskContactId = contactData.items[0].id;
                                                    possibleMatches = firstName + " " + lastName + " " + autotaskContactId;

                                                    cmd3 = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [ContactID] = '" + autotaskContactId + "', [ContactName] = '" + firstName + " " + lastName + "' WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                                    cmd3.ExecuteNonQuery();

                                                    // Get AssetID in Autotask via N-central ID
                                                    string assetIdUrl = "https://webservices6.autotask.net/ATServicesRest/V1.0/ConfigurationItems/query?search={ \"filter\":[ {\"op\":\"eq\", \"field\":\"N-central Device ID\",\"udf\":true,\"value\":\"" + nCentralAssetId + "\"}]}";

                                                    var response2 = await client.GetAsync(assetIdUrl);

                                                    var content2 = await response2.Content.ReadAsStringAsync();

                                                    dynamic assetData = JObject.Parse(content2);

                                                    Console.WriteLine("Count: "+assetData.items.Count);
                                                    Console.WriteLine(assetData.ToString());

                                                    // add if statement for Count of data in

                                                    if(assetData.items.Count > 0)
                                                    {
                                                        int ATID = assetData.items[0].id;

                                                        // Insert matching asset to contact
                                                        updateAsset ua = new updateAsset()
                                                        {
                                                            id = ATID,
                                                            contactId = autotaskContactId
                                                        };

                                                        string stringJson = JsonConvert.SerializeObject(ua, Formatting.Indented);

                                                        var stringContent = new StringContent(stringJson, Encoding.UTF8, "application/json");

                                                        Console.WriteLine(stringJson);

                                                        //string assetUrl = "https://webservices6.autotask.net/ATServicesRest/V1.0/ConfigurationItems";

                                                        //response2 = await client.PatchAsync(assetUrl, stringContent);

                                                        //content2 = await response2.Content.ReadAsStringAsync();
                                                    }
                                                    else
                                                    {
                                                        Console.WriteLine("Device not available in Autotask");
                                                    }

                                                }
                                            }
                                            catch(Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                                LogToFile2(ex.Message);
                                                System.Environment.Exit(0);
                                            }
                                        }
                                    }
                                    connAsset.Close();
                                }
                            }
                        }
                    }
                }
                connWarehouse.Close();
            }
        }
        public static void LogToFile(string nCentralDeviceId, string internalDeviceId, string deviceName, string userName, string customerName, string possibleMatches)
        {
            try
            {
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                string logFilename = Path.Combine(logFolder, $"device ID and UserName for " + customerName + ".txt");

                using (StreamWriter sw = File.AppendText(logFilename))
                {
                    sw.WriteLine($"{internalDeviceId}||{nCentralDeviceId}||{deviceName}||{userName}||{possibleMatches}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error logging message: {ex.Message}");
            }
        }

        public static void LogToFile2(string text, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                string logFilename = Path.Combine(logFolder, $"Error.txt");

                using (StreamWriter sw = File.AppendText(logFilename))
                {
                    sw.WriteLine($"{text}\t{memberName}\t{sourceLineNumber}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error logging message: {ex.Message}");
            }
        }

        public string quotationDeletion(string word)
        {
            //Delete any " and ' marks if the description is not empty
            if (word != null)
            {
                return Regex.Replace(word, @"[^a-zA-Z0-9\-\\ .,:/<>!?|`~@#$%^*()_+=\n]", string.Empty);
            }
            else
            {
                return "";
            }
        }

        public string[] getConfig()
        {
            string[] data = { };
            try
            {
                StreamReader sr = new StreamReader("config.txt");
                string dat = string.Empty;
                int index = 0;
                data = new string[11];

                while ((dat = sr.ReadLine()) != null)
                {
                    data[index] = dat;
                    index++;
                }
                sr.Dispose();
            }
            catch (Exception ex)
            {
                //LogToFile("Failed to get config file " + ex.Message);
            }
            return data;
        }
    }

    public class updateAsset
    {
        public int id;
        public string? contactId;
    }
}