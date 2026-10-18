port module Main exposing (main)

import Dict
import Json.Encode as E
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (encodeVersionedDistribution)
import Morphir.IR.Name as N
import Morphir.IR.Path as P
import Morphir.IR.Type as T
import Morphir.IR.Value as V
import Platform

port output : E.Value -> Cmd msg
n=N.fromString
p=P.fromString
fq owner name=(p "Example",p owner,n name)
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
marker=T.Reference () (fq "D10" "Marker") []
callback=T.Function () int int
var kind name=V.Variable kind (n name)
input name kind=(n name,kind,kind)
value name inputs result body=(n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
reference=
    let
        markerType=(n "Marker",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "ignored",tpe=int}])}})
        union=(n "IFactory",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "MarkerFactory") [(n "arg1",marker)]}}})
        make=value "Make" [input "value" int] int (var int "value")
        contract=value "Make" [input "implementation" callback,input "value" int] int (V.Apply int (var callback "implementation") (var int "value"))
        witnessName="call_t_Example.D10.IFactory_make"
        apply=value "Apply" [input witnessName callback,input "value" int] int (V.Apply int (var callback witnessName) (var int "value"))
        adapter=V.Lambda callback (V.AsPattern int (V.WildcardPattern int) (n "staticArgument0")) (V.Apply int (V.Reference callback (fq "D10.Marker" "Make")) (var int "staticArgument0"))
        run=value "Run" [input "value" int] int (V.Apply int (V.Apply callback (V.Reference (T.Function () callback callback) (fq "D10.Rules" "Apply")) adapter) (var int "value"))
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [markerType,union] []),(p "D10.Marker",moduleDefinition [] [make]),(p "D10.IFactory",moduleDefinition [] [contract]),(p "D10.Rules",moduleDefinition [] [apply,run])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("static-interfaces",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
