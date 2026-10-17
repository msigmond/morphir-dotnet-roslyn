module D4Sample exposing (sample)

import D3Sample
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
fq name = (p "Morphir.SDK",p "Maybe",n name)
int = T.Reference () (p "Morphir.SDK",p "Basics",n "Int") []
bool = T.Reference () (p "Morphir.SDK",p "Basics",n "Bool") []
string = T.Reference () (p "Morphir.SDK",p "String",n "String") []
maybe t = T.Reference () (fq "Maybe") [t]
optional = T.Reference () (p "Example",p "CurrentSupported",n "OptionalCustomer") []
annotated = T.Reference () (p "Example",p "CurrentSupported",n "AnnotatedReference") []
var t name = V.Variable t (n name)
whole value = V.Literal int (L.WholeNumberLiteral value)
boolean value = V.Literal bool (L.BoolLiteral value)
text value = V.Literal string (L.StringLiteral value)
nothing t = V.Constructor (maybe t) (fq "Nothing")
just t value = V.Apply (maybe t) (V.Constructor (T.Function () t (maybe t)) (fq "Just")) value
field t owner name = V.Field t owner (n name)
input name t = (n name,t,t)
wrap inputs result body = {access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}}
letIn result name boundType bound body = V.LetDefinition result (n name) {inputTypes=[],outputType=boundType,body=bound} body
binary name arg result left right = V.Apply result (V.Apply (T.Function () arg result) (V.Reference (T.Function () arg (T.Function () arg result)) (p "Morphir.SDK",p "Basics",n name)) left) right
not value = V.Apply bool (V.Reference (T.Function () bool bool) (p "Morphir.SDK",p "Basics",n "not")) value
length value = V.Apply int (V.Reference (T.Function () string int) (p "Morphir.SDK",p "String",n "length")) value
caseMaybe t result subject name absent present = V.PatternMatch result subject
    [(V.ConstructorPattern (maybe t) (fq "Nothing") [],absent)
    ,(V.ConstructorPattern (maybe t) (fq "Just") [V.AsPattern t (V.WildcardPattern t) (n name)],present)]
data = var optional "data"
age = field (maybe int) data "Age"
region = field (maybe string) data "Region"
defaultOptional = V.Record optional (Dict.fromList [(n "age",nothing int),(n "region",nothing string)])
constructOptional = V.Record optional (Dict.fromList [(n "age",just int (whole 18)),(n "region",just string (text "UK"))])
adult = caseMaybe int bool age "presentValue" (boolean False) (binary "greaterThanOrEqual" int bool (var int "presentValue") (whole 18))
regionDefault = caseMaybe string string region "presentValue" (text "unknown") (var string "presentValue")
regionLength = caseMaybe string int region "presentValue"
    (whole 99)
    (length (var string "presentValue"))
guardedAnnotation = caseMaybe string int (field (maybe string) (var annotated "data") "Region") "presentValue" (whole 0) (length (var string "presentValue"))
addZero = caseMaybe int (maybe int) (var (maybe int) "value") "presentValue" (nothing int)
    (just int (binary "add" int int (var int "presentValue") (whole 0)))
equal = caseMaybe int bool (var (maybe int) "a") "presentValue1"
    (caseMaybe int bool (var (maybe int) "b") "presentValue" (boolean True) (boolean False))
    (caseMaybe int bool (var (maybe int) "b") "presentValue2" (boolean False) (binary "equal" int bool (var int "presentValue1") (var int "presentValue2")))
greater = caseMaybe int bool (var (maybe int) "a") "presentValue" (boolean False)
    (caseMaybe int bool (var (maybe int) "b") "presentValue1" (boolean False) (binary "greaterThanOrEqual" int bool (var int "presentValue") (var int "presentValue1")))
nullableBoolean isAnd =
    let
        missing = nothing bool
        present name = just bool (var bool name)
        choose value = if isAnd then V.IfThenElse (maybe bool) value missing (just bool value) else V.IfThenElse (maybe bool) value (just bool value) missing
    in
    caseMaybe bool (maybe bool) (var (maybe bool) "a") "presentValue1"
        (caseMaybe bool (maybe bool) (var (maybe bool) "b") "presentValue" missing (choose (var bool "presentValue")))
        (caseMaybe bool (maybe bool) (var (maybe bool) "b") "presentValue2" (choose (var bool "presentValue1"))
            (just bool (binary (if isAnd then "and" else "or") bool bool (var bool "presentValue1") (var bool "presentValue2"))))
xor = caseMaybe bool (maybe bool) (var (maybe bool) "a") "presentValue" (nothing bool)
    (caseMaybe bool (maybe bool) (var (maybe bool) "b") "presentValue1" (nothing bool)
        (just bool (binary "xor" bool bool (var bool "presentValue") (var bool "presentValue1"))))
