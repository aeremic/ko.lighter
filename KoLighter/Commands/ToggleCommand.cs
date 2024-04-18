namespace KoLighter
{
	[Command(PackageIds.Toggle)]
	public class ToggleCommand : BaseCommand<ToggleCommand>
	{
		protected override void BeforeQueryStatus(EventArgs e)
		{
			Command.Checked = General.Instance.IsEnabled;
		}

		protected override async Task ExecuteAsync(OleMenuCmdEventArgs e)
		{
			var settings = await General.GetLiveInstanceAsync();

			settings.IsEnabled = !settings.IsEnabled;

			await settings.SaveAsync();
		}
	}
}
