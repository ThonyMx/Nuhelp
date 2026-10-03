using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Opulos.Core.UI;
using Opulos.Core.Win32;
using Opulos.NüHelp.CHM;

namespace Opulos.NüHelp {
public class CHMWindowDetailPanel : Pane {

	Label lbName = new Label() { Text = "Name / Handle" };
	TextBox tbName = new TextBox();

	Label lbCaption = new Label() { Text = "Window Caption" };
	TextBox tbCaption = new TextBox();

	Label lbButtons = new Label() { Text = "Buttons" };
	EnumFlagComboBox<CHMButton> comboButtons = new EnumFlagComboBox<CHMButton>();

	Label lbWinProps = new Label() { Text = "Window Properties" };
	EnumFlagComboBox<CHMWinProp> comboWinProps = new EnumFlagComboBox<CHMWinProp>();

	Label lbJumpButton1URL = new Label() { Text = "Jump Button 1 URL" };
	ComboBox3<String> comboJumpButton1URL = new ComboBox3<String>() { Enabled = false };

	Label lbJumpButton1Text = new Label() { Text = "Jump Button 1 Text" };
	TextBox tbJumpButton1Text = new TextBox() { Enabled = false };

	Label lbJumpButton2URL = new Label() { Text = "Jump Button 2 URL" };
	ComboBox3<String> comboJumpButton2URL = new ComboBox3<String>() { Enabled = false };

	Label lbJumpButton2Text = new Label() { Text = "Jump Button 2 Text" };
	TextBox tbJumpButton2Text = new TextBox() { Enabled = false };

	Label lbWindowBounds = new Label() { Text = "Window Bounds (X Y W H)" };
	NumericUpDown nudWindowX = new NumericUpDown() { DecimalPlaces = 0, Minimum = -9999, Maximum = 9999 };
	NumericUpDown nudWindowY = new NumericUpDown() { DecimalPlaces = 0, Minimum = -9999, Maximum = 9999 };
	NumericUpDown nudWindowW = new NumericUpDown() { DecimalPlaces = 0, Minimum = 0, Maximum = 9999 };
	NumericUpDown nudWindowH = new NumericUpDown() { DecimalPlaces = 0, Minimum = 0, Maximum = 9999 };

	Label lbDefaultPage = new Label() { Text = "Default Page" };
	ComboBox3<String> comboDefaultPage = new ComboBox3<String>() { DropDownStyle = ComboBoxStyle.DropDownList };

	Label lbHomePage = new Label() { Text = "Home Page" };
	ComboBox3<String> comboHomePage = new ComboBox3<String>();

	Label lbTOCWidth = new Label() { Text = "TOC Width" };
	NumericUpDown nudTOCWidth = new NumericUpDown() { Minimum = 0, Maximum = 9999, DecimalPlaces = 0 };

	Label lbWindowStyles = new Label() { Text = "Window Styles" };
	EnumFlagComboBox<WS> comboWindowStyles = new EnumFlagComboBox<WS>();
	Label lbWindowExStyles = new Label() { Text = "Window Extended Styles" };
	EnumFlagComboBox<WSEX> comboWindowExStyles = new EnumFlagComboBox<WSEX>();

	Label lbShowState = new Label() { Text = "Show State" };
	ComboBox3<SW> comboShowState = new ComboBox3<SW>(Enum.GetValues(typeof(SW)).Cast<SW>().ToArray()) { DropDownStyle = ComboBoxStyle.DropDownList };

	CheckBox cbTOCInitiallyHidden = new CheckBox() { Text = "TOC Initially Hidden" };

	Label lbTabPosition = new Label() { Text = "Tab Position" };
	ComboBox3<CHMTabPosition> comboTabPosition = new ComboBox3<CHMTabPosition>() { DropDownStyle = ComboBoxStyle.DropDownList };

	Label lbNotifyID = new Label() { Text = "Notify ID" };
	NumericUpDown nudNotifyID = new NumericUpDown() { DecimalPlaces = 0, Maximum = 999999, Minimum = 0 };

	CHMWindow current;

