using System;
using System.IO;
using System.Text;
using CLI.ConfigFile;
using CLI.InputOutput;
using DRV3;

namespace CLI
{
    public class ASCII_Interface
    {
        /// <summary>
        /// Tells the user how to move through the menu.
        /// </summary>
        private readonly string[] commands =
        {
            @"         Use UP, DOWN and ENTER to move",
            @"               through the menu.",
            @""
        };

        /// <summary>
        /// Contains all the options saved from the user.
        /// </summary>
        private readonly AppConfig configF;

        /// <summary>
        /// Contains the top of the interface.
        /// </summary>
        private readonly string[] header =
        {
            @"  +----------------------------------------+",
            @"  |        Danganronpa V3: STX TOOL        |",
            @"  |       Version 1.00 (15 MAY 2019)       |",
            @"  |              by Liquid S!              |",
            @"  +----------------------------------------+",
            @""
        };

        /// <summary>
        /// Its value determine what option should be highlighted.
        /// </summary>
        private int currentSelection;

        /// <summary>
        /// Changes to "true" when the user press "Enter". This boolean is used by "PrintMainMenu" to do the action chosen by
        /// the user only after the Interface has been printed.
        /// </summary>
        private bool doAction;

        /// <summary>
        /// Contains the options from the Main Menu.
        /// </summary>
        private readonly string[] mainMenu =
        {
            @"  +----------------------------------------+",
            @"       Extract text",
            @"       Repack  text",
            @"       Option: Use TXT instead of PO: No Selection (False)",
            @"       Option: Swap English and Japanese: No Selection (False)",
            @"       Exit",
            @"  +----------------------------------------+",
            @""
        };

        /// <summary>
        /// Changes to "true" when the user changes an option.
        /// </summary>
        private bool shouldReloadAfter;

        public ASCII_Interface()
        {
            currentSelection = 0;

            PrintHeader();
            PrintCommands();
            configF = new AppConfig("App.config");
        }

        public void PrintHeader()
        {
            Console.WriteLine(string.Join("\n", header));
        }

        public void PrintCommands()
        {
            Console.WriteLine(string.Join("\n", commands));
        }

        /// <summary>
        /// Print to console the FULL ASCII interface.
        /// </summary>
        public void PrintFullInterface(ConsoleKey keyPressedByUser)
        {
            Console.Clear();
            PrintHeader();
            PrintCommands();
            PrintMainMenu(keyPressedByUser);
        }

        /// <summary>
        /// Print to console the Main Menu.
        /// </summary>
        /// <param name="keyPressedByUser">The key pressed by the user.</param>
        public void PrintMainMenu(ConsoleKey keyPressedByUser)
        {
            doAction = false;

            UpdatePositionOrExecuteOption(keyPressedByUser, mainMenu.Length);

            PrintMenuAndHighlightFocusedOption(mainMenu);

            shouldReloadAfter = false;

            if (doAction)
            {
                ExecuteSelectedOption();
            }

            if (shouldReloadAfter)
            {
                doAction = false;
                PrintFullInterface(keyPressedByUser);
            }
        }

        /// <summary>
        /// Print to console the menu and highlight the current focused option.
        /// </summary>
        /// <param name="menu">Menu where the user is.</param>
        private void PrintMenuAndHighlightFocusedOption(string[] menu)
        {
            for (int i = 0; i < menu.Length; i++)
            {
                if (i == currentSelection)
                {
                    // Highlight the focused option.
                    Console.BackgroundColor = ConsoleColor.DarkYellow;
                    Console.ForegroundColor = ConsoleColor.Black;

                    StringBuilder sb = new StringBuilder(menu[i]);
                    sb[3] = '-';
                    sb[4] = '-';
                    sb[5] = '>';

                    Console.Write(sb + "\n");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(menu[i] + "\n");
                }
            }
        }

        /// <summary>
        /// Update "currentSelection" or execute the option chosen by the user.
        /// </summary>
        /// <param name="keyPressedByUser"></param>
        /// <param name="menuSize"></param>
        private void UpdatePositionOrExecuteOption(ConsoleKey keyPressedByUser, int menuSize)
        {
            switch (keyPressedByUser)
            {
                case ConsoleKey.UpArrow:
                {
                    currentSelection = currentSelection == 1 ? menuSize - 3 : currentSelection - 1;
                    break;
                }

                case ConsoleKey.DownArrow:
                {
                    currentSelection = currentSelection == menuSize - 3 ? 1 : currentSelection + 1;
                    break;
                }

                case ConsoleKey.Enter:
                {
                    if (!shouldReloadAfter)
                    {
                        doAction = true;
                    }

                    break;
                }
            }
        }

        private void ExecuteSelectedOption()
        {
            if (currentSelection == 1)
            {
                if (!Directory.Exists(configF.STX_Folder))
                {
                    ShowMessages.ErrorMessage($"{configF.STX_Folder} doesn't exist!");
                }
                else if (!Directory.Exists(configF.WRD_Folder))
                {
                    ShowMessages.ErrorMessage($"{configF.WRD_Folder} doesn't exist!");
                }
                else
                {
                    ShowMessages.EventMessage("Wait...\n");
                    Main.ExtractTextFromSTXfiles(configF.STX_Folder, configF.WRD_Folder);
                    ShowMessages.EventMessage("Done!");
                }
            }
            else if (currentSelection == 2)
            {
                string outFormatFolder = "EXTRACTED_FILES";

                if (!Directory.Exists(outFormatFolder) ||
                    (Directory.GetFiles(outFormatFolder, "*.po").Length == 0 &&
                     Directory.GetFiles(outFormatFolder, "*.txt").Length == 0))
                {
                    ShowMessages.ErrorMessage($"{outFormatFolder} folder doesn't exist or it's empty!");
                }
                else if (!Directory.Exists(configF.STX_Folder))
                {
                    ShowMessages.ErrorMessage($"{configF.STX_Folder} doesn't exist!");
                }
                else
                {
                    ShowMessages.EventMessage("Wait...\n");
                    uint found = Main.RepackText(outFormatFolder, configF.STX_Folder);
                    if (found == 0)
                    {
                        ShowMessages.EventMessage("No suitable files found! Try changing the option in the main menu.");
                    }
                    else
                    {
                        ShowMessages.EventMessage("Done!");
                    }
                }
            }
            else if (currentSelection == 3)
            {
                Main.UseTxtInsteadOfPo = !Main.UseTxtInsteadOfPo;
                mainMenu[3] = "       Option: Use TXT instead of PO: " + Main.UseTxtInsteadOfPo;
                shouldReloadAfter = true;
            }
            else if (currentSelection == 4)
            {
                Main.SwapENGAndJAP = !Main.SwapENGAndJAP;
                mainMenu[4] = "       Option: Swap English and Japanese: " + Main.SwapENGAndJAP;
                shouldReloadAfter = true;
            }
            else if (currentSelection == mainMenu.Length - 3)
            {
                Environment.Exit(0);
            }
        }
    }
}