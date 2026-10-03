using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Opulos.Core.Utils;
using Opulos.Core.UI;
using Opulos.Core.Win32;

namespace Opulos.NüHelp {

public class FontsPanel : Pane {

	FontDialog dialogFont = null;
	ColorDialog dialogColor = null;

	Label lbTOCFont = new Label() { Text = "TOC Font" };
	Label lbIndexFont = new Label() { Text = "Index Font" };
	Label lbDefaultFont = new Label() { Text = "Default Font" };

	TextBox tbTOCFont = new TextBox() { ReadOnly = true };
	TextBox tbIndexFont = new TextBox() { ReadOnly = true, BackColor = Color.White };
	TextBox tbDefaultFont = new TextBox() { ReadOnly = true, BackColor = Color.White };

	Button btnTOCFont;
	Button btnIndexFont;
	Button btnDefaultFont;

	ToolStripMenuItem miTOCForegroundColor = new ToolStripMenuItem("Foreground Color...");
	ToolStripMenuItem miTOCBackgroundColor = new ToolStripMenuItem("Background Color...");

	Label lbTOCCharset = new Label() { Text = "Charset" };
	Label lbIndexCharset = new Label() { Text = "Charset" };
	Label lbDefaultCharset = new Label() { Text = "Charset" };

	ComboBox comboTOCCharset = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 30 };
	ComboBox comboIndexCharset = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 30 };
	ComboBox comboDefaultCharset = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 30 };

	Font fntTOC = null;
	Font fntIndex = null;
	Font fntDefault = null;
	Color colorTOCText = Color.Black;
	Color colorTOCBackground = Color.White;

	Label lbTOCTreeViewStyles = new Label() { Text = "TOC Tree View Styles" };
	EnumFlagComboBox<TVS> comboTOCTreeViewStyles = new EnumFlagComboBox<TVS>();

	Label lbTOCWindowStyles = new Label() { Text = "TOC Window Styles" };
	EnumFlagComboBox<WS> comboTOCWindowStyles = new EnumFlagComboBox<WS>();
	Label lbTOCWindowExStyles = new Label() { Text = "TOC Window Extended Styles" };
	EnumFlagComboBox<WSEX> comboTOCWindowExStyles = new EnumFlagComboBox<WSEX>();

	Label lbTOCDefaultFolderImage = new Label() { Text = "TOC Default Folder Image", ImageAlign = ContentAlignment.MiddleRight };
	NumericUpDown nudTOCDefaultFolderImage = new NumericUpDown() { DecimalPlaces = 0, Minimum = 1, Maximum = 8 };

	Label lbTOCDefaultItemImage = new Label() { Text = "TOC Default Item Image", ImageAlign = ContentAlignment.MiddleRight };
	NumericUpDown nudTOCDefaultItemImage = new NumericUpDown() { DecimalPlaces = 0, Minimum = 9, Maximum = 42 };

	Image[] images = new Image[42];

	public FontsPanel() {
		btnTOCFont = SB(miTOCForegroundColor, miTOCBackgroundColor);
		btnIndexFont = SB();
		btnDefaultFont = SB();

		Bitmap img = Properties.Resources.HelpIcons;
		for (int i = 0; i < images.Length; i++) {
			int x = i * 16;
			images[i] = CropImage(img, new Rectangle(x, 0, 16, 16));
		}

		lbTOCDefaultFolderImage.Image = images[0];
		lbTOCDefaultItemImage.Image = images[8];

		nudTOCDefaultFolderImage.ValueChanged += delegate {
			lbTOCDefaultFolderImage.Image = images[Convert.ToInt32(nudTOCDefaultFolderImage.Value) - 1];
		};
		nudTOCDefaultItemImage.ValueChanged += delegate {
			lbTOCDefaultItemImage.Image = images[Convert.ToInt32(nudTOCDefaultItemImage.Value) - 1];
		};

		RCLayout r1 = new RCLayout(2);
		r1[lbTOCDefaultFolderImage].fillX = 1;
		r1[lbTOCDefaultItemImage].fillX = 1;
		Pane p1 = new Pane(r1);
		p1.add(lbTOCTreeViewStyles, comboTOCTreeViewStyles);
		p1.add(lbTOCWindowStyles, comboTOCWindowStyles);
		p1.add(lbTOCWindowExStyles, comboTOCWindowExStyles);
		p1.add(lbTOCDefaultFolderImage, nudTOCDefaultFolderImage);
		p1.add(lbTOCDefaultItemImage, nudTOCDefaultItemImage);

		RCLayout r0 = new RCLayout(5);
		r0.Col[1].expand = 1;
		r0.Col[1].shrink = 1;
		r0[tbIndexFont].fillX = 1;
		r0[tbTOCFont].fillX = 1;
		r0[tbDefaultFont].fillX = 1;
		r0[p1].cover = new RCCover(5, 1, new double[] { 0, 0, 0, 0, 0}, null);
		r0.OverallInsets = new Insets(10);
		setLayout(r0);

		add(lbTOCFont, tbTOCFont, btnTOCFont, lbTOCCharset, comboTOCCharset);
		add(lbIndexFont, tbIndexFont, btnIndexFont, lbIndexCharset, comboIndexCharset);
		add(lbDefaultFont, tbDefaultFont, btnDefaultFont, lbDefaultCharset, comboDefaultCharset);
		add(p1);

		btnIndexFont.Click += btn_Click;
		btnTOCFont.Click += btn_Click;
		btnDefaultFont.Click += btn_Click;
		miTOCForegroundColor.Click += miChangeColor;
		miTOCBackgroundColor.Click += miChangeColor;

		GdiCharSet[] arr = (GdiCharSet[]) Enum.GetValues(typeof(GdiCharSet));
		arr = arr.OrderBy(g => Enum.GetName(typeof(GdiCharSet), g)).ToArray();
		Object[] arr2 = arr.Cast<Object>().ToArray();
		comboTOCCharset.Items.AddRange(arr2);
		comboIndexCharset.Items.AddRange(arr2);
		comboDefaultCharset.Items.AddRange(arr2);

		comboTOCCharset.SelectedItem = GdiCharSet.ANSI;
		comboIndexCharset.SelectedItem = GdiCharSet.ANSI;
		comboDefaultCharset.SelectedItem = GdiCharSet.ANSI;

		comboIndexCharset.SelectedIndexChanged += combo_SelectedIndexChanged;
		comboTOCCharset.SelectedIndexChanged += combo_SelectedIndexChanged;
		comboDefaultCharset.SelectedIndexChanged += combo_SelectedIndexChanged;
	}

	public static Bitmap CropImage(Image img, Rectangle cropRect) {
		Bitmap target = new Bitmap(cropRect.Width, cropRect.Height);

		using (Graphics g = Graphics.FromImage(target)) {
			g.DrawImage(img, new Rectangle(0, 0, target.Width, target.Height), cropRect, GraphicsUnit.Pixel);
		}
		return target;
	}

	void miChangeColor(object sender, EventArgs e) {
		if (dialogColor == null) {
			dialogColor = new ColorDialog();
			dialogColor.FullOpen = true;
			dialogColor.AnyColor = true; // enables the Display All Colors button
		}

		Color c = (sender == miTOCBackgroundColor ? colorTOCBackground : colorTOCText);
		dialogColor.Color = c;

		var r = dialogColor.ShowDialog(this.FindForm());
		if (r == DialogResult.OK) {
			c = dialogColor.Color;

			if (sender == miTOCBackgroundColor) {
				colorTOCBackground = c;
				tbTOCFont.BackColor = c;
			}
			else {
				colorTOCText = c;
				tbTOCFont.ForeColor = c;
			}
		}
	}

	private SplitButton SB(params ToolStripMenuItem[] items) {
		SplitButton btn = new SplitButton("Change...");
		ToolStripMenuItem mi = new ToolStripMenuItem("Clear");
		btn.DropDownMenu.Items.Add(mi);

		foreach (var mi2 in items)
			btn.DropDownMenu.Items.Add(mi2);

		mi.Click += delegate {
			if (btn == btnTOCFont)
				fntTOC = null;
			else if (btn == btnIndexFont)
				fntIndex = null;
			else if (btn == btnDefaultFont)
				fntDefault = null;

			updateTextBoxes();
		};
		return btn;
	}

	void combo_SelectedIndexChanged(object sender, EventArgs e) {
		updateTextBoxes();
	}

	void btn_Click(Object src, EventArgs e) {

		Font f = src == btnTOCFont ? fntTOC : src == btnIndexFont ? fntIndex : fntDefault;
		f = GetFont(f);
		if (f == null) return;

		if (src == btnTOCFont)
			fntTOC = f;
		else if (src == btnIndexFont)
			fntIndex = f;
		else if (src == btnDefaultFont)
			fntDefault = f;

		updateTextBoxes();
	}

	private void updateTextBoxes() {
		tbTOCFont.Text = ToString(fntTOC, (byte) comboTOCCharset.SelectedItem);
		tbIndexFont.Text = ToString(fntIndex, (byte) comboIndexCharset.SelectedItem);
		tbDefaultFont.Text = ToString(fntDefault, (byte) comboDefaultCharset.SelectedItem);

		tbTOCFont.ForeColor = colorTOCText;
		tbTOCFont.BackColor = colorTOCBackground;
	}

	private static String ToString(Font f, byte charset) {
		if (f == null)
			return "";
		String txt = f.FontFamily.Name + "," + (int) Math.Round(f.Size) + "," + charset;
		return txt;
	}

	public void InitFields(Settings s) {
		var o = s.CHMOptions;

		if (!String.IsNullOrEmpty(o.TOCFont.FamilyName) && o.TOCFont.Size > 0)
			fntTOC = FontEx.New(o.TOCFont.FamilyName, o.TOCFont.Size);
		else
			fntTOC = null;

		comboTOCCharset.SelectedItem = (GdiCharSet) o.TOCFont.CharSet;

		if (!String.IsNullOrEmpty(o.IndexFont.FamilyName) && o.IndexFont.Size > 0)
			fntIndex = FontEx.New(o.IndexFont.FamilyName, o.IndexFont.Size);
		else
			fntIndex = null;

		comboIndexCharset.SelectedItem = (GdiCharSet) o.IndexFont.CharSet;

		if (!String.IsNullOrEmpty(o.DefaultFont.FamilyName) && o.DefaultFont.Size > 0)
			fntDefault = FontEx.New(o.DefaultFont.FamilyName, o.DefaultFont.Size);
		else
			fntDefault = null;

		RGBOrder z = RGBOrder.BGR;
		comboDefaultCharset.SelectedItem = (GdiCharSet) o.DefaultFont.CharSet;
		if (String.IsNullOrEmpty(o.TOCForegroundColor))
			colorTOCText = Color.Black;
		else
			colorTOCText = ColorUtil.ToColor(o.TOCForegroundColor, z);

		if (String.IsNullOrEmpty(o.TOCBackgroundColor))
			colorTOCBackground = Color.White;
		else
			colorTOCBackground = ColorUtil.ToColor(o.TOCBackgroundColor, z);

		updateTextBoxes();

		comboTOCTreeViewStyles.EnumFlags = (TVS) o.TOCTreeViewStyles;
		comboTOCWindowExStyles.EnumFlags = (WSEX) o.TOCWindowExStyles;
		comboTOCWindowStyles.EnumFlags = (WS) o.TOCWindowStyles;

		setNUD(nudTOCDefaultFolderImage, o.TOCDefaultFolderImage);
		setNUD(nudTOCDefaultItemImage, o.TOCDefaultItemImage);
	}

	public static void setNUD(NumericUpDown nud, decimal value) {
		if (value < nud.Minimum)
			value = nud.Minimum;
		else if (value > nud.Maximum)
			value = nud.Maximum;

		nud.Value = value;
	}

	public void ApplyValues(Settings s) {
		var o = s.CHMOptions;

		if (fntTOC != null) {
			o.TOCFont.FamilyName = fntTOC.FontFamily.Name;
			o.TOCFont.Size = (int) Math.Round(fntTOC.Size);
			o.TOCFont.CharSet = (byte) comboTOCCharset.SelectedItem;
		}
		else {
			o.TOCFont.FamilyName = "";
		}

		if (fntIndex != null) {
			o.IndexFont.FamilyName = fntIndex.FontFamily.Name;
			o.IndexFont.Size = (int) Math.Round(fntIndex.Size);
			o.IndexFont.CharSet = (byte) comboIndexCharset.SelectedItem;
		}
		else {
			o.IndexFont.FamilyName = "";
		}

		if (fntDefault != null) {
			o.DefaultFont.FamilyName = fntDefault.FontFamily.Name;
			o.DefaultFont.Size = (int) Math.Round(fntDefault.Size);
			o.DefaultFont.CharSet = (byte) comboDefaultCharset.SelectedItem;
		}
		else {
			o.DefaultFont.FamilyName = "";
		}

		o.TOCTreeViewStyles = (int) comboTOCTreeViewStyles.EnumFlags;
		o.TOCWindowExStyles = (int) comboTOCWindowExStyles.EnumFlags;
		o.TOCWindowStyles = (int) comboTOCWindowStyles.EnumFlags;

		RGBOrder z = RGBOrder.BGR;
		String h1 = colorTOCText.ToHex("0x", z);
		String h2 = colorTOCBackground.ToHex("0x", z);
		o.TOCForegroundColor = (h1 == Color.Black.ToHex("0x", z) || h1 == Color.Empty.ToHex("0x", z) ? "" : h1);
		o.TOCBackgroundColor = (h2 == Color.White.ToHex("0x", z) ? "" : h2); // clear translates to 0x000000 which prevents black from working

		o.TOCDefaultFolderImage = Convert.ToInt32(nudTOCDefaultFolderImage.Value);
		o.TOCDefaultItemImage = Convert.ToInt32(nudTOCDefaultItemImage.Value);
	}

	public Font GetFont(Font initFont) {
		if (dialogFont == null) {
			dialogFont = new FontDialog();
			dialogFont.ShowApply = false;
			dialogFont.ShowEffects = false;
			dialogFont.ShowColor = false; // not possible to ShowColor without Effects
		}

		if (initFont != null)
			dialogFont.Font = initFont;

		DialogResult r = dialogFont.ShowDialog(this.FindForm());
		if (r == DialogResult.OK) {
			return dialogFont.Font;
		}
		return null;
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
		if (dialogFont != null) {
			dialogFont.Dispose();
			dialogFont = null;
		}
		if (dialogColor != null) {
			dialogColor.Dispose();
			dialogColor = null;
		}

		if (images != null) {
			foreach (Image img in images)
				img.Dispose();
			images = null;
		}
	}
}

}
