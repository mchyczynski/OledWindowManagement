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
	public static int shiftRange = 16;
	public static bool enableSizeVariance = false;
	public static bool enablePositionVariance = true;
	public static bool enableWholeSpaceShift = true;
	public static bool enableOutOfBoundChecks = true;
	
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
		
		//generateSplitBorders();
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
		MessageBox.Show("iFinalWinX " +iFinalWinX+ "\tiFinalWinY " + iFinalWinY+
		                "\niFinalWinW " +iFinalWinW+ "\tiFinalWinH " +iFinalWinH+ 
		                "\nrandomShiftW " +randomShiftW+ "\trandomShiftH " + randomShiftH+
		                "\nrandomShiftX " +randomShiftX+ "\trandomShiftY " +randomShiftY, "Info", MessageBoxButtons.OK);
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
	
	public static void generateSplitBorders()
	{
		string horizontalBorderKey = "horizontalBorderKey";
        string storedVal = BFS.ScriptSettings.ReadValue(horizontalBorderKey);
		
		if (string.IsNullOrEmpty(storedVal)) {
			MessageBox.Show("Not stored", "xx", MessageBoxButtons.OK);
			BFS.ScriptSettings.WriteValue(horizontalBorderKey, "my val");
		}
		else 
		{
			MessageBox.Show("Already stored:" + storedVal, "xx", MessageBoxButtons.OK);
		}
	}
}