# ProPilot — Dependencies

---

## NuGet Packages

| Package | Version | Purpose |
|---|---|---|
| Esri.ArcGISPro.Extensions.* | 3.6 | ArcGIS Pro SDK — map access, GP tools, CIM, UI framework |
| CommunityToolkit.Mvvm | Latest stable | MVVM source generators: [ObservableProperty], [RelayCommand] |
| LLamaSharp | Latest stable | In-process LLM inference (llama.cpp C# binding) |
| LLamaSharp.Backend.Cpu | Latest stable | Native backend for CPU inference (no CUDA required) |
| System.Text.Json | Built-in (.NET 8) | JSON serialization/deserialization |
| System.Net.Http.Json | Built-in (.NET 8) | HTTP JSON helper methods for Ollama/OpenAI clients |

### Do NOT Add

| Package | Reason |
|---|---|
| Newtonsoft.Json | Pro 3.6 pins 13.0.3.27908 — only use if transitive dependency requires it |
| LLamaSharp.Backend.Cuda11/12 | Adds CUDA dependency — CPU-only for max compatibility |
| Microsoft.Extensions.DependencyInjection | Overkill for an add-in — use manual construction |
| Any logging framework | Use `System.Diagnostics.Debug.WriteLine()` — Pro has its own logging |

---

## External Dependencies (User Machine)

### Required

| Component | Source | Notes |
|---|---|---|
| ArcGIS Pro 3.6 | Esri licensing | Host application |
| .NET 8.0 Runtime | Installed with Pro 3.6 | No separate install needed |

### Required (First Run — Auto-Downloaded)

| Component | Source | Size | Stored At |
|---|---|---|---|
| GGUF model file (Light) | HuggingFace: `microsoft/Phi-3-mini-4k-instruct-gguf` | ~1.5 GB | `%APPDATA%\ProPilot\models\` |
| GGUF model file (Standard) | HuggingFace: `TheBloke/Mistral-7B-Instruct-v0.2-GGUF` | ~4 GB | `%APPDATA%\ProPilot\models\` |

### Optional (Fallback Providers)

| Component | Source | When Needed |
|---|---|---|
| Ollama | `https://ollama.com/download` | Only if LLamaSharp native DLLs are blocked |
| OpenAI API key | `https://platform.openai.com/api-keys` | Only if user opts into cloud provider |

---

## Version Pins

| Dependency | Pin | Reason |
|---|---|---|
| ArcGIS Pro SDK | 3.6 | Target platform |
| .NET | 8.0 | Pro 3.6 requires .NET 8 |
| Newtonsoft.Json (if used) | 13.0.3.27908 | Pro 3.6 bundles this exact version |
| C# language version | 12 | Matches .NET 8 SDK default |
| Visual Studio | 2022 17.8+ | Required for .NET 8 + Pro SDK 3.6 |

---

## File System Footprint

| Location | Contents | Size |
|---|---|---|
| `Documents\ArcGIS\AddIns\ArcGISPro\{guid}\` | Add-in DLLs + native LLamaSharp backend | ~50-100 MB |
| `%APPDATA%\ProPilot\models\` | GGUF model file(s) | 1.5-4 GB per model |
| `%APPDATA%\ProPilot\settings.json` | User-level settings | < 1 KB |
| `%APPDATA%\ProPilot\logs\` | Audit logs (v1.x) | Grows over time |
| Project `.aprx` custom properties | Project-level settings | < 1 KB |
