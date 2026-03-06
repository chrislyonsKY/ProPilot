# ProPilot — Acceptance Criteria

---

## Must Have (v1.0)

- [ ] ProWindow opens via ribbon button and keyboard shortcut (Ctrl+Shift+P)
- [ ] User can type natural language commands in the command bar
- [ ] Commands are parsed by bundled LLamaSharp (no external dependencies)
- [ ] First-run SetupWindow prompts for model tier selection and download
- [ ] Manual GGUF file placement supported (Browse button in SetupWindow)
- [ ] Parsed commands display in preview panel before execution
- [ ] User can Execute, Cancel, or Edit previewed commands
- [ ] All 30 curated commands work reliably (>90% correct interpretation)
- [ ] Map context (layers, fields, selections, extent) is passed to LLM
- [ ] Fuzzy layer/field name matching works (e.g., "streams" → "Streams_KY")
- [ ] Settings persist per-project with user defaults fallback
- [ ] Status bar shows model name, provider, and connection state
- [ ] Command history stored per session (collapsible panel)
- [ ] Error handling for: model not found, inference timeout, invalid JSON, no active map

## Should Have (v1.x)

- [ ] Ollama fallback provider (for environments where native DLLs are blocked)
- [ ] OpenAI cloud provider (opt-in with environment variable API key)
- [ ] Settings ProWindow with provider selection and configuration
- [ ] Command history persistence across sessions
- [ ] "Did you mean?" suggestions when confidence is low (<0.5)
- [ ] Keyboard shortcuts: Enter to submit, Escape to cancel, Ctrl+Enter to execute
- [ ] Command autocomplete based on history
- [ ] Audit logging for CIO-126 compliance (timestamp, command, result)

## Could Have (v2)

- [ ] Additional LLM providers (Anthropic, Azure OpenAI, local ONNX)
- [ ] Layout/cartography commands
- [ ] Custom user-defined command mappings (extensible command vocabulary)
- [ ] Voice input (speech-to-text → command)
- [ ] Geodatabase attachment support in map context
- [ ] Fully bundled model inside .esriAddInX (no first-run download)
- [ ] Multi-language support (command input in Spanish, French, etc.)

---

## Testing Criteria

### Per-Command Test Matrix

Each of the 30 commands must be tested with at least:
- 3 natural language variations (different phrasings, same intent)
- Correct layer/field resolution from fuzzy input
- Correct parameter extraction (distance, color, expression)
- Preview accuracy (all parameters shown correctly)
- Successful execution (map state changes as expected)

### Edge Case Tests

- [ ] Empty map (no layers) — graceful "no layers available" message
- [ ] No selection when selection-dependent command issued
- [ ] Misspelled layer name — fuzzy match resolves correctly
- [ ] Ambiguous command — most likely interpretation selected
- [ ] Unknown command — `command_type: "unknown"` with helpful message
- [ ] LLM timeout — user sees timeout message, not crash
- [ ] Invalid JSON response — retry once, then show error
- [ ] Model not downloaded — SetupWindow appears instead of CommandWindow
- [ ] Low confidence (<0.5) — warning indicator in preview

### Performance Targets

| Metric | Target | Measured On |
|---|---|---|
| Command parse time (LLamaSharp, Phi-3 Mini) | < 2 seconds | 16GB RAM, CPU |
| Command parse time (LLamaSharp, Mistral 7B) | < 4 seconds | 16GB RAM, CPU |
| Command parse time (Ollama, Mistral 7B) | < 3 seconds | 16GB RAM, CPU |
| Model load time (first command, Phi-3 Mini) | < 8 seconds | 16GB RAM, CPU |
| Model load time (first command, Mistral 7B) | < 15 seconds | 16GB RAM, CPU |
| SDK command execution | < 0.5 seconds | Any hardware |
| GP tool execution | Varies by tool | Standard Pro performance |
