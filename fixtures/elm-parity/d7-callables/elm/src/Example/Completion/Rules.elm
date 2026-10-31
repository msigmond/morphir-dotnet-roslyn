module Example.Completion.Rules exposing (..)
double : Int -> Int
double x = x * 2
reference : Int -> Int
reference = double
invoke : (Int -> Int) -> Int -> Int
invoke callback value = callback value
value : Int
value = 1
zeroReference : Int
zeroReference = value
zeroInvoke : Int -> Int
zeroInvoke callback = callback
capture : Int -> Int
capture a =
    let
        local : Int
        local = a
    in local
parameters : Int -> Int
parameters a =
    let
        local : Int -> Int
        local x = x + a
    in local 2
recursive : Int -> Int
recursive n =
    let
        loop : Int -> Int
        loop value = if value <= 0 then 0 else loop (value - 1)
    in loop n

construct : Int -> Int
construct = double
