using Nightcrew.Core;
using Xunit;

namespace Nightcrew.Tests;

/// <summary>The journal with a second copy of its record (the registry in the app; in memory here).</summary>
public sealed class RestoreJournalBackupTests : IDisposable
{
    private static readonly PowerValues Owner = new(1, 1, 900, 600);

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "nightcrew-tests-" + Guid.NewGuid().ToString("N"));
    private readonly FakePolicy _policy = new(Owner);
    private readonly FakeBackup _backup = new();
    private readonly WarnLog _log = new();

    private string RecordPath => Path.Combine(_dir, "restore.json");

    private RestoreJournal NewJournal() => new(_policy, RecordPath, _log, backup: _backup);

    public RestoreJournalBackupTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Arm_saves_a_registry_copy()
    {
        NewJournal().Arm();

        Assert.Equal(Owner, _backup.Copy!.Owner);
        Assert.Equal(RestoreJournal.ArmValuesFor(8), _backup.Copy.Written);
    }

    [Fact]
    public void Restore_clears_the_copy()
    {
        NewJournal().Arm();

        Assert.True(NewJournal().Restore());

        Assert.Null(_backup.Copy);
        Assert.Equal(Owner, _policy.Values);
    }

    [Fact]
    public void Lost_file_restores_the_user_values_from_the_copy()
    {
        NewJournal().Arm();
        File.Delete(RecordPath);

        Assert.True(NewJournal().Restore());

        Assert.Equal(Owner, _policy.Values);
        Assert.Null(_backup.Copy);
    }

    [Fact]
    public void Corrupt_file_restores_the_user_values_from_the_copy()
    {
        NewJournal().Arm();
        File.WriteAllText(RecordPath, "\0\0\0");

        Assert.True(NewJournal().Restore());

        Assert.Equal(Owner, _policy.Values);
    }

    [Fact]
    public void Arming_again_after_a_lost_file_keeps_the_user_values()
    {
        NewJournal().Arm();
        File.Delete(RecordPath);

        var journal = NewJournal();
        journal.Arm();

        Assert.Equal(Owner, journal.OwnerValues);
        Assert.True(File.Exists(RecordPath));
    }

    [Fact]
    public void A_stale_copy_is_ignored_when_the_settings_are_the_users()
    {
        var users = new PowerValues(1, 0, 1200, 600);
        _backup.Copy = new RestoreRecord { Owner = new PowerValues(2, 2, 300, 300), Written = RestoreJournal.ArmValuesFor(8), Scheme = _policy.Active.ToString("D") };
        _policy.Values = users;

        var journal = NewJournal();
        journal.Arm();

        Assert.Equal(users, journal.OwnerValues);
        Assert.Equal(users, _backup.Copy!.Owner);
    }

    [Fact]
    public void A_readable_file_wins_over_a_different_copy()
    {
        NewJournal().Arm();
        _backup.Copy = new RestoreRecord { Owner = new PowerValues(2, 2, 300, 300), Written = RestoreJournal.ArmValuesFor(8), Scheme = _policy.Active.ToString("D") };

        Assert.True(NewJournal().Restore());

        Assert.Equal(Owner, _policy.Values);
        Assert.Null(_backup.Copy);
    }

    [Fact]
    public void Lost_file_restores_the_copy_to_the_plan_it_names()
    {
        var first = _policy.Active;
        NewJournal().Arm();
        var otherValues = new PowerValues(1, 1, 600, 300);
        var other = _policy.AddScheme(otherValues);
        _policy.Activate(other);
        File.Delete(RecordPath);

        Assert.True(NewJournal().Restore());

        Assert.Equal(Owner, _policy.ValuesOf(first));
        Assert.Equal(otherValues, _policy.ValuesOf(other));
    }

    [Fact]
    public void HasRecord_is_true_with_only_the_copy()
    {
        NewJournal().Arm();
        File.Delete(RecordPath);

        Assert.True(NewJournal().HasRecord);
    }

    [Fact]
    public void A_copy_that_cannot_be_read_counts_as_none()
    {
        NewJournal().Arm();
        File.WriteAllText(RecordPath, "\0\0\0");
        _backup.FailLoad = true;
        var journal = NewJournal();

        Assert.True(journal.Restore()); // corrupt record and no usable copy: Windows' defaults over our values, as before

        Assert.Equal(_policy.Defaults, _policy.Values);
        Assert.False(journal.HasRecord);
    }

    [Fact]
    public void A_copy_that_fails_to_save_does_not_stop_arming()
    {
        _backup.FailSave = true;

        NewJournal().Arm();

        Assert.Equal(RestoreJournal.ArmValuesFor(8), _policy.Values);
        Assert.Contains(_log.Warnings, warning => warning.Contains("registry"));
    }

    private sealed class FakeBackup : IRestoreBackup
    {
        public RestoreRecord? Copy { get; set; }
        public bool FailSave { get; set; }
        public bool FailLoad { get; set; }

        public void Save(RestoreRecord record)
        {
            if (FailSave) throw new UnauthorizedAccessException("denied");
            Copy = Clone(record);
        }

        public RestoreRecord? Load() => FailLoad ? throw new UnauthorizedAccessException("denied") : Copy is null ? null : Clone(Copy);

        public void Clear() => Copy = null;

        private static RestoreRecord Clone(RestoreRecord record) =>
            new() { Owner = record.Owner, Written = record.Written, Scheme = record.Scheme, CreatedUtc = record.CreatedUtc };
    }

    private sealed class WarnLog : ILog
    {
        public List<string> Warnings { get; } = [];
        public void Info(string message) { }
        public void Warn(string message) => Warnings.Add(message);
    }
}
