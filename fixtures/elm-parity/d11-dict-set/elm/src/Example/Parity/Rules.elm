module Example.Parity.Rules exposing (..)
import Dict exposing (Dict)
import Set exposing (Set)
dictionary : Int -> String -> Dict Int String
dictionary key value = Dict.fromList [(key,value)]
indexInitializer : Int -> String -> Dict Int String
indexInitializer key value = Dict.fromList [(key,value)]
emptyDictionary : Dict Int String
emptyDictionary = Dict.empty
read : Dict Int String -> Int -> Maybe String
read items key = Dict.get key items
containsKey : Dict Int String -> Int -> Bool
containsKey items key = Dict.member key items
count : Dict Int String -> Int
count items = Dict.size items
keys : Dict Int String -> List Int
keys items = Dict.keys items
values : Dict Int String -> List String
values items = Dict.values items
set : Int -> Set Int
set value = Set.fromList [value,2]
setFromList : List Int -> Set Int
setFromList items = Set.fromList items
immutable : Int -> Set Int
immutable value = Set.fromList [value,2]
contains : Set Int -> Int -> Bool
contains items value = Set.member value items
setCount : Set Int -> Int
setCount items = Set.size items
isEmpty : Set Int -> Bool
isEmpty items = Set.isEmpty items
setSpread : Set Int -> List Int
setSpread items = List.concat [Set.toList items]
emptyImmutable : Dict Int String
emptyImmutable = Dict.empty
identity : Dict k v -> Dict k v
identity items = items

readGeneric : Dict k v -> k -> Maybe v
readGeneric items key = Dict.get key items
containsGeneric : Set t -> t -> Bool
containsGeneric items value = Set.member value items
