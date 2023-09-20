namespace MochaMothMedia.DeveloperConsole
{
	public interface IPreProcessor
    {
        string Name { get; }
        string Usage { get; }
        IConsole DeveloperConsole { get; set; }
        string PreProcess(string input);
        string[] GetHelp(ICommandArguments arguments);
    }
}