	public CHMWindowDetailPanel() {

		CHMTabPosition[] tabPos = (CHMTabPosition[]) Enum.GetValues(typeof(CHMTabPosition));
		comboTabPosition.AddRange(tabPos);
		comboTabPosition.SelectedItem = CHMTabPosition.Top;

		RCLayout r0 = new RCLayout(2);
		r0.Col[1].shrink = 1;
		r0.Col[1].fillX = 1;
		r0[comboTabPosition].fillX = 0;
		r0[comboButtons].fillX = 0;
		r0[nudTOCWidth].fillX = 0;
		r0[nudNotifyID].fillX = 0;

		r0.OverallInsets = new Insets(10);
		setLayout(r0);

		add(lbName, tbName);
		add(lbCaption, tbCaption);
		add(lbDefaultPage, comboDefaultPage);
		add(lbHomePage, comboHomePage);
		add(lbWinProps, comboWinProps);
		add(lbWindowBounds, new Pane(nudWindowX, nudWindowY, nudWindowW, nudWindowH));
		add(lbTOCWidth, nudTOCWidth);
		add(lbTabPosition, comboTabPosition);
		add(lbButtons, comboButtons);
		add(lbJumpButton1Text, tbJumpButton1Text);
		add(lbJumpButton1URL, comboJumpButton1URL);
		add(lbJumpButton2Text, tbJumpButton2Text);
		add(lbJumpButton2URL, comboJumpButton2URL);
		add(cbTOCInitiallyHidden, new Control());
		add(lbShowState, comboShowState);
		add(lbWindowStyles, comboWindowStyles);
		add(lbWindowExStyles, comboWindowExStyles);
		add(lbNotifyID, nudNotifyID);

		comboButtons.CheckStateChanged += comboButtons_CheckedChanged;
	}

	void comboButtons_CheckedChanged(Object sender, ItemCheckEventArgs e) {
		if (this.ReadOnly)
			return;

		bool b1 = comboButtons.CheckedItems.Contains("" + CHMButton.Jump1);
		bool b2 = comboButtons.CheckedItems.Contains("" + CHMButton.Jump2);
		tbJumpButton1Text.Enabled = b1;
		comboJumpButton1URL.Enabled = b1;
		tbJumpButton2Text.Enabled = b2;
		comboJumpButton2URL.Enabled = b2;
	}

	private bool _readOnly = false;
	public bool ReadOnly {
		get {
			return _readOnly;
		}
		set {
			_readOnly = value;
			SetReadOnly(this, value);
		}
	}

	private static void SetReadOnly(Control c, bool b) {
		if (c is TextBox)
			((TextBox) c).ReadOnly = b;
		else if (c is ComboCheckBox) {
			((ComboCheckBox) c).Enabled = !b;
		}
		else if (c is ComboBox || c is CheckBox || c is NumericUpDown) {
			c.Enabled = !b;
		}
		else if (c.Controls.Count > 0) {
			foreach (Control c2 in c.Controls)
				SetReadOnly(c2, b);
		}
	}

	/*private static void init(ComboCheckBox combo, int e, Type t) {
		for (int i = 0; i < combo.Items.Count; i++) {
			Object o = combo.Items[i];
			Enum e2 = null;
			if (o is Enum)
				e2 = (Enum) o;
			else //if (o is String)
				e2 = (Enum) Enum.Parse(t, (String) o, true);

			long l1 = Convert.ToInt64(e);
			long l2 = Convert.ToInt64(e2);
			long l3 = l1 | l2;
			combo.ItemChecked[o] = l3 == l1;
		}
	}*/

	/*private static T get<T>(ComboCheckBox combo) {
		Type t = typeof(T);
		ulong val = 0;
		for (int i = 0; i < combo.Items.Count; i++) {
			if (combo.ItemChecked[i]) {
				Object o = combo.Items[i];
				Enum e;
				if (o is Enum)
					e = (Enum) o;
				else
					e = (Enum) Enum.Parse(t, (String) o);

				ulong l1 = Convert.ToUInt64(e);
				val = val | l1;
			}
		}

		return (T) Enum.ToObject(t, val);
	}*/

	public CHMWindow Current {
		get {
			ApplyValues(current);
			return current;
		}
	}

