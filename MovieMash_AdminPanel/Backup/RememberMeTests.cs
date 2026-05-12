using Microsoft.VisualStudio.TestTools.UnitTesting;
using MovieApp_Adminpanel.Services;

namespace MovieApp_Adminpanel.Tests
{
	[TestClass]
	public class RememberMeTests
	{
		[TestMethod]
		public void RememberMe_SaveAndLoad_WorksCorrectly()
		{
			RememberMeData data = new RememberMeData
			{
				Username = "admin",
				Password = "123",
				RememberMe = true
			};

			RememberMeService.Save(data);

			var loaded = RememberMeService.Load();

			Assert.AreEqual("admin", loaded.Username);
			Assert.AreEqual("123", loaded.Password);
			Assert.IsTrue(loaded.RememberMe);
		}

		[TestMethod]
		public void UserRole_Admin_IsValid()
		{
			string role = "admin";

			bool isValid = role == "admin" || role == "moderator";

			Assert.IsTrue(isValid);
		}

		[TestMethod]
		public void UserRole_User_IsInvalid()
		{
			string role = "user";

			bool isValid = role == "admin" || role == "moderator";

			Assert.IsFalse(isValid);
		}
	}
}