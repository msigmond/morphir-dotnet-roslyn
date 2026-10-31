module Example.Completion.Rules exposing (..)
import Morphir.SDK.Decimal as Decimal exposing (Decimal)
m0 : Decimal -> Decimal
m0 p0 = Decimal.abs p0
m1 : Decimal -> Decimal -> Decimal
m1 p0 p1 = Decimal.add p0 p1
m2 : Float -> Decimal
m2 p0 = Decimal.fromFloat p0
m3 : Float -> Decimal
m3 p0 = Decimal.fromFloat p0
m4 : Int -> Decimal
m4 p0 = Decimal.fromInt p0
m5 : Int -> Decimal
m5 p0 = Decimal.fromInt p0
m6 : Int -> Decimal
m6 p0 = Decimal.fromInt p0
m7 : Int -> Decimal
m7 p0 = Decimal.fromInt p0
m8 : Decimal -> Decimal -> Bool
m8 receiver p0 = Decimal.eq receiver p0
m9 : Decimal -> Decimal -> Bool
m9 p0 p1 = Decimal.eq p0 p1
m10 : Decimal
m10  = Decimal.minusOne
m11 : Decimal -> Decimal -> Decimal
m11 p0 p1 = Decimal.mul p0 p1
m12 : Decimal -> Decimal
m12 p0 = Decimal.negate p0
m13 : Decimal
m13  = Decimal.one
m14 : Decimal -> Decimal
m14 p0 = Decimal.round p0
m15 : Decimal -> Decimal -> Decimal
m15 p0 p1 = Decimal.sub p0 p1
m16 : Decimal -> Decimal
m16 p0 = Decimal.truncate p0
m17 : Decimal
m17  = Decimal.zero
m18 : Float -> Decimal
m18 p0 = Decimal.fromFloat p0
m19 : Float -> Decimal
m19 p0 = Decimal.fromFloat p0
m20 : Decimal -> Float
m20 p0 = Decimal.toFloat p0
m21 : Decimal -> Float
m21 p0 = Decimal.toFloat p0
m22 : Int -> Decimal
m22 p0 = Decimal.fromInt p0
m23 : Int -> Decimal
m23 p0 = Decimal.fromInt p0
m24 : Int -> Decimal
m24 p0 = Decimal.fromInt p0
m25 : Int -> Decimal
m25 p0 = Decimal.fromInt p0
m26 : Int -> Decimal
m26 p0 = Decimal.fromInt p0
m27 : Int -> Decimal
m27 p0 = Decimal.fromInt p0
m28 : Int -> Decimal
m28 p0 = Decimal.fromInt p0
m29 : Int -> Decimal
m29 p0 = Decimal.fromInt p0
m30 : Decimal -> Decimal -> Bool
m30 p0 p1 = Decimal.neq p0 p1
m31 : Decimal -> Decimal -> Decimal
m31 p0 p1 = Decimal.mul p0 p1
m32 : Decimal -> Decimal
m32 p0 = p0
m33 : Decimal -> Decimal -> Decimal
m33 p0 p1 = Decimal.add p0 p1
m34 : Decimal -> Decimal
m34 p0 = Decimal.negate p0
m35 : Decimal -> Decimal -> Decimal
m35 p0 p1 = Decimal.sub p0 p1
m36 : Decimal -> Decimal -> Bool
m36 p0 p1 = Decimal.lt p0 p1
m37 : Decimal -> Decimal -> Bool
m37 p0 p1 = Decimal.lte p0 p1
m38 : Decimal -> Decimal -> Bool
m38 p0 p1 = Decimal.eq p0 p1
m39 : Decimal -> Decimal -> Bool
m39 p0 p1 = Decimal.gt p0 p1
m40 : Decimal -> Decimal -> Bool
m40 p0 p1 = Decimal.gte p0 p1
