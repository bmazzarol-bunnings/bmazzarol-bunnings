# CAF Sweep Map — dotnet.common

Issue: [#1761](https://github.com/Bunnings-Technology-Delivery/dotnet.common/issues/1761)

This document maps every location in `dotnet.common` (scanned from `origin/main`) that references
`Spectre.Console.Cli` — either via a `PackageReference` in a `.csproj`/`.props` file, or via
`using Spectre.Console.Cli` in a `.cs` file. It is the input for deciding which tools are
**keeper candidates** for the CAF (Command Abstraction Framework) migration.

## Package Reference Locations

| File | Kind | Notes |
|------|------|-------|
| `src/dotnet/Directory.Packages.props` | Central package version | Pins `Spectre.Console.Cli` 1.0.0-alpha.0.16, `Spectre.Console` 0.57.2, `Spectre.Console.Cli.Testing` 1.0.0-alpha.0.16, `Spectre.Console.Testing` 0.57.2 |
| `src/dotnet/Common.Tool.props` | Shared MSBuild props | Adds `PackageReference Spectre.Console.Cli` to every project that imports it (9 tool projects + 1 test project: ApigeeGenerator, Bagc, ChangeLogTool, CopilotArtifactManagementTool, EmpXTool, JsonSchemaTool, RoslynIndexerTool, TemplateEnhancementTool, TestReportingTool, and CopilotArtifactManagementTool.Tests) |
| `src/dotnet/Components/Spectre/Bunnings.Common.Spectre/Bunnings.Common.Spectre.csproj` | Component library | Provides `ServiceCollectionTypeRegistrar`, `ServiceProviderTypeResolver`, `ConfiguratorExtensions` — the DI bridge for Spectre CLI |
| `src/dotnet/Usecases/MonoRepoMigration/Bunnings.UseCase.MonoRepoMigrationTool/Bunnings.UseCase.MonoRepoMigrationTool.csproj` | Library (packable) | Direct `PackageReference Spectre.Console.Cli` (does not use Common.Tool.props) |

All 12 test projects reference `Spectre.Console.Cli` (and typically `Spectre.Console.Cli.Testing`)
for `CommandAppTester`-based unit tests. They are not migration targets themselves but will need
their test harness updated alongside the tools they cover.

## Summary Table

| # | Location | Tool / Project | Commands | Settings POCOs | Raw Spectre Rendering | Complexity | Keeper? |
|---|----------|---------------|----------|---------------|----------------------|------------|---------|
| 1 | `Tools/ApigeeGenerator` | `apigee-generator` | 2 | Yes (2 files) | Moderate (5 files) | Low | ✅ Yes |
| 2 | `Tools/Bagc` | `bagc` | 10 | Inline (per-command) | None | Low | ✅ Yes |
| 3 | `Tools/ChangeLogTool` | `change-log` | 5 | Yes (5 files) | Moderate (9 files) | Low | ✅ Yes |
| 4 | `Tools/CopilotArtifactManagementTool` | `copilot-am` | ~30 | Yes (7 files) | Heavy (32 files + Terminal.Gui TUI) | High | ✅ Yes (flagship) |
| 5 | `Tools/EmpExperienceTool` | `empx` | 12 | Yes (11 files) | Heavy (33 files, LiveDisplay) | High | ✅ Yes |
| 6 | `Tools/JsonSchemaTool` | `json-schema-tool` | 1 | Yes (1 file) | Minimal (1 file) | Low | ✅ Yes |
| 7 | `Tools/RoslynIndexerTool` | `roslyn-indexer` | 1 | Inline (nested) | Minimal (1 file) | Low | ✅ Yes |
| 8 | `Tools/TemplateEnhacementTool` | `template-enhancement` | 2 | Yes (2 files) | Moderate (6 files) | Low | ✅ Yes |
| 9 | `Usecases/AspNetCore/Tools/ApigeeGatewayExporterTool` | internal tool | 1 | Yes (1 file) | Light (2 files) | Low | ✅ Yes |
| 10 | `Usecases/AspNetCore/Tools/ProductExporterTool` | internal tool | 2 | Yes (2 files) | Light (3 files) | Low | ✅ Yes |
| 11 | `Usecases/AspNetCore/Tools/ScopeExporterTool` | internal tool | 2 | Yes (2 files) | Light (4 files) | Low | ✅ Yes |
| 12 | `Usecases/MonoRepoMigration` | `mono-repo-migration` | 1 | Yes (1 file) | Light (1 file) | Low | ✅ Yes |
| 13 | `Components/Spectre` | `Bunnings.Common.Spectre` | 0 (infra) | N/A | None | Med | ⚠️ Replace |
| 14 | `Usecases/Bunnings.UseCase.Registration` | `InternalToolHost` infra | 0 (infra) | N/A | None | Med | ⚠️ Replace |
| 15 | `Tools/TestReportingTool` | `test-reporting` | 0 | N/A | None | — | ❌ Non-target (inherits ref via Common.Tool.props but uses no Spectre.Console.Cli types) |

---

## Per-Location Detail

### 1. ApigeeGenerator (`apigee-generator`)

- **Path:** `src/dotnet/Tools/ApigeeGenerator/Bunnings.Common.Tools.ApigeeGenerator/`
- **Commands (2):** `proxy`, `products`
- **Settings POCOs:** `ProductsCommand.Settings.cs`, `ProxyCommand.Settings.cs`
- **Raw Spectre.Console usage:** 5 files — `Program.cs` (DI setup), both command files and their settings. Uses `AnsiConsole` for output formatting.
- **DI pattern:** Uses `ServiceCollectionTypeRegistrar` + `Bunnings.Common.Spectre` extensions.
- **Migration complexity:** **Low** — 2 commands, straightforward settings, moderate rendering.
- **Keeper rationale:** Small, self-contained tool. Good first candidate for CAF migration to validate the pattern.

### 2. Bagc (`bagc`)

- **Path:** `src/dotnet/Tools/Bagc/Bunnings.Common.Tools.Bagc/`
- **Commands (10):** `profile create|list|use|show|remove`, `auth login|status|logout`, `api call|describe|schema`
- **Settings POCOs:** Inline `Settings` nested classes within each command file (no separate `.Settings.cs` files).
- **Raw Spectre.Console usage:** **None** — no `using Spectre.Console;` in the tool project. Pure CLI argument parsing.
- **DI pattern:** Uses `ServiceCollectionTypeRegistrar` with full DI graph (HttpClient, SecretStore, ProfileStore).
- **Migration complexity:** **Low** — no rendering to migrate, but has the richest DI graph (secret store, HTTP client, profile store) which CAF must accommodate.
- **Keeper rationale:** Validates CAF's DI/service-provider integration. No TUI coupling makes it a clean migration target.

### 3. ChangeLogTool (`change-log`)

- **Path:** `src/dotnet/Tools/ChangeLogTool/Bunnings.Common.Tools.ChangeLogTool/`
- **Commands (5):** `bump`, `create`, `list`, `release`, `docs`
- **Settings POCOs:** 5 separate `.Settings.cs` files (one per command).
- **Raw Spectre.Console usage:** 9 files — commands use `Table`, `MarkupLine` for formatted output.
- **DI pattern:** Bare `CommandApp()` (no DI registrar).
- **Migration complexity:** **Low** — no DI to migrate, standard settings pattern, moderate rendering.
- **Keeper rationale:** Simplest non-trivial tool. Good for validating CAF's rendering abstraction (tables, markup).

### 4. CopilotArtifactManagementTool (`copilot-am`)

- **Path:** `src/dotnet/Tools/CopilotArtifactManagementTool/Bunnings.Common.Tools.CopilotArtifactManagementTool/`
- **Commands (~30):** Branches: `skills` (list/install/remove/update/init + sources list/add/remove), `agents` (list/install/remove/update + sources), `apps` (list/install/remove/update/run + sources, net10 only), `profiles` (list/show/install), plus `tui` (net10 only).
- **Settings POCOs:** 7 files — `RepositorySettings`, `ArtefactRepositorySettings`, `InstallArtefactRepositorySettings`, `SourceSettings`, `BaseListCommand.Settings.cs`, `ProfileRepositorySettings`, `InstallProfileSettings`.
- **Raw Spectre.Console usage:** **Heavy** — 32 files use `Spectre.Console` for tables, panels, progress. Additionally, 11 files use **Terminal.Gui** for a full interactive TUI (`TuiCommand`, `TuiForm`, `TuiSession`, `TuiViews`, `SourcesDialog`, etc.).
- **DI pattern:** Bare `CommandApp()` with `PropagateExceptions`.
- **Migration complexity:** **High** — largest command surface, TUI (Terminal.Gui) is a significant architectural dependency, heavy rendering, multi-target (net8/net10 conditional compilation).
- **Keeper rationale:** Flagship tool. The TUI is a differentiator that CAF must either support or the TUI must be extracted. Highest-risk migration but highest-value proof.

### 5. EmpExperienceTool (`empx`)

- **Path:** `src/dotnet/Tools/EmpExperienceTool/Bunnings.Common.Tools.EmpXTool/`
- **Commands (12):** `receive`, `publish`, `ping`, `publish-files`, `seed create|start|resume|handle`, `move`, `samples`, `config` (alias `iddqd`), `cache` (hidden).
- **Settings POCOs:** 11 files — `BaseSettings.cs` + one `.Settings.cs` per command.
- **Raw Spectre.Console usage:** **Heavy** — 33 files. Includes `LiveDisplay` (custom live table for receive streaming), `ProgressBar`, extensive `Table`/`MarkupLine` usage. `SamplesCommand` alone has 86 rendering references.
- **DI pattern:** Bare `CommandApp()`.
- **Migration complexity:** **High** — `LiveDisplay` is a custom interactive rendering pattern (streaming message counters), heavy table/markup usage across all commands, debugger-mode interactive prompt in `Program.cs`.
- **Keeper rationale:** Most rendering-intensive tool. Validates CAF's ability to handle live/interactive display patterns. The `LiveDisplay` component is a key architectural piece.

### 6. JsonSchemaTool (`json-schema-tool`)

- **Path:** `src/dotnet/Tools/JsonSchemaTool/Bunnings.Common.Tools.JsonSchemaTool/`
- **Commands (1):** `generate-models`
- **Settings POCOs:** 1 file — `GenerateModelsCommand.Settings.cs`.
- **Raw Spectre.Console usage:** Minimal — 1 file (likely just `AnsiConsole` for output).
- **DI pattern:** Bare `CommandApp()`.
- **Migration complexity:** **Low** — single command, minimal rendering.
- **Keeper rationale:** Trivial migration. Useful as a smoke-test for the CAF pipeline.

### 7. RoslynIndexerTool (`roslyn-indexer`)

- **Path:** `src/dotnet/Tools/RoslynIndexerTool/Bunnings.Common.Tools.RoslynIndexerTool/`
- **Commands (1):** `index`
- **Settings POCOs:** Inline nested `Settings` class within `IndexCommand.cs`.
- **Raw Spectre.Console usage:** Minimal — 1 file. Uses `IProgress<T>` for progress reporting.
- **DI pattern:** Bare `CommandApp()`.
- **Migration complexity:** **Low** — single command, simple settings, progress bar only.
- **Keeper rationale:** Validates CAF's progress-reporting abstraction.

### 8. TemplateEnhancementTool (`template-enhancement`)

- **Path:** `src/dotnet/Tools/TemplateEnhacementTool/Bunnings.Common.Tools.TemplateEnhancementTool/`
- **Commands (2):** `add-symbols`, `add-manifest`
- **Settings POCOs:** 2 files — `AddManifest.Settings.cs`, `SetCommonSymbols.Settings.cs`.
- **Raw Spectre.Console usage:** Moderate — 6 files.
- **DI pattern:** Uses `ServiceCollectionTypeRegistrar` + `Bunnings.Common.Spectre` (same as ApigeeGenerator). Injects `IFileSystem`, `JsonSerializerOptions`.
- **Migration complexity:** **Low** — 2 commands, DI pattern shared with ApigeeGenerator.
- **Keeper rationale:** Pairs with ApigeeGenerator as a DI-based tool. Migrating both together validates the DI path.

### 9. ApigeeGatewayExporterTool (internal)

- **Path:** `src/dotnet/Usecases/AspNetCore/Tools/ApigeeGatewayExporterTool/Bunnings.UseCase.AspNetCore.ApigeeGatewayExporterTool/`
- **Commands (1):** `export`
- **Settings POCOs:** 1 file — `ExportGatewayConfigurationCommand.Settings.cs`.
- **Raw Spectre.Console usage:** Light — 2 files.
- **DI pattern:** Uses `InternalToolHost` / `InternalTool<T>` registration (AspNetCore host-based).
- **Migration complexity:** **Low** — single command, hosted in an ASP.NET Core app.
- **Keeper rationale:** Validates CAF's hosted/internal-tool pattern (not a standalone CLI).

### 10. ProductExporterTool (internal)

- **Path:** `src/dotnet/Usecases/AspNetCore/Tools/ProductExporterTool/Bunnings.UseCase.AspNetCore.ProductExporterTool/`
- **Commands (2):** `export`, `list`
- **Settings POCOs:** 2 files — `ExportProductsCommand.Settings.cs`, `ListProductsCommand.Settings.cs`.
- **Raw Spectre.Console usage:** Light — 3 files.
- **DI pattern:** `InternalToolHost` / `InternalTool<T>`.
- **Migration complexity:** **Low** — 2 commands, hosted.
- **Keeper rationale:** Same hosted pattern as #9.

### 11. ScopeExporterTool (internal)

- **Path:** `src/dotnet/Usecases/AspNetCore/Tools/ScopeExporterTool/Bunnings.UseCase.AspNetCore.ScopeExporterTool/`
- **Commands (2):** `export`, `list`
- **Settings POCOs:** 2 files — `ExportScopesCommand.Settings.cs`, `ListScopesCommand.Settings.cs`.
- **Raw Spectre.Console usage:** Light — 4 files. Includes a `BaseScopesCommand` shared base.
- **DI pattern:** `InternalToolHost` / `InternalTool<T>`.
- **Migration complexity:** **Low** — 2 commands, shared base class pattern.
- **Keeper rationale:** Validates CAF's support for shared command base classes.

### 12. MonoRepoMigration (`mono-repo-migration`)

- **Path:** `src/dotnet/Usecases/MonoRepoMigration/Bunnings.UseCase.MonoRepoMigrationTool/`
- **Commands (1):** `list`
- **Settings POCOs:** 1 file — `ListCommand.Settings.cs`.
- **Raw Spectre.Console usage:** Light — 1 file.
- **DI pattern:** Standalone library exposing `AddMonoRepoMigrationTool(this IConfigurator, params Assembly[])` — designed to be plugged into a custom dotnet CLI host.
- **Migration complexity:** **Low** — single command, but the plugin-style registration API is a unique pattern.
- **Keeper rationale:** Validates CAF's extensibility/plugin model (tool as a library that configures a host's `IConfigurator`).

