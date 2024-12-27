// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Represents a collapsed line section.
	/// Use the Uncollapse() method to uncollapse the section.
	/// </summary>
	public sealed class CollapsedLineSection
	{
		DocumentLine start, end;
		HeightTree heightTree;
		private CollapsedLineSectionType _collapsedLineSectionType;
#if DEBUG
		internal string ID;
		static int nextId;
#else
		const string ID = "";
#endif

		internal CollapsedLineSection(HeightTree heightTree, DocumentLine start, DocumentLine end, CollapsedLineSectionType collapsedLineSectionType)
		{
			this.heightTree = heightTree;
			this.start = start;
			this.end = end;
			this._collapsedLineSectionType = collapsedLineSectionType;
#if DEBUG
			unchecked {
				this.ID = " #" + (nextId++);
			}
#endif
		}

		/// <summary>
		/// Gets if the document line is collapsed.
		/// This property initially is true and turns to false when uncollapsing the section.
		/// </summary>
		public bool IsCollapsed {
			get { return start != null; }
		}

		/// <summary>
		/// Gets the start line of the section.
		/// When the section is uncollapsed or the text containing it is deleted,
		/// this property returns null.
		/// </summary>
		public DocumentLine Start {
			get { return start; }
			internal set { start = value; }
		}

		/// <summary>
		/// Gets the end line of the section.
		/// When the section is uncollapsed or the text containing it is deleted,
		/// this property returns null.
		/// </summary>
		public DocumentLine End {
			get { return end; }
			internal set { end = value; }
		}

		/// <summary>
		/// Uncollapses the section.
		/// This causes the Start and End properties to be set to null!
		/// Does nothing if the section is already uncollapsed.
		/// </summary>
		public void Uncollapse()
		{
			if (start == null)
				return;

			if (!heightTree.IsDisposed) {
				heightTree.Uncollapse(this);
#if DEBUG
				heightTree.CheckProperties();
#endif
			}

			start = null;
			end = null;
		}

		/// <summary>
		/// Gets a string representation of the collapsed section.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", MessageId = "System.Int32.ToString")]
		public override string ToString()
		{
			return "[CollapsedSection" + ID + " Start=" + (start != null ? start.LineNumber.ToString() : "null")
				+ " End=" + (end != null ? end.LineNumber.ToString() : "null") + "]";
		}


		/// <summary>
		/// Gets the CollapsedLineSection type.
		/// </summary>
		public CollapsedLineSectionType CollapsedLineSectionType {
			get { return _collapsedLineSectionType; }
		}

	}

	/// <summary>
	/// Specifies the type of CollapsedLineSection.
	/// </summary>
	public enum CollapsedLineSectionType
	{
		/// <summary>
		/// CollapsedLineSection created as result of folding. 
		/// </summary>
		Folding,
		/// <summary>
		/// CollapsedLineSection created as result of hiding. 
		/// </summary>
		Hiding
	}
}
