﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace DatabaseLogging
{
#pragma warning disable CA1063 // Implement IDisposable Correctly
    public class DatabaseLogger : ILogger, IDisposable
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        private readonly Queue<LogMessage> pendingLogs = new();
        private bool disposed;
        Context context;

        public DatabaseLogger(Context context, ThreadPriority threadPriority)
        {
            this.context = context;
            new Thread(ProcessLogs) { Priority = threadPriority }.Start();
        }

        private void ProcessLogs(object obj)
        {
            while (!disposed || pendingLogs.Count > 0  )
            {
                if (pendingLogs.Count > 0)
                {
                    var logMessage = pendingLogs.Dequeue();
                    context.Add(logMessage);
                    context.SaveChanges();
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return new NullDisposable();
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null) return;

            var message = formatter(state, exception);

            var logProperties = ImmutableList<LogProperty>.Empty;

            if (state is IReadOnlyList<KeyValuePair<string, object>> kvps)
            {
                logProperties = ImmutableList.Create(kvps.Select(kvp => new LogProperty(kvp.Key, kvp.Value.ToString())).ToArray());
            }

            pendingLogs.Enqueue(new LogMessage(Guid.NewGuid(), logLevel, eventId.Id, exception?.ToString(), message, DateTimeOffset.UtcNow, logProperties));
        }

#pragma warning disable CA1063 // Implement IDisposable Correctly
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        public void Dispose()
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
#pragma warning restore CA1063 // Implement IDisposable Correctly
        {
            disposed = true;
        }
    }

}
