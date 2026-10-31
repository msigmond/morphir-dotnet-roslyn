module Example.Parity.Rules exposing (..)

import Example.Parity exposing (..)

classRead : ClassData -> Int
classRead data = data.value
structRead : StructData -> Int
structRead data = data.value
recordRead : RecordData -> Int
recordRead data = data.value
mutableRecordRead : MutableRecord -> Int
mutableRecordRead data = data.value
readonlyRead : ReadonlyData -> Int
readonlyRead data = data.value
