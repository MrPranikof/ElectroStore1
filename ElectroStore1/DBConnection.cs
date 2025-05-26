using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace ElectroStore1
{
    public static class DBConnection
    {
        private static readonly string connectionString = @"Data Source=.\;Initial Catalog=ElectroStoreDB;Integrated Security=True";

        public static SqlConnection GetConnection()
        {
            SqlConnection connection = new SqlConnection(connectionString);
            return connection;
        }
    }
}
