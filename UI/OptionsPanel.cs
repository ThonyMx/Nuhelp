using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Opulos.Core.UI;
using Opulos.Core.Utils;
using Opulos.NüHelp.CHM;

namespace Opulos.NüHelp {

public class OptionsPanel : Pane {

	Label lbTitle = new Label() { Text = "Title" };
	TextBox tbTitle = new TextBox2();

	Label lbDefaultPage = new Label() { Text = "Default Page" };
	ComboBox3<String> comboDefaultPage = new ComboBox3<String>() { DropDownStyle = ComboBoxStyle.DropDownList }; //n => String.IsNullOrEmpty(n) ? "" : Path.GetFileName(n));

	Label lbOutputFolder = new Label() { Text = "Output Folder" };
	TextBox tbOutputFolder = new TextBox2();

	Label lbCHMFilename = new Label() { Text = "CHM Filename" };
	TextBox tbCHMFilename = new TextBox2();

	Label lbLanguage = new Label() { Text = "Language" };
	ComboBox3<String> comboLanguage = new ComboBox3<String>() { DropDownStyle = ComboBoxStyle.DropDownList };

	Label lbCompatibilily = new Label() { Text = "Compatibility" };
	ComboBox3<String> comboCompatibility = new ComboBox3<String>() { DropDownStyle = ComboBoxStyle.DropDownList };

	Label lbErrorLogFile = new Label() { Text = "Error Log File" };
	TextBox tbErrorLogFile = new TextBox2();

	Label lbStopWordsFile = new Label() { Text = "Stop Words File" };
	TextBox tbStopWordsFile = new TextBox2();

	GroupBox gbExePath = null;
	Button btnBrowse = new Button() { Text = "Browse...", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink };
	TextBox tbCompilerExePath = new TextBox2();

	CheckBox cbDeleteTempFiles = new CheckBox() { Text = "Delete temp files when finished" };
	CheckBox cbOverwriteExistingCHM = new CheckBox() { Text = "Overwrite CHM file if it exists" };
	CheckBox cbOpenAfterCompile = new CheckBox() { Text = "Open CHM file after compile" };
	CheckBox cbReplaceFontSizes = new CheckBox() { Text = "Replace Font Sizes" };

	Label lbAutoTOC = new Label() { Text = "Auto TOC" };
	NumericUpDown nudAutoTOC = new NumericUpDown() { DecimalPlaces = 0, Minimum = -1, Maximum = 9 };

	CheckBox cbAutoIndex = new CheckBox() { Text = "Auto compile index" };
	CheckBox cbBinaryTOC = new CheckBox() { Text = "Binary TOC" };
	CheckBox cbBinaryIndex = new CheckBox() { Text = "Binary index" };
	CheckBox cbFullTextSearch = new CheckBox() { Text = "Full text search" };

	CheckBox cbLogCompiledFilenames = new CheckBox() { Text = "Log Compiled Filenames" };
	CheckBox cbDisplayCompileNotes = new CheckBox() { Text = "Display Compile Notes" };
	CheckBox cbFlattenFolders = new CheckBox() { Text = "Flatten Folders" };
	CheckBox cbSupportEnhancedDecompilation = new CheckBox() { Text = "Support Enhanced Decompilation" };

	CheckBox cbWordVisible = new CheckBox() { Text = "Word Visible" };
	CheckBox cbCreateCSharpFile = new CheckBox() { Text = "Create C# file" };
	Label lbCSharpFilename = new Label() { Text = "C# Filename" };
	TextBox tbCSharpFilename = new TextBox2() { Enabled = false };
	Label lbCSharpNamespace = new Label() { Text = "C# Namespace" };
	TextBox tbCSharpNamespace = new TextBox2() { Enabled = false };
	Label lbFileLockedTimeoutSeconds = new Label() { Text = "File Locked Timeout (seconds)" };
	NumericUpDown nudFileLockedTimeoutSeconds = new NumericUpDown() { DecimalPlaces = 3, Minimum = 0.5m, Maximum = 9999 };
	CheckBox cbOverrideHTags = new CheckBox() { Text = "Override H-Tags" };
	ComboBox3<String> comboHTagSize = new ComboBox3<String>("h1", "h2", "h3", "h4", "h5", "h6") { DropDownStyle = ComboBoxStyle.DropDownList };
	Label lbMaxNestedHeader = new Label() { Text = "Max Nested Header" };
	ComboBox3<String> comboMaxNestedHeader = new ComboBox3<String>("h1", "h2", "h3", "h4", "h5", "h6") { DropDownStyle = ComboBoxStyle.DropDownList };

