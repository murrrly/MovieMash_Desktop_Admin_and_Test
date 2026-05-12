using Microsoft.VisualStudio.TestTools.UnitTesting;
using MovieApp_Adminpanel;

namespace MovieApp_Adminpanel.Tests
{
	[TestClass]
	public class LoginTests
	{
		[TestMethod]
		public void IsArgon2idHash_ValidHash_ReturnsTrue()
		{
			// Arrange
			var login = new LoginWindow();

			string hash = "$argon2id$v=19$m=65536,t=3,p=1$test$test";

			// Act
			bool result = login.IsArgon2idHash(hash);

			// Assert
			Assert.IsTrue(result);
		}

		[TestMethod]
		public void IsArgon2idHash_InvalidHash_ReturnsFalse()
		{
			var login = new LoginWindow();

			string hash = "123456";

			bool result = login.IsArgon2idHash(hash);

			Assert.IsFalse(result);
		}

		[TestMethod]
		public void IsArgon2idHash_EmptyHash_ReturnsFalse()
		{
			var login = new LoginWindow();

			bool result = login.IsArgon2idHash("");

			Assert.IsFalse(result);
		}

		[TestMethod]
		public void HashPasswordArgon2id_ReturnsHash()
		{
			var login = new LoginWindow();

			string hash = login.HashPasswordArgon2id("admin123");

			Assert.IsNotNull(hash);
			Assert.IsTrue(hash.StartsWith("$argon2id$"));
		}

		[TestMethod]
		public void VerifyArgon2idPassword_CorrectPassword_ReturnsTrue()
		{
			var login = new LoginWindow();

			string password = "admin123";

			string hash = login.HashPasswordArgon2id(password);

			bool result = login.VerifyArgon2idPassword(password, hash);

			Assert.IsTrue(result);
		}

		[TestMethod]
		public void VerifyArgon2idPassword_WrongPassword_ReturnsFalse()
		{
			var login = new LoginWindow();

			string hash = login.HashPasswordArgon2id("admin123");

			bool result = login.VerifyArgon2idPassword("wrongpass", hash);

			Assert.IsFalse(result);
		}

		[TestMethod]
		public void ConstantTimeEquals_SameArrays_ReturnsTrue()
		{
			byte[] a = { 1, 2, 3 };
			byte[] b = { 1, 2, 3 };

			bool result = LoginWindow.ConstantTimeEquals(a, b);

			Assert.IsTrue(result);
		}


	}
}