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

			//taggerStartMatchList = new List<string>()
			//{
			//	"<!-- ko if:",
			//};

			//taggerEndMatchList = new List<string>()
			//{
			//	"<!-- /ko -->",
			//};

			View = view;
			SourceBuffer = buffer;
			CurrentChar = null;

			View.Caret.PositionChanged += CaretPositionChanged;
			View.LayoutChanged += ViewLayoutChanged;
		}

		private void ViewLayoutChanged(object sender, TextViewLayoutChangedEventArgs e)
		{
			// is there a really a change?
			if (e.NewSnapshot != e.OldSnapshot)
			{
				UpdateAtCaretPosition(View.Caret.Position);
			}
		}

		private void CaretPositionChanged(object sender, CaretPositionChangedEventArgs e)
		{
			UpdateAtCaretPosition(e.NewPosition);
		}

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
				if (IsCaretAtStartTag(currentChar))
				{
					// TODO: Walk thru and search for pair
				}
				else if (IsCaretAtEndTag(currentChar))
				{
					// TODO: Walk thru and search for pair
				}
			}
			else if (taggerMatchTrigger == previousCharText)
			{
				if (IsCaretAtStartTag(previousChar))
				{
					// TODO: Walk thru and search for pair
				}
				else if (IsCaretAtEndTag(previousChar))
				{
					// TODO: Walk thru and search for pair
				}
			}
		}

		private bool IsCaretAtStartTag(SnapshotPoint startPoint)
		{
			var line = startPoint.GetContainingLine();
			var lineText = line.GetText();
			var position = startPoint.Position - line.Start.Position;

			var correctCharsCount = 0;

			if (position + 11 <= lineText.Length)
			{
				for (var i = 0; i < 11; i++)
				{
					if (lineText[position + i] != taggerStartMatchArray[i])
					{
						break;
					}

					correctCharsCount++;
				}
			}

			if (correctCharsCount == 11)
			{
				return true;
			}

			return false;
		}

		private bool IsCaretAtEndTag(SnapshotPoint startPoint)
		{
			var line = startPoint.GetContainingLine();
			var lineText = line.GetText();
			var position = startPoint.Position - line.Start.Position;

			var correctCharsCount = 0;

			if (position + 12 <= lineText.Length)
			{
				for (var i = 0; i < 12; i++)
				{
					if (lineText[position + i] != taggerEndMatchArray[i])
					{
						break;
					}

					correctCharsCount++;
				}
			}

			if (correctCharsCount == 12)
			{
				return true;
			}

			return false;
		}
	}
}
