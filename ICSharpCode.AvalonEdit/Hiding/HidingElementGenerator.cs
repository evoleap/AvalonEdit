using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Hiding
{
	/// <summary>
	/// A <see cref="VisualLineElementGenerator"/> that produces line elements for hidden <see cref="HidingSection"/>s.
	/// </summary>
	public sealed class HidingElementGenerator : VisualLineElementGenerator, ITextViewConnect
	{
		readonly List<TextView> _textViews = new List<TextView>();
		HidingManager _hidingManager;

		#region HidingManager property / connecting with TextView
		
		/// <summary>
		/// Gets/Sets the hiding manager from which the hidings should be shown.
		/// </summary>
		public HidingManager HidingManager {
			get {
				return _hidingManager;
			}
			set {
				if (_hidingManager != value) {
					if (_hidingManager != null) {
						foreach (TextView v in _textViews)
							_hidingManager.RemoveFromTextView(v);
					}
					_hidingManager = value;
					if (_hidingManager != null) {
						foreach (TextView v in _textViews)
							_hidingManager.AddToTextView(v);
					}
				}
			}
		}

		void ITextViewConnect.AddToTextView(TextView textView)
		{
			_textViews.Add(textView);
			if (_hidingManager != null)
				_hidingManager.AddToTextView(textView);
		}

		void ITextViewConnect.RemoveFromTextView(TextView textView)
		{
			_textViews.Remove(textView);
			if (_hidingManager != null)
				_hidingManager.RemoveFromTextView(textView);
		}
		#endregion

		public override void StartGeneration(ITextRunConstructionContext context)
		{
			base.StartGeneration(context);
			if (_hidingManager != null) {
				if (!_hidingManager._textViews.Contains(context.TextView)) {
					throw new ArgumentException("Invalid TextView");
				}
				if (context.Document != _hidingManager._document) {
					throw new ArgumentException("Invalid document");
				}
			}
		}

		public override int GetFirstInterestedOffset(int startOffset)
		{
			if (_hidingManager != null) {
				return _hidingManager.GetNextHiddenHidingStart(startOffset);
			} else {
				return -1;
			}
		}

		/// <inheritdoc/>
		public override VisualLineElement ConstructElement(int offset)
		{
			return null;
		}
	}
}
