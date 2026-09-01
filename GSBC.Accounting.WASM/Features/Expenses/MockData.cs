#if DEBUG
using GSBC.Accounting.Shared.Contracts.Entities.Features.Expenses;

namespace GSBC.Accounting.WASM.Features.Expenses;

/// <summary>
/// Fills a form with a plausible, randomly chosen scenario, so the pages can be demonstrated and the
/// PDF exercised without twenty minutes of typing.
/// </summary>
/// <remarks>
/// <b>The whole file is inside <c>#if DEBUG</c>, so it does not exist in a published build.</b> The
/// scope doc is explicit that the control must not merely be hidden by CSS - a button that ships and is
/// invisible is still a button, and this one writes rows.
/// <para>
/// <b>Every scenario sets <see cref="ExpenseFormModel.IsMockData"/> and prefixes its reference with
/// <c>MOCK-</c>.</b> A demonstration submission must never be mistakable for a real claim in the
/// database - somebody will eventually run a report over that table.
/// </para>
/// <para>
/// Each press produces a visibly different submission: a different scenario, different amounts, a
/// different reference and different dates. Two mock submissions side by side have to be tellable apart
/// at a glance, or neither the PDF renderer nor a review flow can be checked against them.
/// </para>
/// <para>
/// <b>This fills the form only. The receipts are uploaded by the page afterwards</b> - see
/// <c>ExpenseForm.FillWithMockData</c> and <see cref="MockReceipt"/> - because an upload needs a
/// submission id and a live HTTP client, neither of which belongs in a pure generator. Without them the
/// result would not be submittable: a purchase with no evidence against it is refused, which is the
/// whole point of section 3 now.
/// </para>
/// <para>
/// <b>Every scenario exercises all three itemisation modes across its purchases</b>, because those three
/// are what the redesign is: a receipt that needs nothing typed out, one that needs only its personal
/// lines, and one that needs the lot. A generator that only ever produced the easy one would leave two
/// thirds of section 3 unseen.
/// </para>
/// </remarks>
public static class MockData
{
    private static readonly Random Rng = new();

    public static void Fill(ExpenseFormModel model)
    {
        if (model.Kind == SubmissionKind.DebitCardPurchase)
            FillDebitCard(model);
        else
            FillReimbursement(model);
    }

    private static void FillDebitCard(ExpenseFormModel model)
    {
        (string submitter, string ministry, string supplier, string narrative, (string item, string details, decimal gross)[] lines) =
            Pick(DebitCardScenarios);

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly transaction = today.AddDays(-Rng.Next(1, 14));

        model.SubmitterName = submitter;
        model.FormDate = today;
        model.Role = Pick(new[] { ClaimantRole.Volunteer, ClaimantRole.Employee, ClaimantRole.Pastor });
        model.MinistryDepartment = ministry;
        model.CardLastFourDigits = Rng.Next(1000, 10000).ToString();
        model.TransactionDate = transaction;
        model.TransactionTime = new TimeOnly(Rng.Next(8, 19), Rng.Next(0, 4) * 15);
        model.SupplierMerchant = supplier;
        model.BankReference = Reference("EFTPOS");
        model.PurposeActivity = ministry;
        model.EventProject = ministry + " — " + Pick(new[] { "term planning", "working bee", "camp prep", "weekly programme" });
        model.PriorApprovalBy = Pick(new[] { "Pr. Daniel Okafor", "Ruth Vasquez", "Pr. Helen Marsh" });
        model.ApprovalDate = transaction.AddDays(-Rng.Next(1, 10));
        model.PurposeNarrative = narrative;

        model.Details = lines
            .Select((l, index) => Detail(l, ministry, supplier, transaction.AddDays(-index), index))
            .ToList();

        decimal gross = model.Details.Sum(x => x.TotalIncGst ?? 0m);

        // Every third scenario is an awkward one, and the awkward paths are the whole reason this exists:
        // a Yes unlocks a reveal, and section 5 opens. A generator that only ever produces tidy data
        // proves nothing.
        //
        // The personal-portion path is NOT part of this any more - it is exercised on every press, by
        // the second purchase, because the three itemisation modes are what section 3 now is.
        bool awkward = Rng.Next(3) == 0;

        if (awkward)
        {
            // Section 5 is opened by the EVIDENCE, not by a flag on the form: a purchase whose only file
            // is a bank line. The page marks the last purchase's upload that way when this is set - see
            // ExpenseForm.FillWithMockData - and the declaration below is what that then requires.
            model.MissingSupplier = supplier;
            model.MissingDate = transaction;
            model.MissingAmount = model.Details[^1].TotalIncGst;
            model.MissingReason =
                "Paper receipt was lost between the shop and the church. I asked the store for a "
                + "reprint on " + today.AddDays(-1).ToString("d MMMM") + " and they could not retrieve it. "
                + "The bank line from the church account is attached instead.";
            model.MissingDeclared = true;

            // Question 2: meals and hospitality, which opens the attendee table.
            model.Compliance[1] = true;
            model.Attendees =
            [
                new AttendeeModel
                {
                    Date = transaction,
                    Person = Pick(new[] { "Sam Whitfield", "Amara Nwosu", "Josh Bentley" }),
                    Relationship = "Volunteer leader",
                    Amount = 42.00m,
                    PrivateShare = 0m,
                    Reason = "Planning meal before the term's programme"
                }
            ];
            model.ComplianceDetails =
                "Meal was for volunteer leaders only. No family members attended and no private share applies.";
        }

        // Every question answered, because "not answered" is a state a reviewer must be able to see and a
        // half-answered mock would hide it behind noise.
        for (int i = 0; i < model.Compliance.Length; i++)
            model.Compliance[i] ??= false;

        // The reconciliation the debit card form exists for. Set to the receipts' total so a mock
        // submission passes it - the mismatch path is worth seeing, but it is one keystroke away in the
        // amount-charged box and does not need a generator to reach.
        model.AmountCharged = Round(gross);

        for (int i = 0; i < model.Declarations.Length; i++)
            model.Declarations[i] = true;

        model.SignatureName = submitter;
        model.IsMockData = true;
    }

