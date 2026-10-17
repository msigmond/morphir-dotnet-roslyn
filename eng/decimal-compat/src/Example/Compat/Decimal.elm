module Example.Compat.Decimal exposing (Decimal, make, fromInteger, negate, add, subtract, multiply, divide, remainder, compare, roundTo, toInteger, scale, isNegative)

import Example.Compat.Integers as I
import Morphir.SDK.Decimal as D

-- Input domains: coefficient is nonnegative and <= 2^96-1, scale is 0..28.
-- epsilon is a proved extraction of D.fromString "0.0000000000000000000000000001".
type alias Decimal =
    { coefficient : I.Integer, scale : Int, negative : Bool, value : D.Decimal }

powerTen : Int -> I.Integer
powerTen count =
    if count <= 0 then I.fromSmall 1 else I.multiply (I.fromSmall 10) (powerTen (count - 1))

magnitude : I.Integer -> I.Integer
magnitude value = {negative=False,digits=value.digits}

sdkMagnitude : List Int -> D.Decimal
sdkMagnitude digits =
    case digits of
        [] -> D.zero
        head :: tail -> D.add (D.fromInt head) (D.mul (D.fromInt 65536) (sdkMagnitude tail))

sdkFactor : Int -> D.Decimal -> D.Decimal
sdkFactor count value =
    if count <= 0 then value else sdkFactor (count - 1) (D.mul (D.fromInt 10) value)

make : D.Decimal -> Bool -> Int -> I.Integer -> Decimal
make epsilon negative digits coefficient =
    let
        value : D.Decimal
        value = D.mul (sdkMagnitude coefficient.digits) (sdkFactor (28 - digits) epsilon)
    in
    {coefficient=magnitude coefficient,scale=digits,negative=negative,value=if negative then D.negate value else value}

fromInteger : D.Decimal -> I.Integer -> Decimal
fromInteger epsilon value = make epsilon value.negative 0 (magnitude value)

negate : Decimal -> Decimal
negate value = {coefficient=value.coefficient,scale=value.scale,negative=not value.negative,value=D.negate value.value}

signed : Decimal -> I.Integer
signed value = if value.negative then I.negate value.coefficient else value.coefficient

aligned : Int -> Decimal -> I.Integer
aligned digits value = I.multiply (signed value) (powerTen (digits - value.scale))

maximum : I.Integer
maximum = I.subtract (I.shiftLeft (I.fromSmall 1) 96) (I.fromSmall 1)

roundedQuotient : Int -> Bool -> I.Integer -> I.Integer -> I.Integer
roundedQuotient mode negative numerator denominator =
    let
        quotient : I.Integer
        quotient = I.divide numerator denominator
        rest : I.Integer
        rest = I.remainder numerator denominator
        half : Int
        half = I.compare (I.add rest rest) denominator
        increase : Bool
        increase =
            if mode == 0 then half > 0 || half == 0 && I.compare (I.bitAnd quotient (I.fromSmall 1)) (I.fromSmall 0) /= 0
            else if mode == 1 then half >= 0
            else if mode == 3 then negative && rest.digits /= []
            else if mode == 4 then not negative && rest.digits /= []
            else False
    in
    if increase then I.add quotient (I.fromSmall 1) else quotient

-- The source proof ensures a representable result before this helper is called.
fit : D.Decimal -> Bool -> Int -> I.Integer -> Int -> Decimal
fit epsilon negative digits coefficient dropped =
    let
        rounded : I.Integer
        rounded = roundedQuotient 0 negative coefficient (powerTen dropped)
    in
    if I.compare rounded maximum <= 0 then make epsilon negative (digits - dropped) rounded
    else fit epsilon negative digits coefficient (dropped + 1)

add : D.Decimal -> Decimal -> Decimal -> Decimal
add epsilon left right =
    let
        digits : Int
        digits = max left.scale right.scale
        coefficient : I.Integer
        coefficient = I.add (aligned digits left) (aligned digits right)
        negative : Bool
        negative =
            if coefficient.digits /= [] then coefficient.negative
            else if left.coefficient.digits == [] then
                if left.scale >= right.scale then left.negative else right.negative
            else if left.scale <= right.scale then left.negative else right.negative
    in
    fit epsilon negative digits (magnitude coefficient) 0

