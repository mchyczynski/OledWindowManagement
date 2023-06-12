using System;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;

// The 'windowHandle' parameter will contain the window handle for the:
// - Active window when run by hotkey
// - Window Location target when run by a Window Location rule
// - TitleBar Button owner when run by a TitleBar Button
// - Jump List owner when run from a Taskbar Jump List
// - Currently focused window if none of these match
public static class DisplayFusionFunction
{
	public static int shiftRange = 160;
	public static bool enableSizeVariance = false;
	public static bool enablePositionVariance = true;
	public static bool enableWholeSpaceShift = true;
	public static bool enableOutOfBoundChecks = true;
	
	public static string topMarginKey = "topMarginKey";
	public static string bottomMarginKey = "bottomMarginKey";
	public static string leftMarginKey = "leftMarginKey";
	public static string rightMarginKey = "rightMarginKey";
	public static string horizontalSplitKey = "horizontalSplitKey";
	public static string verticalMiddleSplitKey = "verticalMiddleSplitKey";
	public static string verticalLeftSplitKey = "verticalLeftSplitKey";
	public static string verticalRightSplitKey = "verticalRightSplitKey";

	public static int topMargin = 0;
	public static int bottomMargin = 0;
	public static int leftMargin = 0;
	public static int rightMargin = 0;
	public static int horizontalSplit = 0;
	public static int verticalMiddleSplit = 0;
	public static int verticalLeftSplit = 0;
	public static int verticalRightSplit = 0;
	
	public static DateTime lastBordersRecalc = DateTime.MinValue;
	
