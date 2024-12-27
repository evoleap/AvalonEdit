using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Hiding;

namespace AvalonEdit.Sample
{
	public class DemoHidingStrategyBelow20
	{
		public void UpdateHidings(HidingManager manager, TextDocument document)
		{
			if (document == null || manager == null)
				return;

			List<NewHiding> newHidings = new List<NewHiding>();
			if (document.LineCount >= 20) {
				var start = document.GetLineByNumber(20).Offset;
				var end = document.TextLength;
				newHidings.Add(new NewHiding(start, end));
			}

			manager.UpdateHidings(newHidings, -1);
		}
	}

	public class DemoHidingStrategyWholeDocument
	{
		public void UpdateHidings(HidingManager manager, TextDocument document)
		{
			if (document == null || manager == null)
				return;

			List<NewHiding> newHidings = new List<NewHiding>();
			if (document.LineCount > 0) {
				var start = document.GetLineByNumber(1).Offset;
				var end = document.TextLength;
				newHidings.Add(new NewHiding(start, end));
			}

			manager.UpdateHidings(newHidings, -1);
		}
	}

	public class DemoHidingStrategyFirst20
	{
		public void UpdateHidings(HidingManager manager, TextDocument document)
		{
			if (document == null || manager == null)
				return;

			List<NewHiding> newHidings = new List<NewHiding>();
			if (document.LineCount > 20) {
				var start = document.GetLineByNumber(1).Offset;
				var end = document.GetLineByNumber(20).EndOffset;
				newHidings.Add(new NewHiding(start, end));
			}

			manager.UpdateHidings(newHidings, -1);
		}
	}
}
