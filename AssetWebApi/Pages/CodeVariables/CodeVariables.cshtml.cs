using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.SqlClient;

namespace assetWebApi.Pages.CodeVariables
{
    public class CodeVariablesModel : PageModel
    {
        public CodeVar cv = new CodeVar();
        public string errorMessage = "";
        public void OnGet()
        {
            try
            {
                string connString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["DefaultConnection"];

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT [delay],[logDays] FROM [Asset].[dbo].[CodeVariables] WHERE [No] = 1", conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                cv.delay = reader.GetDouble(0).ToString();
                                cv.logDays = reader.GetInt32(1).ToString();
                            }
                        }
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Hello" + ex.Message;
            }
        }

        public void OnPost()
        {
            cv.delay = Request.Form["delay"];
            cv.logDays = Request.Form["logDays"];

            if (cv.delay.Length == 0 || cv.logDays.Length == 0)
            {
                errorMessage = "all field are required";
                return;
            }

            try
            {
                string connString = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build().GetSection("ConnectionStrings")["DefaultConnection"];

                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("UPDATE [Asset].[dbo].[CodeVariables] SET [delay]='" + cv.delay + "',[logDays]='" + cv.logDays + "' WHERE [No]= 1", conn)) 
                    {
                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                errorMessage = "Hi" + ex.Message;
                return;
            }
            Response.Redirect("/");
        }
    }

    public class CodeVar
    {
        public string delay;
        public string logDays;
    }
}
