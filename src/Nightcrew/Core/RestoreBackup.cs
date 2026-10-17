using System.Globalization;

namespace Nightcrew.Core;

/// <summary>
/// A second copy of the restore record, kept apart from restore.json, so a lost or damaged file can't lose the user's
/// own power values. <see cref="Save"/> may throw; <see cref="Load"/> gives null when there is no copy; <see cref="Clear"/>
/// never throws.
/// </summary>
public interface IRestoreBackup
{
    void Save(RestoreRecord record);
    RestoreRecord? Load();
    void Clear();
}

/// <summary>A restore record as named registry values: the eight numbers as DWORDs, the plan and the time as strings.</summary>
public static class RestoreBackupFormat
{
    private static readonly string[] Numbers =
    [
        nameof(RestoreRecord.OwnerLidAc), nameof(RestoreRecord.OwnerLidDc), nameof(RestoreRecord.OwnerStandbyAc), nameof(RestoreRecord.OwnerStandbyDc),
        nameof(RestoreRecord.WrittenLidAc), nameof(RestoreRecord.WrittenLidDc), nameof(RestoreRecord.WrittenStandbyAc), nameof(RestoreRecord.WrittenStandbyDc),
    ];

    public static IReadOnlyDictionary<string, object> ToValues(RestoreRecord record)
    {
        uint[] numbers =
        [
            record.OwnerLidAc, record.OwnerLidDc, record.OwnerStandbyAc, record.OwnerStandbyDc,
            record.WrittenLidAc, record.WrittenLidDc, record.WrittenStandbyAc, record.WrittenStandbyDc,
        ];
        var values = new Dictionary<string, object>();
        for (var i = 0; i < Numbers.Length; i++) values[Numbers[i]] = unchecked((int)numbers[i]); // a DWORD is read back as int
        if (record.Scheme is not null) values[nameof(RestoreRecord.Scheme)] = record.Scheme;
        values[nameof(RestoreRecord.CreatedUtc)] = record.CreatedUtc.ToString("o", CultureInfo.InvariantCulture);
        return values;
    }

    /// <summary>Null when any of the eight numbers is missing or not an int; a missing or bad time gives default.</summary>
    public static RestoreRecord? FromValues(Func<string, object?> get)
    {
        var numbers = new uint[Numbers.Length];
        for (var i = 0; i < Numbers.Length; i++)
        {
            if (get(Numbers[i]) is not int value) return null;
            numbers[i] = unchecked((uint)value);
        }
        var created = get(nameof(RestoreRecord.CreatedUtc)) is string text
            && DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed) ? parsed : default;
        return new RestoreRecord
        {
            Owner = new PowerValues(numbers[0], numbers[1], numbers[2], numbers[3]),
            Written = new PowerValues(numbers[4], numbers[5], numbers[6], numbers[7]),
            Scheme = get(nameof(RestoreRecord.Scheme)) as string,
            CreatedUtc = created,
        };
    }
}
