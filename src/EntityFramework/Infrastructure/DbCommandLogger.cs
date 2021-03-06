﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

namespace System.Data.Entity.Infrastructure
{
    using System.Data.Common;
    using System.Data.Entity.Resources;
    using System.Data.Entity.Utilities;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    ///     This is the default logger used when some <see cref="Action{String}" /> is set onto the <see cref="Database.Log" />
    ///     property. A different logger can be used by creating a class that inherits from this class and overrides
    ///     some or all methods to change behavior.
    /// </summary>
    /// <remarks>
    ///     To set the new logger create a code-based configuration for EF using <see cref="DbConfiguration" /> and then
    ///     set the logger class to use with <see cref="DbConfiguration.CommandLogger" />.
    ///     Note that setting the type of logger to use with this method does change the way command are
    ///     logged when <see cref="Database.Log" /> is used. It is still necessary to set a <see cref="Action{String}" />
    ///     onto <see cref="Database.Log" /> before any commands will be logged.
    ///     For more low-level control over logging/interception see <see cref="IDbCommandInterceptor" /> and
    ///     <see cref="Interception" />.
    /// </remarks>
    public class DbCommandLogger : IDbCommandInterceptor
    {
        private readonly DbContext _context;
        private readonly Action<string> _sink;
        private readonly Stopwatch _stopwatch = new Stopwatch();

        /// <summary>
        ///     Creates a logger that will not filter by any <see cref="DbContext" /> and will instead log every command
        ///     from any context and also commands that do not originate from a context.
        /// </summary>
        /// <remarks>
        ///     This constructor is not used when a delegate is set on <see cref="Database.Log" />. Instead it can be
        ///     used by setting the logger directly using <see cref="Interception.AddInterceptor" />.
        /// </remarks>
        /// <param name="sink">The delegate to which output will be sent.</param>
        public DbCommandLogger(Action<string> sink)
        {
            Check.NotNull(sink, "sink");

            _sink = sink;
        }

        /// <summary>
        ///     Creates a logger that will only log commands the come from the given <see cref="DbContext" /> instance.
        /// </summary>
        /// <remarks>
        ///     This constructor must be called by a class that inherits from this class to override the behavior
        ///     of <see cref="Database.Log" />.
        /// </remarks>
        /// <param name="context">The context for which commands should be logged.</param>
        /// <param name="sink">The delegate to which output will be sent.</param>
        public DbCommandLogger(DbContext context, Action<string> sink)
        {
            Check.NotNull(context, "context");
            Check.NotNull(sink, "sink");

            _context = context;
            _sink = sink;
        }

        /// <summary>
        ///     The context for which commands are being logged, or null if commands from all contexts are
        ///     being logged.
        /// </summary>
        public DbContext Context
        {
            get { return _context; }
        }

        /// <summary>
        ///     The delegate to which output is being sent.
        /// </summary>
        public Action<string> Sink
        {
            get { return _sink; }
        }

        /// <summary>
        ///     The stop watch used to time executions.
        /// </summary>
        public Stopwatch Stopwatch
        {
            get { return _stopwatch; }
        }

        /// <summary>
        ///     This method is called before a call to <see cref="DbCommand.ExecuteNonQuery" /> or
        ///     one of its async counterparts is made.
        ///     The default implementation calls <see cref="Executing" />
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Executing(command, interceptionContext);
            Stopwatch.Restart();
        }

