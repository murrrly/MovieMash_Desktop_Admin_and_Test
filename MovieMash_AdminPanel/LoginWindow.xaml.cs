using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MovieApp_Adminpanel.Pages;
using MovieApp_Adminpanel.Services;
using MySqlConnector;
using Konscious.Security.Cryptography;
using System.Text;
using System.Linq;

namespace MovieApp_Adminpanel
{
	public partial class LoginWindow : Window
	{
		private long _currentUserId;
		private string _currentUserRole;

		public readonly DatabaseService db = new DatabaseService();

		public LoginWindow()
		{
			InitializeComponent();
			LoadRememberMe();
		}

		private void LoadRememberMe()
		{
			var data = RememberMeService.Load();
			UsernameTextBox.Text = data.Username;
			PasswordBox.Password = data.Password;
			RememberMeCheckBox.IsChecked = data.RememberMe;
		}

		private void SaveRememberMe()
		{
			var data = new RememberMeData
			{
				Username = RememberMeCheckBox.IsChecked == true ? UsernameTextBox.Text : "",
				Password = RememberMeCheckBox.IsChecked == true ? PasswordBox.Password : "",
				RememberMe = RememberMeCheckBox.IsChecked == true
			};

			RememberMeService.Save(data);
		}

		public bool IsArgon2idHash(string hash)
		{
			if (string.IsNullOrEmpty(hash)) return false;
			return hash.StartsWith("$argon2id$");
		}

		public string HashPasswordArgon2id(string password)
		{
			byte[] salt = new byte[16];
			using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
			{
				rng.GetBytes(salt);
			}

			using (var hasher = new Argon2id(Encoding.UTF8.GetBytes(password)))
			{
				hasher.Salt = salt;
				hasher.DegreeOfParallelism = 1;
				hasher.Iterations = 3;
				hasher.MemorySize = 65536;

				byte[] hash = hasher.GetBytes(32);

				string saltBase64 = Convert.ToBase64String(salt);
				string hashBase64 = Convert.ToBase64String(hash);

				return $"$argon2id$v=19$m={hasher.MemorySize},t={hasher.Iterations},p={hasher.DegreeOfParallelism}${saltBase64}${hashBase64}";
			}
		}

		public bool VerifyArgon2idPassword(string password, string storedHash)
		{
			if (!IsArgon2idHash(storedHash))
				return false;

			try
			{
				var parts = storedHash.Split('$');
				if (parts.Length != 6)
					return false;

				string paramsPart = parts[3];
				if (paramsPart.StartsWith("v=19$"))
					paramsPart = paramsPart.Substring(5);

				var paramPairs = paramsPart.Split(',');
				int memorySize = 65536;
				int iterations = 3;
				int parallelism = 1;

				foreach (var pair in paramPairs)
				{
					var kv = pair.Split('=');
					if (kv.Length == 2)
					{
						switch (kv[0])
						{
							case "m": memorySize = int.Parse(kv[1]); break;
							case "t": iterations = int.Parse(kv[1]); break;
							case "p": parallelism = int.Parse(kv[1]); break;
						}
					}
				}

				byte[] salt = Convert.FromBase64String(parts[4]);
				byte[] expectedHash = Convert.FromBase64String(parts[5]);

				using (var hasher = new Argon2id(Encoding.UTF8.GetBytes(password)))
				{
					hasher.Salt = salt;
					hasher.DegreeOfParallelism = parallelism;
					hasher.Iterations = iterations;
					hasher.MemorySize = memorySize;

					byte[] computedHash = hasher.GetBytes(32);
					return ConstantTimeEquals(computedHash, expectedHash);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Argon2id verification error: {ex.Message}");
				return false;
			}
		}

		public static bool ConstantTimeEquals(byte[] a, byte[] b)
		{
			if (a.Length != b.Length)
				return false;

			int result = 0;
			for (int i = 0; i < a.Length; i++)
				result |= a[i] ^ b[i];

			return result == 0;
		}

		internal async Task<bool> UpgradeToArgon2id(MySqlConnection conn, long userId, string plainPassword)
		{
			try
			{
				string newHash = HashPasswordArgon2id(plainPassword);

				string updateSql = @"
UPDATE users 
SET password_hash = @newHash 
WHERE id = @userId";

				using (var updateCmd = new MySqlCommand(updateSql, conn))
				{
					updateCmd.Parameters.AddWithValue("@newHash", newHash);
					updateCmd.Parameters.AddWithValue("@userId", userId);
					await updateCmd.ExecuteNonQueryAsync();
				}

				return true;
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed to upgrade password hash: {ex.Message}");
				return false;
			}
		}

		private async void LoginButton_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(UsernameTextBox.Text) || string.IsNullOrEmpty(PasswordBox.Password))
			{
				ShowStatus("⚠ Заполните все поля", false);
				return;
			}

