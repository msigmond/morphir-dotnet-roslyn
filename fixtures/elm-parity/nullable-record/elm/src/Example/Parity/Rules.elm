module Example.Parity.Rules exposing (..)

import Example.Parity exposing (..)

identity : Maybe Cell -> Maybe Cell
identity cell = cell
read : Cell -> Maybe String
read cell = cell.value
