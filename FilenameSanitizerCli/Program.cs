using FilenameSanitizer;

namespace FilenameSanitizerCli;

internal sealed class Program
{
    public static void Main(string[] args)
    {
        var (workingFolder, semiCommaSeparatedPatterns) = ParseArguments(args);

        if (string.IsNullOrEmpty(workingFolder))
        {
            ShowHelp();
            return;
        }

        var filenameSanitizer = new FilenameSanitizer.FilenameNormalizer(workingFolder);

        try
        {
            if (workingFolder == ".")
            {
                workingFolder = Environment.CurrentDirectory; // Default to current directory if no folder is specified
            }
            var resolvedFolder = ResolveAndValidateFolder(workingFolder, filenameSanitizer);
            if (resolvedFolder == null)
            {
                return;
            }

            // Update working folder with resolved path
            workingFolder = resolvedFolder;

            ProcessSanitization(workingFolder, semiCommaSeparatedPatterns, filenameSanitizer);

            // Output results
            ConsoleOutputLogs(filenameSanitizer.Logger);

            // Write log file to the target folder
            filenameSanitizer.Logger.FlushToFile();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: {ex.Message}");
            filenameSanitizer.Logger.Errors.Add(ex.Message);
            Console.ResetColor();
        }
        finally
        {
            PrintCompletion(filenameSanitizer.Logger);
        }

    }

    /// <summary>
    ///    Displays the help information for using the FilenameSanitizerCli application, including usage instructions   
    ///   and examples of command-line arguments.
    /// </summary>
    /// <remarks>
    ///   The help information outlines the default settings file, the default replace patterns file, and the maximum filename length. It also provides usage examples for sanitizing filenames in the current directory, a specified folder, and removing specific patterns from filenames.
    /// </remarks>
    private static void ShowHelp()
    {
        Console.WriteLine("FilenameSanitizerCli - Sanitize filenames for OS compatibility");
        Console.WriteLine("Default settings file: " + SanitizerConstants.SanitizerSettingsFile);
        Console.WriteLine("Default replace patterns file: " + SanitizerConstants.SanitizerReplacePatternsFile);
        Console.WriteLine("File path over " + SanitizerConstants.MaxPosixNameLength + " characters, will be cut off.");

        Console.WriteLine("\nUsage:");
        Console.WriteLine(
            "  FilenameSanitizerCli                     - Sanitize all filenames in folder - in current directory.");
        Console.WriteLine(
            "  FilenameSanitizerCli <folder>            - Sanitize all filenames in folder, using settings in setting files.");
        Console.WriteLine(
            "  FilenameSanitizerCli <folder> <patterns> - Remove patterns from filenames, separated with semicolons");
        Console.WriteLine("\nExamples:");
        Console.WriteLine("  FilenameSanitizerCli .");
        Console.WriteLine(@"  FilenameSanitizerCli C:\MyFiles");
        Console.WriteLine(@"  FilenameSanitizerCli C:\MyFiles prefix-;_old;.bak");
    }

    /// <summary>
    /// Resolves the specified folder (expanding "." to the current directory) and validates that it exists.
    /// </summary>
    /// <param name="folder">The folder path provided by the user.</param>
    /// <param name="filenameSanitizer">An instance of `IFilenameSanitizer` used to record errors in the logger.</param>
    /// <returns>The resolved folder path when valid; otherwise <c>null</c> when the folder is missing or inaccessible.</returns>
    private static string? ResolveAndValidateFolder(string folder, FilenameNormalizer filenameSanitizer)
    {
        if (folder == ".")
        {
            folder = Environment.CurrentDirectory;
        }

        if (!Directory.Exists(folder))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Error: The specified folder '{folder}' does not exist or cannot access it.");
            filenameSanitizer.Logger.Errors.Add($"The specified folder '{folder}' does not exist or cannot access it.");
            Console.ResetColor();
            return null;
        }

