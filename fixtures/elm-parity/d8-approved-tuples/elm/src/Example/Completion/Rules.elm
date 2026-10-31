module Example.Completion.Rules exposing (..)
make : Int -> Bool -> (Int,Bool)
make code flag = (code,flag)
construct : Int -> Bool -> (Int,Bool)
construct code flag = (code,flag)
code : (Int,Bool) -> Int
code pair = Tuple.first pair
flag : (Int,Bool) -> Bool
flag pair = Tuple.second pair

triple : Int -> Bool -> String -> (Int,Bool,String)
triple code flag text = (code,flag,text)
