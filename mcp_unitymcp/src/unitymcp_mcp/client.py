import os
import time
import httpx
from typing import Optional, List, Dict, Any

class UnityMCPError(Exception):
    """Custom exception for UnityMCP client errors."""
    pass

class UnityClient:
    def __init__(self):
        # Base URL from env var or default
        # Base URL from env var or default
        self.base_url = os.getenv("UNITYMCP_UNITY_URL", os.getenv("UNITYMCP_URL", "http://127.0.0.1:7777"))
        # Timeouts: 2s connect, 10s read
        self.client = httpx.Client(timeout=httpx.Timeout(10.0, connect=2.0))

    def _check_response(self, resp: httpx.Response) -> Dict[str, Any]:
        """Validates HTTP response and Unity's 'ok' field."""
        if resp.status_code != 200:
            raise UnityMCPError(f"HTTP Error {resp.status_code}: {resp.text}")
        
        try:
            data = resp.json()
        except Exception:
            raise UnityMCPError(f"Invalid JSON response: {resp.text}")

        if not data.get("ok"):
            errors = data.get("errors", [])
            error_msg = ", ".join(errors) if errors else "Unknown error"
            raise UnityMCPError(f"Unity Error: {error_msg}")
            
        return data

    def logs_recent(self, count: int = 50) -> List[Dict[str, Any]]:
        """GET /logs/recent"""
        try:
            resp = self.client.get(f"{self.base_url}/logs/recent", params={"count": count})
            data = self._check_response(resp)
            return data.get("data", {}).get("logs", [])
        except httpx.RequestError as e:
            raise UnityMCPError(f"Connection failed: {e}")

    def snapshot(self) -> Dict[str, Any]:
        """GET /state/snapshot"""
        try:
            resp = self.client.get(f"{self.base_url}/state/snapshot")
            data = self._check_response(resp)
            return data.get("data", {}).get("snapshot", {})
        except httpx.RequestError as e:
            raise UnityMCPError(f"Connection failed: {e}")

    def write_script(self, path: str, content: str) -> Dict[str, Any]:
        """POST /scripts/write"""
        try:
            payload = {"path": path, "content": content}
            resp = self.client.post(f"{self.base_url}/scripts/write", json=payload)
            data = self._check_response(resp)
            return data.get("data", {})
        except httpx.RequestError as e:
            raise UnityMCPError(f"Connection failed: {e}")

    def compiler_errors(self, since: Optional[str] = None, clear: bool = False) -> List[Dict[str, Any]]:
        """GET /compiler/errors"""
        try:
            params = {}
            if since: params["since"] = since
            if clear: params["clear"] = "true"
            resp = self.client.get(f"{self.base_url}/compiler/errors", params=params)
            data = self._check_response(resp)
            return data.get("data", {}).get("errors", [])
        except httpx.RequestError as e:
            raise UnityMCPError(f"Connection failed: {e}")

    def apply(self, request_id: str, commands: List[Dict[str, Any]]) -> Dict[str, Any]:
        """POST /command/apply"""
        try:
            payload = {"requestId": request_id, "commands": commands}
            resp = self.client.post(f"{self.base_url}/command/apply", json=payload)
            data = self._check_response(resp)
            return data # Return full data to include 'created' etc.
        except httpx.RequestError as e:
            raise UnityMCPError(f"Connection failed: {e}")

    def play_run(self, seconds: int = 2) -> List[Dict[str, Any]]:
        """POST /play/run (Async orchestration)"""
        try:
            # 1. Start Play Mode
            resp = self.client.post(f"{self.base_url}/play/run", json={"seconds": seconds})
            self._check_response(resp) # Ensure it started

            # 2. Wait for execution (seconds + buffer)
            time.sleep(seconds + 1.0) 

            # 3. Fetch results
            resp_last = self.client.get(f"{self.base_url}/play/last")
            data = self._check_response(resp_last)
            return data.get("data", {}).get("exceptions", [])
        except httpx.RequestError as e:
            raise UnityMCPError(f"Connection failed: {e}")

    def ping(self) -> bool:
        """Tiny ping method using logs_recent(1)"""
        try:
            self.logs_recent(1)
            return True
        except:
            return False
