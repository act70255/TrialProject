using System.Text;

namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// ConsoleCommandParser 類別，負責解析輸入資料與命令內容。
/// </summary>
public sealed class ConsoleCommandParser : IConsoleCommandParser
{
    /// <summary>
    /// 解析資料。
    /// </summary>
    public ConsoleCommand Parse(string input)
    {
        List<string> tokens = Tokenize(input);
        if (tokens.Count == 0)
        {
            throw new InvalidOperationException("Empty command.");
        }

        string name = tokens[0].ToLowerInvariant();
        IReadOnlyList<string> args = tokens.Skip(1).ToList();
        return new ConsoleCommand(name, args);
    }

    /// <summary>
    /// 轉換為kenize。
    /// </summary>
    private static List<string> Tokenize(string input)
    {
        List<string> tokens = new();
        StringBuilder current = new();
        bool inQuote = false;

        foreach (char ch in input)
        {
            if (ch == '"')
            {
                inQuote = !inQuote;
                continue;
            }

            if (char.IsWhiteSpace(ch) && !inQuote)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            current.Append(ch);
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }
}
