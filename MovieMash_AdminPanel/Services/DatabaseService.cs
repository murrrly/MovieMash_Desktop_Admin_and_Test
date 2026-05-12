using MySqlConnector;
using System.Configuration;

namespace MovieApp_Adminpanel.Services
{
	public class DatabaseService
	{
		private readonly string _connectionString;

		public DatabaseService()
		{
			_connectionString = ConfigurationManager
				.ConnectionStrings["MySQLConnection"]
				.ConnectionString;
		}

		public MySqlConnection GetConnection()
		{
			return new MySqlConnection(_connectionString);
		}
	}
}