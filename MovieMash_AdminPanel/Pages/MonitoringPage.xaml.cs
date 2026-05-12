using MovieApp_Adminpanel.Services;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MovieApp_Adminpanel.Pages
{
	public partial class MonitoringPage : Page
	{
		private DatabaseService db = new DatabaseService();

		public MonitoringPage()
		{
			InitializeComponent();
			LoadLogs();
		}

		private void LoadLogs()
		{
			var logs = new List<dynamic>();

			try
			{
				using var conn = db.GetConnection();
				conn.Open();

				bool tableExists = CheckTableExists(conn, "user_activity");

				if (tableExists)
				{
					using var cmd = new MySqlCommand(@"
                        SELECT u.username,
                               ua.activity_type,
                               ua.movie_id,
                               ua.created_at
                        FROM user_activity ua
                        JOIN users u ON u.id = ua.user_id
                        ORDER BY ua.created_at DESC
                        LIMIT 100", conn);

					using var reader = cmd.ExecuteReader();

					while (reader.Read())
					{
						logs.Add(new
						{
							Username = reader["username"]?.ToString(),
							Activity = reader["activity_type"]?.ToString(),
							MovieId = reader["movie_id"]?.ToString(),
							Date = Convert.ToDateTime(reader["created_at"])
								.ToString("yyyy-MM-dd HH:mm")
						});
					}
				}
				else
				{
					// REVIEWS
					using var cmdReviews = new MySqlCommand(@"
                        SELECT u.username,
                               ur.tmdb_id as movie_id,
                               CONCAT('Оставил отзыв на ', ur.media_type) as activity_type,
                               ur.created_at
                        FROM user_reviews ur
                        JOIN users u ON u.id = ur.user_id
                        ORDER BY ur.created_at DESC
                        LIMIT 50", conn);

					using var r1 = cmdReviews.ExecuteReader();

					while (r1.Read())
					{
						logs.Add(new
						{
							Username = r1["username"]?.ToString(),
							Activity = r1["activity_type"]?.ToString(),
							MovieId = r1["movie_id"]?.ToString(),
							Date = Convert.ToDateTime(r1["created_at"]).ToString("yyyy-MM-dd HH:mm")
						});
					}
					r1.Close();

					// RATINGS
					using var cmdRatings = new MySqlCommand(@"
                        SELECT u.username,
                               ur.tmdb_id as movie_id,
                               CONCAT('Поставил оценку ', ur.rating, ' на ', ur.media_type) as activity_type,
                               ur.rated_at as created_at
                        FROM user_ratings ur
                        JOIN users u ON u.id = ur.user_id
                        ORDER BY ur.rated_at DESC
                        LIMIT 50", conn);

					using var r2 = cmdRatings.ExecuteReader();

					while (r2.Read())
					{
						logs.Add(new
						{
							Username = r2["username"]?.ToString(),
							Activity = r2["activity_type"]?.ToString(),
							MovieId = r2["movie_id"]?.ToString(),
							Date = Convert.ToDateTime(r2["created_at"]).ToString("yyyy-MM-dd HH:mm")
						});
					}
				}

				LogsGrid.ItemsSource = logs;
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка загрузки логов: " + ex.Message);
			}
		}

		private bool CheckTableExists(MySqlConnection conn, string tableName)
		{
			using var cmd = new MySqlCommand(@"
                SELECT COUNT(*) 
                FROM information_schema.tables 
                WHERE table_schema = DATABASE() 
                AND table_name = @tableName", conn);

			cmd.Parameters.AddWithValue("@tableName", tableName);

			return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
		}

		private void Refresh_Click(object sender, RoutedEventArgs e)
		{
			LoadLogs();
		}

		// ===================== CLICK WINDOW =====================
		private void LogsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			dynamic row = LogsGrid.SelectedItem;
			if (row == null) return;

			var window = new LogDetailsWindow(
				row.Username,
				row.Activity,
				row.Date,
				row.MovieId
			);

			window.ShowDialog();
		}
	}
}