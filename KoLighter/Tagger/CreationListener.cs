﻿using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoLighter.Tagger
{
	[Export(typeof(IViewTaggerProvider))]
	[ContentType("text")]
	[ContentType("Razor")]
	[TagType(typeof(TextMarkerTag))]
	public class CreationListener : IViewTaggerProvider
	{
		public ITagger<T> CreateTagger<T>(ITextView textView, ITextBuffer buffer) where T : ITag
		{
			if (textView == null)
			{
				return null;
			}

			return new KoTagger(textView, buffer) as ITagger<T>;
		}
	}
}
