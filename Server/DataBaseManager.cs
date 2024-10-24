using System.Collections.Generic;
using Npgsql;
using Data;
using Microsoft.Data.Sqlite;

namespace Server
{
    /// <summary>
    /// Verwaltet die Interaktion mit der PostgreSQL-Datenbank für Chatnachrichten.
    /// </summary>
    public class DatabaseManager
    {
        private const string ConnectionString = "Host=192.168.0.18;Port=5432;Username=chatb;Password=bernhardt2024;Database=chat_db";

        /// <summary>
        /// Datenbank wird initialisiert, indem, falls nicht vorhanden, die Tabelle ChatMessages erstellt wird.
        /// </summary>
        public void InitializeDatabase()
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS ChatMessages (
                    Id SERIAL PRIMARY KEY,
                    Sender TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                );
            ";
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Speichert eine Chatnachricht in der Datenbank.
        /// </summary>
        public void SaveMessage(ChatMessage message)
        {
            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                INSERT INTO ChatMessages (Sender, Content)
                VALUES (@sender, @content); // Verwende @ für Parameter
            ";
            command.Parameters.AddWithValue("sender", message.Sender);
            command.Parameters.AddWithValue("content", message.Content);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Ruft alle Chatnachrichten aus der Datenbank ab.
        /// </summary>
        public List<ChatMessage> GetAllMessages()
        {
            var messages = new List<ChatMessage>();

            using var connection = new NpgsqlConnection(ConnectionString);
            connection.Open();

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
                    Timestamp = reader.GetDateTime(2).ToLocalTime()
                });
            }

            return messages; // Gibt die Liste der Chatnachrichten zurück.
        }
    }
}