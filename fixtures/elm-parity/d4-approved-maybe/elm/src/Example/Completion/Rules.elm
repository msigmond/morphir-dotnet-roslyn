module Example.Completion.Rules exposing (..)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
import Example.Completion exposing (Data)
import Example.Completion.Data as Data
null : Maybe String
null = Nothing
text : Maybe String
text = Just "x"
number : Maybe Int
number = Just 2
branch : Bool -> Maybe Int
branch b = if b then Just 1 else Nothing
fallback : Maybe Int -> Int
fallback value = Maybe.withDefault 3 value
multiply : Maybe Int -> Maybe Int -> Maybe Int
multiply a b = Maybe.map2 (*) a b
present : Maybe Int -> Bool
present value = value /= Nothing
missing : Maybe Data -> Bool
missing value = value == Nothing
notMissing : Maybe Data -> Bool
notMissing value = value /= Nothing
read : Maybe Data -> Maybe Int
read data = Maybe.map (\item -> item.value) data
zero : Int
zero = 0
false : Bool
false = False
absent : Maybe Int
absent = Nothing

echo : Maybe Int -> Maybe Int
echo value = value
call : Maybe Int
call = echo (Just 2)
length : Maybe String -> Maybe Int
length value = Just (Maybe.withDefault 2 (Maybe.map (\item -> String.length item) value))
invoke : Maybe Data -> Int -> Maybe Int
invoke data extra = Maybe.map (\item -> Data.read item extra) data

mixed : Maybe Int -> Int -> Maybe Int
mixed a b = Maybe.map2 (*) a (Just b)
negate : Maybe Int -> Maybe Int
negate value = Maybe.map Basics.negate value
not : Maybe Bool -> Maybe Bool
not value = Maybe.map Basics.not value

defaultFloat : Float
defaultFloat = 0.0
defaultDecimal : Decimal
defaultDecimal = Decimal.fromFloat 0.0
defaultChar : Char
defaultChar = '\u{0000}'
defaultReference : Maybe String
defaultReference = Nothing

textOf : Maybe Data -> Maybe String
textOf data = Maybe.map (\item -> Data.getText0 item) data
localReference : Maybe Data -> Maybe String
localReference data =
    let
        value : Maybe String
        value = Maybe.map (\item -> Data.getText0 item) data
    in value
localBranch : Bool -> String -> Maybe String
localBranch flag value =
    let
        result : Maybe String
        result = if flag then Nothing else Just value
    in result
localTuple : Maybe String -> (Maybe String,Int)
localTuple text =
    let
        pair : (Maybe String,Int)
        pair = (text,1)
    in pair
plain : Data -> Maybe Int
plain data = Maybe.map (\item -> item.value) (Just data)
plainMissing : Data -> Bool
plainMissing data = Just data == Nothing
plainPattern : Data -> Bool
plainPattern data = Just data /= Nothing
plainFallback : String -> String
plainFallback value = Maybe.withDefault "x" (Just value)
nullFallback : String -> String
nullFallback value = Maybe.withDefault value Nothing
applyOptional : Maybe (Int -> Int) -> Int -> Maybe Int
applyOptional callback value = Maybe.map (\item -> item value) callback