    private static void FillReimbursement(ExpenseFormModel model)
    {
        (string submitter, string ministry, string supplier, string narrative, (string item, string details, decimal gross)[] lines) =
            Pick(ReimbursementScenarios);

        DateOnly today = DateOnly.FromDateTime(DateTime.Now);
        DateOnly from = today.AddDays(-Rng.Next(14, 40));

        model.SubmitterName = submitter;
        model.FormDate = today;
        model.Role = Pick(new[] { ClaimantRole.Volunteer, ClaimantRole.Employee, ClaimantRole.ResponsiblePerson });
        model.MinistryDepartment = ministry;
        model.ContactPhoneEmail = $"04{Rng.Next(10, 100)} {Rng.Next(100, 1000)} {Rng.Next(100, 1000)}";
        model.ExpensePeriodFrom = from;
        model.ExpensePeriodTo = today;
        model.PaymentMethod = PaymentMethod.Eft;
        model.BankDetailsOnFile = true;
        model.PurposeActivity = narrative.Split('.')[0];
        model.EventProject = ministry;
        model.PriorApprovalBy = Pick(new[] { "Pr. Daniel Okafor", "Ruth Vasquez" });
        model.ApprovalDate = from;
        model.PurposeNarrative = narrative;

        model.Details = lines
            .Select((l, index) => Detail(l, ministry, supplier, from.AddDays(index * 3), index))
            .ToList();

        bool awkward = Rng.Next(3) == 0;

        if (awkward)
        {
            // Question 1 on this form is the motor vehicle one, which opens the trip record - a
            // genuinely different table from the debit card form's.
            model.Compliance[0] = true;
            model.Trips =
            [
                new TripModel
                {
                    Date = from.AddDays(2),
                    From = "Church office",
                    To = Pick(new[] { "Mount Tamborine camp site", "Springwood distribution centre", "Logan hospital" }),
                    BusinessKm = Rng.Next(24, 140),
                    ApprovedRate = 0.880m,
                    Purpose = ministry + " visit"
                }
            ];
            model.ComplianceDetails = "Return trip in my own vehicle. Fuel is not claimed separately.";

            // Section 5 opens off the evidence - the page marks the last purchase's file as a bank line
            // when this is set. See the debit card branch, where the same thing is explained at length.
            model.MissingSupplier = supplier;
            model.MissingDate = from;
            model.MissingAmount = model.Details[^1].TotalIncGst;
            model.MissingReason =
                "The market stall gives no receipt. The bank transaction from my own account is attached "
                + "instead, showing the amount and the payee.";
            model.MissingDeclared = true;
        }

        for (int i = 0; i < model.Compliance.Length; i++)
            model.Compliance[i] ??= false;

        for (int i = 0; i < model.Declarations.Length; i++)
            model.Declarations[i] = true;

        model.SignatureName = submitter;
        model.IsMockData = true;

        // Not a field on this form, but the reference has to be recognisable as mock data somewhere the
        // reviewer will see it.
        model.ComplianceDetails = (model.ComplianceDetails is null ? "" : model.ComplianceDetails + " ")
                                  + Reference("MOCK");
    }

