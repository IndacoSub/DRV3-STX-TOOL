using DRV3;
using System;
using System.Diagnostics;
using System.IO;

namespace CLI
{
	public class Program
	{

		private static void Main()
		{
			ConfigFile.AppConfig configF = new ConfigFile.AppConfig("App.config");

			if (DRV3.Main.BatchCompile)
			{
				DRV3.Main.UseTxtInsteadOfPo = true;
				string outFormatFolder = "EXTRACTED_FILES";

				if (!Directory.Exists(outFormatFolder) ||
					((Directory.GetFiles(outFormatFolder, "*.po").Length == 0) && (Directory.GetFiles(outFormatFolder, "*.txt").Length == 0)))
				{
					InputOutput.ShowMessages.ErrorMessage($"{outFormatFolder} folder doesn't exist or it's empty!");
				}
				else if (!Directory.Exists(configF.STX_Folder))
				{
					InputOutput.ShowMessages.ErrorMessage($"{configF.STX_Folder} doesn't exist!");
				}
				else
				{
					InputOutput.ShowMessages.EventMessage("Wait...\n");
					uint found = DRV3.Main.RepackText(outFormatFolder, configF.STX_Folder);
					if (found == 0)
					{
						InputOutput.ShowMessages.EventMessage("No suitable files found! Try changing the option in the main menu.");
					}
					else
					{
						InputOutput.ShowMessages.EventMessage("Done!");
					}
				}
			}
			else
			{
				if (DRV3.Main.ViewWRD)
				{
					string wrdpath = "";    // Paste here manually
					if (wrdpath.Length > 0)
					{
						WRD wrd = new WRD(wrdpath);
					}
					else
					{
						Console.WriteLine("Empty WRD path (DRV3_STX_TOOL -> Program.cs)!");
					}
				}
				else
				{
					Console.CursorVisible = true;
					Console.ResetColor();

					ASCII_Interface consoleInterface = new ASCII_Interface();

					consoleInterface.PrintFullInterface(ConsoleKey.DownArrow);

					while (true)
					{
						ConsoleKey keyPressedByUser = ReadInput.WaitForArrowKeys();
						consoleInterface.PrintFullInterface(keyPressedByUser);
					}
				}
			}
		}
	}
}
