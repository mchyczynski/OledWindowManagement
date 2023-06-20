	using System;
	using System.Text;
	using System.Reflection;
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

		// public static int topMargin = 0;
		// public static int bottomMargin = 0;
		// public static int leftMargin = 0;
		// public static int rightMargin = 0;
		// public static int horizontalTopSplit = 0;
		// public static int horizontalMiddleSplit = 0;
		// public static int horizontalBottomSplit = 0;
		// public static int verticalLeftSplit = 0;
		// public static int verticalMiddleSplit = 0;
		// public static int verticalRightSplit = 0;
		
		public static DateTime lastBordersRecalc = DateTime.MinValue;
		public static uint moveWindowTimerDelay = 1000; 

		public static string KEY_SHIFT = "16";
		public static string KEY_CTRL = "17";
		public static string KEY_A = "65";
		public static string KEY_Q = "81";

		// to store marings and splits for each monitor
		// e.g. bordersDict[monitorRectAsID][horizontalTopSplitKey] = 10;
		public static Dictionary<Rectangle, Dictionary<string, int>> bordersDict = new Dictionary<Rectangle, Dictionary<string, int>>();
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
					
					// {{ "DodgerBlue", "Black", "LeftmostTop" }},
					
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

				foreach(IntPtr windowHandle in BFS.Window.GetVisibleAndMinimizedWindowHandles())
				{
					string name = BFS.Window.GetText(windowHandle);

					if (BFS.Window.IsMinimized(windowHandle) || // dont move minimized windows because it wil un-minimize them
						BFS.Window.IsMaximized(windowHandle) ||  // ignore maximized windows
						(String.IsNullOrEmpty(name) || name == "Program Manager") ) // ignore fake windows
					{
						continue;
					}

					MessageBox.Show("Processing window" + BFS.Application.GetAppIDByWindow(windowHandle).ToString() + " " + BFS.Window.GetText(windowHandle));
					Rectangle monitorRect = getCurrentWindowMonitorBounds(windowHandle);
					Rectangle windowRect = BFS.Window.GetBounds(windowHandle);

					if(monitorRect.Width == 0 || monitorRect.Height == 0)
					{
						MessageBox.Show("STOP move no monitor"); // todo just ignore this window?
						enableWindowsPositionTimedShift = false;
						break;
					}

					int horDirection = -1, verDirection = -1;
					int finalHorShift = 0, finalVerShift = 0;
					// string windowHandleKey = Marshal.PtrToStringUni(windowHandle);

					// int dir;
					// if(!windowDirectionsDict.TryGetValue(windowHandleKey, out dir))
					// {
					// 	if(keyAlreadyGenerated(windowHandleKey))
					// 	{
					// 		dir = readIntKey(windowHandleKey);
					// 	}
					// 	else
					// 	{
					// 		dir = 1;
					// 	}
					// 	windowDirectionsDict[windowHandleKey] = dir;
					// }

					// TODO move margins and splits
					//  find what direction
					// one pass through all windows and decide direction for next move? (so that 2 loops not needed)

			 		MarginStatus marginInfo = getWindowMarginsStatus(monitorRect, windowRect);
					if (isWindowInsideMargins(marginInfo))
					{
						continue; // ignore for now
						// check if new position will be outside of current monitor and switch shift direction

						// update shift based on direction from prev iteration
						finalHorShift = horShiftDistance * horDirection; // todo
						finalVerShift = verShiftDistance * verDirection;


						// would hit monitor LEFT?
						if (windowRect.X+finalHorShift <= monitorRect.X) 
						{
							MessageBox.Show($"windowRect.X {windowRect.X} +finalHorShift {finalHorShift} ({windowRect.X+finalHorShift}) <= monitorRect.X {monitorRect.X}");
							horDirection = 1;
						}

						// would hit monitor RIGHT?
						if (windowRect.X+finalHorShift + windowRect.Width >= monitorRect.X + monitorRect.Width)
						{
							MessageBox.Show($@"windowRect.X {windowRect.X} +finalHorShift {finalHorShift} +windowRect.Width {windowRect.Width} ({windowRect.X+finalHorShift+windowRect.Width}) 
											>=
											monitorRect.X {monitorRect.X} + monitorRect.Width {monitorRect.Width} ({monitorRect.X+monitorRect.Width})");
							horDirection = -1;
						}
						
						// would hit monitor TOP?
						if (windowRect.Y+finalVerShift <= monitorRect.Y)
						{				
							MessageBox.Show($"windowRect.Y {windowRect.Y} +finalVerShift {finalVerShift} ({windowRect.Y+finalVerShift}) <= monitorRect.Y {monitorRect.Y}");
							verDirection = 1;
						}
						
						// would hit monitor BOTTOM?
						if(windowRect.Y+finalVerShift + windowRect.Height >= monitorRect.Y + monitorRect.Height)
						{
							MessageBox.Show($@"windowRect.Y {windowRect.Y} +finalVerShift {finalVerShift} +windowRect.Height {windowRect.Height} ({windowRect.Y+finalVerShift+windowRect.Height}) 
											>= 
											monitorRect.Y {monitorRect.Y} + monitorRect.Height {monitorRect.Height} ({monitorRect.Y+monitorRect.Height})");
							verDirection = -1;
						}

						// calculate final shift and move window // moved on begginign so that direction is changed for next move
						// finalHorShift = horShiftDistance * horDirection;
						// finalVerShift = verShiftDistance * verDirection;
						BFS.Window.SetLocation(windowHandle, windowRect.X+finalHorShift, windowRect.Y+finalVerShift);
						// MessageBox.Show("Moved window " + BFS.Application.GetAppIDByWindow(windowHandle).ToString());
						// move with other windows inside
						// move margins and splits ()
					}
					else // OUTSIDE MARGINS
					{
						HandleWindowOutsideMargins(marginInfo, windowHandle, monitorRect, windowRect, out horDirection, out verDirection);
						
						// move independently each window based on which margins we want to meet
						// horDirection and  verDirection calculated in isWindowInsideOuterMargins based on which margins we are outside
						if(horDirection == 0 && verDirection == 0)
						{
							MessageBox.Show($"Error OUTSIDE MARGINS no decision: horDirection {horDirection} verDirection {verDirection} ");
						}
						finalHorShift = horShiftDistance * horDirection;
						finalVerShift = verShiftDistance * verDirection;
						MessageBox.Show(" before set location");
						BFS.Window.SetLocation(windowHandle, windowRect.X+finalHorShift, windowRect.Y+finalVerShift);
					}


				} // foreach window
			} // while true

			BFS.ScriptSettings.WriteValue(moveFlagKey, false.ToString());
			MessageBox.Show("Stopping move");
		}

		public static MarginStatus getWindowMarginsStatus(Rectangle monitorRect, Rectangle windowRect)
		{
			int newLeft = windowRect.X;
			int newTop = windowRect.Y;
			int newRight = newLeft + windowRect.Width;
			int newBottom = newTop + windowRect.Height;

			int topMargin = bordersDict[monitorRect][topMarginKey];
			int bottomMargin = bordersDict[monitorRect][bottomMarginKey];
			int leftMargin = bordersDict[monitorRect][leftMarginKey];
			int rightMargin = bordersDict[monitorRect][rightMarginKey];

			var marginStatus = new MarginStatus
			{
				tooHigh = newTop < topMargin,
				tooLow = newBottom > bottomMargin,
				tooLeft = newLeft < leftMargin,
				tooRight = newRight > rightMargin
			};
			if(isWindowInsideMargins(marginStatus)) // INSIDE margins
			{
				MessageBox.Show($"Window [.X{newLeft} .Y{newTop} ({newRight}/{newBottom})] INSIDE margins [.T{topMargin} .B{bottomMargin} .L{leftMargin} .R{rightMargin}]");
			}
			else // OUTSIDE margins
			{
				MessageBox.Show($"Window [.X{newLeft} .Y{newTop} ({newRight}/{newBottom})] outside margins [.T{topMargin} .B{bottomMargin} .L{leftMargin} .R{rightMargin}]");
			}
			return marginStatus;
		}

		public static MarginStatus getWindowBorderStatus(Rectangle monitorRect, Rectangle windowRect)
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
			if(isWindowInsideMargins(marginStatus)) // INSIDE margins
			{
				MessageBox.Show($"Window [.X{newLeft} .Y{newTop} ({newRight}/{newBottom})] INSIDE monitor borders [.T{topMargin} .B{bottomMargin} .L{leftMargin} .R{rightMargin}]");
			}
			else // OUTSIDE margins
			{
				MessageBox.Show($"Window [.X{newLeft} .Y{newTop} ({newRight}/{newBottom})] outside monitor borders [.T{topMargin} .B{bottomMargin} .L{leftMargin} .R{rightMargin}]");
			}
			return marginStatus;
		}

		public static bool isWindowInsideMargins(MarginStatus marginInfo)
		{
			bool windowIsInsideMargins = !(marginInfo.tooHigh || marginInfo.tooLow || marginInfo.tooLeft || marginInfo.tooRight); // outside margis when any flag true
			return windowIsInsideMargins;
		}

		public static void decideVerDirectionForOutsideMargins(MarginStatus marginInfo, IntPtr windowHandle, Rectangle monitorRect, Rectangle  windowRect, out int verDirection)
		{
			MessageBox.Show("decideVerDirectionForOutsideMarigns");
			string windowHandleKey = windowHandle.ToString() + "_outmargins_verdir";  // todo
			MarginStatus borderInfo = getWindowBorderStatus(monitorRect, windowRect);

			if (marginInfo.tooLow && marginInfo.tooHigh) // outside both margins
			{
				if(borderInfo.tooHigh && !borderInfo.tooLow) verDirection = 1; // go down, back inside monitor
				else if (!borderInfo.tooHigh && borderInfo.tooLow) verDirection = -1; // go up back, inside monitor
				else if (!borderInfo.tooHigh && !borderInfo.tooLow) // outside both margins but in monitor borders
				{
					if(keyAlreadyGenerated(windowHandleKey))
					{
						verDirection = readIntKey(windowHandleKey);
						MessageBox.Show($"readIntKey verDirection: {verDirection}");
					}
					else 
					{
						verDirection = 1; // todo what dir when outside margins ver but in both monitor borders? random?
						BFS.ScriptSettings.WriteValue(windowHandleKey, verDirection.ToString());
					}
				}
				else // outside monitor up and down, move window down to show top
				{
					verDirection = 1;
					MessageBox.Show("warning window both to high and too low"); // todo remove?
				}
			}
			else if (marginInfo.tooLow && !marginInfo.tooHigh) verDirection = -1; // force to go up
			else if (!marginInfo.tooLow && marginInfo.tooHigh) verDirection = 1; // force to go down
			else verDirection = 0; // vertically in margins, no move // todo really no move?
			MessageBox.Show($"verDirection: {verDirection}");
		}

		public static void decideHorDirectionForOutsideMargins(MarginStatus marginInfo, IntPtr windowHandle, Rectangle monitorRect, Rectangle windowRect, out int horDirection)
		{
			MessageBox.Show("decideHorDirectionForOutsideMargins");
			string windowHandleKey = windowHandle.ToString() + "_outmargins_hordir"; // todo
			MarginStatus borderInfo = getWindowBorderStatus(monitorRect, windowRect);

			if (marginInfo.tooLeft && marginInfo.tooRight) // outside both margins
			{
				if (borderInfo.tooRight && !borderInfo.tooLeft) horDirection = -1; // go left, back inside monitor
				else if (!borderInfo.tooRight && borderInfo.tooLeft) horDirection = 1; // go right, back inside monitor
				else if (!borderInfo.tooRight && !borderInfo.tooLeft) // outside both margins but in monitor borders
				{
					if (keyAlreadyGenerated(windowHandleKey))
					{
						horDirection = readIntKey(windowHandleKey);
						MessageBox.Show($"readIntKey horDirection: {horDirection}");
					}
					else
					{
						horDirection = 1; // todo what dir when outside margins hor but in both monitor borders? random?
						BFS.ScriptSettings.WriteValue(windowHandleKey, horDirection.ToString());
					}
				}
				else // outside monitor left and right, move window to the right to show left side
				{
					horDirection = 1;
					MessageBox.Show("warning window both too right and too left"); // todo remove?
				}
			}
			else if (marginInfo.tooLeft && !marginInfo.tooRight) horDirection = 1; // force to go right
			else if (!marginInfo.tooLeft && marginInfo.tooRight) horDirection = -1; // force to go left
			else horDirection = 0; // horizontally in margins, no move // todo really no move?
			MessageBox.Show($"horDirection: {horDirection}");
		}

		public static void HandleWindowOutsideMargins(MarginStatus marginInfo, IntPtr windowHandle, Rectangle monitorRect, Rectangle  windowRect,  out int horDirection, out int verDirection)
		{
			MessageBox.Show("HandleWindowOutsideMargins");

			// decide vertical direction
			decideVerDirectionForOutsideMargins(marginInfo, windowHandle, monitorRect, windowRect, out verDirection);
			decideHorDirectionForOutsideMargins(marginInfo, windowHandle, monitorRect, windowRect, out horDirection);

			MessageBox.Show("HandleWindowOutsideMargins end");
		}

		// public static MoveSingleWIndow(IntPtr windowHandle, int shiftHor, int shiftVer)
		// {
		// 	Rectangle windowRect = BFS.Window.GetBounds(windowHandle);
		// 	BFS.Window.SetLocation(windowHandle, windowRect.X+finalHorShift, windowRect.Y+finalVerShift);
		// }
		
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

		

		// topMarginKey, bottomMarginKey, leftMarginKey, rightMarginKey - outer borders
		// horizontalTopSplitKey - upper horizontal split for e.g bottom border on topmost window in 3 window horizontal split
		// horizontalMiddleSplitKey - horizontal split in the middle for e.g one top window and one bottom window
		// horizontalBottomSplitKey - lower horizontal split for e.g top border on bottommost window in 3 window horizontal split
		// verticalMiddleSplitKey - right in the middle of screen for Left / Right split or 4 corners split
		// verticalLeftSplitKey - left border when doing 1-1-1, 1-2 splits 
		// verticalRightSplitKey - right border when doing 1-1-1 and 2-1 splits
		public static void generateSplitBordersAllMonitors()
		{

			Rectangle[] allMonitors = BFS.Monitor.GetMonitorBoundsNoSplits();
			// uint[] allMonitors = BFS.Monitor.GetMonitorIDs();

			foreach (Rectangle monitorRectAsID in allMonitors)
			{
				Rectangle monitorRect = monitorRectAsID; //BFS.Monitor.GetMonitorBoundsByID(monitorRectAsID);
				uint monitorID = BFS.Monitor.GetMonitorIDByRect(monitorRectAsID);

				// todo add check for each monitor (not all one flag because new can be connected)
				// bool alreadyGenerated = readBoolKey(bordersAlreadyGeneratedKey);

				// create dict of borders for this monitorRectAsID if not present 
				if (!bordersDict.TryGetValue(monitorRectAsID, out Dictionary<string,int> borders))
				{
					bordersDict[monitorRectAsID] = new Dictionary<string,int>();
				}

				generateSplitBorder(monitorRectAsID, topMarginKey,  monitorRect.Y + GetRandomShift(0, shiftRange));
				generateSplitBorder(monitorRectAsID, bottomMarginKey, monitorRect.Y + monitorRect.Height - GetRandomShift(0, shiftRange));
				generateSplitBorder(monitorRectAsID, leftMarginKey, monitorRect.X + GetRandomShift(0, shiftRange));
				generateSplitBorder(monitorRectAsID, rightMarginKey, monitorRect.X + monitorRect.Width - GetRandomShift(0, shiftRange));
				generateSplitBorder(monitorRectAsID, horizontalTopSplitKey, monitorRect.Y + monitorRect.Height / 3 + GetRandomShift(shiftRange));
				generateSplitBorder(monitorRectAsID, horizontalMiddleSplitKey, monitorRect.Y + monitorRect.Height / 2 + GetRandomShift(shiftRange));
				generateSplitBorder(monitorRectAsID, horizontalBottomSplitKey, monitorRect.Y + monitorRect.Height * 2 / 3 + GetRandomShift(shiftRange));
				generateSplitBorder(monitorRectAsID, verticalLeftSplitKey, monitorRect.X + monitorRect.Width / 3 + GetRandomShift(shiftRange));
				generateSplitBorder(monitorRectAsID, verticalMiddleSplitKey, monitorRect.X + monitorRect.Width / 2 + GetRandomShift(shiftRange));
				generateSplitBorder(monitorRectAsID, verticalRightSplitKey, monitorRect.X + monitorRect.Width * 2 / 3 + GetRandomShift(shiftRange));
				//MessageBox.Show($"generateSplitBorder monitor ({monitorID}) X.{monitorRect.X} Y.{monitorRect.Y} [{monitorRect.Width}/{monitorRect.Height}] vemidsplit: {readIntKey(rightMarginKey+"_2")}");
			}

			// todo remove?
			DateTime currentTime = DateTime.Now; // Pobranie aktualnego czasu
			lastBordersRecalc = currentTime;
		}

		public static void generateSplitBorder(Rectangle monitorRectAsID, string borderKey, int newValue)
		{
			// use for storing in settings
			string monitorBorderkey = borderKey + "_" + monitorRectAsID.ToString();
			BFS.ScriptSettings.WriteValue(monitorBorderkey, ""); // TODO remove

			if (keyAlreadyGenerated(monitorBorderkey))
			{
				// ignore received new value and override with stored value
				//MessageBox.Show($"Key already generated: {monitorBorderkey}");
				newValue = readIntKey(monitorBorderkey);
			}
			else
			{
				// save in settings
				BFS.ScriptSettings.WriteValue(monitorBorderkey, newValue.ToString());
			}

				// try adding to bordersDict
			if (bordersDict[monitorRectAsID].TryAdd(borderKey, newValue))
			{
			}

			uint monitorID = BFS.Monitor.GetMonitorIDByRect(monitorRectAsID);
			//MessageBox.Show($"generateSplitBorder ({monitorID})[{monitorBorderkey}] W:{newValue.ToString()}, R:{readIntKey(monitorBorderkey)}");
		}
		
		public static void Left(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int verticalMiddleSplit = bordersDict[monitorRectAsID][verticalMiddleSplitKey];
			int leftMargin = bordersDict[monitorRectAsID][leftMarginKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];

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
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int rightMargin = bordersDict[monitorRectAsID][rightMarginKey];
			int verticalMiddleSplit = bordersDict[monitorRectAsID][verticalMiddleSplitKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];

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
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int leftMargin = bordersDict[monitorRectAsID][leftMarginKey];
			int verticalMiddleSplit = bordersDict[monitorRectAsID][verticalMiddleSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = verticalMiddleSplit - leftMargin;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
						
		public static void TopRight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int rightMargin = bordersDict[monitorRectAsID][rightMarginKey];
			int verticalMiddleSplit = bordersDict[monitorRectAsID][verticalMiddleSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = rightMargin - verticalMiddleSplit;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
					
		public static void BottomLeft(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = bordersDict[monitorRectAsID][leftMarginKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int verticalMiddleSplit = bordersDict[monitorRectAsID][verticalMiddleSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = verticalMiddleSplit - leftMargin;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
						
		public static void BottomRight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int rightMargin = bordersDict[monitorRectAsID][rightMarginKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int verticalMiddleSplit = bordersDict[monitorRectAsID][verticalMiddleSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = rightMargin - verticalMiddleSplit;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		
		// ************* 6 split *************

		public static void LeftmostTop(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = bordersDict[monitorRectAsID][leftMarginKey];
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = verticalLeftSplit - leftMargin;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		
		public static void LeftmostBottom(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = bordersDict[monitorRectAsID][leftMarginKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];
			
			int width = verticalLeftSplit - leftMargin;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
					
		public static void LeftmostCenter(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = bordersDict[monitorRectAsID][leftMarginKey];
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = verticalLeftSplit - leftMargin;
			int height = (bottomMargin - topMargin) / 2;
			int y = horizontalMiddleSplit - height / 2;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, y, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		public static void LeftmostFullHeight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int leftMargin = bordersDict[monitorRectAsID][leftMarginKey];
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];

			int width = verticalLeftSplit - leftMargin;
			int height = bottomMargin - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}
		
		public static void MiddleTop(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = verticalRightSplit - verticalLeftSplit;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
			
		public static void MiddleBottom(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = verticalRightSplit - verticalLeftSplit;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
			
		public static void MiddleCenter(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = verticalRightSplit - verticalLeftSplit;
			int height = (bottomMargin - topMargin) / 2;
			int y = horizontalMiddleSplit - height / 2;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, y, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	

		public static void MiddleFullHeight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];

			int width = verticalRightSplit - verticalLeftSplit;
			int height = bottomMargin - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
			
		public static void RightmostTop(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int rightMargin = bordersDict[monitorRectAsID][rightMarginKey];
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = rightMargin - verticalRightSplit;
			int height = horizontalMiddleSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, topMargin, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
						
		public static void RightmostBottom(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int rightMargin = bordersDict[monitorRectAsID][rightMarginKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];
			
			int width = rightMargin - verticalRightSplit;
			int height = bottomMargin - horizontalMiddleSplit;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, horizontalMiddleSplit, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}

		public static void RightmostCenter(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int rightMargin = bordersDict[monitorRectAsID][rightMarginKey];
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int horizontalMiddleSplit = bordersDict[monitorRectAsID][horizontalMiddleSplitKey];

			int width = rightMargin - verticalRightSplit;
			int height = (bottomMargin - topMargin) / 2;
			int y = horizontalMiddleSplit - height / 2;
			BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, y, width, height);
			if (enableWindowsAutoShift) StartMovingWindows(windowHandle);
		}	
			
		public static void RightmostFullHeight(IntPtr windowHandle)
		{
			Rectangle monitorRectAsID = getMouseMonitorBounds();
			int rightMargin = bordersDict[monitorRectAsID][rightMarginKey];
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];

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
			int topMargin = bordersDict[monitorRectAsID][topMarginKey];
			int bottomMargin = bordersDict[monitorRectAsID][bottomMarginKey];
			int leftMargin = bordersDict[monitorRectAsID][leftMarginKey];
			int rightMargin = bordersDict[monitorRectAsID][rightMarginKey];
			int verticalLeftSplit = bordersDict[monitorRectAsID][verticalLeftSplitKey];
			// int verticalMiddleSplit = bordersDict[monitorRectAsID][verticalMiddleSplitKey];
			int verticalRightSplit = bordersDict[monitorRectAsID][verticalRightSplitKey];
			int horizontalTopSplit = bordersDict[monitorRectAsID][horizontalTopSplitKey];
			int horizontalBottomSplit = bordersDict[monitorRectAsID][horizontalBottomSplitKey];

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

		public static bool timerBorderRecalculateExpired()
		{
			DateTime currentTime = DateTime.Now; // Pobranie aktualnego czasu
			TimeSpan elapsedTime = currentTime - lastBordersRecalc; // Obliczenie czasu, który minął

			// return (elapsedTime.TotalSeconds >= 15);
			return false;
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
			Rectangle[] monitorBoundsAll = BFS.Monitor.GetMonitorBoundsNoSplits();
			
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
			Rectangle[] monitorBoundsAll = BFS.Monitor.GetMonitorBoundsNoSplits();
			
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
	}