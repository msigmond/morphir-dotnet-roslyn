module Example.Compat.Integers exposing (Integer, Division, fromSmall, toSmall, add, subtract, multiply, negate, compare, wrap, divide, remainder, bitAnd, bitOr, bitXor, complement, shiftLeft, shiftRight, shiftUnsigned, toBinaryFloat, toTruncatedFloat, shiftBigUnsigned, fromFiniteFloat, floatCast)


type alias Integer =
    { negative : Bool, digits : List Int }

type alias Division =
    { quotient : Integer, remainder : Integer }

type alias Multiple =
    { factor : Integer, scaled : Integer }

clean : List Int -> List Int
clean digits =
    case digits of
        [] -> []
        head :: tail ->
            let
                rest : List Int
                rest = clean tail
            in
            if head == 0 && rest == [] then [] else head :: rest

make : Bool -> List Int -> Integer
make negative digits =
    let
        canonical : List Int
        canonical = clean digits
    in
    { negative = negative && canonical /= [], digits = canonical }

zero : Integer
zero = { negative = False, digits = [] }

one : Integer
one = { negative = False, digits = [1] }

fromPositive : Int -> List Int
fromPositive value =
    if value == 0 then [] else modBy 65536 value :: fromPositive (value // 65536)

fromSmall : Int -> Integer
fromSmall value = make (value < 0) (fromPositive (abs value))

smallMagnitude : List Int -> Int
smallMagnitude digits =
    case digits of
        [] -> 0
        head :: tail -> head + 65536 * smallMagnitude tail

toSmall : Integer -> Int
toSmall value = if value.negative then -(smallMagnitude value.digits) else smallMagnitude value.digits

negate : Integer -> Integer
negate value = make (not value.negative) value.digits

compareMagnitude : List Int -> List Int -> Int
compareMagnitude left right =
    case left of
        [] -> if right == [] then 0 else -1
        a :: aa ->
            case right of
                [] -> 1
                b :: bb ->
                    let
                        higher : Int
                        higher = compareMagnitude aa bb
                    in
                    if higher /= 0 then higher else if a < b then -1 else if a > b then 1 else 0

compare : Integer -> Integer -> Int
compare left right =
    if left.negative /= right.negative then
        if left.negative then -1 else 1
    else if left.negative then -(compareMagnitude left.digits right.digits)
    else compareMagnitude left.digits right.digits

addCarry : Int -> List Int -> List Int
addCarry carry digits =
    if carry == 0 then digits else
    case digits of
        [] -> [carry]
        head :: tail ->
            let
                total : Int
                total = head + carry
            in
            modBy 65536 total :: addCarry (total // 65536) tail

addDigits : Int -> List Int -> List Int -> List Int
addDigits carry left right =
    case left of
        [] -> addCarry carry right
        a :: aa ->
            case right of
                [] -> addCarry carry left
                b :: bb ->
                    let
                        total : Int
                        total = a + b + carry
                    in
                    modBy 65536 total :: addDigits (total // 65536) aa bb

subtractDigits : Int -> List Int -> List Int -> List Int
subtractDigits borrow left right =
    case left of
        [] -> []
        a :: aa ->
            case right of
                [] ->
                    let
                        difference : Int
                        difference = a - borrow
                    in
                    (if difference < 0 then difference + 65536 else difference) :: subtractDigits (if difference < 0 then 1 else 0) aa []
                b :: bb ->
                    let
                        difference : Int
                        difference = a - b - borrow
                    in
                    (if difference < 0 then difference + 65536 else difference) :: subtractDigits (if difference < 0 then 1 else 0) aa bb

add : Integer -> Integer -> Integer
add left right =
    if left.negative == right.negative then make left.negative (addDigits 0 left.digits right.digits)
    else if compareMagnitude left.digits right.digits >= 0 then make left.negative (subtractDigits 0 left.digits right.digits)
    else make right.negative (subtractDigits 0 right.digits left.digits)

subtract : Integer -> Integer -> Integer
subtract left right = add left (negate right)

multiplyDigit : Int -> Int -> List Int -> List Int
multiplyDigit digit carry digits =
    case digits of
        [] -> if carry == 0 then [] else [carry]
        head :: tail ->
            let
                product : Int
                product = head * digit + carry
            in
            modBy 65536 product :: multiplyDigit digit (product // 65536) tail

multiplyDigits : List Int -> List Int -> List Int
multiplyDigits left right =
    case left of
        [] -> []
        head :: tail -> addDigits 0 (multiplyDigit head 0 right) (0 :: multiplyDigits tail right)

multiply : Integer -> Integer -> Integer
multiply left right = make (left.negative /= right.negative) (multiplyDigits left.digits right.digits)

zeros : Int -> List Int -> List Int
zeros count rest = if count <= 0 then rest else 0 :: zeros (count - 1) rest

powerOfTwo : Int -> Integer
powerOfTwo bits = make False (zeros (bits // 16) [2 ^ modBy 16 bits])

lowDigits : Int -> List Int -> List Int
lowDigits bits digits =
    if bits <= 0 then [] else
    case digits of
        [] -> []
        head :: tail -> if bits >= 16 then head :: lowDigits (bits - 16) tail else [modBy (2 ^ bits) head]

wrap : Int -> Bool -> Integer -> Integer
wrap bits signed value =
    let
        low : Integer
        low = make False (lowDigits bits value.digits)
        modulus : Integer
        modulus = powerOfTwo bits
        residue : Integer
        residue = if value.negative && low.digits /= [] then subtract modulus low else low
    in
    if signed && compare residue (powerOfTwo (bits - 1)) >= 0 then negate (subtract modulus residue) else residue

largestMultiple : Integer -> Integer -> Integer -> Multiple
largestMultiple numerator scaled factor =
    let
        doubled : Integer
        doubled = add scaled scaled
    in
    if compare doubled numerator > 0 then {scaled=scaled,factor=factor}
    else largestMultiple numerator doubled (add factor factor)

unsignedDivision : Integer -> Integer -> Division
unsignedDivision numerator denominator =
    if compare numerator denominator < 0 then {quotient=zero,remainder=numerator}
    else
        let
            multiple : Multiple
            multiple = largestMultiple numerator denominator one
            rest : Division
            rest = unsignedDivision (subtract numerator multiple.scaled) denominator
        in
        {quotient=add multiple.factor rest.quotient,remainder=rest.remainder}

divide : Integer -> Integer -> Integer
divide left right =
    let
        result : Division
        result = unsignedDivision (make False left.digits) (make False right.digits)
    in
    make (left.negative /= right.negative) result.quotient.digits

remainder : Integer -> Integer -> Integer
remainder left right =
    let
        result : Division
        result = unsignedDivision (make False left.digits) (make False right.digits)
    in
    make left.negative result.remainder.digits

length : List Int -> Int
length digits = case digits of
    [] -> 0
    _ :: tail -> 1 + length tail

bitDigit : Int -> Int -> Int -> Int
bitDigit operation left right =
    if left == 0 && right == 0 then 0 else
    let
        a : Bool
        a = modBy 2 left /= 0
        b : Bool
        b = modBy 2 right /= 0
        low : Int
        low = if (if operation == 0 then a && b else if operation == 1 then a || b else a /= b) then 1 else 0
    in
    low + 2 * bitDigit operation (left // 2) (right // 2)

bitDigits : Int -> List Int -> List Int -> List Int
bitDigits operation left right =
    case left of
        [] ->
            case right of
                [] -> []
                b :: bb -> (if operation == 0 then 0 else b) :: bitDigits operation [] bb
        a :: aa ->
            case right of
                [] -> (if operation == 0 then 0 else a) :: bitDigits operation aa []
                b :: bb -> (bitDigit operation a b) :: bitDigits operation aa bb

bitOperation : Int -> Integer -> Integer -> Integer
bitOperation operation left right =
    let
        bits : Int
        bits = 16 * (1 + max (length left.digits) (length right.digits))
        a : Integer
        a = wrap bits False left
        b : Integer
        b = wrap bits False right
    in
    wrap bits True (make False (bitDigits operation a.digits b.digits))

bitAnd : Integer -> Integer -> Integer
bitAnd = bitOperation 0

bitOr : Integer -> Integer -> Integer
bitOr = bitOperation 1

bitXor : Integer -> Integer -> Integer
bitXor = bitOperation 2

complement : Integer -> Integer
complement value = subtract (negate value) one

drop : Int -> List Int -> List Int
drop count digits =
    if count <= 0 then digits else
    case digits of
        [] -> []
        _ :: tail -> drop (count - 1) tail

reverseOnto : List Int -> List Int -> List Int
reverseOnto digits acc = case digits of
    [] -> acc
    head :: tail -> reverseOnto tail (head :: acc)

divideDigit : Int -> Int -> List Int -> List Int
divideDigit divisor carry digits =
    case digits of
        [] -> []
        head :: tail ->
            let
                current : Int
                current = carry * 65536 + head
            in
            (current // divisor) :: divideDigit divisor (modBy divisor current) tail

rightMagnitude : Int -> List Int -> List Int
rightMagnitude bits digits = reverseOnto (divideDigit (2 ^ modBy 16 bits) 0 (reverseOnto (drop (bits // 16) digits) [])) []

leftPositive : Integer -> Int -> Integer
leftPositive value count = make value.negative (zeros (count // 16) (multiplyDigit (2 ^ modBy 16 count) 0 value.digits))

rightPositive : Integer -> Int -> Integer
rightPositive value count =
    let
        quotient : Integer
        quotient = make value.negative (rightMagnitude count value.digits)
    in
    if value.negative && clean (lowDigits count value.digits) /= [] then subtract quotient one else quotient

shiftLeft : Integer -> Int -> Integer
shiftLeft value count = if count < 0 then rightPositive value -count else leftPositive value count

shiftRight : Integer -> Int -> Integer
shiftRight value count = if count < 0 then leftPositive value -count else rightPositive value count

shiftUnsigned : Int -> Integer -> Int -> Integer
shiftUnsigned bits value count = shiftRight (wrap bits False value) (modBy bits count)

bitLength : List Int -> Int
bitLength digits = case digits of
    [] -> 0
    head :: tail -> if tail == [] then smallBits head else 16 + bitLength tail

smallBits : Int -> Int
smallBits value = if value == 0 then 0 else 1 + smallBits (value // 2)

magnitudeFloat : List Int -> Float
magnitudeFloat digits = case digits of
    [] -> 0
    head :: tail -> toFloat head + 65536 * magnitudeFloat tail

toBinaryFloat : Int -> Integer -> Float
toBinaryFloat precision value =
    let
        cut : Int
        cut = max 0 (bitLength value.digits - precision)
        top : Integer
        top = make False (rightMagnitude cut value.digits)
        low : Integer
        low = make False (lowDigits cut value.digits)
        halfway : Integer
        halfway = powerOfTwo (max 0 (cut - 1))
        rounded : Integer
        rounded = if cut > 0 && (compare low halfway > 0 || compare low halfway == 0 && modBy 2 (toSmall (wrap 1 False top)) /= 0) then add top one else top
        magnitude : Float
        magnitude = magnitudeFloat rounded.digits * (2 ^ toFloat cut)
    in
    if value.negative then -magnitude else magnitude

floatDigits : Float -> List Int
floatDigits value =
    if value < 1 then [] else
    let
        quotient : Float
        quotient = toFloat (floor (value / 65536))
        digit : Int
        digit = floor (value - quotient * 65536)
    in
    digit :: floatDigits quotient

fromFiniteFloat : Float -> Integer
fromFiniteFloat value = make (value < 0) (floatDigits (abs value))

floatCast : Int -> Bool -> Float -> Integer
floatCast bits signed value =
    let
        boundary : Integer
        boundary = powerOfTwo (if signed then bits - 1 else bits)
        limit : Float
        limit = toBinaryFloat 53 boundary
    in
    if bits < 32 then wrap bits signed (floatCast 32 True value)
    else if isNaN value then zero
    else if signed && value <= (0 - limit) then negate boundary
    else if value >= limit then subtract boundary one
    else if not signed && value <= 0 then zero
    else fromFiniteFloat value


-- BigInteger's pinned binary64 conversion truncates the normalized significand.
toTruncatedFloat : Integer -> Float
toTruncatedFloat value =
    let
        cut : Int
        cut = max 0 (bitLength value.digits - 53)
        top : Integer
        top = make False (rightMagnitude cut value.digits)
        magnitude : Float
        magnitude = magnitudeFloat top.digits * (2 ^ toFloat cut)
    in
    if value.negative then -magnitude else magnitude

-- The pinned BigInteger logical shift uses its 32-bit storage digit width.
shiftBigUnsigned : Integer -> Int -> Integer
shiftBigUnsigned value count =
    if count < 0 then shiftLeft value -count
    else if count == 0 || not value.negative then shiftRight value count
    else
        let
            storage : Int
            storage = 32 * max 1 ((length value.digits + 1) // 2)
            bits : Int
            bits = if compare (make False value.digits) (powerOfTwo (storage - 1)) > 0 then storage + 32 else storage
            remaining : Int
            remaining = bits - 32 * (count // 32)
        in
        if count >= bits then fromSmall -1
        else wrap remaining True (shiftRight (wrap bits False value) count)
