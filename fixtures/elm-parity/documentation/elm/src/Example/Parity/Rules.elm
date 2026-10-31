module Example.Parity.Rules exposing (read)
import Example.Parity exposing (Data)
{-|Written operation.-}
read : Data -> Int
read data = data.value
