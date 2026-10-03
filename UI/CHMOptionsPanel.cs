using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Opulos.Core.Utils;
using Opulos.Core.UI;
using System.Drawing;

namespace Opulos.NüHelp {
public class CHMOptionsPanel : Panel, IHelpURL {

	TabControl tabControl = new TabControl2();
	TabPage tpOptions = new TP("Options", HelpConstants.Options);
	TabPage tpHtml = new TP("HTML", HelpConstants.HTML);
	TabPage tpFonts = new TP("Fonts && Styles", HelpConstants.Fonts);
	TabPage tpEncodings = new TP("Encodings", HelpConstants.Encodings);
	TP tpExtraFiles = new TP("Extra Files", HelpConstants.ExtraFiles);
	TabPage tpWindows = new TP("Windows", HelpConstants.Windows);
	TabPage tpAdvanced = new TP("Advanced", HelpConstants.AdvancedOptions);

	OptionsPanel panelOptions = new OptionsPanel() { Dock = DockStyle.Fill };
	HtmlPanel panelHtml = new HtmlPanel() { Dock = DockStyle.Fill };
	FontsPanel panelFonts = new FontsPanel() { Dock = DockStyle.Fill };
	EncodingsPanel panelEncodings = new EncodingsPanel() { Dock = DockStyle.Fill };
	ExtraFilesPanel panelExtraFiles = new ExtraFilesPanel() { Dock = DockStyle.Fill };
	WindowsPanel panelWindows = new WindowsPanel() { Dock = DockStyle.Fill };
	AdvancedOptionsPanel panelAdvanced = new AdvancedOptionsPanel() { Dock = DockStyle.Fill };

	private class TP : TabPage, IHelpURL {
		public TP(String name, String helpURL) : base(name) {
			HelpURL = helpURL;
		}

		public String HelpURL { get; private set; }
	}

	public String HelpURL {
		get {
			return HelpConstants.CHMOptions;
		}
	}

	public CHMOptionsPanel() {
		tpOptions.Controls.Add(panelOptions);
		tpHtml.Controls.Add(panelHtml);
		tpFonts.Controls.Add(panelFonts);
		tpEncodings.Controls.Add(panelEncodings);
		tpExtraFiles.Controls.Add(panelExtraFiles);
		tpWindows.Controls.Add(panelWindows);
		tpAdvanced.Controls.Add(panelAdvanced);

		tabControl.TabPages.Add(tpOptions);
		tabControl.TabPages.Add(tpHtml);
		tabControl.TabPages.Add(tpFonts);
		tabControl.TabPages.Add(tpEncodings);
		tabControl.TabPages.Add(tpExtraFiles);
		tabControl.TabPages.Add(tpWindows);
		tabControl.TabPages.Add(tpAdvanced);

		RCLayout r0 = new RCLayout(1);
		r0.esf(tabControl);
		Pane p0 = new Pane(r0);
		p0.add(tabControl);
		p0.Dock = DockStyle.Fill;
		Controls.Add(p0);
	}

	public override Size GetPreferredSize(Size proposedSize) {
		return tabControl.PreferredSize;
	}

	public void InitFields(Settings s, List<String> allFiles) {
		panelHtml.InitFields(s);
		panelEncodings.InitFields(s);
		panelExtraFiles.InitFields(s);
		panelFonts.InitFields(s);
		panelOptions.InitFields(s, allFiles);
		panelWindows.InitFields(s, allFiles);
		panelAdvanced.InitFields(s);
	}

	public void ApplyValues(Settings s) {
		panelHtml.ApplyValues(s);
		panelEncodings.ApplyValues(s);
		panelExtraFiles.ApplyValues(s);
		panelFonts.ApplyValues(s);
		panelOptions.ApplyValues(s);
		panelWindows.ApplyValues(s);
		panelAdvanced.ApplyValues(s);
	}

}
}
