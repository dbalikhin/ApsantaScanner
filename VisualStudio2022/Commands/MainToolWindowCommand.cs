namespace VisualStudio2022
{
    [Command(PackageIds.MainCommand)]
    internal sealed class MainToolWindowCommand : BaseCommand<MainToolWindowCommand>
    {
        protected override Task ExecuteAsync(OleMenuCmdEventArgs e)
        {
            return MainToolWindow.ShowAsync();
        }
    }
}
