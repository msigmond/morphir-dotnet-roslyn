using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;

namespace Example.FestivalRobotRental;

// A fictional event company rents singing and moving robots.
// Demo builds five orders. The plan accepts two orders for three robots.
// All business data and rules are in this file. Start with Demo, then Plan.

// 1. Business data.
public enum RentalState { Requested, Confirmed, Cancelled }

public record RentalOrder(
    string OrderId, string RobotModel, int Quantity, int Days,
    decimal DailyRate, decimal? DiscountPercent, RentalState State, char Zone);

public record RentalQuote(
    string OrderId, decimal Rental, decimal Discount, decimal Delivery, int DispatchDays)
{
    public decimal Amount => Rental - Discount + Delivery;
}

public record Review<T>(T Item, bool Accepted);
public record RobotCatalog(HashSet<string> Models, Dictionary<string, decimal> Promotions);
public record BatchPlan(
    Review<RentalOrder>[] Reviews, RentalQuote[] Quotes, ChargeTotal Total,
    int RobotCount, int CartonCount, string? FirstOrder, double CapacityUse, decimal RewardCredit);

public readonly record struct ChargeTotal(decimal Amount)
{
    public static ChargeTotal operator +(ChargeTotal left, ChargeTotal right) =>
        new(left.Amount + right.Amount);
}

// The constructor assigns every stored field. The policy has no hidden defaults.
public class RentalPolicy
{
    public readonly int MinimumQuantity;
    public readonly int Capacity;
    public readonly decimal DeliveryFee;

    public RentalPolicy(int minimumQuantity, int capacity, decimal deliveryFee)
    {
        MinimumQuantity = minimumQuantity;
        Capacity = capacity;
        DeliveryFee = deliveryFee;
    }

    public bool Accepts(RentalOrder order) =>
        order.Quantity >= MinimumQuantity && order.Quantity <= Capacity && order.Days > 0;
}

// Primary parameters become data fields. The method reads those fields.
public class DeliveryRule(int localDays, int remoteDays)
{
    public int Days(char zone) => zone switch { 'L' => localDays, _ => remoteDays };
}

public static class RentalBusiness
{
    // 2. Complete demo: review, filter, quote, fold, and plan dispatch.
    public static BatchPlan Demo()
    {
        var policy = CreatePolicy(deliveryFee: 10m, minimumQuantity: 1);
        var catalog = new RobotCatalog(AllowedModels(), PromotionRates());
        var delivery = new DeliveryRule(1, 3);
        return Plan(DemoOrders(), policy, catalog, delivery);
    }

    public static BatchPlan Plan(
        RentalOrder[] orders, RentalPolicy policy, RobotCatalog catalog, DeliveryRule delivery)
    {
        var reviews = orders.Select(order => ReviewOrder(order, policy, catalog)).ToArray();
        var accepted = reviews.Where(review => review.Accepted).Select(review => review.Item).ToArray();
        var quotes = accepted.Select(QuoteWith(policy, catalog, delivery)).ToArray();
        var total = quotes.Aggregate(new ChargeTotal(0m), (sum, quote) => sum + new ChargeTotal(quote.Amount));
        var robots = reviews.Select(ReviewedQuantity).Aggregate(0, (sum, quantity) => sum + quantity);
        var cartons = CartonsNeeded(robots, 2);
        var utilisation = CapacityUse(robots, policy.Capacity);
        var credit = ProjectCredit(5m, 10m, 2);
        return new BatchPlan(reviews, quotes, total, robots, cartons, FirstOrder(quotes), utilisation, credit);
    }

    // 3. Order review. Plain references remain plain. State and set membership are business rules.
    public static Review<RentalOrder> ReviewOrder(RentalOrder order, RentalPolicy policy, RobotCatalog catalog)
    {
        if (order.State != RentalState.Confirmed) return Assess(order, false);
        var accepted = policy.Accepts(order) && catalog.Models.Contains(order.RobotModel) && order.OrderId.Length > 0;
        return Assess(order, accepted);
    }

    public static Review<T> Assess<T>(T item, bool accepted) => new(item, accepted);
    public static T ReviewedItem<T>(Review<T> review) => review.Item;
    public static RentalOrder Confirm(RentalOrder order) => order with { State = RentalState.Confirmed };
    public static bool SameOrder(RentalOrder left, RentalOrder right) => left == right;

    public static int ReviewedQuantity(Review<RentalOrder> review) =>
        DispatchQuantity((review.Item.Quantity, review.Accepted));

    public static int DispatchQuantity((int Quantity, bool Accepted) decision) => decision switch
    {
        (_, false) => 0,
        (0, _) => 0,
        (var quantity, true) => quantity
    };

    // 4. Pricing. Division returns decimal?. Each fallback is written in the business rule.
    public static RentalQuote QuoteOrder(
        RentalOrder order, RentalPolicy policy, RobotCatalog catalog, DeliveryRule delivery)
    {
        var rental = (decimal)(order.Quantity * order.Days) * order.DailyRate;
        var discountRate = order.DiscountPercent ?? (PromotionFor(catalog.Promotions, order.RobotModel) ?? 0m);
        var discount = Divide(rental * discountRate, 100m) ?? 0m;
        var deliveryFee = policy.DeliveryFee * DeliveryMultiplier(order.Zone);
        return new RentalQuote(order.OrderId, rental, discount, deliveryFee, delivery.Days(order.Zone));
    }

    public static decimal? Divide(decimal amount, decimal divisor) => amount / divisor;
    public static decimal RoundedAmount(RentalQuote quote) => decimal.Round(quote.Amount);
    public static bool HasDiscount(RentalOrder order) => order.DiscountPercent.HasValue;
    public static decimal? PromotionFor(IReadOnlyDictionary<string, decimal> promotions, string model) => promotions[model];

