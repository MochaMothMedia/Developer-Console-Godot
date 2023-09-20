using Godot;
using System;
using System.Collections.Generic;
using System.IO;

namespace MochaMothMedia.DeveloperConsole.Consoles
{
	internal class SimpleDeveloperConsole : IConsole, IDisposable
	{
		#region Public Properties
		public LoggingLevel LoggingLevel { get; set; }
		public string MessageLog { get; private set; }
		public int BufferCount => _commandBuffer == null ? 0 : _commandBuffer.Count;
		#endregion

		#region Private Properties
		StreamWriter LogFileWriter
		{
			get
			{
				if (_logFileWriter == null)
				{
					DateTime now = DateTime.UtcNow;
					string folderPath = ProjectSettings.LocalizePath($"user://Logging Output");
					string filePath = $"{folderPath}/{now.Year:0000}-{now.Month:00}-{now.Day:00}-{now.Hour:00}-{now.Minute:00}-{now.Second:00}-{now.Millisecond:000}.txt";

					if (!Directory.Exists(folderPath))
						Directory.CreateDirectory(folderPath);

					if (!File.Exists(filePath))
					{
						FileStream newFileStream = File.Create(filePath);
						newFileStream.Close();
						newFileStream.Dispose();
					}

					_logFileWriter = new StreamWriter(filePath, true);
				}

				return _logFileWriter;
			}
		}
		#endregion

		#region Fields
		[Export] OverrideRule _commandNameOverrideRule = OverrideRule.Ignore;
		[Export] SpacingStyle _spacingStyle = SpacingStyle.Spacious;
		[Export] LoggingLevel _loggingLevel = LoggingLevelHelpers.LoggingLevelAll;
		[Export] IGetCommandArguments _getCommandArguments;
		[Export] int _indentSize = 8;
		[Export] int _maxVisibleCharacters = 8000;

		Dictionary<string, IConsoleCommand> _commands = new Dictionary<string, IConsoleCommand>();
		List<IPreProcessor> _preProcessors = new List<IPreProcessor>();
		List<string> _commandBuffer = new List<string>();
		StreamWriter _logFileWriter;
		int _indent = 0;
		#endregion

		#region Utilities
		void Indent(int indentLevel = 1) => _indent += _indentSize * indentLevel;
		void Dedent(int indentLevel = 1) => _indent -= _indentSize * indentLevel;

		string HandleCommand(ICommandArguments commandArguments)
		{
			try
			{
				IConsoleCommand command = _commands[commandArguments.CommandName];
				return command.Execute(commandArguments);
			}
			catch (KeyNotFoundException keyNotFoundException)
			{
				GD.PrintErr(keyNotFoundException.Message);
				PushMessage($"Command '{commandArguments.CommandName}' not found. Use command 'help' for available commands.", LoggingLevel.Error);
				return string.Empty;
			}
			catch (Exception exception)
			{
				string[] messages = new string[]
				{
					$"There was an error while running the command.",
					$"'{commandArguments.TextEntered}'",
					$"Produced",
					$"{exception.Message}"
				};

				PushMessages(messages, LoggingLevel.Error);
				PushMessage(exception.StackTrace, LoggingLevel.Exception);
				return string.Empty;
			}
		}

		void HandleHelpCommand(ICommandArguments commandArguments)
		{
			if (commandArguments.ArgumentQuantity == 0)
			{
				PushMessage("To use a command, use the following syntax:");

				Indent();
				PushMessage("{command name} [arguments|flags]");
				if (_spacingStyle == SpacingStyle.Spacious)
					PushMessage(string.Empty);
				Dedent();

				PushMessage("Available Commands:");

				Indent();
				foreach (KeyValuePair<string, IConsoleCommand> consoleCommand in _commands)
					PushMessage($"{consoleCommand.Key}: {consoleCommand.Value.Usage}");
				if (_spacingStyle == SpacingStyle.Spacious)
					PushMessage(string.Empty);
				Dedent();

				PushMessage("Available Preprocessors:");

				Indent();
				foreach (IPreProcessor preProcessor in _preProcessors)
					PushMessage($"{preProcessor.Name}: {preProcessor.Usage}");
				if (_spacingStyle == SpacingStyle.Spacious)
					PushMessage(String.Empty);
				Dedent();
				return;
			}
			else
			{
				foreach (KeyValuePair<string, IConsoleCommand> consoleCommand in _commands)
					if (commandArguments.GetArgument(0) == consoleCommand.Key)
						PushMessages(consoleCommand.Value.GetHelp(commandArguments));

				if (_spacingStyle == SpacingStyle.Spacious)
					PushMessage(string.Empty);

				foreach (IPreProcessor preProcessor in _preProcessors)
				{
					if (commandArguments.GetArgument(0) == preProcessor.Name)
					{
						PushMessage("Preprocessor:");
						Indent();
						PushMessages(preProcessor.GetHelp(commandArguments));
						Dedent();
					}
				}
			}
		}

		String ManageDuplicateName(string name, ICollection<string> existingNames)
		{
			switch (_commandNameOverrideRule)
			{
				case OverrideRule.Replace:
					PushMessage($"Name will be overridden", LoggingLevel.Warning);
					return name;

				case OverrideRule.Rename:
					string newName = GetIncrementedName(name, existingNames);
					PushMessage($"Name will be renamed to '{newName}'", LoggingLevel.Warning);
					return newName;
			}

			PushMessage($"Name will be ignored.", LoggingLevel.Warning);
			return null;
		}

