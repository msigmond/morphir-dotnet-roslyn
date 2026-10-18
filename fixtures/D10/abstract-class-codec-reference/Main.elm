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
n=N.fromString
p=P.fromString
fq owner name=(p "Example",p owner,n name)
ref name=T.Reference () (fq "D10" name) []
int=T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
bool=T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
maybe kind=T.Reference () (p "Morphir.SDK",p "Maybe",n "Maybe") [kind]
var kind name=V.Variable kind (n name)
input name kind=(n name,kind,kind)
whole number=V.Literal int (L.WholeNumberLiteral number)
value name inputs result body=(n name,{access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}})
moduleDefinition types values={access=Public,value={types=Dict.fromList types,values=Dict.fromList values,doc=Nothing}}
alias name=(n name,{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [])}})
binary name left right=V.Apply bool (V.Apply (T.Function () int bool) (V.Reference (T.Function () int (T.Function () int bool)) (p "Morphir.SDK",p "Basics",n name)) left) right
allows owner compare boundary=value "Allows" [input "receiver" (ref owner),input "age" int] bool (binary compare (var int "age") (whole boundary))
call owner kind receiver=V.Apply bool (V.Apply (T.Function () int bool) (V.Reference (T.Function () kind (T.Function () int bool)) (fq owner "Allows")) receiver) (var int "age")
branch name kind owner=(V.ConstructorPattern (ref "Rule") (fq "D10" name) [V.AsPattern kind (V.WildcardPattern kind) (n "rule")],call owner kind (var kind "rule"))
reference=
    let
        union=(n "Rule",{access=Public,value={doc="",value=T.CustomTypeDefinition [] {access=Public,value=Dict.fromList [(n "MinimumAgeRule",[(n "arg1",ref "MinimumAge")]),(n "MaximumAgeRule",[(n "arg1",ref "MaximumAge")])]}}})
        dispatch=value "Allows" [input "receiver" (ref "Rule"),input "age" int] bool (V.PatternMatch bool (var (ref "Rule") "receiver") [branch "MinimumAgeRule" (ref "MinimumAge") "D10.MinimumAge",branch "MaximumAgeRule" (ref "MaximumAge") "D10.MaximumAge"])
        test=value "Test" [input "rule" (maybe (ref "Rule")),input "age" int] bool (V.PatternMatch bool (var (maybe (ref "Rule")) "rule") [(V.ConstructorPattern (maybe (ref "Rule")) (p "Morphir.SDK",p "Maybe",n "Nothing") [],V.Literal bool (L.BoolLiteral False)),(V.ConstructorPattern (maybe (ref "Rule")) (p "Morphir.SDK",p "Maybe",n "Just") [V.AsPattern (ref "Rule") (V.WildcardPattern (ref "Rule")) (n "presentValue")],call "D10.Rule" (ref "Rule") (var (ref "Rule") "presentValue"))])
        minimum=V.Record (ref "MinimumAge") Dict.empty
        tagged=V.Apply (ref "Rule") (V.Constructor (T.Function () (ref "MinimumAge") (ref "Rule")) (fq "D10" "MinimumAgeRule")) minimum
        present=V.Apply (maybe (ref "Rule")) (V.Constructor (T.Function () (ref "Rule") (maybe (ref "Rule"))) (p "Morphir.SDK",p "Maybe",n "Just")) tagged
        example=value "Example" [input "age" int] bool (V.Apply bool (V.Apply (T.Function () int bool) (V.Reference (T.Function () (maybe (ref "Rule")) (T.Function () int bool)) (fq "D10.Rules" "Test")) present) (var int "age"))
    in Library (p "Example") Dict.empty {modules=Dict.fromList [(p "D10",moduleDefinition [alias "MinimumAge",alias "MaximumAge",union] []),(p "D10.MinimumAge",moduleDefinition [] [allows "MinimumAge" "greaterThanOrEqual" 18]),(p "D10.MaximumAge",moduleDefinition [] [allows "MaximumAge" "lessThanOrEqual" 65]),(p "D10.Rule",moduleDefinition [] [dispatch]),(p "D10.Rules",moduleDefinition [] [test,example])]}
main : Program () () Never
main=Platform.worker {init=\_ -> ((),output (E.object [("abstract-classes",encodeVersionedDistribution reference)])),update=\_ state -> (state,Cmd.none),subscriptions=\_ -> Sub.none}
