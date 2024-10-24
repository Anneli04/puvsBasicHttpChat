using System.Collections.Generic;
using Npgsql;
using Data;
using Microsoft.Data.Sqlite;

namespace Server
{
    /// <summary>
    /// Verwaltet die Interaktion mit der PostgreSQL-Datenbank für Chatnachrichten.
    /// </summary>
    public class DatabaseManager : IDisposable
    {
        private const string ConnectionString = "Host=192.168.0.18;Port=5432;Username=chatb;Password=bernhardt2024;Database=chat_db";
        private NpgsqlConnection connection;

        public DatabaseManager()
        {
            connection = new NpgsqlConnection(ConnectionString);
            connection.Open();
        }


        /// <summary>
        /// Datenbank wird initialisiert, indem, falls nicht vorhanden, die Tabelle ChatMessages erstellt wird.
        /// </summary>
        public void InitializeDatabase()
        {
            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS ChatMessages (
                    Id SERIAL PRIMARY KEY,
                    Sender TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp TIMESTAMP
                );
            ";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Speichert eine Chatnachricht in der Datenbank.
        /// </summary>
        public void SaveMessage(ChatMessage message)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            @"
                INSERT INTO ChatMessages (Sender, Content, Timestamp) VALUES (@sender, @content, @timestamp);
            ";
            command.Parameters.AddWithValue("sender", message.Sender);
            command.Parameters.AddWithValue("content", message.Content);
            command.Parameters.AddWithValue("timestamp", message.Timestamp);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Ruft alle Chatnachrichten aus der Datenbank ab.
        /// </summary>
        public List<ChatMessage> GetAllMessages()
        {
            var messages = new List<ChatMessage>();

            var command = connection.CreateCommand();
            command.CommandText = "SELECT Sender, Content, Timestamp FROM ChatMessages ORDER BY Id;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                messages.Add(new ChatMessage
                {
                    Sender = reader.GetString(0),
                    Content = reader.GetString(1),
                    // Konvertiert den UTC-Zeitstempel in die lokale Zeit.
                    Timestamp = reader.GetDateTime(2)
                });
            }

            return messages; // Gibt die Liste der Chatnachrichten zurück.
        }

        public void Dispose()
        {
            connection.Dispose();
        }
    }
}