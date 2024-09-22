
public class AppLogger {

  public static void LogSolution(string Message) {
    Console.WriteLine(Message);
  }

  public static void LogStep(string Message) {
    Console.WriteLine();
    Console.WriteLine(" > " + Message);
  }

  public static void LogSubstep(string Message) {
    Console.WriteLine("   - " + Message);
  }

  public static void LogOperationStart(string Message) {
    Console.WriteLine();
    Console.Write(" > " + Message);
  }

  public static void LogOperationInProgress() {
    Console.Write(".");
  }

  public static void LogOperationComplete() {
    Console.WriteLine();
  }

  public static void LogException(Exception ex) {
    var originalColor = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine();
    Console.WriteLine($"ERROR: {ex.GetType().ToString()}");
    Console.WriteLine(ex.Message);
    Console.WriteLine();
    Console.ForegroundColor = originalColor;
  }
}
