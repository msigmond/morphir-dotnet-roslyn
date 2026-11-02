module Example.Parity.Rules exposing (..)
sum : List Int -> Int -> Int
sum values seed = List.foldl (\item state -> (\total value -> total + value) state item) seed values
captured : List Int -> Int -> Int -> Int
captured values seed extra = List.foldl (\item state -> (\total value -> total + value + extra) state item) seed values
callback : List Int -> String -> (String -> Int -> String) -> String
callback values seed callback = List.foldl (\item state -> callback state item) seed values
method : List Int -> Int -> Int
method values seed = List.foldl (\item state -> subtract state item) seed values
subtract : Int -> Int -> Int
subtract total value = total - value
nested : List Int -> Int -> Int
nested values seed = List.foldl (\item state -> (\total value -> total + value) state item) seed (List.filter (\value -> value > 0) values)
names : List Int -> Int -> Int
names values seed = List.foldl (\item1 state1 -> (\state item -> state + item) state1 item1) seed values
state : Int -> Int -> Int
state total value = total - value
namedState : List Int -> Int -> Int
namedState values seed = List.foldl (\item state1 -> state state1 item) seed values
localName : List Int -> Int -> Int
localName values seed =
    List.foldl
        (\item state1 ->
            (\total value ->
                let
                    state : Int
                    state = 1
                in
                total + value
            ) state1 item
        ) seed values
