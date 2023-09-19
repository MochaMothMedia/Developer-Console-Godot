namespace MochaMothMedia.DeveloperConsole
{
	public interface ICommandArguments
    {
        string TextEntered { get; }
        string CommandName { get; }
        int ArgumentQuantity { get; }

        string GetArgument(int index);
        string GetFlag(char flagName);
    }
}