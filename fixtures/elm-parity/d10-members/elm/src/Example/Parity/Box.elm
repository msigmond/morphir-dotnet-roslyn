module Example.Parity.Box exposing (..)
import Example.Parity exposing (Box)
read : Box t -> t
read this = this.value
getCurrent0 : Box t -> t
getCurrent0 this = this.value