	Label lbBlankHeaders = new Label() { Text = "Blank Headers" };
	CheckBox cbBlankHeadersShowWarning = new CheckBox() { Text = "Show Warning" };
	ComboBox3<BlankHeader> comboBlankHeaders = new ComboBox3<BlankHeader>(e => ((DescriptionAttribute) e.GetType().GetMember(e.ToString())[0].GetCustomAttributes(typeof(DescriptionAttribute),true)[0]).Description) { DropDownStyle = ComboBoxStyle.DropDownList };

	Label lbKeywordsDelim = new Label() { Text = "Keywords Delim" };
	ComboBox3<String> comboKeywordsDelim = new ComboBox3<String>(";",",","|");

	OpenFileDialog dialogOpenFile = null;

	public OptionsPanel() {
		comboBlankHeaders.AddRange(Enum.GetValues(typeof(BlankHeader)).Cast<BlankHeader>().ToList());

		comboLanguage.AddRange(GetLanguages());
		comboCompatibility.AddRange(new String[] { "1.1 or later", "1.0" });
		comboCompatibility.SelectedIndex = 0;

		RCLayout r1 = new RCLayout(2);
		Pane p1 = new Pane(r1);
		r1.Col[1].expand = 1;
		r1.Col[1].shrink = 1;
		r1.Col[1].fillX = 1;
		p1.add(lbTitle, tbTitle);
		p1.add(lbDefaultPage, comboDefaultPage);
		p1.add(lbOutputFolder, tbOutputFolder);
		p1.add(lbCHMFilename, tbCHMFilename);
		p1.add(lbLanguage, comboLanguage);
		p1.add(lbErrorLogFile, tbErrorLogFile);
		p1.add(lbStopWordsFile, tbStopWordsFile);
		p1.add(lbCompatibilily, comboCompatibility);
		p1.add(lbCSharpFilename, tbCSharpFilename);
		p1.add(lbCSharpNamespace, tbCSharpNamespace);

		Pane p2 = new Pane(0, tbCompilerExePath, btnBrowse) { Dock = DockStyle.Fill };
		gbExePath = new GroupBox2(p2, "HTML Help Workshop EXE path");

		Pane p3 = new Pane(LayoutDirection.Vertical, cbAutoIndex, cbFullTextSearch, cbBinaryIndex, cbBinaryTOC, cbLogCompiledFilenames, cbDisplayCompileNotes, cbFlattenFolders, cbSupportEnhancedDecompilation, new Pane(lbAutoTOC, nudAutoTOC));
		GroupBox gbCompileOptions = new GroupBox2(p3, "Compile Options");

		RCLayout r4b = new RCLayout(2);
		Pane p4b = new Pane(r4b);
		p4b.add(cbDeleteTempFiles, cbWordVisible);
		p4b.add(cbOverwriteExistingCHM, cbCreateCSharpFile);
		p4b.add(cbOpenAfterCompile, cbReplaceFontSizes);

		Pane p4 = new Pane(LayoutDirection.Vertical);
		p4.add(p4b);
		p4.add(new Pane(cbOverrideHTags, comboHTagSize));
		p4.add(new Pane(lbMaxNestedHeader, comboMaxNestedHeader));
		p4.add(new Pane(lbKeywordsDelim, comboKeywordsDelim));
		p4.add(new Pane(lbFileLockedTimeoutSeconds, nudFileLockedTimeoutSeconds));
		p4.add(new Pane(lbBlankHeaders, comboBlankHeaders, cbBlankHeadersShowWarning));
		
		GroupBox gbGeneralOptions = new GroupBox2(p4, "General Options");

		Pane p5 = new Pane(gbCompileOptions, gbGeneralOptions);
		ScrollPane sp5 = new ScrollPane(p5);

		RCLayout r0 = new RCLayout(1);
		r0.Col[0].expand = 1;
		r0.Col[0].shrink = 1;
		r0.OverallInsets = new Insets(10);
		r0[p1].fillX = 1;
		r0[gbExePath].fillX = 1;
		r0[sp5].fillX = 1;
		r0[sp5].fillY = 1;
		r0[sp5].expandRow = 1;
		r0[sp5].shrinkRow = 1;
		Pane p0 = this;
		this.setLayout(r0);
		p0.add(p1);
		p0.add(gbExePath);
		p0.add(sp5);

		btnBrowse.Click += btnBrowse_Click;
		cbCreateCSharpFile.CheckedChanged += delegate {
			tbCSharpFilename.Enabled = cbCreateCSharpFile.Checked;
			tbCSharpNamespace.Enabled = cbCreateCSharpFile.Checked;
		};
	}

