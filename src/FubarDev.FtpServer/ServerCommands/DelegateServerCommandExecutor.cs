// <copyright file="DelegateServerCommandExecutor.cs" company="Fubar Development Junker">
// Copyright (c) Fubar Development Junker. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;

namespace FubarDev.FtpServer.ServerCommands
{
    /// <summary>
    /// This <see cref="IServerCommandExecutor"/> implementation creates a compiled delegate to call the server command handlers.
    /// </summary>
    public class DelegateServerCommandExecutor : IServerCommandExecutor
    {
        [NotNull]
        private readonly IFtpConnectionAccessor _ftpConnectionAccessor;

        [NotNull]
        private readonly Dictionary<Type, Delegate> _serverCommandHandlerDelegates = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateServerCommandExecutor"/> class.
        /// </summary>
        /// <param name="ftpConnectionAccessor">Accessor to get the FTP connection.</param>
        public DelegateServerCommandExecutor([NotNull] IFtpConnectionAccessor ftpConnectionAccessor)
        {
            _ftpConnectionAccessor = ftpConnectionAccessor;
        }

        /// <inheritdoc />
        public Task ExecuteAsync(IServerCommand serverCommand, CancellationToken cancellationToken)
        {
            var serverCommandType = serverCommand.GetType();
            if (!_serverCommandHandlerDelegates.TryGetValue(serverCommandType, out var cmdDelegate))
            {
                var handlerType = typeof(IServerCommandHandler<>).MakeGenericType(serverCommandType);
                var executeAsyncMethod = handlerType.GetRuntimeMethod("ExecuteAsync", new[] { serverCommandType, typeof(CancellationToken) });
                var handler = _ftpConnectionAccessor.FtpConnection.ConnectionServices.GetRequiredService(handlerType);
                var commandParameter = Expression.Parameter(serverCommandType, "serverCommand");
                var cancellationTokenParameter = Expression.Parameter(typeof(CancellationToken), "cancellationToken");
                var call = Expression.Call(
                    Expression.Constant(handler),
                    executeAsyncMethod,
                    commandParameter,
                    cancellationTokenParameter);
                var expr = Expression.Lambda(call, commandParameter, cancellationTokenParameter);
                cmdDelegate = expr.Compile();
                _serverCommandHandlerDelegates.Add(serverCommandType, cmdDelegate);
            }

            return (Task)cmdDelegate.DynamicInvoke(serverCommand, cancellationToken);
        }
    }
}