# Third-Party Package Licenses

Every third-party dependency in this repository is under a permissive open-source license
(MIT or Apache-2.0), consistent with the technology-selection rules in `ASSUMPTIONS.md` §1
(QuestPDF was rejected specifically because its free tier is revenue-gated, not open source).

## NuGet packages

| Package | License | Used in |
|---|---|---|
| Microsoft.EntityFrameworkCore, .SqlServer, .Design, .InMemory | MIT | Infrastructure, Infrastructure.Tests |
| Microsoft.AspNetCore.Authentication.JwtBearer | MIT | Api |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | MIT | Infrastructure |
| Microsoft.AspNetCore.OpenApi | MIT | Api |
| Microsoft.AspNetCore.Mvc.Testing | MIT | Api.IntegrationTests |
| Microsoft.Extensions.* (Hosting, Hosting.WindowsServices, DependencyInjection, Configuration*, Http, Options.ConfigurationExtensions, Caching.Memory, Logging.Abstractions) | MIT | Api, Application, Infrastructure, Desktop |
| Microsoft.IdentityModel.Tokens, System.IdentityModel.Tokens.Jwt | MIT | Infrastructure |
| Microsoft.NET.Test.Sdk | MIT | all test projects |
| Swashbuckle.AspNetCore | MIT | Api |
| CommunityToolkit.Mvvm | MIT | Desktop |
| System.Security.Cryptography.ProtectedData | MIT | Desktop (DPAPI-encrypted "remember me" refresh token) |
| FluentValidation, FluentValidation.AspNetCore, FluentValidation.DependencyInjectionExtensions | MIT | Application, Api |
| ClosedXML | MIT | Reporting (BOM/MTO/internal-report/quotation `.xlsx` export) |
| coverlet.collector | MIT | all test projects |
| xunit, xunit.runner.visualstudio | Apache-2.0 | all test projects |
| Moq | MIT | Desktop.Tests (referenced by the scaffold; the fake-HTTP-handler tests in this repo did not end up needing it, but it stays available for future ViewModel tests that want it) |
| Microsoft.Playwright | Apache-2.0 | Reporting (drives headless Chromium for PDF rendering — see `ASSUMPTIONS.md` §1 for why over MigraDoc/PdfSharp) |
| PdfPig | Apache-2.0 | Reporting.Tests, Api.IntegrationTests (PDF text-layer assertions in tests only — never shipped in the product itself) |
| Serilog, Serilog.AspNetCore, Serilog.Extensions.Hosting, Serilog.Settings.Configuration, Serilog.Sinks.File, Serilog.Enrichers.Environment, Serilog.Enrichers.Thread | Apache-2.0 | Api, Desktop |
| WixToolset.Sdk, WixToolset.UI.wixext, WixToolset.Util.wixext (v5.x) | MIT | installer/NTNP.Pricing.Installer — WiX Toolset relicensed from MS-RL to MIT starting with v4; v5 (used here) stays MIT. v6/v7 introduced a separate "Open Source Maintenance Fee" for the CLI tool itself — deliberately avoided; see `ASSUMPTIONS.md` §2/§11. |

Headless Chromium itself (driven by Playwright, not redistributed by this repository) is under the
BSD-3-Clause-derived Chromium license; Playwright downloads it separately via `playwright install
chromium` on the application server (see `docs/deployment.md`).

## Fonts

| Font | License | Notes |
|---|---|---|
| Vazirmatn (Regular/Medium/SemiBold/Bold) | SIL Open Font License 1.1 | Persian typeface, bundled in `src/NTNP.Pricing.Desktop/Assets/Fonts/` and `src/NTNP.Pricing.Reporting/Assets/Fonts/` (embedded in generated PDFs); full license text at `docs/licenses/OFL-1.1-Vazirmatn.txt`. Freely redistributable, including in a commercial product, provided the font itself is not sold on its own. |

## What this means for distribution

- No package here requires this product's own source code to be published (no copyleft/GPL/LGPL
  dependencies).
- No package here is "free for non-commercial use only" or otherwise revenue-gated.
- Attribution: MIT and Apache-2.0 both require preserving their license text somewhere reachable
  from a distributed copy — this file plus each package's own license (fetched with the package by
  NuGet, under `~/.nuget/packages/<id>/<version>/`) satisfy that. The WiX installer's `License.rtf`
  is a placeholder NTNP internal-use notice, not a third-party license page — see the note inside
  that file and `ASSUMPTIONS.md` §10.
