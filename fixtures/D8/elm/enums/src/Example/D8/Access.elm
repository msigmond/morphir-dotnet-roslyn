module Example.D8.Access exposing (all, alias, none, read, write)
import Example.D8 exposing (Access(..))
none : Access
none = Access 0
read : Access
read = Access 1
alias : Access
alias = Access 1
write : Access
write = Access 2
all : Access
all = Access 3
