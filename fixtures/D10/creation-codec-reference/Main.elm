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
unit = T.Unit ()
witnessType = T.Function () unit variable
witness = V.Variable witnessType (n "new_t")
value name body = (n name,{access=Public,value={doc="",value={inputTypes=[(n "new_t",witnessType,witnessType)],outputType=variable,body=body}}})
reference =
    let
        create=value "Create" (V.Apply variable witness (V.Unit unit))
        forward=value "Forward" (V.Apply variable (V.Reference (T.Function () witnessType variable) (p "Example",p "D10.Creation",n "Create")) witness)
        moduleDefinition={access=Public,value={types=Dict.empty,values=Dict.fromList [create,forward],doc=Nothing}}
    in Library (p "Example") Dict.empty {modules=Dict.singleton (p "D10.Creation") moduleDefinition}
main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("generic-creation",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
