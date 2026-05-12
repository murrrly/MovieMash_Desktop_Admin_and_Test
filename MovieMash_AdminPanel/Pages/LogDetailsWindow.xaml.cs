using System.Windows;

namespace MovieApp_Adminpanel.Pages
{
	public partial class LogDetailsWindow : Window
	{
		public LogDetailsWindow(string user, string activity, string date, string movieId)
		{
			InitializeComponent();

			UserText.Text = "Пользователь: " + user;
			ActivityText.Text = "Действие: " + activity;
			DateText.Text = "Дата: " + date;
			MovieText.Text = "Фильм ID: " + movieId;
		}
	}
}