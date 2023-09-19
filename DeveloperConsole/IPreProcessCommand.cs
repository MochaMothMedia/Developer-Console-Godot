namespace MochaMothMedia.DeveloperConsole
{
	public interface IPreProcessCommand
    {
        string Name { get; }
        string Usage { get; }
        IConsole DeveloperConsole { get; set; }
        string PreProcess(string input);
        string[] GetHelp(ICommandArguments arguments);
    }
}
