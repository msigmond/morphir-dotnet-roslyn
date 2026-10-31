module Example.Completion.Data exposing (..)
import Example.Completion exposing (Data)
read : Data -> Int -> Int
read this extra = this.value + extra

getText0 : Data -> String
getText0 this = "x"