    /// <summary>
    /// One purchase, in whichever itemisation mode its position calls for.
    /// </summary>
    /// <remarks>
    /// <b>The mode is chosen by <paramref name="index"/> rather than at random, so every press exercises
    /// all three.</b> They are what the redesign of section 3 is, and two of them - the mixed itemisation
    /// and the personal-items floor under the non-reimbursed field - are the parts with arithmetic in
    /// them and therefore the parts worth seeing on screen. A generator that rolled a die would show a
    /// tidy all-church form one press in three and prove nothing about the other two.
    /// <list type="number">
    /// <item>Nothing personal, receipt itemises: a total and GST, no lines typed out.</item>
    /// <item>Personal items on an itemised receipt: the personal lines only, and the non-reimbursed
    /// amount floored on them.</item>
    /// <item>Unitemised evidence: everything listed, each line marked Church use or not.</item>
    /// </list>
    /// </remarks>
    private static ExpenseDetailModel Detail(
        (string Item, string Details, decimal Gross) line,
        string ministry,
        string scenarioSupplier,
        DateOnly date,
        int index
    )
    {
        ExpenseDetailModel detail = new()
        {
            // WHERE IT WAS BOUGHT, not what was bought - the two scenario shapes name the shop in
            // different places and it is easy to take the wrong one. The card scenarios carry one shop
            // for the whole transaction ("Bunnings Warehouse Springwood") and put the product in
            // line.Item; the reimbursement scenarios say "Various" and name the shop at the head of
            // line.Details ("Woolworths - meal for the Ferreira family").
            //
            // Getting this wrong put "Pine shelving 2400mm" in the supplier box, which looked plausible
            // enough on screen to survive one review.
            Supplier = scenarioSupplier is "Various" or ""
                ? SupplierOf(line.Details)
                : scenarioSupplier,
            PurchaseDate = date,
            Purpose = ministry,
            TotalIncGst = line.Gross,
            // Australian GST is one eleventh of a GST-inclusive price.
            GstAmount = Round(line.Gross / 11m),
            NonReimbursedAmount = 0m
        };

        switch (index % 3)
        {
            case 0:
                detail.ContainsPersonalItems = false;
                detail.ReceiptIsItemised = true;
                break;

            case 1:
                detail.ContainsPersonalItems = true;
                detail.ReceiptIsItemised = true;
                detail.Items =
                [
                    new ExpenseDetailItemModel
                    {
                        Description = Pick(new[] { "Milk and bread for home", "Birthday card", "Phone charger" }),
                        Amount = Round(line.Gross * 0.12m)
                    }
                ];
                break;

            default:
                detail.ContainsPersonalItems = true;
                detail.ReceiptIsItemised = false;
                detail.Items =
                [
                    new ExpenseDetailItemModel
                    {
                        Description = line.Details,
                        Amount = Round(line.Gross * 0.6m),
                        IsChurchUse = true
                    },
                    new ExpenseDetailItemModel
                    {
                        Description = "Second half of the same order",
                        Amount = Round(line.Gross * 0.25m),
                        IsChurchUse = true
                    },
                    new ExpenseDetailItemModel
                    {
                        Description = "Personal - picked up at the same time",
                        Amount = Round(line.Gross * 0.15m)
                    }
                ];
                break;
        }

        // The same call the page makes after any item edit: raise the non-reimbursed amount to what was
        // itemised as personal. Doing it here rather than assigning a figure means the mock data obeys
        // the rule rather than merely happening to satisfy it.
        detail.ClampNonReimbursed();

        return detail;
    }