nullableNot = caseMaybe bool (maybe bool) (var (maybe bool) "value") "presentValue" (nothing bool) (just bool (not (var bool "presentValue")))
constructedAge = letIn int "localData" optional constructOptional (whole 18)
nullableLocal = letIn int "localValue" (maybe int) (just int (whole 18)) (whole 18)
referenceCall = caseMaybe string string
    (V.Apply (maybe string) (V.Reference (T.Function () (maybe string) (maybe string)) (p "Example",p "CurrentSupported.CurrentSupportedExample",n "EchoReference")) (var (maybe string) "value"))
    "presentValue" (text "unknown") (var string "presentValue")
guardCopy = letIn int "localCopy" optional data
    (caseMaybe int int age "presentValue" (whole 0) (letIn int "parameterData" optional defaultOptional (var int "presentValue")))
coalesceState = letIn int "localResult" int (whole 3)
    (caseMaybe int int (var (maybe int) "value") "presentValue"
        (letIn int "localResult1" int (whole 4) (letIn int "localActual" int (var int "localResult1") (var int "localResult1")))
        (letIn int "localActual1" int (var int "presentValue") (var int "localResult")))

defaultFromMember = caseMaybe int int (var (maybe int) "value") "presentValue" (whole 99) (var int "presentValue")
initializeMissing = caseMaybe int int (var (maybe int) "value") "presentValue"
    (letIn int "parameterValue" (maybe int) (just int (whole 18)) (whole 18)) (var int "presentValue")

sample =
    case D3Sample.sample of
        Library package dependencies definition ->
            let
                additions = Dict.fromList
                    [(n "MissingAdult",wrap [input "data" optional] bool adult)
                    ,(n "RegionOrUnknown",wrap [input "data" optional] string regionDefault)
                    ,(n "RegionLength",wrap [input "data" optional] int regionLength)
                    ,(n "GuardedAnnotation",wrap [input "data" annotated] int guardedAnnotation)
                    ,(n "NullableAddZero",wrap [input "value" (maybe int)] (maybe int) addZero)
                    ,(n "NullableEqual",wrap [input "a" (maybe int),input "b" (maybe int)] bool equal)
                    ,(n "NullableGreater",wrap [input "a" (maybe int),input "b" (maybe int)] bool greater)
                    ,(n "NullableAnd",wrap [input "a" (maybe bool),input "b" (maybe bool)] (maybe bool) (nullableBoolean True))
                    ,(n "NullableOr",wrap [input "a" (maybe bool),input "b" (maybe bool)] (maybe bool) (nullableBoolean False))
                    ,(n "NullableXor",wrap [input "a" (maybe bool),input "b" (maybe bool)] (maybe bool) xor)
                    ,(n "NullableNot",wrap [input "value" (maybe bool)] (maybe bool) nullableNot)
                    ,(n "DefaultOptional",wrap [] optional defaultOptional)
                    ,(n "ConstructOptional",wrap [] optional constructOptional)
                    ,(n "ConstructedAge",wrap [] int constructedAge)
                    ,(n "NullableLocal",wrap [] int nullableLocal)
                    ,(n "NullResult",wrap [] (maybe string) (nothing string))
                    ,(n "EchoReference",wrap [input "value" (maybe string)] (maybe string) (var (maybe string) "value"))
                    ,(n "ReferenceCall",wrap [input "value" (maybe string)] string referenceCall)
                    ,(n "GuardCopy",wrap [input "data" optional] int guardCopy)
                    ,(n "CoalesceState",wrap [input "value" (maybe int)] int coalesceState)
                    ,(n "DefaultFromMember",wrap [input "value" (maybe int)] int defaultFromMember)
                    ,(n "InitializeMissing",wrap [input "value" (maybe int)] int initializeMissing)
                    ,(n "DefaultRecord",wrap [] optional defaultOptional)]
                types = Dict.fromList
                    [(n "OptionalCustomer",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "age",tpe=maybe int},{name=n "region",tpe=maybe string}])}})
                    ,(n "AnnotatedReference",{access=Public,value={doc="",value=T.TypeAliasDefinition [] (T.Record () [{name=n "region",tpe=maybe string}])}})]
                extendValues controlled = let original=controlled.value in {controlled|value={original|values=Dict.union additions original.values}}
                extendTypes controlled = let original=controlled.value in {controlled|value={original|types=Dict.union types original.types}}
            in Library package dependencies {modules=definition.modules |> Dict.update (p "CurrentSupported") (Maybe.map extendTypes) |> Dict.update (p "CurrentSupported.CurrentSupportedExample") (Maybe.map extendValues)}
