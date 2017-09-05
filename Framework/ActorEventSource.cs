#region Copyright

//=======================================================================================
// Microsoft Azure Customer Advisory Team  
//
// This sample is supplemental to the technical guidance published on the community
// blog at http://blogs.msdn.com/b/paolos/. 
// 
// Author: Paolo Salvatori
//=======================================================================================
// Copyright © 2016 Microsoft Corporation. All rights reserved.
// 
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
// EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF 
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. YOU BEAR THE RISK OF USING IT.
//=======================================================================================

#endregion

#region Using Directives

using System;
using System.Diagnostics.Tracing;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

#endregion

namespace Microsoft.AzureCat.Samples.Framework
{
    [EventSource(Name = "LongRunningActors-Framework-Actor")]
    public sealed class ActorEventSource : EventSource
    {
        #region Public Static Fields
        public static ActorEventSource Current = new ActorEventSource();
        #endregion 

        #region Public Methods
        private const int MessageEventId = 1;
        [Event(MessageEventId, Level = EventLevel.Informational, Message = "{0}")]
        public void Message(string message, [CallerFilePath] string source = "", [CallerMemberName] string method = "")
        {
            if (!IsEnabled())
                return;
            WriteEvent(MessageEventId, $"[{GetClassFromFilePath(source) ?? "UNKNOWN"}::{method ?? "UNKNOWN"}] {message}");
        }

        [NonEvent]
        public void ActorMessage(Actor actor, string message, [CallerFilePath] string source = "",
            [CallerMemberName] string method = "", params object[] args)
        {
            if (!IsEnabled())
                return;
            var finalMessage = string.Format(message, args);
            ActorMessage(
                actor.GetType().ToString(),
                actor.Id.ToString(),
                actor.ActorService.Context.CodePackageActivationContext.ApplicationTypeName,
                actor.ActorService.Context.CodePackageActivationContext.ApplicationName,
                actor.ActorService.Context.ServiceTypeName,
                actor.ActorService.Context.ServiceName.ToString(),
                actor.ActorService.Context.PartitionId,
                actor.ActorService.Context.ReplicaId,
                actor.ActorService.Context.NodeContext.NodeName,
                GetClassFromFilePath(source) ?? "UNKNOWN",
                method ?? "UNKNOWN",
                finalMessage);
        }

        [NonEvent]
        public void ActorHostInitializationFailed(Exception e, [CallerFilePath] string source = "",
            [CallerMemberName] string method = "")
        {
            if (IsEnabled())
                ActorHostInitializationFailed(e.ToString(), GetClassFromFilePath(source) ?? "UNKNOWN",
                    method ?? "UNKNOWN");
        }

        [NonEvent]
        public void Error(Exception e, [CallerFilePath] string source = "", [CallerMemberName] string method = "")
        {
            if (IsEnabled())
            {
                Error($"[{GetClassFromFilePath(source) ?? "UNKNOWN"}::{method ?? "UNKNOWN"}] {e}");
            }
        }

        private const int RequestEventId = 5;
        [Event(RequestEventId, Level = EventLevel.Informational)]
        public void RequestComplete(string requestName, bool isSuccess, long duration, string response)
        {
            if (IsEnabled())
                WriteEvent(RequestEventId,
                           requestName, 
                           isSuccess, 
                           duration, 
                           response);
        }

        private const int DependencyEventId = 6;
        [Event(DependencyEventId, Level = EventLevel.Informational)]
        public void Dependency(string target, bool isSuccess, long duration, string response, string type)
        {
            if (IsEnabled())
                WriteEvent(DependencyEventId,
                           target,
                           isSuccess,
                           duration,
                           response,
                           type);
        }

        private const int ReceivedMessageEventId = 7;
        [Event(ReceivedMessageEventId, Level = EventLevel.Informational)]
        public void ReceivedMessage()
        {
            if (IsEnabled())
                WriteEvent(ReceivedMessageEventId);
        }

        private const int StoppedMessageEventId = 8;
        [Event(StoppedMessageEventId, Level = EventLevel.Informational)]
        public void StoppedMessage()
        {
            if (IsEnabled())
                WriteEvent(StoppedMessageEventId);
        }

        private const int ProcessedMessageEventId = 9;
        [Event(ProcessedMessageEventId, Level = EventLevel.Informational)]
        public void ProcessedMessage()
        {
            if (IsEnabled())
                WriteEvent(ProcessedMessageEventId);
        }

        #endregion

        #region Private Methods
        private const int ActorMessageEventId = 2;
        [Event(ActorMessageEventId, Level = EventLevel.Informational, Message = "Message={11}")]
        private void ActorMessage(string actorType,
                                  string actorId,
                                  string applicationTypeName,
                                  string applicationName,
                                  string serviceTypeName,
                                  string serviceName,
                                  Guid partitionId,
                                  long replicaOrInstanceId,
                                  string nodeName,
                                  string source,
                                  string method,
                                  string message)
        {
            WriteEvent(
                ActorMessageEventId,
                actorType,
                actorId,
                applicationTypeName,
                applicationName,
                serviceTypeName,
                serviceName,
                partitionId,
                replicaOrInstanceId,
                nodeName,
                source,
                method,
                message);
        }

        private const int ActorHostInitializationFailedEventId = 3;
        [Event(ActorHostInitializationFailedEventId, Level = EventLevel.Error, Message = "Actor host initialization failed: {0}")]
        private void ActorHostInitializationFailed(string exception, string source, string method)
        {
            WriteEvent(ActorHostInitializationFailedEventId, exception, source, method);
        }

        private const int ErrorEventId = 4;
        [Event(ErrorEventId, Level = EventLevel.Error, Message = "{0}")]
        private void Error(string exception)
        {
            WriteEvent(ErrorEventId, exception);
        }
        #endregion

        #region Private Static Methods
        private static string GetClassFromFilePath(string sourceFilePath)
        {
            if (string.IsNullOrWhiteSpace(sourceFilePath))
                return null;
            var file = new FileInfo(sourceFilePath);
            return Path.GetFileNameWithoutExtension(file.Name);
        } 
        #endregion
    }
}