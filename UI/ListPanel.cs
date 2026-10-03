using Opulos.Core.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Opulos.NüHelp {
public class ListPanel : Pane {

	TextBox tb = new TextBox();
	Button btnAdd = new Button() { Text = "Add", AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink, Enabled = false };
	Button btnRemove = new Button() { Text = "Remove", AutoSizeMode = AutoSizeMode.GrowAndShrink, Enabled = false };
	ListBox listBox = new ListBox() { SelectionMode = SelectionMode.MultiExtended };

	public ListPanel() {
		Pane p1 = new Pane(0, tb, btnAdd, btnRemove);
		RCLayout r0 = new RCLayout(1);
		r0.esf(listBox);
		r0[p1].fillX = 1;
		setLayout(r0);
		add(p1);
		add(listBox);

		listBox.SelectedIndexChanged += delegate {
			btnRemove.Enabled = listBox.SelectedItems.Count > 0;
			tb.Text = "" + listBox.SelectedItem;
		};

		tb.TextChanged += delegate {
			String text = tb.Text.Trim();
			btnAdd.Enabled = text.Length > 0;
		};

		btnAdd.Click += btn_Click;
		btnRemove.Click += btn_Click;
	}

	void btn_Click(Object src, EventArgs e) {
		if (src == btnAdd) {
			listBox.Items.Add(tb.Text.Trim());
			tb.Text = "";
		}
		else if (src == btnRemove) {
			Object[] arr = listBox.SelectedItems.Cast<Object>().ToArray();
			foreach (Object o in arr)
				listBox.Items.Remove(o);
		}
	}

	public IList List {
		get {
			return listBox.Items.Cast<Object>().ToList();
		}
		set {
			listBox.Items.Clear();
			foreach (Object o in value)
				listBox.Items.Add(o);
		}
	}
}
}
