namespace Nightcrew.Core;

public interface IGpuUsage
{
    /// <summary>Process id to the utilization percent of its busiest GPU engine. Empty when unavailable.</summary>
    IReadOnlyDictionary<int, double> PercentByPid();
}

/// <summary>
/// Busy while an agent, or a process an agent started, uses the GPU at or above the threshold. A game or a video export
/// outside the agents doesn't count. <paramref name="agentPids"/> gives the agent tree of the current tick (the agent-tree
/// signal is sampled first); <paramref name="nameOf"/> turns a pid into a process name for the panel, null showing "pid N".
/// </summary>
public sealed class GpuSignal(IGpuUsage gpu, double thresholdPercent, Func<IReadOnlyCollection<int>> agentPids, Func<int, string?>? nameOf = null)
    : IBusySignal, IHolderSource
{
    public string Name => "gpu";

    /// <summary>The busiest agent pid of the last busy sample; kept after the GPU goes quiet.</summary>
    public int? LastBusyPid { get; private set; }

    /// <summary>Every agent process at or above the threshold in the last busy sample, busiest first.</summary>
    public IReadOnlyList<Holder> Holders { get; private set; } = [];

    /// <summary>
    /// Every process at or above the threshold in the last sample that had one, agent or not, busiest first, so
    /// Efficiency mode leaves GPU-busy apps such as games alone.
    /// </summary>
    public IReadOnlyList<int> BusyPids { get; private set; } = [];

    public bool Sample(TimeSpan now)
    {
        var busy = gpu.PercentByPid()
            .Where(pair => pair.Value >= thresholdPercent)
            .OrderByDescending(pair => pair.Value)
            .ToList();
        if (busy.Count == 0) return false;
        BusyPids = busy.Select(pair => pair.Key).ToList();

        var agents = agentPids();
        var agentBusy = busy.Where(pair => agents.Contains(pair.Key)).ToList();
        if (agentBusy.Count == 0) return false;
        LastBusyPid = agentBusy[0].Key;
        Holders = agentBusy.Select(pair => new Holder(nameOf?.Invoke(pair.Key) ?? $"pid {pair.Key}", $"GPU {Math.Round(pair.Value):0}%")).ToList();
        return true;
    }
}
