using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLParserProyecto
{
    public class Context
    {
        protected readonly string Instance = "localhost\\MSSQL";
        protected readonly string Database = "master";
        protected readonly string ConnectionString;
        public Context()
        {
            ConnectionString = $"Server= {Instance}; Database={Database}; Integrated Security=yes";
        }
        public Context(string instance = "localhost\\MSSQL", string database = "master")
        {
            Instance = instance;
            Database = database;
            ConnectionString = $"Server= {Instance}; Database={Database}; Integrated Security=yes";
        }
        public SqlConnection GetConnection()
        {
            return new SqlConnection(ConnectionString);
        }
        public string GetConnectionString()
        {
            return ConnectionString;
        }
        public string GetInstance()
        {
            return ConnectionString;
        }
        public string GetDatabase()
        {
            return Database;
        }
    }
}