	private void btnBrowse_Click(object sender, EventArgs e) {
		try {
			if (dialogOpenFile == null) {
				dialogOpenFile = new OpenFileDialog();
				dialogOpenFile.CheckFileExists = true;
				dialogOpenFile.Title = "Find Compiler hhc.exe";
				dialogOpenFile.Filter = "hhc.exe|hhc.exe|All Exe|*.exe";
			}

			if (!String.IsNullOrEmpty(tbCompilerExePath.Text))
				dialogOpenFile.InitialDirectory = System.IO.Path.GetDirectoryName(tbCompilerExePath.Text.Trim('"'));

			if (dialogOpenFile.ShowDialog(this) == DialogResult.OK) {
				tbCompilerExePath.Text = dialogOpenFile.FileName;
			}
		} catch (Exception ex) {
			MessageBox2.ShowError(this, ex);
		}
	}

	public void ApplyValues(Settings s) {
		SharedSettings.Default.HHCExePath = tbCompilerExePath.Text;

		s.DeleteTempFiles = cbDeleteTempFiles.Checked;
		s.OpenAfterCompile = cbOpenAfterCompile.Checked;
		s.ReplaceFontSizes = cbReplaceFontSizes.Checked;
		s.OverwriteExistingCHM = cbOverwriteExistingCHM.Checked;
		s.OutputFolder = tbOutputFolder.Text.Trim();
		s.WordVisible = cbWordVisible.Checked;
		s.CreateCSharpFile = cbCreateCSharpFile.Checked;
		s.CSharpFilename = tbCSharpFilename.Text.Trim();
		s.CSharpNamespace = tbCSharpNamespace.Text.Trim();
		s.HtmlFileLockedTimeoutMillis = (int) Convert.ToDouble(nudFileLockedTimeoutSeconds.Value) * 1000;
		s.OverrideHTags = cbOverrideHTags.Checked;
		s.HTagSize = comboHTagSize.SelectedItem;
		s.MaxNestedHeader = comboMaxNestedHeader.SelectedItem;
		s.ShowBlankHeadersWarning = cbBlankHeadersShowWarning.Checked;
		s.BlankHeaders = comboBlankHeaders.SelectedItem;
		s.KeywordsDelim = comboKeywordsDelim.Text;

		CHMOptions o = s.CHMOptions;
		o.DefaultTopic = comboDefaultPage.Text.Trim();
		o.Title = tbTitle.Text.Trim();
		o.CompiledFile = tbCHMFilename.Text.Trim();
		o.Language = comboLanguage.SelectedItem;
		o.Compatibility = comboCompatibility.SelectedItem;
		o.ErrorLogFile = tbErrorLogFile.Text.Trim();
		o.FullTextSearchStopListFile = tbStopWordsFile.Text.Trim();
		o.BinaryIndex = cbBinaryIndex.Checked;
		o.BinaryTOC = cbBinaryTOC.Checked;
		o.FullTextSearch = cbFullTextSearch.Checked;
		o.AutoIndex = cbAutoIndex.Checked;
		o.AutoTOC = Convert.ToInt32(nudAutoTOC.Value);
		o.DisplayCompileProgress = cbLogCompiledFilenames.Checked;
		o.DisplayCompileNotes = cbDisplayCompileNotes.Checked;
		o.Flat = cbFlattenFolders.Checked;
		o.SupportEnhancedDecompilation = cbSupportEnhancedDecompilation.Checked;
	}

