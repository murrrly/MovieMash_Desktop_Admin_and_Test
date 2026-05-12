using Microsoft.VisualStudio.TestTools.UnitTesting;
using MovieApp_Adminpanel.Services;
using MySqlConnector;
using System.Diagnostics;

namespace MovieApp_Adminpanel.Tests
{
	[TestClass]
	public class DatabaseTests
	{
		[TestMethod]
		public void GetConnection_ReturnsMySqlConnection()
		{
			DatabaseService db = new DatabaseService();

			MySqlConnection conn = db.GetConnection();

			Assert.IsNotNull(conn);
		}

		[TestMethod]
		public void ConnectionString_IsNotEmpty()
		{
			DatabaseService db = new DatabaseService();

			var conn = db.GetConnection();

			Assert.IsFalse(string.IsNullOrEmpty(conn.ConnectionString));
		}

		[TestMethod]
		public void HashPasswordArgon2id_PerformanceTest()
		{
			var login = new LoginWindow();

			Stopwatch sw = new Stopwatch();

			sw.Start();

			login.HashPasswordArgon2id("admin123");

			sw.Stop();

			Assert.IsTrue(sw.ElapsedMilliseconds < 5000);
		}

		[TestMethod]
		public void HashPasswordArgon2id_GeneratesUniqueHashes()
		{
			var login = new LoginWindow();

			string hash1 = login.HashPasswordArgon2id("admin123");
			string hash2 = login.HashPasswordArgon2id("admin123");

			Assert.AreNotEqual(hash1, hash2);
		}
	}
}