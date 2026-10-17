module D3Sample exposing (sample)

import D2Sample
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
string = T.Reference () (p "Morphir.SDK",p "String",n "String") []
customer = T.Reference () (p "Example",p "CurrentSupported",n "Customer") []
var t name = V.Variable t (n name)
whole number = V.Literal int (L.WholeNumberLiteral number)
text value = V.Literal string (L.StringLiteral value)
input name t = (n name,t,t)
letIn result name boundType bound body = V.LetDefinition result (n name) {inputTypes=[],outputType=boundType,body=bound} body
binary name arg result left right = V.Apply result (V.Apply (T.Function () arg result) (V.Reference (T.Function () arg (T.Function () arg result)) (p "Morphir.SDK",p "Basics",n name)) left) right
not value = V.Apply bool (V.Reference (T.Function () bool bool) (p "Morphir.SDK",p "Basics",n "not")) value
wrap inputs result body = {access=Public,value={doc="",value={inputTypes=inputs,outputType=result,body=body}}}
category = letIn int "localCategory" int (whole 0)
    (V.IfThenElse int (var bool "active")
        (letIn int "localCategory1" int (whole 1)
            (V.IfThenElse int (binary "greaterThanOrEqual" int bool (var int "age") (whole 18))
                (letIn int "localCategory2" int (whole 2) (var int "localCategory2"))
                (var int "localCategory1")))
        (var int "localCategory"))
early = V.IfThenElse int (not (var bool "active")) (whole 0)
    (letIn int "localAdult" int (whole 18)
        (V.IfThenElse int (binary "greaterThanOrEqual" int bool (var int "age") (var int "localAdult")) (whole 2) (whole 1)))
reassign = letIn int "localZero" int (whole 0)
    (letIn int "parameterValue" int (binary "add" int int (var int "value") (var int "localZero")) (var int "parameterValue"))
scoped = letIn int "localCopy" int (var int "value")
    (letIn int "localExtra" int (whole 0)
        (letIn int "localCopy1" int (binary "add" int int (var int "localCopy") (var int "localExtra")) (var int "localCopy1")))
initLater = V.IfThenElse int (var bool "active")
    (letIn int "localResult" int (whole 1) (var int "localResult"))
    (letIn int "localResult1" int (whole 2) (var int "localResult1"))
boolean = binary "xor" bool bool
    (binary "and" bool bool (not (binary "equal" bool bool (var bool "a") (var bool "b"))) (binary "or" bool bool (var bool "a") (var bool "b")))
    (binary "notEqual" bool bool (var bool "a") (var bool "b"))
knownLabel = letIn string "localLabel" string (text "eligible")
    (V.IfThenElse string (var bool "active") (var string "localLabel") (text "inactive"))
valueCopy = letIn int "localCopy" customer (var customer "customer")
    (letIn int "parameterCustomer" customer (V.Record customer (Dict.fromList [(n "active",V.Literal bool (L.BoolLiteral False)),(n "age",whole 0)]))
        (V.Field int (var customer "localCopy") (n "Age")))

assignmentOrder = letIn int "parameterValue" int (whole 0)
    (binary "add" int int (var int "value") (var int "parameterValue"))
nestedAssignment = letIn int "parameterOther" int (whole 1)
    (letIn int "parameterValue" int (binary "add" int int (var int "parameterOther") (whole 0)) (var int "parameterOther"))
shortAssignment = V.IfThenElse bool (var bool "active")
    (letIn bool "parameterOther" bool (V.Literal bool (L.BoolLiteral False))
        (letIn bool "localResult" bool (var bool "parameterOther") (var bool "parameterOther")))
    (letIn bool "localResult1" bool (V.Literal bool (L.BoolLiteral False)) (var bool "other"))
conditionalAssignment = V.IfThenElse int (var bool "active")
    (letIn int "parameterValue" int (whole 1) (var int "parameterValue"))
    (letIn int "parameterValue1" int (whole 2) (var int "parameterValue1"))
argumentAssignment = letIn int "parameterValue" int (whole 0)
    (V.Apply int (V.Apply (T.Function () int int)
        (V.Reference (T.Function () int (T.Function () int int)) (p "Example",p "CurrentSupported.CurrentSupportedExample",n "FirstArgument"))
        (var int "value")) (var int "parameterValue"))

sample =
    case D2Sample.sample of
        Library package dependencies definition ->
            let
                moduleName = p "CurrentSupported.CurrentSupportedExample"
                additions = Dict.fromList
                    [(n "Category",wrap [input "age" int,input "active" bool] int category)
                    ,(n "Early",wrap [input "age" int,input "active" bool] int early)
                    ,(n "Reassign",wrap [input "value" int] int reassign)
                    ,(n "Scoped",wrap [input "value" int] int scoped)
                    ,(n "InitLater",wrap [input "active" bool] int initLater)
                    ,(n "Boolean",wrap [input "a" bool,input "b" bool] bool boolean)
                    ,(n "KnownLabel",wrap [input "active" bool] string knownLabel)
                    ,(n "ConstantBranch",wrap [] int (whole 3))
                    ,(n "ValueCopy",wrap [input "customer" customer] int valueCopy)
                    ,(n "AssignmentOrder",wrap [input "value" int] int assignmentOrder)
                    ,(n "NestedAssignment",wrap [input "value" int,input "other" int] int nestedAssignment)
                    ,(n "ShortAssignment",wrap [input "active" bool,input "other" bool] bool shortAssignment)
                    ,(n "ConditionalAssignment",wrap [input "active" bool,input "value" int] int conditionalAssignment)
                    ,(n "ArgumentAssignment",wrap [input "value" int] int argumentAssignment)
                    ,(n "FirstArgument",{access=Private,value={doc="",value={inputTypes=[input "first" int,input "second" int],outputType=int,body=var int "first"}}})]
                extend controlled = let original=controlled.value in {controlled|value={original|values=Dict.union additions original.values}}
            in Library package dependencies {modules=Dict.update moduleName (Maybe.map extend) definition.modules}