### 13. Bunnings.Common.Spectre (component)

- **Path:** `src/dotnet/Components/Spectre/Bunnings.Common.Spectre/`
- **Role:** Infrastructure — provides `ServiceCollectionTypeRegistrar` (ITypeRegistrar), `ServiceProviderTypeResolver` (ITypeResolver), and `ConfiguratorExtensions.SetApplicationVersionFromAssembly()`.
- **Raw Spectre.Console usage:** None (only `Spectre.Console.Cli` interfaces).
- **Migration complexity:** **Medium** — must be replaced by CAF's equivalent DI bridge.
- **Keeper rationale:** **Replace** — this is the Spectre-specific glue. CAF should provide its own `ITypeRegistrar`/`ITypeResolver` or eliminate the need.

### 14. Bunnings.UseCase.Registration (InternalToolHost)

- **Path:** `src/dotnet/Usecases/Bunnings.UseCase.Registration/`
- **Role:** Infrastructure — `InternalToolHost` wraps a `CommandApp` inside an ASP.NET Core `IHost`. `InternalTool<T>` is the abstract base for hosted tools. `IInternalTool` defines the contract.
- **Raw Spectre.Console usage:** None (only `Spectre.Console.Cli` types: `CommandApp`, `ITypeRegistrar`, `ITypeResolver`).
- **Migration complexity:** **Medium** — must be adapted to CAF's host model.
- **Keeper rationale:** **Replace** — the `CommandApp` instantiation and `ITypeRegistrar`/`ITypeResolver` wiring is Spectre-specific. CAF needs an equivalent hosted-tool mechanism.

