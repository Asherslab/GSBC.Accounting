// Copied from GSBC.ImpactKids' Shared.Contracts. It is what makes a bare [ProtoContract] and
// CallContext work in every file here without a using, and it is the reason a new contract file can be
// three lines long.
global using ProtoBuf;
global using ProtoBuf.Grpc;
global using ProtoBuf.Grpc.Configuration;

global using GSBC.Accounting.Shared.Contracts.Entities.Interfaces;
global using GSBC.Accounting.Shared.Contracts.Messages.Responses.Base;
