using CloudFileManager.Shared.Common;
using Microsoft.Extensions.DependencyInjection;

namespace CloudFileManager.Presentation.ConsoleApp;

/// <summary>
/// ConsoleCommandLoop 類別，負責控制互動迴圈與指令生命週期。
/// </summary>
public sealed class ConsoleCommandLoop
{
    private readonly IConsoleCommandParser _parser;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ConsoleSessionState _sessionState;

    /// <summary>
    /// 初始化 ConsoleCommandLoop。
    /// </summary>
    public ConsoleCommandLoop(IConsoleCommandParser parser, IServiceScopeFactory scopeFactory, ConsoleSessionState sessionState)
    {
        _parser = parser;
        _scopeFactory = scopeFactory;
        _sessionState = sessionState;
    }

    /// <summary>
    /// 執行主迴圈。
    /// </summary>
    public void Run()
    {
        PrintWelcome();

        while (true)
        {
            System.Console.Write($"當前路徑:{_sessionState.CurrentDirectoryPath}> ");
            string? input = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            try
            {
                ConsoleCommand command = _parser.Parse(input);
                using IServiceScope scope = _scopeFactory.CreateScope();
                IConsoleCommandExecutor executor = scope.ServiceProvider.GetRequiredService<IConsoleCommandExecutor>();
                bool shouldContinue = executor.Execute(command);
                if (!shouldContinue)
                {
                    return;
                }
            }
            catch (InvalidOperationException ex)
            {
                System.Console.WriteLine($"[ERROR] {ex.Message}");
            }
            catch (ArgumentException ex)
            {
                System.Console.WriteLine($"[ERROR] {ex.Message}");
            }
            catch
            {
                System.Console.WriteLine($"[ERROR] Command execution failed due to an unexpected error. [{OperationErrorCodes.CommandExecutionUnexpected}]");
            }
        }
    }

    /// <summary>
    /// 輸出Welcome。
    /// </summary>
    private static void PrintWelcome()
    {
        System.Console.WriteLine("[INFO] CloudFileManager CLI");
        System.Console.WriteLine("[INFO] Type 'help' to see commands. Type 'exit' to quit.");
    }
}