---

## Migration Order Recommendation

| Phase | Tools | Rationale |
|-------|-------|-----------|
| 1 (smoke) | JsonSchemaTool, RoslynIndexerTool | Single command, minimal rendering. Fastest win. |
| 2 (simple) | ChangeLogTool, ApigeeGenerator, TemplateEnhancementTool | 2–5 commands, standard patterns, validates DI + rendering. |
| 3 (hosted) | ApigeeGatewayExporter, ProductExporter, ScopeExporter, MonoRepoMigration | Validates hosted/plugin patterns. |
| 4 (complex) | Bagc, EmpX | Rich DI / heavy rendering. |
| 5 (flagship) | CopilotArtifactManagementTool | TUI, largest surface. Last. |
| Infra | Common.Spectre, UseCase.Registration | Replace alongside phase 2–3. |

## Test Projects Affected

All 12 test projects use `Spectre.Console.Cli.Testing` (`CommandAppTester`). They will need their
test harness updated to CAF's equivalent testing utilities as each tool migrates:

- `Bunnings.Common.Tools.ApigeeGenerator.Tests`
- `Bunnings.Common.Tools.Bagc.Tests`
- `Bunnings.Common.Tools.ChangeLogTool.Tests`
- `Bunnings.Common.Tools.CopilotArtifactManagementTool.Tests`
- `Bunnings.Common.Tools.EmpXTool.Tests`
- `Bunnings.Common.Tools.JsonSchemaTool.Tests`
- `Bunnings.Common.Tools.TemplateEnhancementTool.Tests`
- `Bunnings.UseCase.AspNetCore.Tests`
- `Bunnings.UseCase.AspNetCore.ApigeeGatewayExporterTool.Tests`
- `Bunnings.UseCase.AspNetCore.ProductExporterTool.Tests`
- `Bunnings.UseCase.AspNetCore.ScopeExporterTool.Tests`
- `Bunnings.UseCase.MonoRepoMigrationTool.Tests`
