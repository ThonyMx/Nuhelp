using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;

using Opulos.Core.IO;
using Opulos.NüHelp.CHM;
using System.ComponentModel;

namespace Opulos.NüHelp {

public class Settings : SettingsBase {

	public static Settings Default = null;

	static Settings() {
		String configFile = Path.Combine(DefaultFolder, "DefaultSettings.ini");
		Default = Load(configFile);
	}

	public static Settings Load(String filename) {
		Settings s = null;

		if (File.Exists(filename)) {
			try {
				s = FromXML<Settings>(filename);
			} catch {
				s = new Settings() { FilePath = filename };
			}
		}
		else {
			s = new Settings();
		}

		s.FilePath = filename;

		if (String.IsNullOrEmpty(s.OutputFolder))
			s.OutputFolder = Path.Combine(Path.GetDirectoryName(DefaultFilePath), "temp");

		if (String.IsNullOrEmpty(s.HtmlBodyOpenTag))
			s.HtmlBodyOpenTag = "<body style=' margin: 5px 5px 5px 5px; padding: 5px 5px 5px 5px;font-family: Verdana, Arial, Helvetica, sans-serif;' >";

		if (String.IsNullOrEmpty(s.CHMOptions.TOCFont.FamilyName))
			 s.CHMOptions.TOCFont = new CHMFont() { FamilyName = "Calibri", Size = 12 };

		if (String.IsNullOrEmpty(s.CHMOptions.IndexFont.FamilyName))
			s.CHMOptions.IndexFont = new CHMFont() { FamilyName = "Calibri", Size = 10 };

		if (String.IsNullOrEmpty(s.KeywordsDelim))
			s.KeywordsDelim = ";";

		return s;
	}

	public Settings() {
		CSharpFilename = "";
		CSharpNamespace = "";
		CreateCSharpFile = true;
		HtmlFooter = "<p><a href=\"[next]\">Next</a>\r\n<hr />";
		HtmlHeader = "<hr /><a href=\"[prev]\">Previous</a>";
		OpenAfterCompile = true;
		OverwriteExistingCHM = true;
		DeleteTempFiles = true;
		UseMiddleHeaderFooterAlways = true;
		CreateCSharpFile = true;
		OverrideHTags = true;
		ReplaceFontSizes = true;
		HTagSize = "h1";
		MaxNestedHeader = "h3";
		CHMOptions = new CHMOptions();
		ExtraFiles = new List<String>();
		ShowBlankHeadersWarning = true;
		BlankHeaders = BlankHeader.ShowInTOC;
		KeywordsDelim = ";";
		AdvancedOptions = new AdvancedOptions();
	}

	public CHMOptions CHMOptions { get; set; }
	public List<String> ExtraFiles { get; set; }

	public String HtmlFooter { get; set; }
	public String HtmlHeader { get; set; }
	public String HtmlHeaderFirstPage { get; set; }
	public String HtmlHeaderLastPage { get; set; }
	public String HtmlFooterFirstPage { get; set; }
	public String HtmlFooterLastPage { get; set; }
	public String HtmlBodyOpenTag { get; set; }
	public bool UseMiddleHeaderFooterAlways { get; set; }

	public bool WordVisible { get; set; }
	public bool CreateCSharpFile { get; set; }
	public String CSharpFilename { get; set; }
	public String CSharpNamespace { get; set; }

	public String OutputFolder { get; set; }
	public bool DeleteTempFiles { get; set; }
	public bool OpenAfterCompile { get; set; }
	public bool OverwriteExistingCHM { get; set; }
	public bool OverrideHTags { get; set; }
	public bool ReplaceFontSizes { get; set; }
	public String HTagSize { get; set; }
	public String MaxNestedHeader { get; set; }

	public int HtmlFileLockedTimeoutMillis = 10000;
	public int HtmlFilesReadEncoding = -1; // -1 == auto detect
	public int HtmlFilesWriteEncoding = -1; // -1 == System default
	public int HHCEncoding = 1252;
	public int HHKEncoding = 1252;
	public int HHPEncoding = 1252;

	public bool ShowBlankHeadersWarning { get; set; }
	public BlankHeader BlankHeaders { get; set; }
	public String KeywordsDelim { get; set; }

	public AdvancedOptions AdvancedOptions { get; set; }

	[XmlIgnore]
	public String Title {
		get {
			foreach (var w in CHMOptions.Windows) {
				if (w.Name == CHMOptions.DefaultWindow) {
					if (!String.IsNullOrEmpty(w.TitleBarText))
						return w.TitleBarText;
				}
			}
			return CHMOptions.Title;
		}
	}
}

public enum BlankHeader {

	[Description("Show in TOC")]
	ShowInTOC,

	[Description("Hide in TOC, linked")]
	HideInTOCLinked,

	[Description("Hide in TOC, not linked")]
	HideInTOCNotLinked

}

public class AdvancedOptions {

	public String PrecompileProcess = "";
	public String PrecompileProcessArguments = "";
	public String OutputTokensFilename = "";
	public String OutputTokens = "";
	public int OutputEncodingCodePage = UTF8Encoding.UTF8.CodePage;
	public String OutputDelimChar = "<tab>";
	public String OutputNewline = "\\r\\n";
	public bool OutputHeaderRow = false;
	public QuoteMode QuoteMode = QuoteMode.Normal;
	public bool IncludeEmptyRows = false;

	public String OutputQuoteChar = "\"";
	public String TokenSplitChar = "|";
	public String TokenLiteralChar = "\"";

}

public enum QuoteMode {
	Normal,
	Always,
	Never,
	NormalNeverLiterals,
}

}
