using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Opulos.NüHelp.CHM;
using Opulos.Core.Utils;
using Opulos.Core.UI;

namespace Opulos.NüHelp {

public class OptionsDialog : Dialog<CHMOptionsPanel> {

	private static readonly String TITLE = "CHM Options {0}";

	MenuStrip menuBar = new MenuStrip();
	ToolStripMenuItem miFile = new ToolStripMenuItem() { Text = "CHM Options &File" };

	ToolStripMenuItem miOpen = new ToolStripMenuItem("&Open...", Properties.Resources.Open) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
	ToolStripMenuItem miSave = new ToolStripMenuItem("&Save", Properties.Resources.Save) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };
	ToolStripMenuItem miSaveAs = new ToolStripMenuItem("Save &As...", Properties.Resources.SaveAs) { DisplayStyle = ToolStripItemDisplayStyle.ImageAndText };

	OpenFileDialog dialogOpen = null;
	SaveFileDialog dialogSave = null;

	List<String> AllFilenames = new List<String>();
	String CurrentOrigXML = "";

	public OptionsDialog() : base(new CHMOptionsPanel()) {
		this.Text = String.Format(TITLE, "").Trim();
		Current = new Settings();

		miFile.DropDown.Items.Add(miOpen);
		miFile.DropDown.Items.Add(miSave);
		miFile.DropDown.Items.Add(miSaveAs);
	
		miOpen.ImageScaling = ToolStripItemImageScaling.None;
		miSave.ImageScaling = ToolStripItemImageScaling.None;
		miSaveAs.ImageScaling = ToolStripItemImageScaling.None;

		menuBar.Items.Add(miFile);
		menuBar.ImageScalingSize = new Size(32, 32);
		menuBar.GripStyle = ToolStripGripStyle.Hidden;

		miOpen.Click += menuItem_Click;
		miSave.Click += menuItem_Click;
		miSaveAs.Click += menuItem_Click;
		miOpen.DropDownItemClicked += miOpen_DropDownItemClicked;

		Controls.Add(menuBar);
		this.MainMenuStrip = menuBar;

		refreshRecentFilesMenu();
	}

	public Settings Current {
		get; set;
	}

	public void InitFields(Settings s, List<String> allFiles) {
		String n = "";
		if (!String.IsNullOrEmpty(s.FilePath))
			n = " - " + Path.GetFileName(s.FilePath);

		this.Text = String.Format(OptionsDialog.TITLE, n).Trim();

		Content.InitFields(s, allFiles);
		this.AllFilenames = allFiles;
		CHMOptions chmOptions = s.CHMOptions;
		CurrentOrigXML = s.ToXML();
		Current = s;
	}

	public void ApplyValues(Settings s) {
		Content.ApplyValues(s);
	}

	public DialogResult CheckSave() {
		Settings s2 = new Settings();
		Content.ApplyValues(s2);

		DialogResult r = DialogResult.No;

		if (s2.ToXML() != CurrentOrigXML) {
			r = MessageBox2.Show(this, "Save changes to current?", "Save Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

			if (r == DialogResult.Yes) {
				s2.FilePath = Current.FilePath;
				s2.Save();
			}
		}

		return r;
	}

	void menuItem_Click(Object src, EventArgs e) {
		try {
			if (src == miOpen) {
				miFile.DropDown.Close();
				if (dialogOpen == null) {
					dialogOpen = new OpenFileDialog();
					dialogOpen.CheckFileExists = true;
					dialogOpen.Title = "Open Settings";
					dialogOpen.Filter = "Ini Files|*.ini";

					if (!String.IsNullOrEmpty(SharedSettings.Default.SourceFolderIni))
						dialogOpen.InitialDirectory = SharedSettings.Default.SourceFolderIni;
					else
						dialogOpen.InitialDirectory = Settings.DefaultFolder;
				}

				if (dialogOpen.ShowDialog(this.FindForm()) == DialogResult.OK) {
					if (CheckSave() == DialogResult.Cancel)
						return;

					SharedSettings.Default.SourceFolderIni = Path.GetDirectoryName(dialogOpen.FileName);
					Settings s = Settings.Load(dialogOpen.FileName);
					InitFields(s, AllFilenames);

					addFilename(s.FilePath);
				}
			}
			else if (src == miSave || src == miSaveAs) {
				bool prompt = src == miSaveAs || String.IsNullOrEmpty(Current.FilePath);
				if (prompt) {
					if (dialogSave == null) {
						dialogSave = new SaveFileDialog();
						dialogSave.OverwritePrompt = true;
						dialogSave.Title = "Settings Save As";
						dialogSave.Filter = "Ini Files|*.ini";

						if (!String.IsNullOrEmpty(SharedSettings.Default.SourceFolderIni))
							dialogSave.InitialDirectory = SharedSettings.Default.SourceFolderIni;
						else
							dialogSave.InitialDirectory = Settings.DefaultFolder;
					}

					if (dialogSave.ShowDialog(this.FindForm()) != DialogResult.OK)
						return;

					Settings s = new Settings();
					s.FilePath = dialogSave.FileName;
					ApplyValues(s);
					InitFields(s, AllFilenames);
					s.Save();
				}
				else {
					ApplyValues(Current);
					Current.Save();
				}
				addFilename(Current.FilePath); // moves name to top
			}
		} catch (Exception ex) {
			MessageBox2.ShowError(this, ex);
		}
	}

	private void addFilename(String fn) {
		SharedSettings s = SharedSettings.Default;
		s.SourceFolderIni = Path.GetDirectoryName(fn);
		s.RecentIniFiles.RemoveAll(t => String.Compare(t, fn, true) == 0);
		s.RecentIniFiles.RemoveAll(t => String.Compare(t, Settings.Default.FilePath, true) == 0);
		s.RecentIniFiles.Insert(0, fn);
		if (String.Compare(fn, Settings.Default.FilePath, true) != 0)
			s.RecentIniFiles.Insert(0, Settings.Default.FilePath);
		while (s.RecentIniFiles.Count > s.MaxRecentIniFiles && s.RecentIniFiles.Count > 0) {
			s.RecentIniFiles.RemoveAt(s.RecentIniFiles.Count - 1);
		}
		refreshRecentFilesMenu();
	}

	private void refreshRecentFilesMenu() {
		miOpen.DropDown.Items.Clear();
		foreach (String fn in SharedSettings.Default.RecentIniFiles) {
			if (File.Exists(fn))
				miOpen.DropDown.Items.Add(fn);
		}
	}

	void miOpen_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
		try {
			if (CheckSave() == DialogResult.Cancel)
				return;

			String filename = e.ClickedItem.Text;
			if (!File.Exists(filename)) {
				var r = MessageBox2.Show(this, "The file no longer exists.  Remove from list?", "File Not Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if (r == DialogResult.Yes) {
					miOpen.DropDownItems.Remove(e.ClickedItem);
					SharedSettings.Default.RecentIniFiles.RemoveAll(t => String.Compare(t, e.ClickedItem.Text, true) == 0);
				}
			}
			else {
				addFilename(filename); // move to top
				Settings s3 = Settings.Load(filename);
				InitFields(s3, AllFilenames);
			}

		} catch (Exception ex) {
			MessageBox2.ShowError(this, ex);
		}
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
		if (disposing) {
			if (dialogOpen != null) {
				dialogOpen.Dispose();
				dialogOpen = null;
			}
			if (dialogSave != null) {
				dialogSave.Dispose();
				dialogSave = null;
			}
		}
	}

}



}