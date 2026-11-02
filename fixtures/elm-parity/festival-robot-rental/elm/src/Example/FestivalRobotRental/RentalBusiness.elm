module Example.FestivalRobotRental.RentalBusiness exposing (..)
import Dict exposing (Dict)
import Set exposing (Set)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
import Example.FestivalRobotRental exposing (RentalOrder, RentalQuote, RentalState, Review, RobotCatalog, BatchPlan, ChargeTotal, RentalPolicy, DeliveryRule)
import Example.FestivalRobotRental.RentalState as State
import Example.FestivalRobotRental.RentalPolicy as Policy
import Example.FestivalRobotRental.DeliveryRule as Delivery
import Example.FestivalRobotRental.RentalQuote as Quote
import Example.FestivalRobotRental.ChargeTotal as Total

demo : BatchPlan
demo =
    let
        policy : RentalPolicy
        policy = createPolicy 1 4 (Decimal.fromFloat 10.0)
        catalog : RobotCatalog
        catalog = RobotCatalog allowedModels promotionRates
        delivery : DeliveryRule
        delivery = Delivery.new0NoneIntNoneInt 1 3
    in
    plan demoOrders policy catalog delivery

plan : List RentalOrder -> RentalPolicy -> RobotCatalog -> DeliveryRule -> BatchPlan
plan orders policy catalog delivery =
    let
        reviews : List (Review RentalOrder)
        reviews = List.map (\order -> reviewOrder order policy catalog) orders
        accepted : List RentalOrder
        accepted = List.map (\review -> review.item) (List.filter (\review -> review.accepted) reviews)
        quotes : List RentalQuote
        quotes = List.map (quoteWith policy catalog delivery) accepted
        total : ChargeTotal
        total = List.foldl (\item state -> (\sum quote -> Total.opAddition0NoneGlobalExampleFestivalRobotRentalChargeTotalNoneGlobalExampleFestivalRobotRentalChargeTotal sum (ChargeTotal (Quote.getAmount0 quote))) state item) (ChargeTotal (Decimal.fromFloat 0.0)) quotes
        robots : Int
        robots = List.foldl (\item state -> (\sum quantity -> sum + quantity) state item) 0 (List.map reviewedQuantity reviews)
        cartons : Int
        cartons = cartonsNeeded robots 2
        utilisation : Float
        utilisation = capacityUse robots policy.capacity
        credit : Decimal
        credit = projectCredit (Decimal.fromFloat 5.0) (Decimal.fromFloat 10.0) 2
    in
    BatchPlan reviews quotes total robots cartons (firstOrder quotes) utilisation credit

reviewOrder : RentalOrder -> RentalPolicy -> RobotCatalog -> Review RentalOrder
reviewOrder order policy catalog =
    if order.state /= State.confirmed then
        assess order False
    else
        let
            accepted : Bool
            accepted = (Policy.accepts policy order && Set.member order.robotModel catalog.models) && String.length order.orderId > 0
        in
        assess order accepted

assess : t -> Bool -> Review t
assess item accepted = Review item accepted
reviewedItem : Review t -> t
reviewedItem review = review.item
confirm : RentalOrder -> RentalOrder
confirm order = { order | state = State.confirmed }
sameOrder : RentalOrder -> RentalOrder -> Bool
sameOrder left right = left == right
reviewedQuantity : Review RentalOrder -> Int
reviewedQuantity review = dispatchQuantity (review.item.quantity, review.accepted)
dispatchQuantity : (Int, Bool) -> Int
dispatchQuantity decision = case decision of
    (_, False) -> 0
    (0, _) -> 0
    (quantity, True) -> quantity

quoteOrder : RentalOrder -> RentalPolicy -> RobotCatalog -> DeliveryRule -> RentalQuote
quoteOrder order policy catalog delivery =
    let
        rental : Decimal
        rental = Decimal.mul (Decimal.fromInt (order.quantity * order.days)) order.dailyRate
        discountRate : Decimal
        discountRate = Maybe.withDefault (Maybe.withDefault (Decimal.fromFloat 0.0) (promotionFor catalog.promotions order.robotModel)) order.discountPercent
        discount : Decimal
        discount = Maybe.withDefault (Decimal.fromFloat 0.0) (divide (Decimal.mul rental discountRate) (Decimal.fromFloat 100.0))
        deliveryFee : Decimal
        deliveryFee = Decimal.mul policy.deliveryFee (deliveryMultiplier order.zone)
    in
    RentalQuote order.orderId rental discount deliveryFee (Delivery.days delivery order.zone)

divide : Decimal -> Decimal -> Maybe Decimal
divide amount divisor = Decimal.div amount divisor
roundedAmount : RentalQuote -> Decimal
roundedAmount quote = Decimal.round (Quote.getAmount0 quote)
hasDiscount : RentalOrder -> Bool
hasDiscount order = order.discountPercent /= Nothing
promotionFor : Dict String Decimal -> String -> Maybe Decimal
promotionFor promotions model = Dict.get model promotions
deliveryMultiplier : Char -> Decimal
deliveryMultiplier zone = case zone of
    'L' -> Decimal.fromFloat 1.0
    'R' -> Decimal.fromFloat 2.0
    _ -> Decimal.fromFloat 3.0

cartonsNeeded : Int -> Int -> Int
cartonsNeeded robots cartonSize = robots // cartonSize + (if remainderBy cartonSize robots == 0 then 0 else 1)
capacityUse : Int -> Int -> Float
capacityUse robots capacity = if capacity == 0 then 0.0 else toFloat robots / toFloat capacity
serviceDays : String -> Int
serviceDays service = case service of
    "rush" -> 1
    "standard" -> 2
    _ -> 3
