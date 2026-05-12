using System;
using System.Windows;
using MovieApp_Adminpanel.Services;
using MySqlConnector;


namespace MovieApp_Adminpanel.Windows
{
	public partial class SupportMessageWindow : Window
	{
		private readonly long messageId;
		private readonly DatabaseService db = new DatabaseService();

		public SupportMessageWindow(
			long id,
			string name,
			string subject,
			string message,
			string reply,
			string createdAt)
		{
			InitializeComponent();

			messageId = id;
			DateText.Text = "Отправлено: " + createdAt;
			NameText.Text = name;
			SubjectText.Text = subject;
			MessageText.Text = message;
			ReplyBox.Text = reply;
		}

		private void SendReply_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				string replyText = ReplyBox.Text;

				// ❌ ПУСТАЯ ИЛИ ПРОБЕЛЫ
				if (string.IsNullOrWhiteSpace(replyText))
				{
					MessageBox.Show("Введите текст ответа", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
					return;
				}
				using var conn = db.GetConnection();
				conn.Open();

				using var cmd = new MySqlCommand(@"
                    UPDATE support_messages
                    SET reply_text = @reply,
                        status = 'closed',
                        updated_at = NOW()
                    WHERE id = @id", conn);

				cmd.Parameters.AddWithValue("@reply", ReplyBox.Text);
				cmd.Parameters.AddWithValue("@id", messageId);

				cmd.ExecuteNonQuery();

				MessageBox.Show("Ответ отправлен!");

				Close();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message);
			}
		}
	}
}