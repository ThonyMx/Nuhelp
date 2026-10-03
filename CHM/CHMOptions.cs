using Opulos.Core.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;

namespace Opulos.NüHelp.CHM {

// this object is saved as the hhp file
public class CHMOptions {

	///<summary>Used if you are merging two or more CHM help files together. Lists all CHMs which will merge into this CHM.</summary>
	public List<String> MergeFiles { get; set; } // FileInfo
	public List<CHMAliasMap> AliasMaps { get; set; }
	public List<CHMWindow> Windows { get; set; }

	///<summary>Set to an integer >= 0 to automatically create the toc.hhc file.  This uses the heading tags <h1> to <h9> in your HTML files to generate
	///the TOC. The number is the maximum level of tag to place in the contents. e.g. 1 = , 2 = , 9. WARNING: HHW modifies your HHC file if
	///you specify this.  Default value is null.</summary>
	public int AutoTOC { get; set; }

	///<summary>Set true to automatically create the index.hhk file.  In order for this to work, the html files must contain the
	///<object> tags with the appropriate <param> children.  The <object> tags will automatically be placed in a generated .hhk file.
	///Default value is false.</summary>
	public bool AutoIndex { get; set; }

	///<summary>Set true for older MSDN/VS6 Collections and big TOCs. Must be set false for Merge, Info Types, Custom Icon and other
	///HTML Help features to work.  If true, custom font and background color in TOC won't be used.  Default value is false.</summary>
	public bool BinaryTOC { get; set; }

	///<summary>Certain (generally not needed) HHC Help features require this to be true to work.  With a
	///binary index, the hhc compiler automatically sorts the hhk entries, but the font attribute no longer has any effect.  If set
	///to false, then Index tab will use the order of the items in the hhk file. Default value is false.</summary>
	public bool BinaryIndex { get; set; }

	///<summary>Name of compiled .CHM output file name.  E.g. Outfile.chm</summary>
	public String CompiledFile = ""; // FileInfo

	///<summary>Name of Table Of Contents file. Leave blank for no Contents tab.  E.g. Toc.hhc</summary>
	public String ContentsFile = ""; // FileInfo

	///<summary>Name of Index file. Leave blank for no Index tab.  E.g. Index.hhk</summary>
	public String IndexFile = ""; // FileInfo

	///<summary>Name of default Window defined in the [WINDOWS] section.  The properties and attributes of the default window are used
	///during compilation.  The other windows are ignored.  The Title of the default window overrides the CHMOptions title.</summary>
	public String DefaultWindow = "";

	///<summary>Default topic to open when CHM is opened. E.g. Index.htm.  Must be specified otherwise the help window displays the
	///contents of the index.hhk file, which look like bullet points with no content.</summary>
	public String DefaultTopic = ""; // FileInfo

	///<summary>Leave null for default.  Translates to FontName,FontPtSize,CharSet.  E.g. Georgia,8,0</summary>
	public CHMFont DefaultFont = new CHMFont();

	///<summary>Important to set this correctly since HH 1.x is an ANSI application and must know the correct language,
	///Font, Charset etc to use when sorting index entries and displaying text.</summary>
	public String Language = "0x0409 English - United States"; // "0x409 English (US)";

	///<summary>Help window caption title.  If the DefaultWindow has a title, then its title will be used instead.</summary>
	public String Title = "";

	//Including this gives a compile error: HHC4004: Warning: The option "..." is not recognized
	///<summary>Not required by HH 1.x. Used by MSDN collections. Contains cached index information.</summary>
	//public bool CreateCHIFile { get; set; }

	///<summary>1.0 or 1.1 or later</summary>
	public String Compatibility = "1.1 or later";

	///<summary>Compiler output log file.</summary>
	public String ErrorLogFile = "";

	///<summary>If false the search tab will be completely blank, i.e. buttons, results list and search text box will be invisible.
	///There doesn't seem to be a way to provide a custom search list, so this option seems like it should always be set true if
	///the search tab is going to be visible.  Default value is true.</summary>
	public bool FullTextSearch { get; set; }

	///<summary>Points to a file containing a list of Stop words.  Possibly not well implemented.  Create a txt file
	///containing a word on each line.  Those words will be excluded from Full-text search.  I guess this option is to reduce
	///the size of the search dictionary by excluding words like 'the', 'a', 'in', 'of', etc.</summary>
	public String FullTextSearchStopListFile = "";

	///<summary>Set true to see the files listed in the compiler log output.  Default value is false.</summary>
	public bool DisplayCompileProgress { get; set; }

	///<summary>Seems to do nothing. Possibly never fully implemented by MS.  Default value is false.</summary>
	public bool DisplayCompileNotes { get; set; }

	///<summary>Aka don't include folders in compiled file.  If true, all files will be stored in the CHM's root folder.
	///Default value is false since generally it's a good idea to keep files organized.  If you set to true and your html files
	///contain links to folders, E.g. images\Image.png, those links will be broken.</summary>
	public bool Flat { get; set; }

	///<summary>If the source files (html files, image files) of the chm file were lost, this option can make it easier to decompile
	///the chm file. Default value is false.</summary>
	public bool SupportEnhancedDecompilation { get; set; }