	public void InitFields(Settings s, List<String> allFiles) {
		tbOutputFolder.Text = s.OutputFolder;
		tbCompilerExePath.Text = SharedSettings.Default.HHCExePath;
		cbDeleteTempFiles.Checked = s.DeleteTempFiles;
		cbOpenAfterCompile.Checked = s.OpenAfterCompile;
		cbReplaceFontSizes.Checked = s.ReplaceFontSizes;
		cbOverwriteExistingCHM.Checked = s.OverwriteExistingCHM;
		cbWordVisible.Checked = s.WordVisible;
		cbCreateCSharpFile.Checked = s.CreateCSharpFile;
		tbCSharpFilename.Text = s.CSharpFilename;
		tbCSharpNamespace.Text = s.CSharpNamespace;
		nudFileLockedTimeoutSeconds.Value = Convert.ToDecimal(s.HtmlFileLockedTimeoutMillis / 1000.0);
		cbOverrideHTags.Checked = s.OverrideHTags;
		comboHTagSize.SelectedItem = s.HTagSize;
		comboMaxNestedHeader.SelectedItem = s.MaxNestedHeader;
		cbBlankHeadersShowWarning.Checked = s.ShowBlankHeadersWarning;
		comboBlankHeaders.SelectedItem = s.BlankHeaders;
		comboKeywordsDelim.Text = s.KeywordsDelim;

		var o = s.CHMOptions;
		tbTitle.Text = o.Title;
		tbCHMFilename.Text = o.CompiledFile;
		comboLanguage.SelectedItem = o.Language;
		comboCompatibility.SelectedItem = o.Compatibility;
		tbErrorLogFile.Text = o.ErrorLogFile;
		tbStopWordsFile.Text = o.FullTextSearchStopListFile;
		cbBinaryIndex.Checked = o.BinaryIndex;
		cbBinaryTOC.Checked = o.BinaryTOC;
		cbFullTextSearch.Checked = o.FullTextSearch;
		cbAutoIndex.Checked = o.AutoIndex;
		nudAutoTOC.Value = o.AutoTOC;
		cbLogCompiledFilenames.Checked = o.DisplayCompileProgress;
		cbDisplayCompileNotes.Checked = o.DisplayCompileNotes;
		cbFlattenFolders.Checked = o.Flat;
		cbSupportEnhancedDecompilation.Checked = o.SupportEnhancedDecompilation;

		InitFileCombo(comboDefaultPage, o.DefaultTopic, allFiles);
	}


	public static void InitFileCombo(ComboBox3<String> combo, String currentDefaultFile, List<String> filenames) {
		combo.Clear();

		List<String> list2 = new List<String>();
		foreach (String s in filenames) {
			if (String.IsNullOrEmpty(s))
				continue;
			list2.Add(Path.GetFileName(s));
		}

		int count = list2.RemoveAll(s => String.Compare(s, currentDefaultFile, true) == 0);
		if (!String.IsNullOrEmpty(currentDefaultFile))
			list2.Add(currentDefaultFile);
		list2.Sort();

		combo.Add("");
		combo.AddRange(list2);
		combo.Text = currentDefaultFile;
		combo.SelectedItem = currentDefaultFile;
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
		if (disposing) {
			if (dialogOpenFile != null) {
				dialogOpenFile.Dispose();
				dialogOpenFile = null;
			}
		}
	}

