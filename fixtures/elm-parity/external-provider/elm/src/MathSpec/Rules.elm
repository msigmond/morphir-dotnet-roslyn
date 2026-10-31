module MathSpec.Rules exposing (abs)
abs : Int -> Int
abs x = if x < 0 then Basics.negate x else x