			try
			{
				using (var conn = db.GetConnection())
				{
					await conn.OpenAsync();

					string sql = @"
SELECT id, password_hash, role
FROM users
WHERE username = @username
AND is_active = true";

					using (var cmd = new MySqlCommand(sql, conn))
					{
						cmd.Parameters.AddWithValue("@username", UsernameTextBox.Text);

						using (var reader = await cmd.ExecuteReaderAsync())
						{
							if (!await reader.ReadAsync())
							{
								ShowStatus("❌ Неверный логин или пользователь не активен", false);
								return;
							}

							string storedHashOrPassword = reader.GetString("password_hash");
							string role = reader.GetString("role");
							long userId = reader.GetInt64("id");

							reader.Close();

							bool passwordValid = false;
							bool isArgon2id = IsArgon2idHash(storedHashOrPassword);

							if (isArgon2id)
							{
								passwordValid = VerifyArgon2idPassword(PasswordBox.Password, storedHashOrPassword);
							}
							else
							{
								passwordValid = (PasswordBox.Password == storedHashOrPassword);

								if (passwordValid)
								{
									await UpgradeToArgon2id(conn, userId, PasswordBox.Password);
									ShowStatus("🔄 Пароль обновлен на Argon2id", true);
									await Task.Delay(1500);
								}
							}

							if (!passwordValid)
							{
								ShowStatus("❌ Неверный логин или пароль", false);
								return;
							}

							if (role != "admin" && role != "moderator")
							{
								ShowStatus("❌ У вас нет доступа к панели администратора", false);
								return;
							}

							_currentUserId = userId;
							_currentUserRole = role;

							string updateSql = @"
UPDATE users
SET last_login = NOW()
WHERE id = @id";

							using (var updateCmd = new MySqlCommand(updateSql, conn))
							{
								updateCmd.Parameters.AddWithValue("@id", _currentUserId);
								await updateCmd.ExecuteNonQueryAsync();
							}
						}
					}

					SaveRememberMe();

					AdminSession.CurrentAdminId = _currentUserId;
					AdminSession.Username = UsernameTextBox.Text;
					AdminSession.UserRole = _currentUserRole;

					var dashboardPage = new DashboardPage();
					var mainWindow = new MainWindow();
					mainWindow.MainFrame.Content = dashboardPage;
					mainWindow.Show();
					this.Close();
				}
			}
			catch (Exception ex)
			{
				ShowStatus("Ошибка подключения к БД", false);
				MessageBox.Show($"Ошибка: {ex.Message}");
			}
		}

		private async void ShowStatus(string message, bool isSuccess)
		{
			PopupStatusText.Text = message;
			PopupStatus.Background = isSuccess
				? new SolidColorBrush(Color.FromRgb(39, 174, 96))
				: new SolidColorBrush(Color.FromRgb(192, 57, 43));

			PopupStatus.Visibility = Visibility.Visible;

			var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(300)));
			PopupStatus.BeginAnimation(Border.OpacityProperty, fadeIn);

			await Task.Delay(3000);

			var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, new Duration(TimeSpan.FromMilliseconds(300)));
			fadeOut.Completed += (s, e) => PopupStatus.Visibility = Visibility.Collapsed;
			PopupStatus.BeginAnimation(Border.OpacityProperty, fadeOut);
		}
	}
}