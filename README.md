# Image Convertr

Image Convertr is a cross-platform desktop app for converting entire folders of images without turning the task into a script. Built with Avalonia on .NET, it focuses on a practical batch workflow: choose a source folder, pick a target folder, select the output format, adjust quality, and follow every result in a live conversion history.

![Illustrated header for ImageConvertr](docs/header.jpg)

The project is aimed at everyday image-conversion jobs such as preparing web-ready assets, creating JPEG or PNG copies, converting batches to WebP, or reducing file sizes before sharing image folders.

## Highlights

- Batch convert supported images from a desktop UI
- Convert output files to `PNG`, `JPEG`, or `WebP`
- Adjust image quality for JPEG and WebP output; PNG remains lossless
- Skip or overwrite existing files in the output folder
- Track file-based progress with processed and total counts, a progress bar, and a percentage
- Review a compact, automatically scrolling entry for every discovered file
- Distinguish converted, overwritten, skipped, and failed files without relying on color alone
- Compare source and target filenames, formats, and readily available file sizes
- Finish every batch with a summary entry in the conversion history
- Use the app in English or German, selected automatically from the operating system UI language
- Follow the operating system light or dark theme
- Read `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, and `.webp` input files

> Note: the current conversion workflow processes files from the selected input folder only. It does not recurse into nested subfolders.

## Screenshots

### Ready to convert

![ImageConvertr batch setup screen](docs/screenshot-01.png)

The two-card layout keeps configuration and feedback together. The left card contains the complete batch setup, while the right card starts with a compact `0 / 0` progress state and an empty conversion history. The primary action remains disabled until both folders are selected.

### Live progress during a running batch

![ImageConvertr live progress while converting images](docs/screenshot-02.png)

While a batch is running, the settings are locked and the right-hand card reports processed files, total files, percentage, and overall progress. Each completed file is added to the scrollable history with its status, target filename, format change, and file-size comparison. The newest entry is kept visible automatically.

### Completion summary

![ImageConvertr completion summary after converting images](docs/screenshot-03.png)

After the batch finishes, the final history entry summarizes converted, overwritten, skipped, and failed files together with the space saved. Settings become available again without opening a separate report view.

## Conversion Workflow

1. Select the folder that contains the source images.
2. Choose where the converted copies should be written.
3. Pick the output format: `PNG`, `JPEG`, or `WebP`.
4. Adjust image quality when converting to JPEG or WebP. PNG output is lossless.
5. Decide whether to skip or overwrite existing output files.
6. Start the batch and follow the file-based progress and conversion history.
7. Review the automatically appended summary when the operation finishes.

Starting another batch clears the previous progress and history. Files with unsupported extensions are not converted and appear as skipped entries. Existing target files are skipped unless overwrite is enabled.

## Supported Formats

Image Convertr currently accepts the following input file types:

- `.jpg`
- `.jpeg`
- `.png`
- `.gif`
- `.bmp`
- `.webp`

Converted files can be written as:

- `PNG`
- `JPEG`
- `WebP`

Output files keep the source base name and receive the extension of the selected target format.

## Conversion History

The history is designed to remain readable for batches containing hundreds or thousands of files. Entries use compact rows and communicate their outcome with both a symbol and localized text:

- **Converted**: a new target file was written
- **Overwritten**: an existing target file was replaced
- **Skipped**: the file was unsupported, source and target were identical, or the target already existed
- **Failed**: the image could not be decoded, encoded, read, or written

Successful rows show the source and target filenames, the format transition, and source/target sizes when those values are already available during conversion. The history scrolls internally, so large batches do not increase the application window size.

## Languages and Themes

Image Convertr includes complete English and German resources. The language follows the operating system UI culture:

- German regional cultures such as `de-DE`, `de-AT`, and `de-CH` use German
- English regional cultures use English
- Other currently unsupported cultures fall back to English

The application also follows the operating system light or dark theme. Filenames, file extensions, and codec identifiers remain unchanged regardless of the selected UI language.

## Getting Started

### Prerequisites

- .NET SDK `10.0.400`, as pinned in `global.json`

### Restore

```bash
dotnet restore ImageConvertr.slnx
```

### Build

```bash
dotnet build ImageConvertr.slnx --configuration Release
```

### Run the desktop app

```bash
dotnet run --project src/ImageConvertr.App/ImageConvertr.App.csproj --configuration Release
```

### Run the tests

```bash
dotnet test ImageConvertr.slnx --configuration Release
```

There is no separate lint command. Repository analyzers and code-style checks run as part of the build.

## Project Structure

- `src/ImageConvertr.App`: Avalonia desktop UI shell
- `src/ImageConvertr.Core`: conversion pipeline, models, and image processing
- `tests/ImageConvertr.Tests`: xUnit tests for conversion behavior, progress history, summaries, and localization

The solution intentionally keeps the UI thin. Avalonia-specific code lives in the app project, while the conversion workflow, format handling, file handling, and progress reporting live in the core library.

## Tech Stack

- [.NET](https://dotnet.microsoft.com/)
- [Avalonia UI](https://avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [SkiaSharp](https://github.com/mono/SkiaSharp)
- [xUnit](https://xunit.net/)

## Quality

The repository includes automated coverage for the batch conversion service, output format metadata, progress history, completion summaries, and localization. Tests verify conversion behavior, file handling, overwrite rules, unsupported files, GIF input handling, progress calculation, history reset, English and German resources, culture fallback, and failure handling.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
