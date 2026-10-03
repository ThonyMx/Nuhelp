using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

using Opulos.Core.Win32;

namespace Opulos.NüHelp.CHM {
public class CHMWindow {

	public CHMWindow() {
		//WindowStyles = (int) (WS.WS_OVERLAPPEDWINDOW
	}

	///<summary>The hhp file can contain many definitions for windows, and the user selects which definition to use, which allows the designer to quickly
	///switch the window style.   In the [OPTIONS] section of the hhp file, the "Default Window" option points to a window name, (e.g. Default Window=main).
	///Then the [WINDOWS] section contains an entry main=... where "..." are the parameters of the window defined in this class.</summary>
	public String Name = "main";

	///<summary>Defines the caption of the help window.</summary>
	public String TitleBarText = "";

	///<summary>Defines which buttons appear at the top of the help window.  The default buttons are:
	///Back, Forward, Home, Options, Print and Font Size.  The Options button adds functionality of buttons not included.</summary>
	public int ButtonBar = (int) (CHMButton.Back | CHMButton.Forward | CHMButton.Home | CHMButton.Options | CHMButton.Print | CHMButton.FontSize);

	///<summary>Defines which features are available.  By default, the search tab and favorites tab are visible.  Advanced search is enabled.
	///There is a menu bar.  The naviation pane and button bar are present.  The window remembers its last position and size on screen.
	///<summary>
	public int WinProp = (int) (CHMWinProp.SearchTab | CHMWinProp.AdvancedSearch | CHMWinProp.FavoritesTab | CHMWinProp.MenuBar |  CHMWinProp.ShowTOCAndButtonBar | CHMWinProp.UserPosition | CHMWinProp.ChangeTitle | CHMWinProp.AutoSync); // | WinProp.TopmostWindow;

	///<summary>Only applicable if ButtonBar.Jump1 is set.</summary>
	public String Jump1ButtonURL = "";
	///<summary>Only applicable if ButtonBar.Jump1 is set.</summary>
	public String Jump1ButtonText = "";
	///<summary>Only applicable if ButtonBar.Jump2 is set.</summary>
	public String Jump2ButtonURL = "";
	///<summary>Only applicable if ButtonBar.Jump2 is set.</summary>
	public String Jump2ButtonText = "";

	///<summary>Sets the bounds of the window on the screen.  Note: If all values are zero then this parameter is not saved in the hhp file.</summary>
	public RECT WindowBounds = new RECT();

	///<summary>Name of Table Of Contents file. Leave blank for no Contents tab.</summary>
	public String TOCFile = ""; //toc.hhc"; // FileInfo

	///<summary>Name of Index file. Leave blank for no Index tab.</summary>
	public String IndexFile = ""; //index.hhk"; // FileInfo

	///<summary>Default topic to display when the window opens. Seems to have no impact.</summary>
	public String DefaultFile = ""; //default.html"; // FileInfo

	///<summary>Topic to display when the Home toolbar button is pressed.  Must be specified otherwise pressing home shows 404 page in content pane.</summary>
	public String HomeFile = ""; //home.html"; // FileInfo

	///<summary>Specify the width of the naviation pane.  Set to 0 to use the default width.  Default value is 360.</summary>
	public int TOCWidth = 360;

	///<summary>Specifies which tab is selected by default when the help file is first opened.  Default value is the TOC tab.</summary>
	public int DefaultTab = (int) CHMTab.TOCTab;

	///<summary>Window creation styles. Typically 0xb0000?  Default is zero (which outputs empty string).</summary>
	public int WindowStyles = 0; // WS

	///<summary>Extended Style Flags. Default is 0 which outputs empty string.</summary>
	public int WindowExStyles = 0; // WSEX

	///<summary>If false, the window is hidden but the hh.exe is running.  Default value is true so the window is visible.</summary>
	public int ShowState =  (int) SW.SW_SHOWNORMAL;

	///<summary>This only applies the very first time the help file is displayed.  After the first time, it remembers the previous state.</summary>
	public bool TOCInitiallyHidden = false; // aka AdvOptions

	///<summary>Position of the tabs. Default value is Top, which is what most users would expect.</summary>
	public int TabPosition = (int) CHMTabPosition.Top;

	public int NotifyID = 0;

