using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Opulos.Core.UI;
using System.Windows.Forms;
using Opulos.NüHelp.CHM;

namespace Opulos.NüHelp {
public class AdvancedOptionsPanel : Pane {

private const String tipHHPAppendText = 
@"This text is appended to the hhp file just before hhc.exe is called to compile.
The appended text may contain additional sections, such as ""[ALIAS]"", ""[MAP]"", 
""[TEXT POPUPS]"", ""[INFOTYPES]"" and ""[SUBSETS]""";

private const String tipPrecompileProcess =
@"This process is called just before the the hhc.exe is invoked.
It can be useful to process any files that are part of the HHP Append Text.";

private const String tipPrecompileArgs =
@"The arguments passed to the pre-compile process executable.";

private const String tipTokenFile =
@"If specified, a text file will be created (or overwritten) that contains
a delimited list of the tokens values.  Each line of the text file
will contain tokens from a [CHM ...] header defined in the Word document.
E.g. C:\temp\tokens.txt
";

private const String tipTokens =
@"A list of Tokens, separated by a split character, to output to the Token File.
Literals can also be included, surrounded by quotation marks.

E.g. Level, Title, "" = "", Local, TopicId, AliasName, etc.

Level/Title/Local are built-in.
TopicId/AliasName would have to exist in the [CHM ... ] header text in the Word doc.

Built-in special tokens:
  Level (depth of node)
  Title (header title)
  Local (html filename no path)
  Anchors (a row is added for each bookmark in the node*)
  IndexEntries (a row is added for each index entry in the node*)
  IndexEntries.<prop> (same as above, except use the index's <prop>** value)

*using these special tokens turns the node:row relationship from 1:1 to 1:n where n is
the maximum of (1, #Anchors, #IndexEntries).  There will always be at least one row.

**<prop> can be one of: Name, DisplayName, UncleanName, Text, SeeAlso, Bookmark,
Categories, or any of the attribute names defined in the index curly brackets.
";

private const String tipQuoteMode =
@"Normal means only use quotation characters if needed.  They are needed if
the value contains a newline, quotation or delimter character.

Always means use quotation characters around all values.  E.g. ""1"",""2""

Never means never use quotation characters.

NormalNeverLiterals is same as Normal, except literal tokens are never
quoted.";


	ToolTips tips = new ToolTips();

	GroupBox2 gbHHPAppendText = null;
	RichTextBoxControl tbHHPAppendText = new RichTextBoxControl { Multiline = true };

	Label lbPrecompileProcess = new Label { Text = "Pre-compile process" };
	TextBox tbPrecompileProcess = new TextBox();

	Label lbPrecompileProcessArgs = new Label { Text = "Process Arguments" };
	TextBox tbPrecompileProcessArgs = new TextBox();

	GroupBox gbOutput = null;


	Label lbTokensFile = new Label { Text = "Filename" };
	TextBox tbTokensFile = new TextBox();

	Label lbTokens = new Label { Text = "Tokens" };
	TextBox tbTokens = new TextBox();

	Label lbTokenSplitChar = new Label { Text = "Token Split Char" };
	ComboBox comboTokenSplitChar = new ComboBox2(new String[] { ",", "|", "~" });

	Label lbTokenLiteralChar = new Label { Text = "Token Literal Char" };
	ComboBox comboTokenLiteralChar = new ComboBox2(new Object[] { '"', '\'' });

	Label lbOutputEncoding = new Label { Text = "Encoding" };
	EncodingComboBox comboOutputEncoding = new EncodingComboBox(false);

	Label lbOutputDelim = new Label { Text = "Delim" };
	ComboBox comboOutputDelim = new ComboBox2(new String[] { ",", "|", "<tab>", "<space>", "<none>" });

	Label lbOutputNewline = new Label { Text = "Newline" };
	ComboBox comboOutputNewline = new ComboBox2(new String[] { "\\n", "\\r\\n" });

	Label lbOutputQuoteChar = new Label { Text = "Quote Char" };
	ComboBox comboOutputQuoteChar = new ComboBox2(new Object[] { '"', '\'' });
	Label lbQuoteMode = new Label() { Text = "Quote Mode" };
	ComboBox3<QuoteMode> comboQuoteMode = new ComboBox3<QuoteMode>() { DropDownStyle = ComboBoxStyle.DropDownList };
	CheckBox cbOutputHeaderRow = new CheckBox { Text = "Header Row" };
	CheckBox cbIncludeEmptyRows = new CheckBox { Text = "Include Empty Rows" };
	Button btnPreview = new Button() { Text = "Preview..." };

	public AdvancedOptionsPanel() {
		comboQuoteMode.AddRange(Enum.GetValues(typeof(QuoteMode)).Cast<QuoteMode>().ToList());

		gbHHPAppendText = new GroupBox2(tbHHPAppendText, "HHP Append Text");

		tips.Add(gbHHPAppendText, tipHHPAppendText);
		tips.Add(tbPrecompileProcess, tipPrecompileProcess);
		tips.Add(tbPrecompileProcessArgs, tipPrecompileArgs);
		tips.Add(tbTokensFile, tipTokenFile);
		tips.Add(tbTokens, tipTokens);
		tips.Add(comboOutputEncoding, "The encoding the output text file is saved.");
		tips.Add(comboOutputDelim, "The character used in the output file to delimit the columns.");
		tips.Add(comboOutputNewline, "The newline character to use in the output file.\nTypically Windows uses \\r\\n");
		tips.Add(comboOutputQuoteChar, "The character to use if a value contains a reserved character.");
		tips.Add(comboQuoteMode, tipQuoteMode);
		tips.Add(cbIncludeEmptyRows, "An option to include rows that have no associated token values.");
		tips.Add(comboTokenSplitChar, "The character you want to use to split the tokens above.");
		tips.Add(comboTokenLiteralChar, "The character used to determine to treat a token as a literal string.");
		tips.Add(cbOutputHeaderRow, "Output the tokens specified at the top of the file.");
		tips.Add(btnPreview, "Preview how the token file will look.");

		RCLayout r1 = new RCLayout(2);
		r1.Col[1].expand = 1;
		r1.Col[1].shrink = 1;
		r1.Col[1].fillX = 1;
		var p1 = new Pane(r1);
		p1.add(lbPrecompileProcess, tbPrecompileProcess);
		p1.add(lbPrecompileProcessArgs, tbPrecompileProcessArgs);

		RCLayout r2 = new RCLayout(2);
		r2.Col[1].expand = 1;
		r2.Col[1].shrink = 1;
		r2.Col[1].fillX = 1;
		Pane p2 = new Pane(r2);
		p2.add(lbTokensFile, tbTokensFile);
		p2.add(lbOutputEncoding, comboOutputEncoding);

		Pane p3 = new Pane(new RCLayout(4));
		p3.add(new Pane(lbOutputDelim, comboOutputDelim), new Pane(lbOutputQuoteChar, comboOutputQuoteChar), new Pane(lbQuoteMode, comboQuoteMode), new Panel());
		p3.add(new Pane(lbOutputNewline, comboOutputNewline), cbOutputHeaderRow, cbIncludeEmptyRows, btnPreview);
		GroupBox gb = new GroupBox2(p3, "More Options");
		p2.add(new Panel(), gb);

		p2.add(lbTokens, tbTokens);
		p2.add(new Panel(), new Pane(lbTokenSplitChar, comboTokenSplitChar, lbTokenLiteralChar, comboTokenLiteralChar));

		gbOutput = new GroupBox2(p2, "Output Tokens File");


		RCLayout r0 = new RCLayout(1);
		r0[p1].fillX = 1;
		r0[gbOutput].fillX = 1;
		r0[gbOutput].fillY = 1;
		setLayout(r0);
		r0.esf(gbHHPAppendText);
		add(gbHHPAppendText);
		add(p1);
		add(gbOutput);

		btnPreview.Click += delegate {
			List<CHMNode> list = new List<CHMNode>();
			list.Add(CreateNode(1, "a.html", "Topic A"));
			list.Add(CreateNode(2, "b.html", "Topic B"));

			AdvancedOptions ao = new AdvancedOptions();
			ApplyValues(ao);
			String s = CHMDocument.TokensToString(ao, list);
			var rtbc = new RichTextBoxControl(s, "012345679012345679012345679012345679012345679012345679\n\n\n\n\n");
			rtbc.ReadOnly = true;
			Dialog<RichTextBoxControl> d = new Dialog<RichTextBoxControl>(rtbc, MessageBoxButtons.OK);
			d.Owner = this.FindForm();
			d.Text = "Preview Tokens File";
			d.ShowDialog();
			d.Dispose();
		};
	}

	private static CHMNode CreateNode(int Level, String Local, String Name) {
		CHMNode n = new CHMNode();
		n.Tokens["Level"] = Level;
		n.Tokens["Local"] = Local;
		n.Tokens["Name"] = Name;
		n.Anchors.Add(new CHMAnchor { Anchor = "bookmark" + Level });
		n.IndexEntries.Add(new CHMIndexEntry { Bookmark = "_bm" + Level, Categories = "cat" + Level, Text = "index" + Level });
		return n;
	}


	public void InitFields(Settings s) {
		var o = s.CHMOptions;
		tbHHPAppendText.Text = o.HHPAppendText;

		var ao = s.AdvancedOptions;
		tbPrecompileProcess.Text = ao.PrecompileProcess;
		tbPrecompileProcessArgs.Text = ao.PrecompileProcessArguments;
		tbTokensFile.Text = ao.OutputTokensFilename;
		tbTokens.Text = ao.OutputTokens;
		comboOutputEncoding.SelectedItem = ao.OutputEncodingCodePage;
		comboOutputDelim.Text = ao.OutputDelimChar;
		comboOutputNewline.Text = ao.OutputNewline;
		comboOutputQuoteChar.Text = ao.OutputQuoteChar;
		comboQuoteMode.SelectedItem = ao.QuoteMode;
		cbIncludeEmptyRows.Checked = ao.IncludeEmptyRows;
		cbOutputHeaderRow.Checked = ao.OutputHeaderRow;
		comboTokenSplitChar.Text = ao.TokenSplitChar;
		comboTokenLiteralChar.Text = ao.TokenLiteralChar;
	}

	public void ApplyValues(Settings s) {
		var o = s.CHMOptions;
		o.HHPAppendText = tbHHPAppendText.Text;
		ApplyValues(s.AdvancedOptions);
	}

	public void ApplyValues(AdvancedOptions ao) {
		ao.PrecompileProcess = tbPrecompileProcess.Text;
		ao.PrecompileProcessArguments = tbPrecompileProcessArgs.Text;
		ao.OutputTokensFilename = tbTokensFile.Text;
		ao.OutputTokens = tbTokens.Text;
		ao.OutputEncodingCodePage = comboOutputEncoding.CodePage;
		ao.OutputDelimChar = comboOutputDelim.Text;
		ao.OutputNewline = comboOutputNewline.Text;
		ao.OutputQuoteChar = comboOutputQuoteChar.Text;
		ao.QuoteMode = comboQuoteMode.SelectedItem;
		ao.OutputHeaderRow = cbOutputHeaderRow.Checked;
		ao.IncludeEmptyRows = cbIncludeEmptyRows.Checked;
		ao.TokenSplitChar = comboTokenSplitChar.Text;
		ao.TokenLiteralChar = comboTokenLiteralChar.Text;
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
		if (disposing) {
			if (tips != null) {
				tips.Dispose();
				tips = null;
			}
		}
	}

}
}
