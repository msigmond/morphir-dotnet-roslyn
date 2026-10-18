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
ref name=T.Reference () (fq "D10" name) []
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
bool=T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
var kind name=V.Variable kind (n name)
input name kind=(n name,kind,kind)
value access name inputs result body=(n name,{access=access,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
alias name=(n name,{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "bound",tpe=int}])}})
call owner name receiverKind receiver=V.Apply bool (V.Apply (T.Function () int bool) (V.Reference (T.Function () receiverKind (T.Function () int bool)) (fq owner name)) receiver) (var int "age")
check name comparison=
    let
        body=V.Apply bool (V.Apply (T.Function () int bool) (V.Reference (T.Function () int (T.Function () int bool)) (p "Morphir.SDK",p "Basics",n comparison)) (var int "age")) (V.Field int (var (ref name) "receiver") (n "bound"))
    in value Public "Check" [input "receiver" (ref name),input "age" int] bool body
branch name defaultBody=
    let
        raw=ref "IRule"
        payload=var (ref name) "rule"
        constructor=V.Apply raw (V.Constructor (T.Function () (ref name) raw) (fq "D10" (name++"Rule"))) payload
        body=if defaultBody then call "D10.IRule" "AllowsDefaultBody" raw constructor else call ("D10."++name) "Check" (ref name) payload
    in (V.ConstructorPattern raw (fq "D10" (name++"Rule")) [V.AsPattern (ref name) (V.WildcardPattern (ref name)) (n "rule")],body)
reference=
    let
        union=(n "IRule",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.fromList [(n "MinimumRule",[(n "arg1",ref "Minimum")]),(n "MaximumRule",[(n "arg1",ref "Maximum")])]}}})
        inputs=[input "receiver" (ref "IRule"),input "age" int]
        dispatch name defaultBody=value Public name inputs bool (V.PatternMatch bool (var (ref "IRule") "receiver") [branch "Minimum" defaultBody,branch "Maximum" defaultBody])
        helper=value Private "AllowsDefaultBody" inputs bool (call "D10.IRule" "Check" (ref "IRule") (var (ref "IRule") "receiver"))
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [alias "Minimum",alias "Maximum",union] []),(p "D10.Minimum",moduleDefinition [] [check "Minimum" "greaterThanOrEqual"]),(p "D10.Maximum",moduleDefinition [] [check "Maximum" "lessThanOrEqual"]),(p "D10.IRule",moduleDefinition [] [dispatch "Check" False,dispatch "Allows" True,helper])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("default-interfaces",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
