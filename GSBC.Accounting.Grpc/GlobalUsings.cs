// The Shared.Contracts project has its own GlobalUsings; global usings do not cross a project
// boundary, so the service implementations need CallContext imported here too. Without it the failure
// reads as "does not implement interface member ...Create(..., CallContext)", which points at the
// interface rather than at the missing using.
global using ProtoBuf.Grpc;
