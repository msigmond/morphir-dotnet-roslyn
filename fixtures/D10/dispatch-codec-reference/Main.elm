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
fq moduleName name = (p "Example",p moduleName,n name)
int = T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
bool = T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
variable = T.Variable () (n "t")
box = T.Reference () (fq "D10" "Box") [variable]
minimum = T.Reference () (fq "D10" "MinimumAge") []
maximum = T.Reference () (fq "D10" "MaximumAge") []
rule = T.Reference () (fq "D10" "IRule") []
maybe t = T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [t]
var t name = V.Variable t (n name)
input name t = (n name,t,t)
value name inputs result body = (n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values = {access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
alias name parameters fields = (n name,{access=Public,value={doc="",value=T.TypeAliasDefinition (List.map n parameters) (T.Record () (List.map (\(field,t)->{name=n field,tpe=t}) fields))}})
binary name left right = V.Apply bool (V.Apply (T.Function () int bool) (V.Reference (T.Function () int (T.Function () int bool)) (p "Morphir.SDK",p "Basics",n name)) left) right
allows owner t field comparison = value "Allows" [input "receiver" t,input "age" int] bool (binary comparison (var int "age") (V.Field int (var t "receiver") (n field)))
call owner t subject = V.Apply bool (V.Apply (T.Function () int bool) (V.Reference (T.Function () t (T.Function () int bool)) (fq owner "Allows")) subject) (var int "age")
branch name t owner = (V.ConstructorPattern rule (fq "D10" name) [V.AsPattern t (V.WildcardPattern t) (n "rule")],call owner t (var t "rule"))
reference =
    let
        types=[alias "Box" ["t"] [("value",variable)],alias "MinimumAge" [] [("minimum",int)],alias "MaximumAge" [] [("maximum",int)],
            (n "IRule",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.fromList [(n "MinimumAgeRule",[(n "arg1",minimum)]),(n "MaximumAgeRule",[(n "arg1",maximum)])]}}})]
        dispatch=value "Allows" [input "receiver" rule,input "age" int] bool (V.PatternMatch bool (var rule "receiver") [branch "MinimumAgeRule" minimum "D10.MinimumAge",branch "MaximumAgeRule" maximum "D10.MaximumAge"])
        wrap=value "Wrap" [input "value" variable] box (V.Record box (Dict.singleton (n "value") (var variable "value")))
        test=value "Test" [input "rule" (maybe rule),input "age" int] bool (V.PatternMatch bool (var (maybe rule) "rule")
            [(V.ConstructorPattern (maybe rule) (p "Morphir.SDK",p "Maybe",n "Nothing") [],V.Literal bool (L.BoolLiteral False)),
             (V.ConstructorPattern (maybe rule) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern rule (V.WildcardPattern rule) (n "presentValue")],call "D10.IRule" rule (var rule "presentValue"))])
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition types []),(p "D10.MinimumAge",moduleDefinition [] [allows "D10.MinimumAge" minimum "minimum" "greaterThanOrEqual"]),(p "D10.MaximumAge",moduleDefinition [] [allows "D10.MaximumAge" maximum "maximum" "lessThanOrEqual"]),(p "D10.IRule",moduleDefinition [] [dispatch]),(p "D10.Rules",moduleDefinition [] [wrap,test])]}
main : Program () () Never
main = Platform.worker {init=\_ -> ((),output (E.object [("closed-rules",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
