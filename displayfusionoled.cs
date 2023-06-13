using System;
using System.Text;
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
	public static int shiftRange = 32;
	public static bool enableSizeVariance = false;
	public static bool enablePositionVariance = true;
	public static bool enableWholeSpaceShift = true;
	public static bool enableOutOfBoundChecks = true;
	
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

	public static int topMargin = 0;
	public static int bottomMargin = 0;
	public static int leftMargin = 0;
	public static int rightMargin = 0;
	public static int horizontalTopSplit = 0;
	public static int horizontalMiddleSplit = 0;
	public static int horizontalBottomSplit = 0;
	public static int verticalLeftSplit = 0;
	public static int verticalMiddleSplit = 0;
	public static int verticalRightSplit = 0;
	
	public static DateTime lastBordersRecalc = DateTime.MinValue;

	public static string KEY_SHIFT = "16";
	public static string KEY_CTRL = "17";
	public static string KEY_A = "65";
	public static string KEY_Q = "81";
	
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
				
				{{ "Aquamarine", "Black", "SingleWindow100" }},
				{{ "Aquamarine", "Black", "SingleWindow95" }},
				{{ "Aquamarine", "Black", "SingleWindow90" }},
				{{ "Aquamarine", "Black", "SingleWindow80" }},
				{{ "Aquamarine", "Black", "SingleWindow60" }},
				
				{{ "Khaki", "Black", "Left" }},
				{{ "Khaki", "Black", "Right" }},
				
				// {{ "PaleGreen", "Black", "TopLeft" }},
				// {{ "PaleGreen", "Black", "TopRight" }},
				// {{ "PaleGreen", "Black", "BottomLeft" }},
				// {{ "PaleGreen", "Black", "BottomRight" }},
				
				// {{ "DodgerBlue", "Black", "TopLeftmost" }},
				// {{ "DodgerBlue", "Black", "TopMiddle" }},
				// {{ "DodgerBlue", "Black", "TopRightmost" }},
				// {{ "DodgerBlue", "Black", "BottomLeftmost" }},
				// {{ "DodgerBlue", "Black", "BottomMiddle" }},
				// {{ "DodgerBlue", "Black", "BottomRightmost" }},
				
				{{ "DodgerBlue", "Black", "SixSplitLeft" }},
				{{ "DodgerBlue", "Black", "SixSplitMiddle" }},
				{{ "DodgerBlue", "Black", "SixSplitRight" }},
				
				{{ "DarkViolet", "Black", "NineSplit" }},
				// {{ "DarkViolet", "Black", "" }},
				// {{ "DarkViolet", "Black", "" }},
				// {{ "DarkViolet", "Black", "" }},
				// {{ "DarkViolet", "Black", "" }},
				// {{ "DarkViolet", "Black", "" }},
				// {{ "DarkViolet", "Black", "" }},
				// {{ "DarkViolet", "Black", "" }},
				// {{ "DarkViolet", "Black", "" }},
				
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
		Rectangle monitorRect = getCurrentMonitorBounds();
		
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
				
				// override final position as generateed random shift within free space
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
		
		// display values for debug
		// MessageBox.Show("iFinalWinX " +iFinalWinX+ "\tiFinalWinY " + iFinalWinY+
		                // "\niFinalWinW " +iFinalWinW+ "\tiFinalWinH " +iFinalWinH+ 
		                // "\nrandomShiftW " +randomShiftW+ "\trandomShiftH " + randomShiftH+
		                // "\nrandomShiftX " +randomShiftX+ "\trandomShiftY " +randomShiftY +
						// "\nmonitorRect.X " + monitorRect.X + "\tmonitorRect.Y " + monitorRect.Y);
	}

	public static int GetRandomShift(int range)
	{
		Random random = new Random();
		return random.Next(-range, range + 1);
	}
	
	public static int GetRandomShift(int start, int end)
	{
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
	public static void generateSplitBorders(IntPtr windowHandle)
	{
		// todo add monitor id to keys?
		Rectangle monitorRect = getCurrentMonitorBounds();
		
		// topMarginKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(topMarginKey))
		{
			topMargin = monitorRect.Y + GetRandomShift(0, shiftRange);
			BFS.ScriptSettings.WriteValue(topMarginKey, topMargin.ToString());
		}
		else topMargin = readIntKey(topMarginKey);
				
		// bottomMarginKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(bottomMarginKey))
		{
			bottomMargin = monitorRect.Y + monitorRect.Height - GetRandomShift(0, shiftRange);
			BFS.ScriptSettings.WriteValue(bottomMarginKey, bottomMargin.ToString());
		}
		else bottomMargin = readIntKey(bottomMarginKey);
		
		// leftMarginKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(leftMarginKey))
		{
			leftMargin = monitorRect.X + GetRandomShift(0, shiftRange);
			BFS.ScriptSettings.WriteValue(leftMarginKey, leftMargin.ToString());
		}
		else leftMargin = readIntKey(leftMarginKey);
				
		// rightMarginKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(rightMarginKey))
		{
			rightMargin = monitorRect.X + monitorRect.Width - GetRandomShift(0, shiftRange);
			BFS.ScriptSettings.WriteValue(rightMarginKey, rightMargin.ToString());
		}
		else rightMargin = readIntKey(rightMarginKey);
		
		// horizontalTopSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(horizontalTopSplitKey))
		{
			horizontalTopSplit = monitorRect.Y + monitorRect.Height / 3 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(horizontalTopSplitKey, horizontalTopSplit.ToString());
		}
		else horizontalTopSplit = readIntKey(horizontalTopSplitKey);
		
		// horizontalMiddleSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(horizontalMiddleSplitKey))
		{
			horizontalMiddleSplit = monitorRect.Y + monitorRect.Height / 2 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(horizontalMiddleSplitKey, horizontalMiddleSplit.ToString());
		}
		else horizontalMiddleSplit = readIntKey(horizontalMiddleSplitKey);
		
		// horizontalBottomSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(horizontalBottomSplitKey))
		{
			horizontalBottomSplit = monitorRect.Y + monitorRect.Height * 2 / 3 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(horizontalBottomSplitKey, horizontalBottomSplit.ToString());
		}
		else horizontalBottomSplit = readIntKey(horizontalBottomSplitKey);

		// verticalLeftSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(verticalLeftSplitKey))
		{
			verticalLeftSplit = monitorRect.X + monitorRect.Width / 3 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(verticalLeftSplitKey, verticalLeftSplit.ToString());
		}		
		else verticalLeftSplit = readIntKey(verticalLeftSplitKey);

		// verticalMiddleSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(verticalMiddleSplitKey))
		{
			verticalMiddleSplit = monitorRect.X + monitorRect.Width / 2 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(verticalMiddleSplitKey, verticalMiddleSplit.ToString());
		}
		else verticalMiddleSplit = readIntKey(verticalMiddleSplitKey);

		// verticaRightSplitKey
		if (timerBorderRecalculateExpired() || !keyAlreadyGenerated(verticalRightSplitKey))
		{
			verticalRightSplit = monitorRect.X + monitorRect.Width * 2 / 3 + GetRandomShift(shiftRange);
			BFS.ScriptSettings.WriteValue(verticalRightSplitKey, verticalRightSplit.ToString());
		}
		else verticalRightSplit = readIntKey(verticalRightSplitKey);
		
		DateTime currentTime = DateTime.Now; // Pobranie aktualnego czasu
		lastBordersRecalc = currentTime;
		
		// display values for debug
		// MessageBox.Show("topMargin " + topMargin + "\tbottomMargin " + bottomMargin +
		                // "\nleftMargin " + leftMargin + "\trightMargin " + rightMargin + 
		                // "\nhorTopSplit " + horizontalTopSplit + "\thorMiddleSplit " + horizontalMiddleSplit +"\thorBottomKey " + horizontalBottomSplit +
		                // "\nverLeftSplit " + verticalLeftSplit + "\tverMiddleSplit " + verticalMiddleSplit +"\tverRightKey " + verticalRightSplit);
	}
	
	public static void Left(IntPtr windowHandle)
	{
		generateSplitBorders(windowHandle);

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
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
		}
	}
	
	public static void Right(IntPtr windowHandle)
	{
		generateSplitBorders(windowHandle);

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
			BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, topMargin, width, height);
		}
	}
			
	public static void TopLeft(IntPtr windowHandle)
	{
		int width = verticalMiddleSplit - leftMargin;
		int height = horizontalMiddleSplit - topMargin;
		BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
	}
					
	public static void TopRight(IntPtr windowHandle)
	{
		int width = rightMargin - verticalMiddleSplit;
		int height = horizontalMiddleSplit - topMargin;
		BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, topMargin, width, height);
	}
				
	public static void BottomLeft(IntPtr windowHandle)
	{
		int width = verticalMiddleSplit - leftMargin;
		int height = bottomMargin - horizontalMiddleSplit;
		BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, horizontalMiddleSplit, width, height);
	}
					
	public static void BottomRight(IntPtr windowHandle)
	{
		int width = rightMargin - verticalMiddleSplit;
		int height = bottomMargin - horizontalMiddleSplit;
		BFS.Window.SetSizeAndLocation(windowHandle, verticalMiddleSplit, horizontalMiddleSplit, width, height);
	}
	
	public static void TopLeftmost(IntPtr windowHandle)
	{
		int width = verticalLeftSplit - leftMargin;
		int height = horizontalMiddleSplit - topMargin;
		BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
	}
	
	public static void TopMiddle(IntPtr windowHandle)
	{
		int width = verticalRightSplit - verticalLeftSplit;
		int height = horizontalMiddleSplit - topMargin;
		BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, topMargin, width, height);
	}	
		
	public static void TopRightmost(IntPtr windowHandle)
	{
		int width = rightMargin - verticalRightSplit;
		int height = horizontalMiddleSplit - topMargin;
		BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, topMargin, width, height);
	}	
	
	public static void BottomLeftmost(IntPtr windowHandle)
	{
		int width = verticalLeftSplit - leftMargin;
		int height = bottomMargin - horizontalMiddleSplit;
		BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, horizontalMiddleSplit, width, height);
	}
	
	public static void BottomMiddle(IntPtr windowHandle)
	{
		int width = verticalRightSplit - verticalLeftSplit;
		int height = bottomMargin - horizontalMiddleSplit;
		BFS.Window.SetSizeAndLocation(windowHandle, verticalLeftSplit, horizontalMiddleSplit, width, height);
	}	
		
	public static void BottomRightmost(IntPtr windowHandle)
	{
		int width = rightMargin - verticalRightSplit;
		int height = bottomMargin - horizontalMiddleSplit;
		BFS.Window.SetSizeAndLocation(windowHandle, verticalRightSplit, horizontalMiddleSplit, width, height);
	}
	

	
	public static void SixSplitLeft(IntPtr windowHandle)
	{
		generateSplitBorders(windowHandle);

		if (BFS.Input.IsKeyDown(KEY_SHIFT))
        {
			TopLeftmost(windowHandle);
        }
		else
		{
			BottomLeftmost(windowHandle);
		}
	}
	
	public static void SixSplitMiddle(IntPtr windowHandle)
	{
		generateSplitBorders(windowHandle);

		if (BFS.Input.IsKeyDown(KEY_SHIFT))
        {
			TopMiddle(windowHandle);
        }
		else
		{
			BottomMiddle(windowHandle);
		}
	}

	public static void SixSplitRight(IntPtr windowHandle)
	{
		generateSplitBorders(windowHandle);

		if (BFS.Input.IsKeyDown(KEY_SHIFT))
        {
			TopRightmost(windowHandle);
        }
		else
		{
			BottomRightmost(windowHandle);
		}
	}
	
	public static void NineSplit(IntPtr windowHandle)
	{
		if(BFS.Input.IsKeyDown(MergeKeyCodes(KEY_A, KEY_Q)))
		{
			generateSplitBorders(windowHandle);
		
			int width = verticalLeftSplit - leftMargin;
			int height = horizontalBottomSplit - topMargin;
			BFS.Window.SetSizeAndLocation(windowHandle, leftMargin, topMargin, width, height);
		}
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

		return (elapsedTime.TotalSeconds >= 15);
	}
	
	public static Rectangle getCurrentMonitorBounds()
	{
		// Get the mouse position
		Point mousePosition = new Point(BFS.Input.GetMousePositionX(), BFS.Input.GetMousePositionY());
		
		// Get an array of the bounds for all monitors ignoring splits
        Rectangle[] monitorBoundsAll = BFS.Monitor.GetMonitorBoundsNoSplits();
		
		// Loop through the array of bounds and move the window to the centre of whichever one the mouse cursor is on
		foreach (Rectangle monitorBounds in monitorBoundsAll)
		{
            if (monitorBounds.Contains(mousePosition))
            {
				// string pointAsString = string.Format("X={0},Y={1}", mousePosition.X, mousePosition.Y);
				// MessageBox.Show("mousePosition " + pointAsString +
				// "\nmonitorBounds.Width " + monitorBounds.Width + "\tmonitorBounds.Height " +monitorBounds.Height + 
				// "\nmonitorBounds.X " + monitorBounds.X + "\tmonitorBounds.Y " + monitorBounds.Y);
                return monitorBounds;
            }
		}
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