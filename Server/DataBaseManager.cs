﻿using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Data;

namespace Server
{
    public class DatabaseManager
    {
        private const string ConnectionString = "Data Source=chat.db"; // Pfad zur SQLite-Datenbankdatei

        public void InitializeDatabase()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                CREATE TABLE IF NOT EXISTS ChatMessages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Sender TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP
                );
            ";
            command.ExecuteNonQuery();
        }

        public void SaveMessage(ChatMessage message)
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText =
            @"
                INSERT INTO ChatMessages (Sender, Content)
                VALUES ($sender, $content);
            ";
            command.Parameters.AddWithValue("$sender", message.Sender);
            command.Parameters.AddWithValue("$content", message.Content);
            command.ExecuteNonQuery();
        }

        public List<ChatMessage> GetAllMessages()
        {
            var messages = new List<ChatMessage>();

            using var connection = new SqliteConnection(ConnectionString);
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
                    Timestamp = reader.GetDateTime(2)
                });
            }

            return messages;
        }
    }
}
