(*
   Various helper functions
*)

module Helpers

//Math functionality
let random = System.Random ((int) System.DateTime.Now.Ticks)
let randomHash () = random.Next ()
let normalize (num : double) (min : double) (max : double) = (num - min) / (max - min)