    public static decimal DeliveryMultiplier(char zone) => zone switch
    {
        'L' => 1m,
        'R' => 2m,
        _ => 3m
    };

    // 5. Dispatch. These rules use Int arithmetic, Float casts, tuples, and returned switches.
    public static int CartonsNeeded(int robots, int cartonSize) =>
        robots / cartonSize + (robots % cartonSize == 0 ? 0 : 1);

    public static double CapacityUse(int robots, int capacity) =>
        capacity == 0 ? 0.0 : (double)robots / (double)capacity;

    public static int ServiceDays(string service) => service switch { "rush" => 1, "standard" => 2, _ => 3 };
    public static int StatePriority(RentalState state)
    {
        switch (state) { case RentalState.Confirmed: return 1; default: return 0; }
    }

    public static int CombinedDays((int RentalDays, int ExtraDays) days)
    {
        var (rentalDays, extraDays) = days;
        return rentalDays + extraDays;
    }

    public static Tuple<int, bool> DispatchDecision(RentalOrder order, bool accepted) => Tuple.Create(order.Quantity, accepted);
    public static int CapQuantity(int quantity, int capacity) => Math.Min(quantity, capacity);
    public static long CapQuantity(long quantity, long capacity) => Math.Min(quantity, capacity);
    public static int PlannedQuantity(RentalOrder order, RentalPolicy policy) => CapQuantity(order.Quantity, policy.Capacity);

    // 6. Optional data. Indexing is permitted only with a nullable result.
    public static RentalQuote? QuoteAt(IReadOnlyList<RentalQuote> quotes, int index) => quotes[index];
    public static int OptionalQuantity(RentalOrder? order) => order?.Quantity ?? default(int);
    public static int? UnfilledQuantity(int? requested, int? supplied) => requested - supplied;
    public static bool MissingOrder(RentalOrder? order) => order is null;
    public static RentalOrder? NoOrder() => null;

    public static string? FirstOrder(RentalQuote[] quotes) => quotes switch
    {
        [] => null,
        [var first, ..] => first.OrderId
    };

    // 7. Collections. Reads, copies, and explicit elements have no mutation.
    public static RentalOrder[] AddOrder(RentalOrder[] orders, RentalOrder next) => [.. orders, next];
    public static ImmutableArray<RentalQuote> Snapshot(RentalQuote[] quotes) => ImmutableArray.CreateRange(quotes);
    public static ReadOnlyCollection<RentalQuote> ReadOnlyQuotes(RentalQuote[] quotes) => new(quotes);
    public static RentalOrder[] EmptyOrders() => Array.Empty<RentalOrder>();
    public static int OrderCount(List<RentalOrder> orders) => orders.Count;
    public static bool HasPromotion(RobotCatalog catalog, string model) => catalog.Promotions.ContainsKey(model);
    public static string[] PromotionModels(IReadOnlyDictionary<string, decimal> promotions) => promotions.Keys.ToArray();
    public static decimal[] PromotionPercents(IReadOnlyDictionary<string, decimal> promotions) => promotions.Values.ToArray();

    public static List<decimal> SettlementAmounts(List<RentalQuote> quotes, decimal limit) =>
        quotes.FindAll(quote => quote.Amount <= limit).ConvertAll(RoundedAmount);

    // 8. Functions. These rules use a captured lambda, an anonymous method, a bound method, and a local function.
    public static Func<RentalOrder, RentalQuote> QuoteWith(RentalPolicy policy, RobotCatalog catalog, DeliveryRule delivery) =>
        new Func<RentalOrder, RentalQuote>(order => QuoteOrder(order, policy, catalog, delivery));

    public static Func<RentalQuote, bool> BelowLimit(decimal limit) =>
        delegate(RentalQuote quote) { return quote.Amount <= limit; };

    public static Func<RentalOrder, bool> PolicyCheck(RentalPolicy policy) => policy.Accepts;
    public static bool CheckOrder(Func<RentalOrder, bool> check, RentalOrder order) => check.Invoke(order);

    public static decimal ProjectCredit(decimal startingCredit, decimal dailyBonusPercent, int days)
    {
        var bonusRate = Divide(dailyBonusPercent, 100m) ?? 0m;
        decimal Grow(int remaining, decimal credit) => remaining <= 0
            ? credit
            : Grow(remaining - 1, credit * (1m + bonusRate));
        return Grow(days, startingCredit);
    }

    // 9. Business inputs. Named arguments, defaults, params, constants, and record construction stay visible.
    public static RentalPolicy CreatePolicy(int minimumQuantity = 1, int capacity = 4, decimal deliveryFee = 10m) =>
        new(minimumQuantity, capacity, deliveryFee);

    public static int CombineQuantities(params int[] quantities) => quantities.Aggregate(0, (sum, quantity) => sum + quantity);
    public static HashSet<string> AllowedModels() => ["SING", "MOVE"];
    public static Dictionary<string, decimal> PromotionRates() => new() { ["SING"] = 5m, ["MOVE"] = 0m };

    public static RentalOrder[] DemoOrders() =>
    [
        new("ORD-001", "SING", CombineQuantities(1, 1), CombinedDays((1, 2)), 50m, 10m, RentalState.Confirmed, 'L'),
        new("ORD-002", "MOVE", 1, 2, 80m, null, RentalState.Confirmed, 'R'),
        new("ORD-003", "SING", 4, 1, 50m, null, RentalState.Cancelled, 'L'),
        new("ORD-004", "FLY", 1, 1, 50m, null, RentalState.Confirmed, 'R'),
        new("ORD-005", "MOVE", 0, 1, 80m, null, RentalState.Confirmed, 'L')
    ];
}
