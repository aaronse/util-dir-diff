using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace DirDiff
{
    class Log
    {
        private static string _logFile = null;
        private static bool _logTime = true;
        private static bool _logMessageType = true;
        private static bool _logRelativeTime = false;
        private static DateTime _startTime = DateTime.Now;
        private static bool _verbose = true;
        private static bool _consoleOutput = true;
        private static bool _htmlOutput = true;

        public static bool LogTime
        {
            get { return _logTime; }
            set { _logTime = value; }
        }

        public static bool LogMessageType
        {
            get { return _logMessageType; }
            set { _logMessageType = value; }
        }

        public static bool LogRelativeTime
        {
            get { return _logRelativeTime; }
            set { _logRelativeTime = value; }
        }

        public static bool Verbose
        {
            get { return _verbose; }
            set { _verbose = value; }
        }

        public static bool ConsoleOutput
        {
            get { return _consoleOutput; }
            set { _consoleOutput = value; }
        }

        public static bool HtmlOutput
        {
            get { return _htmlOutput; }
            set { _htmlOutput = value; }
        }

        public static void Error(string msg)
        {
            if (_consoleOutput) Console.ForegroundColor = ConsoleColor.Red;

            Write("[E]", msg);

            if (_consoleOutput) Console.ResetColor();
        }

        public static void Error(string msg, params object[] args)
        {
            msg = string.Format(msg, args);

            if (_consoleOutput) Console.ForegroundColor = ConsoleColor.Red;

            Write("[E]", msg);

            if (_consoleOutput) Console.ResetColor();
        }

        public static void Warn(string msg)
        {
            if (_consoleOutput) Console.ForegroundColor = ConsoleColor.Yellow;

            Write("[W]", msg);

            if (_consoleOutput) Console.ResetColor();
        }

        public static void Warn(string msg, params object[] args)
        {
            msg = string.Format(msg, args);

            if (_consoleOutput) Console.ForegroundColor = ConsoleColor.Yellow;

            Write("[W]", msg);

            if (_consoleOutput) Console.ResetColor();
        }

        public static void Info(string msg)
        {
            if (_consoleOutput) Console.ForegroundColor = ConsoleColor.White;

            Write("[I]", msg);

            if (_consoleOutput) Console.ResetColor();
        }

        public static void Info(string msg, params object[] args)
        {
            msg = string.Format(msg, args);

            if (_consoleOutput) Console.ForegroundColor = ConsoleColor.White;

            Write("[I]", msg);

            if (_consoleOutput) Console.ResetColor();
        }

        public static void Debug(string msg)
        {
            if (!_verbose)
                return;

            if (_consoleOutput) Console.ForegroundColor = ConsoleColor.DarkGray;

            Write("[D]", msg);

            if (_consoleOutput) Console.ResetColor();
        }

        public static void Debug(string msg, params object[] args)
        {
            if (!_verbose)
                return;

            msg = string.Format(msg, args);

            if (_consoleOutput) Console.ForegroundColor = ConsoleColor.DarkGray;

            Write("[D]", msg);

            if (_consoleOutput) Console.ResetColor();
        }

        private static void Write(string msgType, string msg)
        {
            StringBuilder sb = new StringBuilder();
            if (Log.LogMessageType)
            {
                sb.Append(msgType);
                sb.Append(" ");
            }
            if (Log.LogTime)
            {
                if (!Log.LogRelativeTime)
                {
                    sb.Append(DateTime.Now.ToString("HH:mm:ss.ff"));
                }
                else
                {
                    TimeSpan ts = DateTime.Now.Subtract(_startTime);
                    sb.Append(
                        string.Format(
                            "{0:D2}:{1:D2}:{2:D2}.{3:D3}"
                            , ts.Hours
                            , ts.Minutes
                            , ts.Seconds
                            , ts.Milliseconds));
                }
                sb.Append(" ");
            }

            sb.Append(msg);

            // System.Diagnostics.TextWriterTraceListener
            if (_consoleOutput)
            {
                if (_htmlOutput)
                {
                    Console.WriteLine("<br/>" + sb.ToString());
                }
                else
                {
                    Console.WriteLine(sb.ToString());
                }
            }
            if (_htmlOutput)
            {
                System.Diagnostics.Trace.WriteLine("<br/>" + sb.ToString());
            }
            else
            {
                System.Diagnostics.Trace.WriteLine(sb.ToString());
            }
        }

        public static void SetLogFile(string logFile)
        {
            _logFile = logFile;

            Trace.Listeners.Clear();

            if (!string.IsNullOrEmpty(_logFile))
            {
                Trace.Listeners.Add(new TextWriterTraceListener(_logFile));
            }
        }

        public static void Close()
        {
            foreach (TraceListener tl in Trace.Listeners)
            {
                tl.Flush();
            }
        }

    }
}
