// Credits to https://github.com/jpmac26 for explain me how DRV3's files work.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Yarhl.FileFormat;
using Yarhl.Media.Text;

namespace DRV3
{
    //Credits to: https://github.com/jpmac26/DRV3-Tools/blob/master/utils/stx.cpp

    public class STX
    {
        private readonly string filename;
        private readonly uint[] numENG;
        private readonly uint[] numJAP;
        private readonly string[] sentencesENG;
        private readonly string[] sentencesJAP;
        private readonly WRD WRDFile;

        public STX(string fileSTX, string WRDFolder)
        {
            (sentencesENG, numENG) = ReadSentencesFromSTX(fileSTX);
            filename = Path.GetFileNameWithoutExtension(fileSTX);

            string JAPFile = Path.Combine("JAP", filename + ".stx");
            if (File.Exists(JAPFile))
            {
                (sentencesJAP, numJAP) = ReadSentencesFromSTX(JAPFile);
                if (Main.SwapENGAndJAP)
                {
                    var numBAK = numENG;
                    var sentencesBAK = sentencesENG;

                    sentencesENG = sentencesJAP;
                    numENG = numJAP;

                    numJAP = numBAK;
                    sentencesJAP = sentencesBAK;
                }
            }

            string WRDFilePosition = Path.Combine(WRDFolder, filename + ".wrd");

            if (File.Exists(WRDFilePosition))
            {
                WRDFile = new WRD(WRDFilePosition);
            }
        }

