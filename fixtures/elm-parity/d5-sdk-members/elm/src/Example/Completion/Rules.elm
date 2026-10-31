module Example.Completion.Rules exposing (..)
abs : Int -> Int
abs value = Basics.abs value
min : Int -> Int -> Int
min a b = Basics.min a b
max : Float -> Float -> Float
max a b = Basics.max a b
sqrt : Float -> Float
sqrt value = Basics.sqrt value
sin : Float -> Float
sin value = Basics.sin value
cos : Float -> Float
cos value = Basics.cos value
atan2 : Float -> Float -> Float
atan2 a b = Basics.atan2 a b
power : Float -> Float -> Float
power a b = a ^ b
nan : Float -> Bool
nan value = isNaN value
infinite : Float -> Bool
infinite value = isInfinite value
text : Int -> String
text value = String.fromInt value
length : String -> Int
length value = String.length value

tan : Float -> Float
tan x = Basics.tan x
asin : Float -> Float
asin x = Basics.asin x
acos : Float -> Float
acos x = Basics.acos x
atan : Float -> Float
atan x = Basics.atan x
singleSqrt : Float -> Float
singleSqrt x = Basics.sqrt x
longAbs : Int -> Int
longAbs x = Basics.abs x
intMin : Int -> Int -> Int
intMin a b = Basics.min a b
doubleMax : Float -> Float -> Float
doubleMax a b = Basics.max a b
singleAbs : Float -> Float
singleAbs x = Basics.abs x
floatText : Float -> String
floatText x = String.fromFloat x
charText : Char -> String
charText x = String.fromChar x
