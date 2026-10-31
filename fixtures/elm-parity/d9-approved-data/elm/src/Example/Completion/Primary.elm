module Example.Completion.Primary exposing (..)
import Example.Completion exposing (Primary)
new0NoneInt : Int -> Primary
new0NoneInt value = { value = value }
read : Primary -> Int
read this = this.value
