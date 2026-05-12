using System;
using System.IO;
using System.Text.Json;

public class RememberMeData
{
	public string Username { get; set; }
	public string Password { get; set; }
	public bool RememberMe { get; set; }
}

public static class RememberMeService
{
	private static readonly string PathFile = "rememberme.json";

	public static void Save(RememberMeData data)
	{
		File.WriteAllText(PathFile, JsonSerializer.Serialize(data));
	}

	public static RememberMeData Load()
	{
		if (!File.Exists(PathFile)) return new RememberMeData();
		try
		{
			return JsonSerializer.Deserialize<RememberMeData>(File.ReadAllText(PathFile)) ?? new RememberMeData();
		}
		catch { return new RememberMeData(); }
	}
}