	using System;
	using System.Text;
	using System.Reflection;
	using System.Linq;
	using System.Drawing;
	using System.Windows.Forms;
	using System.Collections.Generic;
	using System.Runtime.InteropServices;

	// The 'windowHandle' parameter will contain the window handle for the:
	// - Active window when run by hotkey
	// - Window Location target when run by a Window Location rule
	// - TitleBar Button owner when run by a TitleBar Button
	// - Jump List owner when run from a Taskbar Jump List
	// - Currently focused window if none of these match

	public static class DisplayFusionFunction
	{
		public static int shiftRange = 100;
		public const int horShiftDistance = 10, verShiftDistance = 10;
		public static bool enableSizeVariance = false;
		public static bool enablePositionVariance = true;
		public static bool enableWholeSpaceShift = true;
		public static bool enableOutOfBoundChecks = true;
		public static bool enableWindowsAutoShift = false;
		public static bool enableWindowsPositionTimedShift = false;

		public static string topMarginKey = "topMarginKey";
		public static string bottomMarginKey = "bottomMarginKey";
		public static string leftMarginKey = "leftMarginKey";
		public static string rightMarginKey = "rightMarginKey";
		public static string horizontalTopSplitKey = "horizontalTopSplitKey";
		public static string horizontalMiddleSplitKey = "horizontalMiddleSplitKey";
		public static string horizontalBottomSplitKey = "horizontalBottomSplitKey";
		public static string verticalMiddleSplitKey = "verticalMiddleSplitKey";
		public static string verticalLeftSplitKey = "verticalLeftSplitKey";
		public static string verticalRightSplitKey = "verticalRightSplitKey";
		public static string moveFlagKey = "moveFlagKey";
		public static string horDirectionInsideMarginsKey = "horDirectionInsideMarginsKey";
		public static string verDirectionInsideMarginsKey = "verDirectionInsideMarginsKey";

		public static string verDirPrefixForWindowOutsideMarginsKeyPostfix = "OutsideMarginsVerDirectionKeyPostfix";
		public static string horDirPrefixForWindowOutsideMarginsKeyPostfix = "OutsideMarginsHorDirectionKeyPostfix";

		public static string verMarginsDirectionKey = "verMarginsDirectionKey";
		public static string horMarginsDirectionKey = "horMarginsDirectionKey";
		public static string lastHorShiftKey = "lastHorShiftKey";
		public static string lastVerShiftKey = "lastVerShiftKey";

		public static uint moveWindowTimerDelay = 1000; 

		public static string KEY_SHIFT = "16";
		public static string KEY_CTRL = "17";
		public static string KEY_A = "65";
		public static string KEY_Q = "81";

		public static int FILTER_MONITOR_WIDTH = 3840; //1920;
		public static int FILTER_MONITOR_HEIGHT = 2160; //1200;

		public static Dictionary<string, int> windowDirectionsDict = new Dictionary<string, int>();
		public enum WindowHorizontalPosition
		{
			Left,
			Right,
			Middle
		}
		
		public class MarginStatus
		{
			public bool tooHigh { get; set; }
			public bool tooLow { get; set; }
			public bool tooLeft { get; set; }
			public bool tooRight { get; set; }
		}


		public static void Run(IntPtr windowHandle)
		{
			//these are all of the functions from the "Window Management" functions list
			//the function are just called by their names. to find their names, you can copy them
			//from the context menus, or type "BFS.DisplayFusion.RunFunction(" and a window will come up
			//with all of the available functions
			//Regarding the "--- Cancel ---" entries, these are used to cancel the action, see below "MenuItem_Click"

			// BFS.ScriptSettings.WriteValue(moveFlagKey, false.ToString());
			// MessageBox.Show("Stopping move" + BFS.Application.GetAppIDByWindow(windowHandle).ToString());

			string[, ,] MenuEntries = 
				{
					//	{{ "Background-Color", "Foreground-Color", "Function-Name" }}
					
					{{ "Aquamarine", "Black", "SingleWindow100" }},
					{{ "Aquamarine", "Black", "SingleWindow95" }},
					{{ "Aquamarine", "Black", "SingleWindow90" }},
					{{ "Aquamarine", "Black", "SingleWindow80" }},
					{{ "Aquamarine", "Black", "SingleWindow60" }},
					
					{{ "Khaki", "Black", "Left" }},
					{{ "Khaki", "Black", "Right" }},
					
					// {{ "PaleGreen", "Black", "TopLeft" }},
					
					{{ "DodgerBlue", "Black", "TmpMargins" }},
					
					{{ "DodgerBlue", "Black", "SixSplitLeft" }},
					{{ "DodgerBlue", "Black", "SixSplitMiddle" }},
					{{ "DodgerBlue", "Black", "SixSplitRight" }},
					
					{{ "DarkViolet", "Black", "NineSplitLeft" }},
					{{ "DarkViolet", "Black", "NineSplitMiddle" }},
					{{ "DarkViolet", "Black", "NineSplitRight" }},

					{{ "PaleGreen", "Black", "StartMovingWindows"}},
					
					{{ "Pink", "Maroon", "--- Cancel ---" }}
				};

			// create a new ContextMenuStrip to show the items
			using(ContextMenuStrip menu = new ContextMenuStrip())
			{
				//dont show the padding on the left of the menu
				menu.ShowCheckMargin = false;
				menu.ShowImageMargin = false;

				//add items to the menu, and use our custom function when a user clicks on the items
				for ( int i = 0; i < ( MenuEntries.Length / MenuEntries.Rank ); i++ )
				{
					menu.Items.Add(MenuEntries[i, 0, 2]); // add function name into PPM menu an entry
					menu.Items[menu.Items.Count - 1].Click += (sender, e) => MenuItem_Click(sender, e, windowHandle);; // always invoke MenuItem_Click
					menu.Items[menu.Items.Count - 1].BackColor = Color.FromName( MenuEntries[i, 0, 0]);
					menu.Items[menu.Items.Count - 1].ForeColor = Color.FromName( MenuEntries[i, 0, 1]);
				}

				//if the menu will show on the screen, show it. otherwise, show it above the mouse
				if(BFS.Monitor.GetMonitorBoundsByMouseCursor().Contains(new Point(BFS.Input.GetMousePositionX(), BFS.Input.GetMousePositionY() + menu.Height)))
					menu.Show(BFS.Input.GetMousePositionX(), BFS.Input.GetMousePositionY());
				else
					menu.Show(new Point(BFS.Input.GetMousePositionX(), BFS.Input.GetMousePositionY()), ToolStripDropDownDirection.AboveRight);

				//set focus to the menu
				BFS.Window.Focus(menu.Handle);

				//wait for the menu to close
				while(menu.Visible)
				Application.DoEvents();
			}
		}
		
		private static void MenuItem_Click(object sender, EventArgs e, IntPtr windowHandle)
		{
			
			ToolStripItem item = sender as ToolStripItem;
			if (item == null || item.Text == "--- Cancel ---")
			{
				BFS.ScriptSettings.WriteValue(moveFlagKey, false.ToString());
				enableWindowsPositionTimedShift = false;
				return;
			}

			generateSplitBordersAllMonitors();
			RunMy(item.Text, windowHandle);
		}

		private static void RunMy(string functionName, IntPtr windowHandle)
		{
			// Get type, that contains function
			Type type = typeof(DisplayFusionFunction); 
			
			// Get method based on the name
			MethodInfo method = type.GetMethod(functionName);
			
			if (method != null)
			{
				method.Invoke(null, new object[] { windowHandle });
			}
			else
			{
				MessageBox.Show("Function not found: " + functionName, "Error", MessageBoxButtons.OK);
			}
		}

		public static void StartMovingWindows(IntPtr windowHandle)
		{
			// MessageBox.Show("StartMovingWindows start" + BFS.Application.GetAppIDByWindow(windowHandle).ToString());
			bool alreadyMovingWindows = false;
			bool.TryParse(BFS.ScriptSettings.ReadValue(moveFlagKey), out alreadyMovingWindows);

			if(!alreadyMovingWindows)
			{
				//MessageBox.Show("Starting MoveWindows" + BFS.Application.GetAppIDByWindow(windowHandle).ToString() + " " + BFS.Window.GetText(windowHandle));
				MoveWindows();
			}
			else
			{
				MessageBox.Show("StartMovingWindows FALSE" + BFS.Application.GetAppIDByWindow(windowHandle).ToString()+ " " + BFS.Window.GetText(windowHandle));
			}
		}
		public static void MoveWindows()
		{
			enableWindowsPositionTimedShift = true;
			while(enableWindowsPositionTimedShift)
			{
				BFS.ScriptSettings.WriteValue(moveFlagKey, true.ToString());
				BFS.General.ThreadWait(moveWindowTimerDelay); // first wait so that move is not instant

				shiftMargins();

				foreach(IntPtr windowHandle in GetFilteredWindowHandles())
				{
					// MessageBox.Show("Processing window" + BFS.Application.GetAppIDByWindow(windowHandle).ToString() + " " + BFS.Window.GetText(windowHandle));

					if (wasWindowInsideMargins(windowHandle))
					{
						HandleWindowInsideMargins(windowHandle);
					}
					else // OUTSIDE MARGINS
					{
						// determine direction and  move each window independently of movement of windows inside margins
						HandleWindowOutsideMargins(windowHandle);
					}


				} // foreach window
			} // while true

			BFS.ScriptSettings.WriteValue(moveFlagKey, false.ToString());
			MessageBox.Show("Stopping move");
		}

		public static MarginStatus getWindowBorderStatus(Rectangle monitorRect, Rectangle windowRect, string name)
		{
			// todo add shift argument = where window will be?
			int newLeft = windowRect.X;
			int newTop = windowRect.Y;
			int newRight = newLeft + windowRect.Width;
			int newBottom = newTop + windowRect.Height;

			int topMargin = monitorRect.Y;
			int bottomMargin = monitorRect.Y+monitorRect.Height;
			int leftMargin = monitorRect.X;
			int rightMargin = monitorRect.X+monitorRect.Width;

			var marginStatus = new MarginStatus
			{
				tooHigh = newTop < topMargin,
				tooLow = newBottom > bottomMargin,
				tooLeft = newLeft < leftMargin,
				tooRight = newRight > rightMargin
			};

			if(marginStatus.tooHigh)
			{
				// MessageBox.Show($"Window >>{name}<< [.L{newLeft} .R{newRight} .T{newTop} .B{newBottom}] too high in monitor [.L{leftMargin} .R{rightMargin} .T{topMargin} .B{bottomMargin}]");
			}
			bool inside = !marginStatus.tooHigh && !marginStatus.tooLow && !marginStatus.tooLeft & !marginStatus.tooRight;

			if(inside)
			{
				// MessageBox.Show($"Window [.L{newLeft} .R{newRight} .T{newTop} .B{newBottom}] INSIDE monitor borders [.L{leftMargin} .R{rightMargin} .T{topMargin} .B{bottomMargin}]");
			}
			else
			{
				// MessageBox.Show($"Window [.L{newLeft} .R{newRight} .T{newTop} .B{newBottom}] outside monitor borders [.L{leftMargin} .R{rightMargin} .T{topMargin} .B{bottomMargin}]");
			}

			return marginStatus;
		}

		public static bool wasWindowInsideMargins(IntPtr windowHandle)
		{
			Rectangle monitorRect = getCurrentWindowMonitorBounds(windowHandle);
			Rectangle windowRect = BFS.Window.GetBounds(windowHandle);

			int verLastMarginShift = getKeyValForMonitor(monitorRect, lastVerShiftKey);
			int horLastMarginShift = getKeyValForMonitor(monitorRect, lastHorShiftKey);

			int topMarginOld = getKeyValForMonitor(monitorRect, topMarginKey) - verLastMarginShift;
			int bottomMarginOld = getKeyValForMonitor(monitorRect, bottomMarginKey) - verLastMarginShift;
			int leftMarginOld = getKeyValForMonitor(monitorRect, leftMarginKey) - horLastMarginShift;
			int rightMarginOld = getKeyValForMonitor(monitorRect, rightMarginKey) - horLastMarginShift;

			int topMarginCurr = getKeyValForMonitor(monitorRect, topMarginKey); // todo remove
			int bottomMarginCurr = getKeyValForMonitor(monitorRect, bottomMarginKey);
			int leftMarginCurr = getKeyValForMonitor(monitorRect, leftMarginKey);
			int rightMarginCurrd = getKeyValForMonitor(monitorRect, rightMarginKey);

			int currLeft = windowRect.X;
			int currTop = windowRect.Y;
			int currRight = currLeft + windowRect.Width;
			int currBottom = currTop + windowRect.Height;

			bool tooHigh = currTop < topMarginOld;
			bool tooLow = currBottom > bottomMarginOld;
			bool tooLeft = currLeft < leftMarginOld;
			bool tooRight = currRight > rightMarginOld;
			
			bool windowIsInsideMargins = !tooHigh && !tooLow && !tooLeft & !tooRight; // outside margis when any flag true
			
			if(windowIsInsideMargins) // INSIDE margins
			{
				// MessageBox.Show($"Window [.L{currLeft} .R{currRight} .T{currTop} .B{currBottom}] was INSIDE margins[.L{leftMarginOld}->{leftMarginCurr} .R{rightMarginOld}->{rightMarginCurrd} .T{topMarginOld}->{topMarginCurr} .B{bottomMarginOld}->{bottomMarginCurr}]");
			}
			else // OUTSIDE margins
			{
				// MessageBox.Show($"Window [.L{currLeft} .R{currRight} .T{currTop} .B{currBottom}] was outside margins[.L{leftMarginOld}->{leftMarginCurr} .R{rightMarginOld}->{rightMarginCurrd} .T{topMarginOld}->{topMarginCurr} .B{bottomMarginOld}->{bottomMarginCurr}]");
			}

			return windowIsInsideMargins;
		}

		public static int decideVerDirection(Rectangle windowRect, Rectangle monitorRect, string directionKey, string name)
		{
			int resultDirection;
			MarginStatus borderInfo = getWindowBorderStatus(monitorRect, windowRect, name);

			if(windowRect.Height < monitorRect.Height) // window will fit in monitor
			{
				if(borderInfo.tooHigh && !borderInfo.tooLow) resultDirection = 1; // go down, back inside monitor
				else if (!borderInfo.tooHigh && borderInfo.tooLow) resultDirection = -1; // go up back, inside monitor
				else if (!borderInfo.tooHigh && !borderInfo.tooLow) // in monitor borders
				{
					if(keyAlreadyGenerated(directionKey))
					{
						resultDirection = readIntKey(directionKey);
						// MessageBox.Show($"readIntKey verDirection: {resultDirection}");

						if(resultDirection == 0)
						{
							resultDirection = 1; // move when inside monitor borders
						}
					}
					else 
					{
						resultDirection = GetRandomDirection();
					}
				}
				else // outside monitor up and down, move window down to show top
				{
					resultDirection = 1;
					MessageBox.Show("warning window both to high and too low"); // todo remove?
				}
			} 
			else // window will never fit inside monitor borders because too high
			{
				resultDirection = 0;
				MessageBox.Show("window will never fit inside monitor borders because too high"); // todo remove?
			}
			BFS.ScriptSettings.WriteValue(directionKey, resultDirection.ToString());
			return resultDirection;
		}

		public static int decideHorDirection(Rectangle windowRect, Rectangle monitorRect, string directionKey, string name)
		{
			int resultDirection;
			MarginStatus borderInfo = getWindowBorderStatus(monitorRect, windowRect, name);

			if (windowRect.Width < monitorRect.Width) // window will fit in monitor
			{
				if (borderInfo.tooRight && !borderInfo.tooLeft) resultDirection = -1; // go left, back inside monitor
				else if (!borderInfo.tooRight && borderInfo.tooLeft) resultDirection = 1; // go right, back inside monitor
				else if (!borderInfo.tooRight && !borderInfo.tooLeft) // in monitor borders
				{
					if (keyAlreadyGenerated(directionKey))
					{
						resultDirection = readIntKey(directionKey);
						// MessageBox.Show($"readIntKey horDirection: {resultDirection}");

						if(resultDirection == 0)
						{
							resultDirection = 1; // move when inside monitor borders
						}
					}
					else
					{
						resultDirection = GetRandomDirection();
					}
				}
				else // outside monitor left and right, move window to the right to show left side
				{
					resultDirection = 1;
					MessageBox.Show("warning window both too right and too left"); // todo remove?
				}
			}
			else // window will never fit inside monitor borders bevause too wide
			{
				resultDirection = 0;
				MessageBox.Show("window will never fit inside monitor borders because too wide"); // todo remove?
			}
			BFS.ScriptSettings.WriteValue(directionKey, resultDirection.ToString());
			return resultDirection;
		}

		public static void shiftMargins()
		{
			Rectangle[] allMonitors = GetFilteredMonitorRectList();
			foreach (Rectangle monitorRect in allMonitors)
			{
				int topMargin = getKeyValForMonitor(monitorRect, topMarginKey);
				int bottomMargin = getKeyValForMonitor(monitorRect, bottomMarginKey);
				int leftMargin = getKeyValForMonitor(monitorRect, leftMarginKey);
				int rightMargin = getKeyValForMonitor(monitorRect, rightMarginKey);
				
				Rectangle marginsRect = new Rectangle(
					leftMargin, // x
					topMargin, // y
					rightMargin - leftMargin, // width
					bottomMargin - topMargin // height
				);

				// MessageBox.Show($"shifting margins [.L{leftMargin} .R{rightMargin} .T{topMargin} .B{bottomMargin}] for monitor  [.L{monitorRect.X} .R{monitorRect.X+monitorRect.Width} .T{monitorRect.Y} .B{monitorRect.Y+monitorRect.Height}]");

				string  verMarginsDirectionForMonitorKey = GetKeyNameForMonitor(monitorRect, verMarginsDirectionKey);
				string  horMarginsDirectionForMonitorKey = GetKeyNameForMonitor(monitorRect, horMarginsDirectionKey);

				int verDirection = decideVerDirection(marginsRect, monitorRect, verMarginsDirectionForMonitorKey, "margin ver");
				int horDirection = decideHorDirection(marginsRect, monitorRect, horMarginsDirectionForMonitorKey, "margin hor");

				if(horDirection == 0 && verDirection == 0)
				{
					MessageBox.Show($"Error MARGINS no decision: horDirection {horDirection} verDirection {verDirection} "); // todo remove?
				}

				int verShift = verShiftDistance * verDirection;
				int horShift = horShiftDistance * horDirection;

				decide how far go until hit border exactly then move all margins and splits respectively
				refactor out shiftMarginForMonitor
				decideVerShiftDistance?
				// MessageBox.Show($"shift for margins: x->{horShift} y->{verShift}");
				
				setKeyValForMonitor(monitorRect, topMarginKey, topMargin + verShift);
				setKeyValForMonitor(monitorRect, bottomMarginKey, bottomMargin + verShift);
				setKeyValForMonitor(monitorRect, leftMarginKey, leftMargin + horShift);
				setKeyValForMonitor(monitorRect, rightMarginKey, rightMargin + horShift);
				setKeyValForMonitor(monitorRect, horizontalTopSplitKey, getKeyValForMonitor(monitorRect, horizontalTopSplitKey) + verShift);
				setKeyValForMonitor(monitorRect, horizontalMiddleSplitKey, getKeyValForMonitor(monitorRect, horizontalMiddleSplitKey) + verShift);
				setKeyValForMonitor(monitorRect, horizontalBottomSplitKey, getKeyValForMonitor(monitorRect, horizontalBottomSplitKey) + verShift);
				// MessageBox.Show($"Saving middle split key: {getKeyValForMonitor(monitorRect, verticalMiddleSplitKey)}->{getKeyValForMonitor(monitorRect, verticalMiddleSplitKey)+verShift}");
				setKeyValForMonitor(monitorRect, verticalMiddleSplitKey, getKeyValForMonitor(monitorRect, verticalMiddleSplitKey) + horShift);
				setKeyValForMonitor(monitorRect, verticalLeftSplitKey, getKeyValForMonitor(monitorRect, verticalLeftSplitKey) + horShift);
				setKeyValForMonitor(monitorRect, verticalRightSplitKey, getKeyValForMonitor(monitorRect, verticalRightSplitKey) + horShift);

				setKeyValForMonitor(monitorRect, lastVerShiftKey, verShift);
				setKeyValForMonitor(monitorRect, lastHorShiftKey, horShift);
			}
		}

		public static void HandleWindowInsideMargins(IntPtr windowHandle)
		{
			Rectangle monitorRect = getCurrentWindowMonitorBounds(windowHandle);

			// string  verMarginsDirectionForMonitorKey = GetKeyNameForMonitor(monitorRect,  );
			// string  horMarginsDirectionForMonitorKey = GetKeyNameForMonitor(monitorRect, );

			int verDirection = getKeyValForMonitor(monitorRect, verMarginsDirectionKey);
			int horDirection = getKeyValForMonitor(monitorRect, horMarginsDirectionKey);


			//  = readIntKey(verMarginsDirectionKey);
			//  = readIntKey(horMarginsDirectionKey);

			if(horDirection == 0 && verDirection == 0)
			{
				MessageBox.Show($"Error INSIDE MARGINS no direction horDirection {horDirection} verDirection {verDirection} "); // todo remove?
			}

			SetNewLocation(windowHandle, horDirection, verDirection); 
		}

		public static void HandleWindowOutsideMargins(IntPtr windowHandle)
		{
			// MessageBox.Show("HandleWindowOutsideMargins");
			
			string windowOutsideMarginsVerDirKey = windowHandle.ToString() + verDirPrefixForWindowOutsideMarginsKeyPostfix;
			string windowOutsideMarginsHorDirKey = windowHandle.ToString() + horDirPrefixForWindowOutsideMarginsKeyPostfix;
			
			Rectangle monitorRect = getCurrentWindowMonitorBounds(windowHandle);
			Rectangle windowRect = BFS.Window.GetBounds(windowHandle);

			string name = BFS.Window.GetText(windowHandle);
			int verDirection = decideVerDirection(windowRect, monitorRect, windowOutsideMarginsVerDirKey, name);
			int horDirection = decideHorDirection(windowRect, monitorRect, windowOutsideMarginsHorDirKey, name);

			if(horDirection == 0 && verDirection == 0)
			{
				MessageBox.Show($"Error OUTSIDE MARGINS no decision: horDirection {horDirection} verDirection {verDirection} "); // todo remove?
			}

			SetNewLocation(windowHandle, horDirection, verDirection); 

			// MessageBox.Show("HandleWindowOutsideMargins end");
		}

		public static void SingleWindow100(IntPtr windowHandle)
		{
			SingleWindowX(windowHandle, 1);
		}
		
		public static void SingleWindow95(IntPtr windowHandle)
		{
			SingleWindowX(windowHandle, 0.95);
		}
		
		public static void SingleWindow90(IntPtr windowHandle)
		{
			SingleWindowX(windowHandle, 0.90);
		}
			
		public static void SingleWindow80(IntPtr windowHandle)
		{
			SingleWindowX(windowHandle, 0.80);
		}
		
		public static void SingleWindow60(IntPtr windowHandle)
		{
			SingleWindowX(windowHandle, 0.60);
		}

		
		public static void SingleWindowX(IntPtr windowHandle, double factor)
		{
			// MessageBox.Show("To jest powiadomienie SingleWindow90.", "Tytuł powiadomienia", MessageBoxButtons.OK);

			// check to see if there was an error, if there was, exit function
			if (windowHandle == IntPtr.Zero)
				return;

			// get the position of the window in the monitor, and the current monitor
			Rectangle windowRect = BFS.Window.GetBounds(windowHandle);
			Rectangle monitorRect = getMouseMonitorBounds();
			
			// calculate windows size variance
			int randomShiftW = enableSizeVariance ? GetRandomShift(shiftRange) : 0;
			int randomShiftH = enableSizeVariance ? GetRandomShift(shiftRange) : 0;
			
			// calculate final window size
			int iFinalWinW = Convert.ToInt32(monitorRect.Width * factor) + randomShiftW;
			int iFinalWinH = Convert.ToInt32(monitorRect.Height * factor) + randomShiftH;
			
			// calculate window position variance
			int randomShiftX = 0, randomShiftY = 0;
			int iFinalWinX = monitorRect.X + (monitorRect.Width - iFinalWinW) / 2;
			int iFinalWinY = monitorRect.Y + (monitorRect.Height - iFinalWinH) / 2;
			
			if (enablePositionVariance)
			{
				if(enableWholeSpaceShift) // randomly move windows within whole unused space
				{
					// calculate how much free space there is
					int freeSpaceW = monitorRect.Width - iFinalWinW;
					int freeSpaceH = monitorRect.Height - iFinalWinH;
					
					// override final position as generated random shift within free space
					randomShiftX = GetRandomShift(0, freeSpaceW);
					randomShiftY = GetRandomShift(0, freeSpaceH);
					iFinalWinX = monitorRect.X + randomShiftX;
					iFinalWinY = monitorRect.Y + randomShiftY;
				}
				else // generate const value random shift 
				{
					randomShiftX = GetRandomShift(shiftRange);
					randomShiftY = GetRandomShift(shiftRange);
					
					// calculate final position by adding generated shift to centered position
					iFinalWinX = iFinalWinX + randomShiftX;
					iFinalWinY = iFinalWinY + randomShiftY;
				}

			}

			// when enabled make sure window borders are within monitor
			if(enableOutOfBoundChecks)
			{
				if ( iFinalWinW > monitorRect.Width) iFinalWinW = monitorRect.Width; // check window too wide
				if ( iFinalWinH > monitorRect.Height) iFinalWinH = monitorRect.Height; // check window too high
				
				if (iFinalWinX < monitorRect.X) iFinalWinX = monitorRect.X; // check window out of bound left
				if (iFinalWinY < monitorRect.Y) iFinalWinY = monitorRect.Y; // check window out of bound top

				if (( iFinalWinX + iFinalWinW - monitorRect.X) > monitorRect.Width ) iFinalWinX = monitorRect.X + monitorRect.Width - iFinalWinW; // check window out of bound right
				if (( iFinalWinY + iFinalWinH - monitorRect.Y) > monitorRect.Height ) iFinalWinY = monitorRect.Y + monitorRect.Height - iFinalWinH; // check window out of bound bottom
			}
			
			// move and resize window
			BFS.Window.SetSizeAndLocation(windowHandle, iFinalWinX, iFinalWinY, iFinalWinW, iFinalWinH );
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
			
			// display values for debug
			// MessageBox.Show("iFinalWinX " +iFinalWinX+ "\tiFinalWinY " + iFinalWinY+
							// "\niFinalWinW " +iFinalWinW+ "\tiFinalWinH " +iFinalWinH+ 
							// "\nrandomShiftW " +randomShiftW+ "\trandomShiftH " + randomShiftH+
							// "\nrandomShiftX " +randomShiftX+ "\trandomShiftY " +randomShiftY +
							// "\nmonitorRect.X " + monitorRect.X + "\tmonitorRect.Y " + monitorRect.Y);
		}

		public static int GetRandomShift(int range)
		{
			// return 0;
			Random random = new Random();
			return random.Next(-range, range + 1);
		}
		
		public static int GetRandomShift(int start, int end)
		{
			// return 0;
			Random random = new Random();
			return random.Next(start, end);
		}

		public static int GetRandomDirection()
		{
			// return -1 or 1
			Random random = new Random();
			return random.Next(2) * 2 - 1;
		}

		// topMarginKey, bottomMarginKey, leftMarginKey, rightMarginKey - outer borders
		// horizontalTopSplitKey - upper horizontal split for e.g bottom border on topmost window in 3 window horizontal split
		// horizontalMiddleSplitKey - horizontal split in the middle for e.g one top window and one bottom window
		// horizontalBottomSplitKey - lower horizontal split for e.g top border on bottommost window in 3 window horizontal split
		// verticalMiddleSplitKey - right in the middle of screen for Left / Right split or 4 corners split
		// verticalLeftSplitKey - left border when doing 1-1-1, 1-2 splits 
		// verticalRightSplitKey - right border when doing 1-1-1 and 2-1 splits
		public static void generateSplitBordersAllMonitors()
		{
			Rectangle[] allMonitors = GetAllMonitorRectList();
			foreach (Rectangle monitorRect in allMonitors)
			{
				setKeyValForMonitorWhenEmpty(monitorRect, topMarginKey,  monitorRect.Y + GetRandomShift(0, shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, bottomMarginKey, monitorRect.Y + monitorRect.Height - GetRandomShift(0, shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, leftMarginKey, monitorRect.X + GetRandomShift(0, shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, rightMarginKey, monitorRect.X + monitorRect.Width - GetRandomShift(0, shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, horizontalTopSplitKey, monitorRect.Y + monitorRect.Height / 3 + GetRandomShift(shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, horizontalMiddleSplitKey, monitorRect.Y + monitorRect.Height / 2 + GetRandomShift(shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, horizontalBottomSplitKey, monitorRect.Y + monitorRect.Height * 2 / 3 + GetRandomShift(shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, verticalLeftSplitKey, monitorRect.X + monitorRect.Width / 3 + GetRandomShift(shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, verticalMiddleSplitKey, monitorRect.X + monitorRect.Width / 2 + GetRandomShift(shiftRange));
				setKeyValForMonitorWhenEmpty(monitorRect, verticalRightSplitKey, monitorRect.X + monitorRect.Width * 2 / 3 + GetRandomShift(shiftRange));

				setKeyValForMonitorWhenEmpty(monitorRect, verMarginsDirectionKey,GetRandomDirection());
				setKeyValForMonitorWhenEmpty(monitorRect, horMarginsDirectionKey,GetRandomDirection());
				// uint monitorID = BFS.Monitor.GetMonitorIDByRect(monitorRectAsID);
				//MessageBox.Show($"generateSplitBorder monitor ({monitorID}) X.{monitorRect.X} Y.{monitorRect.Y} [{monitorRect.Width}/{monitorRect.Height}] vemidsplit: {readIntKey(rightMarginKey+"_2")}");
			}
		}

		public static void setKeyValForMonitorWhenEmpty(Rectangle monitorRectAsID, string key, int newValue)
		{
			string fullMonitorKey = GetKeyNameForMonitor(monitorRectAsID, key);

			if (keyAlreadyGenerated(fullMonitorKey))
			{
				// ignore received new value and override with stored value
				newValue = readIntKey(fullMonitorKey);
			}
			else
			{
				// save in settings
				BFS.ScriptSettings.WriteValue(fullMonitorKey, newValue.ToString());
			}
		}
		public static void setKeyValForMonitor(Rectangle monitorRectAsID, string key, int newValue)
		{
			string fullMonitorKey = GetKeyNameForMonitor(monitorRectAsID, key);

			// save in settings
			BFS.ScriptSettings.WriteValue(fullMonitorKey, newValue.ToString());
		}

		public static int getKeyValForMonitor(Rectangle monitorRectAsID, string key)
		{
			string fullMonitorKey = GetKeyNameForMonitor(monitorRectAsID, key);
			
			int result;
			if (keyAlreadyGenerated(fullMonitorKey))
			{
				result = readIntKey(fullMonitorKey);
			}
			else
			{
				result = 0;
				MessageBox.Show($"ERROR no storred setting for monitor key {fullMonitorKey}");
			}
			return result;
		}

		public static void TmpMargins(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);

			int width = rightMargin - leftMargin;
			int height = bottomMargin - topMargin;

			// MessageBox.Show($"Moving windows to X.{leftMargin} Y.{topMargin} [{width}/{height}]");
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);

			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}

		public static void Left(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int verticalMiddleSplit = getKeyValForMonitor(monitorRectAsID, verticalMiddleSplitKey);
			// MessageBox.Show($"Got middle split: {verticalMiddleSplit}");
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);

			if (BFS.Input.IsKeyDown(KEY_SHIFT))
			{
				TopLeft(windowHandle);
			}
			else if (BFS.Input.IsKeyDown(KEY_CTRL))
			{
				BottomLeft(windowHandle);
			}
			else // whole space horizontally when no key pressed
			{
				int width = verticalMiddleSplit - leftMargin;
				int height = bottomMargin - topMargin;

				// MessageBox.Show($"Moving windows to X.{leftMargin} Y.{topMargin} [{width}/{height}]");
				BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
			}
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		
		public static void Right(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int verticalMiddleSplit = getKeyValForMonitor(monitorRectAsID, verticalMiddleSplitKey);
			// MessageBox.Show($"Got middle split: {verticalMiddleSplit}");
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);

			uint monitorID = BFS.Monitor.GetMonitorIDByRect(monitorRectAsID);
			// MessageBox.Show($"Right monitorRectAsID {monitorID}  verticalMiddleSplit {verticalMiddleSplit}");

			if (BFS.Input.IsKeyDown(KEY_SHIFT))
			{
				TopRight(windowHandle);
			}
			else if (BFS.Input.IsKeyDown(KEY_CTRL))
			{
				BottomRight(windowHandle);
			}
			else // whole space horizontally when no key pressed
			{
				int width = rightMargin - verticalMiddleSplit;
				int height = bottomMargin - topMargin;
				// MessageBox.Show($"Moving windows to X.{verticalMiddleSplit} Y.{topMargin} [{width}/{height}]");
				BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, topMargin, width, height);
			}
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}

		// ************* 4 split *************
				
		public static void TopLeft(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int verticalMiddleSplit = getKeyValForMonitor(monitorRectAsID, verticalMiddleSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = verticalMiddleSplit - leftMargin;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
						
		public static void TopRight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int verticalMiddleSplit = getKeyValForMonitor(monitorRectAsID, verticalMiddleSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = rightMargin - verticalMiddleSplit;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
					
		public static void BottomLeft(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int verticalMiddleSplit = getKeyValForMonitor(monitorRectAsID, verticalMiddleSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = verticalMiddleSplit - leftMargin;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
						
		public static void BottomRight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int verticalMiddleSplit = getKeyValForMonitor(monitorRectAsID, verticalMiddleSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = rightMargin - verticalMiddleSplit;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		
		// ************* 6 split *************

		public static void LeftmostTop(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = verticalLeftSplit - leftMargin;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		
		public static void LeftmostBottom(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);
			
			int width = verticalLeftSplit - leftMargin;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
					
		public static void LeftmostCenter(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = verticalLeftSplit - leftMargin;
			int height = (bottomMargin - topMargin) / 2;
			int y = horizontalMiddleSplit - height / 2;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, y, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		public static void LeftmostFullHeight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);

			int width = verticalLeftSplit - leftMargin;
			int height = bottomMargin - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		
		public static void MiddleTop(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = verticalRightSplit - verticalLeftSplit;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
			
		public static void MiddleBottom(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = verticalRightSplit - verticalLeftSplit;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
			
		public static void MiddleCenter(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = verticalRightSplit - verticalLeftSplit;
			int height = (bottomMargin - topMargin) / 2;
			int y = horizontalMiddleSplit - height / 2;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, y, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	

		public static void MiddleFullHeight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);

			int width = verticalRightSplit - verticalLeftSplit;
			int height = bottomMargin - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
			
		public static void RightmostTop(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = rightMargin - verticalRightSplit;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
						
		public static void RightmostBottom(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);
			
			int width = rightMargin - verticalRightSplit;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}

		public static void RightmostCenter(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int horizontalMiddleSplit = getKeyValForMonitor(monitorRectAsID, horizontalMiddleSplitKey);

			int width = rightMargin - verticalRightSplit;
			int height = (bottomMargin - topMargin) / 2;
			int y = horizontalMiddleSplit - height / 2;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, y, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
			
		public static void RightmostFullHeight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);

			int width = rightMargin - verticalRightSplit;
			int height = bottomMargin - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	

		

		
		public static void SixSplitLeft(IntPtr windowHandle)
		{
			if(BFS.Input.IsKeyDown(KEY_SHIFT) && !BFS.Input.IsKeyDown(KEY_CTRL)) // top only
			{
				LeftmostTop(windowHandle);
			}
			else if(!BFS.Input.IsKeyDown(KEY_SHIFT) && BFS.Input.IsKeyDown(KEY_CTRL)) // bottom only
			{
				LeftmostBottom(windowHandle);
			}
			else if (BFS.Input.IsKeyDown(KEY_SHIFT) && BFS.Input.IsKeyDown(KEY_CTRL)) // both up and down - certer
			{
				LeftmostCenter(windowHandle);
			}
			else // no key modifier - full height
			{
				LeftmostFullHeight(windowHandle);
			}
		}
		
		public static void SixSplitMiddle(IntPtr windowHandle)
		{
			if(BFS.Input.IsKeyDown(KEY_SHIFT) && !BFS.Input.IsKeyDown(KEY_CTRL)) // top only
			{
				MiddleTop(windowHandle);
			}
			else if(!BFS.Input.IsKeyDown(KEY_SHIFT) && BFS.Input.IsKeyDown(KEY_CTRL)) // bottom only
			{
				MiddleBottom(windowHandle);
			}
			else if (BFS.Input.IsKeyDown(KEY_SHIFT) && BFS.Input.IsKeyDown(KEY_CTRL)) // both up and down - certer
			{
				MiddleCenter(windowHandle);
			}
			else // no key modifier - full height
			{
				MiddleFullHeight(windowHandle);
			}
		}

		public static void SixSplitRight(IntPtr windowHandle)
		{
			if(BFS.Input.IsKeyDown(KEY_SHIFT) && !BFS.Input.IsKeyDown(KEY_CTRL)) // top only
			{
				RightmostTop(windowHandle);
			}
			else if(!BFS.Input.IsKeyDown(KEY_SHIFT) && BFS.Input.IsKeyDown(KEY_CTRL)) // bottom only
			{
				RightmostBottom(windowHandle);
			}
			else if (BFS.Input.IsKeyDown(KEY_SHIFT) && BFS.Input.IsKeyDown(KEY_CTRL)) // both up and down - certer
			{
				RightmostCenter(windowHandle);
			}
			else // no key modifier - full height
			{
				RightmostFullHeight(windowHandle);
			}
		}

		public static void NineSplitLeft(IntPtr windowHandle)
		{
			NineSplit(windowHandle, WindowHorizontalPosition.Left);
		}
		public static void NineSplitMiddle(IntPtr windowHandle)
		{
			NineSplit(windowHandle, WindowHorizontalPosition.Middle);
		}
		public static void NineSplitRight(IntPtr windowHandle)
		{
			NineSplit(windowHandle, WindowHorizontalPosition.Right);
		}
		
		public static void NineSplit(IntPtr windowHandle, WindowHorizontalPosition position)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = getKeyValForMonitor(monitorRectAsID, topMarginKey);
			int bottomMargin = getKeyValForMonitor(monitorRectAsID, bottomMarginKey);
			int leftMargin = getKeyValForMonitor(monitorRectAsID, leftMarginKey);
			int rightMargin = getKeyValForMonitor(monitorRectAsID, rightMarginKey);
			int verticalLeftSplit = getKeyValForMonitor(monitorRectAsID, verticalLeftSplitKey);
			int verticalMiddleSplit = getKeyValForMonitor(monitorRectAsID, verticalMiddleSplitKey);
			int verticalRightSplit = getKeyValForMonitor(monitorRectAsID, verticalRightSplitKey);
			int horizontalTopSplit = getKeyValForMonitor(monitorRectAsID, horizontalTopSplitKey);
			int horizontalBottomSplit = getKeyValForMonitor(monitorRectAsID, horizontalBottomSplitKey);

			int height = 0, width = 0;
			int x = 0, y = 0;

			switch (position)
			{
				case WindowHorizontalPosition.Left:
					width = verticalLeftSplit - leftMargin;
					x = leftMargin;
					break;
				case WindowHorizontalPosition.Middle:
					width = verticalRightSplit - verticalLeftSplit;
					x = verticalLeftSplit;
					break;
				case WindowHorizontalPosition.Right:
					width = rightMargin - verticalRightSplit;
					x = verticalRightSplit;
					break;
				default:
					MessageBox.Show("No switch for position " + position.ToString() + " in function NineSplit()");
					break;
			}

			if(BFS.Input.IsKeyDown(KEY_SHIFT) && !BFS.Input.IsKeyDown(KEY_CTRL)) // top only
			{
				height = horizontalTopSplit - topMargin;
				y = topMargin;
			}
			else if(!BFS.Input.IsKeyDown(KEY_SHIFT) && BFS.Input.IsKeyDown(KEY_CTRL)) // bottom only
			{
				height = bottomMargin - horizontalBottomSplit;
				y = horizontalBottomSplit;
			}
			else if (BFS.Input.IsKeyDown(KEY_SHIFT) && BFS.Input.IsKeyDown(KEY_CTRL)) // both up and down - certer
			{
				height = horizontalBottomSplit - horizontalTopSplit;
				y = horizontalTopSplit;
			}
			else // no key modifier - full height
			{
				height = bottomMargin - topMargin;
				y = topMargin;
			}

			BFS.Window.SetSizeAndLocation(windowHandle, x, y, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		
		public static bool keyAlreadyGenerated(string key)
		{
			return !string.IsNullOrEmpty(BFS.ScriptSettings.ReadValue(key));
		}
		
		public static int readIntKey(string key)
		{
			return int.Parse(BFS.ScriptSettings.ReadValue(key));
		}

		public static bool readBoolKey(string key)
		{
			bool result = false;
			bool.TryParse(BFS.ScriptSettings.ReadValue(key), out result);
			return result;
		}

		public static Rectangle getMouseMonitorBounds()
		{
			return BFS.Monitor.GetMonitorBoundsByMouseCursor();
		}
		
		public static uint getMouseMonitorID()
		{
			// Get the mouse position
			Point mousePosition = new Point(BFS.Input.GetMousePositionX(), BFS.Input.GetMousePositionY());
			
			// Get an array of the bounds for all monitors ignoring splits
			Rectangle[] monitorBoundsAll = GetAllMonitorRectList();
			
			// Loop through the array of bounds and find monitor in which mouse in located
			foreach (Rectangle monitorBounds in monitorBoundsAll)
			{
				if (monitorBounds.Contains(mousePosition))
				{
					uint id = BFS.Monitor.GetMonitorIDByRect(monitorBounds);
					// MessageBox.Show($"Found monitor for mouse - ID:{id} [X: {monitorBounds.X}, Y: {monitorBounds.Y}, Width: {monitorBounds.Width}, Height: {monitorBounds.Height}]");

					return id;
				}
			}
			return default; 
		}
		
		public static Rectangle getMonitorByPoint(Point point,  Rectangle[] monitorBoundsAll)
		{
			// Loop through the array of bounds and find monitor of current window
			foreach (Rectangle monitorBounds in monitorBoundsAll)
			{
				if (monitorBounds.Contains(point))
				{
					//MessageBox.Show($"Found monitor top left [X: {monitorBounds.X}, Y: {monitorBounds.Y}, Width: {monitorBounds.Width}, Height: {monitorBounds.Height}]");
					return monitorBounds;
				}
			}
			return default;
		}

		public static Rectangle getCurrentWindowMonitorBounds(IntPtr windowHandle)
		{
			Rectangle windowRect = BFS.Window.GetBounds(windowHandle);

			// Get an array of the bounds for all monitors ignoring splits
			Rectangle[] monitorBoundsAll = GetAllMonitorRectList();
			
			// find monitor for CENTER point of window
			Point windowPosition = new Point(windowRect.X + windowRect.Width / 2, windowRect.Y + windowRect.Height / 2);
			Rectangle monitor = getMonitorByPoint(windowPosition, monitorBoundsAll);
			string windowID = BFS.Application.GetAppIDByWindow(windowHandle).ToString() + " " + BFS.Window.GetText(windowHandle);
			if (monitor != Rectangle.Empty) 
				return monitor;
			MessageBox.Show($"Did not find monitor for window CENTER {windowID} [X {windowPosition.X}, Y {windowPosition.Y}, Width {windowRect.Width}, Height {windowRect.Height}]");

			// Find monitor containing windows TOP LEFT corner
			windowPosition = new Point(windowRect.X, windowRect.Y);
			monitor = getMonitorByPoint(windowPosition, monitorBoundsAll);
			if (monitor != Rectangle.Empty) 
				return monitor;
			MessageBox.Show($"Did not find monitor for window TOP LEFT {windowID} [X {windowPosition.X}, Y {windowPosition.Y}, Width {windowRect.Width}, Height {windowRect.Height}]");

			// find monitor for TOP RIGHT window conrner
			windowPosition= new Point(windowRect.X + windowRect.Width, windowRect.Y);
			monitor = getMonitorByPoint(windowPosition, monitorBoundsAll);
			if (monitor != Rectangle.Empty) 
				return monitor;
			MessageBox.Show($"Did not find monitor for window TOP RIGHT {windowID} [X {windowPosition.X}, Y {windowPosition.Y}, Width {windowRect.Width}, Height {windowRect.Height}]");

			// find monitor for BOTTOM LEFT window conrner
			windowPosition= new Point(windowRect.X, windowRect.Y + windowRect.Height);
			monitor = getMonitorByPoint(windowPosition, monitorBoundsAll);
			if (monitor != Rectangle.Empty) 
				return monitor;
			MessageBox.Show($"Did not find monitor for window BOTTOM LEFT {windowID} [X {windowPosition.X}, Y {windowPosition.Y}, Width {windowRect.Width}, Height {windowRect.Height}]");

			// find monitor for BOTTOM RIGHT window conrner
			windowPosition= new Point(windowRect.X + windowRect.Width, windowRect.Y + windowRect.Height);
			monitor = getMonitorByPoint(windowPosition, monitorBoundsAll);
			if (monitor != Rectangle.Empty) 
				return monitor;
			MessageBox.Show($"Did not find monitor for window BOTTOM RIGHT {windowID} [X {windowPosition.X}, Y {windowPosition.Y}, Width {windowRect.Width}, Height {windowRect.Height}]");

			MessageBox.Show($"Did not find ANY monitor for window {windowID} [X {windowRect.X}, Y {windowRect.Y}, Width {windowRect.Width}, Height {windowRect.Height}]");
			return default; 
		}

		public static void SetNewLocation(IntPtr windowHandle, int horDirection, int verDirection)
		{
			// todo check if out of bounds and maybe set it to be exactly on border?
			Rectangle windowRect = BFS.Window.GetBounds(windowHandle);

			int horShift = horShiftDistance * horDirection;
			int verShift = verShiftDistance * verDirection;

			string verLastShiftForWindowKey = windowHandle.ToString() + lastVerShiftKey;
			string horLastShiftForWindowKey = windowHandle.ToString() + lastHorShiftKey;

			BFS.ScriptSettings.WriteValue(verLastShiftForWindowKey, verShift.ToString());
			BFS.ScriptSettings.WriteValue(horLastShiftForWindowKey, horShift.ToString());

			// MessageBox.Show($"Moving windos X->{horShift} Y->{verShift}");
			BFS.Window.SetLocation(windowHandle, windowRect.X + horShift, windowRect.Y + verShift); 
			// MessageBox.Show("Moved window " + BFS.Application.GetAppIDByWindow(windowHandle).ToString());
		}



		public static string MergeKeyCodes(params string[] keyCodes)
		{
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < keyCodes.Length; i++)
			{
				sb.Append(keyCodes[i]);

				if (i < keyCodes.Length - 1)
				{
					sb.Append(";");
				}
			}

			return sb.ToString();
		}

		public static string RectAsKey(Rectangle rec)
		{
			return $"RectangleX{rec.X}Y{rec.Y}Width{rec.Width}Height{rec.Height}";
		}
		public static Rectangle[] GetAllMonitorRectList()
		{
			return BFS.Monitor.GetMonitorBoundsNoSplits();
		}

		public static Rectangle[] GetFilteredMonitorRectList()
		{
			Rectangle[] allMonitors = BFS.Monitor.GetMonitorBoundsNoSplits();

			Rectangle[] filteredMonitors = allMonitors.Where(rectangle => rectangle.Width == FILTER_MONITOR_WIDTH && 
			                                                             rectangle.Height == FILTER_MONITOR_HEIGHT).ToArray();

			return filteredMonitors;
		}

		public static IntPtr[] GetAllWindowHandles()
		{
			return BFS.Window.GetVisibleAndMinimizedWindowHandles();
		}

		public static IntPtr[] GetFilteredWindowHandles()
		{
			IntPtr[] allWindows = BFS.Window.GetVisibleAndMinimizedWindowHandles();


			// todo add ignore windows with size = monitor size because fullscreen are not considered maximized?

			IntPtr[] filteredWindows = allWindows.Where(windowHandle => {


				if (BFS.Window.IsMinimized(windowHandle) || // dont move minimized windows because it wil un-minimize them // todo save when it would be moved and use when unminimized
					BFS.Window.IsMaximized(windowHandle))  // ignore maximized windows
				{
					return false;
				}

				string name = BFS.Window.GetText(windowHandle);
				if (string.IsNullOrEmpty(name) || name == "Program Manager") // ignore fake windows
				{
					return false;
				}

				Rectangle currentWindowMonitorBounds = getCurrentWindowMonitorBounds(windowHandle);

				return currentWindowMonitorBounds.Width == FILTER_MONITOR_WIDTH && 
				       currentWindowMonitorBounds.Height == FILTER_MONITOR_HEIGHT;
			}).ToArray();

			return filteredWindows;
		}

		public static string GetKeyNameForMonitor(Rectangle monitorRect, string key)
		{
			return key + "_" + RectAsKey(monitorRect);
		}
	}