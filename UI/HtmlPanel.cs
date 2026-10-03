using System;
using System.Windows.Forms;

using Opulos.NüHelp.CHM;
using Opulos.Core.Utils;
using Opulos.Core.UI;

namespace Opulos.NüHelp {

public class HtmlPanel : Pane {

	private static readonly String LB_HEADER = "HTML Header {0}:";
	private static readonly String LB_FOOTER = "HTML Footer {0}:";

	RadioButton rbFirst = new RadioButton() { AutoSize = true, Text = "First Page" };
	RadioButton rbMiddle = new RadioButton() { AutoSize = true, Text = "Middle Pages" };
	RadioButton rbLast = new RadioButton() { AutoSize = true, Text = "Last Page" };
	CheckBox cbUseMiddleAlways = new CheckBox() { AutoSize = true, Text = "Use Middle Always" };

	Label lbHeaderHtml = new Label() { Text = "HTML Header Code:" };
	Label lbFooterHtml = new Label() { Text = "HTML Footer Code:" };
	RichTextBoxControl tbFooterHtml = new RichTextBoxControl() { Multiline = true, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both };
	RichTextBoxControl tbHeaderHtml = new RichTextBoxControl() { Multiline = true, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both };
	RichTextBoxControl tbFooterHtmlFirst = new RichTextBoxControl() { Multiline = true, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both, Visible = false };
	RichTextBoxControl tbHeaderHtmlFirst = new RichTextBoxControl() { Multiline = true, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both, Visible = false };
	RichTextBoxControl tbFooterHtmlLast = new RichTextBoxControl() { Multiline = true, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both, Visible = false };
	RichTextBoxControl tbHeaderHtmlLast = new RichTextBoxControl() { Multiline = true, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both, Visible = false };

	Label lbBodyTag = new Label() { Text = "HTML Body Tag:" };
	RichTextBoxControl tbBodyTag = new RichTextBoxControl() { Multiline = true, WordWrap = false, ScrollBars = RichTextBoxScrollBars.Both };

	public HtmlPanel() {

		RCLayout r0 = new RCLayout(1);
		setLayout(r0);
		r0.OverallInsets = new Insets(10);

		foreach (var tb in new [] { tbHeaderHtml, tbHeaderHtmlFirst, tbHeaderHtmlLast }) {
			r0[tb].cover = new RCCover(0, 2, 1, 1);
			r0[tb].fillX = 1;
			r0[tb].fillY = 1;
		}

		foreach (var tb in new [] { tbFooterHtml, tbFooterHtmlFirst, tbFooterHtmlLast }) {
			r0[tb].cover = new RCCover(0, 4, 1, 1);
			r0[tb].fillX = 1;
			r0[tb].fillY = 1;
		}

		r0[tbBodyTag].fillX = 1;
		r0[tbBodyTag].fillY = 1;
		
		r0.Col[0].expand = 1;
		r0.Col[0].shrink = 1;
		r0.Row[2].shrink = r0.Row[2].expand = 0.35;
		r0.Row[4].shrink = r0.Row[4].expand = 0.35;
		r0.Row[6].shrink = r0.Row[6].expand = 0.3;

		add(new Pane(rbFirst, rbMiddle, rbLast, cbUseMiddleAlways));
		add(lbHeaderHtml);
		add(tbHeaderHtml, tbHeaderHtmlFirst, tbHeaderHtmlLast);
		add(lbFooterHtml);
		add(tbFooterHtml, tbFooterHtmlFirst, tbFooterHtmlLast);
		add(lbBodyTag);
		add(tbBodyTag);
		//---

		rbFirst.CheckedChanged += rbCheckedChanged;
		rbLast.CheckedChanged += rbCheckedChanged;
		rbMiddle.CheckedChanged += rbCheckedChanged;

		rbMiddle.Checked = true; // leave after event listener is added
	}

	void rbCheckedChanged(Object src, EventArgs e) {
		lbFooterHtml.Text = String.Format(LB_FOOTER, ((Control) src).Text);
		lbHeaderHtml.Text = String.Format(LB_HEADER, ((Control) src).Text);

		if (rbFirst.Checked) {
			tbFooterHtmlFirst.Visible = true;
			tbHeaderHtmlFirst.Visible = true;
			tbFooterHtml.Visible = false;
			tbHeaderHtml.Visible = false;
			tbFooterHtmlLast.Visible = false;
			tbHeaderHtmlLast.Visible = false;
		}
		else if (rbLast.Checked) {
			tbFooterHtmlFirst.Visible = false;
			tbHeaderHtmlFirst.Visible = false;
			tbFooterHtml.Visible = false;
			tbHeaderHtml.Visible = false;
			tbFooterHtmlLast.Visible = true;
			tbHeaderHtmlLast.Visible = true;
		}
		else if (rbMiddle.Checked) {
			tbFooterHtmlFirst.Visible = false;
			tbHeaderHtmlFirst.Visible = false;
			tbFooterHtml.Visible = true;
			tbHeaderHtml.Visible = true;
			tbFooterHtmlLast.Visible = false;
			tbHeaderHtmlLast.Visible = false;
		}
	}

	public void InitFields(Settings s) {

		tbHeaderHtml.Text = s.HtmlHeader;
		tbHeaderHtmlFirst.Text = s.HtmlHeaderFirstPage;
		tbHeaderHtmlLast.Text = s.HtmlHeaderLastPage;

		tbFooterHtml.Text = s.HtmlFooter;
		tbFooterHtmlFirst.Text = s.HtmlFooterFirstPage;
		tbFooterHtmlLast.Text = s.HtmlFooterLastPage;

		cbUseMiddleAlways.Checked = s.UseMiddleHeaderFooterAlways;

		tbBodyTag.Text = s.HtmlBodyOpenTag;
	}

	public void ApplyValues(Settings s) {
		s.HtmlHeader = tbHeaderHtml.Text;
		s.HtmlHeaderFirstPage = tbHeaderHtmlFirst.Text;
		s.HtmlHeaderLastPage = tbHeaderHtmlLast.Text;

		s.HtmlFooter = tbFooterHtml.Text;
		s.HtmlFooterFirstPage = tbFooterHtmlFirst.Text;
		s.HtmlFooterLastPage = tbFooterHtmlLast.Text;

		s.UseMiddleHeaderFooterAlways = cbUseMiddleAlways.Checked;

		s.HtmlBodyOpenTag = tbBodyTag.Text;
	}

}



}