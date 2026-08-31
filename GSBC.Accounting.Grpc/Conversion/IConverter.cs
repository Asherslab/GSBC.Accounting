namespace GSBC.Accounting.Grpc.Conversion;

/// <summary>
/// Db model to contract record, one direction only.
/// </summary>
/// <remarks>
/// <b>There is deliberately no contract-to-Db mapper.</b> The write path builds the Db model by hand
/// from the request, because a submission's totals are recomputed by the server and must not be able
/// to arrive through a mapper that copies whatever the client sent.
/// <para>
/// The empty marker interface is what <c>AddConverters</c> reflects over, so a new converter needs no
/// registration.
/// </para>
/// </remarks>
public interface IConverter<in TIn, out TOut> : IConverter
{
    public TOut Convert(TIn input);
}

public interface IConverter;
