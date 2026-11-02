module Example.CurrentSupported.ClassicLoanCalculations exposing (..)

import Dict exposing (Dict)
import Set exposing (Set)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
import Example.CurrentSupported exposing (ClassicBox, ClassicLoan, ClassicLoanFee, ClassicState, ClassicValue)
import Example.CurrentSupported.ClassicState as State
import Example.CurrentSupported.ClassicValue as Value

atLeast : Int -> Int -> Bool
atLeast minimum value = value >= minimum

positiveQuantities : List Int -> List Int
positiveQuantities values = List.filter (\value -> value > 0) values

doubleQuantities : List Int -> List Int
doubleQuantities values = List.map (\value -> value * 2) values

totalQuantities : List Int -> Int
totalQuantities values = List.foldl (\item state -> (\total value -> total + value) state item) 0 values

marketValue : ClassicLoan -> Int
marketValue loan =
    loan.quantity * loan.price

dailyFee : ClassicLoan -> Int
dailyFee loan =
    let
        marketVal : Int
        marketVal =
            (marketValue loan)

        rate : Int
        rate =
            loan.annualFeeRate
    in
    marketVal * rate // 100 // 360

calculate : ClassicLoan -> ClassicLoanFee
calculate loan =
    let
        marketVal : Int
        marketVal =
            (marketValue loan)

        dailyF : Int
        dailyF =
            (dailyFee loan)
    in
    ClassicLoanFee loan.securityId marketVal dailyF

positiveMarketValue : ClassicLoan -> Int
positiveMarketValue loan =
    if loan.quantity > 0 then marketValue loan else 0

setQuantity : ClassicLoan -> Int -> ClassicLoan
setQuantity loan quantity = { loan | quantity = quantity }

readPair : ( Int, Int ) -> Int
readPair pair =
    let
        ( quantity, price ) = pair
    in quantity * price

addFee : Int -> Int -> Int
addFee value fee =
    let
        add : Int -> Int
        add amount = amount + fee
    in add value

rounded : Decimal -> Decimal
rounded value = Decimal.round value

optional : Maybe Int -> Int
optional value = Maybe.withDefault 3 value
nullableProduct : Maybe Int -> Maybe Int -> Maybe Int
nullableProduct a b = Maybe.map2 (*) a b
divideDecimals : Decimal -> Decimal -> Maybe Decimal
divideDecimals amount count = Decimal.div amount count
sumDefaults : Int -> Int -> Int
sumDefaults a b = a + b
useDefaults : Int
useDefaults = sumDefaults 2 4
count : List Int -> Int
count values = List.length values
useParams : Int
useParams = count [1,2]
choose0NoneInt : Int -> Int
choose0NoneInt value = value
choose0NoneLong : Int -> Int
choose0NoneLong value = value
useOverload : Int -> Int
useOverload value = choose0NoneInt value
referencePair : Int -> Bool -> (Int,Bool)
referencePair quantity flag = (quantity,flag)
state : ClassicState
state = State.active
createValue : Int -> ClassicValue
createValue value = Value.new0NoneInt value

identity : t -> t
identity value = value
wrap : t -> ClassicBox t
wrap value = ClassicBox value
unwrap : ClassicBox t -> t
unwrap box = box.value
readWrapped : Int -> Int
readWrapped value = unwrap (wrap value)

createItems : Int -> List Int
createItems value = [value,2]
readItem : List Int -> Int -> Maybe Int
readItem items index = List.head (List.drop index items)
itemCount : List Int -> Int
itemCount items = List.length items
createLookup : Int -> String -> Dict Int String
createLookup key value = Dict.fromList [(key,value)]
readLookup : Dict Int String -> Int -> Maybe String
readLookup items key = Dict.get key items
createSet : Int -> Set Int
createSet value = Set.fromList [value,2]

classifyQuantity : Int -> Int
classifyQuantity quantity = case quantity of
    0 -> 0
    _ -> 1
firstOrZero : List Int -> Int
firstOrZero items = case items of
    [] -> 0
    first :: _ -> first
loanPresence : Maybe ClassicLoan -> Int
loanPresence loan = case loan of
    Nothing -> 0
    _ -> 1
classifyState : ClassicState -> Int
classifyState state = case state of
    0 -> 0
    _ -> 1
