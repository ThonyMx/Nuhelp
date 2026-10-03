using System;
using System.Windows.Forms;

namespace Opulos.NüHelp {
public static class HelpConstants {

	public static readonly String CHMFile = "QuickGuide.chm";
	public static readonly String About = "About.html";
	public static readonly String AdvancedOptions = "AdvancedOptions.html";
	public static readonly String Bad_File_Paths = "Bad-File-Paths.html";
	public static readonly String Blank_Headers = "Blank-Headers.html";
	public static readonly String CHMOptions = "CHMOptions.html";
	public static readonly String CommandLine = "CommandLine.html";
	public static readonly String Encodings = "Encodings.html";
	public static readonly String ExtraFiles = "ExtraFiles.html";
	public static readonly String File_Locked = "File-Locked.html";
	public static readonly String Fonts = "Fonts.html";
	public static readonly String Gibberish_in_TOC = "Gibberish-in-TOC.html";
	public static readonly String Help_File_Integration = "Help-File-Integration.html";
	public static readonly String How_It_Works = "How-It-Works.html";
	public static readonly String HTML = "HTML.html";
	public static readonly String Image_Quality = "Image-Quality.html";
	public static readonly String Index_Text_Not_Formatted_Correctly = "Index-Text-Not-Formatted-Correctly.html";
	public static readonly String Invariant_Names = "Invariant-Names.html";
	public static readonly String MainScreen = "MainScreen.html";
	public static readonly String Options = "Options.html";
	public static readonly String Options_General_Options = "Options.html#General_Options";
	public static readonly String ProjectTree = "ProjectTree.html";
	public static readonly String Punycode_Warning_Message = "Punycode-Warning-Message.html";
	public static readonly String Quick_Start = "Quick-Start.html";
	public static readonly String Temporary_Files = "Temporary-Files.html";
	public static readonly String Troubleshooting = "Troubleshooting.html";
	public static readonly String Windows = "Windows.html";

	public static void ShowHelp(Form f, String constant) {
		Help.ShowHelp(f, CHMFile, HelpNavigator.Topic, constant);
	}
}
}
