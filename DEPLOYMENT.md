# ProPilot Deployment Guide

This guide covers deploying ProPilot across different organizational environments, from personal use to government air-gapped networks.

---

## Quick Install (Personal / Open Source)

**For individual users and open-source contributors:**

1. Download the latest `.esriAddInX` from [GitHub Releases](https://github.com/chrislyonsKY/ProPilot/releases)
2. Double-click the `.esriAddInX` file
3. ArcGIS Pro installs it automatically to `%LOCALAPPDATA%\ESRI\ArcGISPro\AssemblyCache`
4. Restart ArcGIS Pro if it's running
5. Click the **ProPilot** button on the Add-In tab

**No admin rights required.** The add-in installs to your user profile.

---

## Government / Enterprise Deployment

### Standard Deployment (Internet Access)

**Best for:** State/federal agencies with standard network access

**Steps:**

1. **IT Review** — Share the architecture docs (`ai-dev/`) with your security team
2. **Risk Classification** — ProPilot is **low-risk** under Kentucky CIO-126:
   - No data leaves the machine
   - Human review before every action
   - No cloud dependencies
   - Open-weight models (auditable)
3. **User Installation** — Users download `.esriAddInX` from GitHub and self-install
4. **Model Download** — First run downloads a GGUF model from HuggingFace (~1.5–4 GB)
5. **No Ongoing Maintenance** — Model runs locally; no updates required

**Network Requirements:**
- GitHub (for add-in download): `https://github.com/chrislyonsKY/ProPilot/releases`
- HuggingFace (for model download, one-time): `https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf` or `https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF`

---

### Restricted Network Deployment

**Best for:** Agencies that block HuggingFace or native DLLs

**Option A: Pre-download Model**

1. On an internet-connected machine, download a GGUF model:
   - **Light:** [Phi-3 Mini 3.8B](https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/blob/main/Phi-3-mini-4k-instruct-q4.gguf) (~1.5 GB)
   - **Standard:** [Mistral 7B Instruct](https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.2-GGUF/blob/main/mistral-7b-instruct-v0.2.Q4_K_M.gguf) (~4 GB)
2. Transfer the `.gguf` file via approved media (USB, network share)
3. Users install the `.esriAddInX` normally
4. On first run, users click **Browse for Model** and select the transferred `.gguf` file

**Option B: Ollama Deployment (for environments blocking LLamaSharp DLLs)**

1. IT installs [Ollama](https://ollama.com) on a shared server or user workstations
2. IT pulls a model: `ollama pull mistral`
3. Users install the ProPilot `.esriAddInX`
4. Users open **Settings** and switch provider from "bundled" to "ollama"
5. Set endpoint to `http://localhost:11434` (or server URL)

**Advantages:**
- No native DLLs (some orgs block `llama.cpp` binaries)
- Centralized model management
- Easier model swapping for power users

---

### Air-Gapped Deployment

**Best for:** Classified or fully isolated networks

**Prerequisites:**
- Approved file transfer mechanism (e.g., DISA-approved USB, physical media)
- ArcGIS Pro 3.6 already installed in the air-gapped environment

**Steps:**

1. **Download on Internet-Connected Machine:**
   - ProPilot `.esriAddInX` from GitHub Releases
   - GGUF model file (`.gguf`) from HuggingFace
   - SHA-256 checksums for both files

2. **Transfer via Approved Media:**
   - Burn files to CD/DVD, or transfer via DISA-approved USB
   - Include checksums for integrity verification

3. **Verify Checksums:**
   ```powershell
   Get-FileHash -Algorithm SHA256 ProPilot.esriAddInX
   Get-FileHash -Algorithm SHA256 Phi-3-mini-4k-instruct-q4.gguf
   ```

4. **Install in Air-Gapped Environment:**
   - Users double-click `.esriAddInX` to install
   - Copy `.gguf` file to a shared network location or each user's `%APPDATA%\ProPilot\models\` directory

5. **First Run:**
   - Click **Browse for Model** and navigate to the `.gguf` file
   - ProPilot never requires internet access after this step

**Data Flow:** ProPilot processes everything locally. No telemetry, no analytics, no "phone home." Fully compliant with air-gapped security policies.

---

## Group Policy Deployment (Optional)

**For large organizations wanting centralized control:**

### Pre-configure Settings

1. Create a default settings file:
   ```json
   {
     "LlmProvider": "bundled",
     "ModelName": "mistral",
     "TimeoutSeconds": 30,
     "OllamaEndpoint": "http://localhost:11434",
     "OpenAiModelName": "gpt-4o-mini"
   }
   ```

2. Deploy via GPO to: `%APPDATA%\ProPilot\settings.json`

### Pre-stage Model Files

1. Download GGUF model to a network share: `\\server\share\ProPilot\models\`
2. Users' first-run setup automatically detects pre-staged models
3. No internet download required

---

## Compliance Documentation

### Security Review Checklist

Provide your security team with these documents:

- `ai-dev/architecture.md` — Data flow, threading model, interfaces
- `ai-dev/guardrails/compliance.md` — CIO-126 risk classification mapping
- `ai-dev/guardrails/coding-standards.md` — Development constraints
- `README.md` (this file) — User-facing overview

### Risk Classification (Kentucky CIO-126)

| Category | Classification | Justification |
|---|---|---|
| **Data Exposure** | Low | No data leaves the machine; only layer/field names processed |
| **Human Review** | Required | Every command previewed before execution |
| **Model Provenance** | Auditable | Open-weight models (Phi-3: MIT, Mistral: Apache 2.0) |
| **Network Dependency** | None (after setup) | Model runs in-process |
| **PII Risk** | None | Map context excludes attribute values |

---

## Troubleshooting

### Model Download Fails

**Symptom:** Setup window shows "Download failed" or progress stalls

**Solutions:**
1. Check network access to HuggingFace: `https://huggingface.co`
2. Verify firewall allows HTTPS on port 443
3. Use **Browse for Model** to select a pre-downloaded `.gguf` file
4. Switch to Ollama provider if native DLLs are blocked

### Add-In Doesn't Appear in ArcGIS Pro

**Symptom:** No ProPilot button on Add-In tab

**Solutions:**
1. Verify ArcGIS Pro 3.6 is installed (check Help ? About)
2. Check Add-In Manager (Project ? Add-In Manager) — ProPilot should be listed
3. If not listed, reinstall by double-clicking `.esriAddInX`
4. Check Windows Event Viewer for .NET load errors

### "No Model Found" Error

**Symptom:** Clicking ProPilot shows "No model file found" error

**Solutions:**
1. Check `%APPDATA%\ProPilot\models\` for `.gguf` files
2. Run the setup flow again (delete models folder and reopen)
3. Manually copy a `.gguf` file to the models directory
4. Switch to Ollama provider in Settings

### LLM Returns Invalid JSON

**Symptom:** Commands fail with "Failed to parse command" in status bar

**Solutions:**
1. This is usually a model issue — try switching to the Standard tier (Mistral 7B)
2. Verify model file integrity (check file size matches expected)
3. For Ollama users: `ollama pull mistral` to re-download
4. For bundled: delete model and re-download via Setup

---

## Version Compatibility

| ProPilot Version | ArcGIS Pro Version | .NET Version | Notes |
|---|---|---|---|
| 1.0 | 3.6 | 8.0 | Initial release |
| Future 1.x | 3.6+ | 8.0+ | Backward compatible with Pro 3.6 |

---

## Support

- **Issues:** [GitHub Issues](https://github.com/chrislyonsKY/ProPilot/issues)
- **Discussions:** [GitHub Discussions](https://github.com/chrislyonsKY/ProPilot/discussions)
- **Email:** chris.lyons@ky.gov (for government/enterprise inquiries)

---

## Uninstallation

1. Open ArcGIS Pro
2. Go to **Project ? Add-In Manager**
3. Find **ProPilot** in the list
4. Click **Remove**
5. Restart ArcGIS Pro

To fully clean up:
```powershell
# Remove settings and models
Remove-Item -Recurse "$env:APPDATA\ProPilot"

# Remove add-in cache (optional)
Remove-Item "$env:LOCALAPPDATA\ESRI\ArcGISPro\AssemblyCache\ProPilot*"
```

---

**Questions?** Open an issue on GitHub or email chris.lyons@ky.gov.
