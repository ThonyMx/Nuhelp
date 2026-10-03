using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using Opulos.NüHelp.CHM;
using Opulos.Core.UI;
using Opulos.Core.Utils;
using System.Collections;
using System.Threading;

namespace Opulos.NüHelp {

public enum InsertPosition {
	Nested,
	Above,
	Below
}


public class NüHelpForm : Form2, IHelpURL, IHelpFile {

	private static readonly String WelcomeTitle = "Help Welcome";

	MenuStrip toolStrip = new MenuStrip { ShowItemToolTips = true };
	TreeView tvCHM = new TreeView();
	Splitter splitter = new Splitter();
	WebBrowser webBrowser = new WebBrowser();

	ToolStripButton btnNew = new ToolStripButton("Clear all files from project.") { DisplayStyle = ToolStripItemDisplayStyle.Image };
	ToolStripSplitButton btnSaveBatch = new ToolStripSplitButton("Save added files to a batch file.") { DisplayStyle = ToolStripItemDisplayStyle.Image };
	ToolStripMenuItem miSaveAsBatch = new ToolStripMenuItem("Save As...");
	ToolStripDropDownButton btnAdd = new ToolStripDropDownButton("Insert a Word document or Html file to the current project.") { DisplayStyle = ToolStripItemDisplayStyle.Image };
	ToolStripDropDownButton btnInsertPosition = new ToolStripDropDownButton("Insert location for added document.") { DisplayStyle = ToolStripItemDisplayStyle.Image };
	ToolStripButton btnRemove = new ToolStripButton("Removes the selected node from the project tree.") { DisplayStyle = ToolStripItemDisplayStyle.Image, Enabled = false };
	ToolStripSeparator sep1 = new ToolStripSeparator();
	ToolStripDropDownButton btnOptions = new ToolStripDropDownButton("CHM Options.") { DisplayStyle = ToolStripItemDisplayStyle.Image };
	ToolStripButton btnCompile = new ToolStripButton("Compile.") { DisplayStyle = ToolStripItemDisplayStyle.Image };
	ToolStripSeparator sep2 = new ToolStripSeparator();
	ToolStripLabel lbOutputFolder = new ToolStripLabel("Output folder:") { IsLink = true, ToolTipText = "Click to open folder." };
	ToolStripTextBox tbOutputFolder = new ToolStripSpringTextBox() { ToolTipText = "CHM file and temporary files are saved in this folder." };
	ToolStripButton btnChangeOutputPath = new ToolStripButton("Browse a new output folder.") { DisplayStyle = ToolStripItemDisplayStyle.Image };
	ToolStripButton btnHelp = new ToolStripButton("Open help file.") { DisplayStyle = ToolStripItemDisplayStyle.Image, Alignment = ToolStripItemAlignment.Right };

	ContextMenuStrip menuRecentFiles = new ContextMenuStrip();
	ToolStripMenuItem miRemove = new ToolStripMenuItem("Remove from list");
	ToolStripMenuItem miOpenFolder = new ToolStripMenuItem("Open folder");
	ToolStripMenuItem miOpenFile = new ToolStripMenuItem("Open file");
	ToolStripMenuItem miCopyPath = new ToolStripMenuItem("Copy path") { ToolTipText = "Copy the file path to clipboard." };

	ToolStripMenuItem miBrowse = new ToolStripMenuItem("Browse...");

	ToolStripMenuItem miEditOptions = new ToolStripMenuItem("Edit Checked...");

	ToolStripRadioButtonMenuItem rbAbove = new ToolStripRadioButtonMenuItem("Above") { ToolTipText = "Document is inserted above the current selected node.", AutoToolTip = false };
	ToolStripRadioButtonMenuItem rbBelow = new ToolStripRadioButtonMenuItem("Below") { ToolTipText = "Document is inserted below the current selected node.", AutoToolTip = false };
	ToolStripRadioButtonMenuItem rbNested = new ToolStripRadioButtonMenuItem("Nested") { ToolTipText = "Document is inserted as a child of the selected node.", AutoToolTip = false, Checked = true };

	OpenFileDialog dialogAddFile = null;
	CHMDocument chmDocument = new CHMDocument();
	ImageList imageList = new ImageList();
	DateTime LastHelpEvent;
	private bool WelcomeAdded = false;
	List<String> TempHtmlFiles = new List<String>();
	OptionsDialog dialogOptions = null;
	Settings settings = Settings.Default;
	Form dummyForm = null;
	ProjectList projectList = new ProjectList();

	SaveFileDialog dialogSaveBatch = null;

	ContextMenuStrip menuTree = new ContextMenuStrip();
	ToolStripMenuItem miNodeGotoParent = new ToolStripMenuItem("Goto Parent") { AutoToolTip = false, ToolTipText = "Select source document node." };
	ToolStripMenuItem miNodeReload = new ToolStripMenuItem("Reload") { AutoToolTip = false, ToolTipText = "Reload source document." };
	ToolStripMenuItem miNodeOpenFile = new ToolStripMenuItem("Open file");
	ToolStripMenuItem miNodeOpenFolder = new ToolStripMenuItem("Open folder");
	ToolStripMenuItem miNodeCopyPath = new ToolStripMenuItem("Copy path");
	ToolStripMenuItem miNodeRemove = new ToolStripMenuItem("Remove") { AutoToolTip = false, ToolTipText = "Remove this node." };
	ToolStripMenuItem miNodeExpandAll = new ToolStripMenuItem("Expand all");
	ToolStripMenuItem miNodeCollapseAll = new ToolStripMenuItem("Collapse all");

