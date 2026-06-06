namespace ClaimsModule.Domain.Common;

/// <summary>
/// Generates GUIDs that are sequential in SQL Server's <c>uniqueidentifier</c> sort order (a "COMB"
/// GUID). Aggregates need their identity before <c>SaveChanges</c> — child FKs and the reserve
/// idempotency key reference it inside the same transaction — so identity is assigned client-side;
/// using sequential values keeps the FRS §15.1 intent (avoid clustered-index fragmentation) that a
/// random <see cref="Guid.NewGuid"/> would defeat. Every GUID PK is mapped <c>ValueGeneratedNever()</c>;
/// there is no <c>NEWSEQUENTIALID()</c> store default (see ARCHITECTURE.md §10).
/// </summary>
public static class SequentialGuid
{
    private static readonly DateTime BaseDate = new(1900, 1, 1);

    public static Guid Create()
    {
        var guidBytes = Guid.NewGuid().ToByteArray();
        var now = DateTime.UtcNow;

        // SQL Server orders uniqueidentifier by the last 6 bytes — encode time there.
        var days = (short)new TimeSpan(now.Ticks - BaseDate.Ticks).Days;
        var msecInDay = (long)(now.TimeOfDay.TotalMilliseconds / 3.333333); // SQL's 1/300s ticks

        var daysBytes = BitConverter.GetBytes(days);
        var msecBytes = BitConverter.GetBytes(msecInDay);
        Array.Reverse(daysBytes);
        Array.Reverse(msecBytes);

        Array.Copy(daysBytes, daysBytes.Length - 2, guidBytes, guidBytes.Length - 6, 2);
        Array.Copy(msecBytes, msecBytes.Length - 4, guidBytes, guidBytes.Length - 4, 4);

        return new Guid(guidBytes);
    }
}