    /// <summary>
    /// The shop out of a scenario's details string, which reads "Woolworths — meal for the Ferreira
    /// family" on the reimbursement scenarios. Falls back to the whole string where there is no dash.
    /// </summary>
    private static string SupplierOf(string details)
    {
        int dash = details.IndexOf('—');

        return dash > 0 ? details[..dash].Trim() : details;
    }

    /// <summary>
    /// Every reference starts <c>MOCK-</c>, so a demonstration row is never mistaken for a real claim.
    /// </summary>
    private static string Reference(string prefix) =>
        $"MOCK-{prefix}-{Rng.Next(1000, 10000)}";

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);

    private static T Pick<T>(IReadOnlyList<T> options) => options[Rng.Next(options.Count)];

    private static readonly (string Submitter, string Ministry, string Supplier, string Narrative,
        (string Item, string Details, decimal Gross)[] Lines)[] DebitCardScenarios =
        [
            ("Marion Ellery", "Youth", "Bunnings Warehouse Springwood",
                "Shelving, hooks and storage tubs to fit out the games shed used by Friday Night Youth "
                + "(about 35 attendees, ages 12-17). Replaces stacked cartons that were a trip hazard and "
                + "lets volunteers set up and pack down safely.",
                [
                    ("Pine shelving 2400mm", "6 lengths", 98.40m),
                    ("Storage tubs 52L", "8 tubs", 71.20m),
                    ("Brackets and screws", "Assorted", 34.95m)
                ]),

            ("Tobias Chen", "Men's ministry", "Coles Underwood",
                "Catering for the men's breakfast on the first Saturday, 48 attending. Bought the "
                + "morning of the event so nothing needed refrigerating overnight at the church.",
                [
                    ("Bacon and eggs", "Bulk", 112.60m),
                    ("Bread, spreads, fruit", "For 48", 68.35m),
                    ("Coffee and tea", "Refill for the urn", 41.90m)
                ]),

            ("Priya Raman", "Kids ministry", "Officeworks Browns Plains",
                "Craft and printing supplies for the school holiday programme, running four mornings "
                + "with about 60 children. Includes the laminating pouches for the new sign-in cards.",
                [
                    ("Cardstock and paper", "5 reams", 62.00m),
                    ("Laminating pouches", "Box of 100", 38.50m),
                    ("Markers and glue", "Class set", 55.75m)
                ]),

            ("Daniel Okafor", "Camps", "Ampol Beenleigh",
                "Fuel and tolls for the bus run to the youth camp at Mount Tamborine, carrying 22 young "
                + "people and 5 leaders. Two return trips because of vehicle capacity.",
                [
                    ("Diesel", "Bus, full tank", 186.40m),
                    ("Tolls", "Return, two trips", 27.60m),
                    ("Parking at camp site", "Two days", 18.00m)
                ])
        ];

    private static readonly (string Submitter, string Ministry, string Supplier, string Narrative,
        (string Item, string Details, decimal Gross)[] Lines)[] ReimbursementScenarios =
        [
            ("Hannah Brightwell", "Pastoral care", "Various",
                "Hospital and home visits across the month, plus a meal delivered to the Ferreira family "
                + "after their father's surgery.",
                [
                    ("", "Woolworths — meal for the Ferreira family", 48.75m),
                    ("", "Chemist Warehouse — first aid restock for the office", 32.10m),
                    ("", "Bakers Delight — morning tea for the visiting team", 24.00m)
                ]),

            ("Samuel Adeyemi", "Worship", "Various",
                "Replacement cables and a music stand light after the Sunday morning set, bought myself "
                + "so the band had them for the following week.",
                [
                    ("", "Mannys — XLR cables x3", 87.00m),
                    ("", "Mannys — music stand light", 44.95m)
                ]),

            ("Grace Whitmore", "Community meals", "Various",
                "Ingredients for the Wednesday community meal across three weeks, serving about 40 "
                + "people each week, bought from the market as it is cheaper than the supermarket.",
                [
                    ("", "Rocklea markets — vegetables, week 1", 96.40m),
                    ("", "Rocklea markets — vegetables, week 2", 88.20m),
                    ("", "Costco — rice, oil, tinned tomatoes", 134.55m)
                ])
        ];
}
#endif
