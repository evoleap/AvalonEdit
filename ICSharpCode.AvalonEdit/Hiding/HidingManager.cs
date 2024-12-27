using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Hiding
{
	/// <summary>
	/// Manages hidden sections for a TextDocument without displaying placeholders or margins.
	/// </summary>
	public class HidingManager : IWeakEventListener
	{
		internal readonly TextDocument _document;

		internal readonly List<TextView> _textViews = new List<TextView>();
		readonly TextSegmentCollection<HidingSection> _hidings;
		bool _isFirstUpdate = true;

		/// <summary>
		/// Creates a new HidingManager instance.
		/// </summary>
		public HidingManager(TextDocument document)
		{
			if (document == null) {
				throw new ArgumentNullException("document");
			}

			_document = document;
			_hidings = new TextSegmentCollection<HidingSection>();
			document.VerifyAccess();
			TextDocumentWeakEventManager.Changed.AddListener(document, this);
		}

		#region ReceiveWeakEvent - Handle document changes (responsible for existing hidings)

		/// <inheritdoc cref="IWeakEventListener.ReceiveWeakEvent"/>
		protected virtual bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			if (managerType == typeof(TextDocumentWeakEventManager.Changed)) {
				OnDocumentChanged((DocumentChangeEventArgs)e);
				return true;
			}
			return false;
		}

		bool IWeakEventListener.ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
		{
			return ReceiveWeakEvent(managerType, sender, e);
		}

		void OnDocumentChanged(DocumentChangeEventArgs e)
		{
			_hidings.UpdateOffsets(e);
			int newEndOffset = e.Offset + e.InsertionLength;
			// extend end offset to the end of the line (including delimiter)
			var endLine = _document.GetLineByOffset(newEndOffset);
			newEndOffset = endLine.Offset + endLine.TotalLength;
			foreach (var affectedHiding in _hidings.FindOverlappingSegments(e.Offset, newEndOffset - e.Offset)) {
				if (affectedHiding.Length == 0) {
					RemoveHiding(affectedHiding);
				} else {
					affectedHiding.ResetCollapsedLineSections();
				}
			}
		}
		#endregion

		#region Manage TextViews
		
		internal void AddToTextView(TextView textView)
		{
			if (textView == null || _textViews.Contains(textView)) {
				throw new ArgumentException();
			}

			_textViews.Add(textView);
			foreach (HidingSection hs in _hidings) {
				if (hs._collapsedSections != null) {
					Array.Resize(ref hs._collapsedSections, _textViews.Count);
					hs.ResetCollapsedLineSections();
				}
			}
		}

		internal void RemoveFromTextView(TextView textView)
		{
			int pos = _textViews.IndexOf(textView);
			if (pos < 0)
				throw new ArgumentException();
			_textViews.RemoveAt(pos);
			foreach (HidingSection hs in _hidings) {
				if (hs._collapsedSections != null) {
					var c = new CollapsedLineSection[_textViews.Count];
					Array.Copy(hs._collapsedSections, 0, c, 0, pos);
					hs._collapsedSections[pos].Uncollapse();
					Array.Copy(hs._collapsedSections, pos + 1, c, pos, c.Length - pos);
					hs._collapsedSections = c;
				}
			}
		}

		internal void Redraw()
		{
			foreach (TextView textView in _textViews) {
				textView.Redraw();
			}
		}

		internal void Redraw(HidingSection hs)
		{
			foreach (TextView textView in _textViews) {
				textView.Redraw(hs);
			}	
		}
		#endregion

		#region Create / Remove / Clear
		
		/// <summary>
		/// Creates a hiding for the specified text section.
		/// </summary>
		public HidingSection CreateHiding(int startOffset, int endOffset)
		{
			if (startOffset > endOffset) {
				throw new ArgumentException("startOffset must be greater or equal to endOffset");
			}
			if (startOffset < 0 || endOffset > _document.TextLength) {
				throw new ArgumentException("Hiding must be within document boundary");
			}
				
			HidingSection hs = new HidingSection(this, startOffset, endOffset);
			_hidings.Add(hs);
			Redraw(hs);
			return hs;
		}

		/// <summary>
		/// Removes a hiding section from this manager.
		/// </summary>
		public void RemoveHiding(HidingSection hs)
		{
			if (hs == null) {
				throw new ArgumentNullException("hs");
			}

			hs.IsHidden = false;
			_hidings.Remove(hs);
			Redraw(hs);
		}

		/// <summary>
		/// Removes all hiding sections.
		/// </summary>
		public void Clear()
		{
			_document.VerifyAccess();
			foreach (HidingSection hs in _hidings) {
				hs.IsHidden = false;
			}
				
			_hidings.Clear();
			Redraw();
		}
		#endregion

		#region Get...Hiding

		/// <summary>
		/// Gets all hidings in this manager.
		/// The hidings are returned sorted by start offset;
		/// for multiple hidings at the same offset the order is undefined.
		/// </summary>
		public IEnumerable<HidingSection> AllHidings {
			get { return _hidings; }
		}

		/// <summary>
		/// Gets the first offset greater or equal to <paramref name="startOffset"/> where a hidden hiding starts.
		/// Returns -1 if there are no hidings after <paramref name="startOffset"/>.
		/// </summary>
		public int GetNextHiddenHidingStart(int startOffset)
		{
			HidingSection hs = _hidings.FindFirstSegmentWithStartAfter(startOffset);
			while (hs != null && !hs.IsHidden) {
				hs = _hidings.GetNextSegment(hs);
			}
				
			return hs != null ? hs.StartOffset : -1;
		}

		/// <summary>
		/// Gets the first hiding with a <see cref="TextSegment.StartOffset"/> greater or equal to
		/// <paramref name="startOffset"/>.
		/// Returns null if there are no hidings after <paramref name="startOffset"/>.
		/// </summary>
		public HidingSection GetNextHiding(int startOffset)
		{
			// TODO: returns the longest hiding instead of any hiding at the first position after startOffset
			return _hidings.FindFirstSegmentWithStartAfter(startOffset);
		}

		/// <summary>
		/// Gets all hidings that start exactly at <paramref name="startOffset"/>.
		/// </summary>
		public ReadOnlyCollection<HidingSection> GetHidingsAt(int startOffset)
		{
			List<HidingSection> result = new List<HidingSection>();
			HidingSection hs = _hidings.FindFirstSegmentWithStartAfter(startOffset);
			while (hs != null && hs.StartOffset == startOffset) {
				result.Add(hs);
				hs = _hidings.GetNextSegment(hs);
			}
			return result.AsReadOnly();
		}

		/// <summary>
		/// Gets all hidings that contain <paramref name="offset" />.
		/// </summary>
		public ReadOnlyCollection<HidingSection> GetHidingsContaining(int offset)
		{
			return _hidings.FindSegmentsContaining(offset);
		}
		#endregion

		#region UpdateHidings

		/// <summary>
		/// Updates the hidings in this <see cref="HidingManager"/> using the given new hidings.
		/// This method will try to detect which new hidings correspond to which existing hidings; and will keep the state
		/// (<see cref="HidingSection.IsHidden"/>) for existing hidings.
		/// Make sure that hidings do not overlap with each other or with foldings.
		/// </summary>
		/// <param name="newHidings">The new set of hidings. These must be sorted by starting offset.</param>
		/// <param name="firstErrorOffset">The first position of a parse error. Existing hidings starting after
		/// this offset will be kept even if they don't appear in <paramref name="newHidings"/>.
		/// Use -1 for this parameter if there were no parse errors.</param>
		public void UpdateHidings(IEnumerable<NewHiding> newHidings, int firstErrorOffset)
		{
			// TODO: this can be simplified, since we should always reset hidings completely
			if (newHidings == null) {
				throw new ArgumentNullException("newHidings");
			}
				
			if (firstErrorOffset < 0) {
				firstErrorOffset = int.MaxValue;
			}

			var oldHidings = this.AllHidings.ToArray();
			int oldHidingIndex = 0;
			int previousStartOffset = 0;

			// merge new hidings into old hidings so that sections keep being collapsed
			// both oldHidings and newHidings are sorted by start offset
			foreach (NewHiding newHiding in newHidings) {
				// ensure newHidings are sorted correctly
				if (newHiding.StartOffset < previousStartOffset) {
					throw new ArgumentException("newHidings must be sorted by start offset");
				}
					
				previousStartOffset = newHiding.StartOffset;

				int startOffset = newHiding.StartOffset.CoerceValue(0, _document.TextLength);
				int endOffset = newHiding.EndOffset.CoerceValue(0, _document.TextLength);

				if (newHiding.StartOffset == newHiding.EndOffset) {
					continue; // ignore zero-length hidings
				}
					
				// remove old hidings that were skipped
				while (oldHidingIndex < oldHidings.Length && newHiding.StartOffset > oldHidings[oldHidingIndex].StartOffset) {
					this.RemoveHiding(oldHidings[oldHidingIndex++]);
				}
				HidingSection section;
				// reuse current hiding if its matching:
				if (oldHidingIndex < oldHidings.Length && newHiding.StartOffset == oldHidings[oldHidingIndex].StartOffset) {
					section = oldHidings[oldHidingIndex++];
					section.Length = newHiding.EndOffset - newHiding.StartOffset;
				} else {
					// no matching current hiding; create a new one:
					section = this.CreateHiding(newHiding.StartOffset, newHiding.EndOffset);
					/*
					// auto-close #regions only when opening the document
					if (_isFirstUpdate) {
						section.IsHidden = newHiding.DefaultHidden;
					}
					*/
					section.IsHidden = true;	
					section.Tag = newHiding;
				}
			}
			_isFirstUpdate = false;
			// remove all outstanding old hidings:
			while (oldHidingIndex < oldHidings.Length) {
				HidingSection oldSection = oldHidings[oldHidingIndex++];
				if (oldSection.StartOffset >= firstErrorOffset)
					break;
				this.RemoveHiding(oldSection);
			}
		}
		#endregion

		#region Install
		
		/// <summary>
		/// Adds Hiding support to the specified text area.
		/// Warning: The hiding manager is only valid for the text area's current document. The hiding manager
		/// must be uninstalled before the text area is bound to a different document.
		/// </summary>
		/// <returns>The <see cref="HidingManager"/> that manages the list of hidings inside the text area.</returns>
		public static HidingManager Install(TextArea textArea)
		{
			if (textArea == null) {
				throw new ArgumentNullException("textArea");
			}
				
			return new HidingManagerInstallation(textArea);
		}

		/// <summary>
		/// Uninstalls the hiding manager.
		/// </summary>
		/// <exception cref="ArgumentException">The specified manager was not created using <see cref="Install"/>.</exception>
		public static void Uninstall(HidingManager manager)
		{
			if (manager == null) {
				throw new ArgumentNullException("manager");
			}
				
			HidingManagerInstallation installation = manager as HidingManagerInstallation;
			if (installation != null) {
				installation.Uninstall();
			} else {
				throw new ArgumentException("HidingManager was not created using HidingManager.Install");
			}
		}

		sealed class HidingManagerInstallation : HidingManager
		{
			TextArea textArea;
			HidingElementGenerator generator;

			public HidingManagerInstallation(TextArea textArea) : base(textArea.Document)
			{
				this.textArea = textArea;
				generator = new HidingElementGenerator() { HidingManager = this };

				textArea.TextView.Services.AddService(typeof(HidingManager), this);
				textArea.TextView.ElementGenerators.Insert(0, generator); // HACK: hiding only works correctly when it has highest priority
			}

			public void Uninstall()
			{
				Clear();
				if (textArea != null) {
					textArea.TextView.ElementGenerators.Remove(generator);
					textArea.TextView.Services.RemoveService(typeof(HidingManager));
					textArea = null;
					generator = null;
				}
			}

		}
		#endregion
	}
}
