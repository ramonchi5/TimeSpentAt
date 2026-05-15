using System;

using LiveSplit.Model;
using LiveSplit.UI.Components;

using TimeSpentAt.UI.Components;

[assembly: ComponentFactory(typeof(TimeSpentAtFactory))]

namespace TimeSpentAt.UI.Components;

public class TimeSpentAtFactory : IComponentFactory
{
    public string ComponentName => "Time Spent At";

    public string Description => "Sums the time spent in segments whose names match configured search text.";

    public ComponentCategory Category => ComponentCategory.Information;

    public IComponent Create(LiveSplitState state)
    {
        return new TimeSpentAt(state);
    }

    public string UpdateName => ComponentName;

    public string XMLURL => "";

    public string UpdateURL => "";

    public Version Version => Version.Parse("1.0.0");
}
