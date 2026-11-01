module Example.Parity.Rules exposing (..)
import Example.Parity exposing (State)
import Example.Parity.State as State
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
integer : Int -> Int
integer value = case value of
    0 -> 1
    _ -> 2
float : Float -> Int
float value = case value of
    0.1 -> 1
    _ -> 0
boolean : Bool -> Int
boolean value = case value of
    True -> 1
    False -> 0
character : Char -> Int
character value = case value of
    'x' -> 1
    _ -> 0
text : String -> Int
text value = case value of
    "x" -> 1
    _ -> 0
enum : State -> Int
enum value = case value of
    0 -> 1
    1 -> 2
    _ -> 0
nullable : Maybe Int -> Int
nullable value = case value of
    Nothing -> 0
    Just 0 -> 1
    _ -> 2
isMissing : Maybe Int -> Bool
isMissing value = value == Nothing
isPresent : Maybe Int -> Bool
isPresent value = value /= Nothing

negativeResult : Int -> Int
negativeResult value = case value of
    0 -> Basics.negate 1
    _ -> 0

partial : Int -> Int
partial value = case value of
    0 -> 1

negativeFloat : Float
negativeFloat = Basics.negate 0.1
negativeDecimal : Decimal
negativeDecimal = Decimal.negate (Decimal.fromFloat 0.1)
groupedNegative : Int
groupedNegative = Basics.negate 1
negate : Int -> Int
negate value = -value
boundary : Int -> Int
boundary value = case value of
    9007199254740991 -> 1
    _ -> 0
lift : Int -> Maybe Int
lift value = case value of
    0 -> Just 1
    _ -> Just 2
nullableText : Maybe String -> Int
nullableText value = case value of
    Nothing -> 0
    Just "x" -> 1
    _ -> 2
enumResult : State -> State
enumResult value = case value of
    0 -> State.active
    _ -> value
