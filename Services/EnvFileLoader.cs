internal static class EnvFileLoader
{
	public static void Load(string path)
	{
		if (!File.Exists(path))
		{
			return;
		}

		foreach (var rawLine in File.ReadAllLines(path))
		{
			var line = rawLine.Trim();
			if (line.Length == 0 || line.StartsWith('#'))
			{
				continue;
			}

			var separatorIndex = line.IndexOf('=');
			if (separatorIndex <= 0)
			{
				continue;
			}

			var key = line[..separatorIndex].Trim();
			var value = line[(separatorIndex + 1)..].Trim().Trim('"');

			if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
			{
				Environment.SetEnvironmentVariable(key, value);
			}
		}
	}
}