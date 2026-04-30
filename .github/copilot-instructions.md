# Copilot Instructions

## Build, test, and lint commands

- Use the SDK pinned in `global.json`.
- Restore: `dotnet restore ImageConvertr.slnx`
- Build: `dotnet build ImageConvertr.slnx --configuration Release`
- Run the desktop app: `dotnet run --project src\ImageConvertr.App\ImageConvertr.App.csproj --configuration Release`
- Run all tests: `dotnet test ImageConvertr.slnx --configuration Release`
- Run a single test: `dotnet test tests\ImageConvertr.Tests\ImageConvertr.Tests.csproj --configuration Release --filter "FullyQualifiedName=ImageConvertr.Tests.ImageConvertrServiceTests.ConvertFolderConvertsSupportedImagesAndSkipsUnsupportedFiles"`
- There is no separate lint command. Repo-wide analyzers and code style checks are enforced during `dotnet build` through `Directory.Build.props`.

## High-level architecture

- The solution is split into three projects:
  - `src\ImageConvertr.App`: Avalonia desktop UI shell.
  - `src\ImageConvertr.Core`: image conversion workflow, models, and file/image processing.
  - `tests\ImageConvertr.Tests`: xUnit tests for both the core service and the view model.
- `ImageConvertr.App` is intentionally thin:
  - `App.axaml.cs` builds the DI container and registers `IImageConvertrService`, `MainWindowViewModel`, and `MainWindow` as singletons.
  - `MainWindow.axaml` binds directly to `MainWindowViewModel` with compiled bindings (`x:DataType`).
  - `MainWindow.axaml.cs` only handles Avalonia-specific folder picking and passes those operations into the view model through delegates.
- `ImageConvertr.Core` owns the conversion pipeline:
  - `ImageConvertResult`, `ImageConversionProgressUpdate`, `ImageOutputFormatOption`, and `LogLevel` define the batch contract.
  - `ImageConvertrService` enumerates supported image files, skips unsupported files, decodes with SkiaSharp, encodes the selected output format, applies overwrite rules, and reports progress.
  - Progress messages are produced in the core service as user-facing text plus a `LogLevel`, then surfaced by the view model in the right-side progress panel.
- Tests mirror that separation:
  - `ImageConvertrServiceTests` uses real temp folders and real SkiaSharp images to verify file-system behavior, supported formats, overwrite rules, unsupported files, progress reporting, and failure handling.
  - `MainWindowViewModelTests` uses a fake `IImageConvertrService` to verify UI-state behavior such as folder selection, conversion state, progress throttling, and completion summaries.

## Key conventions

- Keep Avalonia-specific work in the app project and conversion/business logic in `ImageConvertr.Core`. If a change does not require Avalonia APIs, it usually belongs in the core service or models rather than the window code-behind.
- The view model depends on `IImageConvertrService` and exposes bindable state through CommunityToolkit.Mvvm source generators (`[ObservableProperty]`, `[RelayCommand]`). Follow that pattern instead of hand-written `INotifyPropertyChanged`.
- The window code-behind should stay minimal. In this repo it only initializes the view and wires folder-picker delegates; it does not contain conversion workflow logic.
- User-visible progress/status text originates in the core service via `ImageConversionProgressUpdate`. When changing conversion behavior, keep progress reporting aligned with the actual workflow so the view model and tests continue to reflect accurate messages and severity levels.
- Expected operational failures are surfaced explicitly. `ImageConvertrService` reports decode/encode failures through progress updates, and `MainWindowViewModel` maps thrown exceptions into `LatestActivityMessage` and `LatestActivityLevel`.
- Repo-wide build rules are strict: `Directory.Build.props` enables latest analyzers, enforces code style in build, generates XML docs, and treats warnings as errors for every project.
- Match the existing C# style from `.editorconfig`: file-scoped namespaces, explicit types instead of `var`, braces required, and collection expressions/record types for small immutable model objects.
- Package versions are centralized in `Directory.Packages.props`; add or update package versions there instead of hardcoding versions in individual project files.
