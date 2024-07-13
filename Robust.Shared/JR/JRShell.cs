using Robust.Shared.Console;
using Robust.Shared.Player;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Robust.Shared.JR
{
    public class JRShell : IConsoleShell
    {
        public JRShell(IConsoleShell real)
        {
            _baseShell = real;
        }

        /// <summary>
        /// The console host that owns this shell.
        /// </summary>
        public IConsoleHost ConsoleHost { get { return _baseShell.ConsoleHost; } }

        /// <summary>
        /// Is the shell running on the client?
        /// </summary>
        public bool IsClient => !IsServer;

        /// <summary>
        /// Is the shell running in a local context (no remote peer session)?.
        /// </summary>
        public bool IsLocal { get { return _baseShell.IsLocal; } }

        /// <summary>
        /// Is the shell running on the server?
        /// </summary>
        public bool IsServer { get { return _baseShell.IsServer; } }

        /// <summary>
        /// The remote peer that owns this shell, or the local player if this is a client-side local shell (<see cref="IsLocal" /> is true and <see cref="IsClient"/> is true).
        /// </summary>
        public ICommonSession? Player { get { return _baseShell.Player; } }

        /// <summary>
        /// Executes a command string on this specific session shell. If the command does not exist, the command will be forwarded
        /// to the
        /// remote shell.
        /// </summary>
        /// <param name="command">command line string to execute.</param>
        public void ExecuteCommand(string command)
        {
            _baseShell.ExecuteCommand(command);
        }

        /// <summary>
        /// Executes the command string on the remote peer. This is mainly used to forward commands from the client to the server.
        /// If there is no remote peer (this is a local shell), this function does nothing.
        /// </summary>
        /// <param name="command">Command line string to execute at the remote endpoint.</param>
        public void RemoteExecuteCommand(string command)
        {
            _baseShell.RemoteExecuteCommand(command);
        }

        /// <summary>
        /// Writes a line to the output of the console.
        /// </summary>
        /// <param name="text">Line of text to write.</param>
        public void WriteLine(string text)
        {
            SlayerTK.JRCon.Send(text);
            _baseShell.WriteLine(text);
        }

        public void WriteLine(FormattedMessage message)
        {
            SlayerTK.JRCon.Send(message.ToString());
            _baseShell.WriteLine(message);
        }

        public void WriteMarkup(string markup)
        {
            WriteLine(FormattedMessage.FromMarkupPermissive(markup));
        }

        /// <summary>
        /// Write an error line to the console window.
        /// </summary>
        /// <param name="text">Line of text to write.</param>
        public void WriteError(string text)
        {
            SlayerTK.JRCon.Send(text, new SlayerTK.JRColor(255, 0, 0));
            _baseShell.WriteError(text);
        }

        /// <summary>
        /// Clears the entire console of text.
        /// </summary>
        public void Clear()
        {
            _baseShell.Clear();
        }

        private IConsoleShell _baseShell;
    }
}
