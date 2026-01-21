from fastmcp import FastMCP
import json
from typing import Optional
from .client import UnityClient, UnityMCPError

# Create FastMCP app
mcp = FastMCP("unitymcp")

# Initialize client
client = UnityClient()

@mcp.tool()
def unity_ping() -> dict:
    """Ping the Unity server to check if it's reachable."""
    alive = client.ping()
    return {"ok": alive}

@mcp.tool()
def unity_snapshot() -> dict:
    """Get a JSON snapshot of the current Unity scene hierarchy."""
    return client.snapshot()

@mcp.tool()
def unity_logs_recent(count: int = 50) -> list:
    """Get the most recent logs from the Unity Console."""
    return client.logs_recent(count)

@mcp.tool()
def unity_write_script(path: str, content: str) -> dict:
    """
    Write a C# script to Assets/Generated/ in Unity.
    
    Args:
        path: Relative path (e.g. Test/MyScript.cs)
        content: C# source code
    """
    return client.write_script(path, content)

@mcp.tool()
def unity_compiler_errors(since: Optional[str] = None, clear: bool = False) -> list:
    """
    Get recent compiler errors from Unity.
    
    Args:
        since: Optional ISO timestamp filter
        clear: If True, clears the error buffer after reading
    """
    return client.compiler_errors(since, clear)

@mcp.tool()
def unity_apply(request_id: str, commands_json: str) -> dict:
    """
    Apply a batch of commands to the Unity scene.
    
    Best Practices for Reliability:
    1. CLEANUP FIRST: Always use "DestroyByName" (with all:true) for objects you are about to create.
       Example: [{"type":"DestroyByName", "name":"MyObject", "all":true}, {"type":"CreatePrimitive", ...}]
    2. IDEMPOTENCY: Ensure your sequence produces the same result if run twice.
    3. RETRIES: If this tool fails due to disconnection (Unity recompile), call unity_ping() until it returns true, then retry.
    
    Args:
        request_id: Unique ID for the request
        commands_json: A JSON string containing a list of command objects.
                       Supported types: CreatePrimitive, CreateEmpty, Renamed, SetTransform, AddComponent, AttachScript, DestroyByName.
    """
    try:
        commands = json.loads(commands_json)
        if not isinstance(commands, list):
            raise ValueError("commands_json must parse to a list")
        return client.apply(request_id, commands)
    except json.JSONDecodeError as e:
        return {"ok": False, "errors": [f"Invalid JSON in commands_json: {e}"]}
    except Exception as e:
        return {"ok": False, "errors": [str(e)]}

@mcp.tool()
def unity_play_run(seconds: int = 2) -> list:
    """
    Run Unity Play Mode for N seconds and return any runtime exceptions.
    
    Args:
        seconds: Duration to stay in Play Mode (default 2)
    """
    return client.play_run(seconds)

if __name__ == "__main__":
    import os
    import sys
    import logging

    # Configure logging
    # In stdio mode, we MUST NOT print to stdout.
    # In http mode, stdout is fine, but stderr is also fine.
    # We stick to stderr for consistency.
    logging.basicConfig(
        level=logging.INFO,
        stream=sys.stderr,
        format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )

    # Safe-guard: If running as main, ensure we don't accidentally print anything via other imports
    # (Existing logging config is already robust)

    transport = os.getenv("UNITYMCP_TRANSPORT", "stdio")
    
    # Attempt to suppress FastMCP banner via internal attributes or env vars if possible
    # (FASTMCP_NO_BANNEREnv var should be set by caller)

    if transport == "http":
        # Dev mode: HTTP/SSE
        host = "127.0.0.1"
        port = 8765
        sys.stderr.write(f"[UnityMCP] Starting in HTTP mode on http://{host}:{port}\n")
        mcp.run(transport="sse", host=host, port=port)
    else:
        # Standard mode: stdio (for Claude)
        # Force strict stdio
        try:
            mcp.run(transport="stdio")
        except Exception as e:
             sys.stderr.write(f"Server Error: {e}\n")
             sys.exit(1)
