using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace assetsAndContacts
{
    class Program
    {
        private static readonly string logFolder = "Logs";
        static async Task Main(string[] args)
        {
            Program pg = new Program();
            SqlCommand cmd = new SqlCommand();

            string[] data = pg.getConfig();
            string dataSource = data[4];
            string databaseName = data[5];
            string userId = data[6];
            string password = data[7];
            string connStringAsset = "Data Source = " + dataSource + ";Initial Catalog = " + databaseName + ";User ID = " + userId + ";Password = " + password + ";MultipleActiveResultSets=true";

            bool isProcessing = true;
            double delay = 0;
            int logDays= 0;

            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                Console.WriteLine("Shutdown Requested, Finishing Current Task");
                LogToFile("Shutdown requested");
                delay = 0;

                isProcessing = false;
            };

            while(isProcessing)
            {
                try
                {
                    if(isProcessing)
                    {
                        using(SqlConnection conn = new SqlConnection(connStringAsset))
                        {
                            conn.Open();
                            cmd = new SqlCommand("SELECT [delay] FROM [Asset].[dbo].[CodeVariables] WHERE [No] = 1", conn);
                            delay = Convert.ToDouble(cmd.ExecuteScalar());
                            cmd = new SqlCommand("SELECT [logDays] FROM [Asset].[dbo].[CodeVariables] WHERE [No] = 1", conn);
                            logDays = Convert.ToInt32(cmd.ExecuteScalar());
                            conn.Close();
                        }
                    }
                }
                catch(Exception ex)
                {
                    LogToFile("Error at getting code variable" + ex.Message);
                }
                await Task.WhenAll(pg.contactAssetRun(pg), Task.Delay(TimeSpan.FromHours(delay)));
                DeleteOldLogs(logDays);
            }
        }

        public async Task contactAssetRun(Program pg)
        {
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

            dataSource = data[11];
            databaseName = data[12];
            userId = data[13];
            password = data[14];
            string connStringReport6 = "Data Source = " + dataSource + ";Initial Catalog = " + databaseName + ";User ID = " + userId + ";Password = " + password + ";MultipleActiveResultSets=true";

            string nCentralAssetId;
            string internalAssetId;
            string assetName;
            string currentSyncUser;
            string previousSyncUser = "";

            string nCentralCompanyId;
            string autotaskCompanyId;
            string companyName;
            int result = 1000;
            int filtered = 0;

            string possibleMatches = "";
            string firstName = "";
            string lastName = "";
            string autotaskContactId = "";

            using (SqlConnection connWarehouse = new SqlConnection(connStringWarehouse))
            {
                connWarehouse.Open();
                SqlCommand cmd = new SqlCommand("SELECT c1.[customerid],c1.[customername],c1.[psacustomername] FROM [ods_smarti_ds1].[dbo].[customer] c1 LEFT JOIN [ods_smarti_ds1].[dbo].customer c2 ON c1.parentid = c2.customerid WHERE c1.deleted = 0 AND c1.psacustomername != '' and c1.parentid != 257 and c1.parentid != 392 and c1.[parentid] != 1 and c2.parentid != 392 and c1.customerid != 254 AND c1.customerid !=341 AND c1.customerid != 257 AND c1.parentid != 229 ORDER BY c1.customerName ASC", connWarehouse);
                //SqlCommand cmd = new SqlCommand("SELECT c1.[customerid],c1.[customername],c1.[psacustomername] FROM [ods_smarti_ds1].[dbo].[customer] c1 LEFT JOIN [ods_smarti_ds1].[dbo].customer c2 ON c1.parentid = c2.customerid WHERE c1.deleted = 0 AND c1.psacustomername != '' and c1.parentid != 257 and c1.parentid != 392 and c1.[parentid] != 1 and c2.parentid != 392 and c1.customerid != 254 AND c1.customerid !=341 AND c1.customerid != 257 AND c1.parentid != 229 AND (c1.customername LIKE '%Betta%' OR c1.customername LIKE '%Antipodes%')", connWarehouse);
                //SqlCommand cmd = new SqlCommand("SELECT c1.[customerid],c1.[customername],c1.[psacustomername] FROM [ods_smarti_ds1].[dbo].[customer] c1 LEFT JOIN [ods_smarti_ds1].[dbo].customer c2 ON c1.parentid = c2.customerid WHERE c1.deleted = 0 AND c1.psacustomername != '' and c1.parentid != 257 and c1.parentid != 392 and c1.[parentid] != 1 and c2.parentid != 392 and c1.customerid != 254 AND c1.customerid !=341 AND c1.customerid != 257 AND c1.parentid != 229 AND (c1.customername LIKE '%Antipodes%' OR c1.customername LIKE '%Corban%' OR c1.customername LIKE '%Italia%' OR c1.customername LIKE '%Fencible%')", connWarehouse);

                using (SqlDataReader reader1 = cmd.ExecuteReader())
                {
                    while (reader1.Read())
                    {
                        nCentralCompanyId = reader1.GetInt32(0).ToString();
                        companyName = reader1.GetString(1);
                        autotaskCompanyId = reader1.GetString(2);

                        Console.WriteLine("Company: " + nCentralCompanyId + " | " + companyName + " | " + autotaskCompanyId);
                        LogToFile("Company: " + nCentralCompanyId + " | " + companyName + " | " + autotaskCompanyId);

                        SqlCommand cmd2 = new SqlCommand("SELECT dd.[N-central DeviceID],dd.DeviceID,dd.DeviceName,fccllu.[username],dd.CustomerID,d.customerid,c.customername FROM [warehouse].[dbo].[fact_cim_custom last loggedin user] fccllu LEFT JOIN [warehouse].[dbo].[dim_Device] dd ON dd.DeviceID = fccllu.deviceid LEFT JOIN ods_smarti_ds1.dbo.device d ON d.deviceid = dd.[N-central DeviceID] LEFT JOIN ods_smarti_ds1.dbo.customer c ON c.customerid = d.customerid WHERE (dd.DeviceClassID = 99 OR dd.DeviceClassID = 107) AND c.customername NOT LIKE '%unmanaged%' AND d.deleted = 0 AND dd.[N-central CustomerID] = " + nCentralCompanyId, connWarehouse);

                        using (SqlDataReader reader2 = cmd2.ExecuteReader())
                        {
                            while (reader2.Read())
                            {
                                nCentralAssetId = reader2.GetInt32(0).ToString();
                                internalAssetId = reader2.GetInt32(1).ToString();
                                assetName = pg.quotationDeletion(reader2.GetString(2));
                                currentSyncUser = pg.nonAlphaNumericDeletion(reader2.GetString(3));

                                Console.WriteLine("Asset: " + nCentralAssetId + " | " + assetName + " | " + currentSyncUser);
                                LogToFile("Asset: " + nCentralAssetId + " | " + assetName + " | " + currentSyncUser);

                                using (SqlConnection connAsset = new SqlConnection(connStringAsset))
                                {
                                    connAsset.Open();

                                    //Check if Device already exist in the database
                                    SqlCommand cmd3 = new SqlCommand("SELECT COUNT(*) FROM [Asset].[dbo].[AssetContact] WHERE [N-Central ID] = " + nCentralAssetId, connAsset);

                                    using (SqlDataReader reader3 = cmd3.ExecuteReader())
                                    {
                                        while (reader3.Read())
                                        {
                                            result = reader3.GetInt32(0);
                                        }
                                    }

                                    Console.WriteLine("Result: " + result);

                                    if (result == 0) // if not available make a new entry where last login user = last sync user
                                    {
                                        cmd3 = new SqlCommand("INSERT INTO [Asset].dbo.[AssetContact] ([N-Central ID],[Company ID],[CompanyName],[AssetName],[CurrentSyncUser],[LastSyncUser],[contactId],[contactName],[Filtered]) VALUES (" + nCentralAssetId + ",'" + autotaskCompanyId + "','" + companyName + "','" + assetName + "','" + currentSyncUser + "','No Previous Sync',0,'No Match',0)", connAsset);
                                        cmd3.ExecuteNonQuery();
                                        LogToFile(assetName + " is new, added to database");
                                    }
                                    else 
                                    {
                                        //Check if Device is filtered
                                        cmd3 = new SqlCommand("SELECT COUNT(*) FROM [Asset].[dbo].[AssetContact] WHERE [N-Central ID] = " + nCentralAssetId + " AND [Filtered] = 1", connAsset);

                                        using (SqlDataReader reader3 = cmd3.ExecuteReader())
                                        {
                                            while (reader3.Read())
                                            {
                                                filtered = reader3.GetInt32(0);
                                            }
                                        }

                                        Console.WriteLine("Filter: " + filtered);

                                        if (filtered != 1)// Check if last sync user = last login user
                                        {
                                            cmd3 = new SqlCommand("SELECT [CurrentSyncUser] FROM [Asset].[dbo].[AssetContact] WHERE [N-Central ID] = " + nCentralAssetId, connAsset);

                                            using (SqlDataReader reader4 = cmd3.ExecuteReader())
                                            {
                                                while (reader4.Read())
                                                {
                                                    previousSyncUser = reader4.GetString(0);
                                                }
                                            }

                                            if (!previousSyncUser.Equals(currentSyncUser)) // Update Data in SQL
                                            {
                                                cmd3 = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [CurrentSyncUser] = '" + currentSyncUser + "',[LastSyncUser] = '" + previousSyncUser + "' WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                                cmd3.ExecuteNonQuery();
                                                LogToFile("Updating " + assetName + "with new login user " + currentSyncUser);
                                            }
                                        }
                                        else
                                        {
                                            LogToFile(assetName + " is Filtered");
                                        }
                                    }

                                    if ((result == 0 || !previousSyncUser.Equals(currentSyncUser)) && filtered != 1) // if data was not available hence result = 0 or current last login user is not the same as last sync user, GET Company ID
                                    {
                                        // try match with available contacts
                                        string contactApiUrlCompany = "https://webservices6.autotask.net/ATServicesRest/V1.0/Contacts/query?search={\"filter\":[{\"op\":\"eq\", \"field\":\"companyID\",\"value\":\"" + autotaskCompanyId + "\"},{\"op\":\"contains\", \"field\":\"emailAddress\",\"value\":\"" + currentSyncUser + "\"},{\"op\":\"eq\", \"field\":\"isActive\",\"value\":\"1\"}]}";
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
                                                    LogToFile(possibleMatches + " for " + assetName + " trying SQL search");

                                                    //SQL search using Report6 warehouse
                                                    using (SqlConnection connReport = new SqlConnection(connStringReport6))
                                                    {
                                                        connReport.Open();
                                                        string modifyCurrentSyncUser = "%" + string.Join("%", currentSyncUser.ToCharArray()) + "%";

                                                        cmd3 = new SqlCommand("SELECT COUNT(*) FROM [TF_3603000_WH].[dbo].[wh_account_contact] WHERE is_active = 1 AND account_id = '" + autotaskCompanyId + "' AND CONCAT(first_name,' ',last_name) LIKE '" + modifyCurrentSyncUser + "'", connReport);

                                                        using (SqlDataReader reader5 = cmd3.ExecuteReader())
                                                        {
                                                            while (reader5.Read())
                                                            {
                                                                result = reader5.GetInt32(0);
                                                            }
                                                        }

                                                        if (result == 1)
                                                        {
                                                            Console.WriteLine("Found using SQL");
                                                            LogToFile("Found using SQL");

                                                            Console.WriteLine(modifyCurrentSyncUser);

                                                            cmd3 = new SqlCommand("SELECT [account_contact_id],[first_name],[last_name] FROM [TF_3603000_WH].[dbo].[wh_account_contact] WHERE is_active = 1 AND account_id = '" + autotaskCompanyId + "' AND CONCAT(first_name,' ',last_name) LIKE '" + modifyCurrentSyncUser + "'", connReport);

                                                            using (SqlDataReader reader5 = cmd3.ExecuteReader())
                                                            {
                                                                while (reader5.Read())
                                                                {
                                                                    autotaskContactId = reader5.GetInt32(0).ToString();
                                                                    firstName = reader5.GetString(1);
                                                                    lastName = reader5.GetString(2);
                                                                }
                                                            }

                                                            firstName = pg.nonAlphaNumericDeletion(firstName);
                                                            lastName = pg.nonAlphaNumericDeletion(lastName);

                                                            cmd3 = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [ContactID] = '" + autotaskContactId + "', [ContactName] = '" + firstName + " " + lastName + "' WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                                            cmd3.ExecuteNonQuery();

                                                            await Task.WhenAll(sendDataToAutotask(nCentralAssetId, assetName, firstName, lastName, userName, secret, apiIntegrationCode, autotaskContactId));
                                                        }
                                                        else if (result > 1)
                                                        {
                                                            Console.WriteLine(currentSyncUser + " has more than 1 match");
                                                            LogToFile(currentSyncUser + " has more than 1 match");

                                                            cmd3 = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [ContactName] = 'Multiple Match' WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                                            cmd3.ExecuteNonQuery();
                                                        }
                                                        else
                                                        {
                                                            string lUser = "";

                                                            cmd3 = new SqlCommand("SELECT [LastSyncUser] FROM [Asset].[dbo].[AssetContact] WHERE [N-Central ID] = " + autotaskCompanyId, connAsset);
                                                            using (SqlDataReader reader5 = cmd3.ExecuteReader())
                                                            {
                                                                while (reader5.Read())
                                                                {
                                                                    lUser = reader5.GetString(0);
                                                                }
                                                            }

                                                            if (!lUser.Equals("No Previous Sync"))
                                                            {
                                                                cmd3 = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [ContactID] = '0', [ContactName] = 'Conflict' WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                                                cmd3.ExecuteNonQuery();
                                                            }

                                                            Console.WriteLine(currentSyncUser + " has no match");
                                                            LogToFile(currentSyncUser + " has no match");
                                                        }
                                                    }
                                                }
                                                else if (contactData.items.Count > 1)
                                                {
                                                    Console.WriteLine("More than 1");
                                                    for (int i = 0; i < contactData.items.Count; i++)
                                                    {
                                                        firstName = contactData.items[i].firstName;
                                                        firstName = pg.nonAlphaNumericDeletion(firstName);
                                                        lastName = contactData.items[i].lastName;
                                                        lastName = pg.nonAlphaNumericDeletion(lastName);
                                                        Console.WriteLine("First NameMM: " + firstName);
                                                        Console.WriteLine("Last NameMM: " + lastName);

                                                        autotaskContactId = contactData.items[i].id;

                                                        if (i + 1 < contactData.items.Count)
                                                        {
                                                            if (i != 0)
                                                            {
                                                                possibleMatches = possibleMatches + " " + firstName + " " + lastName + " " + autotaskContactId + " or";
                                                                LogToFile(possibleMatches);
                                                            }
                                                            else
                                                            {
                                                                possibleMatches = firstName + " " + lastName + " " + autotaskContactId + " or";
                                                                LogToFile(possibleMatches);
                                                            }
                                                        }
                                                        else
                                                        {
                                                            possibleMatches = possibleMatches + " " + firstName + " " + lastName + " " + autotaskContactId;
                                                            LogToFile(possibleMatches);
                                                        }
                                                    }
                                                    cmd3 = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [ContactName] = 'Multiple Match' WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                                    cmd3.ExecuteNonQuery();
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Match");
                                                    firstName = contactData.items[0].firstName;
                                                    Console.WriteLine("FirstName: " + firstName);
                                                    firstName = pg.nonAlphaNumericDeletion(firstName);

                                                    lastName = contactData.items[0].lastName;
                                                    Console.WriteLine("LastName: " + lastName);
                                                    lastName = pg.nonAlphaNumericDeletion(lastName);

                                                    autotaskContactId = contactData.items[0].id;
                                                    possibleMatches = firstName + " " + lastName + " " + autotaskContactId;

                                                    cmd3 = new SqlCommand("UPDATE [Asset].[dbo].[AssetContact] SET [ContactID] = '" + autotaskContactId + "', [ContactName] = '" + firstName + " " + lastName + "' WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                                    cmd3.ExecuteNonQuery();

                                                    await Task.WhenAll(sendDataToAutotask(nCentralAssetId, assetName, firstName, lastName, userName, secret, apiIntegrationCode, autotaskContactId));
                                                }
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex);
                                                LogToFile(ex.Message);
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

            // check every stored asset and check if they are still online
            using (SqlConnection connAsset = new SqlConnection(connStringAsset))
            {
                connAsset.Open();
                SqlCommand cmd3 = new SqlCommand("SELECT [N-Central ID],[AssetName] FROM [Asset].[dbo].[AssetContact]", connAsset);
                bool deleted = true;
                int unmanaged = 0;
                int exist = 0;

                using (SqlDataReader reader3 = cmd3.ExecuteReader())
                {
                    while (reader3.Read())
                    {
                        nCentralAssetId = reader3.GetString(0);
                        assetName = reader3.GetString(1);

                        Console.WriteLine("ID: " + nCentralAssetId);
                        Console.WriteLine("Name: " + assetName);

                        using (SqlConnection connWarehouse = new SqlConnection(connStringWarehouse))
                        {
                            connWarehouse.Open();

                            //check if exist
                            SqlCommand cmd4 = new SqlCommand("SELECT COUNT(*) FROM [ods_smarti_ds1].[dbo].[device] WHERE deviceid = " + nCentralAssetId, connWarehouse);
                            using (SqlDataReader reader4 = cmd4.ExecuteReader())
                            {
                                while(reader4.Read())
                                {
                                    exist = reader4.GetInt32(0);
                                }
                            }

                            Console.WriteLine("Exist: " + exist);

                            //check if it's deleted
                            cmd4 = new SqlCommand("SELECT [deleted] FROM [ods_smarti_ds1].[dbo].[device] WHERE deviceid = " + nCentralAssetId, connWarehouse);

                            using (SqlDataReader reader4 = cmd4.ExecuteReader())
                            {
                                while (reader4.Read())
                                {
                                    deleted = reader4.GetBoolean(0);
                                }
                            }

                            Console.WriteLine("Deleted: " + deleted);

                            //check if it's moved to unmanaged
                            cmd4 = new SqlCommand("SELECT COUNT(*) FROM [warehouse].[dbo].[dim_Device] dd LEFT JOIN ods_smarti_ds1.dbo.device d ON d.deviceid = dd.[N-central DeviceID] LEFT JOIN ods_smarti_ds1.dbo.customer c ON c.customerid = d.customerid WHERE (dd.DeviceClassID = 99 OR dd.DeviceClassID = 107) AND c.customername LIKE '%unmanaged%' AND dd.[N-central DeviceID] = " + nCentralAssetId, connWarehouse);

                            using (SqlDataReader reader4 = cmd4.ExecuteReader())
                            {
                                while (reader4.Read())
                                {
                                    unmanaged = reader4.GetInt32(0);
                                }
                            }

                            Console.WriteLine("Unmanaged: " + unmanaged);

                            if (deleted || unmanaged > 0 || exist < 1)
                            {
                                Console.WriteLine("Delete " + nCentralAssetId);
                                LogToFile(assetName + " is deleted, unmanaged or don't exist");
                                SqlCommand cmd5 = new SqlCommand("DELETE FROM [Asset].[dbo].[AssetContact] WHERE [N-Central ID] = " + nCentralAssetId, connAsset);
                                cmd5.ExecuteNonQuery();
                            }
                            connWarehouse.Close();
                        }
                    }
                }
                connAsset.Close();
            }
        }

        public async Task sendDataToAutotask(string nCentralAssetId, string assetName, string firstName, string lastName, string userName, string secret, string apiIntegrationCode, string autotaskContactId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("UserName", userName);
                client.DefaultRequestHeaders.Add("Secret", secret);
                client.DefaultRequestHeaders.Add("ApiIntegrationCode", apiIntegrationCode);

                // Get AssetID in Autotask via N-central ID
                string assetIdUrl = "https://webservices6.autotask.net/ATServicesRest/V1.0/ConfigurationItems/query?search={ \"filter\":[ {\"op\":\"eq\", \"field\":\"N-central Device ID\",\"udf\":true,\"value\":\"" + nCentralAssetId + "\"}]}";

                var response2 = await client.GetAsync(assetIdUrl);

                var content2 = await response2.Content.ReadAsStringAsync();

                dynamic assetData = JObject.Parse(content2);

                Console.WriteLine("Count: " + assetData.items.Count);

                // add if statement for Count of data in
                if (assetData.items.Count > 0)
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

                    string assetUrl = "https://webservices6.autotask.net/ATServicesRest/V1.0/ConfigurationItems";

                    response2 = await client.PatchAsync(assetUrl, stringContent);

                    content2 = await response2.Content.ReadAsStringAsync();

                    if (response2.IsSuccessStatusCode)
                    {
                        LogToFile(assetName + " succesfully updated in Autotask with contact " + firstName + " " + lastName);
                    }
                    else
                    {
                        LogToFile(assetName + " failed to update");
                        LogToFile(content2);
                    }
                }
                else
                {
                    Console.WriteLine("Device not available in Autotask");
                }
            }
               
        }

        public static void LogToFile(string text, [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                string logFilename = Path.Combine(logFolder, $"log_{DateTime.Now:yyyyMMdd}.txt");

                using (StreamWriter sw = File.AppendText(logFilename))
                {
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{memberName}\t{sourceLineNumber}\t{text}");
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

        public string nonAlphaNumericDeletion(string word)
        {
            if (word != null)
            {
                return Regex.Replace(word, @"[^a-zA-Z0-9 ]", string.Empty);
            }
            else
            {
                return "";
            }
        }

        public string[] getConfig()
        {
            string[] data = Array.Empty<string>();
            try
            {
                StreamReader sr = new StreamReader("config.txt");
                string dat = string.Empty;
                int index = 0;
                data = new string[15];

                while ((dat = sr.ReadLine()) != null)
                {
                    data[index] = dat;
                    index++;
                }
                sr.Dispose();
            }
            catch (Exception ex)
            {
                LogToFile("Failed to get config file " + ex.Message);
            }
            return data;
        }

        private static void DeleteOldLogs(int logDays)
        {
            try
            {
                var logFiles = Directory.GetFiles(logFolder, "log_*.txt");

                foreach (var logFile in logFiles)
                {
                    string filename = Path.GetFileNameWithoutExtension(logFile);
                    string dateString = filename.Substring(4); // assuming filename starts with "log_"

                    if (DateTime.TryParseExact(dateString, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out DateTime logDate))
                    {
                        var diffOfDates = DateTime.Now - logDate;
                        //Console.WriteLine(diffOfDates.Days.ToString());
                        if (diffOfDates.Days > logDays)
                        {
                            //Console.WriteLine("delete " + logFile);
                            File.Delete(logFile);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error deleting old logs: {ex.Message}");
            }
        }
    }

    public class updateAsset
    {
        public int id;
        public string? contactId;
    }
}