	public NüHelpForm() {
		menuRecentFiles.Items.Add(miRemove);
		menuRecentFiles.Items.Add(new ToolStripSeparator());
		menuRecentFiles.Items.Add(miOpenFolder);
		menuRecentFiles.Items.Add(miOpenFile);
		menuRecentFiles.Items.Add(miCopyPath);

		toolStrip.SuspendLayout();
		SuspendLayout();

		menuTree.ImageScalingSize = new Size(24, 24);
		miNodeReload.Image = Properties.Resources.Reload;
		miNodeOpenFile.Image = Properties.Resources.openfile;
		miNodeOpenFolder.Image = Properties.Resources.openfolder;
		miNodeRemove.Image = Properties.Resources.remove2;
		miNodeExpandAll.Image = Properties.Resources.expand_all;
		miNodeCollapseAll.Image = Properties.Resources.collapse_all;
		menuTree.Items.Add(miNodeReload);
		menuTree.Items.Add(new ToolStripSeparator());
		menuTree.Items.Add(miNodeOpenFile);
		menuTree.Items.Add(miNodeOpenFolder);
		menuTree.Items.Add(miNodeCopyPath);
		menuTree.Items.Add(new ToolStripSeparator());
		menuTree.Items.Add(miNodeRemove);
		menuTree.Items.Add(new ToolStripSeparator());
		menuTree.Items.Add(miNodeExpandAll);
		menuTree.Items.Add(miNodeCollapseAll);
		tvCHM.ContextMenuStrip = menuTree;

		tvCHM.NodeMouseClick += (o, e) => {
			// bug in the TreeView causes a right click to select the
			// previously selected node
			tvCHM.SelectedNode = e.Node;
			CHMNode n = e.Node.Tag as CHMNode; // top node might be CHMDocument
			bool hasOrig = false;
			bool isSrcNode = false;
			if (n != null) {
				hasOrig = (n.OriginalFile2 != null);
				isSrcNode = (n.OriginalFile != null);
				//miNodeReload.Tag = e.Node;
			}

			menuTree.Items.RemoveAt(0);
			menuTree.Items.Insert(0, isSrcNode ? miNodeReload : miNodeGotoParent);

			miNodeCopyPath.Enabled = hasOrig;
			miNodeOpenFile.Enabled = hasOrig;
			miNodeOpenFolder.Enabled = hasOrig;
			miNodeGotoParent.Enabled = hasOrig;
			miNodeReload.Enabled = isSrcNode;
			miNodeExpandAll.Enabled = e.Node.Nodes.Count > 0;
			miNodeCollapseAll.Enabled = e.Node.Nodes.Count > 0;
		};

		menuTree.ItemClicked += menuTree_ItemClicked;

		toolStrip.Items.AddRange(new ToolStripItem[] {
			btnNew,
			btnAdd,
			btnInsertPosition,
			btnRemove,
			btnSaveBatch,
			sep1,
			btnOptions,
			btnCompile,
			sep2,
			lbOutputFolder,
			tbOutputFolder,
			btnChangeOutputPath,
			btnHelp,
		});

		btnInsertPosition.DropDown.ShowItemToolTips = true;
		btnInsertPosition.DropDown.Items.AddRange(new [] { rbAbove, rbBelow, rbNested });

		// Button Padding:
		btnNew.Padding = new Padding(5);
		btnAdd.Padding = new Padding(5);
		btnInsertPosition.Padding = new Padding(5);
		btnRemove.Padding = new Padding(5);
		btnSaveBatch.Padding = new Padding(5);
		btnOptions.Padding = new Padding(5);
		btnCompile.Padding = new Padding(5);
		btnChangeOutputPath.Padding = new Padding(5);
		btnHelp.Padding = new Padding(5);

		// Button Images:
		btnNew.Image = Properties.Resources.New;
		btnAdd.Image = Properties.Resources.Add;
		btnInsertPosition.Image = Properties.Resources.Insert2;
		btnRemove.Image = Properties.Resources.Remove;
		btnSaveBatch.Image = Properties.Resources.Save2;
		btnOptions.Image = Properties.Resources.Options;
		btnCompile.Image = Properties.Resources.Compile3;
		btnChangeOutputPath.Image = Properties.Resources.Folder;
		btnHelp.Image = Properties.Resources.Help;

		miOpenFile.Image = Properties.Resources.openfile;
		miRemove.Image = Properties.Resources.remove2;
		miOpenFolder.Image = Properties.Resources.openfolder;

		Font fnt = FontEx.New(SystemFonts.MenuFont.FontFamily, SharedSettings.Default.FontSize);

		menuRecentFiles.ImageScalingSize = new Size(24, 24);
		toolStrip.ImageScalingSize = new Size(32, 32);
		toolStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
		this.Font = fnt;
		menuRecentFiles.Font = fnt;
		menuTree.Font = fnt;
		toolStrip.Font = fnt;
		this.MainMenuStrip = toolStrip;
		tbOutputFolder.Font = fnt;
		tbOutputFolder.Text = settings.OutputFolder;
		this.FontChanged += delegate {
			tbOutputFolder.Font = this.Font;
			menuTree.Font = this.Font;
		};


		tvCHM.Dock = DockStyle.Left;
		tvCHM.HideSelection = false;
		tvCHM.Size = new Size(200, 200);
		tvCHM.AfterSelect += tvCHM_AfterSelect;

		imageList.Images.Add(Properties.Resources.TreeRoot);
		imageList.Images.Add(Properties.Resources.TreeFolder);
		imageList.Images.Add(Properties.Resources.TreeItem);
		tvCHM.ImageList = imageList;

		webBrowser.Dock = DockStyle.Fill;

		SharedSettings s = SharedSettings.Default;
		if (File.Exists(s.FilePath)) {
			this.StartPosition = FormStartPosition.Manual;
			this.Bounds = new Rectangle(s.WindowX, s.WindowY, s.WindowW, s.WindowH);
			this.WindowState = s.WindowState;
		}
		else {
			this.Size = new Size(s.WindowW, s.WindowH); // must be done before StartPosition is set
			this.StartPosition = FormStartPosition.CenterScreen;
		}

		Controls.Add(webBrowser);
		Controls.Add(splitter);
		Controls.Add(tvCHM);
		Controls.Add(toolStrip);
		Icon =  Properties.Resources.NüHelp;
		Text = "NüHelp v" + AssemblyUtils.getProgramCompiledDate().ToString("yyyy'.'MM'.'dd");
		toolStrip.ResumeLayout(false);
		toolStrip.PerformLayout();
		ResumeLayout(false);
		PerformLayout();

		btnNew.Click += buttonClicked;
		btnRemove.Click += buttonClicked;
		btnSaveBatch.ButtonClick += buttonClicked;
		btnCompile.Click += buttonClicked;
		btnChangeOutputPath.Click += buttonClicked;

		// call it on a BeginInvoke so the menu closes
		btnAdd.DropDownItemClicked += (o, e) => {
			this.BeginInvoke((Action<Object,ToolStripItemClickedEventArgs>) btnAdd_DropDownItemClicked, o, e);
		};

		btnAdd.DropDownClosed += delegate {
			menuRecentFiles.Hide();
		};

		// alternatively could set AutoClose to false and close the menu when an item is clicked.
		// although, it's kind of nice to have the menus auto-close.
		//btnAdd.DropDown.AutoClose = false;
		btnAdd.AutoCloseDelay();
		btnInsertPosition.AutoCloseDelay();
		btnOptions.AutoCloseDelay();
		btnSaveBatch.AutoCloseDelay();

		btnSaveBatch.DropDown.Items.Add(miSaveAsBatch);
		miSaveAsBatch.ImageScaling = ToolStripItemImageScaling.None;
		miSaveAsBatch.Image = Properties.Resources.Pencil;
		miSaveAsBatch.Click += delegate {
			this.BeginInvoke((Action) delegate {
				try {
					SaveBatch(true);
				} catch (Exception ex) {
					MessageBox2.ShowError(this, ex);
				}
			});
		};

		miBrowse.ImageScaling = ToolStripItemImageScaling.None;
		miBrowse.Image = Properties.Resources.Browse;

		miEditOptions.ImageScaling = ToolStripItemImageScaling.None;
		miEditOptions.Image = Properties.Resources.Pencil2;

		// need to do this otherwise right click will hide the menu
		DateTime RightClickEventTime = DateTime.MinValue;
		btnAdd.DropDown.Closing += (o, e) => {
			TimeSpan ts = DateTime.Now - RightClickEventTime;
			if (ts.TotalSeconds < 0.01)
				e.Cancel = true;
		};

		this.Deactivate += delegate {
			btnAdd.DropDown.Hide();
		};

		btnAdd.DropDown.MouseClick += (o,e) => {
			ToolStripItem RightClickItem = null;
			if (e.Button == MouseButtons.Right) {
				Point p = e.Location;
				for (int i = 1; i < btnAdd.DropDown.Items.Count; i++) {
					if (btnAdd.DropDownItems[i].Bounds.Contains(p)) {
						RightClickItem = btnAdd.DropDownItems[i];
						break;
					}
				}

				if (RightClickItem != null) {
					RightClickEventTime = DateTime.Now;
					menuRecentFiles.Show(btnAdd.DropDown, e.Location);
				}
			}
			menuRecentFiles.Tag = RightClickItem;
		};

		menuRecentFiles.ItemClicked += menuRecentFiles_ItemClicked;

		lbOutputFolder.Click += delegate {
			using (System.Diagnostics.Process.Start("explorer.exe", tbOutputFolder.Text)) {}
		};

		btnHelp.Click += delegate {
			OnHelpRequested(null);
		};

		// after a Windows update, 
		webBrowser.PreviewKeyDown += (o,e) => {
			if (e.KeyCode == Keys.F1)
				OnHelpRequested(null);
		};

		btnOptions.DropDown.ItemClicked += DropDown_ItemClicked;

		if (SharedSettings.Default.RecentIniFiles.Count == 0)
			SharedSettings.Default.RecentIniFiles.Add(settings.FilePath);

		NewProject(true);
		refreshRecentFilesMenu();
		refreshSettingsMenu(Settings.Default.FilePath);

		webBrowser.Navigated += webBrowser_Navigated;

		tvCHM.Click += tvCHM_Click;
		tvCHM.DrawMode = TreeViewDrawMode.OwnerDrawText;
		tvCHM.DrawNode += tvCHM_DrawNode;
	}

