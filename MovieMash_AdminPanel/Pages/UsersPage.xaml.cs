using MovieApp_Adminpanel.Services;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;
using System.Windows.Media;

namespace MovieApp_Adminpanel.Pages
{
	public partial class UsersPage : Page
	{
		private DatabaseService db = new DatabaseService();

		public UsersPage()
		{
			InitializeComponent();
			LoadUsers("");
		}

		private void LoadUsers(string search)
		{
			var users = new List<dynamic>();

			try
			{
				using var conn = db.GetConnection();
				conn.Open();

				// Убрано is_deleted, оставлено только is_active
				string query = @"
                    SELECT username, email, is_active, created_at
                    FROM users
                    WHERE (username LIKE @search OR email LIKE @search)
                    ORDER BY created_at DESC";

				using var cmd = new MySqlCommand(query, conn);
				cmd.Parameters.AddWithValue("@search", $"%{search}%");

				using var reader = cmd.ExecuteReader();

				while (reader.Read())
				{
					bool isActive = Convert.ToBoolean(reader["is_active"]);

					users.Add(new
					{
						Username = reader["username"]?.ToString(),
						Email = reader["email"]?.ToString(),
						IsActive = isActive ? "Активен" : "Забанен",
						StatusColor = isActive ? Brushes.LightGreen : Brushes.Red,
						CreatedAt = Convert.ToDateTime(reader["created_at"]).ToString("dd.MM.yyyy"),
						ActionText = isActive ? "Заблокировать" : "Разблокировать",
						ActionColor = isActive ? Brushes.Red : Brushes.DimGray
					});
				}

				UsersGrid.ItemsSource = users;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки: " + ex.Message);
			}
		}

		private void ToggleStatus_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				var button = sender as Button;
				if (button == null || button.Tag == null) return;

				string userEmail = button.Tag.ToString();

				using var conn = db.GetConnection();
				conn.Open();

				// Обновляем статус пользователя (is_active)
				string query = "UPDATE users SET is_active = NOT is_active WHERE email = @email";

				using var cmd = new MySqlCommand(query, conn);
				cmd.Parameters.AddWithValue("@email", userEmail);
				cmd.ExecuteNonQuery();

				LoadUsers(SearchBox.Text);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка смены статуса: " + ex.Message);
			}
		}

		private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			LoadUsers(SearchBox.Text);
		}

		private void ExportCsv_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				SaveFileDialog dialog = new SaveFileDialog
				{
					Filter = "JSON file (*.json)|*.json",
					FileName = "users.json"
				};

				if (dialog.ShowDialog() == true)
				{
					var exportData = new List<object>();

					foreach (dynamic item in UsersGrid.ItemsSource)
					{
						exportData.Add(new
						{
							item.Username,
							item.Email,
							item.IsActive,
							StatusColor = (item.StatusColor as SolidColorBrush)?.Color.ToString(),
							item.CreatedAt,
							item.ActionText,
							ActionColor = (item.ActionColor as SolidColorBrush)?.Color.ToString()
						});
					}

					var options = new JsonSerializerOptions
					{
						WriteIndented = true
					};

					string json = JsonSerializer.Serialize(exportData, options);
					File.WriteAllText(dialog.FileName, json);

					MessageBox.Show("Готово!");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка экспорта: " + ex.Message);
			}
		}
	}
}