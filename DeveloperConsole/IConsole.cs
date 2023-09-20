namespace MochaMothMedia.DeveloperConsole
{
	public interface IConsole
    {
        LoggingLevel LoggingLevel { get; set; }
        string MessageLog { get; }
        int BufferCount { get; }

        void SetActive(bool active);
        void RegisterCommand(IConsoleCommand command);
        void RegisterPreProcessCommand(IPreProcessor preProcessCommand);
        T GetCommand<T>() where T : class, IConsoleCommand;
        T GetPreProcessCommand<T>() where T : class, IPreProcessor;
        string ProcessCommand(string input);
        void PushMessage(string message, LoggingLevel level = LoggingLevel.Message);
        void PushMessages(string[] messages, LoggingLevel level = LoggingLevel.Message);
        void PushMessageIndented(string message, LoggingLevel level = LoggingLevel.Message, int indentLevel = 1);
        void PushMessagesIndented(string[] messages, LoggingLevel level = LoggingLevel.Message, int indentLevel = 1);
        string GetCommandFromBuffer(int index);
        void ClearLog();
    }
}
