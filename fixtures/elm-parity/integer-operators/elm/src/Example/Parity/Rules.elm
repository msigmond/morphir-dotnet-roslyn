module Example.Parity.Rules exposing (..)


add : Int -> Int -> Int
add a b = a + b
subtract : Int -> Int -> Int
subtract a b = a - b
multiply : Int -> Int -> Int
multiply a b = a * b
divide : Int -> Int -> Int
divide a b = a // b
equal : Int -> Int -> Bool
equal a b = a == b
notEqual : Int -> Int -> Bool
notEqual a b = a /= b
less : Int -> Int -> Bool
less a b = a < b
lessEqual : Int -> Int -> Bool
lessEqual a b = a <= b
greater : Int -> Int -> Bool
greater a b = a > b
greaterEqual : Int -> Int -> Bool
greaterEqual a b = a >= b
