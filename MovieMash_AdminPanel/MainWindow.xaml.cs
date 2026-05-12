using System.Windows;
using System.Windows.Controls;
using MovieApp_Adminpanel.Pages;

namespace MovieApp_Adminpanel
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			// При старте показываем Dashboard (Главную со статистикой)
			// Подписываемся на событие навигации
			MainFrame.Navigated += MainFrame_Navigated;

			MainFrame.Navigate(new DashboardPage());
		}

		private void MainFrame_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e)
		{
			// Проверяем, какая страница загрузилась, и обновляем UI
			if (e.Content is DashboardPage)
			{
				PageTitle.Text = "Главная";
				SetActiveMenu(MenuDashboard);
			}
			else if (e.Content is ModerationPage)
			{
				PageTitle.Text = "Отзывы и модерация";
				SetActiveMenu(MenuModeration);
			}
			else if (e.Content is MonitoringPage)
			{
				PageTitle.Text = "История логов";
				SetActiveMenu(MenuMonitoring);
			}
		}

		/// <summary>
		/// Устанавливает подсветку активного пункта меню через свойство Tag
		/// </summary>
		private void SetActiveMenu(MenuItem activeItem)
		{
			// Оставляем только те кнопки, которые соответствуют твоим новым задачам
			var items = new[]
			{
				MenuDashboard,
				MenuUsers,
				MenuModeration,
				MenuMonitoring, // Используем его как страницу Логов/Истории
				MenuProfile
			};

			foreach (var item in items)
			{
				if (item != null) item.Tag = null;
			}

			if (activeItem != null) activeItem.Tag = "Active";
		}

		// --- ОБРАБОТЧИКИ НАЖАТИЙ ---

		private void Dashboard_Click(object sender, RoutedEventArgs e)
		{
			MainFrame.Navigate(new DashboardPage());
			SetActiveMenu(MenuDashboard);
			PageTitle.Text = "Главная (Статистика)";
		}

		private void Users_Click(object sender, RoutedEventArgs e)
		{
			MainFrame.Navigate(new UsersPage());
			SetActiveMenu(MenuUsers);
			PageTitle.Text = "Управление пользователями";
		}

		private void Moderation_Click(object sender, RoutedEventArgs e)
		{
			MainFrame.Navigate(new ModerationPage());
			SetActiveMenu(MenuModeration);
			PageTitle.Text = "Отзывы и модерация";
		}

		private void Monitoring_Click(object sender, RoutedEventArgs e)
		{
			// Используем существующую MonitoringPage как страницу логов (истории)
			MainFrame.Navigate(new MonitoringPage());
			SetActiveMenu(MenuMonitoring);
			PageTitle.Text = "История логов и активность";
		}

		private void AdminProfile_Click(object sender, RoutedEventArgs e)
		{
			MainFrame.Navigate(new AdminProfilePage());
			SetActiveMenu(MenuProfile);
			PageTitle.Text = "Профиль администратора";
		}
	}
}