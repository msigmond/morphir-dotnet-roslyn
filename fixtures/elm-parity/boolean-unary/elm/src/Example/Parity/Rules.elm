module Example.Parity.Rules exposing (..)


and : Bool -> Bool -> Bool
and a b = a && b
or : Bool -> Bool -> Bool
or a b = a || b
not : Bool -> Bool
not a = Basics.not a
xor : Bool -> Bool -> Bool
xor a b = Basics.xor a b
negate : Int -> Int
negate a = Basics.negate a
positive : Int -> Int
positive a = a
floatNegate : Float -> Float
floatNegate a = Basics.negate a
