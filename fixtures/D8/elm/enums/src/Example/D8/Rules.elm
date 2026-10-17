module Example.D8.Rules exposing (describe)
import Example.D8 exposing (Access(..))
import Example.D8.Access as Access

describe : Access -> ( Int, Bool )
describe access =
    ( (case access of
        Access enumValue -> enumValue
      )
    , access == Access.read
    )
