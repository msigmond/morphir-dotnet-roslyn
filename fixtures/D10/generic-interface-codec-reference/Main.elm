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
fq owner name=(p "Example",p owner,n name)
ref name argument=T.Reference () (fq "D10" name) [argument]
t=T.Variable () (n "t")
u=T.Variable () (n "u")
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
var kind name=V.Variable kind (n name)
input name kind=(n name,kind,kind)
value name inputs result body=(n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
alias name fields=(n name,{access=Public,value={doc="",value=T.TypeAliasDefinition [n "u"] (T.Record () (List.map (\(field,kind)->{name=n field,tpe=kind}) fields))}})
branch name owner needsDefault=
    let
        payload=ref name t
        signature=T.Function () payload t
        method=V.Reference (if needsDefault then T.Function () t signature else signature) (fq owner "Read")
        function=if needsDefault then V.Apply signature method (var t "default_t") else method
    in (V.ConstructorPattern (ref "IValue" t) (fq "D10" (name++"Value")) [V.AsPattern payload (V.WildcardPattern payload) (n "rule")],V.Apply t function (var payload "rule"))
reference=
    let
        union=(n "IValue",{access=Public,value={doc="",value=T.CustomTypeDefinition [n "t"] {access=Public,value=Dict.fromList [(n "DefaultHolderValue",[(n "arg1",ref "DefaultHolder" t)]),(n "ValueHolderValue",[(n "arg1",ref "ValueHolder" t)])]}}})
        types=[union,alias "ValueHolder" [("value",u)],alias "DefaultHolder" [("ignored",int)]]
        read=value "Read" [input "receiver" (ref "ValueHolder" u)] u (V.Field u (var (ref "ValueHolder" u) "receiver") (n "value"))
        defaultRead=value "Read" [input "default_u" u,input "receiver" (ref "DefaultHolder" u)] u (var u "default_u")
        dispatch=value "Read" [input "default_t" t,input "receiver" (ref "IValue" t)] t (V.PatternMatch t (var (ref "IValue" t) "receiver") [branch "ValueHolder" "D10.ValueHolder" False,branch "DefaultHolder" "D10.DefaultHolder" True])
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition types []),(p "D10.ValueHolder",moduleDefinition [] [read]),(p "D10.DefaultHolder",moduleDefinition [] [defaultRead]),(p "D10.IValue",moduleDefinition [] [dispatch])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("generic-interfaces",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