statePriority : RentalState -> Int
statePriority state = case state of
    1 -> 1
    _ -> 0
combinedDays : (Int, Int) -> Int
combinedDays days =
    let
        (rentalDays, extraDays) = days
    in
    rentalDays + extraDays
dispatchDecision : RentalOrder -> Bool -> (Int, Bool)
dispatchDecision order accepted = (order.quantity, accepted)
capQuantity0NoneIntNoneInt : Int -> Int -> Int
capQuantity0NoneIntNoneInt quantity capacity = min quantity capacity
capQuantity0NoneLongNoneLong : Int -> Int -> Int
capQuantity0NoneLongNoneLong quantity capacity = min quantity capacity
plannedQuantity : RentalOrder -> RentalPolicy -> Int
plannedQuantity order policy = capQuantity0NoneIntNoneInt order.quantity policy.capacity

quoteAt : List RentalQuote -> Int -> Maybe RentalQuote
quoteAt quotes index = List.head (List.drop index quotes)
optionalQuantity : Maybe RentalOrder -> Int
optionalQuantity order = Maybe.withDefault 0 (Maybe.map (\item -> item.quantity) order)
unfilledQuantity : Maybe Int -> Maybe Int -> Maybe Int
unfilledQuantity requested supplied = Maybe.map2 (-) requested supplied
missingOrder : Maybe RentalOrder -> Bool
missingOrder order = order == Nothing
noOrder : Maybe RentalOrder
noOrder = Nothing
firstOrder : List RentalQuote -> Maybe String
firstOrder quotes = case quotes of
    [] -> Nothing
    first :: _ -> Just first.orderId

addOrder : List RentalOrder -> RentalOrder -> List RentalOrder
addOrder orders next = List.concat [orders, [next]]
snapshot : List RentalQuote -> List RentalQuote
snapshot quotes = quotes
readOnlyQuotes : List RentalQuote -> List RentalQuote
readOnlyQuotes quotes = quotes
emptyOrders : List RentalOrder
emptyOrders = []
orderCount : List RentalOrder -> Int
orderCount orders = List.length orders
hasPromotion : RobotCatalog -> String -> Bool
hasPromotion catalog model = Dict.member model catalog.promotions
promotionModels : Dict String Decimal -> List String
promotionModels promotions = Dict.keys promotions
promotionPercents : Dict String Decimal -> List Decimal
promotionPercents promotions = Dict.values promotions
settlementAmounts : List RentalQuote -> Decimal -> List Decimal
settlementAmounts quotes limit = List.map roundedAmount (List.filter (\quote -> Decimal.lte (Quote.getAmount0 quote) limit) quotes)

quoteWith : RentalPolicy -> RobotCatalog -> DeliveryRule -> RentalOrder -> RentalQuote
quoteWith policy catalog delivery order = quoteOrder order policy catalog delivery
belowLimit : Decimal -> RentalQuote -> Bool
belowLimit limit quote = Decimal.lte (Quote.getAmount0 quote) limit
policyCheck : RentalPolicy -> (RentalOrder -> Bool)
policyCheck policy = Policy.accepts policy
checkOrder : (RentalOrder -> Bool) -> RentalOrder -> Bool
checkOrder check order = check order
projectCredit : Decimal -> Decimal -> Int -> Decimal
projectCredit startingCredit dailyBonusPercent days =
    let
        bonusRate : Decimal
        bonusRate = Maybe.withDefault (Decimal.fromFloat 0.0) (divide dailyBonusPercent (Decimal.fromFloat 100.0))
        grow : Int -> Decimal -> Decimal
        grow = \remaining credit ->
            if remaining <= 0 then credit else grow (remaining - 1) (Decimal.mul credit (Decimal.add (Decimal.fromFloat 1.0) bonusRate))
    in
    grow days startingCredit

createPolicy : Int -> Int -> Decimal -> RentalPolicy
createPolicy minimumQuantity capacity deliveryFee = Policy.new0NoneIntNoneIntNoneDecimal minimumQuantity capacity deliveryFee
combineQuantities : List Int -> Int
combineQuantities quantities = List.foldl (\item state -> (\sum quantity -> sum + quantity) state item) 0 quantities
allowedModels : Set String
allowedModels = Set.fromList ["SING", "MOVE"]
promotionRates : Dict String Decimal
promotionRates = Dict.fromList [("SING", Decimal.fromFloat 5.0), ("MOVE", Decimal.fromFloat 0.0)]
demoOrders : List RentalOrder
demoOrders =
    [ RentalOrder "ORD-001" "SING" (combineQuantities [1,1]) (combinedDays (1,2)) (Decimal.fromFloat 50.0) (Just (Decimal.fromFloat 10.0)) State.confirmed 'L'
    , RentalOrder "ORD-002" "MOVE" 1 2 (Decimal.fromFloat 80.0) Nothing State.confirmed 'R'
    , RentalOrder "ORD-003" "SING" 4 1 (Decimal.fromFloat 50.0) Nothing State.cancelled 'L'
    , RentalOrder "ORD-004" "FLY" 1 1 (Decimal.fromFloat 50.0) Nothing State.confirmed 'R'
    , RentalOrder "ORD-005" "MOVE" 0 1 (Decimal.fromFloat 80.0) Nothing State.confirmed 'L'
    ]