		string GetIncrementedName(string name, ICollection<string> existingNames)
		{
			int increment = 1;
			string newName;

			do
			{
				newName = $"{name}_{increment++}";
			} while (existingNames.Contains(newName));

			return newName;
		}
		#endregion

		#region IConsole Implementation
		public void ClearLog()
		{
			MessageLog = string.Empty;
		}
		
		public string GetCommandFromBuffer(int index)
		{
			if (_commandBuffer.Count > index)
				return _commandBuffer[index];

			return _commandBuffer[_commandBuffer.Count - 1];
		}
		
		public string ProcessCommand(string input)
		{
			if (_commandBuffer == null)
				_commandBuffer = new List<string>();

			_commandBuffer.Insert(0, input);
			string[] commandPipeline = input.Split('|');
			string output = string.Empty;

			for(int i = 0; i < commandPipeline.Length; i++)
			{
				string command = commandPipeline[i].Trim();

				PushMessage($"> {command}");
				int indents = 0;
				Indent();
				indents++;

				foreach (IPreProcessor preProcessor in _preProcessors)
				{
					string newInput = preProcessor.PreProcess(command);

					if (newInput != command)
					{
						command = newInput;
						PushMessage($"{command}");
						Indent();
						indents++;

						if (string.IsNullOrWhiteSpace(command))
						{
							for (int j = 0; j < indents; j++)
								Dedent();
							return string.Empty;
						}
					}
				}

				ICommandArguments commandArguments = _getCommandArguments.GetArguments(command);

				if (commandArguments.CommandName == "help")
					HandleHelpCommand(commandArguments);
				else
					output = HandleCommand(commandArguments);

				if (_spacingStyle == SpacingStyle.Spacious)
					PushMessage(string.Empty);

				for (int j = 0; j < indents; j++)
					Dedent();

				if (commandPipeline.Length > i + 1)
					commandPipeline[i + 1] += $" {output}";
			}

			return output;
		}
		
		public void PushMessage(string message, LoggingLevel level = LoggingLevel.Message)
		{
			string log = $"\n{new string(' ', _indent)}{message}";
			if (_loggingLevel.HasFlag(level))
			{
				MessageLog += log;
				if (MessageLog.Length > _maxVisibleCharacters)
					MessageLog = MessageLog.Substring(MessageLog.Length - _maxVisibleCharacters);
			}
			LogFileWriter.Write(log);
			LogFileWriter.Flush();
		}
		
		public void PushMessages(string[] messages, LoggingLevel level = LoggingLevel.Message)
		{
			string logs = "\n" + string.Join($"\n{new string(' ', _indent)}", messages);
			if (_loggingLevel.HasFlag(level))
			{
				MessageLog += logs;
				if (MessageLog.Length > _maxVisibleCharacters)
					MessageLog = MessageLog.Substring(MessageLog.Length - _maxVisibleCharacters);
			}
			LogFileWriter.Write(logs);
			LogFileWriter.Flush();
		}
		
		public void PushMessageIndented(string message, LoggingLevel level = LoggingLevel.Message, int indentLevel = 1)
		{
			Indent(indentLevel);
			PushMessage(message, level);
			Dedent(indentLevel);
		}
		
		public void PushMessagesIndented(string[] messages, LoggingLevel level = LoggingLevel.Message, int indentLevel = 1)
		{
			Indent(indentLevel);
			PushMessages(messages, level);
			Dedent(indentLevel);
		}
		
		public void RegisterCommand(IConsoleCommand command)
		{
			if (_commands == null)
				_commands = new Dictionary<string, IConsoleCommand>();

			if (_commands.ContainsKey(command.Name))
			{
				PushMessage($"Command with name '{command.Name}' is already registered.", LoggingLevel.Warning);
				String newName = ManageDuplicateName(command.Name, _commands.Keys);

				if (newName != null)
				{
					if (_commands.ContainsKey(newName))
						_commands[newName] = command;
					else
						_commands.Add(newName, command);
				}

				if (_spacingStyle == SpacingStyle.Spacious)
					PushMessage(string.Empty);
			}
			else
				_commands.Add(command.Name, command);

			command.DeveloperConsole = this;
		}
		
		public void RegisterPreProcessCommand(IPreProcessor preProcessCommand)
		{
			if (_preProcessors == null)
				_preProcessors = new List<IPreProcessor>();

			_preProcessors.Add(preProcessCommand);
			preProcessCommand.DeveloperConsole = this;
		}
		
		public void SetActive(bool active) { }
		
		T IConsole.GetCommand<T>()
		{
			foreach (KeyValuePair<string, IConsoleCommand> command in _commands)
				if (command.Value is T)
					return command.Value as T;

			return null;
		}
		
		T IConsole.GetPreProcessCommand<T>()
		{
			foreach (IPreProcessor preProcessCommand in _preProcessors)
				if (preProcessCommand is T)
					return preProcessCommand as T;

			return null;
		}
		#endregion

		#region IDisposable Implementation
		public void Dispose()
		{
			if (_logFileWriter != null)
			{
				_logFileWriter.Close();
				_logFileWriter.Dispose();
			}
		}
		#endregion
	}
}
