using System.Diagnostics;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Hiding
{
	/// <summary>
	/// A section that can be hidden.
	/// </summary>
	public sealed class HidingSection : TextSegment
	{
		readonly HidingManager _manager;
		bool _isHidden;
		/// <summary>
		/// Holds CollapsedLineSection for each TextView
		/// </summary>
		internal CollapsedLineSection[] _collapsedSections;

		internal HidingSection(HidingManager manager, int startOffset, int endOffset)
		{
			Debug.Assert(manager != null);
			this._manager = manager;
			this.StartOffset = startOffset;
			this.Length = endOffset - startOffset;
		}

		/// <summary>
		/// Gets/sets if the section is hidden.
		/// </summary>
		public bool IsHidden {
			get { return _isHidden; }
			set {
				if (_isHidden != value) {
					_isHidden = value;
					ResetCollapsedLineSections(); // create/destroy CollapsedLineSections
					//_manager._textViews.ForEach(textView => textView.EnsureVisualLines());
					_manager.Redraw(this);
				}
			}
		}

		internal void ResetCollapsedLineSections()
		{
			// 1. Uncollapse all sections if _isHidden = false
			if (!_isHidden) {
				RemoveCollapsedLineSections();
				return;
			}

			// 2. Get startLine and endLine

			// It is possible that StartOffset/EndOffset get set to invalid values via the property setters in TextSegment,
			// so we coerce those values into the valid range.
			DocumentLine startLine = _manager._document.GetLineByOffset(StartOffset.CoerceValue(0, _manager._document.TextLength));
			DocumentLine endLine = _manager._document.GetLineByOffset(EndOffset.CoerceValue(0, _manager._document.TextLength));
			
			if (startLine.LineNumber > endLine.LineNumber) {
				RemoveCollapsedLineSections();
				return;
			}

			// 3. Populate _collapsedSections
			if (_collapsedSections == null) {
				_collapsedSections = new CollapsedLineSection[_manager._textViews.Count];
			}

			// Loop through all collapsedSections (for each TextView)
			//DocumentLine startLinePlusOne = startLine.NextLine;
			for (int i = 0; i < _collapsedSections.Length; i++) {
				var collapsedSection = _collapsedSections[i];
				if (collapsedSection == null || collapsedSection.Start != startLine || collapsedSection.End != endLine) {
					// recreate this collapsed section
					if (collapsedSection != null) {
						Debug.WriteLine("CollapsedLineSection validation - recreate collapsed section from " + startLine + " to " + endLine);
						collapsedSection.Uncollapse();
					}
					_collapsedSections[i] = _manager._textViews[i].CollapseLines(startLine, endLine,CollapsedLineSectionType.Hiding);
				}
			}
		}

		void RemoveCollapsedLineSections()
		{
			if (_collapsedSections != null) {
				foreach (var collapsedSection in _collapsedSections) {
					if (collapsedSection?.Start != null)
						collapsedSection.Uncollapse();
				}
				_collapsedSections = null;
			}
		}

		protected override void OnSegmentChanged()
		{
			ResetCollapsedLineSections();
			base.OnSegmentChanged();
			// don't redraw if the HidingSection wasn't added to the HidingManager's collection yet
			if (IsConnectedToCollection) {
				//_manager._textViews.ForEach(textView => textView.EnsureVisualLines());
				_manager.Redraw(this);
			}

		}

		/// <summary>
		/// Gets/Sets an additional object associated with this hiding section.
		/// </summary>
		public object Tag { get; set; }
	}
}