namespace CourierKata;

/// <summary>The priced contents of a cart: one line item per parcel and the total cost in dollars.</summary>
public sealed record Quote(IReadOnlyList<LineItem> Items, decimal Total);