	public override String ToString() {
		StringBuilder sb = new StringBuilder();
		sb.Append(Name).Append("=").Append("\"").Append(TitleBarText).Append("\""); // A
		sb.Append(",").Append(get(TOCFile)); // B
		sb.Append(",").Append(get(IndexFile)); // C
		sb.Append(",").Append(get(DefaultFile)); // D
		sb.Append(",").Append(get(HomeFile)); // E
		sb.Append(",").Append(get(Jump1ButtonURL)); // F
		sb.Append(",").Append("\"" + Jump1ButtonText + "\""); // G
		sb.Append(",").Append(get(Jump2ButtonURL)); // H
		sb.Append(",").Append("\"" + Jump2ButtonText + "\""); // I
		sb.Append(",").Append(hex((int) WinProp)); // J
		sb.Append(",").Append(TOCWidth == 0 ? "" : "" + TOCWidth); // K

		sb.Append(",").Append(hex((int) ButtonBar)); // L
		sb.Append(",").Append(ToString(WindowBounds)); // M
		sb.Append(",").Append(WindowStyles == 0 ? "" : hex(WindowStyles)); // N
		sb.Append(",").Append(WindowExStyles == 0 ? "" : hex(WindowExStyles)); // O
		sb.Append(",").Append((int) ShowState); //Visible ? "" : "0"); // P

		sb.Append(",").Append(TOCInitiallyHidden ? "1" : ""); // Q
		sb.Append(",").Append("" + ((int) DefaultTab)); // R
		sb.Append(",").Append("" + ((int) TabPosition)); // S
		sb.Append("," + NotifyID); // T (ID to send in WM_NOTIFY messages).

		return sb.ToString();
	}

	private static String get(String page) {
		if (String.IsNullOrEmpty(page))
			return "";

		if (File.Exists(page))
			return "\"" + Path.GetFileName(page) + "\"";

		return "\"" + page + "\"";
	}


	public static String hex(int x) {
		return (x == 0 ? "" : "0x" + x.ToString("x"));
	}

	private static String ToString(RECT r) {
		if (r.X == 0 && r.Y == 0 && r.Width == 0 && r.Height == 0)
			return "";
		return "[" + r.X + "," + r.Y + "," + (r.X + r.Width) + "," + (r.Y + r.Height) + "]";
	}

	public static CHMWindow CreateDefaultWindow(String homeFile, String windowTitle, String name) {
		CHMWindow w = new CHMWindow();
		w.HomeFile = homeFile;
		w.DefaultFile = "";
		w.IndexFile = null;
		w.TOCFile = null;
		w.TitleBarText = windowTitle;
		w.Name = name;
		return w;
	}
}

// don't like the way Rectangle is serialized
public class RECT {
	public int X;
	public int Y;
	public int Width;
	public int Height;
}

/*
These styles match up with the User32.dll constants.  How I imagine the original intent would be
the window process that displays the chm file would read the WindowStyles and WindowExStyles as it creates
the window.  This would be the equivalent of overriding the CreateParams of a Form.  Possibly this worked
on older versions of windows.  A lot of the flags don't seem to work on Windows 8.

I tested using the help workshop, not in code, hence why the values are not set.  These are only provided
for information purposes.

Possibly the WinProp.NoDefStyle and NoDefExStyle need to be set in order for some of these to work, and
possibly Help workshop doesn't set those flags.
*/
//public enum WS : uint {}
	/*
	MaximizeBox,  // show/hide the maximize button (doesn't work, maximize always there)
	Maximize, // initial state of window is maximized (didn't test)
	Popup, // window style is popup (might work if window is a child window)  The TOC appears detached from the tab
	SystemMenu, // as an overlapped window, system menu always seems to be visible (except if ExStyle is ToolWindow)
	ClipChildWindows, // only applicable if you wanted the help file to show inside an MDI application (didn't test)
	ClipSiblingWindows, // ditto
	HorizontalScroll, // adds a horizontal scroll to the overall help window (works, but not useful)
	MinimizeBox, // show/hide the minimize button (doesn't work, minimize always there)
	Minimize, // initial state of window is minimized (didn't test)
	VerticalScroll, // adds a vertical scrollbar to the overall help window (works, but not useful)
	ChildWindow, // only applicable for an MDI application (didn't test)
	Border, // a thin border is added around the TOC pane.
	DlgFrame,
	Visible,
	Overlapped,
	ThickFrame,
	Disabled, // this works, the window can't be closed or moved (not useful)
	*/

//public enum WSEX : uint {}
	/*
	ToolWindow, // this works, create a ToolWindow style (no title though)
	ClientEdge, // not sure what this is for
	StaticEdge, // not sure what this is for
	RightAlignedText, // doesn't seem to work, possibly requires corresponding font
	RightToLeftReadingOrder, // doesn't seemt o work
	ControlParentWindow, // not sure what this is for
	AcceptFiles, // possibly for dropping html files on it?
	Topmost, // didn't test
	ContextHelp, // tried using to include a question mark button on the title bar, didn't work
	NoParentNotify, // probably for the WM message passing, didn't test
	LeftScrollBar, // works but not useful
	Transparent, // it's weird this option is available because it only works in conjuction with a transparent color
	*/


}
