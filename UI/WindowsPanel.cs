using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Opulos.Core.UI;
using Opulos.NüHelp.CHM;

namespace Opulos.NüHelp {

public class WindowsPanel : Pane {

	Button btnAdd = new Button() { Text = "Add..." };
	Button btnEdit = new Button() { Text = "Edit...", Enabled = false };
	Button btnRemove = new Button() { Text = "Remove...", Enabled = false };

	CHMWindowDetailPanel panelWindow = new CHMWindowDetailPanel() { ReadOnly = true };
	GroupBox2 gbViewOnly;

	Label lbWindows = new Label() { Text = "Primary Window" };
	ComboBox3<CHMWindow> comboWindows = new ComboBox3<CHMWindow>(w => w.Name) { DropDownStyle = ComboBoxStyle.DropDownList, Sorted = true };

	Dialog<CHMWindowDetailPanel> dialogAdd = null;
	Dialog<CHMWindowDetailPanel> dialogEdit = null;
	List<String> filenames = new List<String>();

	public WindowsPanel() {
		RCLayout r0 = new RCLayout(1);
		r0.OverallInsets = new Insets(10);
		r0.esf(panelWindow);
		setLayout(r0);

		gbViewOnly = new GroupBox2(panelWindow, "View Only - Use Edit Button");

		add(new Pane(btnAdd, btnEdit, btnRemove));
		add(new Pane(lbWindows, comboWindows));
		add(gbViewOnly);

		btnAdd.Click += btn_Click;
		btnEdit.Click += btn_Click;
		btnRemove.Click += btn_Click;

		comboWindows.SelectedIndexChanged += delegate {
			bool b = (comboWindows.SelectedItem != null);
			btnEdit.Enabled = b;
			btnRemove.Enabled = b;
			CHMWindow w = comboWindows.SelectedItem;
			if (w != null)
				panelWindow.InitFields(w, filenames);
		};
	}

	protected override void OnFontChanged(EventArgs e) {
		base.OnFontChanged(e);
		gbViewOnly.Font = new System.Drawing.Font(this.Font.FontFamily, this.Font.Size, System.Drawing.FontStyle.Bold);
		panelWindow.Font = this.Font;
	}

	void btn_Click(Object src, EventArgs e) {
		if (src == btnAdd) {
			if (dialogAdd == null) {
				dialogAdd = new Dialog<CHMWindowDetailPanel>(new CHMWindowDetailPanel());
				dialogAdd.Text = "Add CHMWindow";
				dialogAdd.Owner = this.FindForm();
				dialogAdd.ButtonClicked += dialog_ButtonClicked;
			}

			dialogAdd.Content.InitFields(new CHMWindow(), filenames);
			dialogAdd.Show(btnAdd);
		}
		else if (src == btnEdit) {
			if (dialogEdit == null) {
				dialogEdit = new Dialog<CHMWindowDetailPanel>(new CHMWindowDetailPanel());
				dialogEdit.Text = "Edit CHMWindow";
				dialogEdit.Owner = this.FindForm();
				dialogEdit.ButtonClicked += dialog_ButtonClicked;
			}

			dialogEdit.Content.InitFields(comboWindows.SelectedItem, filenames);
			dialogEdit.Show(btnEdit);
		}
		else if (src == btnRemove) {
			var r = MessageBox2.Show(this.FindForm(), "Are you sure you want to remove the window from the list?", "Confirm Remove", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (r != DialogResult.OK)
				return;

			Object o = comboWindows.SelectedItem;
			comboWindows.SelectedIndex = -1; // must be done before remove.
			comboWindows.Items.Remove(o);
		}
	}

	public void InitFields(Settings s, List<String> filenames) {
		this.filenames = filenames;
		comboWindows.Clear();
		comboWindows.AddRange(s.CHMOptions.Windows);
		CHMWindow dw = s.CHMOptions.Windows.Find(w => String.Compare(w.Name, s.CHMOptions.DefaultWindow, true) == 0);
		comboWindows.SelectedItem = dw;
	}

	public void ApplyValues(Settings s) {
		s.CHMOptions.Windows.Clear();
		s.CHMOptions.Windows.AddRange(comboWindows.AllItems);
		CHMWindow w = comboWindows.SelectedItem;
		s.CHMOptions.DefaultWindow = (w == null ? "" : w.Name);
	}

	void dialog_ButtonClicked(Form2 dialog, DialogResult r, object src) {
		if (r == DialogResult.OK) {
			if (src == btnAdd) {
				CHMWindow window = dialogAdd.Content.Current;
				dialogAdd.Content.ApplyValues(window);
				comboWindows.Add(window);
				comboWindows.SelectedItem = window;
				panelWindow.InitFields(window, filenames);
			}
			else if (src == btnEdit) {
				var w = dialogEdit.Content.Current;
				dialogEdit.Content.ApplyValues(w); // if the name is changed then have to remove and
				comboWindows.Items.Remove(w);      // add to get the name to refresh
				comboWindows.Add(w);
				comboWindows.SelectedItem = w;
				panelWindow.InitFields(w, filenames);
			}

			this.PerformLayout();
		}

		dialog.Hide();
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
		if (disposing) {
			if (dialogAdd != null) {
				dialogAdd.Dispose();
				dialogAdd = null;
			}
			if (dialogEdit != null) {
				dialogEdit.Dispose();
				dialogEdit = null;
			}
		}
	}
}



}
