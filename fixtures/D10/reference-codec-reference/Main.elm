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
fq owner name=(p "Example",p owner,n name)
ref name=T.Reference () (fq "D10" name) []
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
maybe t=T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [t]
counter=ref "Counter"
holder=ref "Holder"
valueHolder=ref "ValueHolder"
reader=ref "IRead"
var t name=V.Variable t (n name)
input name t=(n name,t,t)
value name inputs result body=(n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
alias name fields=(n name,{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () (List.map (\(field,t)->{name=n field,tpe=t}) fields))}})
read t=value "Read" [input "receiver" t] (maybe counter) (V.Field (maybe counter) (var t "receiver") (n "value"))
branch name t owner=(V.ConstructorPattern reader (fq "D10" name) [V.AsPattern t (V.WildcardPattern t) (n "rule")],V.Apply (maybe counter) (V.Reference (T.Function () t (maybe counter)) (fq owner "Read")) (var t "rule"))
reference=
    let
        readerType=(n "IRead",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.fromList [(n "HolderRead",[(n "arg1",holder)]),(n "ValueHolderRead",[(n "arg1",valueHolder)])]}}})
        types=[alias "Counter" [("value",int)],alias "Holder" [("value",maybe counter)],alias "ValueHolder" [("value",maybe counter)],readerType]
        dispatch=value "Read" [input "receiver" reader] (maybe counter) (V.PatternMatch (maybe counter) (var reader "receiver") [branch "HolderRead" holder "D10.Holder",branch "ValueHolderRead" valueHolder "D10.ValueHolder"])
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition types []),(p "D10.Holder",moduleDefinition [] [read holder]),(p "D10.ValueHolder",moduleDefinition [] [read valueHolder]),(p "D10.IRead",moduleDefinition [] [dispatch])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("reference-interfaces",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
