using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MovieApp_Adminpanel.Services;
using MySqlConnector;
using System.Windows.Media.Animation;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;

namespace MovieApp_Adminpanel.Pages
{
	public partial class ModerationPage : Page
	{
		private readonly DatabaseService db = new DatabaseService();
		private readonly string[] badWords =
		{
			"блядь", "блять", "сука", "нахуй", "хуй", "пизда", "пиздец",
			"ебать", "ебаный", "ебаная", "хуета", "хер", "пидор"
		};
		private string GetMediaTitle(string tmdbId, string mediaType)
		{
			if (string.IsNullOrEmpty(tmdbId))
				return "Неизвестно";

			string typeText = mediaType == "tv" ? "📺 Сериал" : "🎬 Фильм";
			return $"{typeText} #{tmdbId}";
		}

		private readonly DispatcherTimer refreshTimer;
		private DateTime lastReviewsUpdate = DateTime.MinValue;
		private DateTime lastSupportUpdate = DateTime.MinValue;
		private long lastReviewId = 0;
		private long lastMessageId = 0;

		public ModerationPage()
		{
			InitializeComponent();
			InitializeLastIds();
			LoadModerationQueue();
			LoadSupportMessages();
			LoadAllReviews("");

			// 🔥 автообновление каждые 10 секунд
			refreshTimer = new DispatcherTimer();
			refreshTimer.Interval = TimeSpan.FromSeconds(10);
			refreshTimer.Tick += RefreshTimer_Tick;
			refreshTimer.Start();
		}
		private void InitializeLastIds()
		{
			using var conn = db.GetConnection();
			conn.Open();

			using var cmd = new MySqlCommand(@"
        SELECT 
            (SELECT MAX(id) FROM user_reviews),
            (SELECT MAX(id) FROM support_messages)
    ", conn);

			using var reader = cmd.ExecuteReader();

			if (reader.Read())
			{
				lastReviewId = reader.IsDBNull(0) ? 0 : Convert.ToInt64(reader.GetValue(0));
				lastMessageId = reader.IsDBNull(1) ? 0 : Convert.ToInt64(reader.GetValue(1));
			}
		}

		// ================= AUTO REFRESH =================
		private void RefreshTimer_Tick(object sender, EventArgs e)
		{
			CheckForNewReviews();
			CheckForNewMessages();
		}

		private void CheckForNewReviews()
		{
			try
			{
				using var conn = db.GetConnection();
				conn.Open();

				using var cmd = new MySqlCommand(@"
			SELECT MAX(id)
			FROM user_reviews", conn);

				var result = cmd.ExecuteScalar();
				if (result == null || result == DBNull.Value) return;

				long maxId = Convert.ToInt64(result);

				if (maxId > lastReviewId)
				{
					lastReviewId = maxId;

					LoadModerationQueue();
					LoadAllReviews("");

					ShowToast("🆕 Новые отзывы");
				}
			}
			catch { }
		}

		private void CheckForNewMessages()
		{
			try
			{
				using var conn = db.GetConnection();
				conn.Open();

				using var cmd = new MySqlCommand(@"
					SELECT MAX(id)
					FROM support_messages", conn);

				var result = cmd.ExecuteScalar();
				if (result == null || result == DBNull.Value) return;

				long maxId = Convert.ToInt64(result);

				if (maxId > lastMessageId)
				{
					lastMessageId = maxId;

					LoadSupportMessages();
					ShowToast("📩 Новая жалоба");
				}
			}
			catch { }
		}

		// ================= TOAST =================
		private async void ShowToast(string text)
		{
			InfoToastText.Text = text;

			InfoToast.Background = new SolidColorBrush(Color.FromRgb(229, 57, 53));

			InfoToast.Visibility = Visibility.Visible;

			var fadeIn = new System.Windows.Media.Animation.DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
			var moveIn = new System.Windows.Media.Animation.DoubleAnimation(-20, 0, TimeSpan.FromMilliseconds(200));

			InfoToast.BeginAnimation(OpacityProperty, fadeIn);
			((TranslateTransform)InfoToast.RenderTransform)
				.BeginAnimation(TranslateTransform.YProperty, moveIn);

			await System.Threading.Tasks.Task.Delay(2000);

			var fadeOut = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(400));
			var moveOut = new System.Windows.Media.Animation.DoubleAnimation(0, -20, TimeSpan.FromMilliseconds(400));

			InfoToast.BeginAnimation(OpacityProperty, fadeOut);
			((TranslateTransform)InfoToast.RenderTransform)
				.BeginAnimation(TranslateTransform.YProperty, moveOut);

			await System.Threading.Tasks.Task.Delay(400);
			InfoToast.Visibility = Visibility.Collapsed;
		}

		private async void ShowInfoToast(string text, bool isError = false)
		{
			InfoToastText.Text = text;

			InfoToast.Background = new SolidColorBrush(
				isError ? Color.FromRgb(229, 57, 53) : Color.FromRgb(45, 45, 45)
			);

			InfoToast.Visibility = Visibility.Visible;

			var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
			var moveIn = new DoubleAnimation(-20, 0, TimeSpan.FromMilliseconds(200));

			InfoToast.BeginAnimation(OpacityProperty, fadeIn);
			((TranslateTransform)InfoToast.RenderTransform)
				.BeginAnimation(TranslateTransform.YProperty, moveIn);

			await System.Threading.Tasks.Task.Delay(2000);

			var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300));
			var moveOut = new DoubleAnimation(0, -20, TimeSpan.FromMilliseconds(300));

			InfoToast.BeginAnimation(OpacityProperty, fadeOut);
			((TranslateTransform)InfoToast.RenderTransform)
				.BeginAnimation(TranslateTransform.YProperty, moveOut);

			await System.Threading.Tasks.Task.Delay(300);

			InfoToast.Visibility = Visibility.Collapsed;
		}

