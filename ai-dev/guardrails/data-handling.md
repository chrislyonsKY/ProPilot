# Data Handling Guardrails — ProPilot

## LLM Interaction

- NEVER send geodatabase connection strings, passwords, or credentials to the LLM
- Map context includes layer names, field names, feature counts — NOT field values or actual data
- User commands may contain sensitive query expressions — these are sent to the local LLM only (never transmitted off-machine)
- Ollama runs on localhost — no network egress for LLM calls

## API Keys & Credentials

- NEVER hardcode API keys in source code, config files, settings JSON, or chat messages
- OpenAI API key MUST be read from `OPENAI_API_KEY` environment variable at runtime
- API keys MUST NOT appear in logs, error messages, or debug output
- API keys MUST NOT be committed to git (add `.env` to `.gitignore`)
- If a key is ever exposed (pasted in chat, committed to repo, etc.), revoke it immediately and generate a new one

## User Data

- Command history stored in memory per session (not persisted to disk in v1)
- Settings files contain only: endpoint URL, model name, timeout value
- No telemetry, no usage tracking, no analytics
- ProPilot does not read or store feature attribute values

## Error Messages

- Error messages displayed to user may contain layer names and field names (acceptable)
- Error messages must NOT contain file system paths beyond what the user already knows
- GP tool error messages are passed through as-is (they come from Esri's code)
