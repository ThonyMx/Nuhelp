using Opulos.Core.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Opulos.NüHelp {

public class ExtraFilesPanel : Pane {

	GroupBox2 gbExtraFiles;
	GroupBox2 gbMergeCHMFiles;

	ListPanel panelExtraFiles = new ListPanel();
	ListPanel panelMergeCHMFiles = new ListPanel();

	public ExtraFilesPanel() {
		gbExtraFiles = new GroupBox2(panelExtraFiles, "Extra Files");
		gbMergeCHMFiles = new GroupBox2(panelMergeCHMFiles, "Merge CHM Files");

		RCLayout r0 = new RCLayout(1);
		setLayout(r0);
		r0.OverallInsets = new Insets(10);
		r0.Col[0].expand = 1;
		r0.Col[0].shrink = 1;
		r0.Row[0].expand = 0.5;
		r0.Row[0].shrink = 0.5;
		r0.Row[1].expand = 0.5;
		r0.Row[1].shrink = 0.5;
		r0.ParamDefault.fillX = 1;
		r0.ParamDefault.fillY = 1;

		add(gbExtraFiles);
		add(gbMergeCHMFiles);
	}

	public void InitFields(Settings s) {
		panelExtraFiles.List = s.ExtraFiles;
		panelMergeCHMFiles.List = s.CHMOptions.MergeFiles;
	}

	public void ApplyValues(Settings s) {
		s.ExtraFiles.Clear();
		s.CHMOptions.MergeFiles.Clear();

		s.ExtraFiles.AddRange(panelExtraFiles.List.Cast<String>());
		s.CHMOptions.MergeFiles.AddRange(panelMergeCHMFiles.List.Cast<String>());
	}
}
}
