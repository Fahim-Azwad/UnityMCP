from typing import Any, Dict, List, Optional
from pydantic import BaseModel, Field

# --- Command Models ---

class CommandData(BaseModel):
    type: str # CreatePrimitive, CreateEmpty, Rename, SetTransform, AddComponent, AttachScript, DestroyByName
    id: Optional[str] = None
    primitive: Optional[str] = None
    name: Optional[str] = None
    target: Optional[str] = None
    parent: Optional[str] = None
    position: Optional[List[float]] = None # [x, y, z]
    rotation: Optional[List[float]] = None # [x, y, z, w]
    scale: Optional[List[float]] = None # [x, y, z]
    componentType: Optional[str] = None
    scriptClass: Optional[str] = None # For AttachScript
    all: bool = False # For DestroyByName

class CommandRequest(BaseModel):
    requestId: Optional[str] = None
    commands: List[CommandData]

# --- Response Models ---

class CreatedObject(BaseModel):
    instanceId: int
    path: str

class CommandResponseData(BaseModel):
    requestId: Optional[str] = None
    created: Dict[str, CreatedObject] = Field(default_factory=dict)

class BaseResponse(BaseModel):
    ok: bool
    errors: List[str] = Field(default_factory=list)

class CommandResponse(BaseResponse):
    data: CommandResponseData

# --- Script Models ---

class ScriptWriteRequest(BaseModel):
    path: str
    content: str

class ScriptWriteResponseData(BaseModel):
    assetPath: str

class ScriptWriteResponse(BaseResponse):
    data: Optional[ScriptWriteResponseData] = None

# --- Compiler Models ---

class CompilerError(BaseModel):
    timestamp: str
    message: str
    stackTrace: str

class CompilerErrorsResponseData(BaseModel):
    errors: List[CompilerError]

class CompilerErrorsResponse(BaseResponse):
    data: CompilerErrorsResponseData

# --- Play Mode Models ---

class PlayRunRequest(BaseModel):
    seconds: int

class PlayRunException(BaseModel):
    timestamp: str
    type: str
    message: str
    stackTrace: str

class PlayRunResponseData(BaseModel):
    exceptions: List[PlayRunException]

class PlayRunResponse(BaseResponse):
    data: PlayRunResponseData
