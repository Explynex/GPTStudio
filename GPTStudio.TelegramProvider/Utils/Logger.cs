namespace GPTStudio.TelegramProvider.Utils;
internal static class Logger
{
    public static void Print(string value,bool endlEnd = true,bool endlStart = false,bool beforeCommand = false, ConsoleColor color = ConsoleColor.White)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write((endlStart ? "\n" : "") + DateTime.Now + "\t| ");
        Console.ForegroundColor = color;
        Console.Write(value + (endlEnd ? "\n" : ""));

        if(beforeCommand)
            Print("Command: /", false,color: ConsoleColor.Cyan);
        else
            Console.ForegroundColor = ConsoleColor.White;
    }

    public static void PrintError(string value)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("\n" + DateTime.Now + "\t| ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Error:" + value);
        Console.ForegroundColor = ConsoleColor.White;
    }
}
