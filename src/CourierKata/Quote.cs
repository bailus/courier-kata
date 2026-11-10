namespace CourierKata;

/// <summary>The priced contents of a cart.</summary>
public sealed record Quote(IReadOnlyList<LineItem> Items);
