using System.Collections.Concurrent;
using Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Server
{
    public class ChatServer
    {
        private readonly ConcurrentQueue<ChatMessage> messageQueue = new();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<ChatMessage>> waitingClients = new();
        private readonly object lockObject = new();
        private readonly List<ChatMessage> chatHistory = new(); // Liste für den Chatverlauf

        public void Configure(IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/messages", async context =>
                {
                    var tcs = new TaskCompletionSource<ChatMessage>();
                    context.Request.Query.TryGetValue("id", out var rawId);
                    var id = rawId.ToString();

                    Console.WriteLine($"Client '{id}' registered");

                    // Registriere den Client
                    var error = true;
                    lock (this.lockObject)
                    {
                        if (this.waitingClients.ContainsKey(id))
                        {
                            if (this.waitingClients.TryRemove(id, out _))
                            {
                                Console.WriteLine($"Client '{id}' removed from waiting clients");
                            }
                        }

                        if (this.waitingClients.TryAdd(id, tcs))
                        {
                            Console.WriteLine($"Client '{id}' added to waiting clients");
                            error = false;
                        }
                    }

                    if (error)
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        await context.Response.WriteAsync("Internal server error.");
                    }

                    var message = await tcs.Task;
                    Console.WriteLine($"Client '{id}' received message: {message.Content}");
                    await context.Response.WriteAsJsonAsync(message);
                });

                endpoints.MapPost("/messages", async context =>
                {
                    var message = await context.Request.ReadFromJsonAsync<ChatMessage>();

                    if (message == null || string.IsNullOrEmpty(message.Sender))
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("Message invalid.");
                        return;
                    }

                    // Filterung des Nachrichteninhalts
                    message.Content = MessageFilter.FilterMessage(message.Sender, message.Content);
                    Console.WriteLine($"Received message from client: {message.Content}");

                    message.Timestamp = DateTime.Now;

                    chatHistory.Add(message); // Füge die Nachricht zum Verlauf hinzu

                    this.messageQueue.Enqueue(message);

                    lock (this.lockObject)
                    {
                        foreach (var (id, client) in this.waitingClients)
                        {
                            Console.WriteLine($"Broadcasting to client '{id}'");
                            client.TrySetResult(message);
                        }
                    }

                    Console.WriteLine($"Broadcasted message to all clients: {message.Content}");
                    context.Response.StatusCode = StatusCodes.Status201Created;
                    await context.Response.WriteAsync("Message received and processed.");
                });

                // Endpunkt zum Abrufen des Chatverlaufs
                endpoints.MapGet("/history", async context =>
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(chatHistory);
                });
            });
        }
    }
}
