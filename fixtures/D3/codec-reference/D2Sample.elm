module D2Sample exposing (sample)

import D1Sample
import Dict
import Json.Encode as E
import Morphir.IR.AccessControlled exposing (Access(..))
import Morphir.IR.Distribution exposing (Distribution(..))
import Morphir.IR.FormatVersion.Codec exposing (encodeVersionedDistribution)
import Morphir.IR.Name as N
import Morphir.IR.Path as P
import Morphir.IR.Type as T
import Morphir.IR.Value as V
import Morphir.IR.Literal as L
import Platform

n = N.fromString
p = P.fromString
int = T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
bool = T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
empty = {types=Dict.empty,values=Dict.empty,doc=Nothing}
wrap access body = {access=access,value={doc="",value=body}}
variable name = V.Variable int (n name)
input name = (n name,int,int)
whole number = V.Literal int (L.WholeNumberLiteral number)
call moduleName local argument = V.Apply bool (V.Reference (T.Function () int bool) (p "Example",p moduleName,n local)) argument
adultBody = V.Apply bool (V.Apply (T.Function () int bool) (V.Reference (T.Function () int (T.Function () int bool)) (p "Morphir.SDK",p "Basics",n "greaterThanOrEqual")) (variable "age")) (whole 18)
function body = {inputTypes=[input "age"],outputType=bool,body=body}
constant number = {inputTypes=[],outputType=int,body=whole number}
project sharedAccess = Library (p "Example") Dict.empty
    {modules=Dict.fromList
        [ (p "D2.Business.Rules",{access=Public,value={empty|values=Dict.fromList
            [(n "Adult",wrap Private (function (call "D2.Shared.AgeRule" "adult" (variable "age"))))
            ,(n "Eligible",wrap Public (function (call "D2.Business.Rules" "adult" (variable "age"))))
            ,(n "GeneratedThreshold",wrap Public (constant 18))
            ,(n "Conditional",wrap Public (constant 18))]}})
        , (p "D2.Shared",{access=Public,value={empty|types=Dict.fromList [(n "Unused",wrap Private (T.TypeAliasDefinition [] (T.Record () [{name=n "Flag",tpe=bool}])))]}})
        , (p "D2.Shared.AgeRule",{access=sharedAccess,value={empty|values=Dict.fromList [(n "Adult",wrap sharedAccess (function adultBody))]}})
        ]}

sample = case D1Sample.sample of
    Library package dependencies definition ->
        let
            name = p "CurrentSupported.CurrentSupportedExample"
            original = Dict.get name definition.modules
            extra = Dict.fromList
                [(n "ProjectAdult",wrap Private (function (call "CurrentSupported.Shared.AgeRule" "adult" (variable "age"))))
                ,(n "ProjectEligible",wrap Public (function (call "CurrentSupported.CurrentSupportedExample" "projectAdult" (variable "age"))))
                ,(n "GeneratedThreshold",wrap Public (constant 18))]
            extended = original |> Maybe.map (\controlled -> {controlled|value=let old=controlled.value in {old|values=Dict.union extra old.values}})
            withMain = case extended of
                Just mainModule -> Dict.insert name mainModule definition.modules
                Nothing -> definition.modules
            shared = {access=Private,value={empty|values=Dict.fromList [(n "Adult",wrap Private (function adultBody))]}}
        in Library package dependencies {modules=Dict.insert (p "CurrentSupported.Shared.AgeRule") shared withMain}
