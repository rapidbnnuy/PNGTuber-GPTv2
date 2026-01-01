# Command Mapping & Configuration

This document serves as the source of truth for Streamer.bot Command GUIDs. The plugin uses these GUIDs to programmatically enable commands on startup and map incoming execution events to the correct internal logic, regardless of the user-facing command name (e.g., `!setnick` could be renamed to `!changeself`, but the GUID remains constant).

## Administrative Commands

| Command | GUID | Description |
| :--- | :--- | :--- |
| `!help` | `c2332078-d9cb-4cbc-80ce-21bb86330986` | Prints a list of available commands to chat. |
| `!clearchathistory` | `8ac3c57a-d353-4bdb-8768-52eb15352a4a` | Clears in-memory chat history queue. |
| `!clearprompthistory` | `799c4402-33f3-425d-979f-0fd36e27a1aa` | Clears GPT message history (context). |
| `!clearlogfile` | `e5759e3f-edc1-453d-a67a-009c013d7e4c` | Clears the current log file. |
| `!version` | `66cdc4d3-c52d-4c31-b323-277f4d52c6a0` | Returns current solution version. |

## Identity & Knowledge Commands

| Command | GUID | Description |
| :--- | :--- | :--- |
| `!setnick <nick>` | `ccd7aa20-4a65-4467-ad34-4a50ff74e163` | Sets user's nickname in DB. |
| `!removenick` | `72a08e87-463b-4a3d-ae16-de5b8f51c18f` | Removes user's nickname. |
| `!currentnick` | `17b06f6a-2f25-4fd0-a0e6-48aacf5cc782` | Prints current nickname/pronouns. |
| `!setpronouns` | `777782d6-9e97-4dae-9f86-dafaa58cafaa` | Instructions for setting pronouns (Alejo). |
| `!forget` | `23890ed1-131a-42bd-90f0-9d36c5b775b6` | Clears user knowledge entry. |
| `!getmemory` | `23a7aa90-a1ad-464c-a6c4-861f73223084` | Prints user knowledge entry. |
| `!rememberthis <k> <v>`| `80f22f3f-0197-4347-b37f-b6993c86d68a` | Saves keyword/definition pair. |
| `!forgetthis <k>` | `66cdc4d3-c52d-4c31-b323-277f4d52c6a0` | Deletes keyword/definition pair. |

## AI & Character Interaction

| Command | GUID | Description |
| :--- | :--- | :--- |
| `!? <msg>` | `6a23a150-93ce-4201-8db2-d2fd866f90d8` | GPT Response (Character 1). |
| `!? 2 <msg>` | `43cd2165-35d8-4250-809a-91ea076c9b9f` | GPT Response (Character 2). |
| `!? 3 <msg>` | `b2167b8a-d9ab-41d2-bdf2-5a17138316f6` | GPT Response (Character 3). |
| `!? 4 <msg>` | `c2108c71-e7b1-4f66-8481-56c9fb20ab2a` | GPT Response (Character 4). |
| `!? 5 <msg>` | `58528480-483f-4612-b172-eae7f341ea0a` | GPT Response (Character 5). |

## TTS / Speaker Commands

| Command | GUID | Description |
| :--- | :--- | :--- |
| `!speak <msg>` | `1b833436-5215-4053-a8e3-46d9f1ba3af7` | TTS (Character 1). |
| `!speak 2 <msg>` | `38f9b8a5-8aed-42e8-916d-effd433777bb` | TTS (Character 2). |
| `!speak 3 <msg>` | `35b592dc-c861-449e-861f-93abbf203ef3` | TTS (Character 3). |
| `!speak 4 <msg>` | `90ab0841-9de0-4579-bcc9-a65ac253d1a1` | TTS (Character 4). |
| `!speak 5 <msg>` | `3ef90757-e83d-4f7b-9a54-d8a4edd7e1e8` | TTS (Character 5). |

## Simple Actions

| Command | GUID | Description |
| :--- | :--- | :--- |
| `!sayplay` | `6caffb4d-7a47-46cc-b7c2-084cd49127b6` | Says "!play" in chat. |
