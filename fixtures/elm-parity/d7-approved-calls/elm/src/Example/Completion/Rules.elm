module Example.Completion.Rules exposing (..)
f0NoneInt : Int -> Int
f0NoneInt x = x + 1
f0NoneLong : Int -> Int
f0NoneLong x = x + 2
pick : Int -> Int -> Int
pick a b = a * 10 + b
first : List Int -> Int
first values = List.length values
count : List Int -> Int
count values = List.length values
intCall : Int -> Int
intCall x = f0NoneInt x
longCall : Int -> Int
longCall x = f0NoneLong x
named : Int
named = pick 5 4
default : Int
default = pick 2 3
partial : Int
partial = pick 2 7
expanded : Int
expanded = first [1,2,3]
empty : Int
empty = count []
explicit : Int
explicit = first [4,5]
