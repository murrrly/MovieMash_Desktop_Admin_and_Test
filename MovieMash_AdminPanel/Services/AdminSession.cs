namespace MovieApp_Adminpanel
{
	public static class AdminSession
	{
		public static long CurrentAdminId { get; set; }
		public static string Username { get; set; }
		public static string Email { get; set; }
		public static string Role { get; set; }
		public static string UserRole { get; set; } // добавьте это свойство
	}
}