	void tvCHM_DrawNode(object sender, DrawTreeNodeEventArgs e) {
        var state = e.State;
        Font font = e.Node.NodeFont ?? e.Node.TreeView.Font;
        Color fore = e.Node.ForeColor;
        if (fore == Color.Empty) fore = e.Node.TreeView.ForeColor;
        if (e.Node == e.Node.TreeView.SelectedNode) {
            fore = SystemColors.HighlightText;
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
            ControlPaint.DrawFocusRectangle(e.Graphics, e.Bounds, fore, SystemColors.Highlight);
            TextRenderer.DrawText(e.Graphics, e.Node.Text, font, e.Bounds, fore, TextFormatFlags.GlyphOverhangPadding);
        }
        else {
			if (e.Node.BackColor.IsEmpty)
				e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
			else {
				using (Brush b = new SolidBrush(e.Node.BackColor))
					e.Graphics.FillRectangle(b, e.Bounds);
			}
            TextRenderer.DrawText(e.Graphics, e.Node.Text, font, e.Bounds, fore, TextFormatFlags.GlyphOverhangPadding | TextFormatFlags.NoPrefix);
        }
	}

	private DateTime tvCHM_ClickTime = DateTime.UtcNow;
	void tvCHM_Click(object sender, EventArgs e) {
		tvCHM_ClickTime = DateTime.UtcNow;
	}

	void webBrowser_Navigated(object sender, WebBrowserNavigatedEventArgs e) {
		try {
			TimeSpan ts = DateTime.UtcNow - tvCHM_ClickTime;
			if (ts.TotalSeconds < 0.2)
				return;

			Uri url = e.Url;
			List<CHMNode> allNodes = chmDocument.GetAllNodes();
			CHMNode selectedNode = null;
			foreach (var n in allNodes) {
				if (new Uri("file://" + n.Local).Equals(url)) {
					selectedNode = n;
					break;
				}
			}

			if (selectedNode == null)
				return;

			Stack s = new Stack();
			foreach (var n in tvCHM.Nodes)
				s.Push(n);

			while (s.Count > 0) {
				TreeNode tn = (TreeNode) s.Pop();
				if (tn.Tag == selectedNode) {
					tvCHM.SelectedNode = tn;
					tn.EnsureVisible();
					break;
				}

				foreach (var n in tn.Nodes)
					s.Push(n);
			}
		} catch {}
	}

	void menuRecentFiles_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
		try {
			ToolStripItem RightClickItem = (ToolStripItem) menuRecentFiles.Tag;
			if (RightClickItem == null)
				return;

			var item = e.ClickedItem;
			if (item != miRemove) {
				// close the menu
				btnAdd.ForceClose();
			}

			if (item == miRemove) {
				String fn = RightClickItem.Text;
				SharedSettings.Default.RecentFiles.RemoveAll(t => String.Compare(t, fn, true) == 0);
				refreshRecentFilesMenu();
			}
			else if (item == miOpenFolder) {
				using (System.Diagnostics.Process.Start("explorer.exe", String.Format("/select,\"{0}\"", RightClickItem.Text))) {}
			}
			else if (item == miOpenFile) {
				String fn = RightClickItem.Text;
				String ext = Path.GetExtension(fn).TrimStart('.');
				if (String.Compare(ext, "bat", true) == 0) {
					String exe = FileUtil.GetDefaultOpenWith("txt");
					if (exe == null)
						MessageBox2.Show(this, "No process found that can open txt files.", "Open Batch File", MessageBoxButtons.OK, MessageBoxIcon.Error);
					else
						using (System.Diagnostics.Process.Start(exe, "\"" + fn + "\"")) {}
				}
				else
					using (System.Diagnostics.Process.Start(fn)) {}
			}
			else if (item == miCopyPath) {
				Clipboard.SetText(RightClickItem.Text);
			}
		} catch (Exception ex) {
			MessageBox2.ShowError(this, ex);
		}
	}

