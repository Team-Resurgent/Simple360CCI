# Simple360CCI
CLI tool from [Team-Resurgent/Simple360CCI](https://github.com/Team-Resurgent/Simple360CCI) that converts between **ISO** and **CCI** (and can unpack **ZIP** inputs) using **`Xbox360Toolkit`** (`ISOContainerReader`, `CCIContainerReader`, `ContainerUtility`).
Source: [`Simple360CCI/Program.cs`](https://github.com/Team-Resurgent/Simple360CCI/blob/main/Simple360CCI/Program.cs).
## What it does
- Scans an **input directory** for files ending in `.zip`, `.iso`, or `.cci`.
- For each file:
  - **ZIP**: extracts the first **`.iso`** entry from the archive into a temp folder.
  - **ISO / CCI**: opens via `ISOContainerReader` or `CCIContainerReader`, mounts with `TryMount()`, then converts with:
    - `ContainerUtility.ConvertContainerToCCI(...)` when output format is **CCI**, or  
    - `ContainerUtility.ConvertContainerToISO(...)` when output format is **ISO**.
- Writes the result to the **output directory** (optionally deletes the source file after success).
## Requirements
- **Input** and **output** must be **existing directories** and must **not** be the same path.
- Uses **SharpCompress** for ZIP handling and **Xbox360Toolkit** for container read/write.
## Command-line options
| Option | Description |
|--------|-------------|
| `-i`, `--input=` | Source **folder** containing `.zip` / `.iso` / `.cci` files |
| `-o`, `--output=` | Destination **folder** for converted files |
| `-f`, `--format=` | Output format: **`ISO`** or **`CCI`** (default: **`CCI`**) |
| `-d`, `--delete` | Delete the original file after a successful conversion |
| `-h`, `--help` | Show help |
## Usage examples
```bash
# Convert all supported files in .\in to CCI in .\out (default format)
Simple360CCI -i C:\path\to\input -o C:\path\to\output
# Same, explicit format
Simple360CCI -i C:\path\to\input -o C:\path\to\output -f CCI
# Produce ISO instead
Simple360CCI -i C:\path\to\input -o C:\path\to\output -f ISO
# Delete originals after success
Simple360CCI -i C:\path\to\input -o C:\path\to\output -d
```
