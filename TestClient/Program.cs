// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

#region Using Directives



#endregion

namespace Microsoft.AzureCat.Samples.TestClient
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Fabric;
    using System.Fabric.Query;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AzureCat.Samples.Entities;
    using Microsoft.AzureCat.Samples.WorkerActorService.Interfaces;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Generator;
    using Microsoft.ServiceFabric.Actors.Query;
    using Newtonsoft.Json;

    internal class Program
    {
        #region Main Method

        public static void Main(string[] args)
        {
            try
            {
                // Sets window size and cursor color
                Console.SetWindowSize(130, 30);
                Console.ForegroundColor = ConsoleColor.White;

                // Reads configuration settings
                ReadConfiguration();

                // Sets actor service URIs
                WorkerActorServiceUri = ActorNameFormat.GetFabricServiceUri(typeof(IWorkerActor), ApplicationName);
                QueueActorServiceUri = ActorNameFormat.GetFabricServiceUri(typeof(IQueueActor), ApplicationName);
                ProcessorActorServiceUri = ActorNameFormat.GetFabricServiceUri(typeof(IProcessorActor), ApplicationName);

                int i;
                while ((i = SelectOption()) != TestList.Count + 1)
                {
                    try
                    {
                        PrintTestParameters(TestList[i - 1].Name);
                        TestList[i - 1].Action();
                    }
                    catch (Exception ex)
                    {
                        PrintException(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        #endregion

        #region Private Constants

        //************************************
        // Private Constants
        //************************************
        private const string ApplicationName = "LongRunningActors";
        private const string DelayProperty = "delay";
        private const string StepsProperty = "steps";


        //***************************
        // Configuration Parameters
        //***************************
        private const string GatewayUrlParameter = "gatewayUrl";
        private const string MessageCountParameter = "messageCount";
        private const string StepsParameter = "steps";
        private const string DelayParameter = "delay";

        //************************************
        // Default Values
        //************************************
        private const string DefaultGatewayUrl = "http://localhost:8082/worker";
        private const int DefaultMessageCount = 3;
        private const int DefaultSteps = 5;
        private const int DefaultDelay = 1;

        #endregion

        #region Private Static Fields

        private static readonly List<Test> TestList = new List<Test>
        {
            new Test
            {
                Name = "Sequential Test Via Actor Proxy",
                Description = "Simulates an actor processing messages in a sequential order.",
                Action = TestSequentialProcessingTaskViaActorProxy
            },
            new Test
            {
                Name = "Sequential Test Via Gateway Service",
                Description = "Simulates an actor processing messages in a sequential order.",
                Action = TestSequentialProcessingTaskViaGatewayService
            },
            new Test
            {
                Name = "Parallel Test Via Actor Proxy",
                Description = "Simulates an actor processing messages in parallel.",
                Action = TestParallelMessageProcessingViaActorProxy
            },
            new Test
            {
                Name = "Parallel Test Via Gateway Service",
                Description = "Simulates an actor processing messages in parallel.",
                Action = TestParallelMessageProcessingViaGatewayService
            },
            new Test
            {
                Name = "Get Statistics Via Actor Proxy",
                Description = "Retrieves the processing statistics.",
                Action = TestGetProcessingStatisticsViaActorProxy
            },
            new Test
            {
                Name = "Get Statistics Via Gateway",
                Description = "Retrieves the processing statistics.",
                Action = TestGetProcessingStatisticsViaGateway
            },
            new Test
            {
                Name = "Stop Sequential Test Via Actor Proxy",
                Description = "Stops the sequential message processing task using a cancellation token.",
                Action = StopSequentialProcessingTaskViaActorProxy
            },
            new Test
            {
                Name = "Stop Parallel Test Via Actor Proxy",
                Description = "Stops a parallel message processing task using a cancellation token.",
                Action = StopParallelProcessingTaskViaActorProxy
            }
        };

        private static readonly string Line = new string('-', 129);
        private static Uri WorkerActorServiceUri;
        private static Uri QueueActorServiceUri;
        private static Uri ProcessorActorServiceUri;
        private static string GatewayUrl;
        private static int MessageCount;
        private static int Steps;
        private static int Delay;
        private static int Id;

        #endregion

        #region Test Methods

        private static void TestSequentialProcessingTaskViaActorProxy()
        {
            try
            {
                // Sets device name used in the ActorId constructor
                const string workerId = "worker01";

                // Creates actor proxy
                IWorkerActor proxy = ActorProxy.Create<IWorkerActor>(new ActorId(workerId), WorkerActorServiceUri);
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] ActorProxy for the [{workerId}] created.");

                // Enqueues N messages. Note: the sequential message processing task emulates K steps of H seconds each to process each message.
                // However, since it runs on a separate task not awaited by the actor ProcessMessageAsync method,
                // the method itself returns immediately without waiting the the task completion.
                // This allows the actor to continue to enqueue requests, while processing messages on a separate task.
                List<Message> messageList = CreateMessageList();

                foreach (Message message in messageList)
                {
                    proxy.StartSequentialProcessingAsync(message).Wait();
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Message [{JsonSerializerHelper.Serialize(message)}] sent.");
                }

                while (proxy.IsSequentialProcessingRunningAsync().Result)
                {
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Waiting for the sequential message processing task completion...");
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Sequential message processing task completed.");

                // Retrieves statistics
                Statistics statistics = proxy.GetProcessingStatisticsAsync().Result;
                if (statistics == null)
                {
                    return;
                }

                // Prints statistics
                PrintStatistics(statistics);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private static void TestParallelMessageProcessingViaActorProxy()
        {
            try
            {
                // Sets device name used in the ActorId constructor
                const string workerId = "worker01";

                // Creates actor proxy
                IWorkerActor proxy = ActorProxy.Create<IWorkerActor>(new ActorId(workerId), WorkerActorServiceUri);
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] ActorProxy for the [{workerId}] created.");

                // Creates N messages
                List<Message> messageList = CreateMessageList();

                List<Task> taskList = new List<Task>();
                Func<string, Task> waitHandler = async messageId =>
                {
                    try
                    {
                        while (await proxy.IsParallelProcessingRunningAsync(messageId))
                        {
                            Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Waiting for [{messageId}] parallel processing task completion...");
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                        Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] [{messageId}] Parallel message processing task completed.");
                    }
                    catch (Exception ex)
                    {
                        PrintException(ex);
                    }
                };

                // Start parallel processing
                foreach (Message message in messageList)
                {
                    if (!proxy.StartParallelProcessingAsync(message).Result)
                    {
                        continue;
                    }
                    taskList.Add(waitHandler(message.MessageId));
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Message [{JsonSerializerHelper.Serialize(message)}] sent.");
                }

                // Wait for message processing completion
                Task.WaitAll(taskList.ToArray());

                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Parallel message processing tasks completed.");

                // Retrieves statistics
                Statistics statistics = proxy.GetProcessingStatisticsAsync().Result;
                if (statistics == null)
                {
                    return;
                }

                // Prints statistics
                PrintStatistics(statistics);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private static void TestSequentialProcessingTaskViaGatewayService()
        {
            try
            {
                // Sets device name used in the ActorId constructor
                const string workerId = "worker01";

                // Creates http proxy
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = new Uri(GatewayUrl)
                };
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] HttpClient for the [{workerId}] created.");

                // Enqueues N messages. Note: the sequential message processing task emulates K steps of H seconds each to process each message.
                // However, since it runs on a separate task not awaited by the actor ProcessMessageAsync method,
                // the method itself returns immediately without waiting the the task completion.
                // This allows the actor to continue to enqueue requests, while processing messages on a separate task.
                List<Message> messageList = CreateMessageList();

                string json;
                StringContent postContent;
                HttpResponseMessage response;
                foreach (Message message in messageList)
                {
                    json = JsonConvert.SerializeObject(
                        new Payload
                        {
                            WorkerId = workerId,
                            Message = message
                        });
                    postContent = new StringContent(json, Encoding.UTF8, "application/json");
                    response = httpClient.PostAsync(Combine(httpClient.BaseAddress.AbsoluteUri, "api/sequential/start"), postContent).Result;
                    response.EnsureSuccessStatusCode();
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Message [{JsonSerializerHelper.Serialize(message)}] sent.");
                }
                json = JsonConvert.SerializeObject(
                    new Payload
                    {
                        WorkerId = workerId
                    });
                postContent = new StringContent(json, Encoding.UTF8, "application/json");
                while (bool.Parse(
                    httpClient.PostAsync(
                        Combine(
                            httpClient.BaseAddress.AbsoluteUri,
                            "api/sequential/monitor"),
                        postContent).Result.Content.ReadAsStringAsync().Result))
                {
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Waiting for the sequential message processing task completion...");
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                    postContent = new StringContent(json, Encoding.UTF8, "application/json");
                }
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Sequential message processing task completed.");

                // Retrieves statistics
                json = JsonConvert.SerializeObject(
                    new Payload
                    {
                        WorkerId = workerId
                    });
                postContent = new StringContent(json, Encoding.UTF8, "application/json");

                response = httpClient.PostAsync(Combine(httpClient.BaseAddress.AbsoluteUri, "api/statistics"), postContent).Result;
                response.EnsureSuccessStatusCode();
                json = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                Statistics statistics = JsonConvert.DeserializeObject(json, typeof(Statistics)) as Statistics;

                if (statistics == null)
                {
                    return;
                }

                // Prints statistics
                PrintStatistics(statistics);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private static void TestParallelMessageProcessingViaGatewayService()
        {
            try
            {
                // Sets device name used in the ActorId constructor
                const string workerId = "worker01";

                // Creates http proxy
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = new Uri(GatewayUrl)
                };
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] HttpClient for the [{workerId}] created.");

                // Creates N messages
                List<Message> messageList = CreateMessageList();

                List<Task> taskList = new List<Task>();
                Func<string, Task> waitHandler = async messageId =>
                {
                    try
                    {
                        string jsonString = JsonConvert.SerializeObject(
                            new Payload
                            {
                                WorkerId = workerId,
                                Message = new Message
                                {
                                    MessageId = messageId
                                }
                            });
                        StringContent stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                        while (bool.Parse(
                            await (await httpClient.PostAsync(
                                Combine(
                                    httpClient.BaseAddress.AbsoluteUri,
                                    "api/parallel/monitor"),
                                stringContent)).Content.ReadAsStringAsync()))
                        {
                            Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Waiting for [{messageId}] parallel processing task completion...");
                            Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                            stringContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
                        }
                        Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] [{messageId}] parallel processing tasks completed.");
                    }
                    catch (Exception ex)
                    {
                        PrintException(ex);
                    }
                };

                string json;
                StringContent postContent;
                HttpResponseMessage response;

                foreach (Message message in messageList)
                {
                    json = JsonConvert.SerializeObject(
                        new Payload
                        {
                            WorkerId = workerId,
                            Message = message
                        });
                    postContent = new StringContent(json, Encoding.UTF8, "application/json");
                    response = httpClient.PostAsync(Combine(httpClient.BaseAddress.AbsoluteUri, "api/parallel/start"), postContent).Result;
                    response.EnsureSuccessStatusCode();
                    bool value;
                    if (bool.TryParse(response.Content.ReadAsStringAsync().Result, out value) && !value)
                    {
                        continue;
                    }
                    taskList.Add(waitHandler(message.MessageId));
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Message [{JsonSerializerHelper.Serialize(message)}] sent.");
                }

                Task.WaitAll(taskList.ToArray());
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Parallel message processing tasks completed.");

                // Retrieves statistics
                json = JsonConvert.SerializeObject(
                    new Payload
                    {
                        WorkerId = workerId
                    });
                postContent = new StringContent(json, Encoding.UTF8, "application/json");

                response = httpClient.PostAsync(Combine(httpClient.BaseAddress.AbsoluteUri, "api/statistics"), postContent).Result;
                response.EnsureSuccessStatusCode();
                json = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                Statistics statistics = JsonConvert.DeserializeObject(json, typeof(Statistics)) as Statistics;

                if (statistics == null)
                {
                    return;
                }

                // Prints statistics
                PrintStatistics(statistics);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private static void TestGetProcessingStatisticsViaActorProxy()
        {
            try
            {
                // Sets device name used in the ActorId constructor
                const string workerId = "worker01";

                // Creates actor proxy
                IWorkerActor proxy = ActorProxy.Create<IWorkerActor>(new ActorId(workerId), WorkerActorServiceUri);
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] ActorProxy for the [{workerId}] created.");

                // Retrieves statistics
                Statistics statistics = proxy.GetProcessingStatisticsAsync().Result;
                if (statistics == null)
                {
                    return;
                }

                // Prints statistics
                PrintStatistics(statistics);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private static void TestGetProcessingStatisticsViaGateway()
        {
            try
            {
                // Sets device name used in the ActorId constructor
                const string workerId = "worker01";

                // Creates http proxy
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = new Uri(GatewayUrl)
                };
                httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] HttpClient for the [{workerId}] created.");

                // Retrieves statistics
                string json = JsonConvert.SerializeObject(
                    new Payload
                    {
                        WorkerId = workerId
                    });
                StringContent postContent = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = httpClient.PostAsync(Combine(httpClient.BaseAddress.AbsoluteUri, "api/statistics"), postContent).Result;
                response.EnsureSuccessStatusCode();
                json = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                Statistics statistics = JsonConvert.DeserializeObject(json, typeof(Statistics)) as Statistics;

                if (statistics == null)
                {
                    return;
                }

                // Prints statistics
                PrintStatistics(statistics);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private static void StopSequentialProcessingTaskViaActorProxy()
        {
            try
            {
                // Sets device name used in the ActorId constructor
                const string workerId = "worker01";

                // Creates actor proxy
                IWorkerActor proxy = ActorProxy.Create<IWorkerActor>(new ActorId(workerId), WorkerActorServiceUri);
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] ActorProxy for the [{workerId}] created.");

                // Enqueues 1 message with 10 steps. Note: the sequential message processing task emulates K steps of H seconds each to process each message.
                // However, since it runs on a separate task not awaited by the actor ProcessMessageAsync method,
                // the method itself returns immediately without waiting the the task completion.
                // This allows the actor to continue to enqueue requests, while processing messages on a separate task.
                List<Message> messageList = CreateMessageList(1, 10);

                foreach (Message message in messageList)
                {
                    proxy.StartSequentialProcessingAsync(message).Wait();
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Message [{JsonSerializerHelper.Serialize(message)}] sent.");
                }

                // Waits a couple of seconds before stopping the sequential message processing
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Wait 5 seconds before stopping the sequential message processing...");
                for (int i = 5; i > 0; i--)
                {
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] {i}...");
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }

                // Stops the sequential message processing
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Stopping the sequential message processing...");
                proxy.StopSequentialProcessingAsync().Wait();

                while (proxy.IsSequentialProcessingRunningAsync().Result)
                {
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Waiting for the sequential message processing task to stop...");
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Sequential message processing task successfully stopped.");

                // Retrieves statistics
                Statistics statistics = proxy.GetProcessingStatisticsAsync().Result;
                if (statistics == null)
                {
                    return;
                }

                // Prints statistics
                PrintStatistics(statistics);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        private static void StopParallelProcessingTaskViaActorProxy()
        {
            try
            {
                // Sets device name used in the ActorId constructor
                const string workerId = "worker01";

                // Creates actor proxy
                IWorkerActor proxy = ActorProxy.Create<IWorkerActor>(new ActorId(workerId), WorkerActorServiceUri);
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] ActorProxy for the [{workerId}] created.");

                // Enqueues 1 message with 10 steps. Note: the sequential message processing task emulates K steps of H seconds each to process each message.
                // However, since it runs on a separate task not awaited by the actor ProcessMessageAsync method,
                // the method itself returns immediately without waiting the the task completion.
                // This allows the actor to continue to enqueue requests, while processing messages on a separate task.
                List<Message> messageList = CreateMessageList(1, 10);

                foreach (Message message in messageList)
                {
                    proxy.StartParallelProcessingAsync(message).Wait();
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Message [{JsonSerializerHelper.Serialize(message)}] sent.");
                }

                // Waits a couple of seconds before stopping the sequential message processing
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Wait 5 seconds before stopping the parallel message processing...");
                for (int i = 5; i > 0; i--)
                {
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] {i}...");
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }

                // Stops the sequential message processing
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Stopping the parallel message processing...");
                proxy.StopParallelProcessingAsync(messageList.First().MessageId).Wait();

                while (proxy.IsParallelProcessingRunningAsync(messageList[0].MessageId).Result)
                {
                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Waiting for the parallel message processing task to stop...");
                    Task.Delay(TimeSpan.FromSeconds(1)).Wait();
                }
                Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Parallel message processing task successfully stopped.");

                // Retrieves statistics
                Statistics statistics = proxy.GetProcessingStatisticsAsync().Result;
                if (statistics == null)
                {
                    return;
                }

                // Prints statistics
                PrintStatistics(statistics);
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        public static void EnumerateActorsViaActorProxy()
        {
            try
            {
                FabricClient fabricClient = new FabricClient();
                // Creates Uri list
                List<Uri> uriList = new List<Uri>
                {
                    WorkerActorServiceUri,
                    QueueActorServiceUri,
                    ProcessorActorServiceUri
                };

                foreach (Uri uri in uriList)
                {
                    ServicePartitionList partitionList = fabricClient.QueryManager.GetPartitionListAsync(uri).Result;

                    Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] [{uri}]:");
                    int total = 0;

                    foreach (Partition partition in partitionList)
                    {
                        Int64RangePartitionInformation partitionInformation = partition.PartitionInformation as Int64RangePartitionInformation;
                        if (partitionInformation == null)
                        {
                            continue;
                        }
                        long partitionKey = partitionInformation.LowKey;

                        // Creates CancellationTokenSource
                        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

                        // Creates ContinuationToken
                        ContinuationToken continuationToken = null;

                        // Creates ActorServiceProxy for WorkerActorService
                        IActorService actorServiceProxy = ActorServiceProxy.Create(uri, partitionKey);
                        int actorCount = 0;
                        List<ActorInformation> actorInformationList = new List<ActorInformation>();
                        do
                        {
                            PagedResult<ActorInformation> queryResult = actorServiceProxy.GetActorsAsync(continuationToken, cancellationTokenSource.Token).Result;
                            if (queryResult.Items.Any())
                            {
                                actorInformationList.AddRange(queryResult.Items);
                                actorCount += queryResult.Items.Count();
                            }
                            continuationToken = queryResult.ContinuationToken;
                        } while (continuationToken != null);

                        // Prints results
                        Console.WriteLine($"                          > Partition [{partitionInformation.Id}] contains [{actorCount}] actors.");
                        foreach (ActorInformation actorInformation in actorInformationList)
                        {
                            Console.WriteLine($"                            > ActorId [{actorInformation.ActorId}]");
                        }
                        total += actorCount;
                    }

                    // Prints results
                    Console.WriteLine($"                          > Total: [{total}] actors");
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        #endregion

        #region Private Static Methods

        private static int SelectOption()
        {
            // Create a line

            int optionCount = TestList.Count + 1;

            Console.WriteLine("Select an option:");
            Console.WriteLine(Line);

            for (int i = 0; i < TestList.Count; i++)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("[{0}] ", i + 1);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write(TestList[i].Name);
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(" - " + TestList[i].Description);
            }

            // Add exit option
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[{0}] ", optionCount);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Exit");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(" - Close the test application.");
            Console.WriteLine(Line);

            // Select an option
            Console.WriteLine($"Press a key between [1] and [{optionCount}]: ");
            char key = 'a';
            while (key < '1' || key > ('1' + optionCount))
            {
                key = Console.ReadKey(true).KeyChar;
            }
            return key - '1' + 1;
        }

        private static void PrintException(
            Exception ex,
            [CallerFilePath] string sourceFilePath = "",
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            // Write Line
            Console.WriteLine(Line);

            InternalPrintException(ex, sourceFilePath, memberName, sourceLineNumber);

            // Write Line
            Console.WriteLine(Line);
        }

        private static void InternalPrintException(
            Exception ex,
            string sourceFilePath = "",
            string memberName = "",
            int sourceLineNumber = 0)
        {
            AggregateException exception = ex as AggregateException;
            if (exception != null)
            {
                foreach (Exception e in exception.InnerExceptions)
                {
                    if (sourceFilePath != null)
                    {
                        InternalPrintException(e, sourceFilePath, memberName, sourceLineNumber);
                    }
                }
                return;
            }
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"{ex.GetType().Name}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            string fileName = null;
            if (File.Exists(sourceFilePath))
            {
                FileInfo file = new FileInfo(sourceFilePath);
                fileName = file.Name;
            }
            Console.Write(string.IsNullOrWhiteSpace(fileName) ? "Unknown" : fileName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(string.IsNullOrWhiteSpace(memberName) ? "Unknown" : memberName);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(":");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(sourceLineNumber.ToString(CultureInfo.InvariantCulture));
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("]");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(": ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(!string.IsNullOrWhiteSpace(ex.Message) ? ex.Message : "An error occurred.");
        }

        private static List<Message> CreateMessageList(int messages = -1, int stepCount = -1)
        {
            List<Message> messageList = new List<Message>();
            Random random = new Random();
            stepCount = stepCount < 0 ? Steps : stepCount;
            messages = messages < 0 ? MessageCount : messages;
            for (int i = 0; i < messages; i++)
            {
                messageList.Add(
                    new Message
                    {
                        MessageId = $"{++Id:000}",
                        Body = $"value: {random.Next(1, 51)}",
                        Properties = new Dictionary<string, object>
                        {
                            {DelayProperty, TimeSpan.FromSeconds(Delay)},
                            {StepsProperty, stepCount}
                        }
                    });
            }
            return messageList;
        }

        private static void PrintTestParameters(string testName)
        {
            Console.WriteLine(Line);
            Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Test Name: [{testName}]");
            Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Message Count: [{MessageCount}]");
            Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Processing Steps: [{Steps}]");
            Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Delay in Seconds: [{Delay}]");
            Console.WriteLine(Line);
        }

        private static void PrintStatistics(Statistics statistics)
        {
            // Writes statistics
            Console.WriteLine($" - [{DateTime.Now.ToLocalTime()}] Statistics:");
            Console.WriteLine();
            Console.WriteLine($"                          > Received = [{statistics.Received}]");
            Console.WriteLine($"                          > Complete = [{statistics.Complete}]");
            Console.WriteLine($"                          > Stopped  = [{statistics.Stopped}]");
            Console.WriteLine($"                          > MinValue = [{statistics.MinValue}]");
            Console.WriteLine($"                          > MaxValue = [{statistics.MaxValue}]");
            Console.WriteLine($"                          > TotValue = [{statistics.TotalValue}]");
            Console.WriteLine($"                          > AvgValue = [{statistics.AverageValue}]");
            Console.WriteLine();

            // Writes latest N results
            if (!statistics.Results.Any())
            {
                return;
            }
            Console.WriteLine($"                          Latest [{statistics.Results.Count()}] results:");
            Console.WriteLine();
            foreach (Result result in statistics.Results)
            {
                Console.WriteLine($"                          > MessageId = [{result.MessageId}] ReturnValue = [{result.ReturnValue}]");
            }
        }

        private static void ReadConfiguration()
        {
            try
            {
                GatewayUrl = ConfigurationManager.AppSettings[GatewayUrlParameter] ?? DefaultGatewayUrl;
                if (string.IsNullOrWhiteSpace(GatewayUrl))
                {
                    throw new ArgumentException($"The [{GatewayUrlParameter}] setting in the configuration file is null or invalid.");
                }
                string value = ConfigurationManager.AppSettings[MessageCountParameter];
                if (!int.TryParse(value, out MessageCount))
                {
                    MessageCount = DefaultMessageCount;
                }
                value = ConfigurationManager.AppSettings[StepsParameter];
                if (!int.TryParse(value, out Steps))
                {
                    Steps = DefaultSteps;
                }
                value = ConfigurationManager.AppSettings[DelayParameter];
                if (!int.TryParse(value, out Delay))
                {
                    Delay = DefaultDelay;
                }
            }
            catch (Exception ex)
            {
                PrintException(ex);
            }
        }

        public static string Combine(string uri1, string uri2)
        {
            uri1 = uri1.TrimEnd('/');
            uri2 = uri2.TrimStart('/');
            return $"{uri1}/{uri2}";
        }

        #endregion
    }

    internal class Test
    {
        #region Public Properties

        public string Name { get; set; }

        public string Description { get; set; }

        public Action Action { get; set; }

        #endregion
    }
}