	void menuTree_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
		try {
			var item = e.ClickedItem;
			TreeNode tn = tvCHM.SelectedNode;
			if (item == miNodeCopyPath) {
				CHMNode n = (CHMNode) tn.Tag;
				Clipboard.SetText(n.OriginalFile2.FullName);
			}
			else if (item == miNodeOpenFile) {
				CHMNode n = (CHMNode) tn.Tag;
				using (System.Diagnostics.Process.Start(n.OriginalFile2.FullName)) {}
			}
			else if (item == miNodeOpenFolder) {
				CHMNode n = (CHMNode) tn.Tag;
				using (System.Diagnostics.Process.Start("explorer.exe", String.Format("/select,\"{0}\"", n.OriginalFile2.FullName))) {}
			}
			else if (item == miNodeGotoParent) {
				CHMNode n = (CHMNode) tn.Tag;
				FileInfo f = n.OriginalFile2;
				Stack s = new Stack();
				foreach (TreeNode tn2 in tvCHM.Nodes)
					s.Push(tn2);

				while (s.Count > 0) {
					TreeNode tn2 = (TreeNode) s.Pop();
					CHMNode n2 = tn2.Tag as CHMNode;
					if (n2 != null && n2.OriginalFile == f) {
						tvCHM.SelectedNode = tn2;
						tn2.EnsureVisible();
						break;
					}
					foreach (var tn3 in tn2.Nodes)
						s.Push(tn3);
				}
			}
			else if (item == miNodeRemove)
				RemoveNode(tn);
			else if (item == miNodeExpandAll)
				tn.ExpandAll();
			else if (item == miNodeCollapseAll)
				tn.Collapse(false);
			else if (item == miNodeReload) {
				ReloadResult rr = null;
				try {
					tvCHM.BeginUpdate();
					rr = Reload(tn);
				} finally {
					tvCHM.EndUpdate();
				}
				if (rr.HasErrors)
					MessageBox2.Show(this, rr.ResultMessage, "Reload Warnings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				//else
				//	MessageBox2.Show(this, "Reload successful.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
		} catch (Exception ex) {
			MessageBox2.ShowError(this, ex);
		}
	}

	private class ReloadResult {
		public List<ReinsertInfo> ParentNotFound = new List<ReinsertInfo>();
		public List<ReinsertInfo> SiblingNotFound = new List<ReinsertInfo>();

		public bool HasErrors {
			get {
				return ParentNotFound.Count > 0 || SiblingNotFound.Count > 0;
			}
		}

		public String ResultMessage {
			get {
				if (SiblingNotFound.Count > 0 || ParentNotFound.Count > 0) {
					StringBuilder sb = new StringBuilder();
					sb.AppendLine("The following topics could not be attached in their original location:");
					foreach (ReinsertInfo ri in ParentNotFound)
						sb.AppendLine("{0} (parent not found: {1})".Format2(ri.node.Text, ri.parent.Text));
					foreach (ReinsertInfo ri in SiblingNotFound)
						sb.AppendLine("{0} (sibling not found: {1})".Format2(ri.node.Text, ri.sibling.Text));
					return sb.ToString();
				}
				return "";
			}
		}
	}

	private ReloadResult Reload(TreeNode tn) {
		TreeNode p = tn.Parent; // parent will never be null because the tn has an OriginalFile
		// p's Tag might be a CHMNode or it might be a CHMDocument

		CHMNode tn_c = (CHMNode) tn.Tag;
		String srcFile = tn_c.SourceFile; // e.g. HelpSettings_raw.html
		FileInfo origFile = tn_c.OriginalFile;

		// first, store the expanded state for each nested node of p.
		//
		// all nodes with the same source file must be removed. The other nodes might be located at the
		// same level as tn (as siblings) or they might be nested. Other nodes with a different source file
		// can be scattered around the nodes that need to be removed. The other nodes need to be detached
		// from the tree (but remembering their position), and then all children of the parent node (p)
		// will be deleted.
		//
		// Now p is completely empty. The original file will be added as a nested child to p (without refreshing
		// the treeview yet). The detached nodes are added back around the reloaded nodes. It's possible that the
		// remembered position no longer exists. In this case, the detached node is appended as the last child to
		// the appropriate parent.
		//
		// Now all nodes are reattached. The treeview is refreshed and the expanded states are reset. The selected
		// node is the treenode that contains the same original file. It's just that simple.

		// store the expand state of the nodes
		Hashtable htExpanded = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
		List<TreeNode> pNodes = AllNodes(p);
		foreach (TreeNode pn in pNodes) {
			if (pn.Nodes.Count > 0)
				htExpanded[pn.Text] = pn.IsExpanded;
		}

		// now detach the nodes that need to be preserved
		List<ReinsertInfo> riList = new List<ReinsertInfo>();
		DetachNodes(p, riList, srcFile);
		riList.Reverse();

		// now all nodes that need to be preserved are detached. All the children of p are deleted.
		for (int i = p.Nodes.Count - 1; i >= 0; i--) {
			RemoveNode(p.Nodes[i]);
		}

		// p is now completely empty
		if (origFile.Extension.TrimStart('.').StartsWith("doc", StringComparison.InvariantCultureIgnoreCase))
			File.Delete(srcFile); // this is the raw html file
		tvCHM.SelectedNode = p;
		
		NodeList(p).Clear(); // remove all p's CHMNode children
		addFilename(origFile.FullName, InsertPosition.Nested, false); // no refresh
		List<CHMNode> newNodes = new List<CHMNode>();
		AllNodes(p, newNodes);

		ReloadResult rr = new ReloadResult();

		// now reattach the nodes. The majoriy of the code is finding the right parent and
		// right index to reinsert each node.
		foreach (ReinsertInfo ri in riList) {
			TreeNode n = ri.node;
			CHMNode n_c = (CHMNode) n.Tag;

			TreeNode p2 = p;
			int index = -1;
			bool parentFound = false;
			List<CHMNode> nodeList = null;
			if (ri.parent != p) {
				CHMNode ri_p_c = (CHMNode) ri.parent.Tag;
				// we need to find former parent node
				// if it doesn't exist then the main parent p is used
				foreach (CHMNode nn in newNodes) {
					if (NameMatch(nn, ri_p_c)) { //String.Compare(nn.Name, ri_p_c.Name, true) == 0) {
						nodeList = nn.ChildNodes;
						parentFound = true;
						break;
					}
				}
			}
			else
				parentFound = true;
			
			if (nodeList == null)
				nodeList = NodeList(p);

			if (parentFound) {
				// if the former parent was found, then insert accordingly
				// otherwise the code will append the node to (p).
				if (ri.sibling == null) {
					index = 0;
				}
				else {
					CHMNode sibling_c = (CHMNode) ri.sibling.Tag;
					for (int i = 0; i < nodeList.Count; i++) {
						CHMNode n2 = nodeList[i];
						//if (String.Compare(n2.Name, n_c.Name, true) == 0) {
						if (NameMatch(n2, sibling_c)) {
							index = i  + 1; // +1 because it needs to be below
							break;
						}
					}	
				}
			}

			if (index == -1) {
				nodeList.Add(n_c); // add to end, the former sibling text no longer exists

				if (!parentFound)
					rr.ParentNotFound.Add(ri);
				else
					rr.SiblingNotFound.Add(ri);
			}
			else
				nodeList.Insert(index, n_c);
		}

		// now all nodes have been reattached.
		// the treeview can be refreshed
		AddNodes(NodeList(p), p);

		// finally select the node that has the original file
		foreach (TreeNode n in p.Nodes) {
			CHMNode n_c = (CHMNode) n.Tag;
			if (n_c.OriginalFile != null && String.Compare(n_c.OriginalFile.FullName, origFile.FullName, true) == 0) {
				tvCHM.SelectedNode = n;
				break;
			}
		}

		p.ExpandAll();
		foreach (TreeNode n in AllNodes(p)) {
			Object o = htExpanded[n.Text];
			if (o != null) {
				bool b = (bool) o;
				if (!b)
					n.Collapse(true);
			}
		}

		return rr;
	}

	private static bool NameMatch(CHMNode n1, CHMNode n2) {
		if (String.Compare(n1.Name, n2.Name, true) == 0)
			return true;

		if (!String.IsNullOrEmpty(n1.InvariantName)) {
			if (String.Compare(n1.InvariantName, n2.InvariantName, true) == 0)
				return true;
		}
		return false;
	}

	private static void AllNodes(TreeNode p, List<CHMNode> list) {
		Object tag = p.Tag;
		if (tag is CHMNode)
			AllNodes((CHMNode) tag, list);
		else {// CHMDocument
			CHMDocument p_doc = (CHMDocument) tag;
			foreach (CHMNode n in p_doc.Nodes)
				AllNodes(n, list);
		}
	}

	private static void AllNodes(CHMNode n, List<CHMNode> list) {
		list.Add(n);
		foreach (CHMNode c in n.ChildNodes)
			AllNodes(c, list);
	}

	private static List<CHMNode> NodeList(TreeNode n) {
		Object tag = n.Tag;
		if (tag is CHMNode)
			return ((CHMNode) tag).ChildNodes;
		return ((CHMDocument) tag).Nodes;
	}


	private static void Insert(CHMNode n, TreeNode p, int index) {
		Object tag = p.Tag;
		if (tag is CHMNode) {
			CHMNode p_c = (CHMNode) tag;
			p_c.ChildNodes.Insert(index, n);
		}
		else if (tag is CHMDocument) {
			var p_doc = (CHMDocument) tag;
			p_doc.Nodes.Insert(index, n);
		}
	}

	// look for children nodes that have the same source file. All other nodes need to be detached to prevent
	// them from being deleted.
	private static void DetachNodes(TreeNode n, List<ReinsertInfo> riList, String srcFile) {
		for (int i = n.Nodes.Count - 1; i >= 0; i--) {
			TreeNode n2 = n.Nodes[i];
			CHMNode n2_c = (CHMNode) n2.Tag;
			if (String.Compare(n2_c.SourceFile, srcFile, true) == 0) {
				// the node has the same source file, so we keep it attached
				// and look to see if it has any chidren with a different source file
				DetachNodes(n2, riList, srcFile);
			}
			else {
				// since this node has a different source file, it means all of its nested nodes
				// will also have a different source file. Thus, we only need to detach this node.
				ReinsertInfo ri = new ReinsertInfo();
				ri.node = n2;
				// if sibling is set, then this node will be reattached below that node
				// otherwise this node will be inserted in the parent at index position 0
				ri.sibling = (i > 0 ? n.Nodes[i-1] : null);
				ri.parent = n;
				riList.Add(ri);
				n2.Remove();
			}
		}
	}



	private class ReinsertInfo {
		// node will be reinserted below sibling (if set)
		// otherwise it is inserted at index position 0 in
		// children of the parent
		public TreeNode node;
		public TreeNode sibling;
		public TreeNode parent;
	}

	void DropDown_ItemClicked(Object src, ToolStripItemClickedEventArgs e) {
		try {
			if (e.ClickedItem == miEditOptions) {
				e.ClickedItem.Owner.Hide();
				if (dialogOptions == null) {
					dialogOptions = new OptionsDialog();
					dialogOptions.Owner = this;
					dialogOptions.ButtonClicked += dialogOptions_ButtonClicked;
				}

				dialogOptions.InitFields(settings, GetAllFiles());
				dialogOptions.Show(btnOptions);
			}
			else {
				settings = Settings.Load(e.ClickedItem.Text);
				tbOutputFolder.Text = settings.OutputFolder;
			}
		} catch (Exception ex) {
			MessageBox2.ShowError(this, ex);
		}
	}

	void btnAdd_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
		try {
		String fn = null;
		if (e.ClickedItem == miBrowse) {
			if (dialogAddFile == null) {
				dialogAddFile = new OpenFileDialog();
				if (Directory.Exists(tbOutputFolder.Text))
					dialogAddFile.InitialDirectory = tbOutputFolder.Text;
				else
					dialogAddFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

				dialogAddFile.Filter = "Word Documents|*.doc;*.docx|Html files|*.htm;*.html|Batch Files|*.bat";
				dialogAddFile.CheckFileExists = true;
				dialogAddFile.Multiselect = false;
			}

			if (!String.IsNullOrEmpty(SharedSettings.Default.SourceFolder))
				dialogAddFile.InitialDirectory = SharedSettings.Default.SourceFolder;

			if (dialogAddFile.ShowDialog(this) == DialogResult.OK) {
				fn = dialogAddFile.FileName;
			}
		}
		else {
			fn = e.ClickedItem.Text;
		}

		if (!String.IsNullOrEmpty(fn)) {
			if (File.Exists(fn)) {
				addFilename(fn, CurrentInsertPosition);
				addToRecentFileMenu(fn);
			}
			else {
				MessageBox2.Show(this, "Cannot find file: " + fn, "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		} catch (Exception ex) {
			RefreshView();
			MessageBox2.ShowError(this, ex);
		} finally {
			this.Cursor = Cursors.Default;
		}
	}

	private InsertPosition CurrentInsertPosition {
		get {
			if (rbAbove.Checked)
				return InsertPosition.Above;
			if (rbBelow.Checked)
				return InsertPosition.Below;
			return InsertPosition.Nested;
		}
	}

	private void addToRecentFileMenu(String fn) {
		String ext = Path.GetExtension(fn).TrimStart('.');
		SharedSettings s = SharedSettings.Default;
		if (String.Compare(ext, "bat", true) != 0)
			s.SourceFolder = Path.GetDirectoryName(fn);
		s.RecentFiles.RemoveAll(t => String.Compare(t, fn, true) == 0);
		s.RecentFiles.Insert(0, fn);
		while (s.RecentFiles.Count > s.MaxRecentFiles && s.RecentFiles.Count > 0) {
			s.RecentFiles.RemoveAt(s.RecentFiles.Count - 1);
		}

		refreshRecentFilesMenu();
	}

	private void SetCurrentSettings() {
		var active = projectList.OutputFiles.Where(o => o.ActiveFile).FirstOrDefault();
		if (active != null)
			active.SettingsFile = new FileInfo(settings.FilePath);
	}

	private void addFilename(String filename, InsertPosition insertPosition, bool refresh = true) {
		FileInfo file = new FileInfo(filename);
		String ext = file.Extension;
		if (String.Compare(ext, ".doc", true) == 0 || String.Compare(ext, ".docx", true) == 0)
			AppendWordDocument(file, insertPosition, refresh);
		else if (String.Compare(ext, ".htm", true) == 0 || String.Compare(ext, ".html", true) == 0)
			AppendHtmlFile(file, null, file, insertPosition, refresh);
		else if (String.Compare(ext, ".bat", true) == 0) {
			// parse batch file
			// add files to current project list
			// override the errorbox and successbox
			var projectList2 = ProjectList.Parse(file);
			ProjectList.OutputFile outputFile = null;
			if (projectList2.OutputFiles.Count > 1) {
				var combo = new ComboBox3<ProjectList.OutputFile>(o => o.RawArg, projectList2.OutputFiles.ToArray()) { DropDownStyle = ComboBoxStyle.DropDownList, SelectedIndex = 0, PreferredTextTemplate = "" };
				var result = InputBox.Show(this, "Select Project", "Batch file contains multiple projects. Select one to load:", MessageBoxButtons.OKCancel, combo, 1, 0);
				if (result != DialogResult.OK)
					return;

				outputFile = combo.SelectedItem;
			}
			else if (projectList2.OutputFiles.Count == 1)
				outputFile = projectList2.OutputFiles[0];
			else {
				MessageBox2.Show(this, "Batch file does not contain a -File: parameter.", "Empty Batch", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
				

			FileInfo settingsFile = null;
			foreach (var of in projectList2.OutputFiles) {
				if (of.SettingsFile != null)
					settingsFile = of.SettingsFile;
				if (of == outputFile)
					break;
			}

			if (settingsFile == null) {
				// use whatever settings are currently selected, which could be
				// the default setttings. Do not use Settings.Default;
			}
			else {
				var s = SharedSettings.Default;
				String fn = settingsFile.FullName;
				settings = Settings.FromXML<Settings>(fn);

				s.RecentIniFiles.RemoveAll(t => String.Compare(t, fn, true) == 0);
				s.RecentIniFiles.RemoveAll(t => String.Compare(t, Settings.Default.FilePath, true) == 0);
				s.RecentIniFiles.Insert(0, fn);
				if (String.Compare(fn, Settings.Default.FilePath, true) != 0)
					s.RecentIniFiles.Insert(0, Settings.Default.FilePath);

				refreshSettingsMenu(settings.FilePath);
			}

			if (outputFile.Entries.Count > 0)
				SharedSettings.Default.SourceFolder = outputFile.Entries[0].ContentFile.Directory.FullName;

			outputFile.ActiveFile = true;
			outputFile.SettingsFile = new FileInfo(settings.FilePath);
			NewProject(false);
			projectList = projectList2;

			ThreadController tcon = new ThreadController();
			int numThreads = Math.Max(1, SharedSettings.Default.NumThreads);
			String threads = (numThreads > 1 ? " threads." : " thread.");
			Label lb = new Label { AutoSize = true, Text = "Preparing to convert doc to html files using " + numThreads + threads };
			tcon.ProgressChanged += (o, e) => {
				this.BeginInvoke((Action) delegate {
					lb.Text = e.Message;
				});
			};

			Pane p = new Pane(lb);
			p.Font = this.Font;
			Dialog<Pane> dialog = new Dialog<Pane>(p, MessageBoxButtons.OK);
			dialog.Text = "Load Progress";
			dialog.HideOnClose = false;
			dialog.setButtonText("Cancel");
			dialog.Owner = this;
			dialog.FormClosed += delegate {
				tcon.RequestStop();
			};

			//dialog.FormClosing += (o, e) => {
			//	var r = MessageBox2.Show(dialog, "Are you sure you want to cancel load?", "Confirm Cancel", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			//	if (r == DialogResult.Cancel)
			//		e.Cancel = true;
			//};

			Thread th = new Thread(() => {
				try {
					NüHelpMain.Append(SharedSettings.Default.NumThreads, outputFile, chmDocument, settings, TempHtmlFiles, tcon);
					this.BeginInvoke((Action) delegate {
						dialog.Close();
					});
				} catch (Exception ex) {
					this.BeginInvoke((Action) delegate {
						MessageBox2.ShowError(dialog, ex);
						dialog.ErrorProvider.SetError(dialog.OKButton, "Error happened. Loading stopped.");
					});
				} finally {
					this.BeginInvoke((Action) delegate {
						RefreshView();
					});
				};
			});
			th.IsBackground = true;
			th.Start();
			dialog.Pack();
			dialog.ShowDialog(this);
			dialog.Dispose();
		}

		bool LinkBlankHeaders = (settings.BlankHeaders == BlankHeader.ShowInTOC || settings.BlankHeaders == BlankHeader.HideInTOCLinked);
		chmDocument.UpdateHeadersAndFooters(LinkBlankHeaders, null);
	}

	private void refreshRecentFilesMenu() {
		var item = btnAdd.DropDown.Items.Cast<ToolStripItem>().Where(t => t != miBrowse).ToList();
		btnAdd.DropDown.Items.Clear();
		btnAdd.DropDown.Items.Add(miBrowse);

		foreach (String fn in SharedSettings.Default.RecentFiles) {
			if (File.Exists(fn))
				btnAdd.DropDown.Items.Add(fn);
		}

		item.ForEach(t => t.Dispose());
	}

	private void refreshSettingsMenu(String currentFilename) {
		var oldItems = btnOptions.DropDown.Items.Cast<ToolStripItem>().Where(t => t != miEditOptions).ToList();
		btnOptions.DropDown.Items.Clear();
		oldItems.ForEach(x => x.Dispose());
		btnOptions.DropDown.Items.Add(miEditOptions);

		foreach (String fn in SharedSettings.Default.RecentIniFiles) {
			if (File.Exists(fn) || fn == Settings.Default.FilePath) {
				var mi = new ToolStripRadioButtonMenuItem(fn);
				btnOptions.DropDown.Items.Add(mi);
				if (String.Compare(fn, currentFilename, true) == 0) {
					mi.Checked = true;
				}
			}
		}
	}


	//// Requires that the version is specified as 1.0.* in AssemblyInfo.cs
	//public static DateTime getProgramCompiledDate() {
	//	Version v = System.Reflection.Assembly.GetEntryAssembly().GetName().Version;
	//	// v.Build = days since Jan. 1, 2000
	//	DateTime date = new DateTime(2000, 1, 1);
	//	return date.AddDays(v.Build);
	//}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
		if (disposing) {
			if (imageList != null) {
				imageList.Dispose();
				imageList = null;
			}
			if (dialogOptions != null) {
				dialogOptions.Dispose();
				dialogOptions = null;
			}
			if (dialogAddFile != null) {
				dialogAddFile.Dispose();
				dialogAddFile = null;
			}
			if (dummyForm != null) {
				dummyForm.Dispose();
				dummyForm = null;
			}
			if (dialogSaveBatch != null) {
				dialogSaveBatch.Dispose();
				dialogSaveBatch = null;
			}
		}
	}

	protected override void OnFormClosing(FormClosingEventArgs e) {
		base.OnFormClosing(e);
		this.BringToFront2();
		var r = MessageBox2.Show(this, "Are you sure you want to quit?", "Confirm Exit", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
		if (r == DialogResult.Cancel)
			e.Cancel = true;
	}

	void buttonClicked(Object src, EventArgs evt) {
		try {
		if (src == btnChangeOutputPath) {
			using (FolderBrowserDialog dlg = new FolderBrowserDialog()) {
				if (dlg.ShowDialog(this) == DialogResult.OK) {
					tbOutputFolder.Text = dlg.SelectedPath;
				}
			}
		}
		else if (src == btnNew) {
			var r = MessageBox2.Show(this, "Do you want to start a new project?", "Clear Project", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (r != DialogResult.OK)
				return;

			NewProject(false);
		}
		else if (src == btnRemove) {
			var n = tvCHM.SelectedNode;
			if (n == null)
				return;

			var r = MessageBox2.Show(this, "Remove the selected node, including its nested nodes?", "Confirm Remove", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (r != DialogResult.OK)
				return;

			RemoveNode(n);
		}
		else if (src == btnSaveBatch) {
			SaveBatch(false);
		}
		else if (src == btnCompile) {
			compileToCHM();
		}
		} catch (Exception ex) {
			RefreshView();
			MessageBox2.ShowError(this, ex);
		} finally {
			this.Cursor = Cursors.Default;
		}
	}

	private void SaveBatch(bool prompt) {
		ProjectList.OutputFile active = null;
		active = projectList.OutputFiles.Where(o => o.ActiveFile).FirstOrDefault();
		if (active == null) {
			active = new ProjectList.OutputFile { ActiveFile = true };
			projectList.OutputFiles.Add(active);
		}

		List<ProjectList.Entry> entries = null;
		try {
			entries = chmDocument.ToProjectOutputFile().Entries;
		} catch (Exception ex) {
			MessageBox2.ShowError(this, new ArgumentException(ex.Message + "\n\nSave batch cancelled."), "Ambigous Topic Name");
			return;
		}
		active.Entries.Clear();
		active.Entries.AddRange(entries);

		if (active.Entries.Count < 1) {
			MessageBox2.Show(this, "Must have at least one file added to save as a batch (.bat) file.", "Save to Batch File", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		if (prompt || projectList.BatchFileSourceFile == null) {
			if (dialogSaveBatch == null) {
				dialogSaveBatch = new SaveFileDialog();
				dialogSaveBatch.Title = "Save to Batch File";
				dialogSaveBatch.Filter = "Batch Files|*.bat";
				dialogSaveBatch.OverwritePrompt = true;
				//dialogSaveBatch.Encoding = CurrentEncoding;

				if (!String.IsNullOrEmpty(SharedSettings.Default.SourceFolderBatch))
					dialogSaveBatch.InitialDirectory = SharedSettings.Default.SourceFolderBatch;
				else
					dialogSaveBatch.InitialDirectory = Settings.DefaultFolder;
			}

			if (dialogSaveBatch.ShowDialog(this.FindForm()) == DialogResult.OK) {
				String fn = dialogSaveBatch.FileName;
				projectList.BatchFileSourceFile = new FileInfo(fn);
			}
		}

		var f = projectList.BatchFileSourceFile;
		if (f != null) {
			SharedSettings.Default.SourceFolderBatch = f.Directory.FullName;
			active.SettingsFile = new FileInfo(settings.FilePath);

			String commandLine = ProjectList.ToCommandLine(projectList);

			Encoding enc = new UTF8Encoding(false);
			bool isAscii = IsASCII(commandLine, enc);
			if (!isAscii)
				commandLine = "@echo off" + Environment.NewLine + "chcp " + enc.CodePage + Environment.NewLine + commandLine + Environment.NewLine + "chcp " + GetCurrentCHCP();

			byte[] data = enc.GetBytes(commandLine);
			File.WriteAllBytes(f.FullName, data);
			addToRecentFileMenu(projectList.BatchFileSourceFile.FullName);
		}
	}

	private static String GetCurrentCHCP() {
		String chcp = "437";
		try {
			using (var pi = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c chcp") { CreateNoWindow =true, UseShellExecute = false, RedirectStandardOutput = true })) {
				using (var s = pi.StandardOutput) {
					var t = s.ReadLine();
					int x = t.LastIndexOf(':');
					String t2 = t.Substring(x+1).Trim();
					if (t2.Length > 0)
						chcp = t2;
				}
			}
		} catch {}
		return chcp;
	}

	private void RemoveNode(TreeNode n) {
		if (n == RootNode)
			NewProject(false);
		else {
			if (settings.DeleteTempFiles)
				DeleteTempFiles2(n);

			// remove n from its parent
			if (n.Parent != null) {
				Object t = n.Parent.Tag;
				if (t is CHMNode)
					((CHMNode) t).ChildNodes.Remove((CHMNode) n.Tag);
				else if (t is CHMDocument)
					((CHMDocument) t).Nodes.Remove((CHMNode) n.Tag);
			}

			n.Remove(); // do this last otherwise n.Parent == null
		}
	}

	void dialogOptions_ButtonClicked(Form2 dialog, DialogResult r, object src) {
		if (r == DialogResult.OK) {
			settings = dialogOptions.Current;
			dialogOptions.Content.ApplyValues(settings);
			settings.Save();

			String title = settings.Title;
			if (!String.IsNullOrEmpty(title))
				tvCHM.Nodes[0].Text = title;

			tbOutputFolder.Text = settings.OutputFolder;
			refreshSettingsMenu(settings.FilePath);
		}
		dialog.Hide();
	}

	private List<String> GetAllFiles() {
		List<String> list = new List<String>();
		GetAllFiles(RootNode, list);
		return list;
	}

	private static void GetAllFiles(TreeNode n, List<String> list) {
		CHMNode c = n.Tag as CHMNode;
		if (c != null && File.Exists(c.Local))
			list.Add(c.Local);

		foreach (TreeNode child in n.Nodes)
			GetAllFiles(child, list);
	}

	private void DeleteTempFiles(TreeNode n) {
		foreach (String f in TempHtmlFiles) {
			if (File.Exists(f))
				File.Delete(f);
		}

		DeleteEmptyDirectories(settings.OutputFolder);
		DeleteTempFiles2(n);
		TempHtmlFiles.Clear();
	}

	public static void DeleteEmptyDirectories(String folder) {
		if (!Directory.Exists(folder))
			return;

		String[] folders = Directory.GetDirectories(folder);
		foreach (String f in folders) {
			String[] files = Directory.GetFiles(f, "*", SearchOption.AllDirectories);
			if (files.Length == 0) {
				Directory.Delete(f, true);
			}
		}
	}

	private static void DeleteTempFiles2(TreeNode n) {
		if (n == null) // initially TopNode is null
			return;

		CHMNode c = n.Tag as CHMNode;
		// TopNode is a CHMDocument
		if (c != null && !String.IsNullOrEmpty(c.Local) && File.Exists(c.Local))
			File.Delete(c.Local);

		foreach (TreeNode child in n.Nodes)
			DeleteTempFiles2(child);
	}

	private void NewProject(bool AddWelcome) {
		WelcomeAdded = AddWelcome;
		if (settings.DeleteTempFiles)
			DeleteTempFiles(RootNode);
		chmDocument.Clear();
		chmDocument.Title = "";
		webBrowser.Navigate("about:blank");
		projectList = new ProjectList();

		if (AddWelcome) {
			String dir = tbOutputFolder.Text;
			String fn = Path.Combine(dir, "Welcome.html");
			FileInfo f = new FileInfo(fn);
			chmDocument.Title = WelcomeTitle;
			chmDocument.Nodes.Add(new CHMNode() { Name="Welcome", Local = fn, SourceFile = fn, WriteEncoding = Encoding.UTF8 });

			if (!Directory.Exists(dir))
				Directory.CreateDirectory(dir);

			if (!File.Exists(fn)) {
				using (var stream = new StreamWriter(File.Create(fn))) {
					stream.WriteLine("<html><head><title>Welcome</title></head><body><span style='font-size:x-large;font-family:Arial'>This is the welcome page.</span></body></html>");
				}
			}
		}

		RefreshView();
	}

	protected override void OnClosed(EventArgs e) {
		base.OnClosed(e);

		SharedSettings ss = SharedSettings.Default;
		var r = this.Bounds;
		if (this.WindowState == FormWindowState.Normal) {
			ss.WindowH = r.Height;
			ss.WindowW = r.Width;
			ss.WindowX = r.X;
			ss.WindowY = r.Y;
		}
		ss.WindowState = this.WindowState;
		ss.FontSize = this.Font.Size;
		ss.Save();

		SharedSettings.Default.Save();
		settings.OutputFolder = tbOutputFolder.Text.Trim();
		settings.Save();

		if (settings.DeleteTempFiles)
			DeleteTempFiles(RootNode);

		FontEx.Dispose();
	}

	private static List<TreeNode> AllNodes(TreeNode n) {
		List<TreeNode> list = new List<TreeNode>();
		if (n != null)
			AllNodes(n, list);
		return list;
	}

	private static void AllNodes(TreeNode n, List<TreeNode> list) {
		list.Add(n);
		foreach (TreeNode n2 in n.Nodes)
			AllNodes(n2, list);
	}

	private class TreeNodeStates {

		TreeView tv = null;
		Hashtable htExpanded = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
		String selectedText = "";
		public TreeNodeStates(TreeView tv) {
			this.tv = tv;
			var n = tv.SelectedNode;
			selectedText = (n == null ? "" : n.Text);
			// remember node state (expanded or not)

			if (tv.Nodes.Count > 0) {
				foreach (TreeNode n2 in AllNodes(tv.Nodes[0])) {
					if (n2.Nodes.Count > 0)
						htExpanded[n2.Text] = n2.IsExpanded;
				}
			}
		}

		public void Restore() {
			List<TreeNode> allNodes = AllNodes(tv.Nodes[0]);
			foreach (TreeNode n2 in allNodes) {
				if (String.Compare(n2.Text, selectedText, true) == 0)
					tv.SelectedNode = n2;

				if (n2.Nodes.Count > 0) {
					Object o = htExpanded[n2.Text];
					if (o == null) {
						n2.Expand();
					}
					else {
						bool b = (bool) o;
						if (b)
							n2.Expand();
						else
							n2.Collapse(true);
					}
				}
			}

			tv.TopNode = (tv.SelectedNode == null ? tv.Nodes[0] : tv.SelectedNode);
		}
	}

	public void RefreshView() {
		TreeNodeStates tns = null;
		bool wasNull = false;
		if (tns == null) {
			tns = new TreeNodeStates(tvCHM);
			wasNull = true;
		}

		this.tvCHM.BeginUpdate();
		this.tvCHM.Nodes.Clear();
		String title = "";
		if (!String.IsNullOrEmpty(settings.CHMOptions.Title))
			title = settings.CHMOptions.Title;
		else
			title = chmDocument.Title;

		TreeNode rootNode = new TreeNode(title);
		rootNode.ImageIndex = 0;
		rootNode.SelectedImageIndex = 0;
		rootNode.Tag = chmDocument;
		AddNodes(chmDocument.Nodes, rootNode);
		tvCHM.Nodes.Add(rootNode);

		if (wasNull) {
			tns.Restore();
			this.tvCHM.EndUpdate();
		}
	}

	private void AddNodes(List<CHMNode> list, TreeNode RootNode) {
		if (list == null || list.Count == 0)
			return;

		foreach (CHMNode node in list) {
			TreeNode n = new TreeNode(node.Name);
			if (node.OriginalFile != null)
				n.BackColor = Color.LightYellow;

			n.Tag = node;
			if (node.ChildNodes.Count > 0)
				n.ImageIndex = 1;
			else
				n.ImageIndex = 2;
			n.SelectedImageIndex = n.ImageIndex;
			RootNode.Nodes.Add(n);
			if (node.ChildNodes.Count > 0)
				AddNodes(node.ChildNodes, n);
		}
	}

	private bool CreateOutputFolder(String folder) {
		if (!Directory.Exists(folder)) {
			var r = MessageBox2.Show(this, "Output folder: " + folder + " does not exist.  Create it?", "Output Folder", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			if (r != DialogResult.OK)
				return false;

			Directory.CreateDirectory(folder);
		}
		return true;
	}

	private bool AppendNewFile(FileInfo file, bool updateRootNodeText) {
		if (!File.Exists(file.FullName)) {
			MessageBox2.Show(this, "File does not exist: " + file.FullName, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		if (RootNode.Text == WelcomeTitle)
			NewProject(false);

		if (RootNode.Nodes.Count == 0 && updateRootNodeText) {
			RootNode.Text = Path.GetFileNameWithoutExtension(file.Name);
		}

		return true;
	}

	public static FileInfo GetNonExistingHtmlFile(FileInfo docFile, String dir) {
		FileInfo htmlFile = null;
		int k = 0;
		do {
			String n = Path.GetFileNameWithoutExtension(docFile.Name) + "_raw"; // added 2015-05-07
			if (k > 0)
				n += "-" + k;
			n += ".html";
			k++;
			htmlFile = new FileInfo(Path.Combine(dir, n));
		} while (File.Exists(htmlFile.FullName));
		return htmlFile;
	}


	private void AppendWordDocument(FileInfo docFile, InsertPosition insertPosition, bool refresh) {
		if (!AppendNewFile(docFile, refresh))
			return;

		String dir = tbOutputFolder.Text.Trim();
		if (!CreateOutputFolder(dir))
			return;

		FileInfo htmlFile = GetNonExistingHtmlFile(docFile, dir);

		this.Cursor = Cursors.WaitCursor;
		Encoding enc = CHMCompile.SaveWordToHtml2(docFile, htmlFile, settings.WordVisible);
		AppendHtmlFile(htmlFile, enc, docFile, insertPosition, refresh);
		this.Cursor = Cursors.Default;
		TempHtmlFiles.Add(htmlFile.FullName);
	}

	private void AppendHtmlFile(FileInfo htmlFile, Encoding readEncoding, FileInfo sourceFile, InsertPosition insertPosition, bool refresh) {
		if (!AppendNewFile(htmlFile, refresh))
			return;

		int x = 0;
		bool wasLocked = false;
		while (FileUtil.IsFileLocked(htmlFile)) {
			wasLocked = true;
			String lockedBy = "";
			try {
				List<System.Diagnostics.Process> list = FileUtil.GetFileLockedByProcesses(htmlFile.FullName);
				foreach (var p in list) {
					if (lockedBy.Length > 0)
						lockedBy += "\n";

					lockedBy += p.ProcessName + " " + p.MainWindowTitle;
				}
			} catch {
				lockedBy = "unknown";
			}

			if (x > 0) {
				String msg = "";
				if (x == 1) {
					msg += "NuHelp is unable to add the file {0} because it is currently locked by the following process:\n{1}\n\n";
					msg += "Would you like to wait a few seconds for the file to become available?";
					msg = String.Format(msg, htmlFile.FullName, lockedBy);
				}
				else {
					msg += "The file is still locked.  Wait a few more seconds?";
				}

				var r = MessageBox2.Show(this, msg, "File Locked", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
				if (r == DialogResult.No) {
					MessageBox2.Show(this, String.Format("User cancelled add since the file {0} is locked by another process:\n{1}", htmlFile.FullName, lockedBy), "Add Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
			}

			System.Threading.Thread.Sleep(3000);
			x++;
		}

		if (wasLocked) {
			MessageBox2.Show(this, "The file is now unlocked and the process will continue.", "Success Unlocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		bool wasEmpty = chmDocument.Nodes.Count == 0;

		TreeNode tn = tvCHM.SelectedNode;
		CHMNode n = null;

		if (tn != null && tn != RootNode) {
			n = (CHMNode) tn.Tag;
		}

		//projectList.Add(sourceFile, (n == null ? null : n.Name), insertPosition);

		settings.OutputFolder = tbOutputFolder.Text.Trim();
		List<String> warningMessages = new List<String>();
		chmDocument.AppendHtmlFile(htmlFile, settings, n, readEncoding, TempHtmlFiles, warningMessages, sourceFile, insertPosition);

		// if multiple files are added one after the other, then the headers and footers need to be updated
		// so that the [next] and [prev] links refer to the correct page.  The first file added has correct
		// links, so only update if > 1 file added to the same project tree.
		bool LinkBlankHeaders = (settings.BlankHeaders == BlankHeader.ShowInTOC || settings.BlankHeaders == BlankHeader.HideInTOCLinked);
		if (!wasEmpty || !LinkBlankHeaders)
			chmDocument.UpdateHeadersAndFooters(LinkBlankHeaders, warningMessages);

		if (warningMessages.Count > 0) {
			StringBuilder sb = new StringBuilder();
			foreach (String s in warningMessages)
				sb.AppendLine(s);
			//MessageBox2.Show(this, sb.ToString(), "Warning Messages", MessageBoxButtons.OK, MessageBoxIcon.Warning);

			TextBox2 tb = new TextBox2(sb.ToString(), "0123456789012345678901234567890123456789\n\n\n\n\n\n\n\n\n") { ReadOnly = true };
			InputBox.Show(this, "Warning Messages", "Warning messages:", MessageBoxButtons.OK, tb, 1, 1, controlBox:true);
		}

		SetCurrentSettings();

		if (refresh)
			RefreshView();
	}

	private TreeNode RootNode {
		get {
			if (tvCHM.Nodes.Count > 0) // always has one node
				return tvCHM.Nodes[0];
			return null;
		}
	}

	private void tvCHM_AfterSelect(object sender, TreeViewEventArgs e) {
		try {
			TreeNode node = tvCHM.SelectedNode;

			btnRemove.Enabled = e.Node != null; // && e.Node != tvCHM.TopNode;

			if (node.Tag is CHMDocument) {
				//CHMDocument doc = (CHMDocument) node.Tag;
				this.webBrowser.Navigate("about:blank");
			}
			else if (node.Tag is CHMNode) {
				CHMNode cn = (CHMNode) node.Tag;
				if (!String.IsNullOrEmpty(cn.Local))
					this.webBrowser.Navigate(cn.Local);
				else
					webBrowser.Navigate("about:blank");
			}
		} catch (Exception ex) {
			MessageBox2.ShowError(this, ex);
		}
	}

	private void compileToCHM() {
		String dir = tbOutputFolder.Text.Trim();
		if (!CreateOutputFolder(dir))
			return;

		String hhcPath = SharedSettings.Default.HHCExePath;
		if (!File.Exists(hhcPath)) {
			MessageBox2.Show(this, "Compiler exe file not found.  Click the 'Options' button and edit EXE path.\nRemember to install htmlhelp.exe in the zip file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		settings.OutputFolder = dir;
		FileInfo cf = CHMCompile.GetCHMFile(settings, chmDocument);

		if (File.Exists(cf.FullName)) {
			// if not automatically overwrite then prompt for delete
			if (!settings.OverwriteExistingCHM) {
				var r = MessageBox2.Show(this, String.Format("Output CHM file {0} already exists.  Overwrite file?", cf.Name), "Confirm Overwrite", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
				if (r != DialogResult.OK)
					return;
			}
			try {
				File.Delete(cf.FullName);
			} catch (Exception ex) {
				throw new ArgumentException(ex.Message);
			}
		}

		SharedSettings ss = SharedSettings.Default;
		String compilerMessages = null;

		bool LinkBlankHeaders = (settings.BlankHeaders == BlankHeader.ShowInTOC || settings.BlankHeaders == BlankHeader.HideInTOCLinked);
		chmDocument.UpdateHeadersAndFooters(LinkBlankHeaders, null);

		this.Cursor = Cursors.WaitCursor;
		bool success = CHMCompile.Compile(chmDocument, settings, ss.HHCExePath, out compilerMessages);
		this.Cursor = Cursors.Default;

		bool hasErrors = (compilerMessages != null && compilerMessages.IndexOf("Error:") >= 0);
		String title = "";
		if (success) {
			if (hasErrors)
				title = "Created with Errors";
			else
				title = "Success: CHM file created";
		}
		else
			title = "Error";

		MessageBox2.Show(this, compilerMessages, title, MessageBoxButtons.OK, (success ? MessageBoxIcon.None : MessageBoxIcon.Error));

		if (success && settings.OpenAfterCompile) {
			if (dummyForm == null)
				dummyForm = new Form();
			Help.ShowHelp(dummyForm, cf.FullName);
		}
	}

	protected override void OnHelpRequested(HelpEventArgs hevent) {
		// pressing F1 when the WebBrowser control is focused causes
		// the OnHelpRequested to be fired twice.  After doing a windows update,
		// the webbrowser no longer passes the F1 event to the form.  So a
		// delegate is added in the constructor.
		TimeSpan ts = DateTime.Now - LastHelpEvent;
		if (ts.TotalSeconds < 1)
			return;
		
		LastHelpEvent = DateTime.Now;

		if (hevent != null) {
			if (hevent.Handled)
				return;

			hevent.Handled = true;
		}

		this.BeginInvoke((Action) delegate {
			// a corrupt ieframe.dll can cause a memory access violation exception.
			// thus, wrap the call to ShowHelp() in a try-catch so the entire program
			// doesn't crash.
			try {
				Control c = ControlHelpEx.FindActiveControl(Form.ActiveForm) ?? this;
				c.ShowHelp();
			} catch (Exception ex) {
				MessageBox2.ShowError(this, ex, "Help Error");
			}
		});
	}

	public String HelpURL {
		get {
			return HelpConstants.MainScreen;
		}
	}

	public String HelpFile {
		get {
			return HelpConstants.CHMFile;
		}
	}

	private static bool IsASCII(String s, Encoding enc) {
		byte[] arr1 = Encoding.ASCII.GetBytes(s);
		byte[] arr2 = enc.GetBytes(s);
		if (arr1.Length != arr2.Length)
			return false;
		for (int i = 0; i < arr1.Length; i++) {
			if (arr1[i] != arr2[i])
				return false;
		}
		return true;
	}
}

}