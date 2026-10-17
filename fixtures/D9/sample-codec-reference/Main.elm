port module Main exposing (main)

import Dict
import Json.Encode as E
import Json.Decode as D
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (encodeVersionedDistribution,decodeVersionedDistribution)
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
        raw=ref "CurrentSupported" typeName
        initial=V.Record raw (Dict.singleton (n "value") (whole 0))
        updated=V.UpdateRecord raw initial (Dict.singleton (n "value") (var int "value"))
        result=if referenceType then just raw updated else updated
    in function name int (if referenceType then maybe raw else raw) result
read name typeName referenceType =
    let
        raw=ref "CurrentSupported" typeName
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
            ++ [(n "Owner",alias Public (T.Record () [])),(n "SampleCounter",alias Public (layout "Value" int)),(n "ConstructedData",alias Public (layout "Value" int))]
        node=ref "CurrentSupported.Owner" "Node"
        nodeDefinition=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "Node") [(n "arg1",layout "Next" (maybe node))]}
        nested=[(n "Hidden",alias Private (layout "flag" bool)),(n "Nested",alias Public (layout "Count" int)),(n "Node",{access=Private,value={doc="",value=nodeDefinition}})]
        values=[make "MakeClass" "ClassData" True,make "MakeStruct" "StructData" False,make "MakeReadonly" "ReadonlyData" False,make "MakeRecord" "RecordData" True,make "MakeValue" "RecordValue" False,make "MakeReadonlyRecord" "ReadonlyRecord" False
            ,read "ReadClass" "ClassData" True,read "ReadStruct" "StructData" False,read "ReadReadonly" "ReadonlyData" False,read "ReadRecord" "RecordData" True,read "ReadValue" "RecordValue" False,read "ReadReadonlyRecord" "ReadonlyRecord" False] ++ sampleValues
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "CurrentSupported",moduleDefinition types []),(p "CurrentSupported.Owner",moduleDefinition nested []),(p "CurrentSupported.CurrentSupportedExample",moduleDefinition [] values),(p "CurrentSupported.ConstructedData",moduleDefinition [] constructedMembers),(p "CurrentSupported.StructData",moduleDefinition [] operatorMembers)]}
-- Complete additional D9 definitions, authored against the original codec.
letValue name value body = V.LetDefinition (V.valueAttribute body) (n name) {inputTypes=[],outputType=V.valueAttribute value,body=value} body
owned t subject name = V.PatternMatch t subject [(V.ConstructorPattern (maybe t) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern t (V.WildcardPattern t) (n name)],var t name)]
public name inputs outputType body = (n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=outputType,body=body}}})
constructed = ref "CurrentSupported" "ConstructedData"
constructorBody value = V.UpdateRecord constructed (V.Record constructed (Dict.singleton (n "value") (whole 0))) (Dict.singleton (n "value") value)
constructedMembers =
    [public "New__0__None_global::System.Int32" [(n "value",int,int)] constructed (constructorBody (var int "value"))
    ,public "GetRead__0__" [(n "receiver",constructed,constructed)] int (V.Field int (var constructed "receiver") (n "value"))]
structData = ref "CurrentSupported" "StructData"
operatorName = "op_LogicalNot__0__None_global::Example.CurrentSupported.StructData"
conversionName = "op_Explicit__0__None_global::Example.CurrentSupported.StructData"
operatorMembers =
    let
        equal=V.Reference (T.Function () int (T.Function () int bool)) (p "Morphir.SDK",p "Basics",n "equal")
        field=V.Field int (var structData "value") (n "value")
    in [public operatorName [(n "value",structData,structData)] bool (V.Apply bool (V.Apply (T.Function () int bool) equal field) (whole 0)),public conversionName [(n "value",structData,structData)] int field]
sampleValues =
    let
        counter=ref "CurrentSupported" "SampleCounter"
        initial=V.UpdateRecord counter (V.Record counter (Dict.singleton (n "value") (whole 0))) (Dict.singleton (n "value") (whole 1))
        first=owned counter (var (maybe counter) "local_counter") "ownedData"
        aliased=owned counter (var (maybe counter) "local_alias") "ownedData_1"
        updated=V.UpdateRecord counter aliased (Dict.singleton (n "value") (whole 2))
        aliasBody=letValue "local_counter" (just counter initial) (letValue "local_alias" (just counter first) (V.Field int updated (n "value")))
        getter=V.Reference (T.Function () constructed int) (p "Example",p "CurrentSupported.ConstructedData",n "GetRead__0__")
        constructedBody=letValue "parameter_value" (var int "value") (V.Apply int getter (constructorBody (var int "parameter_value")))
        record=ref "CurrentSupported" "RecordValue"
        recordValue value=V.UpdateRecord record (V.Record record (Dict.singleton (n "value") (whole 0))) (Dict.singleton (n "value") value)
        left=V.Field int (recordValue (var int "a")) (n "value")
        right=V.Field int (recordValue (var int "b")) (n "value")
        equal=V.Reference (T.Function () int (T.Function () int bool)) (p "Morphir.SDK",p "Basics",n "equal")
        compared=V.Apply bool (V.Apply (T.Function () int bool) equal left) right
        equality=V.IfThenElse bool compared (V.Literal bool (L.BoolLiteral True)) (V.Literal bool (L.BoolLiteral False))
        copied=V.UpdateRecord record (V.UpdateRecord record (var record "local_original") Dict.empty) (Dict.singleton (n "value") (whole 2))
        withBody=letValue "local_original" (recordValue (var int "value")) (letValue "local_copy" copied (V.Field int (var record "local_copy") (n "value")))
    in [public "DataAlias" [] int aliasBody,function "DataConstructed" int int constructedBody,public "DataRecordsEqual" [(n "a",int,int),(n "b",int,int)] bool equality,function "DataWith" int int withBody
        ,function "DataOperator" int bool (V.Apply bool (V.Reference (T.Function () structData bool) (p "Example",p "CurrentSupported.StructData",n operatorName)) (V.UpdateRecord structData (V.Record structData (Dict.singleton (n "value") (whole 0))) (Dict.singleton (n "value") (var int "value"))))
        ,function "DataConvert" int int (V.Apply int (V.Reference (T.Function () structData int) (p "Example",p "CurrentSupported.StructData",n conversionName)) (V.UpdateRecord structData (V.Record structData (Dict.singleton (n "value") (whole 0))) (Dict.singleton (n "value") (var int "value"))))]
references previous =
    case (previous,reference) of
        (Library package dependencies baseline,Library _ _ increment) ->
            let
                merge path addition modules =
                    Dict.update path (\existing -> Just (case existing of
                        Nothing -> addition
                        Just old ->
                            let
                                first=old.value
                                second=addition.value
                            in if List.any (\key -> Dict.member key first.types) (Dict.keys second.types) || List.any (\key -> Dict.member key first.values) (Dict.keys second.values) then Debug.todo "Independent cumulative increment collides with a prior definition" else {old|value={first|types=Dict.union second.types first.types,values=Dict.union second.values first.values}})) modules
                combined=Dict.foldl merge baseline.modules increment.modules
            in Library package dependencies {modules=combined}
main : Program E.Value () Never
main=Platform.worker {init=\flags -> ((),output (case D.decodeValue (D.field "previous" decodeVersionedDistribution) flags of
    Ok previous -> E.object [("sample",encodeVersionedDistribution (references previous))]
    Err error -> E.object [("error",E.string (D.errorToString error))])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
