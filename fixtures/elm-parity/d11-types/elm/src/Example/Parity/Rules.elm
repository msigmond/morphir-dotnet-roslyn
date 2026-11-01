module Example.Parity.Rules exposing (..)
import Dict exposing (Dict)
import Set exposing (Set)
list : List Int -> List Int
list items = items
missingList : Maybe (List Int)
missingList = Nothing
dictionary : Dict Int String -> Dict Int String
dictionary items = items
missingDictionary : Maybe (Dict Int String)
missingDictionary = Nothing
hashSet : Set Int -> Set Int
hashSet items = items
missingHashSet : Maybe (Set Int)
missingHashSet = Nothing
iEnumerable : List Int -> List Int
iEnumerable items = items
missingIEnumerable : Maybe (List Int)
missingIEnumerable = Nothing
iCollection : List Int -> List Int
iCollection items = items
missingICollection : Maybe (List Int)
missingICollection = Nothing
iList : List Int -> List Int
iList items = items
missingIList : Maybe (List Int)
missingIList = Nothing
iDictionary : Dict Int String -> Dict Int String
iDictionary items = items
missingIDictionary : Maybe (Dict Int String)
missingIDictionary = Nothing
iSet : Set Int -> Set Int
iSet items = items
missingISet : Maybe (Set Int)
missingISet = Nothing
iReadOnlyCollection : List Int -> List Int
iReadOnlyCollection items = items
missingIReadOnlyCollection : Maybe (List Int)
missingIReadOnlyCollection = Nothing
iReadOnlyList : List Int -> List Int
iReadOnlyList items = items
missingIReadOnlyList : Maybe (List Int)
missingIReadOnlyList = Nothing
iReadOnlyDictionary : Dict Int String -> Dict Int String
iReadOnlyDictionary items = items
missingIReadOnlyDictionary : Maybe (Dict Int String)
missingIReadOnlyDictionary = Nothing
iReadOnlySet : Set Int -> Set Int
iReadOnlySet items = items
missingIReadOnlySet : Maybe (Set Int)
missingIReadOnlySet = Nothing
readOnlyCollection : List Int -> List Int
readOnlyCollection items = items
missingReadOnlyCollection : Maybe (List Int)
missingReadOnlyCollection = Nothing
readOnlyDictionary : Dict Int String -> Dict Int String
readOnlyDictionary items = items
missingReadOnlyDictionary : Maybe (Dict Int String)
missingReadOnlyDictionary = Nothing
immutableArray : List Int -> List Int
immutableArray items = items
missingImmutableArray : Maybe (List Int)
missingImmutableArray = Nothing
immutableList : List Int -> List Int
immutableList items = items
missingImmutableList : Maybe (List Int)
missingImmutableList = Nothing
immutableDictionary : Dict Int String -> Dict Int String
immutableDictionary items = items
missingImmutableDictionary : Maybe (Dict Int String)
missingImmutableDictionary = Nothing
immutableHashSet : Set Int -> Set Int
immutableHashSet items = items
missingImmutableHashSet : Maybe (Set Int)
missingImmutableHashSet = Nothing
