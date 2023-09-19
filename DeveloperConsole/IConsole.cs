namespace MochaMothMedia.DeveloperConsole
{
	public interface IConsole
    {
        LoggingLevel LoggingLevel { get; set; }
        string MessageLog { get; }
        int BufferCount { get; }

        void SetActive(bool active);
        void RegisterCommand(IConsoleCommand command);
        void RegisterPreProcessCommand(IPreProcessCommand preProcessCommand);
        T GetCommand<T>() where T : class, IConsoleCommand;
        T GetPreProcessCommand<T>() where T : class, IPreProcessCommand;
        string ProcessCommand(string input);
        void PushMessage(string message);
        void PushMessages(string[] messages);
        void PushMessageIndented(string message, int indentLevel = 1);
        void PushMessagesIndented(string[] messages, int indentLevel = 1);
        string GetCommandFromBuffer(int index);
        void ClearLog();
    }
}
