using System;
using System.Collections.Generic;
using System.Text;

namespace Opulos.NüHelp.CHM {
[Flags]
public enum CHMWinProp : int {
	///<summary>Automatically hide/show tri-pane window</summary>
	AutoHideShow = 1 << 0,
	///<summary>Top-most window</summary>
	TopmostWindow = 1 << 1,
	///<summary>The window menu and minimize/maximize/close buttons won't appear if this is set.</summary>
	NoTitleBar = 1 << 2,
	///<summary>no default window styles</summary>
	NoDefStyles = 1 << 3,
	///<summary>no default extended window styles</summary>
	NoDefExStyles = 1 << 4,
	///<summary>If this isn't set, then only the content pane is visible.</summary>
	ShowTOCAndButtonBar = 1 << 5, // aka TriPane
	///<summary>no text on toolbar buttons</summary>
	NoButtonText = 1 << 6,
	///<summary>Post WM_QUIT message when window closes.  Possibly only for notification of the help window closing?</summary>
	PostQuit = 1 << 7,
	///<summary>If true, the selected item in the Navigation pane will automatically synchronize with the Topic pane.</summary>
	AutoSync = 1 << 8,
	///<summary>send tracking notification messages</summary>
	Tracking = 1 << 9,
	///<summary>include search tab in navigation pane</summary>
	SearchTab = 1 << 10,
	///<summary>include history tab in navigation pane</summary>
	HistoryTab = 1 << 11,
	///<summary>include favorites tab in navigation pane</summary>
	FavoritesTab = 1 << 12,
	///<summary>Put current HTML title in title bar (even if not specified the title is updated).</summary>
	ChangeTitle = 1 << 13,
	///<summary>No content pane.</summary>
	NoContentPane = 1 << 14, // aka NavigationWindowOnly
	///<summary>Don't display a toolbar</summary>
	NoToolBar = 1 << 15,
	///<summary>Shows the File, Edit, View, Go, Help menu bar</summary>
	MenuBar = 1 << 16,
	///<summary>Display advanced search features.  Only applicable if SearchTab is set.</summary>
	AdvancedSearch = 1 << 17,
	///<summary>Remember the user position of the help window for next time it opens.</summary>
	UserPosition = 1 << 18,
	CustomTab1 = 1 << 19,
	CustomTab2 = 1 << 20,
	CustomTab3 = 1 << 21,
	CustomTab4 = 1 << 22,
	CustomTab5 = 1 << 23,
	CustomTab6 = 1 << 24,
	CustomTab7 = 1 << 25,
	CustomTab8 = 1 << 26,
	CustomTab9 = 1 << 27,
	///<summary>The window type has a margin.  Seems to have no effect.</summary>
	HasMargin = 1 << 28,
}
}
