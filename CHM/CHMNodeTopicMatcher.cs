using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Opulos.NüHelp.CHM {

public class CHMNodeTopicMatcher {

	Hashtable htInvarName = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
	Hashtable htHeaderTextExact = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
	Hashtable htHeaderTextApprox = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
	Hashtable htHeaderTextNoSpaces = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
	Hashtable htLocalExact = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
	Hashtable htLocalApprox = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
	Hashtable htLocalNoSpaces = new Hashtable(StringComparer.InvariantCultureIgnoreCase);

	public CHMNodeTopicMatcher() {
	}

	public CHMNode FindNode(String topic) {
		bool isAmbigous = false;
		List<CHMNode> nodes = null;
		return FindNode2(topic, out isAmbigous, out nodes);
	}

	public bool IsAmbiguous(String topic) {
		bool isAmbigous = false;
		List<CHMNode> nodes = null;
		FindNode2(topic, out isAmbigous, out nodes);
		return isAmbigous;
	}

	public List<CHMNode> GetAmbiousNodes(String topic) {
		bool isAmbigous = false;
		List<CHMNode> nodes = null;
		FindNode2(topic, out isAmbigous, out nodes);
		return nodes;

	}

	private CHMNode FindNode2(String topic, out bool isAmbiguous, out List<CHMNode> nodes) {
		CHMNode n = GetNode(htInvarName, topic, out isAmbiguous, out nodes);

		if (n == null)
			n = GetNode(htHeaderTextExact, topic, out isAmbiguous, out nodes);

		if (n == null)
			n = GetNode(htLocalExact, topic, out isAmbiguous, out nodes);

		if (n == null)
			n = GetNode(htHeaderTextApprox, topic.Replace(' ', '-'), out isAmbiguous, out nodes);

		if (n == null)
			n = GetNode(htLocalApprox, topic.Replace(' ', '-'), out isAmbiguous, out nodes);

		if (n == null)
			n = GetNode(htHeaderTextNoSpaces, topic.Replace(" ", "").Replace("-", ""), out isAmbiguous, out nodes);

		if (n == null)
			n = GetNode(htLocalNoSpaces, topic.Replace(" ", "").Replace("-", ""), out isAmbiguous, out nodes);

		return n;
	}

	private static CHMNode GetNode(Hashtable ht, String topic, out bool isAmbiguous, out List<CHMNode> list) {
		isAmbiguous = false;
		list = (List<CHMNode>) ht[topic];
		if (list == null)
			return null;
		isAmbiguous = list.Count > 1;
		return list[0]; // if ambiguous, return the first node
	}

	public void Append(List<CHMNode> list) {
		foreach (CHMNode n in list) {
			String invarName = n.InvariantName;
			Add(n.InvariantName, htInvarName, n, ReplaceMode.Exact);

			Add(n.Name, htHeaderTextExact, n, ReplaceMode.Exact);
			Add(n.Name, htHeaderTextApprox, n, ReplaceMode.ReplaceSpacesWithDashes);
			Add(n.Name, htHeaderTextNoSpaces, n, ReplaceMode.RemoveSpacesAndDashes);

			if (!String.IsNullOrEmpty(n.Local)) {
				String fn = Path.GetFileNameWithoutExtension(n.Local);
				Add(fn, htLocalExact, n, ReplaceMode.Exact);
				Add(fn, htLocalApprox, n, ReplaceMode.ReplaceSpacesWithDashes);
				Add(fn, htLocalNoSpaces, n, ReplaceMode.RemoveSpacesAndDashes);
			}
		}
	}

	private enum ReplaceMode {
		Exact,
		ReplaceSpacesWithDashes,
		RemoveSpacesAndDashes
	}

	private static void Add(String s, Hashtable ht, CHMNode n, ReplaceMode rm) {
		if (String.IsNullOrEmpty(s))
			return;

		if (rm == ReplaceMode.RemoveSpacesAndDashes)
			s = s.Replace(" ", "").Replace("-", "");
		else if (rm == ReplaceMode.ReplaceSpacesWithDashes)
			s = s.Replace(' ', '-');

		List<CHMNode> list = (List<CHMNode>) ht[s];
		if (list == null) {
			list = new List<CHMNode>();
			ht[s] = list;
		}
		list.Add(n);
	}
}

}