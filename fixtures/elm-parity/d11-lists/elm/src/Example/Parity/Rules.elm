module Example.Parity.Rules exposing (..)
import Example.System.Collections.Immutable.UserFactory as UserFactory
array : Int -> Int -> List Int
array first second = [first,second]
inferred : Int -> List Int
inferred value = [value]
local : Int -> List Int
local value = let items : List Int
                  items = [value]
              in items
literal : Int -> List Int
literal value = [value,2]
expression : Int -> List Int
expression value = [value,2]
spread : Int -> List Int -> List Int
spread value items = List.concat [[value],items,[2]]
generic : t -> List t -> List t
generic value items = List.concat [[value],items]
nullableElements : Maybe String -> List (Maybe String)
nullableElements value = [value]
length : List Int -> Int
length items = List.length items
count : List Int -> Int
count items = List.length items
contains : List Int -> Int -> Bool
contains items value = List.member value items
copy : List Int -> List Int
copy items = items
readOnly : List Int -> List Int
readOnly items = items
immutable : Int -> List Int
immutable value = [value,2]
immutableArrayLiteral : Int -> List Int
immutableArrayLiteral value = [value,2]
empty : List Int
empty = []
emptyArray : List Int
emptyArray = []
read : List Int -> Int -> Maybe Int
read items index = List.head (List.drop index items)
readList : List Int -> Int -> Maybe Int
readList items index = List.head (List.drop index items)
readParams : Int -> List Int -> Maybe Int
readParams index items = List.head (List.drop index items)
nullableCount : Maybe (List Int) -> Maybe Int
nullableCount items = Maybe.map (\item -> List.length item) items
missing : Maybe (List Int)
missing = Nothing

nullableContains : Maybe (List Int) -> Int -> Maybe Bool
nullableContains items value = Maybe.map (\item -> List.member value item) items
containsNullable : List (Maybe Int) -> Int -> Bool
containsNullable items value = List.member (Just value) items

useSourceFactory : Int -> List Int
useSourceFactory value = UserFactory.create value

sized : Int -> Int -> List Int
sized first second = [first,second]
