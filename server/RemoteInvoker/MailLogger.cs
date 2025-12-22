using MailKit;
using System;

namespace Genius2
{
    class MailLogger : IProtocolLogger
    {


        /// <summary>
        /// Class which writes the log for the SMTP to the text box.
        /// </summary>
        /// <param name="textBox">The textbox to write in.</param>
        public MailLogger()
        {
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // No need to log this.
        }

        /// <summary>
        /// Appends given line to the text box.
        /// Uses invoke, as this is called in a non GUI thread.
        /// </summary>
        /// <param name="message">The message to append.</param>
        private void AppendLog(string message)
        {
            Console.WriteLine(message);
        }

        /// <inheritdoc/>
        public void LogClient(byte[] buffer, int offset, int count)
        {
            this.AppendLog(System.Text.Encoding.Default.GetString(buffer));
        }

        /// <inheritdoc/>
        public void LogConnect(Uri uri)
        {
            this.AppendLog(string.Format("Connect to: {0}", uri));
        }

        /// <inheritdoc/>
        public void LogServer(byte[] buffer, int offset, int count)
        {
            this.AppendLog(System.Text.Encoding.Default.GetString(buffer));
        }
    }
}