	///<summary>The font used in the TOC.</summary>
	public CHMFont TOCFont = new CHMFont();

	public String TOCForegroundColor = ""; // hex color (but in 0xBGR)

	public String TOCBackgroundColor = ""; // hex color (but in 0xBGR)

	///<summary>The font used in the Index.</summary>
	public CHMFont IndexFont = new CHMFont();

	public int TOCWindowStyles = 0;
	public int TOCWindowExStyles = 0;
	public int TOCTreeViewStyles = (int) (TVS.TVS_TRACKSELECT | TVS.TVS_SHOWSELALWAYS | TVS.TVS_HASBUTTONS | TVS.TVS_HASLINES | TVS.TVS_LINESATROOT);

	public int TOCDefaultFolderImage = 1; // closed book (base 1)
	public int TOCDefaultItemImage = 11; // document with writing (base 1)

	// HHPAppendText is a crutch to support any features that NuHelp doesn't support directly.  In the help documentation
	// the user is cautioned that long term additional features may be added to better support this, and thus the setting may
	// need to be adjusted to avoid conflict.
	//
	// This text is appended to the end of the HHP file just before the hhc.exe compile is called.
	public String HHPAppendText = "";

	public CHMOptions() {
		FullTextSearch = true;
		AutoTOC = -1;

		MergeFiles = new List<String>();
		AliasMaps = new List<CHMAliasMap>();
		Windows = new List<CHMWindow>();
	}

	private static String yn(bool b) {
		return (b ? "Yes" : "No");
	}

	public override String ToString() {
		StringBuilder sb = new StringBuilder();
		sb.AppendLine("[OPTIONS]");
		sb.AppendLine("Auto Index=" + yn(AutoIndex));
		if (AutoTOC >= 0) sb.AppendLine("Auto TOC=" + AutoTOC);
		sb.AppendLine("Binary Index=" + yn(BinaryIndex));
		sb.AppendLine("Binary TOC=" + yn(BinaryTOC));
		sb.AppendLine("Compatibility=" + Compatibility);
		if (!String.IsNullOrEmpty(CompiledFile)) sb.AppendLine("Compiled file=" + CompiledFile);
		if (!String.IsNullOrEmpty(ContentsFile)) sb.AppendLine("Contents file=" + Path.GetFileName(ContentsFile));
		//sb.AppendLine("CreateCHIFile=" + yn(CreateCHIFile));
		if (!String.IsNullOrEmpty(DefaultFont.FamilyName)) sb.AppendLine("Default Font=" + DefaultFont.ToString());
		sb.AppendLine("Default Window=" + DefaultWindow);
		if (!String.IsNullOrEmpty(DefaultTopic)) sb.AppendLine("Default topic=" + DefaultTopic); // do not use Path.GetFileName(...), instead we already use GetRelativePath(...)
		sb.AppendLine("Display compile notes=" + yn(DisplayCompileNotes));
		sb.AppendLine("Display compile progress=" + yn(DisplayCompileProgress));
		sb.AppendLine("Enhanced decompilation=" + yn(SupportEnhancedDecompilation));
		if (!String.IsNullOrEmpty(ErrorLogFile)) sb.AppendLine("Error log file=" + ErrorLogFile);
		sb.AppendLine("Flat=" + yn(Flat));
		if (!String.IsNullOrEmpty(FullTextSearchStopListFile)) sb.AppendLine("Full text search stop list file=" + FullTextSearchStopListFile);
		sb.AppendLine("Full-text search=" + yn(FullTextSearch));
		if (!String.IsNullOrEmpty(IndexFile)) sb.AppendLine("Index file=" + Path.GetFileName(IndexFile));
		sb.AppendLine("Language=" + Language);
		sb.AppendLine("Title=" + Title);
		sb.AppendLine();

		if (Windows.Count > 0) {
			sb.AppendLine("[WINDOWS]");
			foreach (CHMWindow w in Windows)
				sb.AppendLine(w.ToString());
				
		}

		if (MergeFiles.Count > 0) {
			sb.AppendLine();
			sb.AppendLine("[MERGE FILES]");
			foreach (String f in MergeFiles)
				sb.AppendLine(f);
		}

		if (AliasMaps.Count > 0) {
			sb.AppendLine();
			sb.AppendLine("[ALIAS]");
			foreach (CHMAliasMap a in AliasMaps)
				sb.Append(a.Alias).Append("=").AppendLine(a.URL);

			sb.AppendLine();
			sb.AppendLine("[MAP]");
			foreach (CHMAliasMap a in AliasMaps) {
				sb.Append("#define ").Append(a.Alias).Append(" ").Append(a.ID);
				if (!String.IsNullOrEmpty(a.Comment))
					sb.Append(" ;").Append(a.Comment);

				sb.AppendLine();
			}
		}

		if (!String.IsNullOrEmpty(HHPAppendText))
			sb.AppendLine(HHPAppendText);

		//sb.AppendLine();
		//sb.AppendLine("[TEXT POPUPS]");
		//sb.AppendLine();
		//sb.AppendLine("[INFOTYPES]");
		//sb.AppendLine();
		//sb.AppendLine("[SUBSETS]");

		return sb.ToString();
	}
}
}
