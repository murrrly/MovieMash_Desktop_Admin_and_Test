using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MovieApp_Adminpanel.Services;
using MySqlConnector;

namespace MovieApp_Adminpanel.Pages
{
	public partial class DashboardPage : Page
	{
		private DatabaseService db = new DatabaseService();

		public DashboardPage()
		{
			InitializeComponent();
			_ = LoadDashboardDataAsync(); // Вызываем асинхронный метод
			_ = CheckTmdbStatusAsync();
		}

		private async Task LoadDashboardDataAsync()
		{
			try
			{
				using var conn = db.GetConnection();
				await conn.OpenAsync();

				// 1. Новые пользователи сегодня (используем created_at)
				using (var cmdNew = new MySqlCommand(
					"SELECT COUNT(*) FROM users WHERE DATE(created_at) = CURDATE()", conn))
				{
					var result = await cmdNew.ExecuteScalarAsync();
					NewUsersCount.Text = result?.ToString() ?? "0";
				}

				// 2. Всего пользователей (is_active вместо is_deleted)
				using (var cmdTotal = new MySqlCommand(
					"SELECT COUNT(*) FROM users WHERE is_active = true", conn))
				{
					var result = await cmdTotal.ExecuteScalarAsync();
					TotalUsersCount.Text = result?.ToString() ?? "0";
				}

				// 3. Очередь модерации (таблица user_reviews, поле review_status)
				using (var cmdReviews = new MySqlCommand(
					"SELECT COUNT(*) FROM user_reviews WHERE review_status = 'pending'", conn))
				{
					var result = await cmdReviews.ExecuteScalarAsync();
					PendingReviewsCount.Text = result?.ToString() ?? "0";
				}

				// 4. Последние события
				await LoadRecentEventsAsync(conn);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка БД: " + ex.Message);
			}
		}

		private async Task LoadRecentEventsAsync(MySqlConnection conn)
		{
			try
			{
				// Проверяем существование таблицы user_activity
				var checkTableCmd = new MySqlCommand(@"
					SELECT COUNT(*) 
					FROM information_schema.tables 
					WHERE table_schema = DATABASE() 
					AND table_name = 'user_activity'", conn);

				bool tableExists = Convert.ToInt32(await checkTableCmd.ExecuteScalarAsync()) > 0;

				if (tableExists)
				{
					using (var cmdEvents = new MySqlCommand(@"
						SELECT activity_type, created_at
						FROM user_activity
						ORDER BY created_at DESC
						LIMIT 10", conn))
					using (var reader = await cmdEvents.ExecuteReaderAsync())
					{
						RecentEventsList.Items.Clear();

						while (await reader.ReadAsync())
						{
							string time = Convert.ToDateTime(reader["created_at"]).ToString("HH:mm");
							string type = reader["activity_type"]?.ToString() ?? "";
							RecentEventsList.Items.Add($"[{time}]  {type}");
						}
					}
				}
				else
				{
					// Если таблицы нет, показываем события из других таблиц
					using (var cmdEvents = new MySqlCommand(@"
						(SELECT 'Добавил отзыв' as activity_type, created_at 
						 FROM user_reviews 
						 ORDER BY created_at DESC 
						 LIMIT 5)
						UNION ALL
						(SELECT 'Добавил в список' as activity_type, added_at as created_at 
						 FROM list_items 
						 ORDER BY added_at DESC 
						 LIMIT 5)
						ORDER BY created_at DESC
						LIMIT 10", conn))
					using (var reader = await cmdEvents.ExecuteReaderAsync())
					{
						RecentEventsList.Items.Clear();

						while (await reader.ReadAsync())
						{
							string time = Convert.ToDateTime(reader["created_at"]).ToString("HH:mm");
							string type = reader["activity_type"]?.ToString() ?? "";
							RecentEventsList.Items.Add($"[{time}]  {type}");
						}
					}
				}
			}
			catch
			{
				// Если ошибка, просто показываем заглушку
				RecentEventsList.Items.Clear();
				RecentEventsList.Items.Add("[--:--]  Нет данных о событиях");
			}
		}

		private async Task CheckTmdbStatusAsync()
		{
			try
			{
				using HttpClient client = new HttpClient();
				client.Timeout = TimeSpan.FromSeconds(5);

				var response = await client.GetAsync("https://www.themoviedb.org/");

				if (response.IsSuccessStatusCode)
				{
					TmdbStatus.Text = "ОНЛАЙН";
					StatusDot.Fill = new SolidColorBrush(Colors.Green);
				}
				else
				{
					TmdbStatus.Text = "ОШИБКА";
					StatusDot.Fill = new SolidColorBrush(Colors.Red);
				}
			}
			catch
			{
				TmdbStatus.Text = "OFFLINE";
				StatusDot.Fill = new SolidColorBrush(Colors.Red);
			}
		}

		private void GoToModeration_Click(object sender, RoutedEventArgs e)
		{
			NavigationService?.Navigate(new ModerationPage());
		}

		private void GoToMonitoring_Click(object sender, RoutedEventArgs e)
		{
			NavigationService?.Navigate(new MonitoringPage());
		}

		private void ExitButton_Click(object sender, RoutedEventArgs e)
		{
			if (MessageBox.Show("Закрыть приложение?", "Выход", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
				Application.Current.Shutdown();
		}
	}
}