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
t=T.Variable () (n "t")
u=T.Variable () (n "u")
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
var kind name=V.Variable kind (n name)
input name kind=(n name,kind,kind)
value name inputs result body=(n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
pattern=V.ConstructorPattern (ref "IMapper") (fq "D10" "MapperMapper") [V.AsPattern (ref "Mapper") (V.WildcardPattern (ref "Mapper")) (n "rule")]
reference=
    let
        mapper=(n "Mapper",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "ignored",tpe=int}])}})
        union=(n "IMapper",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.singleton (n "MapperMapper") [(n "arg1",ref "Mapper")]}}})
        echo=value "Echo" [input "receiver" (ref "Mapper"),input "value" u] u (var u "value")
        zero=value "Zero" [input "default_u" u,input "receiver" (ref "Mapper")] u (var u "default_u")
        echoCall=V.Apply t (V.Apply (T.Function () t t) (V.Reference (T.Function () (ref "Mapper") (T.Function () t t)) (fq "D10.Mapper" "Echo")) (var (ref "Mapper") "rule")) (var t "value")
        zeroCall=V.Apply t (V.Apply (T.Function () (ref "Mapper") t) (V.Reference (T.Function () t (T.Function () (ref "Mapper") t)) (fq "D10.Mapper" "Zero")) (var t "default_t")) (var (ref "Mapper") "rule")
        dispatchEcho=value "Echo" [input "receiver" (ref "IMapper"),input "value" t] t (V.PatternMatch t (var (ref "IMapper") "receiver") [(pattern,echoCall)])
        dispatchZero=value "Zero" [input "default_t" t,input "receiver" (ref "IMapper")] t (V.PatternMatch t (var (ref "IMapper") "receiver") [(pattern,zeroCall)])
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [mapper,union] []),(p "D10.Mapper",moduleDefinition [] [echo,zero]),(p "D10.IMapper",moduleDefinition [] [dispatchEcho,dispatchZero])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("generic-members",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