        /// <summary>
        ///     This method is called after a call to <see cref="DbCommand.ExecuteNonQuery" />  or
        ///     one of its async counterparts is made.
        ///     The default implementation calls <see cref="Executed" />
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Stopwatch.Stop();
            Executed(command, interceptionContext.Result, interceptionContext);
        }

        /// <summary>
        ///     This method is called before a call to <see cref="DbCommand.ExecuteReader(CommandBehavior)" />  or
        ///     one of its async counterparts is made.
        ///     The default implementation calls <see cref="Executing" />
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Executing(command, interceptionContext);
            Stopwatch.Restart();
        }

        /// <summary>
        ///     This method is called after a call to <see cref="DbCommand.ExecuteReader(CommandBehavior)" />  or
        ///     one of its async counterparts is made.
        ///     The default implementation calls <see cref="Executed" />
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Stopwatch.Stop();
            Executed(command, interceptionContext.Result, interceptionContext);
        }

        /// <summary>
        ///     This method is called before a call to <see cref="DbCommand.ExecuteScalar" />  or
        ///     one of its async counterparts is made.
        ///     The default implementation calls <see cref="Executing" />
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Executing(command, interceptionContext);
            Stopwatch.Restart();
        }

        /// <summary>
        ///     This method is called after a call to <see cref="DbCommand.ExecuteScalar" />  or
        ///     one of its async counterparts is made.
        ///     The default implementation calls <see cref="Executed" />
        /// </summary>
        /// <param name="command">The command being executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the call.</param>
        public virtual void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            Stopwatch.Stop();
            Executed(command, interceptionContext.Result, interceptionContext);
        }

        /// <summary>
        ///     Called whenever a command is about to be executed. The default implementation of this method
        ///     filters by <see cref="DbContext" /> set into <see cref="Context" />, if any, and then calls
        ///     <see cref="LogCommand" />. This method would typically only be overridden to change the
        ///     context filtering behavior.
        /// </summary>
        /// <param name="command">The command that will be executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        public virtual void Executing(DbCommand command, DbCommandInterceptionContext interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                LogCommand(command, interceptionContext);
            }
        }

        /// <summary>
        ///     Called whenever a command has completed executing. The default implementation of this method
        ///     filters by <see cref="DbContext" /> set into <see cref="Context" />, if any, and then calls
        ///     <see cref="LogResult" />. This method would typically only be overridden to change the context
        ///     filtering behavior.
        /// </summary>
        /// <param name="command">The command that was executed.</param>
        /// <param name="result">The result of executing the command.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        public virtual void Executed(DbCommand command, object result, DbCommandInterceptionContext interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (Context == null
                || interceptionContext.DbContexts.Contains(Context, ReferenceEquals))
            {
                LogResult(command, result, interceptionContext);
            }
        }

        /// <summary>
        ///     Called to log a command that is about to be executed. Override this method to change how the
        ///     command is logged to <see cref="Sink" />.
        /// </summary>
        /// <param name="command">The command to be logged.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        public virtual void LogCommand(DbCommand command, DbCommandInterceptionContext interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            var commandText = command.CommandText ?? "<null>";
            if (commandText.EndsWith(Environment.NewLine, StringComparison.Ordinal))
            {
                Sink(commandText);
            }
            else
            {
                Sink(commandText);
                Sink(Environment.NewLine);
            }

            if (command.Parameters != null)
            {
                foreach (var parameter in command.Parameters.OfType<DbParameter>())
                {
                    LogParameter(command, interceptionContext, parameter);
                }
            }

            Sink(interceptionContext.IsAsync
                      ? Strings.CommandLogAsync(DateTimeOffset.Now, Environment.NewLine)
                      : Strings.CommandLogNonAsync(DateTimeOffset.Now, Environment.NewLine));
        }

        /// <summary>
        ///     Called by <see cref="LogCommand" /> to log each parameter. This method can be called from an overridden
        ///     implementation of <see cref="LogCommand" /> to log parameters, and/or can be overridden to
        ///     change the way that parameters are logged to <see cref="Sink" />.
        /// </summary>
        /// <param name="command">The command being logged.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        /// <param name="parameter">The parameter to log.</param>
        public virtual void LogParameter(DbCommand command, DbCommandInterceptionContext interceptionContext, DbParameter parameter)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");
            Check.NotNull(parameter, "parameter");

            // -- Name: [Value] (Type = {}, Direction = {}, IsNullable = {}, Size = {}, Precision = {} Scale = {})
            var builder = new StringBuilder();
            builder.Append("-- ")
                .Append(parameter.ParameterName)
                .Append(": '")
                .Append((parameter.Value == null || parameter.Value == DBNull.Value) ? "null" : parameter.Value)
                .Append("' (Type = ")
                .Append(parameter.DbType);

            if (parameter.Direction != ParameterDirection.Input)
            {
                builder.Append(", Direction = ").Append(parameter.Direction);
            }

            if (!parameter.IsNullable)
            {
                builder.Append(", IsNullable = false");
            }

            if (parameter.Size != 0)
            {
                builder.Append(", Size = ").Append(parameter.Size);
            }

            if (((IDbDataParameter)parameter).Precision != 0)
            {
                builder.Append(", Precision = ").Append(((IDbDataParameter)parameter).Precision);
            }

            if (((IDbDataParameter)parameter).Scale != 0)
            {
                builder.Append(", Scale = ").Append(((IDbDataParameter)parameter).Scale);
            }

            builder.Append(")").Append(Environment.NewLine);

            Sink(builder.ToString());
        }

        /// <summary>
        ///     Called to log the result of executing a command. Override this method to change how results are
        ///     logged to <see cref="Sink" />.
        /// </summary>
        /// <param name="command">The command being logged.</param>
        /// <param name="result">The result returned when the command was executed.</param>
        /// <param name="interceptionContext">Contextual information associated with the command.</param>
        public virtual void LogResult(DbCommand command, object result, DbCommandInterceptionContext interceptionContext)
        {
            Check.NotNull(command, "command");
            Check.NotNull(interceptionContext, "interceptionContext");

            if (interceptionContext.Exception != null)
            {
                Sink(Strings.CommandLogFailed(
                    Stopwatch.ElapsedMilliseconds, interceptionContext.Exception.Message, Environment.NewLine));
            }
            else if (interceptionContext.TaskStatus.HasFlag(TaskStatus.Canceled))
            {
                Sink(Strings.CommandLogCanceled(Stopwatch.ElapsedMilliseconds, Environment.NewLine));
            }
            else
            {
                var resultString = result == null
                                       ? "null"
                                       : (result is DbDataReader)
                                             ? result.GetType().Name
                                             : result.ToString();
                Sink(Strings.CommandLogComplete(Stopwatch.ElapsedMilliseconds, resultString, Environment.NewLine));
            }
            Sink(Environment.NewLine);
        }
    }
}