	public void InitFields(CHMWindow w, List<String> files) {
		current = w;
		tbName.Text = w.Name;
		tbCaption.Text = w.TitleBarText;
		OptionsPanel.InitFileCombo(comboDefaultPage, w.DefaultFile, files);
		OptionsPanel.InitFileCombo(comboHomePage, w.HomeFile, files);

		comboWinProps.EnumFlags = (CHMWinProp) w.WinProp;

		nudWindowH.Value = w.WindowBounds.Height;
		nudWindowW.Value = w.WindowBounds.Width;
		nudWindowX.Value = w.WindowBounds.X;
		nudWindowY.Value = w.WindowBounds.Y;
		nudTOCWidth.Value = w.TOCWidth;
		comboTabPosition.SelectedItem = (CHMTabPosition) w.TabPosition;
		comboButtons.EnumFlags = (CHMButton) w.ButtonBar;
		tbJumpButton1Text.Text = w.Jump1ButtonText;
		tbJumpButton2Text.Text = w.Jump2ButtonText;

		// ----------------------------
		// potentially the URL refers to an external website
		OptionsPanel.InitFileCombo(comboJumpButton1URL, w.Jump1ButtonURL, files);
		OptionsPanel.InitFileCombo(comboJumpButton2URL, w.Jump2ButtonURL, files);

		comboShowState.SelectedItem = (SW) w.ShowState;
		cbTOCInitiallyHidden.Checked = w.TOCInitiallyHidden;
		comboWindowStyles.EnumFlags = (WS) w.WindowStyles;
		comboWindowExStyles.EnumFlags = (WSEX) w.WindowExStyles;
		nudNotifyID.Value = w.NotifyID;

		comboButtons_CheckedChanged(null, null); // for some reason, setting the checked doesn't fire the event
	}

	public void ApplyValues(CHMWindow w) {

		w.Name = tbName.Text;
		w.TitleBarText = tbCaption.Text;
		w.DefaultFile = comboDefaultPage.Text;
		w.HomeFile = comboHomePage.Text;

		w.WinProp = (int) comboWinProps.EnumFlags;

		w.WindowBounds.Height = Convert.ToInt32(nudWindowH.Value);
		w.WindowBounds.Width = Convert.ToInt32(nudWindowW.Value);
		w.WindowBounds.X = Convert.ToInt32(nudWindowX.Value);
		w.WindowBounds.Y = Convert.ToInt32(nudWindowY.Value);
		w.TOCWidth = Convert.ToInt32(nudTOCWidth.Value);
		w.TabPosition = (int) comboTabPosition.SelectedItem;
		w.ButtonBar = (int) comboButtons.EnumFlags;
		w.Jump1ButtonText = tbJumpButton1Text.Text;
		w.Jump2ButtonText = tbJumpButton2Text.Text;
		w.Jump1ButtonURL = comboJumpButton1URL.Text;
		w.Jump2ButtonURL = comboJumpButton2URL.Text;
		w.ShowState = (int) comboShowState.SelectedItem;
		w.TOCInitiallyHidden = cbTOCInitiallyHidden.Checked;
		w.WindowStyles = (int) comboWindowStyles.EnumFlags;
		w.WindowExStyles = (int) comboWindowExStyles.EnumFlags;
		w.NotifyID = Convert.ToInt32(nudNotifyID.Value);
	}
}

public class EnumFlagComboBox<T> : ComboCheckBox where T : struct, IConvertible {

	public EnumFlagComboBox() : base(new Object[0]) {
		String[] arr = (String[]) Enum.GetNames(typeof(T));
		Items.AddRange(arr.OrderBy(tv => tv).ToArray(), false);
	}

	public T EnumFlags {
		get {
			Type t = typeof(T);
			ulong val = 0;
			for (int i = 0; i < Items.Count; i++) {
				if (ItemChecked[i]) {
					Object o = Items[i];
					Enum e = (Enum) Enum.Parse(t, (String) o);

					ulong l1 = Convert.ToUInt64(e);
					val = val | l1;
				}
			}

			return (T) Enum.ToObject(t, val);
		}

		set {
			long l1 = Convert.ToInt64(value);
			Type t = typeof(T);
			for (int i = 0; i < Items.Count; i++) {
				Object o = Items[i];
				Enum e2 = (Enum) Enum.Parse(t, (String) o, true);

				long l2 = Convert.ToInt64(e2);
				long l3 = l1 | l2;
				ItemChecked[o] = l3 == l1;
			}
		}
	}
}
}
