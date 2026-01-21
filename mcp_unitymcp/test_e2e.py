import sys
import os
import time

# Add src to path
sys.path.append(os.path.join(os.path.dirname(__file__), "src"))

from unitymcp_mcp.client import UnityClient

def main():
    print("Testing UnityMCP Python Client...")
    client = UnityClient()

    # 1. Ping
    if client.ping():
        print("✅ Ping Successful")
    else:
        print("❌ Ping Failed (Server might be down)")
        return

    # 2. Snapshot
    try:
        snap = client.get_snapshot()
        obj_count = len(snap.get("objects", []))
        print(f"✅ Snapshot: {obj_count} objects")
    except Exception as e:
        print(f"❌ Snapshot Failed: {e}")

    # 3. Write Script
    try:
        resp = client.write_script("Test/PyGenerated.cs", "using UnityEngine; public class PyGenerated : MonoBehaviour {}")
        print(f"✅ Write Script: {resp.data.assetPath}")
    except Exception as e:
        print(f"❌ Write Script Failed: {e}")

    # 4. Compiler Errors
    try:
        errs = client.get_compiler_errors(clear=True)
        print(f"✅ Compiler Errors: {len(errs.data.errors)} errors (cleared)")
    except Exception as e:
        print(f"❌ Compiler Errors Failed: {e}")

    print("Test Complete.")

if __name__ == "__main__":
    main()
