using System;
using System.Collections.Generic;
using System.Text;

namespace Opulos.NüHelp.CHM {
[Flags]
///<summary>Note: If you show 10 or more buttons, the button text is automatically hidden.
///After downloading and installing the Help Workshop, you can find these constants in the file:
///C:\Program Files (x86)\HTML Help Workshop\include\htmlhelp.h
///</summary>
public enum CHMButton : int {

	///<summary>Adds a button that when clicked toggles the left pane's visibility.</summary>
	HideShow = 1 << 1,
	///<summary>Adds a back navigation button.</summary>
	Back = 1 << 2,
	///<summary>Adds a forward navigation button.</summary>
	Forward = 1 << 3,
	///<summary>Adds a stop button to stop the download of the current page.</summary>
	Stop = 1 << 4,
	///<summary>Adds a refresh button to refresh the current page.</summary>
	Refresh = 1 << 5,
	///<summary>Adds a home button that when clicked returns the right content pane to the default page.</summary>
	Home = 1 << 6,

	// These buttons will show up in the help file but clicking them doesn't do anything
	BrowseForward = 1 << 7,
	BrowseBack = 1 << 8,
	Notes = 1 << 9,
	Contents = 1 << 10,

	///<summary>Adds a locate button that when clicked finds and focuses the Content item for the content displayed in the right pane.</summary>
	Locate = 1 << 11, // aka Sync
	///<summary>Adds a option button that has drop down options Hide Tabs, Back, Forward, Home, Stop, Refresh, Internet Options..., Print and Search Highlight Off</summary>
	Options = 1 << 12,
	///<summary>Adds a print button that when clicked brings up a print dialog.</summary>
	Print = 1 << 13,

	// These buttons will show up in the help file but clicking them doesn't do anything
	Index = 1 << 14,
	Search = 1 << 15,
	History = 1 << 16,
	Favorites = 1 << 17,

	///<summary>Adds a button that when clicked can navigate to a defined page.  Note: you have to define a Jump1ButtonURL and Jump1ButtonText.</summary>
	Jump1 = 1 << 18,
	///<summary>Adds a button that when clicked can navigate to a defined page.  Note: you have to define a Jump2ButtonURL and Jump2ButtonText.</summary>
	Jump2 = 1 << 19,

	///<summary>Adds a button that when clicked it cycles through 5 different font sizes, going from smaller to larger.  Also called Zoom.
	///In order for this button to work, font sizes have to be defined as font-size:medium;  If fixed font sizes are used, e.g. font-size:12pt,
	///then the font size won't change.</summary>
	FontSize = 1 << 20,

	// These buttons will show up as an up arrow and a down arrow, but I don't think they do anything.
	TOCNext = 1 << 21,
	TOCPrev = 1 << 22,
}
}
