using System;
using System.Collections.Generic;
using System.Linq;

namespace EasySave_v1._0
{
    public class CommandLineParser
    {
        private static readonly HashSet<string> ValidCommands = new(StringComparer.OrdinalIgnoreCase)
        {
            "create",
            "execute",
            "list",
            "help",
            "all",
            "full",
            "diff",
            "delete"
        };

        // Parse integer indices from arguments. Supports single numbers, comma-separated lists and ranges (e.g. "1-3").
        public static int[] ParseArguments(string[] args)
        {
            if (args == null || args.Length == 0) return Array.Empty<int>();

            var results = new List<int>();

            foreach (var raw in args)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;

                // skip known command tokens
                if (IsValidCommand(raw)) continue;

                // normalize separators: allow comma and semicolon
                var normalized = raw.Replace(';', ',');
                // handle comma separated values
                if (normalized.Contains(','))
                {
                    var parts = normalized.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var p in parts)
                        ParseTokenToList(p, results);
                }
                else
                {
                    ParseTokenToList(normalized, results);
                }
            }

            return results.Distinct().OrderBy(i => i).ToArray();
        }

        public static bool IsValidCommand(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) return false;
            var token = arg.Trim().ToLowerInvariant();
            // treat option style tokens as commands too
            if (token.StartsWith("--") || token.StartsWith("-"))
                token = token.TrimStart('-');

            return ValidCommands.Contains(token);
        }

        private static void ParseTokenToList(string token, List<int> list)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            // range like 2-5
            if (token.Contains('-'))
            {
                var parts = token.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && int.TryParse(parts[0], out var a) && int.TryParse(parts[1], out var b))
                {
                    var start = Math.Min(a, b);
                    var end = Math.Max(a, b);
                    for (int i = start; i <= end; i++) list.Add(i);
                }
                return;
            }

            if (int.TryParse(token, out var v))
                list.Add(v);
        }
    }
}
