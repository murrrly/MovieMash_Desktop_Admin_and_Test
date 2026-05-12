using MovieApp_Adminpanel.Services;
using MySqlConnector;
using System;
using System.Windows;
using System.Windows.Controls;
using Konscious.Security.Cryptography;
using System.Text;
using System.Security.Cryptography;

namespace MovieApp_Adminpanel.Pages
{
	public partial class AdminProfilePage : Page
	{
		private readonly DatabaseService _db = new DatabaseService();

		public AdminProfilePage()
		{
			InitializeComponent();
			LoadAdminData();
		}

		private void LoadAdminData()
		{
			try
			{
				using (var conn = _db.GetConnection())
				{
					conn.Open();

					// Изменил таблицу с administrators на users
					string adminQuery = @"
SELECT username, email, role, last_login 
FROM users 
WHERE id = @id";

					using (var cmd = new MySqlCommand(adminQuery, conn))
					{
						cmd.Parameters.AddWithValue("@id", AdminSession.CurrentAdminId);

						using (var reader = cmd.ExecuteReader())
						{
							if (reader.Read())
							{
								UsernameText.Text = reader["username"]?.ToString();
								EmailText.Text = reader["email"]?.ToString();
								RoleText.Text = reader["role"]?.ToString()?.ToUpper();

								var lastLogin = reader["last_login"];
								LastLoginText.Text = lastLogin != DBNull.Value
									? Convert.ToDateTime(lastLogin).ToString("dd.MM.yyyy HH:mm")
									: "Первый вход";
							}
						}
					}

					// Обновил статистику для новой БД
					ModeratedReviewsText.Text = GetModeratedReviewsCount(conn);
					ActionsTodayText.Text = GetTotalUsersCount(conn);
					ActiveSessionsText.Text = GetActiveSessionsCount(conn);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки профиля: " + ex.Message);
			}
		}

		private string GetModeratedReviewsCount(MySqlConnection conn)
		{
			try
			{
				// Количество промодерированных отзывов (approved или rejected)
				string sql = @"
SELECT COUNT(*) 
FROM user_reviews 
WHERE review_status IN ('approved', 'rejected')";

				using (var cmd = new MySqlCommand(sql, conn))
				{
					return cmd.ExecuteScalar()?.ToString() ?? "0";
				}
			}
			catch
			{
				return "0";
			}
		}

		private string GetTotalUsersCount(MySqlConnection conn)
		{
			try
			{
				// Общее количество активных пользователей
				string sql = "SELECT COUNT(*) FROM users WHERE is_active = true";

				using (var cmd = new MySqlCommand(sql, conn))
				{
					return cmd.ExecuteScalar()?.ToString() ?? "0";
				}
			}
			catch
			{
				return "0";
			}
		}

		private string GetActiveSessionsCount(MySqlConnection conn)
		{
			try
			{
				// Количество активных сессий (не истекших)
				string sql = "SELECT COUNT(*) FROM user_sessions WHERE expires_at > NOW()";

				using (var cmd = new MySqlCommand(sql, conn))
				{
					return cmd.ExecuteScalar()?.ToString() ?? "0";
				}
			}
			catch
			{
				return "0";
			}
		}

		private string ExecuteScalarSafe(MySqlConnection conn, string sql)
		{
			try
			{
				using (var cmd = new MySqlCommand(sql, conn))
				{
					return cmd.ExecuteScalar()?.ToString() ?? "0";
				}
			}
			catch
			{
				return "0";
			}
		}

		private string HashPasswordArgon2id(string password)
		{
			byte[] salt = new byte[16];
			using (var rng = RandomNumberGenerator.Create())
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

		private void ChangePassword_Click(object sender, RoutedEventArgs e)
		{
			string newPassword = PasswordBox.Password.Trim();
			string confirmPassword = ConfirmPasswordBox.Password.Trim();

			if (string.IsNullOrEmpty(newPassword))
			{
				MessageBox.Show("Введите новый пароль!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (newPassword != confirmPassword)
			{
				MessageBox.Show("Пароли не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (newPassword.Length < 6)
			{
				MessageBox.Show("Пароль должен содержать минимум 6 символов!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			try
			{
				using (var conn = _db.GetConnection())
				{
					conn.Open();

					// Хэшируем пароль Argon2id
					string hashedPassword = HashPasswordArgon2id(newPassword);

					// Обновляем в таблице users
					string query = @"
UPDATE users 
SET password_hash = @hash
WHERE id = @id";

					using (var cmd = new MySqlCommand(query, conn))
					{
						cmd.Parameters.AddWithValue("@hash", hashedPassword);
						cmd.Parameters.AddWithValue("@id", AdminSession.CurrentAdminId);
						cmd.ExecuteNonQuery();
					}
				}

				MessageBox.Show("Пароль успешно обновлён! 🔐", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
				PasswordBox.Clear();
				ConfirmPasswordBox.Clear();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка смены пароля: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}