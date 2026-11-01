module Example.CurrentSupported.ClassicLoanCalculations exposing (..)

import Morphir.SDK.Decimal as Decimal exposing (Decimal)
import Example.CurrentSupported exposing (ClassicBox, ClassicLoan, ClassicLoanFee, ClassicState, ClassicValue)
import Example.CurrentSupported.ClassicState as State
import Example.CurrentSupported.ClassicValue as Value

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
