<p align="center">
  <img src="assets/logo-horizontal.png" alt="DuckDNSUpdater" width="520">
</p>

# DuckDNS Updater

Portable Windows app (.NET 8 WinForms) that updates your DuckDNS domain on a configurable interval.

## Requirements

- Windows x64
- To build: [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer
- To run the published EXE: no installed .NET required (self-contained)

## Configuration (`config.json`)

Lives next to the EXE. On first start a template is created if the file is missing.

| Field | Meaning |
|------|-----------|
| `domain` | DuckDNS subdomain without `.duckdns.org` |
| `token` | DuckDNS account token |
| `intervalSeconds` | Update interval in seconds (min. 30, max. 86400) |
| `autoStart` | `true` starts the updater when the app launches |
| `writeLogsToFile` | `true` appends log lines to `duckdns-updater.log` next to the EXE |

Example:

```json
{
  "domain": "my-host",
  "token": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
  "intervalSeconds": 300,
  "autoStart": false,
  "writeLogsToFile": false
}
```

## Usage

1. Enter domain and token
2. Set the interval in seconds
3. **Save** writes `config.json`
4. **Start** runs an update immediately and repeats on the interval
5. **Stop** ends the timer

While the updater is running, the input fields are locked. **Write logs to file** can be toggled at any time.

## Build & portable publish

```bash
dotnet build DuckDNSUpdater.sln -c Release
dotnet test DuckDNSUpdater.sln -c Release
dotnet publish src/DuckDNSUpdater -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Unit and E2E tests live in `tests/DuckDNSUpdater.Tests` (xUnit; FlaUI for UI smoke).
Output: `src/DuckDNSUpdater/bin/Release/net8.0-windows/win-x64/publish/`  
`config.json` is copied along.