		// ================= SUPPORT MESSAGES =================
		private void LoadSupportMessages()
		{
			var messages = new List<object>();

			using var conn = db.GetConnection();
			conn.Open();

			using var cmd = new MySqlCommand(@"
				SELECT *
				FROM support_messages
				ORDER BY created_at DESC", conn);

			using var reader = cmd.ExecuteReader();

			while (reader.Read())
			{
				string status = reader["status"]?.ToString() ?? "";

				messages.Add(new
				{
					Id = Convert.ToInt64(reader["id"]),
					Name = reader["name"]?.ToString(),
					Email = reader["email"]?.ToString(),
					Subject = reader["subject"]?.ToString(),
					Message = reader["message"]?.ToString(),
					ReplyText = reader["reply_text"]?.ToString(),
					Status = status,

					IsClosed = status == "closed",

					StatusText =
						status == "new" ? "НОВОЕ" :
						status == "in_progress" ? "В РАБОТЕ" :
						status == "closed" ? "ЗАКРЫТО" : status,

					CreatedAt = Convert.ToDateTime(reader["created_at"])
						.ToString("dd.MM.yyyy HH:mm")
				});
			}

			SupportMessagesGrid.ItemsSource = messages;
		}

		// ================= OPEN MESSAGE =================
		private void SupportMessagesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			dynamic row = SupportMessagesGrid.SelectedItem;
			if (row == null) return;

			// ❌ закрытые нельзя отвечать
			if (row.IsClosed)
			{
				ShowInfoToast("Это обращение уже закрыто");
				return;
			}

			var window = new Windows.SupportMessageWindow(
				row.Id,
				row.Name,
				row.Subject,
				row.Message,
				row.ReplyText,
				row.Status
			);

			window.ShowDialog();
			LoadSupportMessages();
		}

