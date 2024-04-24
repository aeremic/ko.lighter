using KoLighter.Common;
using KoLighter.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoLighter.Tagger
{
	internal class KoTagger : ITagger<TextMarkerTag>
	{
		private readonly bool _isEnabled;
		private readonly char _taggerMatchTrigger;

		ITextView View { get; set; }
		ITextBuffer SourceBuffer { get; set; }
		SnapshotPoint? CurrentChar { get; set; }

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		internal KoTagger(ITextView view, ITextBuffer buffer)
		{
			_isEnabled = IsEnabled(General.Instance);
			_taggerMatchTrigger = '<';

			View = view;
			SourceBuffer = buffer;
			CurrentChar = null;

			View.Caret.PositionChanged += CaretPositionChanged;
			View.LayoutChanged += ViewLayoutChanged;
		}

		/// <summary>
		/// Is enabled value field.
		/// </summary>
		/// <param name="settings"></param>
		/// <returns></returns>
		private static bool IsEnabled(General settings) => settings.IsEnabled;

		/// <summary>
		/// Event triggered on layout change.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			if (!_isEnabled)
			{
				return;
			}

			// is there a really a change?
			if (e.NewSnapshot != e.OldSnapshot)
			{
				UpdateAtCaretPosition(View.Caret.Position);
			}
		}

		/// <summary>
		/// Event triggered on caret position change.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
		{
			if (!_isEnabled)
			{
				return;
			}

			UpdateAtCaretPosition(e.NewPosition);
		}

		/// <summary>
		/// Update caret position and trigger GetTags method.
		/// </summary>
		/// <param name="caretPosition"></param>
		private void UpdateAtCaretPosition(CaretPosition caretPosition)
		{
			CurrentChar = caretPosition.Point.GetPoint(SourceBuffer, caretPosition.Affinity);

			if (!CurrentChar.HasValue)
			{
				return;
			}

			var tempEvent = TagsChanged;
			if (tempEvent != null)
			{
				tempEvent(this, new SnapshotSpanEventArgs(new SnapshotSpan(SourceBuffer.CurrentSnapshot, 0, SourceBuffer.CurrentSnapshot.Length)));
			}
		}

		/// <summary>
		/// ITagger method implementation.
		/// </summary>
		/// <param name="spans"></param>
		/// <returns></returns>
		public IEnumerable<ITagSpan<TextMarkerTag>> GetTags(NormalizedSnapshotSpanCollection spans)
		{
			// No content in buffer.
			if (spans.Count == 0)
			{
				yield break;
			}

			// Check if the current snapshot wasn't initialized or at the end of a buffer.
			if (!CurrentChar.HasValue || CurrentChar.Value.Position >= CurrentChar.Value.Snapshot.Length)
			{
				yield break;
			}

			var currentChar = CurrentChar.Value;
			if (spans[0].Snapshot != currentChar.Snapshot)
			{
				currentChar = currentChar.TranslateTo(spans[0].Snapshot, PointTrackingMode.Positive);
			}

			var currentCharText = currentChar.GetChar();

			var previousChar = currentChar == 0 ? currentChar : currentChar - 1;
			var previousCharText = previousChar.GetChar();

			var pairedSpan = new SnapshotSpan();

			if (_taggerMatchTrigger == currentCharText)
			{
				if (IsCaretAtStartTag(currentChar) && FindMatchingEndTag(currentChar, out pairedSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentChar.Snapshot,
						currentChar.GetContainingLine().Start.Position, currentChar.GetContainingLine().Length), new TextMarkerTag("blue"));
					yield return new TagSpan<TextMarkerTag>(pairedSpan, new TextMarkerTag("blue"));
				}
				else if (IsCaretAtEndTag(currentChar) && FindMatchingStartTag(currentChar, out pairedSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentChar.Snapshot,
						currentChar.GetContainingLine().Start.Position, currentChar.GetContainingLine().Length), new TextMarkerTag("blue"));
					yield return new TagSpan<TextMarkerTag>(pairedSpan, new TextMarkerTag("blue"));
				}
			}
			else if (_taggerMatchTrigger == previousCharText)
			{
				if (IsCaretAtStartTag(previousChar) && FindMatchingEndTag(previousChar, out pairedSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(previousChar.Snapshot,
						previousChar.GetContainingLine().Start.Position, previousChar.GetContainingLine().Length), new TextMarkerTag("blue"));
					yield return new TagSpan<TextMarkerTag>(pairedSpan, new TextMarkerTag("blue"));
				}
				else if (IsCaretAtEndTag(previousChar) && FindMatchingStartTag(previousChar, out pairedSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(previousChar.Snapshot,
						previousChar.GetContainingLine().Start.Position, previousChar.GetContainingLine().Length), new TextMarkerTag("blue"));
					yield return new TagSpan<TextMarkerTag>(pairedSpan, new TextMarkerTag("blue"));
				}
			}
		}

		/// <summary>
		/// Check if current line of a caret is a start tag.
		/// </summary>
		/// <param name="currentChar"></param>
		/// <returns></returns>
		private bool IsCaretAtStartTag(SnapshotPoint currentChar)
		{
			var line = currentChar.GetContainingLine();
			var lineText = line.GetText().RemoveWhiteSpaces();

			return lineText.Contains(Constants.KoIfStartTag) || lineText.Contains(Constants.KoIfNotStartTag) 
				|| lineText.Contains(Constants.KoTemplateStartTag) || lineText.Contains(Constants.KoForEachStartTag);
		}

		/// <summary>
		/// Check if current line of a caret is a end tag.
		/// </summary>
		/// <param name="currentChar"></param>
		/// <returns></returns>
		private bool IsCaretAtEndTag(SnapshotPoint currentChar)
		{
			var line = currentChar.GetContainingLine();
			var lineText = line.GetText().RemoveWhiteSpaces();

			return lineText.Contains(Constants.KoEndTag);
		}

		/// <summary>
		/// Try finding the end tag based on the current snapshot point.
		/// </summary>
		/// <param name="currentChar"></param>
		/// <param name="count"></param>
		/// <param name="pairedSpan"></param>
		/// <returns></returns>
		private bool FindMatchingEndTag(SnapshotPoint currentChar, out SnapshotSpan pairedSpan)
		{
			var line = currentChar.GetContainingLine();
			var lineNumber = line.LineNumber;

			var walkPosition = line.Start.Position;
			var walkStopNumber = currentChar.Snapshot.LineCount - 1;

			var startTagCount = -1; // Ignore initial tag

			pairedSpan = new SnapshotSpan(currentChar.Snapshot, 1, 1);

			while (true)
			{
				if (IsCaretAtEndTag(new SnapshotPoint(currentChar.Snapshot, walkPosition)))
				{
					if (startTagCount > 0)
					{
						startTagCount--;
					}
					else
					{
						pairedSpan = new SnapshotSpan(currentChar.Snapshot, walkPosition, line.Length);

						return true;
					}
				}
				else if (IsCaretAtStartTag(new SnapshotPoint(currentChar.Snapshot, walkPosition)))
				{
					startTagCount++;
				}

				if (++lineNumber > walkStopNumber)
				{
					break;
				}

				line = line.Snapshot.GetLineFromLineNumber(lineNumber);
				walkPosition = line.Start.Position;
			}

			return false;
		}

		/// <summary>
		/// Try finding the start tag based on the current snapshot point.
		/// </summary>
		/// <param name="currentChar"></param>
		/// <param name="count"></param>
		/// <param name="pairedSpan"></param>
		/// <returns></returns>
		private bool FindMatchingStartTag(SnapshotPoint currentChar, out SnapshotSpan pairedSpan)
		{
			var line = currentChar.GetContainingLine();
			var lineNumber = line.LineNumber;

			var walkPosition = line.Start.Position;
			var walkStopNumber = 0;

			var endTagCount = -1; // Ignore initial tag

			pairedSpan = new SnapshotSpan(currentChar.Snapshot, 1, 1);

			while (true)
			{
				if (IsCaretAtStartTag(new SnapshotPoint(currentChar.Snapshot, walkPosition)))
				{
					if (endTagCount > 0)
					{
						endTagCount--;
					}
					else
					{
						pairedSpan = new SnapshotSpan(currentChar.Snapshot, walkPosition, line.Length);

						return true;
					}
				}
				else if (IsCaretAtEndTag(new SnapshotPoint(currentChar.Snapshot, walkPosition)))
				{
					endTagCount++;
				}

				if (--lineNumber < walkStopNumber)
				{
					break;
				}

				line = line.Snapshot.GetLineFromLineNumber(lineNumber);
				walkPosition = line.Start.Position;
			}

			return false;
		}
	}
}
