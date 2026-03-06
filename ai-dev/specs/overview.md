# ProPilot — Overview

> Natural language command interface for ArcGIS Pro 3.6
> ArcGIS Pro SDK / .NET 8 / C# / WPF / LLamaSharp

**Version:** 1.0 Draft
**Author:** Chris Lyons
**Date:** 2026-03-05
**Status:** Architecture Phase

---

## Problem Statement

ArcGIS Pro requires users to navigate complex ribbon menus, dockpanes, and geoprocessing tool dialogs to perform common map operations. Users must know where tools live, remember parameter names, and click through multi-step dialogs for operations that could be expressed in a single sentence. There is no unified command interface that accepts natural language input and translates it into Pro SDK operations.

Esri's AI Assistant (beta) is limited to help documentation queries and SQL generation — it cannot directly manipulate the map, change symbology, run selections, or execute geoprocessing tools.

## Solution

ProPilot is an ArcGIS Pro add-in that provides a natural language command bar (ProWindow) where users type plain English commands like "zoom to all features in Boyd County" or "make the streams layer blue and increase line width to 3." A bundled local LLM (LLamaSharp) parses the intent, maps it to Pro SDK operations, and presents a structured preview of the interpreted command before execution. The user confirms, edits, or cancels.

## Target Users

- GIS analysts and technicians who know *what* they want but spend time hunting for *where* the tool lives
- ArcMap migrants who remember doing things quickly and find Pro's UI more cumbersome
- Power users who want keyboard-driven workflows without leaving the map view
- GIS managers and trainers who want to demonstrate AI-augmented GIS workflows
- Government GIS staff who need offline, zero-cost AI tools that comply with state IT policy

## Goals

1. **20-30 curated commands** that work with near-perfect reliability in v1
2. **Always-preview execution model** — every command shows a structured preview before firing
3. **Zero external dependencies** — bundled LLamaSharp, no separate install required
4. **Zero cost to end user** — no API keys, subscriptions, or paid models
5. **Sub-3-second response time** on modest hardware (16GB RAM, no dedicated GPU)
6. **Full map context awareness** — LLM knows layer names, field names, selections, visible extent, symbology
7. **Government-safe** — compliant with Kentucky CIO-126, no data leaves the machine

## Non-Goals (v1)

- Open-ended conversation / chat (this is a command interface, not a chatbot)
- Code generation (no Python/Arcade script generation)
- Geodatabase schema modification commands
- Layout/cartography commands (map view only in v1)
- Training or fine-tuning custom models