		// ================= MODERATION =================
		private void LoadModerationQueue()
		{
			var reviews = new List<object>();

			using var conn = db.GetConnection();
			conn.Open();

			using var cmd = new MySqlCommand(@"
				SELECT ur.id, u.username, ur.tmdb_id, ur.media_type, ur.review_text, ur.created_at
				FROM user_reviews ur
				JOIN users u ON u.id = ur.user_id
				WHERE ur.review_status = 'pending'
				ORDER BY ur.created_at DESC", conn);

			using var reader = cmd.ExecuteReader();

			while (reader.Read())
			{
				string tmdbId = reader["tmdb_id"]?.ToString();
				string mediaType = reader["media_type"]?.ToString();

				reviews.Add(new
				{
					ReviewId = Convert.ToInt64(reader["id"]),
					Username = reader["username"]?.ToString(),

					MovieTitle = GetMediaTitle(tmdbId, mediaType), // 🔥 ВОТ ЭТО ВЕРНУЛИ

					ReviewText = HighlightBadWords(reader["review_text"]?.ToString()),
					CreatedAt = Convert.ToDateTime(reader["created_at"])
						.ToString("dd.MM HH:mm")
				});
			}

			ModerationGrid.ItemsSource = reviews;
		}

		private void Approve_Click(object sender, RoutedEventArgs e)
		{
			dynamic row = (sender as Button)?.DataContext;
			if (row == null) return;

			UpdateStatus(row.ReviewId, "approved");
			LoadModerationQueue();
			LoadAllReviews("");
		}

		private void Reject_Click(object sender, RoutedEventArgs e)
		{
			dynamic row = (sender as Button)?.DataContext;
			if (row == null) return;

			UpdateStatus(row.ReviewId, "rejected");
			LoadModerationQueue();
			LoadAllReviews("");
		}

		private void UpdateStatus(long id, string status)
		{
			using var conn = db.GetConnection();
			conn.Open();

			using var cmd = new MySqlCommand(@"
				UPDATE user_reviews
				SET review_status = @s
				WHERE id = @id", conn);

			cmd.Parameters.AddWithValue("@s", status);
			cmd.Parameters.AddWithValue("@id", id);
			cmd.ExecuteNonQuery();

			ShowInfoToast("Обновлено ✔");
		}

		// ================= SEARCH =================
		private void SearchBoxAll_TextChanged(object sender, TextChangedEventArgs e)
		{
			LoadAllReviews(SearchBoxAll.Text);
		}

		private void AllReviewsTab_Selected(object sender, RoutedEventArgs e)
		{
			LoadAllReviews("");
		}

		private void LoadAllReviews(string search)
		{
			var list = new List<object>();

			using var conn = db.GetConnection();
			conn.Open();

			using var cmd = new MySqlCommand(@"
				SELECT u.username, ur.review_text, ur.review_status, ur.created_at
				FROM user_reviews ur
				JOIN users u ON u.id = ur.user_id
				WHERE ur.review_text LIKE @s
				ORDER BY ur.created_at DESC", conn);

			cmd.Parameters.AddWithValue("@s", "%" + search + "%");

			using var reader = cmd.ExecuteReader();

			while (reader.Read())
			{
				list.Add(new
				{
					Username = reader["username"]?.ToString(),
					ReviewText = reader["review_text"]?.ToString(),
					StatusText = reader["review_status"]?.ToString(),
					CreatedAt = Convert.ToDateTime(reader["created_at"])
						.ToString("dd.MM.yyyy")
				});
			}

			AllReviewsGrid.ItemsSource = list;
		}
		private string HighlightBadWords(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return text;

			foreach (var word in badWords)
			{
				text = System.Text.RegularExpressions.Regex.Replace(
					text,
					word,
					m => $"{m.Value.ToUpper()}",
					System.Text.RegularExpressions.RegexOptions.IgnoreCase
				);
			}

			return text;
		}

		private void ReviewText_Loaded(object sender, RoutedEventArgs e)
		{
			if (sender is not TextBlock tb) return;
			if (tb.DataContext == null) return;

			string text = tb.DataContext
				.GetType()
				.GetProperty("ReviewText")
				?.GetValue(tb.DataContext)?.ToString();

			if (string.IsNullOrWhiteSpace(text)) return;

			tb.Inlines.Clear();

			// ищем все плохие слова
			string pattern = string.Join("|", badWords);
			var regex = new Regex(pattern, RegexOptions.IgnoreCase);

			int lastIndex = 0;

			foreach (Match match in regex.Matches(text))
			{
				// обычный текст ДО слова
				if (match.Index > lastIndex)
				{
					tb.Inlines.Add(new Run(text.Substring(lastIndex, match.Index - lastIndex)));
				}

				// САМО СЛОВО (красное)
				tb.Inlines.Add(new Run("❌")
				{
					Foreground = Brushes.Red,
					FontWeight = FontWeights.Bold
				});

				tb.Inlines.Add(new Run(match.Value.ToUpper())
				{
					Foreground = Brushes.Red,
					FontWeight = FontWeights.Bold
				});

				tb.Inlines.Add(new Run("❌")
				{
					Foreground = Brushes.Red,
					FontWeight = FontWeights.Bold
				});

				lastIndex = match.Index + match.Length;
			}

			// остаток текста
			if (lastIndex < text.Length)
			{
				tb.Inlines.Add(new Run(text.Substring(lastIndex)));
			}
		}
	}
}