	public static void Run(IntPtr windowHandle)
	{
		//these are all of the functions from the "Window Management" functions list
		//the function are just called by their names. to find their names, you can copy them
		//from the context menus, or type "BFS.DisplayFusion.RunFunction(" and a window will come up
		//with all of the available functions
		//Regarding the "--- Cancel ---" entries, these are used to cancel the action, see below "MenuItem_Click"
		string[, ,] MenuEntries = 
			{
				//	{{ "Background-Color", "Foreground-Color", "Function-Name" }}
				{{ "Pink", "Maroon", "--- Cancel ---" }},
				//{{ "Khaki", "Black", "Move Window to Next Monitor" }},
				//{{ "Khaki", "Black", "Move Window to Previous Monitor" }},
				//{{ "PaleGreen", "Black", "Size and Move Window to Left Side of Monitor" }},
				//{{ "PaleGreen", "Black", "Size and Move Window to Right Side of Monitor" }},
				//{{ "PaleGreen", "Black", "Size and Move Window to Top Side of Monitor" }},
				//{{ "PaleGreen", "Black", "Size and Move Window to Bottom Side of Monitor" }},
				//{{ "Aquamarine", "Black", "Move Window to Top-Left Corner and Size 50%" }},
				//{{ "Aquamarine", "Black", "Move Window to Top-Right Corner and Size 50%" }},
				//{{ "Aquamarine", "Black", "Move Window to Bottom-Left Corner and Size 50%" }},
				//{{ "Aquamarine", "Black", "Move Window to Bottom-Right Corner and Size 50%" }},
				{{ "Aquamarine", "Black", "SingleWindow100" }},
				{{ "Aquamarine", "Black", "SingleWindow95" }},
				{{ "Aquamarine", "Black", "SingleWindow90" }},
				{{ "Aquamarine", "Black", "SingleWindow80" }},
				{{ "Aquamarine", "Black", "SingleWindow60" }},
				
				{{ "Khaki", "Black", "Left" }},
				{{ "Khaki", "Black", "Right" }},
				
				{{ "Pink", "Maroon", "--- Cancel ---" }}
			};

		//create a new ContextMenuStrip to show the items
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
	
	//this function will get the text of the item and try to run it as a DisplayFusion function
	//"--- Cancel ---", change it to what you used in MenuEntries-List
	private static void MenuItem_Click(object sender, EventArgs e, IntPtr windowHandle)
	{
		
		ToolStripItem item = sender as ToolStripItem;
		if (item == null || item.Text == "--- Cancel ---")
			return;
		
		//generateSplitBorders(windowHandle);
		RunMy(item.Text, windowHandle);
	}

	private static void RunMy(string functionName, IntPtr windowHandle)
	{
		//MessageBox.Show("To jest powiadomienie RunMy.", "Tytuł powiadomienia", MessageBoxButtons.OK);
		
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
		Rectangle monitorRect = BFS.Monitor.GetMonitorWorkAreaByWindow(windowHandle);

		double factorComplement = 1 - factor;
		
		// calculate windows size variance
		int randomShiftW = enableSizeVariance ? GetRandomShift(shiftRange) : 0;
		int randomShiftH = enableSizeVariance ? GetRandomShift(shiftRange) : 0;
		
		// calculate final window size
		int iFinalWinW = Convert.ToInt32(monitorRect.Width * factor) + randomShiftW;
		int iFinalWinH = Convert.ToInt32(monitorRect.Height * factor) + randomShiftH;
		
		// calculate window position variance
		int randomShiftX = 0, randomShiftY = 0;
		int iFinalWinX = (monitorRect.Width - iFinalWinW) / 2;
		int iFinalWinY = (monitorRect.Height - iFinalWinH) / 2;
		
		if (enablePositionVariance)
		{
			if(enableWholeSpaceShift) // randomly move windows within whole unused space
			{
				// calculate how much free space there is
				int freeSpaceW = monitorRect.Width - iFinalWinW;
				int freeSpaceH = monitorRect.Height - iFinalWinH;
				
				// override final position as generateed random shift within free space
				randomShiftX = GetRandomShift(0, freeSpaceW);
				randomShiftY = GetRandomShift(0, freeSpaceH);
				iFinalWinX = randomShiftX;
				iFinalWinY = randomShiftY;
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
			
			if (iFinalWinX < 0) iFinalWinX = 0; // check window out of bound left
			if (iFinalWinY < 0) iFinalWinY = 0; // check window out of bound top

			if (( iFinalWinX + iFinalWinW) > monitorRect.Width ) iFinalWinX = monitorRect.Width - iFinalWinW; // check window out of bound right
			if (( iFinalWinY + iFinalWinH) > monitorRect.Height ) iFinalWinY = monitorRect.Height - iFinalWinH; // check window out of bound bottom
		}
		
		// move and resize window
		BFS.Window.SetSizeAndLocation(windowHandle, iFinalWinX, iFinalWinY, iFinalWinW, iFinalWinH );
		
		// display values for debug
		// MessageBox.Show("iFinalWinX " +iFinalWinX+ "\tiFinalWinY " + iFinalWinY+
		                // "\niFinalWinW " +iFinalWinW+ "\tiFinalWinH " +iFinalWinH+ 
		                // "\nrandomShiftW " +randomShiftW+ "\trandomShiftH " + randomShiftH+
		                // "\nrandomShiftX " +randomShiftX+ "\trandomShiftY " +randomShiftY, "Info", MessageBoxButtons.OK);
	}
		


	
	// zoneFill - what percetange of dedicated zone window shoudl occupy, e.g 0.9 for 90% space fill
	// horizontalFactor - to determine how many zones there will be in the screen e.g. 0.33 (1/3) for one out of three zones 
	// verticalFactor - same as horizontal but for vertical number of zones
	// returns width, height, shiftW, shiftH
	public static (int, int, int, int) CalculateFinalWindowSize(IntPtr windowHandle, double zoneFill, double horizontalFactor, double verticalFactor)
	{
		Rectangle monitorRect = BFS.Monitor.GetMonitorWorkAreaByWindow(windowHandle);
		
		// calculate bae width and height
		int width = Convert.ToInt32(monitorRect.Width * zoneFill * horizontalFactor);
		int height = Convert.ToInt32(monitorRect.Height * zoneFill * verticalFactor);
		
		int shiftW = 0, shiftH = 0;
		
		// when enabled add variance
		if (enableSizeVariance)
		{
			// only decrease window size so it is not out of bounds
			shiftW = GetRandomShift(-shiftRange, 0);
			shiftW = GetRandomShift(-shiftRange, 0);
			
			width = width + shiftW;
			height = height + shiftH;
		}
		
		return (width, height, shiftW, shiftH);
	}

	
	public static int GetRandomShift(int range)
	{
		Random random = new Random();

	//	int randomNumber;
	//	do
	//	{
	//	    randomNumber = random.Next(-range, range + 1);
	//	} 
	//	while (randomNumber >= -range / 2 && randomNumber <= range / 2);
		
	//	return randomNumber;
		return random.Next(-range, range + 1);
	}
	
	public static int GetRandomShift(int start, int end)
	{
		Random random = new Random();
		return random.Next(start, end);
	}

	
	// topMarginKey, bottomMarginKey, leftMarginKey, rightMarginKey - outer borders
	// horizontalSplitKey - only one horizontal split
	// verticalMiddleKey - right in the middle of screen for Left / Right split or 4 corners split
	// verticalLeftKey - left border when doing 1-1-1, 1-2 splits 
	// verticalRightKey - right border when doing 1-1-1 and 2-1 splits
	public static void generateSplitBorders(IntPtr windowHandle)
	{
		Rectangle monitorRect = BFS.Monitor.GetMonitorWorkAreaByWindow(windowHandle);
		
		// topMarginKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(topMarginKey))
		{
			topMargin = 0 + GetRandomShift(0, shiftRange);
			BFS.ScriptSettings.WriteValue(topMarginKey, topMargin.ToString());
		}
		else topMargin = readIntKey(topMarginKey);
				
		// bottomMarginKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(bottomMarginKey))
		{
			bottomMargin = monitorRect.Height - GetRandomShift(0, shiftRange);
			BFS.ScriptSettings.WriteValue(bottomMarginKey, bottomMargin.ToString());
		}
		else bottomMargin = readIntKey(bottomMarginKey);
		
		// leftMarginKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(leftMarginKey))
		{
			leftMargin = 0 + GetRandomShift(0, shiftRange);
			BFS.ScriptSettings.WriteValue(leftMarginKey, leftMargin.ToString());
		}
		else leftMargin = readIntKey(leftMarginKey);
				
		// rightMarginKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(rightMarginKey))
		{
			rightMargin = monitorRect.Width - GetRandomShift(0, shiftRange);
			BFS.ScriptSettings.WriteValue(rightMarginKey, rightMargin.ToString());
		}
		else rightMargin = readIntKey(rightMarginKey);
		
		// horizontalSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(horizontalSplitKey))
		{
			horizontalSplit = monitorRect.Height / 2 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(horizontalSplitKey, horizontalSplit.ToString());
		}
		else horizontalSplit = readIntKey(horizontalSplitKey);

		// verticalMiddleSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(verticalMiddleSplitKey))
		{
			verticalMiddleSplit = monitorRect.Width / 2 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(verticalMiddleSplitKey, verticalMiddleSplit.ToString());
		}
		else verticalMiddleSplit = readIntKey(verticalMiddleSplitKey);

		// verticalLeftSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(verticalLeftSplitKey))
		{
			verticalLeftSplit = monitorRect.Width / 3 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(verticalLeftSplitKey, verticalLeftSplit.ToString());
		}		
		else verticalLeftSplit = readIntKey(verticalLeftSplitKey);

		// verticaRightSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(verticalRightSplitKey))
		{
			verticalRightSplit = monitorRect.Width * 2 / 3 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(verticalRightSplitKey, verticalRightSplit.ToString());
		}
		else verticalRightSplit = readIntKey(verticalRightSplitKey);
		
		DateTime currentTime = DateTime.Now; // Pobranie aktualnego czasu
		lastBordersRecalc = currentTime;
		
		// display values for debug
		// MessageBox.Show("topMargin " + topMargin + "\tbottomMargin " + bottomMargin +
		                // "\nleftMargin " + leftMargin + "\trightMargin " + rightMargin + 
		                // "\nhorizontalSplit " + horizontalSplit + "\tverticalMiddleSplit " + verticalMiddleSplit +
		                // "\nverticalLeftSplit " + verticalLeftSplit + "\tverticaRightKey " + verticalRightSplit, "Info", MessageBoxButtons.OK);
	}
	
	public static void Left(IntPtr windowHandle)
	{
		generateSplitBorders(windowHandle);
		//Rectangle monitorRect = BFS.Monitor.GetMonitorWorkAreaByWindow(windowHandle);
		
		int width = verticalMiddleSplit - leftMargin;
		int height = bottomMargin - topMargin;
		BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
		
		// MessageBox.Show("X " + leftMargin + "\tY " + topMargin +
				// "\nwidth " + width + "\theight " + height );
	}
	
	public static void Right(IntPtr windowHandle)
	{
		generateSplitBorders(windowHandle);
		//Rectangle monitorRect = BFS.Monitor.GetMonitorWorkAreaByWindow(windowHandle);
		
		int width = rightMargin - verticalMiddleSplit;
		int height = bottomMargin - topMargin;
		BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, topMargin, width, height);
		
		// MessageBox.Show("X " + leftMargin + "\tY " + topMargin +
				// "\nwidth " + width + "\theight " + height );
	}
	
	public static bool keyAlreadyGenerated(string key)
	{
		return !string.IsNullOrEmpty(BFS.ScriptSettings.ReadValue(key));
	}
	
	public static int readIntKey(string key)
	{
		return int.Parse(BFS.ScriptSettings.ReadValue(key));
	}
	
	public static bool timerBorderRecalculateExpired()
	{
		DateTime currentTime = DateTime.Now; // Pobranie aktualnego czasu
		TimeSpan elapsedTime = currentTime - lastBordersRecalc; // Obliczenie czasu, który minął

		if (elapsedTime.TotalSeconds >= 15)
		{
			// Triger borders change
			return true;
		}
		else
		{
			// too soon to borders change
			return false;
		}

	}
}