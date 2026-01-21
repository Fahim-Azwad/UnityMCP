# UnityMCP MCP Server

This is a Model Context Protocol (MCP) server that connects Claude Desktop to a local Unity instance running the UnityMCP Kernel.

## Prerequisites

1.  **Unity Editor**: With `UnityMCP` package installed and running (`Tools > UnityMCP > Start Server`).
2.  **Python 3.11+** (managed via `.venv` in the run script).

## Setup for Claude Desktop

### 1. Config Location
macOS: `~/Library/Application Support/Claude/claude_desktop_config.json`

### 2. Configuration Block
Add the `unitymcp` entry. **Replace `/ABSOLUTE/PATH/TO/...` with the actual path.**

```json
{
  "mcpServers": {
    "unitymcp": {
      "command": "/bin/bash",
      "args": [
        "/ABSOLUTE/PATH/TO/mcp_unitymcp/scripts/run_server.sh"
      ],
      "env": {
        "UNITYMCP_URL": "http://127.0.0.1:7777"
        // "UNITYMCP_TRANSPORT": "stdio" // Optional: Script auto-detects
      }
    }
  }
}
```

### 3. Verification
1.  **Start Unity Server**: `Tools > UnityMCP > Start Server`.
2.  **Restart Claude Desktop**.
3.  **Ask Claude**: "Call unity_ping()".

## Troubleshooting

*   **Server Disconnected**: Usually caused by stdout pollution. The `run_server.sh` script handles this by auto-detecting Claude and suppressing output. Check `/tmp/unitymcp_mcp_stderr.log` for errors.
*   **Manual Testing**: You can run the script in a terminal to start HTTP Dev Mode:
    ```bash
    ./scripts/run_server.sh
    # [UnityMCP] Terminal detected. Running HTTP mode on port 8765...
    ```
