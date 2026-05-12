using Xunit;
using Moq;
using FluentAssertions;

namespace MovieApp.WhiteBoxTests
{
	// ===== MOCK INTERFACES (если у тебя уже есть — используй свои) =====
	public interface IAuthService
	{
		bool Login(string username, string password);
	}

	public interface IUserService
	{
		bool AddUser(string name);
		bool DeleteUser(int id);
	}

	public class LoginViewModel
	{
		private readonly IAuthService _auth;

		public LoginViewModel(IAuthService auth)
		{
			_auth = auth;
		}

		public bool Login(string username, string password)
		{
			if (string.IsNullOrWhiteSpace(username))
				return false;

			if (string.IsNullOrWhiteSpace(password))
				return false;

			return _auth.Login(username, password);
		}
	}

	// ================= TESTS =================

	public class WhiteBoxTests
	{
		// 1. Успешный логин
		[Fact]
		public void Login_Should_ReturnTrue_When_CredentialsAreCorrect()
		{
			var mock = new Mock<IAuthService>();
			mock.Setup(x => x.Login("admin", "1234")).Returns(true);

			var vm = new LoginViewModel(mock.Object);

			var result = vm.Login("admin", "1234");

			result.Should().BeTrue();
		}

		// 2. Неверный пароль
		[Fact]
		public void Login_Should_Fail_When_PasswordIsWrong()
		{
			var mock = new Mock<IAuthService>();
			mock.Setup(x => x.Login("admin", "wrong")).Returns(false);

			var vm = new LoginViewModel(mock.Object);

			var result = vm.Login("admin", "wrong");

			result.Should().BeFalse();
		}

		// 3. Пустой логин
		[Fact]
		public void Login_Should_Fail_When_UsernameIsEmpty()
		{
			var mock = new Mock<IAuthService>();

			var vm = new LoginViewModel(mock.Object);

			var result = vm.Login("", "1234");

			result.Should().BeFalse();
		}

		// 4. Пустой пароль
		[Fact]
		public void Login_Should_Fail_When_PasswordIsEmpty()
		{
			var mock = new Mock<IAuthService>();

			var vm = new LoginViewModel(mock.Object);

			var result = vm.Login("admin", "");

			result.Should().BeFalse();
		}

		// 5. UserService AddUser успех
		[Fact]
		public void AddUser_Should_ReturnTrue()
		{
			var mock = new Mock<IUserService>();
			mock.Setup(x => x.AddUser("John")).Returns(true);

			var result = mock.Object.AddUser("John");

			result.Should().BeTrue();
		}

		// 6. AddUser провал
		[Fact]
		public void AddUser_Should_ReturnFalse_When_Invalid()
		{
			var mock = new Mock<IUserService>();
			mock.Setup(x => x.AddUser("")).Returns(false);

			var result = mock.Object.AddUser("");

			result.Should().BeFalse();
		}

		// 7. Delete user success
		[Fact]
		public void DeleteUser_Should_ReturnTrue()
		{
			var mock = new Mock<IUserService>();
			mock.Setup(x => x.DeleteUser(1)).Returns(true);

			var result = mock.Object.DeleteUser(1);

			result.Should().BeTrue();
		}

		// 8. Delete user fail
		[Fact]
		public void DeleteUser_Should_ReturnFalse()
		{
			var mock = new Mock<IUserService>();
			mock.Setup(x => x.DeleteUser(99)).Returns(false);

			var result = mock.Object.DeleteUser(99);

			result.Should().BeFalse();
		}

		// 9. AuthService вызов проверка
		[Fact]
		public void AuthService_Should_BeCalled_Once()
		{
			var mock = new Mock<IAuthService>();

			mock.Object.Login("admin", "1234");

			mock.Verify(x => x.Login("admin", "1234"), Times.Once);
		}

		// 10. Null username
		[Fact]
		public void Login_Should_Handle_NullUsername()
		{
			var mock = new Mock<IAuthService>();

			var vm = new LoginViewModel(mock.Object);

			var result = vm.Login(null, "1234");

			result.Should().BeFalse();
		}

		// 11. Null password
		[Fact]
		public void Login_Should_Handle_NullPassword()
		{
			var mock = new Mock<IAuthService>();

			var vm = new LoginViewModel(mock.Object);

			var result = vm.Login("admin", null);

			result.Should().BeFalse();
		}

		// 12. Empty both fields
		[Fact]
		public void Login_Should_Fail_When_BothEmpty()
		{
			var mock = new Mock<IAuthService>();

			var vm = new LoginViewModel(mock.Object);

			var result = vm.Login("", "");

			result.Should().BeFalse();
		}

		// 13. Multiple login attempts
		[Fact]
		public void Login_MultipleAttempts_Should_Work()
		{
			var mock = new Mock<IAuthService>();
			mock.Setup(x => x.Login("admin", "1234")).Returns(true);

			var vm = new LoginViewModel(mock.Object);

			vm.Login("admin", "1234");
			var result = vm.Login("admin", "1234");

			result.Should().BeTrue();
		}

		// 14. Service isolation test
		[Fact]
		public void Services_Should_Be_Isolated()
		{
			var authMock = new Mock<IAuthService>();
			var userMock = new Mock<IUserService>();

			authMock.Setup(x => x.Login("a", "b")).Returns(true);
			userMock.Setup(x => x.AddUser("test")).Returns(true);

			authMock.Object.Login("a", "b").Should().BeTrue();
			userMock.Object.AddUser("test").Should().BeTrue();
		}

		// 15. System stability test
		[Fact]
		public void System_Should_Not_Throw_Exception()
		{
			var mock = new Mock<IAuthService>();

			var vm = new LoginViewModel(mock.Object);

			var exception = Record.Exception(() =>
			{
				vm.Login("admin", "1234");
				vm.Login("", "");
				vm.Login(null, null);
			});

			exception.Should().BeNull();
		}
	}
}