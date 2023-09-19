namespace MochaMothMedia.DeveloperConsole
{
	public interface IConsoleCommand
    {
        string Name { get; }
        string Usage { get; }
        IConsole DeveloperConsole { get; set; }
        string Execute(ICommandArguments arguments);
        string[] GetHelp(ICommandArguments arguments);
    }
}
