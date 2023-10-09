using Mono.Options;
using SharpCompress.Archives;
using Xbox360Toolkit;
using Xbox360Toolkit.Interface;

internal class Program
{
    private static bool shouldShowHelp = false;
    private static bool delete = false;
    private static string input = string.Empty;
    private static string output = string.Empty;
    private static string format = "CCI";

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
        var inputFiles = Directory.GetFiles(input).Where(s => s.EndsWith(".zip", StringComparison.CurrentCultureIgnoreCase) || s.EndsWith(".iso", StringComparison.CurrentCultureIgnoreCase) || s.EndsWith(".cci", StringComparison.CurrentCultureIgnoreCase)).ToArray();
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

                var extension = format == "CCI" ? ".cci" : ".iso";
                var containerPath = Path.Combine(tempFolder, Path.GetFileNameWithoutExtension(processFile) + extension);
                var processExtension = Path.GetExtension(processFile);

                using (ContainerReader isoContainer = (processExtension.Equals(".iso", StringComparison.CurrentCultureIgnoreCase) ? new ISOContainerReader(processFile) : new CCIContainerReader(processFile)))
                {
                    LogLine("Converting...");
                    if (isoContainer.TryMount() == false)
                    {
                        isoContainer.Dispose();
                        LogLine("Error: Failed to mount.");
                        continue;
                    }
                    if (string.Equals(extension, ".cci") && ContainerUtility.ConvertContainerToCCI(isoContainer, ProcessingOptions.All, containerPath, null) == false)
                    {
                        LogLine("Error: Failed creating cci.");
                        continue;
                    }
                    if (string.Equals(extension, ".iso") && ContainerUtility.ConvertContainerToISO(isoContainer, ProcessingOptions.All, containerPath, null) == false)
                    {
                        LogLine("Error: Failed creating iso.");
                        continue;
                    }
                }

                var destPath = Path.Combine(output, Path.GetFileName(containerPath));
                if (destPath == null)
                {
                    LogLine("Error: Unexpected path is null.");
                    continue;
                }

                File.Move(containerPath, destPath, true);

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
            { "i|input=", "the source path of ISO/CCI/ZIP's.", i => input = i },
            { "o|output=", "the destination path of ISO/CCI's.", i => output = i },
            { "f|format=", "the destination format (ISO/CCI) default CCI.", f => format = f },
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

            if ((format.Equals("ISO", StringComparison.CurrentCultureIgnoreCase) || format.Equals("CCI", StringComparison.CurrentCultureIgnoreCase)) == false)
            {
                throw new OptionException("format should be ISO or CCI.", "format");
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