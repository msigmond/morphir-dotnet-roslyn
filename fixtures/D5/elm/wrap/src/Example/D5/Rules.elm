module Example.D5.Rules exposing (next)

import Example.Compat.Integers as I

next : Int -> Int
next value =
    I.toSmall (I.wrap 32 True (I.add (I.fromSmall value) (I.fromSmall 1)))
