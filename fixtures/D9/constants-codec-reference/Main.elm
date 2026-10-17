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
bool = T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
string = T.Reference () (p "Morphir.SDK",p "String",n "String") []
value access name t body = (n name,{access=access,value={doc="",value={inputTypes=[],outputType=t,body=body}}})
moduleDefinition types values = {access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
reference =
    let
        types=[(n "Limits",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [])}})]
        constants=[value Public "Minimum" int (V.Literal int (L.WholeNumberLiteral 18)),value Private "Enabled" bool (V.Literal bool (L.BoolLiteral True)),value Public "Label" string (V.Literal string (L.StringLiteral "adult"))]
        reads=[value Public "Read" int (V.Reference int (p "Example",p "D9.Limits",n "Minimum")),value Public "Text" string (V.Reference string (p "Example",p "D9.Limits",n "Label"))]
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D9",moduleDefinition types []),(p "D9.Limits",moduleDefinition [] constants),(p "D9.Rules",moduleDefinition [] reads)]}
main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("constants",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