subtract : D.Decimal -> Decimal -> Decimal -> Decimal
subtract epsilon left right = add epsilon left (negate right)

multiply : D.Decimal -> Decimal -> Decimal -> Decimal
multiply epsilon left right =
    let
        digits : Int
        digits = left.scale + right.scale
        coefficient : I.Integer
        coefficient = I.multiply left.coefficient right.coefficient
    in
    if (I.shiftRight left.coefficient 32).digits == [] && (I.shiftRight right.coefficient 32).digits == [] then
        if digits > 47 then make epsilon False 0 (I.fromSmall 0)
        else fit epsilon (left.negative /= right.negative) digits coefficient (max 0 (digits - 28))
    else if coefficient.digits == [] then make epsilon False 0 (I.fromSmall 0)
    else fit epsilon (left.negative /= right.negative) digits coefficient (max 0 (digits - 28))

compare : Decimal -> Decimal -> Int
compare left right =
    let digits = max left.scale right.scale in
    I.compare (aligned digits left) (aligned digits right)

roundTo : D.Decimal -> Int -> Int -> Decimal -> Decimal
roundTo epsilon digits mode value =
    if value.scale <= digits then value
    else make epsilon value.negative digits (roundedQuotient mode value.negative value.coefficient (powerTen (value.scale - digits)))

toInteger : Decimal -> I.Integer
toInteger value =
    let result = I.divide value.coefficient (powerTen value.scale) in
    if value.negative then I.negate result else result

scale : Decimal -> Int
scale value = value.scale

isNegative : Decimal -> Bool
isNegative value = value.negative

unscale : D.Decimal -> Bool -> Int -> I.Integer -> Decimal
unscale epsilon negative digits coefficient =
    if digits > 0 && (I.remainder coefficient (I.fromSmall 10)).digits == [] then
        unscale epsilon negative (digits - 1) (I.divide coefficient (I.fromSmall 10))
    else make epsilon negative digits coefficient

finishDivision : D.Decimal -> Bool -> Int -> I.Integer -> I.Integer -> Decimal
finishDivision epsilon negative digits numerator denominator =
    let rounded = roundedQuotient 0 negative numerator denominator in
    if I.compare rounded maximum <= 0 then unscale epsilon negative digits rounded
    else finishDivision epsilon negative (digits - 1) numerator (I.multiply denominator (I.fromSmall 10))

divideLoop : D.Decimal -> Bool -> Int -> I.Integer -> I.Integer -> I.Integer -> Bool -> Decimal
divideLoop epsilon negative digits quotient rest denominator expanded =
    if rest.digits == [] then
        if expanded then unscale epsilon negative digits quotient else make epsilon negative digits quotient
    else
        let
            nextRest : I.Integer
            nextRest = I.multiply rest (I.fromSmall 10)
            nextQuotient : I.Integer
            nextQuotient = I.add (I.multiply quotient (I.fromSmall 10)) (I.divide nextRest denominator)
        in
        if digits < 28 && I.compare nextQuotient maximum <= 0 then
            divideLoop epsilon negative (digits + 1) nextQuotient (I.remainder nextRest denominator) denominator True
        else finishDivision epsilon negative digits (I.add (I.multiply quotient denominator) rest) denominator

divide : D.Decimal -> Decimal -> Decimal -> Decimal
divide epsilon left right =
    let
        digits : Int
        digits = left.scale - right.scale
        numerator : I.Integer
        numerator = I.multiply left.coefficient (powerTen (max 0 -digits))
    in
    divideLoop epsilon (left.negative /= right.negative) (max 0 digits) (I.divide numerator right.coefficient) (I.remainder numerator right.coefficient) right.coefficient False

remainder : D.Decimal -> Decimal -> Decimal -> Decimal
remainder epsilon left right =
    let
        order : Int
        order = I.compare (magnitude (aligned digits left)) (magnitude (aligned digits right))
        digits : Int
        digits = max left.scale right.scale
        numerator : I.Integer
        numerator = magnitude (aligned digits left)
        denominator : I.Integer
        denominator = magnitude (aligned digits right)
    in
    if order < 0 then left
    else make epsilon left.negative digits (I.remainder numerator denominator)
