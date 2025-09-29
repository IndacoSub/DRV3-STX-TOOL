using System;

namespace CLI.InputOutput
{
	public static class ShowMessages
	{
		/// <summary>
		/// Check if the user is on Windows
		/// </summary>
		private static readonly bool isOnWindows = OperatingSystem.IsWindows();

		/// <summary>
		/// Check if the user is using the CLI or the WinForm.
		/// </summary>
		private static readonly bool isReallyAConsoleWindow = Environment.UserInteractive &&
			(isOnWindows ? (Console.Title != null && Console.Title.Length > 0) : true);

		/// <summary>
		/// Show an error message in the CLI or in the WinForm.
		/// </summary>
		/// <param name="Message">The text's message you want to show.</param>
		public static void ErrorMessage(string Message)
		{
			if (isReallyAConsoleWindow)
			{
				Console.WriteLine($"Error: {Message}");
			}
		}

		/// <summary>
		/// Show an "ok" message in the CLI or in the WinForm.
		/// </summary>
		/// <param name="Message">The text's message you want to show.</param>
		public static void EventMessage(string Message)
		{
			if (isReallyAConsoleWindow)
			{
				Console.WriteLine($"{Message}");
			}
		}
	}
}