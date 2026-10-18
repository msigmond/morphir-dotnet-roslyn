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
ref name = T.Reference () (p "Example",p "D10",n name) [variable]
cell = ref "Cell"
pair = ref "Pair"
maybe t = T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [t]
var t name = V.Variable t (n name)
input name = (n name,variable,variable)
value name inputs result body = (n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values = {access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
alias name = (n name,{access=Public,value={doc="",value=T.TypeAliasDefinition [n "t"] (T.Record () [{name=n "value",tpe=variable}])}})
just t body = V.Apply (maybe t) (V.Constructor (T.Function () t (maybe t)) (p "Morphir.SDK",p "Maybe",n "Just")) body
reference =
    let
        default=var variable "default_t"
        zero t=V.Record t (Dict.singleton (n "value") default)
        defaults=value "Default" [input "default_t"] variable default
        create=value "Create" [input "default_t",input "value"] (maybe cell) (just cell (V.UpdateRecord cell (zero cell) (Dict.singleton (n "value") (var variable "value"))))
        newPair=value "NewPair" [input "default_t"] pair (zero pair)
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [alias "Cell",alias "Pair"] []),(p "D10.Storage",moduleDefinition [] [defaults,create,newPair])]}
main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("generic-storage",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
