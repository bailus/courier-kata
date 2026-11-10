namespace CourierKata;

/// <summary>
/// One line of a quote and its cost in dollars. The kinds of line item are fixed:
/// <see cref="ParcelLineItem"/> and <see cref="SpeedyShippingLineItem"/>.
/// </summary>
public abstract record LineItem
{
    private protected LineItem(decimal cost) => Cost = cost;

    /// <summary>Cost in dollars.</summary>
    public decimal Cost { get; }

    // Records always have a protected copy constructor, so another assembly
    // could still derive from LineItem through it. An internal abstract member
    // can only be implemented inside this assembly, which keeps the set of
    // line-item kinds closed.
    internal abstract void SealHierarchy();
}
