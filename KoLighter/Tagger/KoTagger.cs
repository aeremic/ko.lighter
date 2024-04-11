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

		private List<string> taggerStartMatchList;
		private List<string> taggerEndMatchList;

		ITextView View { get; set; }
		ITextBuffer SourceBuffer { get; set; }
		SnapshotPoint? CurrentChar { get; set; }

		public event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		internal KoTagger(ITextView view, ITextBuffer buffer)
		{
			taggerMatchTrigger = '<';

			taggerStartMatchList = new List<string>()
			{
				"<!-- ko if:",
			};

			taggerEndMatchList = new List<string>()
			{
				"<!-- /ko -->",
			};

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
				// TODO: Check next 10 chars to confirm that this is the start of a tag. If this is the start, do search for an end tag
				// TODO: Check next 11 chars to confirm that this is the end of a tag. If this is the end, do search for a start tag
			}
			else if (taggerMatchTrigger == previousCharText)
			{
				// TODO: Check next 10 chars to confirm that this is the start of a tag. If this is the start, do search for an end tag
				// TODO: Check next 11 chars to confirm that this is the end of a tag. If this is the end, do search for a start tag
			}
		}
	}
}
