using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Opulos.Core.Utils;
using Opulos.Core.UI;

namespace Opulos.NüHelp {

public class EncodingComboBox : ComboBox2 {

	//static Hashtable htEncodingInfo = new Hashtable(); // ht[CodePage] == EncodingInfo
	static Enc[] arrEnc = null;

	static EncodingComboBox() {
		EncodingInfo[] arr = Encoding.GetEncodings();
		arr = arr.OrderBy(e => e.DisplayName).ToArray();

		Enc[] arr2 = new Enc[arr.Length];
		for (int i = 0; i < arr.Length; i++) {
			arr2[i] = new Enc(arr[i]);
			//htEncodingInfo[arr[i].CodePage] = arr[i];
		}

		arrEnc = arr2;
	}

	private static int CP(ComboBox combo) {
		Enc enc = combo.SelectedItem as Enc;
		if (enc == null || enc.enc == null)
			return -1;
		return enc.enc.CodePage;
	}

	private class Enc {
		public EncodingInfo enc = null;
		public Enc(EncodingInfo enc) {
			this.enc = enc;
		}
		public override String ToString() {
			return (enc == null ? "" : enc.DisplayName + " - " + enc.Name + " - " + enc.CodePage);
		}
		public override bool Equals(object obj) {
			int cp = -1;
			if (obj is int)
				cp = (int) obj;
			else if (obj is Enc)
				cp = ((Enc) obj).GetHashCode();
			else if (obj is EncodingInfo)
				cp = ((EncodingInfo) obj).CodePage;
			else
				return false;

			return cp == GetHashCode();
		}

		public override int GetHashCode() {
			if (enc == null)
				return -1;
			return enc.CodePage;
		}
	}

	public EncodingComboBox(bool addNull = true) {
		if (addNull)
			this.Items.Add(new Enc(null));

		this.Items.AddRange(arrEnc);
		this.DropDownStyle = ComboBoxStyle.DropDownList;
	}

	public int CodePage {
		get {
			return CP(this);
		}
	}
}


public class EncodingsPanel : Pane {

	Label lbHtmlReadEncoding = new Label() { Text = "Html Files Read Encoding" };
	EncodingComboBox comboHtmlReadEncoding = new EncodingComboBox();

	Label lbHtmlWriteEncoding = new Label() { Text = "Html Files Write Encoding" };
	EncodingComboBox comboHtmlWriteEncoding = new EncodingComboBox();

	Label lbHHCEncoding = new Label() { Text = "HHC Encoding" };
	EncodingComboBox comboHHCEncoding = new EncodingComboBox();

	Label lbHHKEncoding = new Label() { Text = "HHK Encoding" };
	EncodingComboBox comboHHKEncoding = new EncodingComboBox();

	Label lbHHPEncoding = new Label() { Text = "HHP Encoding" };
	EncodingComboBox comboHHPEncoding = new EncodingComboBox();

	public EncodingsPanel() {

		RCLayout r0 = new RCLayout(2);
		r0.OverallInsets = new Insets(10);
		r0.Col[1].expand = 1;
		r0.Col[1].shrink = 1;
		r0.Col[1].fillX = 1;
		setLayout(r0);
		add(lbHtmlReadEncoding, comboHtmlReadEncoding);
		add(lbHtmlWriteEncoding, comboHtmlWriteEncoding);
		add(lbHHCEncoding, comboHHCEncoding);
		add(lbHHKEncoding, comboHHKEncoding);
		add(lbHHPEncoding, comboHHPEncoding);
	}

	public void InitFields(Settings s) {
		comboHHCEncoding.SelectedItem = s.HHCEncoding;
		comboHHKEncoding.SelectedItem = s.HHKEncoding;
		comboHHPEncoding.SelectedItem = s.HHPEncoding;
		comboHtmlReadEncoding.SelectedItem = s.HtmlFilesReadEncoding;
		comboHtmlWriteEncoding.SelectedItem = s.HtmlFilesWriteEncoding;
	}

	public void ApplyValues(Settings s) {
		s.HHCEncoding = comboHHCEncoding.CodePage;
		s.HHKEncoding = comboHHKEncoding.CodePage;
		s.HHPEncoding = comboHHPEncoding.CodePage;
		s.HtmlFilesReadEncoding = comboHtmlReadEncoding.CodePage;
		s.HtmlFilesWriteEncoding = comboHtmlWriteEncoding.CodePage;
	}


}
}