        private (string[], uint[]) ReadSentencesFromSTX(string fileSTX)
        {
            using (FileStream fs = new FileStream(fileSTX, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                string[] sentences;

                uint headerSize;
                uint NpointersToRead;

                br.ReadUInt32(); // MagicID
                br.ReadUInt32(); // lang
                br.ReadUInt32(); // unk1
                headerSize = br.ReadUInt32(); // Read header size (in hex)
                br.ReadUInt32(); //unk2
                NpointersToRead = br.ReadUInt32();

                uint[] pointers = new uint[NpointersToRead]; // pointers to positions in the file
                uint[] num = new uint[NpointersToRead]; // "number" of each pointer
                sentences =
                    new string[NpointersToRead]; // the number of pointers corresponds to the number of sentences

                // Skip the header
                fs.Seek(headerSize, SeekOrigin.Begin);

                // All the pointers are close to one another
                // so we can read them one after the other
                for (uint i = 0; i < NpointersToRead; i++)
                {
                    // "num[i] = i" cannot be used because
                    // of string deduplication issues
                    // (which are edge cases)
                    num[i] = br.ReadUInt32();
                    pointers[i] = br.ReadUInt32();
                }

                for (uint i = 0; i < NpointersToRead; i++)
                {
                    // For (NpointersToRead), jump to the position
                    // of the pointer, and read the data from there
                    fs.Seek(pointers[i], SeekOrigin.Begin);

                    ushort Letter = 0;
                    string tempSentence = string.Empty;

                    // Read the string until an unsupported character is found,
                    // or the end of stream is reached
                    while (fs.Position != fs.Length && (Letter = br.ReadUInt16()) > 0)
                    {
                        tempSentence += (char)Letter;
                    }

                    // If the string is empty, replace it with "[EMPTY_LINE]"
                    if (tempSentence == string.Empty)
                    {
                        sentences[i] = "[EMPTY_LINE]";
                    }
                    else
                    {
                        // Replace \r\n with \n first, then delete any remaining \r
                        sentences[i] = tempSentence.Replace("\r\n", "\n").Replace("\r", string.Empty);
                    }
                }

                return (sentences, num);
            }
        }

        public string ExpressionByLineNumber(int linenum, string character)
        {
            if (WRDFile == null || character == null || character.Length <= 0)
            {
                return "";
            }

            if (character == "chara_Hatena" || character == "chara_Blank")
            {
                return "";
            }

            if (!WRDFile.charaExpressions.ContainsKey(character))
            {
                return "";
            }

            string expr = "";
            if (WRDFile.charaExpressions[character] == null)
            {
                return "";
            }

            var charaexpressions = WRDFile.charaExpressions[character];
            if (charaexpressions == null)
            {
                Console.WriteLine("No animations found for " + character + "!");
                return "";
            }

            if (charaexpressions.InitialAnimation == null ||
                string.IsNullOrWhiteSpace(charaexpressions.InitialAnimation) ||
                charaexpressions.InitialAnimation == "C999_ABCDE")
            {
                if (character != "chara_Blank" && character != "chara_Hatena" && character != "non")
                {
                    Console.WriteLine("Invalid initial animation for " + character + "!");
                }

                return "";
            }

            if (charaexpressions.Expressions.Count <= 0)
            {
                return charaexpressions.InitialAnimation;
            }

            // Never-seen this character? -> Use default animation
            var first_anim = charaexpressions.Expressions.First();
            uint firstkey = first_anim.Key;
            if (linenum < firstkey)
            {
                return charaexpressions.InitialAnimation;
            }

            for (int j = 0; j < charaexpressions.Expressions.Count; j++)
            {
                var keys = charaexpressions.Expressions.Keys;
                uint lastkey = 0;
                foreach (var key in keys)
                {
                    if (linenum < key)
                    {
                        break;
                    }

                    lastkey = key;
                }

                if (!charaexpressions.Expressions.TryGetValue(lastkey, out string temp))
                {
                    return expr;
                }

                expr = temp;
            }

            return expr;
        }

        public string VoicelineByLineNumber(int linenum)
        {
            if (WRDFile == null)
            {
                return "";
            }

            string line = "";

            if (!WRDFile.voiceLines.TryGetValue((uint)linenum, out line))
            {
                return "";
            }

            return line;
        }

        public void ConvertToPo(string DestinationDir)
        {
            //Read the language used by the user' OS, this way the editor can spellcheck the translation.
            CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;

            Po po = new Po
            {
                Header = new PoHeader("DRV3", "your_email", currentCulture.Name)
            };

            for (int i = 0; i < sentencesENG.Length; i++)
            {
                PoEntry entry = new PoEntry();
                entry.Context = $"{numENG[i]:D4} | {filename}";

                // Print the "Speaker".
                if (WRDFile != null && WRDFile.charaNames.Any())
                {
                    string chara = "";
                    if (i < WRDFile.charaNames.Count && WRDFile.charaNames.ContainsKey((uint)i))
                    {
                        chara = WRDFile.charaNames[(uint)i];
                        entry.Context += $" | {chara}";
                    }
                    else
                    {
                        entry.Context += $" | {"ERROR"}";
                    }

                    string anim = ExpressionByLineNumber(i, chara);

                    if (!string.IsNullOrWhiteSpace(anim))
                    {
                        entry.Context += $" | {anim}";
                    }

                    string voice = VoicelineByLineNumber(i);
                    if (!string.IsNullOrWhiteSpace(voice))
                    {
                        entry.Context += $" | {voice}";
                    }
                }

                // Print the original sentence.
                if (sentencesENG[i] == "" || sentencesENG[i] == string.Empty)
                {
                    entry.Original = "[EMPTY_LINE]";
                    entry.Translated = "[EMPTY_LINE]";
                }
                else if (sentencesENG[i].Length == 1 || sentencesENG[i] == " \n" || sentencesENG[i] == "\n" ||
                         sentencesENG[i] == "..." || sentencesENG[i] == "…" || sentencesENG[i] == "...\n" ||
                         sentencesENG[i] == "…\n" || sentencesENG[i] == "\"...\"" || sentencesENG[i] == "\"…\"" ||
                         sentencesENG[i] == "\"...\n\"" || sentencesENG[i] == "\"…\n\"")
                {
                    // Automatically translate those sentences that doesn't need a translation.
                    entry.Original = sentencesENG[i];
                    entry.Translated = sentencesENG[i];
                }
                else
                {
                    entry.Original = sentencesENG[i];
                }

                if (sentencesJAP != null && sentencesJAP.Any() && sentencesJAP.Length > i && sentencesJAP[i].Length > 0)
                {
                    // The "replaces" are a fix for a Yarhl's bug.
                    entry.ExtractedComments = sentencesJAP[i].Replace("\r\n", "\n#. ").Replace("\n\r", "\n#. ")
                        .Replace("\n", "\n#. ").Replace("\r", string.Empty);
                    ;
                }

                po.Add(entry);
            }

            if (!Directory.Exists(DestinationDir))
            {
                Directory.CreateDirectory(DestinationDir);
            }

            string NewPOAddress = Path.Combine(DestinationDir, filename + ".po");

            po.ConvertWith<Po2Binary, Po, BinaryFormat>().Stream.WriteTo(NewPOAddress);
        }

        public void ConvertToTxt(string DestinationDir)
        {
            string NewTXTAddress = Path.Combine(DestinationDir, filename + ".txt");

            for (int i = 0; i < sentencesENG.Length; i++)
            {
                if (sentencesENG[i] == "" || sentencesENG[i] == string.Empty)
                {
                    sentencesENG[i] = "[EMPTY_LINE]";
                }

                sentencesENG[i] = sentencesENG[i].Replace("\n", "\\n");
            }

            if (!Directory.Exists(DestinationDir))
            {
                Directory.CreateDirectory(DestinationDir);
            }

            File.WriteAllLines(NewTXTAddress, sentencesENG);
        }

        public uint[] GetNumENG()
        {
            return numENG;
        }

        public uint[] GetNumJAP()
        {
            return numJAP;
        }
    }
}