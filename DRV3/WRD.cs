// Credits to https://github.com/jpmac26 for explain me how DRV3's files work.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DRV3
{
	public class WRD
	{
		public class WRDAnimation
		{
			public Dictionary<uint, string> Expressions = new Dictionary<uint, string>();
			public string InitialAnimation = "";
		}

		public Dictionary<string, WRDAnimation> charaExpressions = new Dictionary<string, WRDAnimation>();
		public Dictionary<uint, string> charaNames = new Dictionary<uint, string>();
		public Dictionary<uint, string> voiceLines = new Dictionary<uint, string>();

		public WRD(string fileWRD)
		{
			(charaNames, charaExpressions, voiceLines) = ReadSpeakersFromWRD(fileWRD);
		}

		private void PrintDebug(string str)
		{
			if(DRV3.Main.ViewWRD)
			{
				Console.WriteLine(str);
			}
		}

		private (Dictionary<uint, string>, Dictionary<string, WRDAnimation>, Dictionary<uint, string>)
			ReadSpeakersFromWRD(string fileWRD)
		{
			charaNames = new Dictionary<uint, string>();
			charaExpressions = new Dictionary<string, WRDAnimation>();
			voiceLines = new Dictionary<uint, string>();

			using (FileStream fs = new FileStream(fileWRD, FileMode.Open, FileAccess.Read))
			using (BinaryReader br = new BinaryReader(fs))
			{
				ushort str_count = br.ReadUInt16();
				ushort label_count = br.ReadUInt16();
				ushort param_count = br.ReadUInt16();
				ushort sublabel_count = br.ReadUInt16();

				br.ReadUInt32(); // Padding???

				uint sublabel_offsets_ptr = br.ReadUInt32();
				uint label_offsets_ptr = br.ReadUInt32(); //opCodesZoneEnd
				uint label_names_ptr = br.ReadUInt32();
				uint params_ptr = br.ReadUInt32();
				uint str_ptr = br.ReadUInt32();

				string[] paramsList = new string[param_count];

				// Read Params
				fs.Seek(params_ptr, SeekOrigin.Begin);

				//Console.WriteLine(param_count + " parameters:");
				for (int i = 0; i < param_count; i++)
				{
					byte[] sentence = new byte[br.ReadByte()];
					fs.Read(sentence, 0, sentence.Length);

					paramsList[i] = Encoding.Default.GetString(sentence);
					//Console.WriteLine("\t" + i.ToString() + ": " + paramsList[i]);

					br.ReadByte(); // = 0x0
				}

				// Read OP Codes
				fs.Seek(0x20, SeekOrigin.Begin);

				byte[] last_loc = new byte[2];
				uint speakerCode = 0;
				uint anim = 0;
				uint voiceline = uint.MaxValue;
				byte lastByte = 0;

				Dictionary<uint, uint> temp_animations = new Dictionary<uint, uint>();

				while (fs.Position != fs.Length && fs.Position < label_offsets_ptr)
				{
					if (br.ReadByte() != 0x70)
					{
						continue;
					}

					byte tempVar = br.ReadByte();

					// This is an atrocity, I'm well aware, but I did it with future features in mind. 
					switch (tempVar)
					{
						case 0x00: // FLG - Set Flag
							PrintDebug("FLG");
							br.ReadUInt32();
							break;
						case 0x01: // IFF  -   If Flag
							PrintDebug("IFF");
							break;
						case 0x02: // WAK  -   Wake? Work? (Seems to be used to configure game engine parameters)
							PrintDebug("WAK");
							break;
						case 0x03: // IWAK -   If WAK
							PrintDebug("IWAK");
							br.ReadUInt32();
							br.ReadUInt16();
							break;
						case 0x04: // SWI  -   Begin switch statement
							PrintDebug("SWI");
							br.ReadUInt16();
							break;
						case 0x05: // CAS  -   Switch Case
							PrintDebug("CAS");
							br.ReadUInt16();
							break;
						case 0x06: // MPF  -   Map Flag?
							PrintDebug("MPF");
							br.ReadUInt32();
							br.ReadUInt16();
							break;
						case 0x07: //SPW
							PrintDebug("SPW");
							break;
						case 0x08: // MOD  -   Set Modifier (Also used to configure game engine parameters)
							PrintDebug("MOD");
							br.ReadUInt64();
							break;
						case 0x09: // HUM  -   Human? Seems to be used to initialize "interactable" objects in a map?
							PrintDebug("HUM");
							break;
						case 0x0A: // CHK  -   Check?
							PrintDebug("CHK");
							br.ReadUInt16();
							break;
						case 0x0B: // KTD  -   Kotodama?
							PrintDebug("KTD");
							br.ReadUInt32();
							break;
						case 0x0C: // CLR  -   Clear?
							PrintDebug("CLR");
							break;
						case 0x0D: // RET  -   break? There's another command later which is definitely break, though...
							PrintDebug("RET");
							break;
						case 0x0E: // KNM  -   Kinematics (camera movement)
							PrintDebug("KNM");
							br.ReadUInt16();
							br.ReadUInt16();

							byte[] chr = BitConverter.GetBytes(br.ReadUInt16());
							Array.Reverse(chr);
							uint knmchr = BitConverter.ToUInt16(chr, 0);

							byte[] animation = BitConverter.GetBytes(br.ReadUInt16());
							Array.Reverse(animation);
							anim = BitConverter.ToUInt16(animation, 0);

							if (temp_animations.ContainsKey(knmchr))
							{
								temp_animations.Remove(knmchr);
							}

							temp_animations.Add(knmchr, anim);

							br.ReadUInt16();
							break;
						case 0x0F: // CAP  -   Camera Parameters?
							PrintDebug("CAP");
							break;
						case 0x10: // FIL  -   Load Script File & jump to label
							PrintDebug("FIL");
							br.ReadUInt32();
							break;
						case 0x11: // END  -   End of script or switch case
							PrintDebug("END");
							break;
						case 0x12: // SUB  -   Jump to subroutine
							PrintDebug("SUB");
							br.ReadUInt32();
							break;
						case 0x13: // RTN  -   break (called inside subroutine)
							PrintDebug("RTN");
							break;
						case 0x14: // LAB  -   Label number
							PrintDebug("LAB");
							break;
						case 0x15: // JMP  -   Jump to label
							br.ReadUInt16();
							PrintDebug("JMP");
							break;
						case 0x16: // MOV  -   Movie
							PrintDebug("MOV");
							br.ReadUInt32();
							break;
						case 0x17: // FLS  -   Flash
							PrintDebug("FLS");
							br.ReadUInt64();
							break;
						case 0x18: // FLM  -   Flash Modifier?
							PrintDebug("FLM");
							br.ReadUInt64();
							br.ReadUInt32();
							break;
						case 0x19: // VOI  -   Play voice clip
							PrintDebug("VOI");
							byte[] line = BitConverter.GetBytes(br.ReadUInt16());
							br.ReadUInt16(); // Volume, unneeded
							Array.Reverse(line);
							voiceline = BitConverter.ToUInt16(line, 0);
							break;
						case 0x1A: // BGM  -   Play BGM
							PrintDebug("BGM");
							br.ReadUInt32();
							br.ReadUInt16();
							break;
						case 0x1B: // SE_  -   Play sound effect
							PrintDebug("SE_");
							break;
						case 0x1C: // JIN  -   Play jingle
							PrintDebug("JIN");
							br.ReadUInt32();
							break;
						case 0x1D: // CHN  -   Set active character ID (current person speaking)
							PrintDebug("CHN");
							byte[] temp1 = BitConverter.GetBytes(br.ReadInt16());
							Array.Reverse(temp1);
							speakerCode = BitConverter.ToUInt16(temp1, 0);
							break;
						case 0x1E: // VIB  -   Camera Vibration
							PrintDebug("VIB");
							break;
						case 0x1F: // FDS  -   Fade Screen
							PrintDebug("FDS");
							br.ReadUInt32();
							br.ReadUInt16();
							break;
						case 0x20: // FLA  -   Camera Vibration
							PrintDebug("FLA");
							break;
						case 0x21: // LIG  -   Lighting Parameters
							PrintDebug("LIG");
							br.ReadUInt32();
							br.ReadUInt16();
							break;
						case 0x22: // CHR  -   Character Parameters
							PrintDebug("CHR");
							br.ReadInt16();
							byte[] temp3 = BitConverter.GetBytes(br.ReadInt16());
							Array.Reverse(temp3);
							speakerCode = BitConverter.ToUInt16(temp3, 0);
							//InputManager.Print(fs.Position.ToString() + ", found: " + speakerCode.ToString() + " (" + temp3.ToString() + ")");
							byte[] initial_animation = BitConverter.GetBytes(br.ReadInt16());
							Array.Reverse(initial_animation);
							anim = BitConverter.ToUInt16(initial_animation, 0);

							if (temp_animations.ContainsKey(speakerCode))
							{
								temp_animations.Remove(speakerCode);
							}

							temp_animations.Add(speakerCode, anim);

							//InputManager.Print(anim.ToString());
							break;
						case 0x23: // BGD  -   Background Parameters
							PrintDebug("BGD");
							br.ReadUInt64();
							break;
						case 0x24: // CUT  -   Cutin (display image for things like Truth Bullets, etc.)
							PrintDebug("CUT");
							br.ReadUInt32();
							break;
						case 0x25: // ADF  -   Character Vibration?
							PrintDebug("ADF");
							br.ReadUInt64();
							br.ReadUInt16();
							break;
						case 0x26: // PAL  -   ???
							PrintDebug("PAL");
							break;
						case 0x27: // MAP  -   Load Map
							PrintDebug("MAP");
							break;
						case 0x28: // OBJ  -   Load Object
							PrintDebug("OBJ");
							br.ReadUInt32();
							br.ReadUInt16();
							break;
						case 0x29: // BUL  - ???
							PrintDebug("BUL");
							br.ReadUInt64();
							br.ReadUInt64();
							break;
						case 0x2A: // CRF  -   Cross Fade
							PrintDebug("CRF");
							br.ReadUInt64();
							br.ReadUInt32();
							br.ReadUInt16();
							break;
						case 0x2B: // CAM  -   Camera command
							PrintDebug("CAM");
							br.ReadUInt64();
							br.ReadUInt16();
							break;
						case 0x2C: // KWM  -   Game/UI Mode
							PrintDebug("KWM");
							br.ReadUInt16();
							break;
						case 0x2D: // ARE  -   ???
							PrintDebug("ARE");
							br.ReadInt32();
							br.ReadInt16();
							break;
						case 0x2E: // KEY  -   Enable/disable "key" items for unlocking areas
							PrintDebug("KEY");
							br.ReadInt32();
							break;
						case 0x2F: // WIN  -   Window parameters
							PrintDebug("WIN");
							br.ReadInt64();
							break;
						case 0x30: // MSC  -   ???
							PrintDebug("MSC");
							break;
						case 0x31: // CSM  -   ???
							PrintDebug("CSM");
							break;
						case 0x32: // PST  -   Post-Processing
							PrintDebug("PST");
							break;
						case 0x33: // KNS  -   Kinematics Numeric parameters?
							PrintDebug("KNS");
							br.ReadInt64();
							br.ReadInt16();
							break;
						case 0x34: // FON  -   Set Font
							PrintDebug("FON");
							br.ReadInt32();
							break;
						case 0x35: // BGO  -   Load Background Object
							PrintDebug("BGO");
							br.ReadInt64();
							br.ReadInt16();
							break;
						case 0x36: // LOG  -   Edit Text Backlog?
							PrintDebug("LOG");
							break;
						case 0x37: // SPT  -   Used only in Class Trial? Always set to "non"?
							PrintDebug("SPT");
							br.ReadInt16();
							break;
						case 0x38: // CDV  -   ???
							PrintDebug("CDV");
							br.ReadUInt64();
							br.ReadUInt64();
							br.ReadUInt32();
							break;
						case 0x39: // SZM  -   Size Modifier (Class Trial)?
							PrintDebug("SZM");
							br.ReadUInt64();
							break;
						case 0x3A: // PVI  -   Class Trial Chapter? Pre-trial intermission?
							PrintDebug("PVI");
							br.ReadUInt16();
							break;
						case 0x3B: // EXP  -   Give EXP
							PrintDebug("EXP");
							break;
						case 0x3C: // MTA  -   Used only in Class Trial? Usually set to "non"?
							PrintDebug("MTA");
							br.ReadUInt16();
							break;
						case 0x3D: // MVP  -   Move object to its designated position?
							PrintDebug("MVP");
							br.ReadUInt32();
							br.ReadUInt16();
							break;
						case 0x3E: // POS  -   Object/Exisal position
							PrintDebug("POS");
							br.ReadUInt64();
							br.ReadUInt16();
							break;
						case 0x3F: // ICO  -   Display a Program World character portrait
							PrintDebug("ICO");
							br.ReadUInt64();
							break;
						case 0x40: // EAI  -   Exisal AI
							PrintDebug("EAI");
							br.ReadUInt64();
							br.ReadUInt64();
							br.ReadUInt32();
							break;
						case 0x41: // COL  -   Set object collision
							PrintDebug("COL");
							br.ReadInt32();
							br.ReadInt16();
							break;
						case 0x42: // CFP  -   Camera Follow Path? Seems to make the camera move in some way
							PrintDebug("CFP");
							br.ReadInt64();
							br.ReadInt64();
							br.ReadInt16();
							break;
						case 0x43: // CLT  -   Text modifier command
							PrintDebug("CLT");
							br.ReadInt16();
							break;
						case 0x44: // R=   -   ???
							PrintDebug("R=");
							break;
						case 0x45: // PAD= -   Gamepad button symbol
							PrintDebug("PAD");
							break;
						case 0x46: // LOC= -   Display text string
							PrintDebug("LOC");
							byte[] temp2 = BitConverter.GetBytes(br.ReadInt16());
							Array.Reverse(temp2);
							charaNames.Add(BitConverter.ToUInt16(temp2, 0), paramsList[speakerCode]);
							last_loc = temp2;

							if (voiceline != uint.MaxValue)
							{
								//PrintDebug("Voiceline found at position: " + voiceline + ", is: " + paramsList[voiceline]);
								voiceLines.Add(BitConverter.ToUInt16(temp2, 0), paramsList[voiceline]);
								voiceline = uint.MaxValue;
							}

							foreach (var key in temp_animations)
							{
								WRDAnimation wrdanim = new WRDAnimation();
								wrdanim.InitialAnimation = paramsList[key.Value];
								if (!charaExpressions.ContainsKey(paramsList[key.Key]))
								{
									charaExpressions.Add(paramsList[key.Key], wrdanim);
								}
								else
								{
									// ???
									if (charaExpressions[paramsList[key.Key]].Expressions == null)
									{
										charaExpressions[paramsList[key.Key]].Expressions =
											new Dictionary<uint, string>();
									}

									if (charaExpressions[paramsList[key.Key]].Expressions
										.ContainsKey(BitConverter.ToUInt16(last_loc, 0)))
									{
										charaExpressions[paramsList[key.Key]].Expressions
											.Remove(BitConverter.ToUInt16(last_loc, 0));
									}

									charaExpressions[paramsList[key.Key]].Expressions
										.Add(BitConverter.ToUInt16(last_loc, 0), paramsList[key.Value]);
								}
							}

							break;
						case 0x47: // BTN  -   Wait for button press
							PrintDebug("BTN");
							break;
						case 0x48: // ENT  -   ???
							PrintDebug("ENT");
							break;
						case 0x49: // CED  -   Check End (Used after IFF and IFW commands)
							PrintDebug("CED");
							break;
						case 0x4A: // LBN  -   Local Branch Number (for branching case statements)
							PrintDebug("LBN");
							br.ReadInt16();
							break;
						case 0x4B: // JMN  -   Jump to Local Branch (for branching case statements)
							PrintDebug("JMN");
							br.ReadInt16();
							break;
						default:
							if (DRV3.Main.ViewWRD)
							{
								Console.WriteLine("Unrecognized command: " + tempVar);
							}
							break;
					}

					lastByte = tempVar;
				}
			}

			return (charaNames, charaExpressions, voiceLines);
		}
	}
}