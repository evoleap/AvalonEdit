using System;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Hiding
{
	/// <summary>
	/// Helper class used for <see cref="HidingManager.UpdateHidings"/>.
	/// </summary>
	public class NewHiding : ISegment
	{
		/// <summary>
		/// Gets/Sets the start offset.
		/// </summary>
		public int StartOffset { get; set; }

		/// <summary>
		/// Gets/Sets the end offset.
		/// </summary>
		public int EndOffset { get; set; }

		/// <summary>
		/// Gets/Sets whether the hiding is considered to be a definition.
		/// This has an effect on the 'Show Definitions only' command.
		/// </summary>
		public bool IsDefinition { get; set; }

		/// <summary>
		/// Creates a new NewHiding instance.
		/// </summary>
		public NewHiding()
		{
		}

		/// <summary>
		/// Creates a new NewHiding instance.
		/// </summary>
		public NewHiding(int start, int end)
		{
			if (!(start <= end)) {
				throw new ArgumentException("'start' must be less or equal than 'end'");
			}
				
			this.StartOffset = start;
			this.EndOffset = end;
		}

		int ISegment.Offset {
			get { return this.StartOffset; }
		}

		int ISegment.Length {
			get { return this.EndOffset - this.StartOffset; }
		}
	}
}
