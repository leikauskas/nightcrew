using Nightcrew.Core;
using Xunit;

namespace Nightcrew.Tests;

public sealed class RestoreBackupFormatTests
{
    private static RestoreRecord Sample() => new()
    {
        Owner = new PowerValues(1, 1, 900, 600),
        Written = RestoreJournal.ArmValuesFor(8),
        Scheme = Guid.NewGuid().ToString("D"),
        CreatedUtc = new DateTime(2026, 10, 17, 18, 0, 0, DateTimeKind.Utc),
    };

    private static Dictionary<string, object> ValuesOf(RestoreRecord record) =>
        RestoreBackupFormat.ToValues(record).ToDictionary(pair => pair.Key, pair => pair.Value);

    private static RestoreRecord? Read(Dictionary<string, object> values) =>
        RestoreBackupFormat.FromValues(name => values.TryGetValue(name, out var value) ? value : null);

    [Fact]
    public void Round_trip_keeps_every_field()
    {
        var record = Sample();
        var back = Read(ValuesOf(record));
        Assert.NotNull(back);
        Assert.Equal(record.Owner, back!.Owner);
        Assert.Equal(record.Written, back.Written);
        Assert.Equal(record.Scheme, back.Scheme);
        Assert.Equal(record.CreatedUtc, back.CreatedUtc);
    }

    [Fact]
    public void A_missing_number_gives_null()
    {
        var values = ValuesOf(Sample());
        values.Remove("OwnerStandbyDc");
        Assert.Null(Read(values));
    }

    [Fact]
    public void A_number_of_the_wrong_type_gives_null()
    {
        var values = ValuesOf(Sample());
        values["OwnerLidAc"] = "1";
        Assert.Null(Read(values));
    }

    [Fact]
    public void A_large_timer_survives_the_int_storage()
    {
        var record = Sample();
        record.Written = new PowerValues(0, 0, 0, 48 * 3600 + 3600 + 7);
        Assert.Equal(record.Written, Read(ValuesOf(record))!.Written);
    }

    [Fact]
    public void A_record_without_a_plan_round_trips_without_one()
    {
        var record = Sample();
        record.Scheme = null;
        var values = ValuesOf(record);
        Assert.False(values.ContainsKey("Scheme"));
        Assert.Null(Read(values)!.Scheme);
    }
}