        return folder;
    }

    /// <summary>
    /// Performs the sanitization action depending on whether patterns were provided or not.
    /// </summary>
    /// <param name="folder">The target folder to operate on (used only for informational logging).</param>
    /// <param name="semiCommaSeparatedPatterns">Optional semicolon-separated patterns to remove from filenames.</param>
    /// <param name="filenameSanitizer">The filename sanitizer instance used to perform actions and record logs.</param>
    private static void ProcessSanitization(string folder, string semiCommaSeparatedPatterns, FilenameNormalizer filenameSanitizer)
    {
        if (!string.IsNullOrEmpty(semiCommaSeparatedPatterns))
        {
            Console.WriteLine("Using the patterns provided in the command line parameter to remove:");
            Console.WriteLine(semiCommaSeparatedPatterns);
            filenameSanitizer.Logger.Info.Add("Using the patterns provided in the command line parameter to remove:");
            filenameSanitizer.Logger.Info.Add(semiCommaSeparatedPatterns);

            filenameSanitizer.RenameFilesRemovingPatterns(ToNewLineSeparatedPatterns(semiCommaSeparatedPatterns));
        }
        else
        {
            Console.WriteLine($"Sanitizing filenames in: {folder}");
            filenameSanitizer.Logger.Info.Add($"Sanitizing filenames in: {folder}");
            filenameSanitizer.RenameFilesToMeetOsRequirements();
        }
    }

    /// <summary>
    ///     Converts the semicolon-separated patterns taken from the command line into the
    ///     new-line separated list expected by <see cref="IFilenameSanitizer.RenameFilesRemovingPatterns"/>.
    /// </summary>
    /// <param name="semiCommaSeparatedPatterns">Semicolon-separated patterns, as typed on the command line.</param>
    /// <returns>The patterns separated by <see cref="Environment.NewLine"/>.</returns>
    private static string ToNewLineSeparatedPatterns(string semiCommaSeparatedPatterns)
    {
        var patterns = semiCommaSeparatedPatterns
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(Environment.NewLine, patterns);
    }

    /// <summary>
    /// Prints completion messages, shows log file path and waits for a key press.
    /// </summary>
    /// <param name="logger">The <see cref="OperationLogger"/> that contains the log file path and entries.</param>
    private static void PrintCompletion(OperationLogger logger)
    {
        Console.WriteLine("Operation completed.");
        Console.WriteLine($"Log file written to: {logger.LogFilePath}");

        // Console.ReadKey throws InvalidOperationException when stdin is redirected
        // (scripts, CI, piped input), which turned every non-interactive run into an
        // unhandled crash after the work had already been done.
        if (Console.IsInputRedirected)
        {
            return;
        }

        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    ///  Parses the command-line arguments to extract the target folder and optional patterns for filename sanitization   
    ///  The method handles different argument formats, allowing for a single folder argument or a folder followed by patterns separated by semicolons.
    /// </summary> <param name="args">The array of command-line arguments</param>
    /// <returns>A tuple containing the folder path and a semicolon-separated string of patterns</returns
    private static (string folder, string semicommaSeparatedPatterns) ParseArguments(string[] args)
    {
        if (args.Length == 0)
        {
            return (string.Empty, string.Empty);
        }

        var separator = args.Length > 1 ? ";" : " ";
        var folder = args[0];
        var semiCommaSeparatedPatterns = args.Length > 1 ? string.Join(separator, args[1..]) : string.Empty;

        return (folder, semiCommaSeparatedPatterns);
    }

    /// <summary>
    ///    Outputs the collected log entries to the console, with color coding for errors and warnings.
    /// </summary> <param name="logger">The OperationLogger instance containing the log entries to output</param>
    /// <remarks>
    ///    Errors are displayed in red, warnings in yellow, and informational messages in the default console
    ///   color. The method checks for the presence of each type of log entry before outputting, and resets the console color after displaying errors or warnings.
    /// </remarks>
    private static void ConsoleOutputLogs(OperationLogger logger)
    {
        if (logger.HasErrors)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            foreach (var error in logger.Errors)
            {
                Console.WriteLine($"Error: {error}");
            }

            Console.ResetColor();
        }

        if (logger.HasWarnings)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var warning in logger.Warnings)
            {
                Console.WriteLine($"Warning: {warning}");
            }

            Console.ResetColor();
        }

        if (!logger.HasInfo)
        {
            return;
        }

        foreach (var info in logger.Info)
        {
            Console.WriteLine($"Info: {info}");
        }
    }
}