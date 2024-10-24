using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Server
{
    /// <summary>
    /// Klasse zur Filterung von Nachrichten, die Spam oder unerwünschte Inhalte beinhalten
    /// </summary>
    public static class MessageFilter
    {
        // Verwendung von HashSets zur Speicherung von unangemessenen Inhalten.
        private static HashSet<string> _schimpfwoerter;
        private static HashSet<string> _abkuerzungen;
        private static HashSet<string> _unangebrachteWoerter;

        // Dictionary zur Beobachtung der Zeitstempel der gesendeten Nachrichten für jeden Benutzer.
        private static ConcurrentDictionary<string, List<DateTime>> _userMessageTimestamps = new();
        private const int SpamThreshold = 5; // Anzahl der Nachrichten, die als Spam gelten
        private const int SpamTimeFrameInSeconds = 10; // Zeitrahmen für die Spam-Erkennung

        /// <summary>
        /// Konstruktor, der die Filterkonfiguration beim Laden der Klasse initialisiert.
        /// </summary>
        static MessageFilter()
        {
            LoadFilterConfig();
        }

        /// <summary>
        /// Lädt die Filterkonfiguration und initialisiert die Wörterlisten.
        /// </summary>
        private static void LoadFilterConfig()
        {
            var json = File.ReadAllText("filterConfig.json");
            var config = JsonConvert.DeserializeObject<FilterConfig>(json);
            _schimpfwoerter = new HashSet<string>(config.Schimpfwoerter, StringComparer.OrdinalIgnoreCase);
            _abkuerzungen = new HashSet<string>(config.Abkuerzungen, StringComparer.OrdinalIgnoreCase);
            _unangebrachteWoerter = new HashSet<string>(config.unangebrachteWoerter, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Filtert die Nachrichten und gibt die gefilterte Nachricht zurück.
        /// </summary>
        public static string FilterMessage(string sender, string message)
        {
            // Überprüft auf Spam
            if (IsSpam(sender))
            {
                return "*** Spam detected ***"; // Gibt an, dass Spam erkannt wurde
            }

            // Filtert unangemessene Inhalte
            message = FilterUnwantedContent(message);

            // Verfolgt den Zeitstempel der gesendeten Nachricht
            TrackMessage(sender);

            return message; // Gibt die gefilterte Nachricht zurück
        }

        /// <summary>
        /// Filtert unerwünschte Inhalte aus der Nachricht.
        /// </summary>
        private static string FilterUnwantedContent(string message)
        {
            // Filterung des unangemessenen Inhalts
            message = ReplaceWords(message, _schimpfwoerter, true);
            message = ReplaceWords(message, _unangebrachteWoerter, true);
            message = ReplaceWords(message, _abkuerzungen, true);

            return message; // Gibt die bearbeitete Nachricht zurück
        }

        /// <summary>
        /// Ersetzt unangemessene Wörter in der Nachricht durch Sterne.
        /// </summary>
        private static string ReplaceWords(string message, HashSet<string> words, bool caseInsensitive = false)
        {
            foreach (var word in words)
            {
                string pattern = $@"(?i){Regex.Escape(word)}"; // Filtert die Wörter.
                message = Regex.Replace(message, pattern, m => new string('*', m.Value.Length), caseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None); // Bei der Umwandlung in Sterne spielt die Groß- und Kleinschreibung keine Rolle.
            }
            return message; // Gibt die bearbeitete Nachricht zurück.
        }

        /// <summary>
        /// Überprüft, ob die Nachrichten eines Benutzers als Spam angesehen werden können.
        /// </summary>
        private static bool IsSpam(string sender)
        {
            var now = DateTime.Now;
            var userList = _userMessageTimestamps.GetOrAdd(sender, k => new List<DateTime>());

            // Entfernt Zeitstempel, die älter als die festgelegten Zeitgrenzen sind.
            userList.RemoveAll(ts => (now - ts).TotalSeconds > SpamTimeFrameInSeconds);

            // Überprüft, ob die Anzahl der Nachrichten die Grenze überschreitet.
            return userList.Count > SpamThreshold;
        }

        /// <summary>
        /// Verfolgt die gesendeten Nachrichten eines Benutzers, indem Zeitstempel gespeichert werden.
        /// </summary>
        private static void TrackMessage(string sender)
        {
            _userMessageTimestamps.GetOrAdd(sender, k => new List<DateTime>()).Add(DateTime.Now);
        }

        /// <summary>
        /// Konfiguration für die Filtereinstellungen.
        /// </summary>
        private class FilterConfig
        {
            public List<string>? Schimpfwoerter { get; set; } // Liste der Schimpfwörter
            public List<string>? Abkuerzungen { get; set; } // Liste der Abkürzungen
            public List<string>? unangebrachteWoerter { get; set; } // Liste der unangebrachten Wörter
        }
    }
}
