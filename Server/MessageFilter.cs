using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace Server
{
    public static class MessageFilter
    {
        private static HashSet<string> _schimpfwoerter;
        private static HashSet<string> _abkuerzungen;
        private static HashSet<string> _unangebrachteWoerter;

        static MessageFilter()
        {
            LoadFilterConfig();
        }

        private static void LoadFilterConfig()
        {
            var json = File.ReadAllText("filterConfig.json");
            var config = JsonConvert.DeserializeObject<FilterConfig>(json);
            _schimpfwoerter = new HashSet<string>(config.Schimpfwoerter, StringComparer.OrdinalIgnoreCase);
            _abkuerzungen = new HashSet<string>(config.Abkuerzungen, StringComparer.OrdinalIgnoreCase);
            _unangebrachteWoerter = new HashSet<string>(config.unangebrachteWoerter, StringComparer.OrdinalIgnoreCase);
        }

        public static string FilterMessage(string message)
        {
            // Filtere Schimpfwörter
            foreach (var word in _schimpfwoerter)
            {
                string pattern = $@"\b{Regex.Escape(word)}\b";
                message = Regex.Replace(message, pattern, "***", RegexOptions.IgnoreCase);
            }

            // Filtere unangebrachte Wörter
            foreach (var word in _unangebrachteWoerter)
            {
                string pattern = $@"\b{Regex.Escape(word)}\b";
                message = Regex.Replace(message, pattern, "***", RegexOptions.IgnoreCase);
            }

            // Filtere Abkürzungen
            foreach (var abk in _abkuerzungen)
            {
                message = message.Replace(abk, "***"); // Evtl. ohne Regex, da es keine Wortgrenze braucht
            }

            return message;
        }

        private class FilterConfig
        {
            public List<string>? Schimpfwoerter { get; set; }
            public List<string>? Abkuerzungen { get; set; }
            public List<string>? unangebrachteWoerter { get; set; }
        }
    }
}
