using GSBC.Accounting.Grpc.Data.Models.Expenses;
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;
using Riok.Mapperly.Abstractions;

namespace GSBC.Accounting.Grpc.Conversion;

/// <summary>
/// Bridges the DB model's <see cref="DateTimeOffset"/> to the contract's UTC <see cref="DateTime"/>.
/// </summary>
/// <remarks>
/// Contracts carry <c>DateTime</c> because protobuf-net has no surrogate for <c>DateTimeOffset</c>; the
/// database carries <c>timestamptz</c>. This is the one place the two meet.
/// </remarks>
public class DateTimeConverter : IConverter<DateTimeOffset, DateTime>
{
    public DateTime Convert(DateTimeOffset input) => input.UtcDateTime;
}

[Mapper]
public partial class ExpenseDetailConverter(
    IConverter<DateTimeOffset, DateTime> dateTimeConverter
) : IConverter<DbExpenseDetail, ExpenseDetail>
{
    [UseMapper]
    private readonly IConverter<DateTimeOffset, DateTime> _dateTimeConverter = dateTimeConverter;

    public partial ExpenseDetail Convert(DbExpenseDetail detail);
}

[Mapper]
public partial class ExpenseDetailItemConverter : IConverter<DbExpenseDetailItem, ExpenseDetailItem>
{
    public partial ExpenseDetailItem Convert(DbExpenseDetailItem item);
}

[Mapper]
public partial class ExpenseAttendeeConverter(
    IConverter<DateTimeOffset, DateTime> dateTimeConverter
) : IConverter<DbExpenseAttendee, ExpenseAttendee>
{
    [UseMapper]
    private readonly IConverter<DateTimeOffset, DateTime> _dateTimeConverter = dateTimeConverter;

    public partial ExpenseAttendee Convert(DbExpenseAttendee attendee);
}

[Mapper]
public partial class ExpenseTripConverter(
    IConverter<DateTimeOffset, DateTime> dateTimeConverter
) : IConverter<DbExpenseTrip, ExpenseTrip>
{
    [UseMapper]
    private readonly IConverter<DateTimeOffset, DateTime> _dateTimeConverter = dateTimeConverter;

    public partial ExpenseTrip Convert(DbExpenseTrip trip);
}

[Mapper]
public partial class MissingReceiptDeclarationConverter(
    IConverter<DateTimeOffset, DateTime> dateTimeConverter
) : IConverter<DbMissingReceiptDeclaration, MissingReceiptDeclaration>
{
    [UseMapper]
    private readonly IConverter<DateTimeOffset, DateTime> _dateTimeConverter = dateTimeConverter;

    public partial MissingReceiptDeclaration Convert(DbMissingReceiptDeclaration declaration);
}

[Mapper]
public partial class ExpenseSubmissionConverter(
    IConverter<DateTimeOffset, DateTime> dateTimeConverter
) : IConverter<DbExpenseSubmission, ExpenseSubmission>
{
    [UseMapper]
    private readonly IConverter<DateTimeOffset, DateTime> _dateTimeConverter = dateTimeConverter;

    public partial ExpenseSubmission Convert(DbExpenseSubmission submission);
}
