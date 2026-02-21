using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static System.String;

namespace ScePSX
{
    public sealed class Logger
    {
        public enum Level
        {
            Notice,
            Info,
            Warning,
            Disassembler,
            Error,
            Fatal,
        }

        public string Name
        {
            get; private set;
        }

        private bool Enabled = false;

        private static readonly Dictionary<string, Logger> Loggers = new Dictionary<string, Logger>();

        internal Logger()
        {
        }

        public static Logger CreateAnonymousLogger()
        {
            return new Logger();
        }

        public static Logger GetLogger(string name)
        {
            lock (Loggers)
            {
                if (!Loggers.ContainsKey(name))
                {
                    Loggers[name] = new Logger()
                    {
                        Name = name,
                    };
                }

                return Loggers[name];
            }
        }

        public static event Action<string, Level, string, StackFrame> OnGlobalLog;

        public event Action<Level, string, StackFrame> OnLog;

        private Logger Log(Level level, object format, params object[] Params)
        {
            if (Enabled || OnGlobalLog != null)
            {
                var stackTrace = new StackTrace();
                StackFrame stackFrame = null;
                var stackFrames = stackTrace.GetFrames();
                if (stackFrames != null)
                    foreach (var frame in stackFrames)
                    {
                        if (frame.GetMethod().DeclaringType != typeof(Logger))
                        {
                            stackFrame = frame;
                            break;
                        }
                    }

                if (Enabled)
                {
                    OnLog?.Invoke(level, Format(format.ToString(), Params), stackFrame);
                }

                OnGlobalLog?.Invoke(Name, level, Format(format.ToString(), Params), stackFrame);
            }

            return this;
        }

        public Logger Notice(object format, params object[] Params) => Log(Level.Notice, format, Params);

        public Logger Info(object format, params object[] Params) => Log(Level.Info, format, Params);

        public Logger Warning(object format, params object[] Params) => Log(Level.Warning, format, Params);

        public Logger Disassembler(object format, params object[] Params) => Log(Level.Disassembler, format, Params);

        public Logger Error(object format, params object[] Params) => Log(Level.Error, format, Params);

        public Logger Fatal(object format, params object[] Params) => Log(Level.Fatal, format, Params);

        public static TimeSpan Measure(Action action)
        {
            var start = DateTime.UtcNow;
            action();
            var end = DateTime.UtcNow;
            return end - start;
        }

        public class Stopwatch
        {
            protected List<DateTime> DateTimeList = new List<DateTime>();
            protected DateTime LastDateTime;

            public Stopwatch() => Start();
            public Stopwatch Start()
            {
                LastDateTime = DateTime.UtcNow;
                return this;
            }

            public TimeSpan Tick()
            {
                var now = DateTime.UtcNow;
                DateTimeList.Add(now);
                try
                {
                    return now - LastDateTime;
                } finally
                {
                    LastDateTime = now;
                }
            }

            public override string ToString()
            {
                var timeSpans = new List<TimeSpan>();
                for (var n = 1; n < DateTimeList.Count; n++)
                    timeSpans.Add(DateTimeList[n] - DateTimeList[n - 1]);
                return $"Logger.Stopwatch({Join(",", timeSpans.Select(item => $"{(int)item.TotalMilliseconds} ms"))})";
            }
        }

        public void TryCatch(Action action)
        {
            if (Debugger.IsAttached)
            {
                action();
            } else
            {
                try
                {
                    action();
                } catch (Exception e)
                {
                    Error(e);
                }
            }
        }

        class OutputTextWriter : TextWriter
        {
            private Logger Logger;
            private Level level;

            public OutputTextWriter(Logger logger, Level level)
            {
                this.Logger = logger;
                this.level = level;
            }

            public override Encoding Encoding
            {
                get;
            }

            StringBuilder sb = new StringBuilder();

            public override void Write(char value)
            {
                if (value == '\n')
                {
                    sb.Append(value);
                } else
                {
                    Logger.Log(level, "{0}", sb.ToString());
                    sb.Clear();
                }
            }
        }

        public TextWriter Output(Level level) => new OutputTextWriter(this, level);
    }
}
