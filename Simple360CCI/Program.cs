using Mono.Options;
using SharpCompress.Archives;
using Xbox360Toolkit;

internal class Program
{
    private static bool shouldShowHelp = false;
    private static bool delete = false;
    private static string input = string.Empty;
    private static string output = string.Empty;

    public static void LogLine(string line)
    {
        Console.WriteLine(line);
    }

    public static string? Unpack(string inputFile)
    {
        LogLine("Extracting...");

        var tempFolder = Path.Combine(output, "Temp");
        using (var archiveStream = File.OpenRead(inputFile))
        using (var archive = ArchiveFactory.Open(archiveStream))
        {
            foreach (var entry in archive.Entries)
            {
                if (!Path.GetExtension(entry.Key).Equals(".iso", StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }
                entry.WriteToDirectory(tempFolder);
                return Path.Combine(tempFolder, entry.Key);
            }
        }

        return null;
    }

    public static void Process()
    {
        var inputFiles = Directory.GetFiles(input).Where(s => s.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) || s.EndsWith(".iso", StringComparison.CurrentCultureIgnoreCase)).ToArray();
        var tempFolder = Path.Combine(output, "Temp");

        for (int i = 0; i < inputFiles.Length; i++)
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
            Directory.CreateDirectory(tempFolder);

            try
            {

                var originalFile = inputFiles[i];
                var processFile = originalFile;

                LogLine($"Processing {Path.GetFileNameWithoutExtension(processFile)} - {i + 1} of {inputFiles.Length}");

                if (Path.GetExtension(processFile).Equals(".zip", StringComparison.CurrentCultureIgnoreCase))
                {
                    processFile = Unpack(processFile);
                    if (processFile == null)
                    {
                        LogLine("Error: Failed extracting archive.");
                        continue;
                    }
                }

                var cciPath = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(processFile) + ".cci");

                using (var isoContainer = new ISOContainerReader(processFile))
                {
                    LogLine("Converting...");
                    if (isoContainer.TryMount() == false)
                    {
                        isoContainer.Dispose();
                        LogLine("Error: Failed to mount.");
                        continue;
                    }
                    if (ContainerUtility.ConvertContainerToCCI(isoContainer, ProcessingOptions.All, cciPath, null) == false)
                    {
                        LogLine("Error: Failed creating cci.");
                        continue;
                    }
                }

                var destPath = Path.Combine(output, Path.GetFileName(cciPath));
                if (destPath == null)
                {
                    LogLine("Error: Unexpected path is null.");
                    continue;
                }

                File.Move(cciPath, destPath, true);

                if (delete == true)
                {
                    LogLine($"Deleting '{Path.GetFileName(originalFile)}'.");
                    File.Delete(originalFile);
                }

            }
            catch (Exception ex)
            {
                LogLine($"Error: {ex}");
            }
        }

        if (Directory.Exists(tempFolder))
        {
            Directory.Delete(tempFolder, true);
        }
        LogLine("Done!");
    }

    private static void Main(string[] args)
    {
        var options = new OptionSet {
            { "i|input=", "the source path of ISO/ZIP's.", i => input = i },
            { "o|output=", "the destination path of CCI's.", i => output = i },
            { "d|delete", "delete original file after successful processing.", v => delete = v != null },
            { "h|help", "show this message and exit", h => shouldShowHelp = h != null },
        };

        try
        {
            List<string> extra = options.Parse(args);

            if (shouldShowHelp || args.Length == 0)
            {
                Console.WriteLine("Simple360CCI: ");
                options.WriteOptionDescriptions(System.Console.Out);
                return;
            }

            if (string.IsNullOrEmpty(input) == true)
            {
                throw new OptionException("input path is invalid", "input");
            }

            input = Path.GetFullPath(input);
            if (Directory.Exists(input) == false)
            {
                throw new OptionException("input path does not exist.", "input");
            }

            if (string.IsNullOrEmpty(output) == true)
            {
                throw new OptionException("output path is invalid", "output");
            }

            output = Path.GetFullPath(output);
            if (Directory.Exists(output) == false)
            {
                throw new OptionException("output path does not exist.", "output");
            }

            if (input.Equals(output, StringComparison.CurrentCultureIgnoreCase))
            {
                throw new OptionException("output path should not be same as input.", "output");
            }

            Process();
        }
        catch (OptionException e)
        {
            Console.Write("Simple360CCI: ");
            Console.WriteLine(e.Message);
            Console.WriteLine("Try `Simple360CCI --help' for more information.");
            return;
        }
    }
}