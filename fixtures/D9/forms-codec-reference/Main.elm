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
maybe t = T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [t]
ref owner name = T.Reference () (p "Example",p owner,n name) []
whole value = V.Literal int (L.WholeNumberLiteral value)
var t name = V.Variable t (n name)
just t value = V.Apply (maybe t) (V.Constructor (T.Function () t (maybe t)) (p "Morphir.SDK",p "Maybe",n "Just")) value
layout name t = T.Record () [{name=n name,tpe=t}]
alias access t = {access=access,value={doc="",value=T.TypeAliasDefinition [] t}}
moduleDefinition types values = {access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
function name input outputType body = (n name,{access=Public,value={doc="",value={inputTypes=[(n "value",input,input)],outputType=outputType,body=body}}})
make name typeName referenceType =
    let
        raw=ref "D9" typeName
        initial=V.Record raw (Dict.singleton (n "value") (whole 0))
        updated=V.UpdateRecord raw initial (Dict.singleton (n "value") (var int "value"))
        result=if referenceType then just raw updated else updated
    in function name int (if referenceType then maybe raw else raw) result
read name typeName referenceType =
    let
        raw=ref "D9" typeName
        input=if referenceType then maybe raw else raw
        result=if referenceType then
            V.PatternMatch int (var input "value")
                [(V.ConstructorPattern input (p "Morphir.SDK",p "Maybe",n "Nothing") [],whole 0)
                ,(V.ConstructorPattern input (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern raw (V.WildcardPattern raw) (n "presentValue")],V.Field int (var raw "presentValue") (n "value"))]
            else V.Field int (var raw "value") (n "value")
    in function name input int result
reference =
    let
        types=List.map (\name -> (n name,alias Public (layout "Value" int))) ["ClassData","StructData","ReadonlyData","RecordData","RecordValue","ReadonlyRecord"]
            ++ [(n "Owner",alias Public (T.Record () []))]
        node=ref "D9.Owner" "Node"
        nodeDefinition=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "Node") [(n "arg1",layout "Next" (maybe node))]}
        nested=[(n "Hidden",alias Private (layout "flag" bool)),(n "Nested",alias Public (layout "Count" int)),(n "Node",{access=Private,value={doc="",value=nodeDefinition}})]
        values=[make "MakeClass" "ClassData" True,make "MakeStruct" "StructData" False,make "MakeReadonly" "ReadonlyData" False,make "MakeRecord" "RecordData" True,make "MakeValue" "RecordValue" False,make "MakeReadonlyRecord" "ReadonlyRecord" False
            ,read "ReadClass" "ClassData" True,read "ReadStruct" "StructData" False,read "ReadReadonly" "ReadonlyData" False,read "ReadRecord" "RecordData" True,read "ReadValue" "RecordValue" False,read "ReadReadonlyRecord" "ReadonlyRecord" False]
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D9",moduleDefinition types []),(p "D9.Owner",moduleDefinition nested []),(p "D9.Rules",moduleDefinition [] values)]}
main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("data-forms",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
