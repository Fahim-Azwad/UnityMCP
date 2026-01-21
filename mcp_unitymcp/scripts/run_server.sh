#!/usr/bin/env bash
set -euo pipefail

# Redirect stderr to log file for debugging
exec 2>>/tmp/unitymcp_mcp_stderr.log

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

cd "$ROOT_DIR"

# Ensure venv exists (silent)
if [ ! -d ".venv" ]; then
    python3 -m venv .venv >&2
fi

# Activate venv
source .venv/bin/activate

# Install dependencies if needed (silent)
pip install -e . > /dev/null 2>&1

# Add src to PYTHONPATH (backup)
export PYTHONPATH="$ROOT_DIR/src:${PYTHONPATH:-}"

export UNITYMCP_URL="${UNITYMCP_URL:-http://127.0.0.1:7777}"

# IMPORTANT: cd into src so fastmcp can find the module 'unitymcp_mcp'
cd "$ROOT_DIR/src"

if [ -t 1 ]; then
    # Interactive Terminal -> HTTP Mode (Dev)
    echo "[UnityMCP] Terminal detected. Running HTTP mode on port 8765..." >&2
    fastmcp run unitymcp_mcp.server:mcp --transport=http --port=8765 --no-banner
else
    # Non-interactive (Claude) -> Stdio Mode (Prod)
    # STRICTLY NO OUTPUT TO STDOUT
    fastmcp run unitymcp_mcp.server:mcp --transport=stdio --no-banner
fi