	public static List<String> GetLanguages() {
		List<String> list = new List<String>();
		list.Add("0x0436 Afrikaans - South Africa");
		list.Add("0x041c Albanian - Albania");
		list.Add("0x0484 Alsatian");
		list.Add("0x045e Amharic - Ethiopia");
		list.Add("0x0401 Arabic - Saudi Arabia");
		list.Add("0x1401 Arabic - Algeria");
		list.Add("0x3c01 Arabic - Bahrain");
		list.Add("0x0c01 Arabic - Egypt");
		list.Add("0x0801 Arabic - Iraq");
		list.Add("0x2c01 Arabic - Jordan");
		list.Add("0x3401 Arabic - Kuwait");
		list.Add("0x3001 Arabic - Lebanon");
		list.Add("0x1001 Arabic - Libya");
		list.Add("0x1801 Arabic - Morocco");
		list.Add("0x2001 Arabic - Oman");
		list.Add("0x4001 Arabic - Qatar");
		list.Add("0x2801 Arabic - Syria");
		list.Add("0x1c01 Arabic - Tunisia");
		list.Add("0x3801 Arabic - U.A.E.");
		list.Add("0x2401 Arabic - Yemen");
		list.Add("0x042b Armenian - Armenia");
		list.Add("0x044d Assamese");
		list.Add("0x082c Azeri (Cyrillic)");
		list.Add("0x042c Azeri (Latin)");
		list.Add("0x046d Bashkir");
		list.Add("0x042d Basque");
		list.Add("0x0423 Belarusian");
		list.Add("0x0445 Bengali (India)");
		list.Add("0x0845 Bengali (Bangladesh)");
		list.Add("0x141A Bosnian (Bosnia/Herzegovina)");
		list.Add("0x047e Breton");
		list.Add("0x0402 Bulgarian");
		list.Add("0x0455 Burmese");
		list.Add("0x0403 Catalan");
		list.Add("0x045c Cherokee - United States");
		list.Add("0x0804 Chinese - People's Republic of China");
		list.Add("0x1004 Chinese - Singapore");
		list.Add("0x0404 Chinese - Taiwan");
		list.Add("0x0c04 Chinese - Hong Kong SAR");
		list.Add("0x1404 Chinese - Macao SAR");
		list.Add("0x0483 Corsican");
		list.Add("0x041a Croatian");
		list.Add("0x101a Croatian (Bosnia/Herzegovina)");
		list.Add("0x0405 Czech");
		list.Add("0x0406 Danish");
		list.Add("0x048c Dari");
		list.Add("0x0465 Divehi");
		list.Add("0x0413 Dutch - Netherlands");
		list.Add("0x0813 Dutch - Belgium");
		list.Add("0x0466 Edo");
		list.Add("0x0409 English - United States");
		list.Add("0x0809 English - United Kingdom");
		list.Add("0x0c09 English - Australia");
		list.Add("0x2809 English - Belize");
		list.Add("0x1009 English - Canada");
		list.Add("0x2409 English - Caribbean");
		list.Add("0x3c09 English - Hong Kong SAR");
		list.Add("0x4009 English - India");
		list.Add("0x3809 English - Indonesia");
		list.Add("0x1809 English - Ireland");
		list.Add("0x2009 English - Jamaica");
		list.Add("0x4409 English - Malaysia");
		list.Add("0x1409 English - New Zealand");
		list.Add("0x3409 English - Philippines");
		list.Add("0x4809 English - Singapore");
		list.Add("0x1c09 English - South Africa");
		list.Add("0x2c09 English - Trinidad");
		list.Add("0x3009 English - Zimbabwe");
		list.Add("0x0425 Estonian");
		list.Add("0x0438 Faroese");
		list.Add("0x0429 Farsi");
		list.Add("0x0464 Filipino");
		list.Add("0x040b Finnish");
		list.Add("0x040c French - France");
		list.Add("0x080c French - Belgium");
		list.Add("0x2c0c French - Cameroon");
		list.Add("0x0c0c French - Canada");
		list.Add("0x240c French - Democratic Rep. of Congo");
		list.Add("0x300c French - Cote d'Ivoire");
		list.Add("0x3c0c French - Haiti");
		list.Add("0x140c French - Luxembourg");
		list.Add("0x340c French - Mali");
		list.Add("0x180c French - Monaco");
		list.Add("0x380c French - Morocco");
		list.Add("0xe40c French - North Africa");
		list.Add("0x200c French - Reunion");
		list.Add("0x280c French - Senegal");
		list.Add("0x100c French - Switzerland");
		list.Add("0x1c0c French - West Indies");
		list.Add("0x0462 Frisian - Netherlands");
		list.Add("0x0467 Fulfulde - Nigeria");
		list.Add("0x042f FYRO Macedonian");
		list.Add("0x0456 Galician");
		list.Add("0x0437 Georgian");
		list.Add("0x0407 German - Germany");
		list.Add("0x0c07 German - Austria");
		list.Add("0x1407 German - Liechtenstein");
		list.Add("0x1007 German - Luxembourg");
		list.Add("0x0807 German - Switzerland");
		list.Add("0x0408 Greek");
		list.Add("0x046f Greenlandic");
		list.Add("0x0474 Guarani - Paraguay");
		list.Add("0x0447 Gujarati");
		list.Add("0x0468 Hausa - Nigeria");
		list.Add("0x0475 Hawaiian - United States");
		list.Add("0x040d Hebrew");
		list.Add("0x0439 Hindi");
		list.Add("0x040e Hungarian");
		list.Add("0x0469 Ibibio - Nigeria");
		list.Add("0x040f Icelandic");
		list.Add("0x0470 Igbo - Nigeria");
		list.Add("0x0421 Indonesian");
		list.Add("0x045d Inuktitut");
		list.Add("0x083c Irish");
		list.Add("0x0410 Italian - Italy");
		list.Add("0x0810 Italian - Switzerland");
		list.Add("0x0411 Japanese");
		list.Add("0x0486 K'iche");
		list.Add("0x044b Kannada");
		list.Add("0x0471 Kanuri - Nigeria");
		list.Add("0x0860 Kashmiri");
		list.Add("0x0460 Kashmiri (Arabic)");
		list.Add("0x043f Kazakh");
		list.Add("0x0453 Khmer");
		list.Add("0x0487 Kinyarwanda");
		list.Add("0x0457 Konkani");
		list.Add("0x0412 Korean");
		list.Add("0x0440 Kyrgyz (Cyrillic)");
		list.Add("0x0454 Lao");
		list.Add("0x0476 Latin");
		list.Add("0x0426 Latvian");
		list.Add("0x0427 Lithuanian");
		list.Add("0x046e Luxembourgish");
		list.Add("0x043e Malay - Malaysia");
		list.Add("0x083e Malay - Brunei Darussalam");
		list.Add("0x044c Malayalam");
		list.Add("0x043a Maltese");
		list.Add("0x0458 Manipuri");
		list.Add("0x0481 Maori - New Zealand");
		list.Add("0x0471 Mapudungun");
		list.Add("0x044e Marathi");
		list.Add("0x047c Mohawk");
		list.Add("0x0450 Mongolian (Cyrillic)");
		list.Add("0x0850 Mongolian (Mongolian)");
		list.Add("0x0461 Nepali");
		list.Add("0x0861 Nepali - India");
		list.Add("0x0414 Norwegian (Bokmål)");
		list.Add("0x0814 Norwegian (Nynorsk)");
		list.Add("0x0482 Occitan");
		list.Add("0x0448 Oriya");
		list.Add("0x0472 Oromo");
		list.Add("0x0479 Papiamentu");
		list.Add("0x0463 Pashto");
		list.Add("0x0415 Polish");
		list.Add("0x0416 Portuguese - Brazil");
		list.Add("0x0816 Portuguese - Portugal");
		list.Add("0x0446 Punjabi");
		list.Add("0x0846 Punjabi (Pakistan)");
		list.Add("0x046B Quecha - Bolivia");
		list.Add("0x086B Quecha - Ecuador");
		list.Add("0x0C6B Quecha - Peru");
		list.Add("0x0417 Rhaeto-Romanic");
		list.Add("0x0418 Romanian");
		list.Add("0x0818 Romanian - Moldava");
		list.Add("0x0419 Russian");
		list.Add("0x0819 Russian - Moldava");
		list.Add("0x043b Sami (Lappish)");
		list.Add("0x044f Sanskrit");
		list.Add("0x043c Scottish Gaelic");
		list.Add("0x046c Sepedi");
		list.Add("0x0c1a Serbian (Cyrillic)");
		list.Add("0x081a Serbian (Latin)");
		list.Add("0x0459 Sindhi - India");
		list.Add("0x0859 Sindhi - Pakistan");
		list.Add("0x045b Sinhalese - Sri Lanka");
		list.Add("0x041b Slovak");
		list.Add("0x0424 Slovenian");
		list.Add("0x0477 Somali");
		list.Add("0x042e Sorbian");
		list.Add("0x0c0a Spanish - Spain (Modern Sort)");
		list.Add("0x040a Spanish - Spain (Traditional Sort)");
		list.Add("0x2c0a Spanish - Argentina");
		list.Add("0x400a Spanish - Bolivia");
		list.Add("0x340a Spanish - Chile");
		list.Add("0x240a Spanish - Colombia");
		list.Add("0x140a Spanish - Costa Rica");
		list.Add("0x1c0a Spanish - Dominican Republic");
		list.Add("0x300a Spanish - Ecuador");
		list.Add("0x440a Spanish - El Salvador");
		list.Add("0x100a Spanish - Guatemala");
		list.Add("0x480a Spanish - Honduras");
		list.Add("0x580a Spanish - Latin America");
		list.Add("0x080a Spanish - Mexico");
		list.Add("0x4c0a Spanish - Nicaragua");
		list.Add("0x180a Spanish - Panama");
		list.Add("0x3c0a Spanish - Paraguay");
		list.Add("0x280a Spanish - Peru");
		list.Add("0x500a Spanish - Puerto Rico");
		list.Add("0x540a Spanish - United States");
		list.Add("0x380a Spanish - Uruguay");
		list.Add("0x200a Spanish - Venezuela");
		list.Add("0x0430 Sutu");
		list.Add("0x0441 Swahili");
		list.Add("0x041d Swedish");
		list.Add("0x081d Swedish - Finland");
		list.Add("0x045a Syriac");
		list.Add("0x0428 Tajik");
		list.Add("0x045f Tamazight (Arabic)");
		list.Add("0x085f Tamazight (Latin)");
		list.Add("0x0449 Tamil");
		list.Add("0x0444 Tatar");
		list.Add("0x044a Telugu");
		list.Add("0x041e Thai");
		list.Add("0x0851 Tibetan - Bhutan");
		list.Add("0x0451 Tibetan - People's Republic of China");
		list.Add("0x0873 Tigrigna - Eritrea");
		list.Add("0x0473 Tigrigna - Ethiopia");
		list.Add("0x0431 Tsonga");
		list.Add("0x0432 Tswana");
		list.Add("0x041f Turkish");
		list.Add("0x0442 Turkmen");
		list.Add("0x0480 Uighur - China");
		list.Add("0x0422 Ukrainian");
		list.Add("0x0420 Urdu");
		list.Add("0x0820 Urdu - India");
		list.Add("0x0843 Uzbek (Cyrillic)");
		list.Add("0x0443 Uzbek (Latin)");
		list.Add("0x0433 Venda");
		list.Add("0x042a Vietnamese");
		list.Add("0x0452 Welsh");
		list.Add("0x0488 Wolof");
		list.Add("0x0434 Xhosa");
		list.Add("0x0485 Yakut");
		list.Add("0x0478 Yi");
		list.Add("0x043d Yiddish");
		list.Add("0x046a Yoruba");
		list.Add("0x0435 Zulu");
		list.Add("0x04ff HID (Human Interface Device)");
		return list;
	}

}
}
