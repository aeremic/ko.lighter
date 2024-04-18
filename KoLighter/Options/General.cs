using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KoLighter
{
	internal partial class OptionsProvider
	{
		// Register the options with this attribute on your package class:
		// [ProvideOptionPage(typeof(OptionsProvider.GeneralOptions), "KoLighter", "General", 0, 0, true, SupportsProfiles = true)]
		[ComVisible(true)]
		public class GeneralOptions : BaseOptionPage<General> { }
	}

	public class General : BaseOptionModel<General>
	{
		[Category("My category")]
		[DisplayName("My Option")]
		[Description("An informative description.")]
		[DefaultValue(true)]
		public bool IsEnabled { get; set; } = true;
	}
}
