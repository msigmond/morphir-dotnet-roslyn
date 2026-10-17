port module Main exposing (main)

import Dict
import Json.Encode as E
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (encodeVersionedDistribution)
import Morphir.IR.Literal as L
import Morphir.IR.Name as N
import Morphir.IR.Path as P
import Morphir.IR.Type as T
import Morphir.IR.Value as V
import Platform

port output : E.Value -> Cmd msg
n = N.fromString
p = P.fromString
int = T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
counter = T.Reference () (p "Example",p "D9",n "Counter") []
slot = T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [counter]
whole value = V.Literal int (L.WholeNumberLiteral value)
var t name = V.Variable t (n name)
just value = V.Apply slot (V.Constructor (T.Function () counter slot) (p "Morphir.SDK",p "Maybe",n "Just")) value
-- Only an actual owned allocation or a local alias to it authorizes this
-- single-constructor extraction. No absent case/default is fabricated.
owned subject name = V.PatternMatch counter subject
    [(V.ConstructorPattern slot (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern counter (V.WildcardPattern counter) (n name)],var counter name)]
letValue name value body = V.LetDefinition (V.valueAttribute body) (n name) {inputTypes=[],outputType=V.valueAttribute value,body=value} body
moduleDefinition types values = {access=Public,value={types=types,values=values,doc=Nothing}}
reference =
    let
        layout=T.Record () [{name=n "value",tpe=int}]
        types=Dict.singleton (n "Counter") {access=Public,value={doc="",value=T.TypeAliasDefinition [] layout}}
        zero=V.Record counter (Dict.singleton (n "value") (whole 0))
        initialized=V.UpdateRecord counter zero (Dict.singleton (n "value") (whole 1))
        first=owned (var slot "local_counter") "ownedData"
        alias=owned (var slot "local_alias") "ownedData_1"
        updated=V.UpdateRecord counter alias (Dict.singleton (n "value") (whole 2))
        body=letValue "local_counter" (just initialized)
            (letValue "local_alias" (just first) (V.Field int updated (n "value")))
        values=Dict.singleton (n "BuildAndUpdate") {access=Public,value={doc="",value={inputTypes=[],outputType=int,body=body}}}
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D9",moduleDefinition types Dict.empty),(p "D9.Rules",moduleDefinition Dict.empty values)]}
main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("counter",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
