using System.Data;
using System.Data.SqlClient;

namespace FibiaNM.Logging
{
    public static class DbEntry
    {

        public static void AddLog(string text)
        {
            DateTime dateTime = DateTime.Now;
            using (var db = new FibiaNMDbContext())
            {
                var log = new Log
                {
                    DateTime = dateTime,
                    Text = text

                };
                db.Logs?.Add(log);
                db.SaveChanges();
            }
        }

        public static void DeleteSQLTableData(string connectionstring, string table)
        {
            SqlConnection con = new SqlConnection();
            SqlCommand cmd = new SqlCommand();
            try
            {
                con.ConnectionString = connectionstring;
                con.Open();
                cmd.Connection = con;
                cmd.CommandText = "Delete " + table;
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

            finally
            {
                con.Close();
            }
        }

    }
}
