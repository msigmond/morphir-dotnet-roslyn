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
n = N.fromString
p = P.fromString
variable = T.Variable () (n "t")
box = T.Reference () (p "Example",p "D10",n "Box") [variable]
var t name = V.Variable t (n name)
value name inputs result body = (n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values = {access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
reference =
    let
        types=[(n "Box",{access=Public,value={doc="",value=T.TypeAliasDefinition [n "t"] (T.Record () [{name=n "value",tpe=variable}])}})]
        read=value "Read" [(n "receiver",box,box)] variable (V.Field variable (var box "receiver") (n "value"))
        identity name=value name [(n "value",variable,variable)] variable (var variable "value")
        wrap=value "Wrap" [(n "value",variable,variable)] box (V.Record box (Dict.singleton (n "value") (var variable "value")))
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition types []),(p "D10.Box",moduleDefinition [] [read]),(p "D10.Rules",moduleDefinition [] [identity "Identity",identity "Reference",wrap])]}
main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("generic-values",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
