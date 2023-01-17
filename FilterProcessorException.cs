using System;
using FilterCore.Entry;
using FilterCore.Line;

namespace PickIt;

public class FilterProcessorException : Exception
{
    public IFilterEntry Entry { get; }
    public IFilterLine Line { get; }
    public CustomItem Item { get; }

    public FilterProcessorException(IFilterEntry entry, CustomItem item, string message) : base(message)
    {
        Entry = entry;
        Item = item;
    }

    public FilterProcessorException(IFilterLine line, CustomItem item, string message) : base(message)
    {
        Line = line;
        Item = item;
    }

    public FilterProcessorException(CustomItem item, string message) : base(message)
    {
        Item = item;
    }
}