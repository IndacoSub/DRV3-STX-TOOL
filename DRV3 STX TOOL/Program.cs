using System;
using System.IO;

namespace CLI
{
    internal class Program
    {
        private static void Main()
        {
            DRV3.Main.UseTxtInsteadOfPo = true;
            ConfigFile.AppConfig configF = new ConfigFile.AppConfig("App.config");
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

            /*
            Console.CursorVisible = true;
            Console.ResetColor();

            ASCII_Interface consoleInterface = new ASCII_Interface();

            consoleInterface.PrintFullInterface(ConsoleKey.DownArrow);

            while (true)
            {
                ConsoleKey keyPressedByUser = ReadInput.WaitForArrowKeys();
                consoleInterface.PrintFullInterface(keyPressedByUser);
            }
            */
        }
    }
}
