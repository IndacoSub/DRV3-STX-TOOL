using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DRV3
{
	public class GenericTextFormat
	{
		protected List<uint> num; // Pointer number
		protected string originalSTX = string.Empty;

		/// <summary>
		/// Translated sentences.
		/// </summary>
		protected List<string> sentences;

		protected string STXFFIleName = string.Empty;

		/// <summary>
		/// Create the translated STX.
		/// </summary>
		/// <param name="RepackFolder">Folder where the new STX file are going to be placed.</param>
		public void BuildSTX(string RepackFolder)
		{
			// Ensure we have an original STX file to base our repack on
			if (originalSTX != string.Empty && File.Exists(originalSTX))
			{
				byte[] header;

				// STEP 1: Read and store the header from the original STX
				// --------------------------------------------------------
				// We need to preserve the original header exactly, because it contains
				// metadata, magic numbers, and offsets that the game expects.
				using (FileStream oF = new FileStream(originalSTX, FileMode.Open, FileAccess.Read))
				using (BinaryReader STXBBR = new BinaryReader(oF))
				{
					STXBBR.ReadUInt64(); // Skip MagicID + lang (8 bytes)
					STXBBR.ReadUInt32(); // Skip unk1 (4 bytes)
					uint headerSize = STXBBR.ReadUInt32(); // Read header size

					header = new byte[headerSize];

					oF.Seek(0, SeekOrigin.Begin); // Go back to start
					oF.Read(header, 0, header.Length); // Read full header into memory
				}

				// STEP 2: Create the new STX file for writing
				// -------------------------------------------
				using (FileStream NEWOutFile = new FileStream(Path.Combine(RepackFolder, STXFFIleName), FileMode.Create, FileAccess.Write))
				using (BinaryWriter OutFileBW = new BinaryWriter(NEWOutFile))
				using (BinaryWriter TextUnicode = new BinaryWriter(NEWOutFile, Encoding.Unicode))
				{
					// Write the preserved header first
					OutFileBW.Write(header);

					// Remember where the pointer table will be written
					long pointZone = NEWOutFile.Position;

					// This will store the file offsets for each sentence
					long[] sentencesOffset = new long[sentences.Count];

					// STEP 3: Reserve space for the pointer table
					// -------------------------------------------
					// Each entry in the pointer table is 8 bytes:
					//   - 4 bytes: num[i] (ID)
					//   - 4 bytes: offset to sentence
					// We fill with zeroes now and overwrite later.
					for (int i = 0; i < sentences.Count; i++)
					{
						OutFileBW.Write((long)0);
					}

					// STEP 4: Write all sentences to the file
					// ---------------------------------------
					for (int i = 0; i < sentences.Count; i++)
					{
						bool duplicate = false;

						// Check for duplicate text to save space
						for (int x = 0; x < i; x++)
						{
							if (sentences[i] == sentences[x])
							{
								duplicate = true;
								sentencesOffset[i] = sentencesOffset[x];
								break;
							}
						}

						// If not a duplicate, write it to the file
						if (!duplicate)
						{
							sentencesOffset[i] = NEWOutFile.Position;

							// Write the sentence as UTF-16 (Unicode)
							TextUnicode.Write(sentences[i].ToCharArray());

							// Null terminator for the string
							OutFileBW.Write((ushort)0x00);
						}
					}

					// STEP 5: Ensure num[] covers all sentences
					// -----------------------------------------
					// If there are more sentences than original num entries,
					// we generate new unique IDs that don't collide with existing ones.
					if (num.Count < sentences.Count)
					{
						// Track all used numbers for fast lookup
						HashSet<uint> usedNums = new HashSet<uint>(num);

						// Start from the highest existing number + 1, or 0 if empty
						uint candidate = num.Count > 0 ? num.Max() + 1 : 0;

						for (int extra = num.Count; extra < sentences.Count; extra++)
						{
							// Find the next unused number
							while (usedNums.Contains(candidate))
							{
								candidate++;
							}

							num.Add(candidate);
							usedNums.Add(candidate);
						}
					}

					// STEP 6: Write the pointer table
					// --------------------------------
					// Now that we have all offsets and num values, we overwrite
					// the reserved pointer table space.
					NEWOutFile.Seek(pointZone, SeekOrigin.Begin);

					// TODO: should this be < sentencesOffset.Length?
					for (int i = 0; i < sentences.Count; i++)
					{
						OutFileBW.Write(num[i]); // Unique ID for this sentence
						OutFileBW.Write((uint)sentencesOffset[i]); // Offset to sentence data
					}

					// Write sentence count at offset 0x14
					NEWOutFile.Seek(0x14, SeekOrigin.Begin);
					OutFileBW.Write((uint)sentencesOffset.LongLength);

					// TODO: delete everything after (farthest string found from pointers + the string length)
				}
			}
			else
			{
				// If the original STX file is missing, we can't proceed
				Console.WriteLine("Original STX file for " + STXFFIleName.Replace(".stx", ".txt") + " not found!");
			}
		}
	}
}