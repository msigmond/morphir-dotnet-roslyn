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
bool = T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
witnessType = T.Function () variable bool
var t name = V.Variable t (n name)
condition = V.Apply bool (var witnessType "isNull_t") (var variable "value")
input name t = (n name,t,t)
value name inputs result body = (n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
reference =
    let
        inputs=[input "isNull_t" witnessType,input "value" variable]
        missing=value "Missing" inputs bool condition
        or=value "Or" (inputs++[input "fallback" variable]) variable (V.IfThenElse variable condition (var variable "fallback") (var variable "value"))
        moduleDefinition={access=Public,value={types=Dict.empty,values=Dict.fromList [missing,or],doc=Nothing}}
    in Library (p "Example") Dict.empty {modules=Dict.singleton (p "D10.Presence") moduleDefinition}
main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("generic-presence",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
