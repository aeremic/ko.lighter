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
		private char taggerMatchTrigger;

		private char[] taggerStartMatchArray;
		private char[] taggerEndMatchArray;

		ITextView View { get; set; }
		ITextBuffer SourceBuffer { get; set; }
		SnapshotPoint? CurrentChar { get; set; }

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		internal KoTagger(ITextView view, ITextBuffer buffer)
		{
			taggerMatchTrigger = '<';

			taggerStartMatchArray = new char[]
			{
				'<', '!', '-', '-', ' ', 'k', 'o', ' ', 'i', 'f', ':',
			};

			taggerEndMatchArray = new char[]
			{
				'<', '!', '-', '-', ' ', '/', 'k', 'o', ' ', '-', '-', '>',
			};

			View = view;
			SourceBuffer = buffer;
			CurrentChar = null;

			View.Caret.PositionChanged += CaretPositionChanged;
			View.LayoutChanged += ViewLayoutChanged;
		}

		/// <summary>
		/// Event triggered on layout change.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
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

			if (taggerMatchTrigger == currentCharText)
			{
				if (IsCaretAtStartTag(currentChar) && FindMatchingEndTag(currentChar, View.TextViewLines.Count, out pairedSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentChar.Snapshot, 
						currentChar.GetContainingLine().Start.Position, currentChar.GetContainingLine().Length), new TextMarkerTag("blue"));
					yield return new TagSpan<TextMarkerTag>(pairedSpan, new TextMarkerTag("blue"));
				}
				else if (IsCaretAtEndTag(currentChar) && FindMatchingStartTag(currentChar, View.TextViewLines.Count, out pairedSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(currentChar.Snapshot,
						currentChar.GetContainingLine().Start.Position, currentChar.GetContainingLine().Length), new TextMarkerTag("blue"));
					yield return new TagSpan<TextMarkerTag>(pairedSpan, new TextMarkerTag("blue"));
				}
			}
			else if (taggerMatchTrigger == previousCharText)
			{
				if (IsCaretAtStartTag(previousChar) && FindMatchingEndTag(previousChar, View.TextViewLines.Count, out pairedSpan))
				{
					yield return new TagSpan<TextMarkerTag>(new SnapshotSpan(previousChar.Snapshot, 
						previousChar.GetContainingLine().Start.Position, previousChar.GetContainingLine().Length), new TextMarkerTag("blue"));
					yield return new TagSpan<TextMarkerTag>(pairedSpan, new TextMarkerTag("blue"));
				}
				else if (IsCaretAtEndTag(previousChar) && FindMatchingStartTag(previousChar, View.TextViewLines.Count, out pairedSpan))
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
			var lineText = line.GetText();

			var correctCharsCount = 0;

			if (lineText.Length > 11)
			{
				var increment = 0;
				while (increment < lineText.Length)
				{
					if (lineText[increment] == taggerStartMatchArray[correctCharsCount])
					{
						correctCharsCount++;
					}
					else
					{
						correctCharsCount = 0;
					}

					if (correctCharsCount == 11)
					{
						break;
					}

					increment++;
				}
			}

			return correctCharsCount == 11;
		}

		/// <summary>
		/// Check if current line of a caret is a end tag.
		/// </summary>
		/// <param name="currentChar"></param>
		/// <returns></returns>
		private bool IsCaretAtEndTag(SnapshotPoint currentChar)
		{
			var line = currentChar.GetContainingLine();
			var lineText = line.GetText();

			var correctCharsCount = 0;

			if (lineText.Length > 12)
			{
				var increment = 0;
				while (increment < lineText.Length)
				{
					if (lineText[increment] == taggerEndMatchArray[correctCharsCount])
					{
						correctCharsCount++;
					}
					else
					{
						correctCharsCount = 0;
					}

					if (correctCharsCount == 12)
					{
						break;
					}

					increment++;
				}
			}

			return correctCharsCount == 12;
		}

		/// <summary>
		/// Try finding the end tag based on the current snapshot point.
		/// </summary>
		/// <param name="currentChar"></param>
		/// <param name="count"></param>
		/// <param name="pairedSpan"></param>
		/// <returns></returns>
		private bool FindMatchingEndTag(SnapshotPoint currentChar, int count, out SnapshotSpan pairedSpan)
		{
			var line = currentChar.GetContainingLine();
			var lineNumber = line.LineNumber;

			var walkPosition = line.Start.Position;
			var walkStopNumber = currentChar.Snapshot.LineCount - 1;

			var startTagCount = -1; // Ignore initial tag

			if(count > 0)
			{
				walkStopNumber = Math.Min(walkStopNumber, lineNumber + count);
			}

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
		private bool FindMatchingStartTag(SnapshotPoint currentChar, int count, out SnapshotSpan pairedSpan)
		{
			var line = currentChar.GetContainingLine();
			var lineNumber = line.LineNumber;

			var walkPosition = line.Start.Position;
			var walkStopNumber = 0;

			var endTagCount = -1; // Ignore initial tag

			if (count > 0)
			{
				walkStopNumber = Math.Max(walkStopNumber, lineNumber - count);
			}

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
