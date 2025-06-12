using System.Data.SqlClient;

namespace ElectroStore1
{
    public static class DBConnection
    {
        private static readonly string connectionString = @"Data Source=.\;Initial Catalog=ElectroStoreDBMain;Integrated Security=True";

        public static SqlConnection GetConnection()
        {
            SqlConnection connection = new SqlConnection(connectionString);
            return connection;
        }
    }
}
